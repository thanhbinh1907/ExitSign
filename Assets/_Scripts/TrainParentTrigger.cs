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
			// Tìm script PlayerMovement
			PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
			if (playerMovement == null) return; // Không phải player

			// Xác định xem đây có phải là player của mình không
			bool isMyPlayer = (GameState.CurrentMode == GameMode.SinglePlayer) || (playerMovement.photonView != null && playerMovement.photonView.IsMine);

			if (isMyPlayer)
			{
				// Lấy actor number (dùng 1 cho SP)
				int actorNumber = (GameState.CurrentMode == GameMode.Multiplayer) ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
				Debug.Log($"Người chơi (Local) ActorNumber {actorNumber} đã LÊN TÀU.");

				// 1. Tác vụ cục bộ: Cập nhật trạng thái tàu cho player
				playerMovement.SetTrainTransform(trainTransform, true);

				// 2. Tác vụ cập nhật UI (luôn chạy)
				if (controlButton != null)
				{
					controlButton.OnPlayerBoardingStatusChanged(actorNumber, true);
				}

				// 3. Tác vụ mạng (chỉ chạy ở Multiplayer)
				if (GameState.CurrentMode == GameMode.Multiplayer && myPhotonView != null)
				{
					myPhotonView.RPC("SetPlayerParent", RpcTarget.All, playerMovement.photonView.ViewID, true);
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
			PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
			if (playerMovement == null) return;

			bool isMyPlayer = (GameState.CurrentMode == GameMode.SinglePlayer) || (playerMovement.photonView != null && playerMovement.photonView.IsMine);

			if (isMyPlayer)
			{
				int actorNumber = (GameState.CurrentMode == GameMode.Multiplayer) ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
				Debug.Log($"Người chơi (Local) ActorNumber {actorNumber} đã RỜI TÀU.");

				// 1. Tác vụ cục bộ
				playerMovement.SetTrainTransform(null, false);

				// 2. Tác vụ cập nhật UI
				if (controlButton != null)
				{
					controlButton.OnPlayerBoardingStatusChanged(actorNumber, false);
				}

				// 3. Tác vụ mạng
				if (GameState.CurrentMode == GameMode.Multiplayer && myPhotonView != null)
				{
					myPhotonView.RPC("SetPlayerParent", RpcTarget.All, playerMovement.photonView.ViewID, false);
					myPhotonView.RPC("UpdatePlayerBoardingStatus", RpcTarget.All, actorNumber, false);
				}
			}
		}
	}

	// --- Các hàm RPC đã được chỉnh sửa để KHÔNG parent player nữa ---
	[PunRPC]
	void SetPlayerParent(int playerViewID, bool shouldParent)
	{
		PhotonView targetPlayer = PhotonView.Find(playerViewID);

		if (targetPlayer != null)
		{
			PlayerMovement playerMovement = targetPlayer.GetComponent<PlayerMovement>();

			if (shouldParent)
			{
				// Thay vì parent, chỉ truyền tham chiếu train cho PlayerMovement
				Debug.Log($"RPC: Gán tham chiếu tàu cho player {targetPlayer.name} trên tất cả clients.");
				if (playerMovement != null)
				{
					playerMovement.SetTrainTransform(trainTransform, true);
				}
				else
				{
					Debug.LogWarning("PlayerMovement component không tìm thấy trên target player!");
				}
			}
			else
			{
				// Bỏ tham chiếu tàu
				Debug.Log($"RPC: Tháo tham chiếu tàu cho player {targetPlayer.name} trên tất cả clients.");
				if (playerMovement != null)
				{
					playerMovement.SetTrainTransform(null, false);
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