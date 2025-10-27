using UnityEngine;
using System.Collections.Generic;

// 1. Cập nhật lớp "con" để thêm chỉ số (index)
[System.Serializable]
public class PosterSwap
{
	public Renderer posterRenderer;

	// THÊM DÒNG NÀY
	[Tooltip("Chỉ số material cần đổi: 0 = Element 0, 1 = Element 1, ...")]
	public int materialIndex = 0; // Mặc định là 0

	public Material normalMaterial;
	public Material anomalyMaterial;
}

public class ChangePosterAnomaly : BaseAnomaly
{
	public List<PosterSwap> postersToSwap;

	// 2. Cập nhật hàm Kích hoạt
	public override void ActivateAnomaly()
	{
		foreach (PosterSwap swap in postersToSwap)
		{
			if (swap.posterRenderer != null)
			{
				// Lấy TẤT CẢ materials từ renderer về
				Material[] currentMaterials = swap.posterRenderer.materials;

				// Kiểm tra xem chỉ số có hợp lệ không
				if (swap.materialIndex < currentMaterials.Length)
				{
					// Thay đổi CHỈ material tại chỉ số đó
					currentMaterials[swap.materialIndex] = swap.anomalyMaterial;

					// Gán mảng đã cập nhật TRỞ LẠI renderer
					swap.posterRenderer.materials = currentMaterials;
				}
			}
		}
	}

	// 3. Cập nhật hàm Vô hiệu hóa
	public override void DeactivateAnomaly()
	{
		foreach (PosterSwap swap in postersToSwap)
		{
			if (swap.posterRenderer != null)
			{
				// Lấy TẤT CẢ materials về
				Material[] currentMaterials = swap.posterRenderer.materials;

				// Kiểm tra xem chỉ số có hợp lệ không
				if (swap.materialIndex < currentMaterials.Length)
				{
					// Trả lại material bình thường
					currentMaterials[swap.materialIndex] = swap.normalMaterial;

					// Gán mảng trở lại
					swap.posterRenderer.materials = currentMaterials;
				}
			}
		}
	}
}