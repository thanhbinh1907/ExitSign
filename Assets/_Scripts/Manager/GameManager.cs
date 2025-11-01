using UnityEngine;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
	public string playerPrefabName = "Player";
	[Tooltip("Kéo Prefab người chơi vào đây (cho chế độ Single Player)")]
	public GameObject playerPrefab; // Dùng cho Single Player
	private bool hasSpawned = false;

	void Start()
	{
		if (GameState.CurrentMode == GameMode.SinglePlayer)
		{
			Debug.Log("GameManager: Bắt đầu chế độ Single Player.");
			SpawnPlayerForSinglePlayer();
		}
		else // Chế độ Multiplayer
		{
			Debug.Log($"GameManager Start - Đang ở chế độ Multiplayer.");
			// Mỗi client tự gọi hàm này để spawn chính mình
			StartCoroutine(WaitAndSpawn());
		}
	}

	// HÀM MỚI: Spawn cho chế độ chơi đơn
	void SpawnPlayerForSinglePlayer()
	{
		if (playerPrefab == null)
		{
			Debug.LogError("Chưa gán Player Prefab vào GameManager trong Inspector!");
			return;
		}
		// Spawn tại một vị trí cố định (bạn có thể thay đổi vị trí này)
		Vector3 spawnPosition = new Vector3(0f, 1f, 0f);
		Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
		Debug.Log("Đã spawn người chơi cho chế độ Single Player.");
		hasSpawned = true;
	}

	// Logic spawn cho Multiplayer (từ file gốc)
	IEnumerator WaitAndSpawn()
	{
		// Đợi một chút để đảm bảo scene đã load xong
		yield return new WaitForSeconds(1f);

		// Chỉ spawn nếu chưa spawn (tránh spawn 2 lần nếu có lỗi)
		if (!hasSpawned)
		{
			SpawnPlayer();
			hasSpawned = true;
		}
	}

	// Logic spawn cho Multiplayer (từ file gốc)
	void SpawnPlayer()
	{
		Debug.Log($"Spawning player cho: {PhotonNetwork.LocalPlayer.NickName} (Actor: {PhotonNetwork.LocalPlayer.ActorNumber})");

		Vector3 spawnPosition;

		// ActorNumber 1 luôn là Master Client (người tạo phòng)
		if (PhotonNetwork.LocalPlayer.ActorNumber == 1) // Master Client
		{
			spawnPosition = new Vector3(-2f, 1f, 0f);
			Debug.Log("Master Client spawn bên trái");
		}
		else // Client 2 (hoặc bất kỳ ai khác)
		{
			spawnPosition = new Vector3(2f, 1f, 0f);
			Debug.Log($"Client {PhotonNetwork.LocalPlayer.ActorNumber} spawn bên phải");
		}

		// PhotonNetwork.Instantiate đảm bảo player được đồng bộ qua mạng
		GameObject player = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
		Debug.Log($"Đã spawn! ViewID: {player.GetComponent<PhotonView>().ViewID}");
	}

	// HÀM ĐƯỢC GỌI KHI NGƯỜI CHƠI MỚI VÀO PHÒNG
	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		// Bỏ qua nếu là Single Player
		if (GameState.CurrentMode == GameMode.SinglePlayer) return;

		Debug.Log($"Người chơi mới vào game: {newPlayer.NickName}. Tổng: {PhotonNetwork.CurrentRoom.PlayerCount}");

		// --- GIẢI THÍCH LOGIC SPAWN NGƯỜI CHƠI MỚI ---
		// Khi người chơi `newPlayer` vào phòng, máy của HỌ sẽ tải scene "Gameplay".
		// Khi scene tải xong, script `GameManager` trên máy của HỌ sẽ chạy hàm `Start()`.
		// Hàm `Start()` của HỌ sẽ gọi `WaitAndSpawn()`.
		// `WaitAndSpawn()` của HỌ sẽ gọi `SpawnPlayer()`.
		// `SpawnPlayer()` của HỌ sẽ tự động `PhotonNetwork.Instantiate` chính BẢN THÂN HỌ.

		// Vì vậy, những client khác (như Master Client) KHÔNG CẦN làm gì
		// trong hàm `OnPlayerEnteredRoom` để spawn người chơi mới.
		// Hàm này chủ yếu để thông báo hoặc để Master Client thực hiện logic khác (nếu cần).
	}
}