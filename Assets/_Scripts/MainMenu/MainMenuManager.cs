using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Thêm dòng này

public class MainMenuManager : MonoBehaviour
{
	[Header("Main Menu UI")]
	public GameObject mainMenuPanel;
	public GameObject lobbyPanel;
	public TMP_InputField playerNameInput;
	public Button multiplayerButton;
	public Button singlePlayerButton; // Thêm nút này
	public Button quitButton;

	[Header("References")]
	public NetworkManager networkManager;

	void Start()
	{
		// Thiết lập các button listeners
		multiplayerButton.onClick.AddListener(OnMultiplayerClick);
		singlePlayerButton.onClick.AddListener(OnSinglePlayerClick); // Thêm dòng này
		quitButton.onClick.AddListener(OnQuitClick);

		// Load tên người chơi đã lưu
		string savedName = PlayerPrefs.GetString("playerName", "");
		if (!string.IsNullOrEmpty(savedName))
		{
			playerNameInput.text = savedName;
		}

		// Lắng nghe thay đổi tên
		playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
	}

	// HÀM MỚI
	public void OnSinglePlayerClick()
	{
		// 1. Đặt trạng thái game thành SinglePlayer
		GameState.CurrentMode = GameMode.SinglePlayer;
		Debug.Log("Chế độ chơi đơn đã được chọn.");

		// 2. Tải trực tiếp màn chơi
		// Thay "Gameplay" bằng tên Scene game chính xác của bạn
		SceneManager.LoadScene("Gameplay");
	}

	public void OnMultiplayerClick()
	{
		// Đặt trạng thái game thành Multiplayer
		GameState.CurrentMode = GameMode.Multiplayer;
		Debug.Log("Chế độ nhiều người chơi đã được chọn.");

		// Kiểm tra tên người chơi
		string playerName = playerNameInput.text.Trim();
		if (string.IsNullOrEmpty(playerName))
		{
			Debug.LogWarning("Vui lòng nhập tên người chơi!");
			return;
		}

		// Lưu tên người chơi
		PlayerPrefs.SetString("playerName", playerName);

		// Chuyển sang lobby panel
		mainMenuPanel.SetActive(false);
		lobbyPanel.SetActive(true);

		// Cập nhật tên trong NetworkManager
		if (networkManager != null)
		{
			networkManager.OnPlayerNameChanged(playerName);
		}
	}

	public void OnPlayerNameChanged(string newName)
	{
		PlayerPrefs.SetString("playerName", newName);
	}

	public void BackToMainMenu()
	{
		lobbyPanel.SetActive(false);
		mainMenuPanel.SetActive(true);
	}

	public void OnQuitClick()
	{
		Debug.Log("Thoát game...");
		Application.Quit();
	}
}