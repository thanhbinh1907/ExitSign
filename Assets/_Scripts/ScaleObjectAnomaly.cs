using UnityEngine;
using System.Collections; // Cần thiết cho Coroutine

// Kế thừa từ lớp "cha" BaseAnomaly
public class ScaleObjectAnomaly : BaseAnomaly
{
	[Header("Cài đặt Phóng to")]
	public Vector3 maxScale = new Vector3(3f, 3f, 3f); // Kích thước lớn nhất
	public float scaleSpeed = 0.5f; // Tốc độ phóng to (đơn vị scale/giây)

	// Biến private để lưu kích thước ban đầu
	private Vector3 originalScale;
	private Coroutine scalingCoroutine;

	// Dùng Awake() để lưu lại trạng thái ban đầu
	void Awake()
	{
		// Giả định script này được gắn TRỰC TIẾP lên đối tượng cần phóng to
		originalScale = transform.localScale;
	}

	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		// Đảm bảo dừng coroutine cũ nếu nó đang chạy
		if (scalingCoroutine != null)
		{
			StopCoroutine(scalingCoroutine);
		}
		// Bắt đầu Coroutine mới
		scalingCoroutine = StartCoroutine(ScalingProcess());
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		// Dừng Coroutine
		if (scalingCoroutine != null)
		{
			StopCoroutine(scalingCoroutine);
			scalingCoroutine = null;
		}

		// Reset đồng hồ về kích thước ban đầu ngay lập tức
		transform.localScale = originalScale;
	}

	// Coroutine thực hiện việc phóng to
	private IEnumerator ScalingProcess()
	{
		// Tiếp tục chạy chừng nào một trong các trục scale còn nhỏ hơn maxScale
		while (transform.localScale.x < maxScale.x ||
			   transform.localScale.y < maxScale.y ||
			   transform.localScale.z < maxScale.z)
		{
			// Phóng to đối tượng lên một chút mỗi khung hình
			// Vector3.one * ... đảm bảo nó to lên đồng đều trên mọi trục
			transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;

			// Đảm bảo không vượt quá kích thước tối đa
			transform.localScale = Vector3.Min(transform.localScale, maxScale);

			// Đợi đến khung hình tiếp theo
			yield return null;
		}
	}
}