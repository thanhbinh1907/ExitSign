using UnityEngine;
using UnityEngine.UI; // Cần cho Sliders, Buttons
using UnityEngine.SceneManagement; // Cần để tải Main Menu
using UnityEngine.Audio; // Cần cho AudioMixer
using Photon.Pun; // <-- Đã có

// Kế thừa MonoBehaviourPunCallbacks là đúng
public class PauseManager : MonoBehaviourPunCallbacks
{
	[Header("UI Panels")]
	public GameObject pauseMenuPanel;
	public GameObject settingsMenuPanel;

	[Header("Settings Components")]
	public AudioMixer masterMixer;
	public Slider volumeSlider;
	public Slider sensitivitySlider;

	[Header("Player Reference")]
	public PlayerMovement playerMovement;

	private bool isPaused = false;
	private bool isLeaving = false; // <-- Thêm biến này để tránh gọi leave 2 lần

	private const string MIXER_VOLUME = "MasterVolume";
	private const string PREFS_VOLUME = "MasterVolume";
	private const string PREFS_SENS = "MouseSensitivity";

	void Start()
	{
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		LoadSettings();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}
	}

	// --- HÀM TẠM DỪNG CHÍNH ---

	public void PauseGame()
	{
		isPaused = true;
		Time.timeScale = 0f;
		pauseMenuPanel.SetActive(true);
		settingsMenuPanel.SetActive(false);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void ResumeGame()
	{
		isPaused = false;
		Time.timeScale = 1f;
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// --- CÁC HÀM CHO NÚT BẤM ---

	public void OnSettingsButton()
	{
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(true);
	}

	// --- HÀM ĐÃ SỬA ---
	public void OnMainMenuButton()
	{
		if (isLeaving) return; // Nếu đã nhấn, không làm gì thêm
		isLeaving = true; // Đánh dấu là đang rời

		Time.timeScale = 1f;

		if (PhotonNetwork.InRoom)
		{
			// --- LOGIC MỚI BẮT ĐẦU TỪ ĐÂY ---

			if (PhotonNetwork.IsMasterClient)
			{
				// Nếu tôi là Master Client
				Debug.Log("Master Client rời đi. Gửi RPC cho những người khác...");

				// Gửi RPC đến TẤT CẢ NHỮNG NGƯỜI KHÁC (RpcTarget.Others)
				photonView.RPC("KickFromRoom", RpcTarget.Others);

				// Tự mình rời phòng (sẽ gọi OnLeftRoom() sau đó)
				PhotonNetwork.LeaveRoom();
			}
			else
			{
				// Nếu tôi là Guest (Client thường)
				Debug.Log("Guest Client tự rời đi...");
				PhotonNetwork.LeaveRoom();
			}
			// --- LOGIC MỚI KẾT THÚC ---
		}
		else
		{
			// Nếu không ở trong phòng (test offline)
			SceneManager.LoadScene("Menu");
		}
	}

	// --- HÀM MỚI: RPC ĐỂ KICK GUEST ---
	// Hàm này sẽ được gọi trên máy của Guest khi Master Client rời đi
	[PunRPC]
	void KickFromRoom()
	{
		isLeaving = true; // Đánh dấu là đang rời
		Debug.Log("Master Client đã rời, bạn bị đưa về Main Menu.");

		// Rời phòng. Hàm OnLeftRoom() sẽ được gọi ngay sau đó.
		PhotonNetwork.LeaveRoom();
	}

	// --- PHOTON CALLBACK (Giữ nguyên) ---
	// Hàm này sẽ được gọi sau khi Photon rời phòng thành công
	// (Áp dụng cho cả Master và Guest)
	public override void OnLeftRoom()
	{
		// Tải cảnh "Menu"
		Debug.Log("Đã rời phòng Photon, đang tải Main Menu...");
		SceneManager.LoadScene("Menu");
	}

	public void OnBackButton()
	{
		settingsMenuPanel.SetActive(false);
		pauseMenuPanel.SetActive(true);
	}

	// --- CÁC HÀM CÀI ĐẶT (Giữ nguyên) ---

	public void SetVolume(float volume)
	{
		masterMixer.SetFloat(MIXER_VOLUME, Mathf.Log10(volume) * 20);
		PlayerPrefs.SetString(PREFS_VOLUME, volume.ToString());
	}

	public void SetSensitivity(float sensitivity)
	{
		PlayerPrefs.SetFloat(PREFS_SENS, sensitivity);
		if (playerMovement != null)
		{
			playerMovement.UpdateSensitivity(sensitivity);
		}
	}

	void LoadSettings()
	{
		// Sửa lỗi nhỏ: Dùng PlayerPrefs.GetFloat thay vì getstring
		float volume = PlayerPrefs.GetFloat(PREFS_VOLUME, 1f);
		volumeSlider.value = volume;
		SetVolume(volume);

		float sensitivity = PlayerPrefs.GetFloat(PREFS_SENS, 100f);
		sensitivitySlider.value = sensitivity;
		SetSensitivity(sensitivity);
	}
}