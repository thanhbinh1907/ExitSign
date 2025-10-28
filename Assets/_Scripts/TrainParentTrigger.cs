using UnityEngine;
using Photon.Pun; // Cần thư viện Photon

public class TrainParentTrigger : MonoBehaviour
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

			// 3. QUAN TRỌNG: Chỉ "gắn" (parent)
			// NẾU ĐÓ LÀ NHÂN VẬT CỦA CHÍNH MÌNH
			if (playerView != null && playerView.IsMine)
			{
				Debug.Log("Người chơi (Local) đã LÊN TÀU. Gắn vào tàu.");

				// Gắn transform của Player làm "con" của Tàu
				other.transform.SetParent(trainTransform);
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

			// 3. QUAN TRỌNG: Chỉ "thả" (un-parent)
			// NẾU ĐÓ LÀ NHÂN VẬT CỦA CHÍNH MÌNH
			if (playerView != null && playerView.IsMine)
			{
				Debug.Log("Người chơi (Local) đã RỜI TÀU. Thả ra.");

				// Thả Player ra (không còn là "con" của ai cả)
				other.transform.SetParent(null);
			}
		}
	}
}