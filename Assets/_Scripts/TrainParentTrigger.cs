using UnityEngine;
using Photon.Pun;

public class TrainParentTrigger : MonoBehaviour
{
	private Transform trainTransform;
	private PhotonView myPhotonView;

	[Header("Control Button Reference")]
	public TrainControlButton controlButton;

	void Start()
	{
		trainTransform = transform.root;

		myPhotonView = GetComponent<PhotonView>();
		if (myPhotonView == null)
		{
			Debug.LogError("TrainParentTrigger BỊ LỖI: Không tìm thấy PhotonView component trên object này!");
		}

		if (controlButton == null)
		{
			controlButton = FindObjectOfType<TrainControlButton>();
		}

		Debug.Log($"TrainParentTrigger started. Control button found: {controlButton != null}");
	}

	private void OnTriggerEnter(Collider other)
	{
		Debug.Log($"OnTriggerEnter: {other.name}");

		if (other.CompareTag("Player"))
		{
			PhotonView playerView = other.GetComponent<PhotonView>();
			Debug.Log($"Player detected: {other.name}, PhotonView: {playerView != null}, IsMine: {playerView?.IsMine}");

			if (playerView != null && playerView.IsMine)
			{
				int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
				Debug.Log($"Người chơi (Local) ActorNumber {actorNumber} đã LÊN TÀU. Gửi RPC đồng bộ.");

				if (myPhotonView != null)
				{
					myPhotonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, true);
					myPhotonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, actorNumber, true);
				}
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Debug.Log($"OnTriggerExit: {other.name}");

		if (other.CompareTag("Player"))
		{
			PhotonView playerView = other.GetComponent<PhotonView>();
			Debug.Log($"Player exit detected: {other.name}, PhotonView: {playerView != null}, IsMine: {playerView?.IsMine}");

			if (playerView != null && playerView.IsMine)
			{
				int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
				Debug.Log($"Người chơi (Local) ActorNumber {actorNumber} đã RỜI TÀU. Gửi RPC đồng bộ.");

				if (myPhotonView != null)
				{
					myPhotonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, false);
					myPhotonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, actorNumber, false);
				}
			}
		}
	}

	[PunRPC]
	void SetPlayerParent(int playerViewID, bool shouldParent)
	{
		PhotonView targetPlayer = PhotonView.Find(playerViewID);

		if (targetPlayer != null)
		{
			PlayerMovement playerMovement = targetPlayer.GetComponent<PlayerMovement>();
			CharacterController controller = targetPlayer.GetComponent<CharacterController>();

			if (shouldParent)
			{
				Debug.Log($"RPC: Gắn player {targetPlayer.name} vào tàu trên tất cả clients.");

				// 🔥 QUAN TRỌNG: Tắt CharacterController trước khi parent
				if (controller != null)
				{
					controller.enabled = false;
					Debug.Log($"Đã TẮT CharacterController của {targetPlayer.name}");
				}

				// Parent vào tàu
				targetPlayer.transform.SetParent(trainTransform);

				// Thông báo cho PlayerMovement
				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(true);
				}

				// 🔥 BẬT LẠI CharacterController sau khi parent (quan trọng!)
				if (controller != null)
				{
					controller.enabled = true;
					Debug.Log($"Đã BẬT lại CharacterController của {targetPlayer.name}");
				}
			}
			else
			{
				Debug.Log($"RPC: Thả player {targetPlayer.name} ra khỏi tàu trên tất cả clients.");

				// Thông báo cho PlayerMovement
				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(false);
				}

				// 🔥 Tắt CharacterController trước khi unparent
				if (controller != null)
				{
					controller.enabled = false;
					Debug.Log($"Đã TẮT CharacterController trước khi unparent {targetPlayer.name}");
				}

				// Unparent
				targetPlayer.transform.SetParent(null);

				// 🔥 Bật lại CharacterController
				if (controller != null)
				{
					controller.enabled = true;
					Debug.Log($"Đã BẬT lại CharacterController của {targetPlayer.name}");
				}
			}
		}
		else
		{
			Debug.LogError($"Không tìm thấy player với ViewID: {playerViewID}");
		}
	}

	[PunRPC]
	void UpdatePlayerBoardingStatus(int playerActorNumber, bool onTrain)
	{
		Debug.Log($"RPC: Cập nhật trạng thái player {playerActorNumber} trên tàu: {onTrain}");

		if (controlButton != null)
		{
			controlButton.OnPlayerBoardingStatusChanged(playerActorNumber, onTrain);
			Debug.Log($"Đã gọi OnPlayerBoardingStatusChanged cho player {playerActorNumber}");
		}
		else
		{
			Debug.LogError("Control button không tìm thấy!");
			controlButton = FindObjectOfType<TrainControlButton>();
			if (controlButton != null)
			{
				Debug.Log("Đã tìm thấy control button. Thử lại...");
				controlButton.OnPlayerBoardingStatusChanged(playerActorNumber, onTrain);
			}
		}
	}
}