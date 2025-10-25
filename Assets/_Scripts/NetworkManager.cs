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
	private string currentJoinAttemptRoomName = "";
	private string currentJoinAttemptPassword = "";

	// Operation flags
	private bool isCreatingRoom = false;
	private bool isProcessingRoomOperation = false;

	// Status management
	private enum UIStatus
	{
		Connecting,
		ConnectedToMaster,
		JoiningLobby,
		InLobby,
		CreatingRoom,
		JoiningRoom,
		InRoom,
		LeavingRoom,
		Disconnected,
		Error,
		OperationCancelled
	}

	private UIStatus currentStatus = UIStatus.Connecting;
	private Coroutine statusResetCoroutine;

	void Start()
	{
		// Ensure singleton pattern
		if (!EnsureSingleton()) return;

		// Initialize systems
		InitializeNetworkManager();
	}

	bool EnsureSingleton()
	{
		NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
		if (managers.Length > 1)
		{
			Debug.LogError($"⚠️ FOUND {managers.Length} NetworkManager instances! Destroying duplicates.");
			for (int i = 1; i < managers.Length; i++)
			{
				Destroy(managers[i].gameObject);
			}
			return false;
		}

		Debug.Log("✅ NetworkManager singleton confirmed");
		return true;
	}

	void InitializeNetworkManager()
	{
		Debug.Log("🚀 Initializing NetworkManager...");

		// Reset all states
		ResetAllStates();

		// Initialize UI
		InitializeUI();

		// Setup Photon
		PhotonNetwork.AutomaticallySyncScene = true;

		// Setup event listeners
		SetupUIListeners();

		// Load player settings
		LoadPlayerName();

		// Setup room list layout
		SetupRoomListLayout();

		// Start connection
		StartPhotonConnection();

		Debug.Log("✅ NetworkManager initialization complete");
	}

	void ResetAllStates()
	{
		// Reset operation flags
		isCreatingRoom = false;
		isProcessingRoomOperation = false;

		// Clear join attempt data
		ClearJoinAttemptData();

		// Reset status
		currentStatus = UIStatus.Connecting;

		// Stop any running coroutines
		if (statusResetCoroutine != null)
		{
			StopCoroutine(statusResetCoroutine);
			statusResetCoroutine = null;
		}

		Debug.Log("🔄 All states reset");
	}

	void InitializeUI()
	{
		// Set initial UI states
		SetCreateRoomButtonState(false);
		UpdateStatusDisplay(UIStatus.Connecting);

		// Hide panels that should start hidden
		if (createRoomPanel != null) createRoomPanel.SetActive(false);
		if (joinPasswordModal != null) joinPasswordModal.SetActive(false);
		if (roomPanel != null) roomPanel.SetActive(false);

		Debug.Log("🎨 UI initialized");
	}

	void SetupUIListeners()
	{
		// Create room UI
		if (createPrivateToggle != null)
		{
			createPrivateToggle.onValueChanged.RemoveAllListeners();
			createPrivateToggle.onValueChanged.AddListener(OnPrivateToggleChanged);
		}

		if (createRoomButton != null)
		{
			createRoomButton.onClick.RemoveAllListeners();
			createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
		}

		if (cancelCreateButton != null)
		{
			cancelCreateButton.onClick.RemoveAllListeners();
			cancelCreateButton.onClick.AddListener(OnCancelCreateRoomButtonClicked);
		}

		// Password modal UI
		if (confirmPasswordButton != null)
		{
			confirmPasswordButton.onClick.RemoveAllListeners();
			confirmPasswordButton.onClick.AddListener(OnConfirmPasswordButtonClicked);
		}

		if (cancelPasswordButton != null)
		{
			cancelPasswordButton.onClick.RemoveAllListeners();
			cancelPasswordButton.onClick.AddListener(OnCancelPasswordButtonClicked);
		}

		Debug.Log("🔗 UI listeners setup complete");
	}

	void LoadPlayerName()
	{
		if (playerNameInput != null)
		{
			string savedName = PlayerPrefs.GetString("playerName", "");
			playerNameInput.text = savedName;

			string finalName = string.IsNullOrEmpty(savedName) ? GenerateRandomPlayerName() : savedName;
			PhotonNetwork.NickName = finalName;

			Debug.Log($"👤 Player name loaded: {PhotonNetwork.NickName}");
		}
	}

	string GenerateRandomPlayerName()
	{
		return "Player" + Random.Range(1000, 9999);
	}

	void StartPhotonConnection()
	{
		Debug.Log("🌐 Starting Photon connection...");
		UpdateStatusDisplay(UIStatus.Connecting);
		PhotonNetwork.ConnectUsingSettings();
	}

	// ================= STATUS MANAGEMENT =================

	void UpdateStatusDisplay(UIStatus newStatus, string customMessage = "")
	{
		// Stop any existing reset coroutine
		if (statusResetCoroutine != null)
		{
			StopCoroutine(statusResetCoroutine);
			statusResetCoroutine = null;
		}

		currentStatus = newStatus;

		if (statusText == null) return;

		string statusMessage = GetStatusMessage(newStatus, customMessage);
		statusText.text = statusMessage;

		// Setup auto-reset for temporary statuses
		if (ShouldAutoResetStatus(newStatus))
		{
			statusResetCoroutine = StartCoroutine(ResetStatusAfterDelay(3f));
		}

		Debug.Log($"📱 Status updated: {newStatus} - '{statusMessage}'");
	}

	string GetStatusMessage(UIStatus status, string customMessage)
	{
		if (!string.IsNullOrEmpty(customMessage))
		{
			return customMessage;
		}

		switch (status)
		{
			case UIStatus.Connecting:
				return "Đang kết nối Photon...";
			case UIStatus.ConnectedToMaster:
				return "Đã kết nối. Đang vào Lobby...";
			case UIStatus.JoiningLobby:
				return "Đang vào Lobby...";
			case UIStatus.InLobby:
				return "Đã vào Lobby";
			case UIStatus.CreatingRoom:
				return "Đang tạo phòng...";
			case UIStatus.JoiningRoom:
				return "Đang vào phòng...";
			case UIStatus.InRoom:
				return "Đã vào phòng thành công!";
			case UIStatus.LeavingRoom:
				return "Đang rời phòng...";
			case UIStatus.Disconnected:
				return "Đã mất kết nối";
			case UIStatus.OperationCancelled:
				return "Đã hủy thao tác";
			case UIStatus.Error:
				return "Có lỗi xảy ra";
			default:
				return "Không xác định";
		}
	}

	bool ShouldAutoResetStatus(UIStatus status)
	{
		return status == UIStatus.OperationCancelled ||
			   status == UIStatus.Error ||
			   status == UIStatus.InRoom;
	}

	IEnumerator ResetStatusAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		// Only reset if we're still in a temporary status and in appropriate network state
		if (ShouldAutoResetStatus(currentStatus))
		{
			if (PhotonNetwork.InLobby)
			{
				UpdateStatusDisplay(UIStatus.InLobby);
			}
			else if (PhotonNetwork.IsConnected)
			{
				UpdateStatusDisplay(UIStatus.ConnectedToMaster);
			}
			else
			{
				UpdateStatusDisplay(UIStatus.Disconnected);
			}
		}
	}

	// ================= OPERATION STATE MANAGEMENT =================

	void ClearJoinAttemptData()
	{
		currentJoinAttemptRoomName = "";
		currentJoinAttemptPassword = "";
		Debug.Log("🔄 Join attempt data cleared");
	}

	bool CanPerformRoomOperation(string operationName)
	{
		// Check network state
		if (!PhotonNetwork.InLobby)
		{
			Debug.LogWarning($"❌ Cannot {operationName}: Not in lobby (State: {PhotonNetwork.NetworkClientState})");
			UpdateStatusDisplay(UIStatus.Error, "Chưa kết nối vào lobby!");
			return false;
		}

		// Check operation flags
		if (isProcessingRoomOperation)
		{
			Debug.LogWarning($"❌ Cannot {operationName}: Already processing room operation");
			UpdateStatusDisplay(UIStatus.Error, "Đang xử lý thao tác khác...");
			return false;
		}

		return true;
	}

	void SetOperationInProgress(bool inProgress, string operationType = "")
	{
		isProcessingRoomOperation = inProgress;

		if (inProgress)
		{
			Debug.Log($"🔄 Operation started: {operationType}");
		}
		else
		{
			Debug.Log($"✅ Operation completed/reset: {operationType}");
		}
	}

	void ResetOperationFlags()
	{
		bool wasInProgress = isCreatingRoom || isProcessingRoomOperation;

		isCreatingRoom = false;
		isProcessingRoomOperation = false;

		if (wasInProgress)
		{
			Debug.Log("🔄 Operation flags reset");
		}
	}

	// ================= UI EVENT HANDLERS =================

	public void OnPrivateToggleChanged(bool isPrivate)
	{
		if (passwordContainer != null)
		{
			passwordContainer.SetActive(isPrivate);
		}
		Debug.Log($"🔒 Private toggle changed: {isPrivate}");
	}

	public void ShowCreateRoomPanel()
	{
		Debug.Log("🎯 ShowCreateRoomPanel requested");

		if (!CanPerformRoomOperation("show create room panel"))
		{
			return;
		}

		// Show panel
		if (createRoomPanel != null)
		{
			createRoomPanel.SetActive(true);
		}

		// Reset form
		ResetCreateRoomForm();

		Debug.Log("✅ Create room panel shown");
	}

	void ResetCreateRoomForm()
	{
		if (createRoomNameInput != null) createRoomNameInput.text = "";
		if (createPrivateToggle != null) createPrivateToggle.isOn = false;
		if (createPasswordInput != null) createPasswordInput.text = "";
		if (passwordContainer != null) passwordContainer.SetActive(false);

		Debug.Log("📝 Create room form reset");
	}

	public void OnCreateRoomButtonClicked()
	{
		Debug.Log("🎯 Create Room button clicked");

		if (!CanPerformRoomOperation("create room"))
		{
			return;
		}

		// Validate inputs
		if (!ValidateCreateRoomInputs())
		{
			return;
		}

		// Clear any previous join attempt data
		ClearJoinAttemptData();

		// Set operation flags
		isCreatingRoom = true;
		SetOperationInProgress(true, "creating room");
		UpdateStatusDisplay(UIStatus.CreatingRoom);

		// Execute room creation
		ExecuteRoomCreation();
	}

	bool ValidateCreateRoomInputs()
	{
		// Validate room name
		if (createRoomNameInput == null)
		{
			Debug.LogError("❌ Room name input is null");
			UpdateStatusDisplay(UIStatus.Error, "Lỗi: Không tìm thấy ô nhập tên phòng");
			return false;
		}

		string roomName = createRoomNameInput.text.Trim();
		if (string.IsNullOrEmpty(roomName))
		{
			Debug.LogWarning("❌ Room name is empty");
			UpdateStatusDisplay(UIStatus.Error, "Tên phòng không được trống!");
			return false;
		}

		if (roomName.Length > 20)
		{
			Debug.LogWarning("❌ Room name too long");
			UpdateStatusDisplay(UIStatus.Error, "Tên phòng không được quá 20 ký tự!");
			return false;
		}

		// Validate private room settings
		bool isPrivate = createPrivateToggle != null && createPrivateToggle.isOn;
		if (isPrivate)
		{
			if (createPasswordInput == null || string.IsNullOrEmpty(createPasswordInput.text.Trim()))
			{
				Debug.LogWarning("❌ Private room requires password");
				UpdateStatusDisplay(UIStatus.Error, "Phòng riêng tư cần có mật khẩu!");
				return false;
			}

			if (createPasswordInput.text.Trim().Length < 3)
			{
				Debug.LogWarning("❌ Password too short");
				UpdateStatusDisplay(UIStatus.Error, "Mật khẩu phải có ít nhất 3 ký tự!");
				return false;
			}
		}

		return true;
	}

	void ExecuteRoomCreation()
	{
		string roomName = createRoomNameInput.text.Trim();
		byte maxPlayers = GetValidatedMaxPlayers();
		bool isPrivate = createPrivateToggle != null && createPrivateToggle.isOn;
		string password = isPrivate && createPasswordInput != null ? createPasswordInput.text.Trim() : "";

		Debug.Log($"🏗️ Creating room: '{roomName}' (Max: {maxPlayers}, Private: {isPrivate})");

		try
		{
			RoomOptions roomOptions = CreateRoomOptions(maxPlayers, isPrivate, password);
			bool result = PhotonNetwork.CreateRoom(roomName, roomOptions);

			if (result)
			{
				// Close create room panel
				if (createRoomPanel != null)
				{
					createRoomPanel.SetActive(false);
				}

				Debug.Log("✅ Room creation request sent successfully");
			}
			else
			{
				Debug.LogError("❌ PhotonNetwork.CreateRoom returned false");
				UpdateStatusDisplay(UIStatus.Error, "Không thể tạo phòng!");
				HandleRoomOperationFailure();
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Exception during room creation: {ex.Message}");
			UpdateStatusDisplay(UIStatus.Error, $"Lỗi tạo phòng: {ex.Message}");
			HandleRoomOperationFailure();
		}
	}

	byte GetValidatedMaxPlayers()
	{
		byte defaultMax = 2;

		if (createMaxPlayersInput != null && !string.IsNullOrEmpty(createMaxPlayersInput.text))
		{
			if (byte.TryParse(createMaxPlayersInput.text, out byte parsed))
			{
				if (parsed >= 2 && parsed <= 20)
				{
					return parsed;
				}
				else
				{
					Debug.LogWarning($"⚠️ Invalid max players: {parsed}, using default: {defaultMax}");
				}
			}
			else
			{
				Debug.LogWarning($"⚠️ Could not parse max players: '{createMaxPlayersInput.text}', using default: {defaultMax}");
			}
		}

		return defaultMax;
	}

	RoomOptions CreateRoomOptions(byte maxPlayers, bool isPrivate, string password)
	{
		RoomOptions options = new RoomOptions
		{
			MaxPlayers = maxPlayers,
			IsVisible = true,
			IsOpen = true
		};

		// Setup custom properties
		Hashtable customProperties = new Hashtable();
		customProperties["isPrivate"] = isPrivate;

		if (isPrivate && !string.IsNullOrEmpty(password))
		{
			customProperties["pwd"] = password;
			Debug.Log("🔒 Password set for private room");
		}

		options.CustomRoomProperties = customProperties;
		options.CustomRoomPropertiesForLobby = new string[] { "isPrivate" };

		return options;
	}

	public void OnCancelCreateRoomButtonClicked()
	{
		Debug.Log("🚫 Cancel Create Room button clicked");

		// Reset operation flags
		ResetOperationFlags();

		// Close panel
		if (createRoomPanel != null)
		{
			createRoomPanel.SetActive(false);
		}

		// Update status
		UpdateStatusDisplay(UIStatus.OperationCancelled);

		Debug.Log("✅ Create room operation cancelled");
	}

	void HandleRoomOperationFailure()
	{
		ResetOperationFlags();

		// Re-enable UI elements if needed
		if (createRoomButton != null)
		{
			createRoomButton.interactable = true;
		}
	}

	// ================= ROOM JOINING =================

	public void RequestJoinRoom(RoomInfo roomInfo)
	{
		if (roomInfo == null)
		{
			Debug.LogError("❌ RoomInfo is null");
			return;
		}

		Debug.Log($"🚪 Request to join room: '{roomInfo.Name}'");

		if (!CanPerformRoomOperation("join room"))
		{
			return;
		}

		// Clear previous join data first
		ClearJoinAttemptData();

		// Set new join attempt data
		currentJoinAttemptRoomName = roomInfo.Name;

		// Check if room is private
		bool isPrivate = IsRoomPrivate(roomInfo);

		if (isPrivate)
		{
			ShowPasswordModal(roomInfo.Name);
		}
		else
		{
			ExecuteRoomJoin(roomInfo.Name, "");
		}
	}

	bool IsRoomPrivate(RoomInfo roomInfo)
	{
		return roomInfo.CustomProperties != null &&
			   roomInfo.CustomProperties.ContainsKey("isPrivate") &&
			   (bool)roomInfo.CustomProperties["isPrivate"];
	}

	void ShowPasswordModal(string roomName)
	{
		Debug.Log($"🔒 Showing password modal for room: {roomName}");

		if (joinPasswordModal != null)
		{
			joinPasswordModal.SetActive(true);
		}

		if (joinPasswordRoomNameText != null)
		{
			joinPasswordRoomNameText.text = roomName;
		}

		if (joinPasswordInput != null)
		{
			joinPasswordInput.text = "";
		}
	}

	public void OnConfirmPasswordButtonClicked()
	{
		Debug.Log("🔓 Password confirmation clicked");

		string roomName = currentJoinAttemptRoomName;
		string password = joinPasswordInput != null ? joinPasswordInput.text : "";

		// Close password modal
		if (joinPasswordModal != null)
		{
			joinPasswordModal.SetActive(false);
		}

		if (string.IsNullOrEmpty(roomName))
		{
			Debug.LogError("❌ No room name for password confirmation");
			UpdateStatusDisplay(UIStatus.Error, "Lỗi: Không có tên phòng!");
			return;
		}

		ExecuteRoomJoin(roomName, password);
	}

	public void OnCancelPasswordButtonClicked()
	{
		Debug.Log("🚫 Password input cancelled");

		// Close password modal
		if (joinPasswordModal != null)
		{
			joinPasswordModal.SetActive(false);
		}

		// Clear join attempt data
		ClearJoinAttemptData();

		UpdateStatusDisplay(UIStatus.OperationCancelled);
	}

	void ExecuteRoomJoin(string roomName, string password)
	{
		Debug.Log($"🚪 Executing room join: '{roomName}'");

		// Set join attempt data
		currentJoinAttemptRoomName = roomName;
		currentJoinAttemptPassword = password;

		// Set operation flags
		SetOperationInProgress(true, "joining room");
		UpdateStatusDisplay(UIStatus.JoiningRoom);

		try
		{
			PhotonNetwork.JoinRoom(roomName);
			Debug.Log($"✅ Room join request sent for: {roomName}");
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Exception during room join: {ex.Message}");
			UpdateStatusDisplay(UIStatus.Error, $"Lỗi vào phòng: {ex.Message}");
			HandleRoomOperationFailure();
		}
	}

	// ================= PHOTON CALLBACKS =================

	public override void OnConnectedToMaster()
	{
		Debug.Log("🔗 Connected to Master Server");
		UpdateStatusDisplay(UIStatus.ConnectedToMaster);
		SetCreateRoomButtonState(false);
		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby()
	{
		Debug.Log("🏠 Joined Lobby successfully");
		UpdateStatusDisplay(UIStatus.InLobby);
		SetCreateRoomButtonState(true);
		ClearRoomListUI();
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		Debug.LogWarning($"🔌 Disconnected from Photon: {cause}");
		UpdateStatusDisplay(UIStatus.Disconnected, $"Mất kết nối: {cause}");
		SetCreateRoomButtonState(false);
		ResetOperationFlags();
		ClearJoinAttemptData();
	}

	public override void OnLeftLobby()
	{
		Debug.Log("🚪 Left Lobby");
		UpdateStatusDisplay(UIStatus.Disconnected, "Đã rời Lobby");
		SetCreateRoomButtonState(false);
		ResetOperationFlags();
		ClearJoinAttemptData();
	}

	public override void OnJoinedRoom()
	{
		Debug.Log($"🎉 Successfully joined room: '{PhotonNetwork.CurrentRoom.Name}'");
		LogRoomJoinDetails();

		// Save player name
		PlayerPrefs.SetString("playerName", PhotonNetwork.NickName);

		Room currentRoom = PhotonNetwork.CurrentRoom;

		// Handle password validation for private rooms
		if (!HandlePrivateRoomValidation(currentRoom))
		{
			return; // Password validation failed, will leave room
		}

		// Success - complete room join
		CompleteRoomJoin(currentRoom);
	}

	void LogRoomJoinDetails()
	{
		Debug.Log($"   Room Name: {PhotonNetwork.CurrentRoom.Name}");
		Debug.Log($"   Player Count: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
		Debug.Log($"   Is Master Client: {PhotonNetwork.IsMasterClient}");
		Debug.Log($"   Was Creating Room: {isCreatingRoom}");
		Debug.Log($"   Join Attempt Room: '{currentJoinAttemptRoomName}'");
		Debug.Log($"   Join Attempt Password: '{currentJoinAttemptPassword}'");
	}

	bool HandlePrivateRoomValidation(Room room)
	{
		bool isPrivateRoom = room.CustomProperties.ContainsKey("pwd");
		bool isRoomCreator = isCreatingRoom;

		Debug.Log($"🔐 Private room validation - IsPrivate: {isPrivateRoom}, IsCreator: {isRoomCreator}");

		if (!isPrivateRoom)
		{
			Debug.Log("✅ Public room - no password validation needed");
			return true;
		}

		if (isRoomCreator)
		{
			Debug.Log("✅ Room creator - skipping password validation");
			return true;
		}

		// Validate password for joining players
		return ValidateRoomPassword(room);
	}

	bool ValidateRoomPassword(Room room)
	{
		Debug.Log("🔍 Validating room password for joining player");

		// Check if we have join attempt data
		if (string.IsNullOrEmpty(currentJoinAttemptRoomName))
		{
			Debug.LogWarning("❌ No join attempt room name");
			StartCoroutine(LeaveRoomWithMessage("Lỗi xác thực phòng!"));
			return false;
		}

		// Verify we're joining the correct room
		if (!currentJoinAttemptRoomName.Equals(room.Name, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.LogWarning($"❌ Room name mismatch! Expected: '{currentJoinAttemptRoomName}', Got: '{room.Name}'");
			StartCoroutine(LeaveRoomWithMessage("Phòng không đúng!"));
			return false;
		}

		// Check password
		string expectedPassword = room.CustomProperties["pwd"] as string;
		string providedPassword = currentJoinAttemptPassword;

		Debug.Log($"   Expected: '{expectedPassword}', Provided: '{providedPassword}'");

		if (expectedPassword != providedPassword)
		{
			Debug.LogWarning("❌ Wrong password provided!");
			StartCoroutine(LeaveRoomWithMessage("Sai mật khẩu!"));
			return false;
		}

		Debug.Log("✅ Password validation successful");
		return true;
	}

	IEnumerator LeaveRoomWithMessage(string message)
	{
		UpdateStatusDisplay(UIStatus.Error, message);
		yield return new WaitForSeconds(2f);

		Debug.Log($"🚪 Leaving room due to: {message}");
		PhotonNetwork.LeaveRoom();
		ClearJoinAttemptData();
	}

	void CompleteRoomJoin(Room room)
	{
		Debug.Log("✅ Completing room join process");

		// Update status
		UpdateStatusDisplay(UIStatus.InRoom);

		// Clear join attempt data
		ClearJoinAttemptData();

		// Reset operation flags
		ResetOperationFlags();

		// Switch to room panel
		SwitchToRoomPanel();

		// Setup room UI
		SetupRoomUI(room);

		Debug.Log("🎉 Room join completed successfully!");
	}

	void SwitchToRoomPanel()
	{
		if (lobbyPanel != null) lobbyPanel.SetActive(false);
		if (roomPanel != null) roomPanel.SetActive(true);
	}

	void SetupRoomUI(Room room)
	{
		if (roomPanelManager != null)
		{
			roomPanelManager.SetupRoom(room.Name, room.MaxPlayers);
		}

		UpdatePlayerListInRoomPanel();
	}

	public override void OnLeftRoom()
	{
		Debug.Log("🚪 Left room successfully");

		// Clear all room-related data
		ClearJoinAttemptData();
		ResetOperationFlags();

		// Update UI
		SwitchToLobbyPanel();

		// Update status
		if (PhotonNetwork.InLobby)
		{
			UpdateStatusDisplay(UIStatus.InLobby, "Đã rời phòng");
		}
		else
		{
			UpdateStatusDisplay(UIStatus.Disconnected, "Đã rời phòng và lobby");
		}

		// Re-enable UI elements
		if (createRoomButton != null)
		{
			createRoomButton.interactable = true;
		}

		Debug.Log("✅ Room leave handled successfully");
	}

	void SwitchToLobbyPanel()
	{
		if (roomPanel != null) roomPanel.SetActive(false);
		if (lobbyPanel != null) lobbyPanel.SetActive(true);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		Debug.LogError($"❌ Create room failed - Code: {returnCode}, Message: {message}");
		UpdateStatusDisplay(UIStatus.Error, $"Tạo phòng thất bại: {message}");
		HandleRoomOperationFailure();
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.LogError($"❌ Join room failed - Code: {returnCode}, Message: {message}");
		UpdateStatusDisplay(UIStatus.Error, $"Vào phòng thất bại: {message}");
		HandleRoomOperationFailure();
		ClearJoinAttemptData();
	}

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		Debug.Log($"👋 Player joined room: {newPlayer.NickName}");
		UpdatePlayerListInRoomPanel();
	}

	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		Debug.Log($"👋 Player left room: {otherPlayer.NickName}");
		UpdatePlayerListInRoomPanel();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		Debug.Log($"📋 Room list updated - {roomList.Count} rooms");
		UpdateCachedRoomList(roomList);
		RefreshRoomListUI();
	}

	void UpdateCachedRoomList(List<RoomInfo> roomList)
	{
		foreach (RoomInfo room in roomList)
		{
			if (room.RemovedFromList)
			{
				if (cachedRoomList.ContainsKey(room.Name))
				{
					cachedRoomList.Remove(room.Name);
					Debug.Log($"🗑️ Removed room from cache: {room.Name}");
				}
			}
			else
			{
				cachedRoomList[room.Name] = room;
			}
		}
	}

	// ================= UI MANAGEMENT =================

	void SetCreateRoomButtonState(bool enabled)
	{
		if (createRoomButtonInLobby == null) return;

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
		}

		Debug.Log($"🎛️ Create Room button {(enabled ? "ENABLED" : "DISABLED")}");
	}

	void ClearRoomListUI()
	{
		if (roomListContent == null) return;

		foreach (Transform child in roomListContent)
		{
			Destroy(child.gameObject);
		}

		if (noRoomsText != null)
		{
			noRoomsText.gameObject.SetActive(true);
		}

		Debug.Log("🧹 Room list UI cleared");
	}

	void RefreshRoomListUI()
	{
		ClearRoomListUI();

		if (cachedRoomList.Count == 0)
		{
			if (noRoomsText != null)
			{
				noRoomsText.gameObject.SetActive(true);
			}
			return;
		}

		if (noRoomsText != null)
		{
			noRoomsText.gameObject.SetActive(false);
		}

		// Sort rooms by name
		var sortedRooms = cachedRoomList.Values.OrderBy(room => room.Name).ToList();

		foreach (RoomInfo roomInfo in sortedRooms)
		{
			CreateRoomListItem(roomInfo);
		}

		// Force layout rebuild
		StartCoroutine(RefreshLayoutNextFrame());

		Debug.Log($"🏗️ Room list UI refreshed - {sortedRooms.Count} rooms displayed");
	}

	void CreateRoomListItem(RoomInfo roomInfo)
	{
		if (roomItemPrefab == null || roomListContent == null) return;

		GameObject roomItemObject = Instantiate(roomItemPrefab, roomListContent);
		roomItemObject.SetActive(true);
		roomItemObject.transform.SetAsLastSibling();

		RoomItem roomItem = roomItemObject.GetComponent<RoomItem>();
		if (roomItem != null)
		{
			bool isPrivate = IsRoomPrivate(roomInfo);
			roomItem.Setup(roomInfo.Name, roomInfo.PlayerCount, roomInfo.MaxPlayers, isPrivate, this, roomInfo);
		}
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
			}
		}
	}

	void UpdatePlayerListInRoomPanel()
	{
		if (roomPanelManager == null) return;

		var players = PhotonNetwork.PlayerList;
		List<string> playerNames = new List<string>();

		foreach (var player in players)
		{
			playerNames.Add(player.NickName);
		}

		roomPanelManager.UpdatePlayerList(playerNames, PhotonNetwork.IsMasterClient);
	}

	void SetupRoomListLayout()
	{
		if (roomListContent == null) return;

		// Ensure Vertical Layout Group
		VerticalLayoutGroup layoutGroup = roomListContent.GetComponent<VerticalLayoutGroup>();
		if (layoutGroup == null)
		{
			layoutGroup = roomListContent.gameObject.AddComponent<VerticalLayoutGroup>();
		}

		// Configure layout
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
		}

		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Debug.Log("📐 Room list layout configured");
	}

	// ================= PUBLIC API =================

	public void OnPlayerNameChanged(string newName)
	{
		string finalName = string.IsNullOrEmpty(newName) ? GenerateRandomPlayerName() : newName;
		PhotonNetwork.NickName = finalName;
		PlayerPrefs.SetString("playerName", finalName);
		Debug.Log($"👤 Player name updated: {PhotonNetwork.NickName}");
	}

	public void OnMasterStartGame()
	{
		Debug.Log("🎮 Master client starting game");
		PhotonNetwork.LoadLevel("Gameplay");
	}

	// ================= DEBUG =================

	void OnGUI()
	{
		if (!Debug.isDebugBuild) return;

		GUILayout.BeginArea(new Rect(10, 10, 300, 200));
		GUILayout.Label($"Network State: {PhotonNetwork.NetworkClientState}");
		GUILayout.Label($"Current Status: {currentStatus}");
		GUILayout.Label($"In Lobby: {PhotonNetwork.InLobby}");
		GUILayout.Label($"Is Creating Room: {isCreatingRoom}");
		GUILayout.Label($"Is Processing: {isProcessingRoomOperation}");
		GUILayout.Label($"Join Attempt Room: '{currentJoinAttemptRoomName}'");
		GUILayout.EndArea();
	}
}