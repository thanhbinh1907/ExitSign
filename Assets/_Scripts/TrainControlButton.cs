using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class TrainControlButton : MonoBehaviourPun, IMatchmakingCallbacks
{
	[Header("Tàu điều khiển")]
	public SubwayController trainController;

	[Header("UI Settings")]
	public GameObject interactionPrompt;
	public GameObject waitingPrompt;
	public KeyCode interactionKey = KeyCode.E;

	[Header("Player Tracking")]
	public TrainParentTrigger trainParentTrigger;

	private bool playerInRange = false;
	private bool trainStarted = false;

	// Dictionary để track players trên tàu
	private Dictionary<int, bool> playersOnTrain = new Dictionary<int, bool>();

	void Start()
	{
		if (interactionPrompt != null)
			interactionPrompt.SetActive(false);

		if (waitingPrompt != null)
			waitingPrompt.SetActive(false);

		InitializePlayerTracking();

		// Subscribe to Photon callbacks
		PhotonNetwork.AddCallbackTarget(this);
	}

	void OnDestroy()
	{
		// Unsubscribe when destroyed
		if (PhotonNetwork.NetworkingClient != null)
		{
			PhotonNetwork.RemoveCallbackTarget(this);
		}
	}

	void InitializePlayerTracking()
	{
		playersOnTrain.Clear();

		foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
		{
			playersOnTrain[player.ActorNumber] = false;
		}

		Debug.Log($"Khởi tạo tracking cho {playersOnTrain.Count} players");
	}

	void Update()
	{
		if (playerInRange && Input.GetKeyDown(interactionKey))
		{
			TryStartTrain();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PhotonView playerView = other.GetComponent<PhotonView>();
			if (playerView != null && playerView.IsMine)
			{
				playerInRange = true;
				UpdateUI();
				Debug.Log("Vào vùng điều khiển tàu.");
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PhotonView playerView = other.GetComponent<PhotonView>();
			if (playerView != null && playerView.IsMine)
			{
				playerInRange = false;
				HideAllUI();
				Debug.Log("Rời vùng điều khiển tàu.");
			}
		}
	}

	void UpdateUI()
	{
		if (trainStarted)
		{
			HideAllUI();
			return;
		}

		if (!playerInRange)
		{
			HideAllUI();
			return;
		}

		if (AreAllPlayersOnTrain())
		{
			if (interactionPrompt != null)
				interactionPrompt.SetActive(true);

			if (waitingPrompt != null)
				waitingPrompt.SetActive(false);
		}
		else
		{
			if (interactionPrompt != null)
				interactionPrompt.SetActive(false);

			if (waitingPrompt != null)
				waitingPrompt.SetActive(true);
		}
	}

	void HideAllUI()
	{
		if (interactionPrompt != null)
			interactionPrompt.SetActive(false);

		if (waitingPrompt != null)
			waitingPrompt.SetActive(false);
	}

	bool AreAllPlayersOnTrain()
	{
		int totalPlayers = PhotonNetwork.PlayerList.Length;
		int playersOnTrainCount = 0;

		foreach (var kvp in playersOnTrain)
		{
			if (kvp.Value)
			{
				playersOnTrainCount++;
			}
		}

		Debug.Log($"Players trên tàu: {playersOnTrainCount}/{totalPlayers}");

		return playersOnTrainCount >= totalPlayers && totalPlayers > 0;
	}

	void TryStartTrain()
	{
		if (trainStarted)
		{
			Debug.Log("Tàu đã được khởi động rồi!");
			return;
		}

		if (trainController == null)
		{
			Debug.LogError("Chưa gán TrainController!");
			return;
		}

		if (!AreAllPlayersOnTrain())
		{
			Debug.Log("Chưa đủ tất cả người chơi lên tàu!");
			return;
		}

		Debug.Log("Tất cả người chơi đã lên tàu. Khởi động tàu!");

		trainController.TryStartTrain();
		trainStarted = true;
		HideAllUI();

		photonView.RPC("OnTrainStarted", RpcTarget.Others);
	}

	public void OnPlayerBoardingStatusChanged(int playerActorNumber, bool onTrain)
	{
		playersOnTrain[playerActorNumber] = onTrain;

		Debug.Log($"Player {playerActorNumber} trạng thái trên tàu: {onTrain}");

		if (playerInRange)
		{
			UpdateUI();
		}

		photonView.RPC("SyncPlayerBoardingStatus", RpcTarget.Others, playerActorNumber, onTrain);
	}

	[PunRPC]
	void SyncPlayerBoardingStatus(int playerActorNumber, bool onTrain)
	{
		playersOnTrain[playerActorNumber] = onTrain;
		Debug.Log($"RPC: Player {playerActorNumber} trạng thái trên tàu: {onTrain}");

		if (playerInRange)
		{
			UpdateUI();
		}
	}

	[PunRPC]
	void OnTrainStarted()
	{
		trainStarted = true;
		HideAllUI();
		Debug.Log("RPC: Tàu đã được khởi động bởi người chơi khác.");
	}

	public void ResetButton()
	{
		trainStarted = false;
		InitializePlayerTracking();

		if (playerInRange)
			UpdateUI();
	}

	// Implementation của IMatchmakingCallbacks
	public void OnFriendListUpdate(List<FriendInfo> friendList)
	{
		// Not needed for this use case
	}

	public void OnCreatedRoom()
	{
		// Not needed for this use case
	}

	public void OnCreateRoomFailed(short returnCode, string message)
	{
		// Not needed for this use case
	}

	public void OnJoinedRoom()
	{
		Debug.Log("Joined room - resetting player tracking");
		InitializePlayerTracking();

		if (playerInRange)
			UpdateUI();
	}

	public void OnJoinRoomFailed(short returnCode, string message)
	{
		// Not needed for this use case
	}

	public void OnJoinRandomFailed(short returnCode, string message)
	{
		// Not needed for this use case
	}

	public void OnLeftRoom()
	{
		Debug.Log("Left room");
		playersOnTrain.Clear();
	}

	public void OnPlayerEnteredRoom(Player newPlayer)
	{
		Debug.Log($"Player {newPlayer.ActorNumber} joined room");

		// Thêm player mới vào tracking
		playersOnTrain[newPlayer.ActorNumber] = false;

		if (playerInRange)
			UpdateUI();
	}

	public void OnPlayerLeftRoom(Player otherPlayer)
	{
		Debug.Log($"Player {otherPlayer.ActorNumber} left room");

		// Xóa player khỏi tracking
		if (playersOnTrain.ContainsKey(otherPlayer.ActorNumber))
		{
			playersOnTrain.Remove(otherPlayer.ActorNumber);
		}

		if (playerInRange)
			UpdateUI();
	}

	public void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		// Not needed for this use case
	}

	public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
	{
		// Not needed for this use case
	}

	public void OnMasterClientSwitched(Player newMasterClient)
	{
		Debug.Log($"Master client switched to {newMasterClient.ActorNumber}");
	}

	public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
	{
		// Not needed for this use case
	}
}