using UnityEngine;
using System.Collections;

public class TrainDoor : MonoBehaviour
{
	[Tooltip("Cửa sẽ di chuyển bao xa khi mở. (Ví dụ: (1.2, 0, 0) cho cửa phải, (-1.2, 0, 0) cho cửa trái)")]
	public Vector3 openOffset;

	[Tooltip("Tốc độ di chuyển của cửa")]
	public float doorSpeed = 1.5f;

	private Vector3 closedPosition; // Vị trí "đóng" ban đầu
	private Vector3 openPosition;   // Vị trí "mở" (tính toán)
	private Coroutine doorCoroutine;

	void Awake()
	{
		// Lưu vị trí ban đầu (lúc đóng)
		closedPosition = transform.localPosition;

		// Tính toán vị trí "mở" dựa trên vị trí "đóng" và offset
		openPosition = closedPosition + openOffset;
	}

	// Hàm public để gọi từ bên ngoài (SubwayController)
	public void Open()
	{
		// Dừng coroutine cũ (nếu đang chạy) và bắt đầu coroutine Mở
		if (doorCoroutine != null)
		{
			StopCoroutine(doorCoroutine);
		}
		doorCoroutine = StartCoroutine(MoveDoor(openPosition));
	}

	// Hàm public để gọi từ bên ngoài (SubwayController)
	public void Close()
	{
		// Dừng coroutine cũ (nếu đang chạy) và bắt đầu coroutine Đóng
		if (doorCoroutine != null)
		{
			StopCoroutine(doorCoroutine);
		}
		doorCoroutine = StartCoroutine(MoveDoor(closedPosition));
	}

	// Coroutine thực hiện việc di chuyển cửa từ từ
	private IEnumerator MoveDoor(Vector3 targetPosition)
	{
		// Chừng nào chưa đến rất gần mục tiêu
		while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
		{
			// Di chuyển cửa về phía mục tiêu
			transform.localPosition = Vector3.MoveTowards(
				transform.localPosition,
				targetPosition,
				doorSpeed * Time.deltaTime
			);

			// Đợi đến khung hình tiếp theo
			yield return null;
		}

		// Đảm bảo cửa dừng chính xác ở vị trí mục tiêu
		transform.localPosition = targetPosition;
		doorCoroutine = null;
	}
}