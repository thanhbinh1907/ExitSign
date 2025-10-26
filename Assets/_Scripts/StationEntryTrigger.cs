using UnityEngine;

public class StationEntryTrigger : MonoBehaviour
{
	// Kéo StationAnomalyManager của trạm này vào
	public StationAnomalyManager anomalyManager;

	private void OnTriggerEnter(Collider other)
	{
		// Giả sử chỉ người chơi (Player) mới kích hoạt
		if (other.CompareTag("Player"))
		{
			// Kích hoạt logic anomaly!
			anomalyManager.InitializeStation();
		}
	}
}