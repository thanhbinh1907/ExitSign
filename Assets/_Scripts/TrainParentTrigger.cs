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
				Debug.Log("Người chơi (Local) đã LÊN TÀU. Gửi RPC đồng bộ.");

				// Gửi RPC để set parent trước
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, true);

				// Gửi RPC để cập nhật trạng thái boarding trên TẤT CẢ clients
				photonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, true);
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
				Debug.Log("Người chơi (Local) đã RỜI TÀU. Gửi RPC đồng bộ.");

				// Gửi RPC để unparent trước
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, false);

				// Gửi RPC để cập nhật trạng thái boarding trên TẤT CẢ clients
				photonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, false);
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

				// Tắt controller trước khi parenting
				if (controller != null)
				{
					controller.enabled = false;
				}

				// Set parent
				targetPlayer.transform.SetParent(trainTransform);

				// Bật lại controller
				if (controller != null)
				{
					controller.enabled = true;
				}

				// Thông báo PlayerMovement
				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(true);
				}
			}
			else
			{
				Debug.Log($"RPC: Thả player {targetPlayer.name} ra khỏi tàu trên tất cả clients.");

				// 🔥 FIX: Thông báo PlayerMovement TRƯỚC
				if (playerMovement != null)
				{
					playerMovement.SetOnTrain(false);
				}

				// 🔥 FIX: Tắt controller TRƯỚC khi unparent
				if (controller != null)
				{
					controller.enabled = false;
				}

				// 🔥 FIX: Lưu vị trí world trước khi unparent
				Vector3 worldPosition = targetPlayer.transform.position;
				Quaternion worldRotation = targetPlayer.transform.rotation;

				// Unparent
				targetPlayer.transform.SetParent(null);

				// 🔥 FIX: Đảm bảo vị trí không bị thay đổi
				targetPlayer.transform.position = worldPosition;
				targetPlayer.transform.rotation = worldRotation;

				// 🔥 FIX: Chờ 1 frame rồi bật lại controller
				if (targetPlayer.GetComponent<MonoBehaviour>() != null)
				{
					targetPlayer.GetComponent<MonoBehaviour>().StartCoroutine(EnableControllerAfterFrame(controller));
				}
			}
		}
		else
		{
			Debug.LogError($"Không tìm thấy player với ViewID: {playerViewID}");
		}
	}

	// 🔥 NEW: Coroutine để enable controller sau 1 frame
	private System.Collections.IEnumerator EnableControllerAfterFrame(CharacterController controller)
	{
		yield return new WaitForEndOfFrame();

		if (controller != null)
		{
			controller.enabled = true;
			Debug.Log($"✅ CharacterController đã được bật lại sau unparent.");
		}
	}

	// RPC: Cập nhật trạng thái boarding trên tất cả clients
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