using UnityEngine;

public class TrainStartTrigger : MonoBehaviour
{
	// Kéo object Tàu (ví dụ: Subway_car) vào đây
	public SubwayController trainController;

	// Biến này để đảm bảo trigger chỉ chạy 1 lần
	private bool hasBeenTriggered = false;

	private void OnTriggerEnter(Collider other)
	{
		// Kiểm tra xem có phải là Player không, và trigger chưa được kích hoạt
		if (other.CompareTag("Player") && !hasBeenTriggered)
		{
			// Kiểm tra xem đã gán tàu vào script chưa
			if (trainController != null)
			{
				Debug.Log("Player đã vào trigger. Kích hoạt tàu!");

				// Gọi hàm chạy tàu
				trainController.TryStartTrain();

				// Đánh dấu là đã chạy
				hasBeenTriggered = true;
			}
			else
			{
				Debug.LogError("Chưa gán TrainController vào TrainStartTrigger!");
			}
		}
	}
}