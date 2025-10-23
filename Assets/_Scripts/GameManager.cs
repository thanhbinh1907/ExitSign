using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
	public string playerPrefabName = "Player"; // Đặt tên Prefab nhân vật của bạn ở đây

	void Start()
	{
		Debug.Log("GameManager đã chạy! Đang spawn nhân vật...");

		// Spawn nhân vật ở vị trí ngẫu nhiên
		float randomX = Random.Range(-5f, 5f);
		float randomZ = Random.Range(-5f, 5f);
		Vector3 spawnPosition = new Vector3(randomX, 1, randomZ); // Chỉnh Y nếu cần

		// Dùng PhotonNetwork.Instantiate để spawn nhân vật
		// Prefab "Player" của bạn PHẢI nằm trong thư mục "Resources"
		PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
	}
}