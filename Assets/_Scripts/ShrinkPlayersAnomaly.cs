using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Cần cho Dictionary

// Kế thừa từ lớp "cha" BaseAnomaly
public class ShrinkPlayersAnomaly : BaseAnomaly
{
	[Header("Cài đặt Thu nhỏ")]
	public float minScale = 0.2f; // Kích thước nhỏ nhất (ví dụ: 0.2 = 20%)
	public float shrinkSpeed = 0.1f; // Tốc độ thu nhỏ (scale giảm 0.1 mỗi giây)

	[Header("Tag của Người chơi")]
	public string playerTag = "Player"; // Đảm bảo người chơi của bạn có tag này!

	// Dùng để quản lý coroutine
	private Coroutine shrinkCoroutine;

	// Dùng Dictionary để lưu trữ scale gốc của từng người chơi
	// Rất quan trọng để reset chính xác, kể cả khi có người chơi mới tham gia
	private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		// Xóa danh sách scale cũ (nếu có)
		originalScales.Clear();

		// Lưu scale gốc của tất cả người chơi HIỆN TẠI
		GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
		foreach (GameObject player in players)
		{
			if (!originalScales.ContainsKey(player.transform))
			{
				originalScales.Add(player.transform, player.transform.localScale);
			}
		}

		// Bắt đầu Coroutine thu nhỏ
		if (shrinkCoroutine != null)
		{
			StopCoroutine(shrinkCoroutine);
		}
		shrinkCoroutine = StartCoroutine(ShrinkProcess());
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		// Dừng Coroutine
		if (shrinkCoroutine != null)
		{
			StopCoroutine(shrinkCoroutine);
			shrinkCoroutine = null;
		}

		// Reset scale của tất cả người chơi đã bị ảnh hưởng
		foreach (var pair in originalScales)
		{
			// Kiểm tra xem người chơi còn trong scene không
			if (pair.Key != null)
			{
				pair.Key.localScale = pair.Value; // Trả về scale gốc
			}
		}
		// Xóa danh sách để chuẩn bị cho lần sau
		originalScales.Clear();
	}

	// Coroutine thực hiện việc thu nhỏ theo thời gian
	private IEnumerator ShrinkProcess()
	{
		Vector3 minScaleVector = new Vector3(minScale, minScale, minScale);

		// Chạy vô tận (sẽ bị dừng bởi DeactivateAnomaly)
		while (true)
		{
			// Tìm người chơi MỖI KHUNG HÌNH
			// Điều này để xử lý nếu có người chơi mới tham gia
			GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

			foreach (GameObject player in players)
			{
				Transform playerTransform = player.transform;

				// 1. Nếu là người chơi mới, lưu scale gốc của họ
				if (!originalScales.ContainsKey(playerTransform))
				{
					originalScales.Add(playerTransform, playerTransform.localScale);
				}

				// 2. Tính toán scale mới
				Vector3 currentScale = playerTransform.localScale;
				Vector3 newScale = currentScale - (Vector3.one * shrinkSpeed * Time.deltaTime);

				// 3. Đảm bảo scale không nhỏ hơn mức tối thiểu
				// Vector3.Max sẽ lấy giá trị lớn hơn cho từng thành phần (x, y, z)
				newScale = Vector3.Max(newScale, minScaleVector);

				// 4. Áp dụng scale mới
				playerTransform.localScale = newScale;
			}

			// Đợi đến khung hình tiếp theo
			yield return null;
		}
	}
}