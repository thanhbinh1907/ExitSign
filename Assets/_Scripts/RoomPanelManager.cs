using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class RoomPanelManager : MonoBehaviour
{
	[Header("Room Info")]
	public TMP_Text roomTitleText;
	public TMP_Text statusText;
	public TMP_Text playerCountText;

	[Header("Player List")]
	public Transform playerListContent;
	public GameObject playerItemPrefab;
	public ScrollRect playerListScrollRect;

	[Header("Control Buttons")]
	public Button startButton;
	public Button leaveButton;
	public Button readyButton; // Tùy chọn: nút ready cho người chơi

	[Header("UI Elements")]
	public GameObject masterClientIndicator; // Icon hiện khi là chủ phòng
	public TMP_Text waitingText; // Text "Đang chờ chủ phòng bắt đầu..."

	private string currentRoomName;
	private int maxPlayers;
	private List<GameObject> playerUIItems = new List<GameObject>();

	void Start()
	{
		// Thiết lập button listeners
		if (startButton != null)
		{
			startButton.onClick.AddListener(OnClickStart);
		}

		if (leaveButton != null)
		{
			leaveButton.onClick.AddListener(OnClickLeave);
		}

		if (readyButton != null)
		{
			readyButton.onClick.AddListener(OnClickReady);
		}
	}

	public void SetupRoom(string roomName, int maxPlayers)
	{
		this.currentRoomName = roomName;
		this.maxPlayers = maxPlayers;

		// Cập nhật UI
		if (roomTitleText != null)
		{
			roomTitleText.text = "Phòng: " + roomName;
		}

		UpdateRoomStatus();
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);
	}

	public void UpdatePlayerList(List<string> playerNames, bool isMaster)
	{
		// Xóa danh sách cũ
		ClearPlayerList();

		// Tạo UI item cho từng player
		foreach (string playerName in playerNames)
		{
			CreatePlayerItem(playerName);
		}

		// Cập nhật trạng thái UI
		UpdateMasterClientUI(isMaster);
		UpdateStartButton();
		UpdateRoomStatus();
	}

	void ClearPlayerList()
	{
		// Xóa tất cả player items
		foreach (GameObject item in playerUIItems)
		{
			if (item != null) Destroy(item);
		}
		playerUIItems.Clear();
	}

	void CreatePlayerItem(string playerName)
	{
		if (playerItemPrefab == null || playerListContent == null) return;

		GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);
		playerUIItems.Add(playerItem);

		// Thiết lập tên player
		PlayerItem playerItemScript = playerItem.GetComponent<PlayerItem>();
		if (playerItemScript != null)
		{
			playerItemScript.SetName(playerName);

			// Thêm icon nếu là master client
			if (playerName == PhotonNetwork.MasterClient.NickName)
			{
				playerItemScript.SetAsMasterClient(true);
			}
		}
		else
		{
			// Fallback: tìm text component
			TMP_Text nameText = playerItem.GetComponentInChildren<TMP_Text>();
			if (nameText != null)
			{
				nameText.text = playerName;

				// Thêm (Chủ phòng) nếu là master
				if (playerName == PhotonNetwork.MasterClient.NickName)
				{
					nameText.text += " (Chủ phòng)";
					nameText.color = Color.yellow; // Highlight master client
				}
			}
		}
	}

	void UpdateMasterClientUI(bool isMaster)
	{
		// Hiện/ẩn indicator chủ phòng
		if (masterClientIndicator != null)
		{
			masterClientIndicator.SetActive(isMaster);
		}

		// Cập nhật text chờ đợi
		if (waitingText != null)
		{
			if (isMaster)
			{
				waitingText.text = "Bạn là chủ phòng. Ấn Start để bắt đầu!";
			}
			else
			{
				waitingText.text = "Đang chờ chủ phòng bắt đầu game...";
			}
			waitingText.gameObject.SetActive(true);
		}
	}

	void UpdateStartButton()
	{
		if (startButton == null) return;

		bool isMaster = PhotonNetwork.IsMasterClient;
		bool hasEnoughPlayers = PhotonNetwork.CurrentRoom.PlayerCount >= 2;

		// Chỉ hiện start button cho master client
		startButton.gameObject.SetActive(isMaster);

		// Enable/disable dựa trên số người chơi
		startButton.interactable = isMaster && hasEnoughPlayers;

		// Cập nhật text trên button
		TMP_Text buttonText = startButton.GetComponentInChildren<TMP_Text>();
		if (buttonText != null)
		{
			if (hasEnoughPlayers)
			{
				buttonText.text = "Bắt đầu chơi";
			}
			else
			{
				buttonText.text = "Cần ít nhất 2 người";
			}
		}
	}

	void UpdateRoomStatus()
	{
		// Cập nhật số người chơi
		if (playerCountText != null)
		{
			int current = PhotonNetwork.CurrentRoom.PlayerCount;
			playerCountText.text = $"Người chơi: {current}/{maxPlayers}";
		}

		// Cập nhật status text
		if (statusText != null)
		{
			if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
			{
				statusText.text = "Đang chờ thêm người chơi...";
				statusText.color = Color.yellow;
			}
			else
			{
				statusText.text = "Sẵn sàng bắt đầu!";
				statusText.color = Color.green;
			}
		}
	}

	List<string> GetCurrentPlayerNames()
	{
		List<string> names = new List<string>();
		foreach (var player in PhotonNetwork.PlayerList)
		{
			names.Add(player.NickName);
		}
		return names;
	}

	// ================= BUTTON HANDLERS =================

	public void OnClickStart()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			Debug.LogWarning("Chỉ chủ phòng mới có thể bắt đầu game!");
			return;
		}

		if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
		{
			if (statusText != null)
			{
				statusText.text = "Cần ít nhất 2 người để bắt đầu!";
				statusText.color = Color.red;
			}
			return;
		}

		// THAY ĐỔI: Dùng FindFirstObjectByType thay vì FindObjectOfType
		var networkManager = FindFirstObjectByType<NetworkManager>();
		if (networkManager != null)
		{
			networkManager.OnMasterStartGame();
		}
		else
		{
			// Fallback: load scene trực tiếp
			PhotonNetwork.LoadLevel("Gameplay"); // Thay "Gameplay" bằng tên scene của bạn
		}
	}

	public void OnClickLeave()
	{
		// Xác nhận trước khi rời phòng
		if (statusText != null)
		{
			statusText.text = "Đang rời phòng...";
		}

		PhotonNetwork.LeaveRoom();
	}

	public void OnClickReady()
	{
		// Tùy chọn: implement ready system
		// Có thể dùng Custom Properties để track ready state
		Debug.Log("Ready button clicked - implement ready system here");
	}

	// ================= PHOTON CALLBACKS =================

	// Gọi từ NetworkManager khi có thay đổi về players
	public void OnPlayerListChanged()
	{
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);
	}
}