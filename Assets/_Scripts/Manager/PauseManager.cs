using UnityEngine;
using UnityEngine.UI; // Cần cho Sliders, Buttons
using UnityEngine.SceneManagement; // Cần để tải Main Menu
using UnityEngine.Audio; // Cần cho AudioMixer

public class PauseManager : MonoBehaviour
{
	[Header("UI Panels")]
	public GameObject pauseMenuPanel;
	public GameObject settingsMenuPanel;

	[Header("Settings Components")]
	public AudioMixer masterMixer;
	public Slider volumeSlider;
	public Slider sensitivitySlider;

	[Header("Player Reference")]
	// Kéo Player của bạn vào đây
	public PlayerMovement playerMovement; // Giả sử script của bạn tên là PlayerMovement

	private bool isPaused = false;

	// Tên của thông số bạn đã expose trong AudioMixer
	private const string MIXER_VOLUME = "MasterVolume";
	private const string PREFS_VOLUME = "MasterVolume";
	private const string PREFS_SENS = "MouseSensitivity";

	void Start()
	{
		// Ẩn tất cả UI và khóa chuột khi bắt đầu
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		// Tải cài đặt đã lưu
		LoadSettings();
	}

	void Update()
	{
		// Kiểm tra nếu nhấn nút ESC
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
		Time.timeScale = 0f; // Dừng thời gian trong game
		pauseMenuPanel.SetActive(true);
		settingsMenuPanel.SetActive(false); // Đảm bảo panel cài đặt tắt

		// Mở khóa chuột
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public void ResumeGame()
	{
		isPaused = false;
		Time.timeScale = 1f; // Chạy lại thời gian
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(false);

		// Khóa chuột lại
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// --- CÁC HÀM CHO NÚT BẤM ---

	public void OnSettingsButton()
	{
		pauseMenuPanel.SetActive(false);
		settingsMenuPanel.SetActive(true);
	}

	public void OnMainMenuButton()
	{
		Time.timeScale = 1f; // Phải chạy lại thời gian trước khi đổi scene
							 // Thay "MainMenu" bằng tên Scene menu chính của bạn
		SceneManager.LoadScene("MainMenu");
	}

	public void OnBackButton()
	{
		settingsMenuPanel.SetActive(false);
		pauseMenuPanel.SetActive(true);
	}

	// --- CÁC HÀM CHO CÀI ĐẶT (SLIDERS) ---

	public void SetVolume(float volume)
	{
		// Công thức chuyển đổi giá trị slider (0.0001-1) sang decibel (-80 đến 0)
		masterMixer.SetFloat(MIXER_VOLUME, Mathf.Log10(volume) * 20);
		PlayerPrefs.SetFloat(PREFS_VOLUME, volume); // Lưu cài đặt
	}

	public void SetSensitivity(float sensitivity)
	{
		PlayerPrefs.SetFloat(PREFS_SENS, sensitivity); // Lưu cài đặt

		// Cập nhật độ nhạy của người chơi ngay lập tức
		if (playerMovement != null)
		{
			playerMovement.UpdateSensitivity(sensitivity);
		}
	}

	void LoadSettings()
	{
		// Tải Âm lượng
		float volume = PlayerPrefs.GetFloat(PREFS_VOLUME, 1f); // Mặc định là 1 (tối đa)
		volumeSlider.value = volume;
		SetVolume(volume); // Áp dụng ngay

		// Tải Độ nhạy
		// Giả sử độ nhạy mặc định của bạn là 2f
		float sensitivity = PlayerPrefs.GetFloat(PREFS_SENS, 100f);
		sensitivitySlider.value = sensitivity;
		SetSensitivity(sensitivity); // Áp dụng ngay
	}
}