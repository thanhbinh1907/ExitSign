using UnityEngine;
using Photon.Pun;

// Đây là lớp "cha"
// Bất kỳ script anomaly nào cũng sẽ kế thừa từ lớp này
public abstract class BaseAnomaly : MonoBehaviourPun
{
	// Tất cả anomaly BẮT BUỘC phải có hàm Kích hoạt
	// Đây là nơi logic anomaly xảy ra (hiện bao xác, tắt đèn, v.v.)
	public abstract void ActivateAnomaly();

	// Và một hàm Vô hiệu hóa để reset station
	public abstract void DeactivateAnomaly();
}
