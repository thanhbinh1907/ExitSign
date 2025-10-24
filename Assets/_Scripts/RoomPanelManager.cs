using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class RoomPanelManager : MonoBehaviourPun
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

	[Header("Ready System")]
	private Dictionary<string, bool> playerReadyStates = new Dictionary<string, bool>();

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

		SetupPlayerListLayout();
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
		Debug.Log($"👥 Updating player list - {playerNames.Count} players");

		ClearPlayerList();

		// 🔥 RESET READY STATES WHEN PLAYER LIST CHANGES
		playerReadyStates.Clear();

		foreach (string playerName in playerNames)
		{
			CreatePlayerItem(playerName);
			// Initialize ready state as false
			playerReadyStates[playerName] = false;
		}

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
		Debug.Log($"👤 Creating player item for: '{playerName}'");

		if (playerItemPrefab == null || playerListContent == null) return;

		GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);
		playerUIItems.Add(playerItem);

		playerItem.SetActive(true);
		playerItem.transform.SetAsLastSibling();

		// 🔥 DEBUG: NAME MAPPING
		Debug.Log($"🏷️ PlayerItem created at index {playerUIItems.Count - 1} for player: '{playerName}'");

		PlayerItem playerItemScript = playerItem.GetComponent<PlayerItem>();
		if (playerItemScript != null)
		{
			playerItemScript.ResetIcons();
			playerItemScript.SetName(playerName);

			// 🔥 STORE ORIGINAL NAME IN PLAYERITEM FOR REFERENCE
			playerItemScript.originalPlayerName = playerName;

			bool isMasterClient = (playerName == PhotonNetwork.MasterClient.NickName);
			if (isMasterClient)
			{
				playerItemScript.SetAsMasterClient(true);
				Debug.Log($"👑 {playerName} is MASTER CLIENT");
			}
			else
			{
				playerItemScript.SetAsMasterClient(false);
				Debug.Log($"👤 {playerName} is regular player");
			}

			// 🔥 DEBUG FINAL DISPLAYED NAME
			if (playerItemScript.nameText != null)
			{
				Debug.Log($"🏷️ Final displayed name: '{playerItemScript.nameText.text}' for original: '{playerName}'");
			}
		}
	}

	System.Collections.IEnumerator RefreshPlayerListLayout()
	{
		yield return null; // Wait 1 frame

		if (playerListContent != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)playerListContent);
			Debug.Log("✅ Player list layout refreshed");
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

		// 🔥 CHECK IF ALL NON-MASTER PLAYERS ARE READY
		bool allPlayersReady = true;
		int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
		int readyCount = 0;

		foreach (var kvp in playerReadyStates)
		{
			string playerName = kvp.Key;
			bool isReady = kvp.Value;

			// Skip master client
			if (playerName == PhotonNetwork.MasterClient.NickName) continue;

			if (isReady)
			{
				readyCount++;
			}
			else
			{
				allPlayersReady = false;
			}
		}

		// If only master client in room, can start immediately
		if (totalPlayers == 1 && isMaster)
		{
			allPlayersReady = true;
		}

		// Show start button only for master
		startButton.gameObject.SetActive(isMaster);

		// Enable only if conditions met
		bool canStart = isMaster && hasEnoughPlayers && allPlayersReady;
		startButton.interactable = canStart;

		// Update button text
		TMP_Text buttonText = startButton.GetComponentInChildren<TMP_Text>();
		if (buttonText != null)
		{
			if (!hasEnoughPlayers)
			{
				buttonText.text = "Cần ít nhất 2 người";
			}
			else if (!allPlayersReady)
			{
				buttonText.text = $"Đợi sẵn sàng ({readyCount}/{totalPlayers - 1})";
			}
			else
			{
				buttonText.text = "Bắt đầu chơi";
			}
		}

		Debug.Log($"🎮 Start button - CanStart: {canStart}, Ready: {readyCount}/{totalPlayers - 1}");
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
		Debug.Log("🎯 Ready button clicked");

		string myNickname = PhotonNetwork.NickName;
		bool currentReadyState = playerReadyStates.ContainsKey(myNickname) ? playerReadyStates[myNickname] : false;
		bool newReadyState = !currentReadyState;

		// Update local state
		playerReadyStates[myNickname] = newReadyState;

		// 🔥 SEND READY STATE VIA RPC TO ALL PLAYERS
		photonView.RPC("UpdatePlayerReady", RpcTarget.All, myNickname, newReadyState);

		Debug.Log($"🎯 {myNickname} ready state: {newReadyState}");

		// Update ready button text
		if (readyButton != null)
		{
			TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
			if (buttonText != null)
			{
				buttonText.text = newReadyState ? "Cancel" : "Ready";
			}
		}
	}

	// 🔥 RPC METHOD - RECEIVE READY UPDATES
	[PunRPC]
	void UpdatePlayerReady(string playerName, bool isReady)
	{
		Debug.Log($"📡 RPC received: {playerName} ready = {isReady}");

		// Update ready state
		playerReadyStates[playerName] = isReady;

		// Update UI
		UpdatePlayerReadyState(playerName, isReady);

		// Update start button
		UpdateStartButton();
	}

	void UpdatePlayerReadyState(string playerName, bool isReady)
	{
		Debug.Log($"🔄 Updating ready state for {playerName}: {isReady}");

		foreach (GameObject playerItem in playerUIItems)
		{
			if (playerItem == null) continue;

			PlayerItem playerScript = playerItem.GetComponent<PlayerItem>();
			if (playerScript != null)
			{
				// 🔥 SỬ DỤNG ORIGINAL NAME THAY VÌ DISPLAYED NAME
				string storedName = playerScript.originalPlayerName;

				Debug.Log($"   Checking: StoredName='{storedName}', Looking for='{playerName}'");

				if (storedName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
				{
					Debug.Log($"✅ EXACT MATCH! Setting ready={isReady} for {playerName}");
					playerScript.SetReady(isReady);
					return;
				}
			}
		}

		Debug.LogWarning($"⚠️ Player item not found for: {playerName}");
	}

	// ================= PHOTON CALLBACKS =================

	// Gọi từ NetworkManager khi có thay đổi về players
	public void OnPlayerListChanged()
	{
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);
	}

	void SetupPlayerListLayout()
	{
		if (playerListContent == null) return;

		Debug.Log("🎯 Setting up player list layout...");

		// Ensure Vertical Layout Group
		VerticalLayoutGroup layoutGroup = playerListContent.GetComponent<VerticalLayoutGroup>();
		if (layoutGroup == null)
		{
			layoutGroup = playerListContent.gameObject.AddComponent<VerticalLayoutGroup>();
			Debug.Log("✅ Added VerticalLayoutGroup to PlayerListContent");
		}

		// Configure layout settings
		layoutGroup.childAlignment = TextAnchor.UpperCenter;     // Top-center alignment
		layoutGroup.childControlWidth = true;                    // Control child width
		layoutGroup.childControlHeight = false;                  // Don't control height
		layoutGroup.childForceExpandWidth = true;               // Force expand width
		layoutGroup.childForceExpandHeight = false;             // Don't force expand height
		layoutGroup.spacing = 5f;                               // 5px spacing between players

		// Set padding
		layoutGroup.padding = new RectOffset(5, 5, 5, 5);      // 5px padding all sides

		// Ensure Content Size Fitter
		ContentSizeFitter sizeFitter = playerListContent.GetComponent<ContentSizeFitter>();
		if (sizeFitter == null)
		{
			sizeFitter = playerListContent.gameObject.AddComponent<ContentSizeFitter>();
			Debug.Log("✅ Added ContentSizeFitter to PlayerListContent");
		}

		// Configure size fitter
		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Don't fit horizontal
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;    // Fit to content height

		Debug.Log("✅ Player list layout configured");
	}

	// 🔥 IMPLEMENT REQUIRED INTERFACE METHOD
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		// Not needed for this implementation
	}
}