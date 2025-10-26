using UnityEngine;

public class ChangePosterAnomaly : BaseAnomaly
{
	// Kéo Renderer của poster (hoặc đối tượng có ảnh) vào đây
	public Renderer posterRenderer;

	// Material bình thường (ảnh gốc)
	public Material normalMaterial;

	// Material bất thường (ảnh đã bị thay đổi, ví dụ: ảnh ma quái)
	public Material anomalyMaterial;

	public override void ActivateAnomaly()
	{
		// Khi kích hoạt, đổi sang material bất thường
		if (posterRenderer != null)
		{
			posterRenderer.material = anomalyMaterial;
		}
	}

	public override void DeactivateAnomaly()
	{
		// Khi reset, trả lại material bình thường
		if (posterRenderer != null)
		{
			posterRenderer.material = normalMaterial;
		}
	}
}