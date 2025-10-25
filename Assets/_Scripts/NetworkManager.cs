using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
	public Button createRoomButtonInLobby;

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

		SetCreateRoomButtonState(false);

		PhotonNetwork.AutomaticallySyncScene = true;

		// Thiết lập UI listeners
		SetupUIListeners();

		// Kết nối Photon
		if (statusText != null) statusText.text = "Đang kết nối Photon...";
		PhotonNetwork.ConnectUsingSettings();

		// Load tên đã lưu
		LoadPlayerName();

		// Thiết lập layout cho room list
		SetupRoomListLayout();
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
		// 🔥 CHECK CONNECTION TRƯỚC KHI HIỆN PANEL
		if (!PhotonNetwork.InLobby)
		{
			Debug.LogWarning("❌ Not in lobby - cannot show create room panel!");
			if (statusText != null) statusText.text = "Chưa kết nối vào lobby!";
			return;
		}

		// Gọi từ button "Create Room" trong lobby
		createRoomPanel.SetActive(true);

		// Reset form
		if (createRoomNameInput != null) createRoomNameInput.text = "";
		if (createPrivateToggle != null) createPrivateToggle.isOn = false;
		if (createPasswordInput != null) createPasswordInput.text = "";
		if (passwordContainer != null) passwordContainer.SetActive(false);
	}

	// ================= PHOTON CALLBACKS =================

	public override void OnConnectedToMaster()
	{
		if (statusText != null) statusText.text = "Đã kết nối. Đang vào Lobby...";

		SetCreateRoomButtonState(false);

		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby()
	{
		if (statusText != null) statusText.text = "Đã vào Lobby";

		SetCreateRoomButtonState(true);
		ClearRoomListUI();
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if (statusText != null) statusText.text = "Mất kết nối: " + cause.ToString();
		SetCreateRoomButtonState(false);
	}

	public override void OnLeftLobby()
	{
		Debug.Log("🚪 Left lobby");

		// 🔥 DISABLE CREATE ROOM BUTTON KHI RỜI LOBBY
		SetCreateRoomButtonState(false);

		if (statusText != null) statusText.text = "Đã rời Lobby";
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
		Debug.Log($"🎉 Joined room: {PhotonNetwork.CurrentRoom.Name}");
		Debug.Log($"   Is Master Client: {PhotonNetwork.IsMasterClient}");
		Debug.Log($"   Was creating room: {isCreatingRoom}");

		// Lưu tên người chơi
		PlayerPrefs.SetString("playerName", PhotonNetwork.NickName);

		Room current = PhotonNetwork.CurrentRoom;

		// 🔥 FIX: CHỈ KIỂM TRA PASSWORD KHI KHÔNG PHẢI LÀ NGƯỜI TẠO PHÒNG
		bool isRoomCreator = isCreatingRoom; // Nếu đang tạo phòng thì là creator
		bool isPrivateRoom = current.CustomProperties.ContainsKey("pwd");

		Debug.Log($"🔐 Password check: IsPrivate={isPrivateRoom}, IsCreator={isRoomCreator}");

		if (isPrivateRoom && !isRoomCreator)
		{
			Debug.Log("🔍 Checking password for joining player...");

			// CHỈ KIỂM TRA PASSWORD KHI JOIN PHÒNG (KHÔNG PHẢI TẠO PHÒNG)
			if (!string.IsNullOrEmpty(lastAttemptJoinRoomName) && lastAttemptJoinRoomName == current.Name)
			{
				string expectedPassword = current.CustomProperties["pwd"] as string;
				string providedPassword = lastAttemptJoinPassword;

				Debug.Log($"   Expected: '{expectedPassword}', Provided: '{providedPassword}'");

				if (expectedPassword != providedPassword)
				{
					Debug.LogWarning("❌ Wrong password provided!");
					StartCoroutine(ShowWrongPasswordAndLeave());
					return;
				}
				else
				{
					Debug.Log("✅ Password correct!");
				}
			}
			else
			{
				Debug.LogWarning("❌ No password provided for private room!");
				StartCoroutine(ShowWrongPasswordAndLeave());
				return;
			}
		}
		else if (isPrivateRoom && isRoomCreator)
		{
			Debug.Log("✅ Room creator - skipping password check");
		}
		else
		{
			Debug.Log("✅ Public room or no password required");
		}

		// Reset room creation flag
		isCreatingRoom = false;

		// Hiển thị room panel
		lobbyPanel.SetActive(false);
		roomPanel.SetActive(true);

		if (roomPanelManager != null)
		{
			roomPanelManager.SetupRoom(current.Name, current.MaxPlayers);
		}

		UpdatePlayerListInRoomPanel();

		Debug.Log("🎉 Successfully joined room and setup complete!");
	}

	System.Collections.IEnumerator ShowWrongPasswordAndLeave()
	{
		Debug.LogWarning("🚫 Wrong password - showing error and leaving...");

		if (statusText != null) statusText.text = "Sai mật khẩu!";

		yield return new WaitForSeconds(2f);

		Debug.Log("🚪 Leaving room due to wrong password");
		PhotonNetwork.LeaveRoom();

		// Clear attempt data
		lastAttemptJoinRoomName = "";
		lastAttemptJoinPassword = "";

		Debug.Log("🔄 Password attempt data cleared");
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
		if (!PhotonNetwork.InLobby)
		{
			Debug.LogWarning("❌ Not in lobby - cannot create room!");
			if (statusText != null) statusText.text = "Chưa kết nối vào lobby!";
			return;
		}
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

		isCreatingRoom = true;
		isProcessingRoomOperation = true;

		Debug.Log("🔥 Setting isCreatingRoom = true");

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

				lastAttemptJoinRoomName = "";
				lastAttemptJoinPassword = "";

				Debug.Log("✅ Room creation initiated - flags cleared");
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
		Debug.Log($"🔥 RefreshRoomListUI called - Cached rooms: {cachedRoomList.Count}");

		ClearRoomListUI();

		if (cachedRoomList.Count == 0)
		{
			if (noRoomsText != null) noRoomsText.gameObject.SetActive(true);
			return;
		}

		if (noRoomsText != null) noRoomsText.gameObject.SetActive(false);

		// Sort rooms by creation time (if available) or by name
		var sortedRooms = cachedRoomList.Values.OrderBy(room => room.Name).ToList();

		foreach (RoomInfo info in sortedRooms)
		{
			Debug.Log($"🏗️ Creating room item for: {info.Name}");

			GameObject g = Instantiate(roomItemPrefab, roomListContent);
			g.SetActive(true);

			// 🔥 PHÒNG MỚI Ở DƯỚI - SetAsLastSibling
			g.transform.SetAsLastSibling();

			RoomItem item = g.GetComponent<RoomItem>();
			if (item != null)
			{
				bool isPrivate = info.CustomProperties != null &&
							   info.CustomProperties.ContainsKey("isPrivate") &&
							   (bool)info.CustomProperties["isPrivate"];

				item.Setup(info.Name, info.PlayerCount, info.MaxPlayers, isPrivate, this, info);
			}
		}

		// Force layout rebuild after all items created
		StartCoroutine(RefreshLayoutNextFrame());
	}

	System.Collections.IEnumerator RefreshLayoutNextFrame()
	{
		yield return null; // Wait 1 frame

		if (roomListContent != null)
		{
			RectTransform rectTransform = roomListContent as RectTransform;
			if (rectTransform != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
				Debug.Log("✅ Layout refreshed - room items should be properly positioned");
			}
			else
			{
				Debug.LogWarning("roomListContent is not a RectTransform!");
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

	void SetupRoomListLayout()
	{
		if (roomListContent == null) return;

		// Đảm bảo có Vertical Layout Group
		VerticalLayoutGroup layoutGroup = roomListContent.GetComponent<VerticalLayoutGroup>();
		if (layoutGroup == null)
		{
			layoutGroup = roomListContent.gameObject.AddComponent<VerticalLayoutGroup>();
			Debug.Log("✅ Added VerticalLayoutGroup to RoomListContent");
		}

		// Configure layout settings
		layoutGroup.childAlignment = TextAnchor.UpperCenter;     // Align to top-center
		layoutGroup.childControlWidth = true;                    // Control child width
		layoutGroup.childControlHeight = false;                  // Don't control height
		layoutGroup.childForceExpandWidth = true;               // Force expand width
		layoutGroup.childForceExpandHeight = false;             // Don't force expand height
		layoutGroup.spacing = 10f;                              // 10px spacing between items

		// Set padding
		layoutGroup.padding = new RectOffset(10, 10, 10, 10);   // 10px padding all sides

		// Đảm bảo có Content Size Fitter
		ContentSizeFitter sizeFitter = roomListContent.GetComponent<ContentSizeFitter>();
		if (sizeFitter == null)
		{
			sizeFitter = roomListContent.gameObject.AddComponent<ContentSizeFitter>();
			Debug.Log("✅ Added ContentSizeFitter to RoomListContent");
		}

		// Configure size fitter
		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Don't fit horizontal
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;    // Fit to content height

		Debug.Log("🎯 RoomListContent layout configured");
	}

	void SetCreateRoomButtonState(bool enabled)
	{
		if (createRoomButtonInLobby != null)
		{
			createRoomButtonInLobby.interactable = enabled;

			// 🔥 THAY ĐỔI VISUAL APPEARANCE
			Image buttonImage = createRoomButtonInLobby.GetComponent<Image>();
			TMP_Text buttonText = createRoomButtonInLobby.GetComponentInChildren<TMP_Text>();

			if (enabled)
			{
				// Enabled state: normal colors
				if (buttonImage != null)
				{
					buttonImage.color = new Color(1f, 1f, 1f, 1f); // White/normal
				}
				if (buttonText != null)
				{
					buttonText.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark text
					buttonText.text = "Create Room";
				}

				Debug.Log("✅ Create Room button ENABLED");
			}
			else
			{
				// Disabled state: grayed out
				if (buttonImage != null)
				{
					buttonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.7f); // Gray + transparent
				}
				if (buttonText != null)
				{
					buttonText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Light gray text
					buttonText.text = "Connecting...";
				}

				Debug.Log("❌ Create Room button DISABLED");
			}
		}
	}

}