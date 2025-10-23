using UnityEngine;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
	public string playerPrefabName = "Player";
	private bool hasSpawned = false;

	void Start()
	{
		Debug.Log($"GameManager Start - Số người chơi: {PhotonNetwork.CurrentRoom.PlayerCount}");

		// Đợi một chút để đảm bảo tất cả client đã load scene xong
		StartCoroutine(WaitAndSpawn());
	}

	IEnumerator WaitAndSpawn()
	{
		// Đợi 1 giây để chắc chắn tất cả client đã vào scene
		yield return new WaitForSeconds(1f);

		// Kiểm tra nếu đã có đủ người chơi
		int attempts = 0;
		while (PhotonNetwork.CurrentRoom.PlayerCount < 2 && attempts < 10)
		{
			Debug.Log($"Đang đợi người chơi... Hiện tại: {PhotonNetwork.CurrentRoom.PlayerCount}/2");
			yield return new WaitForSeconds(0.5f);
			attempts++;
		}

		if (!hasSpawned && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
		{
			SpawnPlayer();
			hasSpawned = true;
		}
		else if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
		{
			Debug.LogWarning("Không đủ người chơi để spawn!");
		}
	}

	void SpawnPlayer()
	{
		Debug.Log($"Spawning player cho: {PhotonNetwork.LocalPlayer.NickName} (Actor: {PhotonNetwork.LocalPlayer.ActorNumber})");

		Vector3 spawnPosition;

		if (PhotonNetwork.LocalPlayer.ActorNumber == 1) // Master Client
		{
			spawnPosition = new Vector3(-2f, 1f, 0f);
			Debug.Log("Master Client spawn bên trái");
		}
		else // Client 2
		{
			spawnPosition = new Vector3(2f, 1f, 0f);
			Debug.Log("Client 2 spawn bên phải");
		}

		GameObject player = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
		Debug.Log($"Đã spawn! ViewID: {player.GetComponent<PhotonView>().ViewID}");
	}

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		Debug.Log($"Người chơi mới vào game: {newPlayer.NickName}. Tổng: {PhotonNetwork.CurrentRoom.PlayerCount}");

		// Nếu chưa spawn và giờ đã đủ người thì spawn
		if (!hasSpawned && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
		{
			StartCoroutine(DelayedSpawn());
		}
	}

	IEnumerator DelayedSpawn()
	{
		yield return new WaitForSeconds(0.5f);
		if (!hasSpawned)
		{
			SpawnPlayer();
			hasSpawned = true;
		}
	}
}