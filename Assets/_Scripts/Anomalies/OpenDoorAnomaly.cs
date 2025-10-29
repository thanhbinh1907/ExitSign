using UnityEngine;

// Kế thừa từ lớp "cha" BaseAnomaly
public class OpenDoorAnomaly : BaseAnomaly
{
	// Đặt góc xoay "mở" trong Inspector
	// Ví dụ: (0, 90, 0) hoặc (0, -90, 0) tùy theo trục bản lề
	public Vector3 openRotationAngles;

	// Biến private để lưu trữ góc xoay "đóng" ban đầu
	private Quaternion closedRotation;

	// Dùng Start() để lưu lại trạng thái "đóng" ban đầu khi game bắt đầu
	void Start()
	{
		// Giả định script này được gắn TRỰC TIẾP lên cửa (ví dụ: "Door_A")
		closedRotation = transform.localRotation;
	}

	public override void ActivateAnomaly()
	{
		// Khi kích hoạt, xoay cửa đến vị trí "mở"
		// Dùng Quaternion.Euler để chuyển đổi Vector3 (độ) sang Quaternion (góc xoay)
		transform.localRotation = Quaternion.Euler(openRotationAngles);
	}

	public override void DeactivateAnomaly()
	{
		// Khi reset, trả cửa về vị trí "đóng" ban đầu
		transform.localRotation = closedRotation;
	}
}