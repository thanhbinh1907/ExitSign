using UnityEngine;
using Photon.Pun; // Cần thư viện Photon

public class TrainParentTrigger : MonoBehaviourPun
{
	// Biến này để lưu transform của object Tàu (cấp cao nhất)
	private Transform trainTransform;

	void Start()
	{
		// Tự động tìm object cha cao nhất (là cái Tàu)
		trainTransform = transform.root;
		
		// Kiểm tra xem TrainParentTrigger có PhotonView không
		if (photonView == null)
		{
			Debug.LogError("❌ TrainParentTrigger cần có PhotonView component để đồng bộ parenting!");
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		// 1. Kiểm tra xem có phải là "Player" không
		if (other.CompareTag("Player"))
		{
			// 2. Lấy PhotonView của Player
			PhotonView playerView = other.GetComponent<PhotonView>();

			// 3. QUAN TRỌNG: Chỉ gửi RPC nếu đây là nhân vật của chính mình
			if (playerView != null && playerView.IsMine)
			{
				// Kiểm tra photonView trước khi gửi RPC
				if (photonView == null)
				{
					Debug.LogError("❌ Không thể gửi RPC: TrainParentTrigger không có PhotonView!");
					return;
				}
				
				Debug.Log($"Người chơi (Local) đã LÊN TÀU. Gửi RPC để đồng bộ - ViewID: {playerView.ViewID}");

				// Gửi RPC đến tất cả clients (bao gồm cả bản thân) để gắn player vào tàu
				photonView.RPC("RPC_SetPlayerParent", RpcTarget.AllBuffered, playerView.ViewID, true);
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

			// 3. QUAN TRỌNG: Chỉ gửi RPC nếu đây là nhân vật của chính mình
			if (playerView != null && playerView.IsMine)
			{
				// Kiểm tra photonView trước khi gửi RPC
				if (photonView == null)
				{
					Debug.LogError("❌ Không thể gửi RPC: TrainParentTrigger không có PhotonView!");
					return;
				}
				
				Debug.Log($"Người chơi (Local) đã RỜI TÀU. Gửi RPC để đồng bộ - ViewID: {playerView.ViewID}");

				// Gửi RPC đến tất cả clients (bao gồm cả bản thân) để thả player ra khỏi tàu
				photonView.RPC("RPC_SetPlayerParent", RpcTarget.AllBuffered, playerView.ViewID, false);
			}
		}
	}

	// RPC method to synchronize player parenting across all clients
	[PunRPC]
	void RPC_SetPlayerParent(int playerViewID, bool setParent)
	{
		// Tìm PhotonView của player dựa trên ViewID
		PhotonView playerView = PhotonView.Find(playerViewID);
		
		if (playerView == null)
		{
			Debug.LogWarning($"❌ Không tìm thấy PhotonView với ViewID: {playerViewID}");
			return;
		}

		if (setParent)
		{
			// Gắn player vào tàu
			Debug.Log($"✅ RPC nhận được: Gắn player '{playerView.Owner.NickName}' vào tàu (ViewID: {playerViewID})");
			playerView.transform.SetParent(trainTransform);
		}
		else
		{
			// Thả player ra khỏi tàu
			Debug.Log($"✅ RPC nhận được: Thả player '{playerView.Owner.NickName}' ra khỏi tàu (ViewID: {playerViewID})");
			playerView.transform.SetParent(null);
		}
	}
}