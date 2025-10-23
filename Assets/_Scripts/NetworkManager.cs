using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // 1. Thêm thư viện TextMeshPro

public class NetworkManager : MonoBehaviourPunCallbacks
{
	[Header("UI References")]
	public TMP_InputField roomInputField; // 2. Kéo InputField vào đây
	public GameObject lobbyPanel;         // 3. Kéo Panel UI vào đây
	public TMP_Text statusText;           // 4. Kéo Text trạng thái vào đây

	void Start()
	{
		Debug.Log("Đang kết nối tới server Photon...");
		statusText.text = "Đang kết nối...";

		// --- THÊM DÒNG NÀY ---
		// Tự động đồng bộ Scene cho tất cả client
		PhotonNetwork.AutomaticallySyncScene = true;

		// Chỉ kết nối, không vào phòng
		PhotonNetwork.ConnectUsingSettings();
	}

	// Hàm này được gọi khi kết nối Master server thành công
	public override void OnConnectedToMaster()
	{
		Debug.Log("Đã kết nối Master Server!");
		statusText.text = "Đã kết nối! Hãy tạo/vào phòng.";
		// Tự động tham gia Lobby. Điều này cho phép chúng ta
		// nhận được các thông báo khi tạo/vào phòng
		PhotonNetwork.JoinLobby();
	}

	// --- CÁC HÀM SẼ ĐƯỢC GỌI BẰNG NÚT BẤM ---

	public void OnClick_CreateRoom()
	{
		if (string.IsNullOrEmpty(roomInputField.text))
		{
			statusText.text = "Mã phòng không được để trống!";
			return;
		}

		statusText.text = "Đang tạo phòng...";
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = 2; // Giới hạn 2 người

		// Dùng CreateRoom thay vì JoinOrCreateRoom
		// Nó sẽ thất bại nếu phòng đã tồn tại
		PhotonNetwork.CreateRoom(roomInputField.text, roomOptions);
	}

	public void OnClick_JoinRoom()
	{
		if (string.IsNullOrEmpty(roomInputField.text))
		{
			statusText.text = "Mã phòng không được để trống!";
			return;
		}

		statusText.text = "Đang vào phòng...";
		// Chỉ tham gia phòng, sẽ thất bại nếu phòng không tồn tại
		PhotonNetwork.JoinRoom(roomInputField.text);
	}

	// --- CÁC HÀM CALLBACK (KẾT QUẢ) ---

	// Hàm này được gọi khi vào phòng thành công (cả tạo và tham gia)
	public override void OnJoinedRoom()
	{
		Debug.Log("Đã vào phòng! Tên phòng: " + PhotonNetwork.CurrentRoom.Name);
		statusText.text = "Vào phòng thành công! Đang đợi chủ phòng...";

		// Ẩn UI đi
		lobbyPanel.SetActive(false);

		// --- PHẦN QUAN TRỌNG ---
		// Chỉ chủ phòng (Master Client) mới có quyền load level
		// Những người chơi khác sẽ tự động đi theo (do AustomaticallySyncScene = true)
		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log("Bạn là chủ phòng! Đang tải màn chơi...");

			// Thay "TenSceneGame" bằng tên Scene Gameplay của bạn
			// Ví dụ: "Level_01", "MainGame", "Station"
			PhotonNetwork.LoadLevel("Gameplay");
		}
	}

	// Hàm này được gọi khi TẠO phòng thất bại
	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		Debug.LogError("Tạo phòng thất bại: " + message);
		statusText.text = "Lỗi: Phòng này đã tồn tại!";
	}

	// Hàm này được gọi khi VÀO phòng thất bại
	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.LogError("Vào phòng thất bại: " + message);
		statusText.text = "Lỗi: Phòng không tồn tại hoặc đã đầy!";
	}
}