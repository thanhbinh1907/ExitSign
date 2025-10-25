using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

	// Kick system variables
	private bool isKickInProgress = false;
	private Coroutine currentKickCoroutine = null;

	void Start()
	{
		Debug.Log("🏠 RoomPanelManager Start() called");

		// Thiết lập button listeners
		SetupButtonListeners();

		// Setup player list layout
		SetupPlayerListLayout();

		Debug.Log("✅ RoomPanelManager initialization complete");
	}

	void SetupButtonListeners()
	{
		if (startButton != null)
		{
			startButton.onClick.RemoveAllListeners();
			startButton.onClick.AddListener(OnClickStart);
		}

		if (leaveButton != null)
		{
			leaveButton.onClick.RemoveAllListeners();
			leaveButton.onClick.AddListener(OnClickLeave);
		}

		if (readyButton != null)
		{
			readyButton.onClick.RemoveAllListeners();
			readyButton.onClick.AddListener(OnClickReady);
		}

		Debug.Log("🔗 Button listeners setup complete");
	}

	public void SetupRoom(string roomName, int maxPlayers)
	{
		Debug.Log($"🏠 Setting up room: '{roomName}' with max {maxPlayers} players");

		this.currentRoomName = roomName;
		this.maxPlayers = maxPlayers;

		// Cập nhật UI
		if (roomTitleText != null)
		{
			roomTitleText.text = "Phòng: " + roomName;
		}

		UpdateRoomStatus();
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);

		Debug.Log("✅ Room setup complete");
	}

	public void UpdatePlayerList(List<string> playerNames, bool isMaster)
	{
		Debug.Log($"👥 Updating player list - {playerNames.Count} players, viewerIsMaster={isMaster}");

		ClearPlayerList();

		// Reset ready states when player list changes
		playerReadyStates.Clear();

		foreach (string playerName in playerNames)
		{
			// Use enhanced method with kick logic
			CreatePlayerItemWithKickOption(playerName, isMaster);

			// Initialize ready state as false
			playerReadyStates[playerName] = false;

			Debug.Log($"   Created player item for: '{playerName}' with kick support");
		}

		UpdateMasterClientUI(isMaster);
		UpdateStartButton();
		UpdateRoomStatus();

		// Force layout update and kick button verification
		StartCoroutine(RefreshPlayerListLayoutDelayed());
	}

	void ClearPlayerList()
	{
		Debug.Log($"🧹 Clearing {playerUIItems.Count} player items");

		// Xóa tất cả player items
		foreach (GameObject item in playerUIItems)
		{
			if (item != null)
			{
				Destroy(item);
			}
		}
		playerUIItems.Clear();

		Debug.Log("✅ Player list cleared");
	}

	// Enhanced: Create player item with kick option
	void CreatePlayerItemWithKickOption(string playerName, bool viewerIsMaster)
	{
		Debug.Log($"👤 Creating player item for: '{playerName}', viewerIsMaster={viewerIsMaster}");

		if (playerItemPrefab == null)
		{
			Debug.LogError("❌ playerItemPrefab is null!");
			return;
		}

		if (playerListContent == null)
		{
			Debug.LogError("❌ playerListContent is null!");
			return;
		}

		GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);
		playerUIItems.Add(playerItem);

		playerItem.SetActive(true);
		playerItem.transform.SetAsLastSibling();

		PlayerItem playerItemScript = playerItem.GetComponent<PlayerItem>();
		if (playerItemScript != null)
		{
			Debug.Log($"🔧 Setting up PlayerItem script for: '{playerName}'");

			// 1. Reset everything first
			playerItemScript.ResetIcons();

			// 2. Set name (this stores originalPlayerName)
			playerItemScript.SetName(playerName);

			// 3. Determine if this player is master client
			string masterClientName = PhotonNetwork.MasterClient?.NickName ?? "";
			bool isMasterClient = playerName.Equals(masterClientName, System.StringComparison.OrdinalIgnoreCase);

			Debug.Log($"   Master client check: '{playerName}' == '{masterClientName}' = {isMasterClient}");

			// 4. Set master client status
			playerItemScript.SetAsMasterClient(isMasterClient);

			// 5. Determine kick button visibility
			bool shouldShowKickButton = DetermineKickButtonVisibility(playerName, viewerIsMaster, isMasterClient);

			Debug.Log($"🦵 Kick button decision for '{playerName}':");
			Debug.Log($"   viewerIsMaster: {viewerIsMaster}");
			Debug.Log($"   targetIsMaster: {isMasterClient}");
			Debug.Log($"   isNotSelf: {playerName != PhotonNetwork.NickName}");
			Debug.Log($"   shouldShow: {shouldShowKickButton}");

			// 6. Set kick button visibility
			playerItemScript.SetKickButtonVisible(shouldShowKickButton, viewerIsMaster);

			// 7. Schedule debug check if kick button should be visible
			if (shouldShowKickButton)
			{
				StartCoroutine(DebugKickButtonAfterDelay(playerItemScript, 1f));
			}
		}
		else
		{
			Debug.LogError($"❌ No PlayerItem script found on prefab for: '{playerName}'");
		}
	}

	// Determine kick button visibility logic
	bool DetermineKickButtonVisibility(string targetPlayerName, bool viewerIsMaster, bool targetIsMaster)
	{
		// Must be master client to see kick buttons
		if (!viewerIsMaster)
		{
			Debug.Log($"   Viewer not master → no kick button");
			return false;
		}

		// Can't kick master client
		if (targetIsMaster)
		{
			Debug.Log($"   Target is master → no kick button");
			return false;
		}

		// Can't kick yourself (additional safety check)
		if (targetPlayerName.Equals(PhotonNetwork.NickName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.Log($"   Target is self → no kick button");
			return false;
		}

		Debug.Log($"   All checks passed → show kick button");
		return true;
	}

	// Debug kick button after delay
	IEnumerator DebugKickButtonAfterDelay(PlayerItem playerItem, float delay)
	{
		yield return new WaitForSeconds(delay);

		if (playerItem != null)
		{
			Debug.Log($"🔍 POST-CREATION DEBUG for '{playerItem.originalPlayerName}':");
			playerItem.DebugKickButtonState();

			// Force kick button visible again if needed
			if (playerItem.kickButton != null && !playerItem.kickButton.gameObject.activeSelf)
			{
				Debug.LogWarning($"⚠️ Kick button not active after creation - forcing visible");

				bool viewerIsMaster = PhotonNetwork.IsMasterClient;
				string masterClientName = PhotonNetwork.MasterClient?.NickName ?? "";
				bool targetIsMaster = playerItem.originalPlayerName.Equals(masterClientName, System.StringComparison.OrdinalIgnoreCase);

				bool shouldShow = DetermineKickButtonVisibility(
					playerItem.originalPlayerName,
					viewerIsMaster,
					targetIsMaster
				);

				if (shouldShow)
				{
					Debug.Log($"🔧 Force-setting kick button visible for '{playerItem.originalPlayerName}'");
					playerItem.SetKickButtonVisible(true, viewerIsMaster);
				}
			}
		}
	}

	// Delayed layout refresh with kick button verification
	IEnumerator RefreshPlayerListLayoutDelayed()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame(); // Wait 2 frames for UI to settle

		if (playerListContent != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)playerListContent);
			Debug.Log("✅ Player list layout force-refreshed");
		}

		// Verify and force all kick buttons visible
		yield return new WaitForSeconds(0.5f);
		ForceAllKickButtonsVisible();
	}

	// Force all kick buttons visible
	void ForceAllKickButtonsVisible()
	{
		bool viewerIsMaster = PhotonNetwork.IsMasterClient;

		if (!viewerIsMaster)
		{
			Debug.Log("🦵 Viewer not master - no kick buttons needed");
			return;
		}

		Debug.Log("🦵 Force-checking all kick buttons...");

		string masterClientName = PhotonNetwork.MasterClient?.NickName ?? "";

		foreach (GameObject playerItemObj in playerUIItems)
		{
			if (playerItemObj == null) continue;

			PlayerItem playerItem = playerItemObj.GetComponent<PlayerItem>();
			if (playerItem == null) continue;

			string playerName = playerItem.originalPlayerName;
			bool targetIsMaster = playerName.Equals(masterClientName, System.StringComparison.OrdinalIgnoreCase);

			bool shouldShow = DetermineKickButtonVisibility(playerName, viewerIsMaster, targetIsMaster);

			Debug.Log($"🦵 Force-check '{playerName}': shouldShow={shouldShow}");

			if (shouldShow)
			{
				playerItem.SetKickButtonVisible(true, viewerIsMaster);

				// Double-check after setting
				if (playerItem.kickButton != null && !playerItem.kickButton.gameObject.activeSelf)
				{
					Debug.LogError($"❌ STILL NOT VISIBLE after force-set: '{playerName}'");
					playerItem.DebugKickButtonState();

					// Last resort: Try manual activation
					playerItem.kickButton.gameObject.SetActive(true);
					Debug.Log($"🔧 Manual activation attempted for '{playerName}'");
				}
				else if (playerItem.kickButton != null)
				{
					Debug.Log($"✅ Kick button confirmed visible for '{playerName}'");
				}
			}
		}
	}

	void UpdateMasterClientUI(bool isMaster)
	{
		Debug.Log($"👑 Updating master client UI: isMaster={isMaster}");

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

		// Check if all non-master players are ready
		bool allPlayersReady = true;
		int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
		int readyCount = 0;

		string masterClientName = PhotonNetwork.MasterClient?.NickName ?? "";

		foreach (var kvp in playerReadyStates)
		{
			string playerName = kvp.Key;
			bool isReady = kvp.Value;

			// Skip master client
			if (playerName.Equals(masterClientName, System.StringComparison.OrdinalIgnoreCase)) continue;

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

	// ================= ENHANCED KICK SYSTEM (FIXED) =================

	public void RequestKickPlayer(string playerName)
	{
		Debug.Log($"🦵 Kick requested for player: '{playerName}'");

		// Prevent multiple simultaneous kicks
		if (isKickInProgress)
		{
			Debug.LogWarning("⚠️ Kick already in progress - ignoring request");
			if (statusText != null)
			{
				statusText.text = "Đang thực hiện kick khác...";
				statusText.color = Color.yellow;
			}
			return;
		}

		// Only master client can kick
		if (!PhotonNetwork.IsMasterClient)
		{
			Debug.LogWarning("❌ Only master client can kick players!");
			if (statusText != null)
			{
				statusText.text = "Chỉ chủ phòng mới có thể kick!";
				statusText.color = Color.red;
			}
			return;
		}

		// Find the player to kick
		Photon.Realtime.Player playerToKick = null;
		foreach (var player in PhotonNetwork.PlayerList)
		{
			if (player.NickName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
			{
				playerToKick = player;
				break;
			}
		}

		if (playerToKick == null)
		{
			Debug.LogWarning($"❌ Player '{playerName}' not found in room!");
			if (statusText != null)
			{
				statusText.text = $"Không tìm thấy player: {playerName}";
				statusText.color = Color.red;
			}
			return;
		}

		// Can't kick master client
		if (playerToKick.IsMasterClient)
		{
			Debug.LogWarning("❌ Cannot kick master client!");
			if (statusText != null)
			{
				statusText.text = "Không thể kick chủ phòng!";
				statusText.color = Color.red;
			}
			return;
		}

		// Can't kick yourself
		if (playerToKick.NickName.Equals(PhotonNetwork.NickName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.LogWarning("❌ Cannot kick yourself!");
			if (statusText != null)
			{
				statusText.text = "Không thể kick chính mình!";
				statusText.color = Color.red;
			}
			return;
		}

		// Execute enhanced kick process
		isKickInProgress = true;
		if (currentKickCoroutine != null)
		{
			StopCoroutine(currentKickCoroutine);
		}
		currentKickCoroutine = StartCoroutine(EnhancedKickProcess(playerToKick));
	}

	// 🔥 FIXED: Enhanced multi-step kick system with fallbacks
	IEnumerator EnhancedKickProcess(Photon.Realtime.Player playerToKick)
	{
		Debug.Log($"🔧 Starting enhanced kick process for: {playerToKick.NickName}");

		if (statusText != null)
		{
			statusText.text = $"Đang kick {playerToKick.NickName}...";
			statusText.color = Color.yellow;
		}

		// STEP 1: Send RPC notifications with retry
		yield return StartCoroutine(SendKickNotification(playerToKick));

		// STEP 2: Wait for voluntary leave
		yield return StartCoroutine(WaitForVoluntaryLeave(playerToKick, 4f));

		// STEP 3: Check if player still in room
		if (IsPlayerStillInRoom(playerToKick))
		{
			Debug.LogWarning($"⚠️ Player {playerToKick.NickName} didn't leave voluntarily - trying force methods");
			yield return StartCoroutine(TryForceDisconnectMethods(playerToKick));
		}

		// STEP 4: Final check and UI update
		bool kickSuccessful = !IsPlayerStillInRoom(playerToKick);
		UpdateKickResult(playerToKick.NickName, kickSuccessful);

		// Reset kick state
		isKickInProgress = false;
		currentKickCoroutine = null;

		// Reset status after delay
		yield return new WaitForSeconds(3f);
		UpdateRoomStatus();
	}

	// 🔥 FIXED: Send kick notification with retry mechanism
	IEnumerator SendKickNotification(Photon.Realtime.Player playerToKick)
	{
		int maxRetries = 3;
		int attempts = 0;

		while (attempts < maxRetries)
		{
			attempts++;
			Debug.Log($"📡 Sending kick RPC to {playerToKick.NickName} - attempt {attempts}/{maxRetries}");

			bool rpcSent = false;
			System.Exception caughtException = null;

			// Try-catch WITHOUT yield
			try
			{
				// Send RPC to specific player
				photonView.RPC("NotifyPlayerKick", playerToKick, playerToKick.NickName);

				// Also send to all players for redundancy
				photonView.RPC("BroadcastKickEvent", RpcTarget.All, playerToKick.NickName, PhotonNetwork.NickName);

				rpcSent = true;
				Debug.Log($"✅ Kick RPCs sent successfully to {playerToKick.NickName}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"❌ Failed to send kick RPC (attempt {attempts}): {ex.Message}");
				caughtException = ex;
				rpcSent = false;
			}

			// Yield and break OUTSIDE of try-catch
			if (rpcSent)
			{
				break; // Success - exit retry loop
			}

			if (attempts < maxRetries)
			{
				yield return new WaitForSeconds(0.5f);
			}
		}
	}

	// Wait for voluntary leave with timeout
	IEnumerator WaitForVoluntaryLeave(Photon.Realtime.Player playerToKick, float timeout)
	{
		Debug.Log($"⏱️ Waiting {timeout}s for {playerToKick.NickName} to leave voluntarily");

		float elapsed = 0f;
		int originalPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;

		while (elapsed < timeout && IsPlayerStillInRoom(playerToKick))
		{
			yield return new WaitForSeconds(0.2f);
			elapsed += 0.2f;

			// Check if player count decreased (someone left)
			if (PhotonNetwork.CurrentRoom.PlayerCount < originalPlayerCount)
			{
				Debug.Log($"✅ Player count decreased - checking if {playerToKick.NickName} left");
				break;
			}
		}

		if (!IsPlayerStillInRoom(playerToKick))
		{
			Debug.Log($"✅ {playerToKick.NickName} left voluntarily");
		}
		else
		{
			Debug.LogWarning($"⚠️ {playerToKick.NickName} didn't leave after {timeout}s");
		}
	}

	// 🔥 FIXED: Try multiple force disconnect methods
	IEnumerator TryForceDisconnectMethods(Photon.Realtime.Player playerToKick)
	{
		Debug.Log($"🔧 Trying force disconnect methods for {playerToKick.NickName}");

		// Method 1: Try CloseConnection
		bool method1Success = false;
		try
		{
			Debug.Log($"🔧 Method 1: Attempting PhotonNetwork.CloseConnection");
			PhotonNetwork.CloseConnection(playerToKick);
			method1Success = true;
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning($"⚠️ Method 1 failed: {ex.Message}");
			method1Success = false;
		}

		if (method1Success)
		{
			yield return new WaitForSeconds(1f);

			if (!IsPlayerStillInRoom(playerToKick))
			{
				Debug.Log($"✅ Method 1 successful - {playerToKick.NickName} disconnected");
				yield break;
			}
		}

		// Method 2: Send aggressive disconnect RPC
		bool method2Success = false;
		try
		{
			Debug.Log($"🔧 Method 2: Sending aggressive disconnect RPC");
			photonView.RPC("ForceDisconnectPlayer", playerToKick, playerToKick.NickName);
			method2Success = true;
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning($"⚠️ Method 2 failed: {ex.Message}");
			method2Success = false;
		}

		if (method2Success)
		{
			yield return new WaitForSeconds(1.5f);

			if (!IsPlayerStillInRoom(playerToKick))
			{
				Debug.Log($"✅ Method 2 successful - {playerToKick.NickName} disconnected");
				yield break;
			}
		}

		// Method 3: Simulate network issues
		bool method3Success = false;
		try
		{
			Debug.Log($"🔧 Method 3: Simulating network issues");
			photonView.RPC("SimulateNetworkIssues", playerToKick, playerToKick.NickName);
			method3Success = true;
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning($"⚠️ Method 3 failed: {ex.Message}");
			method3Success = false;
		}

		if (method3Success)
		{
			yield return new WaitForSeconds(2f);
		}

		Debug.LogWarning($"⚠️ All force disconnect methods completed for {playerToKick.NickName}");
	}

	// Check if player still in room
	bool IsPlayerStillInRoom(Photon.Realtime.Player targetPlayer)
	{
		if (targetPlayer == null) return false;

		foreach (var player in PhotonNetwork.PlayerList)
		{
			if (player.ActorNumber == targetPlayer.ActorNumber)
			{
				return true;
			}
		}
		return false;
	}

	// Update kick result UI
	void UpdateKickResult(string playerName, bool successful)
	{
		if (statusText != null)
		{
			if (successful)
			{
				statusText.text = $"Đã kick {playerName} thành công!";
				statusText.color = Color.green;
				Debug.Log($"✅ Kick successful for {playerName}");
			}
			else
			{
				statusText.text = $"Không thể kick {playerName}!";
				statusText.color = Color.red;
				Debug.LogError($"❌ Kick failed for {playerName}");
			}
		}
	}

	// Separate method for emergency disconnect (to avoid yield in try-catch)
	void TryEmergencyDisconnect()
	{
		try
		{
			PhotonNetwork.Disconnect();
			Debug.Log("🔌 Emergency disconnect executed");
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Error in emergency disconnect: {ex.Message}");
		}
	}

	// ================= ENHANCED RPC METHODS =================

	// Broadcast kick event to all players
	[PunRPC]
	void BroadcastKickEvent(string kickedPlayerName, string masterClientName)
	{
		Debug.Log($"📡 BroadcastKickEvent: '{kickedPlayerName}' kicked by '{masterClientName}'");

		// If this is the kicked player, force leave immediately
		if (PhotonNetwork.NickName.Equals(kickedPlayerName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.Log("🦵 I am the kicked player - leaving immediately via broadcast");
			StartCoroutine(ForceLeaveRoom("Bạn đã bị kick bởi chủ phòng!"));
		}
	}

	// Original kick notification with immediate action
	[PunRPC]
	void NotifyPlayerKick(string kickedPlayerName)
	{
		Debug.Log($"📡 NotifyPlayerKick RPC received for '{kickedPlayerName}'");
		Debug.Log($"   My nickname: '{PhotonNetwork.NickName}'");
		Debug.Log($"   Comparison result: {PhotonNetwork.NickName.Equals(kickedPlayerName, System.StringComparison.OrdinalIgnoreCase)}");

		// If this is the kicked player, leave immediately
		if (PhotonNetwork.NickName.Equals(kickedPlayerName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.Log("🦵 I have been kicked from the room!");
			StartCoroutine(ForceLeaveRoom("Bạn đã bị kick khỏi phòng!"));
		}
	}

	// Force disconnect RPC
	[PunRPC]
	void ForceDisconnectPlayer(string playerName)
	{
		Debug.Log($"📡 ForceDisconnectPlayer RPC received for '{playerName}'");

		if (PhotonNetwork.NickName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.Log("🔌 Force disconnect requested - leaving room immediately");
			StartCoroutine(ForceLeaveRoom("Kết nối bị ngắt bởi chủ phòng!"));
		}
	}

	// Simulate network issues RPC
	[PunRPC]
	void SimulateNetworkIssues(string playerName)
	{
		Debug.Log($"📡 SimulateNetworkIssues RPC received for '{playerName}'");

		if (PhotonNetwork.NickName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
		{
			Debug.Log("📡 Simulating network disconnect - leaving room");
			StartCoroutine(ForceLeaveRoom("Mất kết nối mạng!"));
		}
	}

	// 🔥 FIXED: Force leave room with immediate action
	IEnumerator ForceLeaveRoom(string message)
	{
		Debug.Log($"🚪 ForceLeaveRoom called with message: '{message}'");

		// Show message immediately
		if (statusText != null)
		{
			statusText.text = message;
			statusText.color = Color.red;
		}

		// Disable UI to prevent further actions
		DisableRoomUI();

		// Short delay to show message, then leave immediately
		yield return new WaitForSeconds(0.5f);

		Debug.Log("🚪 Executing PhotonNetwork.LeaveRoom() immediately");

		bool leaveSuccess = false;
		System.Exception caughtException = null;

		// Try-catch WITHOUT yield
		try
		{
			PhotonNetwork.LeaveRoom();
			leaveSuccess = true;
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Error in ForceLeaveRoom: {ex.Message}");
			caughtException = ex;
			leaveSuccess = false;
		}

		// Yield OUTSIDE of try-catch
		if (leaveSuccess)
		{
			// Wait for leave to complete
			yield return new WaitForSeconds(1f);

			if (PhotonNetwork.InRoom)
			{
				Debug.LogWarning("⚠️ Still in room after LeaveRoom - trying disconnect");
				TryEmergencyDisconnect();
			}
		}
		else
		{
			// Try emergency disconnect immediately
			TryEmergencyDisconnect();
		}
	}

	// Disable room UI during kick
	void DisableRoomUI()
	{
		Debug.Log("🔒 Disabling room UI for kicked player");

		if (startButton != null) startButton.interactable = false;
		if (leaveButton != null) leaveButton.interactable = false;
		if (readyButton != null) readyButton.interactable = false;

		// Disable all player interaction
		foreach (var playerItem in playerUIItems)
		{
			if (playerItem != null)
			{
				PlayerItem script = playerItem.GetComponent<PlayerItem>();
				if (script != null && script.kickButton != null)
				{
					script.kickButton.interactable = false;
				}
			}
		}
	}

	// ================= BUTTON HANDLERS =================

	public void OnClickStart()
	{
		Debug.Log("🎮 Start button clicked");

		if (!PhotonNetwork.IsMasterClient)
		{
			Debug.LogWarning("❌ Only master client can start game!");
			if (statusText != null)
			{
				statusText.text = "Chỉ chủ phòng mới có thể bắt đầu!";
				statusText.color = Color.red;
			}
			return;
		}

		if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
		{
			Debug.LogWarning("❌ Not enough players to start");
			if (statusText != null)
			{
				statusText.text = "Cần ít nhất 2 người để bắt đầu!";
				statusText.color = Color.red;
			}
			return;
		}

		// Find NetworkManager and start game
		var networkManager = FindFirstObjectByType<NetworkManager>();
		if (networkManager != null)
		{
			Debug.Log("✅ Starting game via NetworkManager");
			networkManager.OnMasterStartGame();
		}
		else
		{
			Debug.LogWarning("⚠️ NetworkManager not found - using fallback");
			// Fallback: load scene directly
			PhotonNetwork.LoadLevel("Gameplay"); // Replace with your gameplay scene name
		}
	}

	public void OnClickLeave()
	{
		Debug.Log("🚪 Leave button clicked");

		// Confirm before leaving room
		if (statusText != null)
		{
			statusText.text = "Đang rời phòng...";
			statusText.color = Color.yellow;
		}

		try
		{
			PhotonNetwork.LeaveRoom();
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Error leaving room: {ex.Message}");
		}
	}

	public void OnClickReady()
	{
		Debug.Log("🎯 Ready button clicked");

		string myNickname = PhotonNetwork.NickName;
		bool currentReadyState = playerReadyStates.ContainsKey(myNickname) ? playerReadyStates[myNickname] : false;
		bool newReadyState = !currentReadyState;

		// Update local state
		playerReadyStates[myNickname] = newReadyState;

		// Send ready state via RPC to all players
		try
		{
			photonView.RPC("UpdatePlayerReady", RpcTarget.All, myNickname, newReadyState);
			Debug.Log($"✅ Ready RPC sent: {myNickname} = {newReadyState}");
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"❌ Failed to send ready RPC: {ex.Message}");
		}

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

	// ================= RPC METHODS =================

	// Enhanced: RPC method - receive ready updates
	[PunRPC]
	void UpdatePlayerReady(string playerName, bool isReady)
	{
		Debug.Log($"📡 RPC received: UpdatePlayerReady - '{playerName}' ready = {isReady}");

		// Update ready state
		playerReadyStates[playerName] = isReady;

		// Update UI
		UpdatePlayerReadyState(playerName, isReady);

		// Update start button
		UpdateStartButton();
	}

	void UpdatePlayerReadyState(string playerName, bool isReady)
	{
		Debug.Log($"🔄 Updating ready state for '{playerName}': {isReady}");

		foreach (GameObject playerItem in playerUIItems)
		{
			if (playerItem == null) continue;

			PlayerItem playerScript = playerItem.GetComponent<PlayerItem>();
			if (playerScript != null)
			{
				// Use original name for comparison
				string storedName = playerScript.originalPlayerName;

				Debug.Log($"   Checking: StoredName='{storedName}', Looking for='{playerName}'");

				if (storedName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
				{
					Debug.Log($"✅ EXACT MATCH! Setting ready={isReady} for '{playerName}'");
					playerScript.SetReady(isReady);
					return;
				}
			}
		}

		Debug.LogWarning($"⚠️ Player item not found for: '{playerName}'");
	}

	// ================= PHOTON CALLBACKS =================

	// Called from NetworkManager when player list changes
	public void OnPlayerListChanged()
	{
		Debug.Log("👥 OnPlayerListChanged called");
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);
	}

	void SetupPlayerListLayout()
	{
		if (playerListContent == null)
		{
			Debug.LogError("❌ playerListContent is null!");
			return;
		}

		Debug.Log("🎯 Setting up player list layout...");

		// Ensure Vertical Layout Group
		VerticalLayoutGroup layoutGroup = playerListContent.GetComponent<VerticalLayoutGroup>();
		if (layoutGroup == null)
		{
			layoutGroup = playerListContent.gameObject.AddComponent<VerticalLayoutGroup>();
			Debug.Log("✅ Added VerticalLayoutGroup to PlayerListContent");
		}

		// Configure layout settings
		layoutGroup.childAlignment = TextAnchor.UpperCenter;
		layoutGroup.childControlWidth = true;
		layoutGroup.childControlHeight = false;
		layoutGroup.childForceExpandWidth = true;
		layoutGroup.childForceExpandHeight = false;
		layoutGroup.spacing = 8f; // Increased spacing for better visibility
		layoutGroup.padding = new RectOffset(5, 5, 5, 5);

		// Ensure Content Size Fitter
		ContentSizeFitter sizeFitter = playerListContent.GetComponent<ContentSizeFitter>();
		if (sizeFitter == null)
		{
			sizeFitter = playerListContent.gameObject.AddComponent<ContentSizeFitter>();
			Debug.Log("✅ Added ContentSizeFitter to PlayerListContent");
		}

		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Debug.Log("✅ Player list layout configured");
	}

	// ================= DEBUG AND TEST METHODS =================

	// Context menu methods for testing
	[ContextMenu("Debug All Kick Buttons")]
	public void DebugAllKickButtons()
	{
		Debug.Log("🔍 === DEBUGGING ALL KICK BUTTONS ===");
		Debug.Log($"PhotonNetwork.IsMasterClient: {PhotonNetwork.IsMasterClient}");
		Debug.Log($"PhotonNetwork.MasterClient?.NickName: '{PhotonNetwork.MasterClient?.NickName}'");
		Debug.Log($"PhotonNetwork.NickName: '{PhotonNetwork.NickName}'");
		Debug.Log($"Player UI Items Count: {playerUIItems.Count}");
		Debug.Log($"Kick In Progress: {isKickInProgress}");

		foreach (GameObject playerItemObj in playerUIItems)
		{
			if (playerItemObj == null)
			{
				Debug.Log("   PlayerItem GameObject is NULL");
				continue;
			}

			PlayerItem playerItem = playerItemObj.GetComponent<PlayerItem>();
			if (playerItem != null)
			{
				playerItem.DebugKickButtonState();
			}
			else
			{
				Debug.Log($"   PlayerItem script missing on GameObject: {playerItemObj.name}");
			}
		}

		Debug.Log("🔍 === END DEBUG ALL KICK BUTTONS ===");
	}

	[ContextMenu("Force All Kick Buttons Visible")]
	public void ForceAllKickButtonsVisibleTest()
	{
		Debug.Log("🔧 FORCE TEST: Making all kick buttons visible");
		ForceAllKickButtonsVisible();
	}

	[ContextMenu("Refresh Player List")]
	public void RefreshPlayerListTest()
	{
		Debug.Log("🔄 TEST: Refreshing player list");
		UpdatePlayerList(GetCurrentPlayerNames(), PhotonNetwork.IsMasterClient);
	}

	[ContextMenu("Test Kick System")]
	public void TestKickSystem()
	{
		Debug.Log("🧪 Testing kick system...");

		if (!PhotonNetwork.IsMasterClient)
		{
			Debug.LogWarning("❌ Not master client - cannot test kick");
			return;
		}

		// Find first non-master player
		foreach (var player in PhotonNetwork.PlayerList)
		{
			if (!player.IsMasterClient)
			{
				Debug.Log($"🧪 Testing kick on player: {player.NickName}");
				RequestKickPlayer(player.NickName);
				break;
			}
		}
	}

	[ContextMenu("Force Leave (Test)")]
	public void TestForceLeave()
	{
		Debug.Log("🧪 Testing force leave...");
		StartCoroutine(ForceLeaveRoom("Test force leave"));
	}

	// Required for PUN
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		// Not needed for this implementation
	}
}