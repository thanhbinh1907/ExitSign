using UnityEngine;
using Photon.Pun;

public class TrainParentTrigger : MonoBehaviourPun
{
	private Transform trainTransform;

	[Header("Control Button Reference")]
	public TrainControlButton controlButton;

	void Start()
	{
		trainTransform = transform.root;

		if (controlButton == null)
		{
			controlButton = FindObjectOfType<TrainControlButton>();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PhotonView playerView = other.GetComponent<PhotonView>();

			if (playerView != null && playerView.IsMine)
			{
				Debug.Log(">>> OnTriggerEnter (Local) - Gửi RPC SetParent(true)"); // DEBUG

				// Gửi RPC để set parent trước
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, true);

				// SỬA: Gửi RPC để cập nhật trạng thái boarding trên TẤT CẢ clients
				photonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, true);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			// DEBUG: Thêm log này BÊN NGOÀI check IsMine
			Debug.Log($"--- OnTriggerExit: Player {other.name} đã rời trigger ---");

			PhotonView playerView = other.GetComponent<PhotonView>();

			if (playerView != null && playerView.IsMine)
			{
				Debug.Log(">>> OnTriggerExit (Local) - Gửi RPC SetParent(false)"); // DEBUG

				// Gửi RPC để unparent trước
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, false);

				// SỬA: Gửi RPC để cập nhật trạng thái boarding trên TẤT CẢ clients
				photonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, false);
			}
		}
	}

	[PunRPC]
	void SetPlayerParent(int playerViewID, bool shouldParent)
	{
		Debug.Log($"--- RPC SetPlayerParent nhận: ViewID {playerViewID}, shouldParent = {shouldParent} ---"); // DEBUG

		PhotonView targetPlayer = PhotonView.Find(playerViewID);

		if (targetPlayer != null)
		{
			PlayerMovement playerMovement = targetPlayer.GetComponent<PlayerMovement>();

			if (shouldParent)
			{
				Debug.Log($"RPC: Gắn player {targetPlayer.name} vào tàu trên tất cả clients.");

				CharacterController controller = targetPlayer.GetComponent<CharacterController>();
				if (controller != null)
				{
					controller.enabled = false;
				}

				targetPlayer.transform.SetParent(trainTransform);

				if (controller != null)
				{
					controller.enabled = true;
				}

				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(true);
				}
			}
			else
			{
				Debug.Log($"RPC: Thả player {targetPlayer.name} ra khỏi tàu trên tất cả clients.");

				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(false);
				}

				CharacterController controller = targetPlayer.GetComponent<CharacterController>();
				if (controller != null)
				{
					controller.enabled = false;
				}

				Debug.Log($"!!! THỰC HIỆN SetParent(null) cho {targetPlayer.name} !!!"); // DEBUG

				// THỬ SỬA: Dùng SetParent(null, true) để đảm bảo world position không đổi
				// Mặc dù SetParent(null) thường tự làm điều này, nhưng đây là cách rõ ràng hơn
				targetPlayer.transform.SetParent(null, true);

				if (controller != null)
				{
					controller.enabled = true;
				}
			}
		}
		else
		{
			Debug.LogError($"Không tìm thấy player với ViewID: {playerViewID}");
		}
	}

	// RPC MỚI: Cập nhật trạng thái boarding trên tất cả clients
	[PunRPC]
	void UpdatePlayerBoardingStatus(int playerActorNumber, bool onTrain)
	{
		Debug.Log($"RPC: Cập nhật trạng thái player {playerActorNumber} trên tàu: {onTrain}");

		if (controlButton != null)
		{
			controlButton.OnPlayerBoardingStatusChanged(playerActorNumber, onTrain);
		}
		else
		{
			Debug.LogError("Control button không tìm thấy!");
		}
	}
}