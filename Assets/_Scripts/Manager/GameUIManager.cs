using UnityEngine;
using UnityEngine.UI; // Cần cho Buttons
using Photon.Pun; // Cần cho PhotonNetwork
using UnityEngine.SceneManagement; // Cần cho SceneManager
using TMPro;

// 1. THAY ĐỔI: Kế thừa từ MonoBehaviourPunCallbacks
public class GameUIManager : MonoBehaviourPunCallbacks
{
	[Header("UI Panels")]
	public GameObject winScreenPanel;
	public GameObject loseScreenPanel;

	[Header("Win Screen Buttons")]
	[Tooltip("Nút duy nhất trên màn hình Win (Tên cũ là winReplayButton)")]
	public Button winReplayButton; // Giờ đây là nút duy nhất

	[Header("Lose Screen Buttons")]
	[Tooltip("Nút duy nhất trên màn hình Lose (Tên cũ là loseReplayButton)")]
	public Button loseReplayButton; // Giờ đây là nút duy nhất

	[Header("Scene Names")]
	[Tooltip("Tên chính xác của cảnh Main Menu của bạn")]
	public string mainMenuSceneName = "MainMenu"; // Đảm bảo tên này ĐÚNG

	void Start()
	{
		// Ẩn cả hai màn hình khi bắt đầu
		if (winScreenPanel != null) winScreenPanel.SetActive(false);
		if (loseScreenPanel != null) loseScreenPanel.SetActive(false);

		// --- 2. THAY ĐỔI: Chỉ gán listener cho các nút TỒN TẠI ---
		// Nút "Replay" (nút duy nhất bạn muốn)
		if (winReplayButton != null)
		{
			winReplayButton.onClick.AddListener(OnLeaveGameClicked);
		}

		if (loseReplayButton != null)
		{
			loseReplayButton.onClick.AddListener(OnLeaveGameClicked);
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

	// --- 3. THAY ĐỔI: Sửa lỗi tự động rejoin ---
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
		else
		{
			// Nếu không ở trong phòng (test offline), tải scene ngay
			SceneManager.LoadScene(mainMenuSceneName);
		}
	}

	// --- 4. THÊM MỚI: Callback sau khi rời phòng ---
	/// <summary>
	/// Hàm này được Photon gọi TỰ ĐỘNG sau khi rời phòng thành công.
	/// </summary>
	public override void OnLeftRoom()
	{
		// Bây giờ mới an toàn để tải Main Menu
		Debug.Log("Đã rời phòng, đang tải Main Menu...");
		SceneManager.LoadScene(mainMenuSceneName);
	}
}