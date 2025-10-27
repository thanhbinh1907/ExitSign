using UnityEngine;
using System.Collections; // Cần thiết cho Coroutine

// Kế thừa từ lớp "cha" BaseAnomaly
public class LowerCeilingAnomaly : BaseAnomaly
{
	[Header("Cài đặt Trần nhà")]
	public float lowerSpeed = 0.2f; // Tốc độ trần nhà đi xuống (mét/giây)
	public float lowestYPosition = -1.5f; // Vị trí Y thấp nhất mà trần nhà sẽ dừng lại

	// Biến private để lưu vị trí ban đầu
	private Vector3 originalPosition;
	private Coroutine loweringCoroutine;

	// Dùng Awake() để lưu lại trạng thái ban đầu
	void Awake()
	{
		// Giả định script này được gắn TRỰC TIẾP lên trần nhà
		originalPosition = transform.position;
	}

	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		// Đảm bảo dừng coroutine cũ nếu nó đang chạy
		if (loweringCoroutine != null)
		{
			StopCoroutine(loweringCoroutine);
		}
		// Bắt đầu Coroutine mới
		loweringCoroutine = StartCoroutine(LoweringProcess());
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		// Dừng Coroutine
		if (loweringCoroutine != null)
		{
			StopCoroutine(loweringCoroutine);
			loweringCoroutine = null;
		}

		// Reset trần nhà về vị trí ban đầu ngay lập tức
		transform.position = originalPosition;
	}

	// Coroutine thực hiện việc hạ trần nhà xuống
	private IEnumerator LoweringProcess()
	{
		// Tiếp tục chạy chừng nào vị trí Y hiện tại còn cao hơn vị trí mục tiêu
		while (transform.position.y > lowestYPosition)
		{
			// Di chuyển trần nhà xuống một chút mỗi khung hình
			transform.Translate(Vector3.down * lowerSpeed * Time.deltaTime);

			// Đợi đến khung hình tiếp theo
			yield return null;
		}

		// Sau khi vòng lặp kết thúc, đảm bảo nó dừng chính xác ở vị trí thấp nhất
		Vector3 finalPos = transform.position;
		finalPos.y = lowestYPosition;
		transform.position = finalPos;
	}
}