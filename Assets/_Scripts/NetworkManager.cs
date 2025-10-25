using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
	public GameObject createRoomPanel;
	public TMP_InputField createRoomNameInput;
	public Toggle createPrivateToggle;
	public GameObject passwordContainer;
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

	// Internal state management
	private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
	private string lastAttemptJoinRoomName = "";
	private string lastAttemptJoinPassword = "";

	private bool isCreatingRoom = false;
	private bool isProcessingRoomOperation = false;

	// Status management
	private Coroutine statusMessageCoroutine;
	private enum UIStatus
	{
		Connecting,
		InLobby,
		CreatingRoom,
		JoiningRoom,
		InRoom,
		Error,
		Cancelled
	}
	private UIStatus currentUIStatus = UIStatus.Connecting;

	void Start()
	{
		// Ensure singleton
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

		// Initialize UI state
		SetCreateRoomButtonState(false);
		UpdateStatusText(UIStatus.Connecting);

		PhotonNetwork.AutomaticallySyncScene = true;

		// Setup UI listeners
		SetupUIListeners();

		// Connect to Photon
		PhotonNetwork.ConnectUsingSettings();

		// Load saved player name
		LoadPlayerName();

		// Setup room list layout
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
			cancelCreateButton.onClick.AddListener(OnClickCancelCreateRoom);
		}

		// Password modal listeners
		if (confirmPasswordButton != null)
		{
			confirmPasswordButton.onClick.AddListener(OnConfirmJoinWithPassword);
		}

		if (cancelPasswordButton != null)
		{
			cancelPasswordButton.onClick.AddListener(() => {
				joinPasswordModal.SetActive(false);
			});
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

	// ================= STATUS MANAGEMENT =================

	void UpdateStatusText(UIStatus newStatus, string customMessage = "")
	{
		currentUIStatus = newStatus;

		if (statusText == null) return;

		// Stop any existing status message coroutine
		if (statusMessageCoroutine != null)
		{
			StopCoroutine(statusMessageCoroutine);
			statusMessageCoroutine = null;
		}

		switch (newStatus)
		{
			case UIStatus.Connecting:
				statusText.text = "Đang kết nối Photon...";
				break;
			case UIStatus.InLobby:
				statusText.text = "Đã vào Lobby";
				break;
			case UIStatus.CreatingRoom:
				statusText.text = "Đang tạo phòng...";
				break;
			case UIStatus.JoiningRoom:
				statusText.text = "Đang vào phòng...";
				break;
			case UIStatus.InRoom:
				statusText.text = "Đã vào phòng thành công!";
				break;
			case UIStatus.Cancelled:
				statusText.text = "Đã hủy tạo phòng";
				statusMessageCoroutine = StartCoroutine(ResetStatusAfterDelay(2f, UIStatus.InLobby));
				break;
			case UIStatus.Error:
				statusText.text = customMessage;
				statusMessageCoroutine = StartCoroutine(ResetStatusAfterDelay(3f, UIStatus.InLobby));
				break;
		}

		Debug.Log($"📱 Status updated: {newStatus} - '{statusText.text}'");
	}

	IEnumerator ResetStatusAfterDelay(float delay, UIStatus resetToStatus)
	{
		yield return new WaitForSeconds(delay);

		// Only reset if still in the temporary status and in appropriate network state
		if ((currentUIStatus == UIStatus.Cancelled || currentUIStatus == UIStatus.Error) && PhotonNetwork.InLobby)
		{
			UpdateStatusText(resetToStatus);
		}
	}

	// ================= FLAG MANAGEMENT =================

	void ResetRoomOperationFlags(bool silent = false)
	{
		bool wasProcessing = isCreatingRoom || isProcessingRoomOperation;

		isCreatingRoom = false;
		isProcessingRoomOperation = false;

		if (!silent && wasProcessing)
		{
			Debug.Log("🔄 Room operation flags reset");
		}
	}

	bool CanPerformRoomOperation()
	{
		if (!PhotonNetwork.InLobby)
		{
			Debug.LogWarning("❌ Not in lobby - cannot perform room operation");
			UpdateStatusText(UIStatus.Error, "Chưa kết nối vào lobby!");
			return false;
		}

		if (isCreatingRoom || isProcessingRoomOperation)
		{
			Debug.LogWarning("❌ Already processing room operation - ignoring");
			UpdateStatusText(UIStatus.Error, "Đang xử lý...");
			return false;
		}

		return true;
	}

	// ================= UI EVENT HANDLERS =================

	public void OnPrivateToggleChanged(bool isPrivate)
	{
		if (passwordContainer != null)
		{
			passwordContainer.SetActive(isPrivate);
		}
	}

	public void ShowCreateRoomPanel()
	{
		Debug.Log("🎯 ShowCreateRoomPanel called");

		if (!CanPerformRoomOperation())
		{
			return;
		}

		// Open panel and reset form
		createRoomPanel.SetActive(true);

		// Reset form fields
		if (createRoomNameInput != null) createRoomNameInput.text = "";
		if (createPrivateToggle != null) createPrivateToggle.isOn = false;
		if (createPasswordInput != null) createPasswordInput.text = "";
		if (passwordContainer != null) passwordContainer.SetActive(false);

		Debug.Log("✅ Create room panel opened and form reset");
	}

	public void OnClickCancelCreateRoom()
	{
		Debug.Log("🚫 Cancel Create Room clicked");

		// Reset flags
		ResetRoomOperationFlags();

		// Close create room panel
		if (createRoomPanel != null)
		{
			createRoomPanel.SetActive(false);
		}

		// Update status with timed message
		UpdateStatusText(UIStatus.Cancelled);

		Debug.Log("✅ Create room operation cancelled");
	}

	// ================= PHOTON CALLBACKS =================

	public override void OnConnectedToMaster()
	{
		Debug.Log("🔗 Connected to Master Server");
		UpdateStatusText(UIStatus.Connecting, "Đã kết nối. Đang vào Lobby...");
		SetCreateRoomButtonState(false);
		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby()
	{
		Debug.Log("🏠 Joined Lobby");
		UpdateStatusText(UIStatus.InLobby);
		SetCreateRoomButtonState(true);
		ClearRoomListUI();
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		Debug.LogWarning($"🔌 Disconnected: {cause}");
		UpdateStatusText(UIStatus.Error, $"Mất kết nối: {cause}");
		SetCreateRoomButtonState(false);
		ResetRoomOperationFlags();
	}

	public override void OnLeftLobby()
	{
		Debug.Log("🚪 Left lobby");
		UpdateStatusText(UIStatus.Error, "Đã rời Lobby");
		SetCreateRoomButtonState(false);
		ResetRoomOperationFlags();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		// Update cached room list
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

		// Save player name
		PlayerPrefs.SetString("playerName", PhotonNetwork.NickName);

		Room current = PhotonNetwork.CurrentRoom;

		// Check password only for joining players (not room creators)
		bool isRoomCreator = isCreatingRoom;
		bool isPrivateRoom = current.CustomProperties.ContainsKey("pwd");

		Debug.Log($"🔐 Password check: IsPrivate={isPrivateRoom}, IsCreator={isRoomCreator}");

		if (isPrivateRoom && !isRoomCreator)
		{
			Debug.Log("🔍 Checking password for joining player...");

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

		// Update UI status
		UpdateStatusText(UIStatus.InRoom);

		// Reset room operation flags
		ResetRoomOperationFlags();

		// Show room panel
		lobbyPanel.SetActive(false);
		roomPanel.SetActive(true);

		if (roomPanelManager != null)
		{
			roomPanelManager.SetupRoom(current.Name, current.MaxPlayers);
		}

		UpdatePlayerListInRoomPanel();

		Debug.Log("🎉 Successfully joined room and setup complete!");
	}

	IEnumerator ShowWrongPasswordAndLeave()
	{
		Debug.LogWarning("🚫 Wrong password - showing error and leaving...");
		UpdateStatusText(UIStatus.Error, "Sai mật khẩu!");

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

		if (this == null) return;

		ResetRoomOperationFlags();

		// Update UI
		if (roomPanel != null)
			try { roomPanel.SetActive(false); } catch { }

		if (lobbyPanel != null)
			try { lobbyPanel.SetActive(true); } catch { }

		UpdateStatusText(UIStatus.InLobby, "Đã rời phòng");

		// Re-enable create room button if needed
		if (createRoomButton != null)
		{
			createRoomButton.interactable = true;
		}
	}

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		Debug.Log($"👋 Player entered room: {newPlayer.NickName}");
		UpdatePlayerListInRoomPanel();
	}

	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		Debug.Log($"👋 Player left room: {otherPlayer.NickName}");
		UpdatePlayerListInRoomPanel();
	}

	// ================= ROOM OPERATIONS =================

	public void OnPlayerNameChanged(string name)
	{
		PhotonNetwork.NickName = string.IsNullOrEmpty(name) ? "Player" + Random.Range(1000, 9999) : name;
		Debug.Log($"🏷️ Player name changed to: {PhotonNetwork.NickName}");
	}

	public void OnClick_CreateRoom()
	{
		Debug.Log("🎯 Create Room button clicked");

		// Check if we can perform room operation
		if (!CanPerformRoomOperation())
		{
			return;
		}

		// Validate inputs before setting flags
		if (!ValidateCreateRoomInputs())
		{
			return;
		}

		// Set operation flags
		isCreatingRoom = true;
		isProcessingRoomOperation = true;
		UpdateStatusText(UIStatus.CreatingRoom);

		Debug.Log("🔥 Room creation flags set");

		// Get room configuration
		string roomName = createRoomNameInput.text.Trim();
		byte maxPlayers = GetMaxPlayersFromInput();
		bool isPrivate = createPrivateToggle != null && createPrivateToggle.isOn;
		string password = isPrivate && createPasswordInput != null ? createPasswordInput.text.Trim() : "";

		// Create room options
		RoomOptions options = CreateRoomOptions(maxPlayers, isPrivate, password);

		// Debug room creation
		Debug.Log($"Attempting to create room: '{roomName}' (Max: {maxPlayers}, Private: {isPrivate})");

		try
		{
			bool result = PhotonNetwork.CreateRoom(roomName, options);
			Debug.Log($"PhotonNetwork.CreateRoom result: {result}");

			if (result)
			{
				// Success - close panel and clear join attempt data
				if (createRoomPanel != null) createRoomPanel.SetActive(false);
				lastAttemptJoinRoomName = "";
				lastAttemptJoinPassword = "";

				Debug.Log("✅ Room creation initiated successfully");
			}
			else
			{
				Debug.LogError("PhotonNetwork.CreateRoom returned false!");
				UpdateStatusText(UIStatus.Error, "Không thể tạo phòng!");
				ResetRoomOperationFlags();
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError($"Exception when creating room: {e.Message}");
			UpdateStatusText(UIStatus.Error, $"Lỗi tạo phòng: {e.Message}");
			ResetRoomOperationFlags();
		}
	}

	bool ValidateCreateRoomInputs()
	{
		// Check room name input
		if (createRoomNameInput == null)
		{
			Debug.LogError("createRoomNameInput is null!");
			UpdateStatusText(UIStatus.Error, "Lỗi: Không tìm thấy ô nhập tên phòng!");
			return false;
		}

		string roomName = createRoomNameInput.text.Trim();
		if (string.IsNullOrEmpty(roomName))
		{
			Debug.LogWarning("Room name is empty!");
			UpdateStatusText(UIStatus.Error, "Tên phòng không được trống!");
			return false;
		}

		// Check private room password
		bool isPrivate = createPrivateToggle != null && createPrivateToggle.isOn;
		if (isPrivate)
		{
			if (createPasswordInput == null || string.IsNullOrEmpty(createPasswordInput.text.Trim()))
			{
				Debug.LogWarning("Private room selected but no password provided!");
				UpdateStatusText(UIStatus.Error, "Phòng riêng tư cần có mật khẩu!");
				return false;
			}
		}

		return true;
	}

	byte GetMaxPlayersFromInput()
	{
		byte defaultMaxPlayers = 2;

		if (createMaxPlayersInput != null && !string.IsNullOrEmpty(createMaxPlayersInput.text))
		{
			if (byte.TryParse(createMaxPlayersInput.text, out byte v) && v > 0 && v <= 20)
			{
				return v;
			}
			else
			{
				Debug.LogWarning($"Invalid max players: '{createMaxPlayersInput.text}' - using default: {defaultMaxPlayers}");
			}
		}

		return defaultMaxPlayers;
	}

	RoomOptions CreateRoomOptions(byte maxPlayers, bool isPrivate, string password)
	{
		RoomOptions options = new RoomOptions
		{
			MaxPlayers = maxPlayers,
			IsVisible = true,
			IsOpen = true
		};

		// Set custom properties
		Hashtable customProperties = new Hashtable();
		customProperties["isPrivate"] = isPrivate;

		if (isPrivate && !string.IsNullOrEmpty(password))
		{
			customProperties["pwd"] = password;
			Debug.Log($"Password set for private room");
		}

		options.CustomRoomProperties = customProperties;
		options.CustomRoomPropertiesForLobby = new string[] { "isPrivate" };

		return options;
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		Debug.LogError($"❌ Create room failed: {message} (Code: {returnCode})");
		UpdateStatusText(UIStatus.Error, $"Tạo phòng thất bại: {message}");
		ResetRoomOperationFlags();
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.LogError($"❌ Join room failed: {message} (Code: {returnCode})");
		UpdateStatusText(UIStatus.Error, $"Vào phòng thất bại: {message}");
		ResetRoomOperationFlags();
	}

	public void RequestJoinRoom(RoomInfo info)
	{
		Debug.Log($"🚪 Requesting to join room: {info.Name}");

		bool isPrivate = info.CustomProperties != null &&
						info.CustomProperties.ContainsKey("isPrivate") &&
						(bool)info.CustomProperties["isPrivate"];

		if (isPrivate)
		{
			// Show password modal for private rooms
			joinPasswordModal.SetActive(true);
			joinPasswordRoomNameText.text = info.Name;
			joinPasswordInput.text = "";
			lastAttemptJoinRoomName = info.Name;
		}
		else
		{
			// Join public room directly
			lastAttemptJoinRoomName = info.Name;
			lastAttemptJoinPassword = "";
			UpdateStatusText(UIStatus.JoiningRoom);
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
			UpdateStatusText(UIStatus.JoiningRoom);
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
		Debug.Log($"🔄 RefreshRoomListUI - Cached rooms: {cachedRoomList.Count}");

		ClearRoomListUI();

		if (cachedRoomList.Count == 0)
		{
			if (noRoomsText != null) noRoomsText.gameObject.SetActive(true);
			return;
		}

		if (noRoomsText != null) noRoomsText.gameObject.SetActive(false);

		// Sort rooms by name
		var sortedRooms = cachedRoomList.Values.OrderBy(room => room.Name).ToList();

		foreach (RoomInfo info in sortedRooms)
		{
			Debug.Log($"🏗️ Creating room item for: {info.Name}");

			GameObject roomItemObject = Instantiate(roomItemPrefab, roomListContent);
			roomItemObject.SetActive(true);
			roomItemObject.transform.SetAsLastSibling();

			RoomItem roomItem = roomItemObject.GetComponent<RoomItem>();
			if (roomItem != null)
			{
				bool isPrivate = info.CustomProperties != null &&
							   info.CustomProperties.ContainsKey("isPrivate") &&
							   (bool)info.CustomProperties["isPrivate"];

				roomItem.Setup(info.Name, info.PlayerCount, info.MaxPlayers, isPrivate, this, info);
			}
		}

		// Force layout rebuild
		StartCoroutine(RefreshLayoutNextFrame());
	}

	IEnumerator RefreshLayoutNextFrame()
	{
		yield return null;

		if (roomListContent != null)
		{
			RectTransform rectTransform = roomListContent as RectTransform;
			if (rectTransform != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
				Debug.Log("✅ Room list layout refreshed");
			}
		}
	}

	void UpdatePlayerListInRoomPanel()
	{
		if (roomPanelManager == null) return;

		var players = PhotonNetwork.PlayerList;
		List<string> names = new List<string>();
		foreach (var player in players)
		{
			names.Add(player.NickName);
		}

		roomPanelManager.UpdatePlayerList(names, PhotonNetwork.IsMasterClient);
	}

	public void OnMasterStartGame()
	{
		Debug.Log("🎮 Master starting game");
		PhotonNetwork.LoadLevel("Gameplay");
	}

	void SetupRoomListLayout()
	{
		if (roomListContent == null) return;

		// Ensure Vertical Layout Group
		VerticalLayoutGroup layoutGroup = roomListContent.GetComponent<VerticalLayoutGroup>();
		if (layoutGroup == null)
		{
			layoutGroup = roomListContent.gameObject.AddComponent<VerticalLayoutGroup>();
			Debug.Log("✅ Added VerticalLayoutGroup to RoomListContent");
		}

		// Configure layout settings
		layoutGroup.childAlignment = TextAnchor.UpperCenter;
		layoutGroup.childControlWidth = true;
		layoutGroup.childControlHeight = false;
		layoutGroup.childForceExpandWidth = true;
		layoutGroup.childForceExpandHeight = false;
		layoutGroup.spacing = 10f;
		layoutGroup.padding = new RectOffset(10, 10, 10, 10);

		// Ensure Content Size Fitter
		ContentSizeFitter sizeFitter = roomListContent.GetComponent<ContentSizeFitter>();
		if (sizeFitter == null)
		{
			sizeFitter = roomListContent.gameObject.AddComponent<ContentSizeFitter>();
			Debug.Log("✅ Added ContentSizeFitter to RoomListContent");
		}

		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Debug.Log("🎯 RoomListContent layout configured");
	}

	void SetCreateRoomButtonState(bool enabled)
	{
		if (createRoomButtonInLobby != null)
		{
			createRoomButtonInLobby.interactable = enabled;

			// Update visual appearance
			Image buttonImage = createRoomButtonInLobby.GetComponent<Image>();
			TMP_Text buttonText = createRoomButtonInLobby.GetComponentInChildren<TMP_Text>();

			if (enabled)
			{
				// Enabled state
				if (buttonImage != null)
				{
					buttonImage.color = new Color(1f, 1f, 1f, 1f);
				}
				if (buttonText != null)
				{
					buttonText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
					buttonText.text = "Create Room";
				}

				Debug.Log("✅ Create Room button ENABLED");
			}
			else
			{
				// Disabled state
				if (buttonImage != null)
				{
					buttonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
				}
				if (buttonText != null)
				{
					buttonText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
					buttonText.text = "Connecting...";
				}

				Debug.Log("❌ Create Room button DISABLED");
			}
		}
	}
}