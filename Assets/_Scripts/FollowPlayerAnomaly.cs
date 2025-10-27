using UnityEngine;

// Kế thừa từ lớp "cha" BaseAnomaly
public class FollowPlayerAnomaly : BaseAnomaly
{
	[Header("Cài đặt Theo dõi")]
	public float lookSpeed = 2.0f; // Tốc độ camera xoay
	public string playerTag = "Player"; // Đảm bảo người chơi có tag này

	// Biến private để lưu trạng thái
	private Quaternion originalRotation;
	private Transform targetToFollow;
	private bool isFollowing = false;

	// Dùng Awake() để lưu lại trạng thái ban đầu
	void Awake()
	{
		// Giả định script này được gắn TRỰC TIẾP lên camera
		originalRotation = transform.rotation;
	}

	// Hàm này được StationAnomalyManager gọi
	public override void ActivateAnomaly()
	{
		isFollowing = true;
		targetToFollow = null; // Xóa mục tiêu cũ (nếu có)
	}

	// Hàm này được gọi khi reset station
	public override void DeactivateAnomaly()
	{
		isFollowing = false;
		targetToFollow = null;

		// Reset camera về góc quay ban đầu
		transform.rotation = originalRotation;
	}

	// Update được gọi mỗi khung hình
	void Update()
	{
		// Nếu anomaly không hoạt động, không làm gì cả
		if (!isFollowing)
		{
			return;
		}

		// Tìm người chơi gần nhất để theo dõi
		FindClosestPlayer();

		// Nếu có mục tiêu (người chơi)
		if (targetToFollow != null)
		{
			// Tính toán hướng nhìn tới người chơi
			Vector3 direction = targetToFollow.position - transform.position;
			Quaternion targetRotation = Quaternion.LookRotation(direction);

			// Xoay camera một cách mượt mà (Slerp)
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
		}
	}

	// Hàm trợ giúp để tìm người chơi gần nhất
	private void FindClosestPlayer()
	{
		GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
		float closestDistance = Mathf.Infinity;
		GameObject closestPlayer = null;

		foreach (GameObject player in players)
		{
			float distance = Vector3.Distance(transform.position, player.transform.position);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestPlayer = player;
			}
		}

		if (closestPlayer != null)
		{
			targetToFollow = closestPlayer.transform;
		}
		else
		{
			targetToFollow = null;
		}
	}
}