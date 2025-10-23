using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Chúng ta dùng "MonoBehaviourPunCallbacks" thay vì "MonoBehaviour"
public class NetworkManager : MonoBehaviourPunCallbacks
{
	void Start()
	{
		Debug.Log("Đang kết nối tới server Photon...");
		// 1. Kết nối tới Photon server bằng cài đặt của bạn
		PhotonNetwork.ConnectUsingSettings();
	}

	// 2. Hàm này được tự động gọi khi kết nối Master server thành công
	public override void OnConnectedToMaster()
	{
		Debug.Log("Đã kết nối Master Server! Đang tham gia phòng...");
		// 3. Tham gia một phòng. Nếu phòng không tồn tại, nó sẽ tự tạo
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = 2; // Giới hạn 2 người chơi
		PhotonNetwork.JoinOrCreateRoom("phong_choi_123", roomOptions, TypedLobby.Default);
	}

	// 4. Hàm này được tự động gọi khi vào phòng thành công
	public override void OnJoinedRoom()
	{
		Debug.Log("Đã vào phòng! Tên phòng: " + PhotonNetwork.CurrentRoom.Name);

		// 5. SPWAN (TẠO RA) NHÂN VẬT NGAY TẠI ĐÂY
		// Thay "TenPrefabNhanVat" bằng tên prefab của bạn ở Bước 3
		PhotonNetwork.Instantiate("Player", new Vector3(0, 1, 0), Quaternion.identity);
	}
}