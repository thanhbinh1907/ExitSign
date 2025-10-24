using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
	[Header("Main Menu UI")]
	public GameObject mainMenuPanel;
	public GameObject lobbyPanel;
	public TMP_InputField playerNameInput;
	public Button multiplayerButton;
	public Button singlePlayerButton;
	public Button settingButton;
	public Button quitButton;

	[Header("References")]
	public NetworkManager networkManager;

	void Start()
	{
		// Thiết lập các button listeners
		multiplayerButton.onClick.AddListener(OnMultiplayerClick);

		// Load tên người chơi đã lưu
		string savedName = PlayerPrefs.GetString("playerName", "");
		if (!string.IsNullOrEmpty(savedName))
		{
			playerNameInput.text = savedName;
		}

		// Lắng nghe thay đổi tên
		playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
	}

	public void OnMultiplayerClick()
	{
		// Kiểm tra tên người chơi
		string playerName = playerNameInput.text.Trim();
		if (string.IsNullOrEmpty(playerName))
		{
			Debug.LogWarning("Vui lòng nhập tên người chơi!");
			// Có thể hiện thông báo lỗi ở đây
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
		// Cập nhật real-time
		PlayerPrefs.SetString("playerName", newName);
	}

	public void BackToMainMenu()
	{
		// Gọi từ lobby để quay về main menu
		lobbyPanel.SetActive(false);
		mainMenuPanel.SetActive(true);
	}
}