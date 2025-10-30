using UnityEngine;
using UnityEngine.UI; // Cần cho Sliders, Buttons
using UnityEngine.SceneManagement; // Cần để tải Main Menu
using UnityEngine.Audio; // Cần cho AudioMixer
using Photon.Pun;

public class PauseManager : MonoBehaviourPunCallbacks
{
	[Header("UI Panels")]
	public GameObject pauseMenuPanel;
	public GameObject settingsMenuPanel;

	[Header("Settings Components")]
	public AudioMixer masterMixer;
	public Slider volumeSlider;
	public Slider sensitivitySlider;

	private bool isPaused = false;
	private bool isLeaving = false;

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

	public void OnSettingsButton()
	{
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(true);
	}

	public void OnMainMenuButton()
	{
		if (isLeaving) return;
		isLeaving = true;

		Time.timeScale = 1f;

		if (PhotonNetwork.InRoom)
		{
			if (PhotonNetwork.IsMasterClient)
			{
				Debug.Log("Master Client rời đi. Gửi RPC cho những người khác...");
				photonView.RPC("KickFromRoom", RpcTarget.Others);
				PhotonNetwork.LeaveRoom();
			}
			else
			{
				Debug.Log("Guest Client tự rời đi...");
				PhotonNetwork.LeaveRoom();
			}
		}
		else
		{
			SceneManager.LoadScene("Menu");
		}
	}

	[PunRPC]
	void KickFromRoom()
	{
		isLeaving = true;
		Debug.Log("Master Client đã rời, bạn bị đưa về Main Menu.");
		PhotonNetwork.LeaveRoom();
	}

	// --- HÀM ĐÃ SỬA ---
	// Hàm này sẽ được gọi sau khi Photon rời phòng thành công
	public override void OnLeftRoom()
	{
		// --- THÊM 2 DÒNG NÀY ---
		// Đảm bảo giải phóng chuột TRƯỚC KHI tải scene mới,
		// áp dụng cho cả Master và Guest.
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		// --- KẾT THÚC SỬA ---

		Debug.Log("Đã rời phòng Photon, đang tải Main Menu...");
		SceneManager.LoadScene("Menu");
	}

	public void OnBackButton()
	{
		settingsMenuPanel.SetActive(false);
		pauseMenuPanel.SetActive(true);
	}

	public void SetVolume(float volume)
	{
		masterMixer.SetFloat(MIXER_VOLUME, Mathf.Log10(volume) * 20);
		PlayerPrefs.SetFloat(PREFS_VOLUME, volume); // Đổi về SetFloat
	}

	public void SetSensitivity(float sensitivity)
	{
		PlayerPrefs.SetFloat(PREFS_SENS, sensitivity); // Lưu cài đặt

		// --- THAY ĐỔI BẮT ĐẦU ---
		// Bỏ tham chiếu "playerMovement" cũ

		// 1. Tìm TẤT CẢ các script PlayerMovement trong scene
		PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();

		// 2. Lặp qua và tìm script của CHÍNH MÌNH (local player)
		foreach (PlayerMovement player in allPlayers)
		{
			// Dùng photonView.IsMine để đảm bảo chỉ cập nhật player của mình
			if (player.photonView != null && player.photonView.IsMine)
			{
				// 3. Gọi hàm cập nhật
				player.UpdateSensitivity(sensitivity);
				Debug.Log("Đã cập nhật sensitivity cho local player.");
				break; // Đã tìm thấy, thoát vòng lặp
			}
		}
		// --- THAY ĐỔI KẾT THÚC ---
	}

	void LoadSettings()
	{
		float volume = PlayerPrefs.GetFloat(PREFS_VOLUME, 1f);
		volumeSlider.value = volume;
		SetVolume(volume);

		float sensitivity = PlayerPrefs.GetFloat(PREFS_SENS, 100f);
		sensitivitySlider.value = sensitivity;
		SetSensitivity(sensitivity);
	}
}