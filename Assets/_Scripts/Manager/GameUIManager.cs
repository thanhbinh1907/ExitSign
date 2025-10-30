using UnityEngine;
using UnityEngine.UI; // Cần cho Buttons
using Photon.Pun; // Cần cho PhotonNetwork
using UnityEngine.SceneManagement; // Cần cho SceneManager
using TMPro;

public class GameUIManager : MonoBehaviour
{
	[Header("UI Panels")]
	public GameObject winScreenPanel;
	public GameObject loseScreenPanel;

	[Header("Win Screen Buttons")]
	public Button winReplayButton;
	public Button winMenuButton;

	[Header("Lose Screen Buttons")]
	public Button loseReplayButton;
	public Button loseMenuButton;

	[Header("Scene Names")]
	[Tooltip("Tên chính xác của cảnh Main Menu của bạn")]
	public string mainMenuSceneName = "MainMenu"; // Đảm bảo tên này ĐÚNG

	void Start()
	{
		// Ẩn cả hai màn hình khi bắt đầu
		if (winScreenPanel != null) winScreenPanel.SetActive(false);
		if (loseScreenPanel != null) loseScreenPanel.SetActive(false);

		// Gán listener cho TẤT CẢ các nút
		// Tất cả đều gọi chung một hàm: OnLeaveGameClicked
		if (winReplayButton != null)
		{
			winReplayButton.onClick.AddListener(OnLeaveGameClicked);
		}
		if (winMenuButton != null)
		{
			winMenuButton.onClick.AddListener(OnLeaveGameClicked);
		}
		if (loseReplayButton != null)
		{
			loseReplayButton.onClick.AddListener(OnLeaveGameClicked);
		}
		if (loseMenuButton != null)
		{
			loseMenuButton.onClick.AddListener(OnLeaveGameClicked);
		}
	}

	// Hàm này sẽ được gọi từ script trigger
	public void ShowWinScreen()
	{
		if (winScreenPanel != null)
		{
			winScreenPanel.SetActive(true);
			UnlockCursor();
		}
	}

	// Hàm này sẽ được gọi từ script trigger
	public void ShowLoseScreen()
	{
		if (loseScreenPanel != null)
		{
			loseScreenPanel.SetActive(true);
			UnlockCursor();
		}
	}

	// Hàm tiện ích để mở khóa chuột khi game kết thúc
	private void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	/// <summary>
	/// Hàm này được TẤT CẢ các nút (Replay, Main Menu) gọi.
	/// Nó sẽ rời phòng Photon và tải lại cảnh Main Menu.
	/// </summary>
	public void OnLeaveGameClicked()
	{
		Debug.Log("Nút được nhấn. Rời phòng và về Main Menu...");

		// Luôn gọi LeaveRoom() để dọn dẹp phòng
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
		}

		// Tải cảnh Main Menu
		// NetworkManager của bạn ở cảnh Main Menu sẽ tự động xử lý 
		// việc "đã rời phòng" và hiển thị lại Lobby.
		SceneManager.LoadScene(mainMenuSceneName);
	}
}