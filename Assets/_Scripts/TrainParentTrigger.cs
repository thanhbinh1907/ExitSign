using UnityEngine;
using Photon.Pun;

public class TrainParentTrigger : MonoBehaviourPun
{
	// Biến này để lưu transform của object Tàu (cấp cao nhất)
	private Transform trainTransform;

	void Start()
	{
		// Tự động tìm object cha cao nhất (là cái Tàu)
		trainTransform = transform.root;
	}

	private void OnTriggerEnter(Collider other)
	{
		// 1. Kiểm tra xem có phải là "Player" không
		if (other.CompareTag("Player"))
		{
			// 2. Lấy PhotonView của Player
			PhotonView playerView = other.GetComponent<PhotonView>();

			// 3. QUAN TRỌNG: Chỉ local player mới gửi RPC để tránh duplicate calls
			if (playerView != null && playerView.IsMine)
			{
				Debug.Log("Người chơi (Local) đã LÊN TÀU. Gửi RPC đồng bộ.");

				// Gửi RPC cho TẤT CẢ clients để đồng bộ việc parenting
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, true);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		// 1. Kiểm tra xem có phải là "Player" không
		if (other.CompareTag("Player"))
		{
			// 2. Lấy PhotonView
			PhotonView playerView = other.GetComponent<PhotonView>();

			// 3. QUAN TRỌNG: Chỉ local player mới gửi RPC
			if (playerView != null && playerView.IsMine)
			{
				Debug.Log("Người chơi (Local) đã RỜI TÀU. Gửi RPC đồng bộ.");

				// Gửi RPC cho TẤT CẢ clients để đồng bộ việc un-parenting
				photonView.RPC("SetPlayerParent", RpcTarget.All, playerView.ViewID, false);
			}
		}
	}

	[PunRPC]
	void SetPlayerParent(int playerViewID, bool shouldParent)
	{
		// Tìm player bằng ViewID
		PhotonView targetPlayer = PhotonView.Find(playerViewID);

		if (targetPlayer != null)
		{
			if (shouldParent)
			{
				Debug.Log($"RPC: Gắn player {targetPlayer.name} vào tàu trên tất cả clients.");
				targetPlayer.transform.SetParent(trainTransform);
			}
			else
			{
				Debug.log($"RPC: Thả player {targetPlayer.name} ra khỏi tàu trên tất cả clients.");
				targetPlayer.transform.SetParent(null);
			}
		}
		else
		{
			Debug.LogError($"Không tìm thấy player với ViewID: {playerViewID}");
		}
	}
}