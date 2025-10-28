using UnityEngine;

// Kế thừa từ lớp "cha" BaseAnomaly
public class HidePlayerLayerAnomaly : BaseAnomaly
{
	[Header("Camera An ninh")]
	// Kéo Camera an ninh (cái đang render) vào đây
	public Camera securityCamera;

	[Header("Layer của Người chơi")]
	// Tên của Layer bạn đã tạo ở Bước 1
	public string playerLayerName = "LocalPlayerBody";

	private int originalCullingMask; // Để lưu cài đặt gốc
	private int playerLayerMask; // Bitmask cho layer "Player"

	void Awake()
	{
		if (securityCamera != null)
		{
			// Lưu lại cài đặt gốc khi game bắt đầu
			originalCullingMask = securityCamera.cullingMask;
		}

		// Chuyển tên layer (string) thành một bitmask (int)
		playerLayerMask = 1 << LayerMask.NameToLayer(playerLayerName);
	}

	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		if (securityCamera == null) return;

		// CullingMask gốc AND VỚI (NOT playerLayerMask)
		// Phép toán bitwise này sẽ tắt bit của layer "Player"
		// mà không ảnh hưởng đến các layer khác.
		securityCamera.cullingMask = originalCullingMask & ~playerLayerMask;
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		if (securityCamera == null) return;

		// Trả lại cài đặt CullingMask gốc
		securityCamera.cullingMask = originalCullingMask;
	}
}