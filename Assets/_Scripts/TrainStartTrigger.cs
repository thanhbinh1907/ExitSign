using UnityEngine;

public class TrainStartTrigger : MonoBehaviour
{
	// Kéo object Tàu (ví dụ: Subway_car) vào đây
	public SubwayController trainController;

	private void OnTriggerEnter(Collider other)
	{
		// Kiểm tra xem có phải là Player không, và trigger chưa được kích hoạt
		if (other.CompareTag("Player"))
		{
			// Kiểm tra xem đã gán tàu vào script chưa
			if (trainController != null)
			{
				Debug.Log("Player đã vào trigger. Kích hoạt tàu!");

				// Gọi hàm chạy tàu
				trainController.TryStartTrain();

			}
			else
			{
				Debug.LogError("Chưa gán TrainController vào TrainStartTrigger!");
			}
		}
	}
}