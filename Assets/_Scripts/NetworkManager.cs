using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
	[Header("UI References")]
	public TMP_InputField roomInputField;
	public GameObject lobbyPanel;
	public TMP_Text statusText;

	void Start()
	{
		Debug.Log("Đang kết nối tới server Photon...");
		statusText.text = "Đang kết nối...";
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.ConnectUsingSettings();
	}

	public override void OnConnectedToMaster()
	{
		Debug.Log("Đã kết nối Master Server!");
		statusText.text = "Đã kết nối! Hãy tạo/vào phòng.";
		PhotonNetwork.JoinLobby();
	}

	public void OnClick_CreateRoom()
	{
		if (string.IsNullOrEmpty(roomInputField.text))
		{
			statusText.text = "Mã phòng không được để trống!";
			return;
		}

		statusText.text = "Đang tạo phòng...";
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = 2;
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
		PhotonNetwork.JoinRoom(roomInputField.text);
	}

	public override void OnJoinedRoom()
	{
		Debug.Log($"Đã vào phòng! Số người hiện tại: {PhotonNetwork.CurrentRoom.PlayerCount}/2");
		lobbyPanel.SetActive(false);

		// QUAN TRỌNG: Chỉ Master Client mới được load scene và CHỈ KHI có đủ 2 người
		if (PhotonNetwork.IsMasterClient)
		{
			if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
			{
				Debug.Log("Đủ 2 người! Master Client đang load scene...");
				statusText.text = "Đang tải game...";
				PhotonNetwork.LoadLevel("Gameplay");
			}
			else
			{
				Debug.Log("Master Client đang đợi người chơi thứ 2...");
				statusText.text = "Đang đợi người chơi thứ 2...";
			}
		}
		else
		{
			statusText.text = "Đã vào phòng! Đang đợi chủ phòng...";
		}
	}

	// QUAN TRỌNG: Khi có người mới vào phòng
	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		Debug.Log($"Người chơi mới: {newPlayer.NickName}. Tổng: {PhotonNetwork.CurrentRoom.PlayerCount}/2");

		// CHỈ Master Client mới load scene khi đủ người
		if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
		{
			Debug.Log("Đủ 2 người! Master Client đang load scene...");
			PhotonNetwork.LoadLevel("Gameplay");
		}
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		Debug.LogError("Tạo phòng thất bại: " + message);
		statusText.text = "Lỗi: Phòng này đã tồn tại!";
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.LogError("Vào phòng thất bại: " + message);
		statusText.text = "Lỗi: Phòng không tồn tại hoặc đã đầy!";
	}
}