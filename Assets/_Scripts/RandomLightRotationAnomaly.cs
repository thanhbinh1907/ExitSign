using UnityEngine;
using System.Collections.Generic; // Cần để dùng List

// Kế thừa từ lớp "cha" BaseAnomaly
public class RandomLightRotationAnomaly : BaseAnomaly
{
	// Kéo TẤT CẢ các đối tượng đèn (Lights) bạn muốn xoay vào đây
	public List<Transform> lightsToRotate;

	// Danh sách private để lưu lại góc xoay "bình thường" ban đầu
	private List<Quaternion> originalRotations;

	// Dùng Awake() để lưu trạng thái ban đầu
	void Awake()
	{
		originalRotations = new List<Quaternion>();
		foreach (Transform light in lightsToRotate)
		{
			// Lưu lại góc xoay ban đầu của mỗi đèn
			originalRotations.Add(light.localRotation);
		}
	}

	// *** HÀM ĐÃ ĐƯỢC CẬP NHẬT ***
	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		// Lặp qua từng đèn bằng index để truy cập góc xoay gốc
		for (int i = 0; i < lightsToRotate.Count; i++)
		{
			// Lấy góc xoay gốc đã lưu (dưới dạng Euler)
			Vector3 originalAngles = originalRotations[i].eulerAngles;

			// 1. Chỉ tạo một giá trị ngẫu nhiên cho trục Y
			float randomYAngle = Random.Range(0f, 360f);

			// 2. Áp dụng góc xoay mới
			// Giữ nguyên X và Z gốc, chỉ thay đổi Y
			lightsToRotate[i].localRotation = Quaternion.Euler(
				originalAngles.x,
				randomYAngle,
				originalAngles.z
			);
		}
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		// Khi reset, trả lại góc xoay ban đầu cho từng đèn
		for (int i = 0; i < lightsToRotate.Count; i++)
		{
			if (i < originalRotations.Count)
			{
				lightsToRotate[i].localRotation = originalRotations[i];
			}
		}
	}
}