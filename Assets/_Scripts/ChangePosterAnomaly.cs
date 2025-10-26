using UnityEngine;
using System.Collections.Generic; // <-- Thêm dòng này để sử dụng List

// 1. Tạo một lớp nhỏ để nhóm các đối tượng cần thay đổi
// [System.Serializable] cho phép nó xuất hiện trong Inspector
[System.Serializable]
public class PosterSwap
{
	public Renderer posterRenderer;
	public Material normalMaterial;
	public Material anomalyMaterial;
}

public class ChangePosterAnomaly : BaseAnomaly
{
	// 2. Thay vì 3 biến đơn lẻ, giờ chúng ta có MỘT danh sách
	// Kéo tất cả các cặp poster bạn muốn thay đổi vào đây
	public List<PosterSwap> postersToSwap;

	// 3. Cập nhật hàm Kích hoạt
	public override void ActivateAnomaly()
	{
		// Lặp qua TẤT CẢ các poster trong danh sách
		foreach (PosterSwap swap in postersToSwap)
		{
			if (swap.posterRenderer != null)
			{
				// Đổi sang material bất thường
				swap.posterRenderer.material = swap.anomalyMaterial;
			}
		}
	}

	// 4. Cập nhật hàm Vô hiệu hóa
	public override void DeactivateAnomaly()
	{
		// Lặp qua TẤT CẢ các poster trong danh sách
		foreach (PosterSwap swap in postersToSwap)
		{
			if (swap.posterRenderer != null)
			{
				// Trả lại material bình thường
				swap.posterRenderer.material = swap.normalMaterial;
			}
		}
	}
}