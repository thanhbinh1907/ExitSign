using UnityEngine;
using Photon.Pun;

// 1. Đổi kế thừa từ MonoBehaviourPun thành MonoBehaviour
public class TrainParentTrigger : MonoBehaviour
{
	private Transform trainTransform;
	private PhotonView myPhotonView; // 2. Biến mới để lưu PhotonView

	[Header("Control Button Reference")]
	public TrainControlButton controlButton;

	void Start()
	{
		trainTransform = transform.root;

		// 3. Tự tìm và lưu PhotonView khi Start
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

				// 4. Dùng biến 'myPhotonView'
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

				// 4. Dùng biến 'myPhotonView'
				if (myPhotonView != null)
				{
					myPhotonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, false);
					myPhotonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, actorNumber, false);
				}
			}
		}
	}

	// --- Các hàm RPC giữ nguyên, không thay đổi ---

	[PunRPC]
	void SetPlayerParent(int playerViewID, bool shouldParent)
	{
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

				targetPlayer.transform.SetParent(null);

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
			// Thử tìm lại control button
			controlButton = FindObjectOfType<TrainControlButton>();
			if (controlButton != null)
			{
				Debug.Log("Đã tìm thấy control button. Thử lại...");
				controlButton.OnPlayerBoardingStatusChanged(playerActorNumber, onTrain);
			}
		}
	}
}