using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
	[Header("Connection UI")]
	public TMP_Text statusText;

	[Header("Player Name")]
	public TMP_InputField playerNameInput;

	[Header("Lobby UI")]
	public GameObject lobbyPanel;
	public Transform roomListContent;
	public GameObject roomItemPrefab;
	public TMP_Text noRoomsText;

	[Header("Create Room UI")]
	public GameObject createRoomPanel;         // Panel chứa UI tạo phòng
	public TMP_InputField createRoomNameInput;
	public Toggle createPrivateToggle;
	public GameObject passwordContainer;        // Container chứa password field
	public TMP_InputField createPasswordInput;
	public TMP_InputField createMaxPlayersInput;
	public Button createRoomButton;
	public Button cancelCreateButton;

	[Header("Join Password Modal")]
	public GameObject joinPasswordModal;
	public TMP_InputField joinPasswordInput;
	public TMP_Text joinPasswordRoomNameText;
	public Button confirmPasswordButton;
	public Button cancelPasswordButton;

	[Header("Room Panel")]
	public GameObject roomPanel;
	public RoomPanelManager roomPanelManager;

	// Internal
	private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
	private string lastAttemptJoinRoomName = "";
	private string lastAttemptJoinPassword = "";

	private bool isCreatingRoom = false;
	private bool isProcessingRoomOperation = false;

	void Start()
	{
		// Thêm vào đầu Start() của NetworkManager
		NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
		if (managers.Length > 1)
		{
			Debug.LogError($"FOUND {managers.Length} NetworkManager instances! Destroying duplicates.");
			for (int i = 1; i < managers.Length; i++)
			{
				Destroy(managers[i].gameObject);
			}
			return;
		}

		Debug.Log("✅ Only 1 NetworkManager found - OK");

		PhotonNetwork.AutomaticallySyncScene = true;

		// Thiết lập UI listeners
		SetupUIListeners();

		// Kết nối Photon
		if (statusText != null) statusText.text = "Đang kết nối Photon...";
		PhotonNetwork.ConnectUsingSettings();

		// Load tên đã lưu
		LoadPlayerName();
	}

	void SetupUIListeners()
	{
		// Create room listeners
		if (createPrivateToggle != null)
		{
			createPrivateToggle.onValueChanged.AddListener(OnPrivateToggleChanged);
		}

		if (createRoomButton != null)
		{
			createRoomButton.onClick.AddListener(OnClick_CreateRoom);
		}

		if (cancelCreateButton != null)
		{
			cancelCreateButton.onClick.AddListener(() => createRoomPanel.SetActive(false));
		}

		// Password modal listeners
		if (confirmPasswordButton != null)
		{
			confirmPasswordButton.onClick.AddListener(OnConfirmJoinWithPassword);
		}

		if (cancelPasswordButton != null)
		{
			cancelPasswordButton.onClick.AddListener(() => joinPasswordModal.SetActive(false));
		}
	}

	void LoadPlayerName()
	{
		if (playerNameInput != null)
		{
			string saved = PlayerPrefs.GetString("playerName", "");
			playerNameInput.text = saved;
			PhotonNetwork.NickName = string.IsNullOrEmpty(saved) ? "Player" + Random.Range(1000, 9999) : saved;
		}
	}

	// ================= UI EVENT HANDLERS =================

	public void OnPrivateToggleChanged(bool isPrivate)
	{
		// Show/hide password container
		if (passwordContainer != null)
		{
			passwordContainer.SetActive(isPrivate);
		}
	}

	public void ShowCreateRoomPanel()
	{
		// Gọi từ button "Create Room" trong lobby
		createRoomPanel.SetActive(true);

		// Reset form
		createRoomNameInput.text = "";
		createPrivateToggle.isOn = false;
		createPasswordInput.text = "";
		passwordContainer.SetActive(false);
	}

	// ================= PHOTON CALLBACKS =================

	public override void OnConnectedToMaster()
	{
		if (statusText != null) statusText.text = "Đã kết nối. Đang vào Lobby...";
		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby()
	{
		if (statusText != null) statusText.text = "Đã vào Lobby";
		ClearRoomListUI();
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if (statusText != null) statusText.text = "Mất kết nối: " + cause.ToString();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		// Cập nhật cached list
		foreach (RoomInfo info in roomList)
		{
			if (info.RemovedFromList)
			{
				if (cachedRoomList.ContainsKey(info.Name))
					cachedRoomList.Remove(info.Name);
			}
			else
			{
				cachedRoomList[info.Name] = info;
			}
		}
		RefreshRoomListUI();
	}

	public override void OnJoinedRoom()
	{
		// Lưu tên người chơi
		PlayerPrefs.SetString("playerName", PhotonNetwork.NickName);

		// Kiểm tra mật khẩu nếu phòng private
		Room current = PhotonNetwork.CurrentRoom;
		if (current.CustomProperties.ContainsKey("pwd"))
		{
			if (!string.IsNullOrEmpty(lastAttemptJoinRoomName) && lastAttemptJoinRoomName == current.Name)
			{
				string expected = current.CustomProperties["pwd"] as string;
				if (expected != lastAttemptJoinPassword)
				{
					StartCoroutine(ShowWrongPasswordAndLeave());
					return;
				}
			}
			else
			{
				StartCoroutine(ShowWrongPasswordAndLeave());
				return;
			}
		}

		// Hiển thị room panel
		lobbyPanel.SetActive(false);
		roomPanel.SetActive(true);

		if (roomPanelManager != null)
		{
			roomPanelManager.SetupRoom(current.Name, current.MaxPlayers);
		}

		UpdatePlayerListInRoomPanel();
	}

	System.Collections.IEnumerator ShowWrongPasswordAndLeave()
	{
		if (statusText != null) statusText.text = "Sai mật khẩu!";
		yield return new WaitForSeconds(2f);
		PhotonNetwork.LeaveRoom();
		lastAttemptJoinRoomName = "";
		lastAttemptJoinPassword = "";
	}

	public override void OnLeftRoom()
	{
		Debug.Log("🚪 Left room");

		// Skip nếu object đã destroyed
		if (this == null) return;

		ResetFlags();

		// Simple null checks
		if (roomPanel != null)
			try { roomPanel.SetActive(false); } catch { }

		if (lobbyPanel != null)
			try { lobbyPanel.SetActive(true); } catch { }

		if (statusText != null)
			try { statusText.text = "Đã rời phòng"; } catch { }
	}

	void ResetFlags()
	{
		isCreatingRoom = false;
		isProcessingRoomOperation = false;

		// Re-enable create room button nếu tồn tại
		if (createRoomButton != null)
		{
			createRoomButton.interactable = true;
		}

		Debug.Log("🔄 Room operation flags reset");
	}

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		UpdatePlayerListInRoomPanel();
	}

	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		UpdatePlayerListInRoomPanel();
	}

	// ================= ROOM OPERATIONS =================

	public void OnPlayerNameChanged(string name)
	{
		PhotonNetwork.NickName = string.IsNullOrEmpty(name) ? "Player" + Random.Range(1000, 9999) : name;
	}

	public void OnClick_CreateRoom()
	{
		// CHẶN MULTIPLE CALLS
		if (isCreatingRoom || isProcessingRoomOperation)
		{
			Debug.LogWarning("❌ Already creating/joining room - ignoring call");
			if (statusText != null) statusText.text = "Đang xử lý...";
			return;
		}

		isCreatingRoom = true;
		isProcessingRoomOperation = true;

		Debug.Log("=== CREATE ROOM DEBUG START ===");
		Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
		Debug.Log($"IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
		Debug.Log($"InLobby: {PhotonNetwork.InLobby}");
		Debug.Log($"NetworkClientState: {PhotonNetwork.NetworkClientState}");
		Debug.Log($"Server: {PhotonNetwork.Server}");

		// Kiểm tra trạng thái
		if (!PhotonNetwork.IsConnectedAndReady)
		{
			Debug.LogError("Not connected and ready!");
			if (statusText != null) statusText.text = "Chưa kết nối đầy đủ!";
			return;
		}

		if (!PhotonNetwork.InLobby)
		{
			Debug.LogError("Not in lobby!");
			if (statusText != null) statusText.text = "Chưa vào lobby!";
			return;
		}

		// Kiểm tra input fields
		if (createRoomNameInput == null)
		{
			Debug.LogError("createRoomNameInput is null!");
			return;
		}

		string roomName = createRoomNameInput.text.Trim();
		Debug.Log($"Room name: '{roomName}'");

		if (string.IsNullOrEmpty(roomName))
		{
			Debug.LogWarning("Room name is empty!");
			if (statusText != null) statusText.text = "Tên phòng không được trống!";
			return;
		}

		// Kiểm tra max players
		byte maxPlayers = 2;
		if (createMaxPlayersInput != null && !string.IsNullOrEmpty(createMaxPlayersInput.text))
		{
			if (byte.TryParse(createMaxPlayersInput.text, out byte v))
			{
				maxPlayers = v;
				Debug.Log($"Max players: {maxPlayers}");
			}
			else
			{
				Debug.LogWarning($"Invalid max players: {createMaxPlayersInput.text}");
			}
		}

		// Tạo room options
		RoomOptions options = new RoomOptions
		{
			MaxPlayers = maxPlayers,
			IsVisible = true,
			IsOpen = true
		};

		// Custom properties
		Hashtable custom = new Hashtable();
		bool isPrivate = createPrivateToggle != null && createPrivateToggle.isOn;
		custom["isPrivate"] = isPrivate;
		Debug.Log($"Is private: {isPrivate}");

		if (isPrivate && createPasswordInput != null && !string.IsNullOrEmpty(createPasswordInput.text))
		{
			custom["pwd"] = createPasswordInput.text;
			Debug.Log("Password set for private room");
		}

		options.CustomRoomProperties = custom;
		options.CustomRoomPropertiesForLobby = new string[] { "isPrivate" };

		// Thử tạo phòng
		Debug.Log($"Attempting to create room: {roomName}");
		try
		{
			bool result = PhotonNetwork.CreateRoom(roomName, options);
			Debug.Log($"CreateRoom result: {result}");

			if (result)
			{
				if (statusText != null) statusText.text = "Đang tạo phòng...";
				if (createRoomPanel != null) createRoomPanel.SetActive(false);
			}
			else
			{
				Debug.LogError("CreateRoom returned false!");
				if (statusText != null) statusText.text = "Không thể tạo phòng!";
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError($"Exception when creating room: {e.Message}");
			if (statusText != null) statusText.text = "Lỗi tạo phòng: " + e.Message;
		}

		Debug.Log("=== CREATE ROOM DEBUG END ===");
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		if (statusText != null) statusText.text = "Tạo phòng thất bại: " + message;
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		if (statusText != null) statusText.text = "Vào phòng thất bại: " + message;
	}

	public void RequestJoinRoom(RoomInfo info)
	{
		bool isPrivate = info.CustomProperties != null &&
						info.CustomProperties.ContainsKey("isPrivate") &&
						(bool)info.CustomProperties["isPrivate"];

		if (isPrivate)
		{
			// Hiện modal nhập mật khẩu
			joinPasswordModal.SetActive(true);
			joinPasswordRoomNameText.text = info.Name;
			joinPasswordInput.text = "";
			lastAttemptJoinRoomName = info.Name;
		}
		else
		{
			// Phòng public - vào luôn
			lastAttemptJoinRoomName = info.Name;
			lastAttemptJoinPassword = "";
			PhotonNetwork.JoinRoom(info.Name);
		}
	}

	public void OnConfirmJoinWithPassword()
	{
		string roomName = lastAttemptJoinRoomName;
		string pwd = joinPasswordInput.text;
		lastAttemptJoinPassword = pwd;
		joinPasswordModal.SetActive(false);

		if (!string.IsNullOrEmpty(roomName))
		{
			PhotonNetwork.JoinRoom(roomName);
		}
	}

	// ================= UI MANAGEMENT =================

	void ClearRoomListUI()
	{
		foreach (Transform t in roomListContent)
		{
			Destroy(t.gameObject);
		}
		if (noRoomsText != null) noRoomsText.gameObject.SetActive(true);
	}

	void RefreshRoomListUI()
	{
		ClearRoomListUI();

		if (cachedRoomList.Count == 0)
		{
			if (noRoomsText != null) noRoomsText.gameObject.SetActive(true);
			return;
		}

		if (noRoomsText != null) noRoomsText.gameObject.SetActive(false);

		foreach (var kv in cachedRoomList)
		{
			RoomInfo info = kv.Value;
			GameObject g = Instantiate(roomItemPrefab, roomListContent);
			RoomItem item = g.GetComponent<RoomItem>();

			if (item != null)
			{
				bool isPrivate = info.CustomProperties != null &&
							   info.CustomProperties.ContainsKey("isPrivate") &&
							   (bool)info.CustomProperties["isPrivate"];

				item.Setup(info.Name, info.PlayerCount, info.MaxPlayers, isPrivate, this, info);
			}
		}
	}

	void UpdatePlayerListInRoomPanel()
	{
		if (roomPanelManager == null) return;

		var players = PhotonNetwork.PlayerList;
		List<string> names = new List<string>();
		foreach (var p in players)
			names.Add(p.NickName);

		roomPanelManager.UpdatePlayerList(names, PhotonNetwork.IsMasterClient);
	}

	public void OnMasterStartGame()
	{
		PhotonNetwork.LoadLevel("Gameplay"); // Tên scene gameplay của bạn
	}
}