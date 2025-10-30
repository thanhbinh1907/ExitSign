using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class StationAnomalyManager : MonoBehaviourPun
{
	// Kéo TẤT CẢ các script anomaly (SimpleShow, ToggleLights, SpawnGhost)
	// từ trong Scene vào danh sách này
	public List<BaseAnomaly> allAnomalies;

	[HideInInspector]
	public bool isStationNormal = true;

	private BaseAnomaly currentAnomaly = null; // Anomaly đang chạy

	// Hàm này được gọi khi người chơi "vào" station
	public void InitializeStation()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			DecideAnomaly();
		}
	}

	void DecideAnomaly()
	{
		// 50% cơ hội có anomaly
		// bool hasAnomaly = Random.Range(0, 100) < 50;
		bool hasAnomaly = true;
		int anomalyID = -1; // -1 là "bình thường"

		if (hasAnomaly && allAnomalies.Count > 0)
		{
			// Chọn một anomaly ngẫu nhiên TỪ DANH SÁCH
			anomalyID = Random.Range(0, allAnomalies.Count);
		}

		// Gửi thông báo RPC cho TẤT CẢ người chơi
		photonView.RPC("SyncAnomalyState", RpcTarget.AllBuffered, anomalyID);
	}

	public bool hasAnomaly()
	{
		return isStationNormal;
	}

	[PunRPC]
	void SyncAnomalyState(int id)
	{
		// Bước 1: Reset anomaly cũ (nếu có)
		if (currentAnomaly != null)
		{
			currentAnomaly.DeactivateAnomaly();
			currentAnomaly = null;
		}

		// Bước 2: Kích hoạt anomaly được chọn
		if (id == -1)
		{
			// Station này BÌNH THƯỜNG
			isStationNormal = true;
			Debug.Log("SYNC: Station này bình thường.");
		}
		else
		{
			// Station này CÓ BẤT THƯỜNG
			isStationNormal = false;
			currentAnomaly = allAnomalies[id];
			currentAnomaly.ActivateAnomaly();
			Debug.Log($"SYNC: Kích hoạt Anomaly #{id}: {currentAnomaly.name}");
		}
	}
}
