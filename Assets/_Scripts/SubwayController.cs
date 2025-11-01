using UnityEngine;
using Photon.Pun;

public class SubwayController : MonoBehaviourPun
{
	// 3. Định nghĩa các trạng thái của tàu
	public enum TrainState
	{
		Idle,           // Đứng yên ở ga
		MovingForward,  // Đang đi tới (đến trạm tiếp)
		Returning       // Đang quay về vị trí cũ
	}

	[Header("Cài đặt Tốc độ")]
	public float moveSpeed = 15f;

	[Header("Mục tiêu (Targets)")]
	[Tooltip("Một GameObject rỗng đặt ở cuối đường ray")]
	public Transform endTarget;

	[Tooltip("Một GameObject rỗng đặt ở đầu đường hầm (nơi tàu sẽ teleport về)")]
	public Transform teleportTarget;

	[Header("Trigger Tag")]
	[Tooltip("Tag của GameObject Trigger ở cuối đường ray")]
	public string endTriggerTag = "TrainEndTrigger";

	[Header("Door Controls")]
	public System.Collections.Generic.List<TrainDoor> trainDoors;

	[Header("Âm thanh")] // <-- THÊM MỚI
	public AudioSource trainSoundSource; // <-- THÊM MỚI (Kéo AudioSource vào đây)

	private Vector3 startPosition; // Vị trí ban đầu của tàu
	private TrainState currentState = TrainState.Idle;

	// --- BIẾN MỚI ---
	// Cờ này sẽ được đặt thành true nếu chúng ta vừa hoàn thành một trạm bình thường
	private bool shouldResetCountOnDeparture = false;
	private int currentStationCount = 1;

	// Reference đến TrainControlButton để reset khi cần
	private TrainControlButton controlButton;

	private StationAnomalyManager instance;

	void Start()
	{
		// Lưu lại vị trí ban đầu khi game bắt đầu
		startPosition = transform.position;

		// Tìm control button
		controlButton = FindObjectOfType<TrainControlButton>();

		// --- THÊM DÒNG NÀY ĐỂ SỬA LỖI ---
		// Giả định rằng StationAnomalyManager nằm trong scene
		instance = FindObjectOfType<StationAnomalyManager>();
		if (instance == null)
		{
			Debug.LogError("SubwayController không tìm thấy StationAnomalyManager!");
		}
		// --- KẾT THÚC SỬA LỖI ---

		foreach (TrainDoor door in trainDoors)
		{
			door.Open();
		}

		// Đảm bảo âm thanh tàu tắt khi bắt đầu
		if (trainSoundSource != null)
		{
			trainSoundSource.Stop();
		}
	}

	void Update()
	{
		// Logic di chuyển này sẽ chạy trên TẤT CẢ các client
		// vì currentState được đồng bộ qua RPC

		if (currentState == TrainState.MovingForward)
		{
			// Di chuyển tàu về phía endTarget
			transform.position = Vector3.MoveTowards(transform.position, endTarget.position, moveSpeed * Time.deltaTime);
		}
		else if (currentState == TrainState.Returning)
		{
			// Di chuyển tàu về vị trí ban đầu
			transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);

			// Nếu đã về đến nơi, dừng lại
			if (transform.position == startPosition)
			{
				currentState = TrainState.Idle;
				Debug.Log("Tàu đã về ga và dừng lại.");

				// --- TẮT ÂM THANH TÀU --- // <-- THÊM MỚI
				if (trainSoundSource != null) trainSoundSource.Stop();

				// --- THÊM CODE MỞ CỬA ---
				foreach (TrainDoor door in trainDoors)
				{
					door.Open();
				}

				// 1. Kiểm tra xem có phải là MasterClient (MP) HOẶC là Single Player
				bool isMasterOrSinglePlayer = (GameState.CurrentMode == GameMode.SinglePlayer) ||
											  (GameState.CurrentMode == GameMode.Multiplayer && PhotonNetwork.IsMasterClient);

				if (isMasterOrSinglePlayer)
				{
					// --- BẮT ĐẦU THAY ĐỔI LOGIC ---

					// 1. HIỂN THỊ UI NGAY LẬP TỨC
					// Hiển thị số trạm hiện tại (ví dụ: Station 2)
					Debug.Log($"Đã đến trạm: {currentStationCount}. Gửi thông báo.");
					if (GameState.CurrentMode == GameMode.Multiplayer)
					{
						photonView.RPC("SyncStationUI", RpcTarget.All, currentStationCount);
					}
					else
					{
						SyncStationUI(currentStationCount);
					}

					// 2. KIỂM TRA VÀ TÍNH TOÁN CHO LẦN SAU
					bool hasAnomaly = instance.hasAnomaly();
					if (hasAnomaly)
					{
						// Trạm này CÓ anomaly, tăng số đếm cho trạm tiếp theo
						currentStationCount++;
						// Đảm bảo cờ reset bị tắt (vì chúng ta đang trong chuỗi anomaly)
						shouldResetCountOnDeparture = false;
					}
					else
					{
						// Trạm này BÌNH THƯỜNG.
						// KHÔNG reset counter vội.
						// Chỉ đặt cờ để báo cho hàm StartTrainRPC biết là cần reset KHI RỜI ĐI.
						shouldResetCountOnDeparture = true;
					}
				}

				// THÔNG BÁO CHO CONTROL BUTTON RESET
				if (controlButton != null)
				{
					controlButton.ResetButton();
				}
			}
		}
	}

	public float GetCurrentStationCount()
	{
		return currentStationCount;
	}

	// Hàm này được gọi khi tàu va chạm với Trigger
	private void OnTriggerEnter(Collider other)
	{
		if (currentState == TrainState.MovingForward && other.CompareTag(endTriggerTag))
		{
			if (GameState.CurrentMode == GameMode.Multiplayer)
			{
				if (PhotonNetwork.IsMasterClient)
				{
					photonView.RPC("TeleportAndReturnRPC", RpcTarget.All);
				}
			}
			else
			{
				TeleportAndReturnRPC(); // Gọi trực tiếp trong Single Player
			}
		}
	}

	// --- CÁC HÀM ĐỂ GỌI TỪ BÊN NGOÀI (NÚT BẤM) ---

	// Hàm này sẽ được nút bấm (Button) gọi
	public void TryStartTrain()
	{
		if (currentState == TrainState.Idle)
		{
			Debug.Log("Lệnh cho tàu chạy...");
			if (GameState.CurrentMode == GameMode.Multiplayer)
			{
				photonView.RPC("StartTrainRPC", RpcTarget.All);
			}
			else
			{
				StartTrainRPC(); // Gọi trực tiếp trong Single Player
			}
		}
	}

	public TrainState GetCurrentState()
	{
		return currentState;
	}

	// --- CÁC HÀM RPC ĐỒNG BỘ ---

	[PunRPC]
	void StartTrainRPC()
	{
		// CHỈ Master Client hoặc Single Player mới có quyền reset
		bool isMasterOrSinglePlayer = (GameState.CurrentMode == GameMode.SinglePlayer) ||
									  (GameState.CurrentMode == GameMode.Multiplayer && PhotonNetwork.IsMasterClient);

		// Nếu cờ "shouldReset" được bật (nghĩa là trạm trước là bình thường)
		if (isMasterOrSinglePlayer && shouldResetCountOnDeparture)
		{
			Debug.Log("Rời trạm bình thường. Reset đếm trạm về 0.");
			currentStationCount = 0;
			shouldResetCountOnDeparture = false; // Xóa cờ sau khi đã reset
		}

		Debug.Log("RPC: Tàu bắt đầu di chuyển.");
		currentState = TrainState.MovingForward;

		// --- PHÁT ÂM THANH TÀU --- // <-- THÊM MỚI
		if (trainSoundSource != null) trainSoundSource.Play();

		// --- THÊM CODE ĐÓNG CỬA ---
		foreach (TrainDoor door in trainDoors)
		{
			door.Close();
		}

	}

	// --- HÀM ĐÃ ĐƯỢC CẬP NHẬT ---
	[PunRPC]
	void TeleportAndReturnRPC()
	{
		Debug.Log("RPC: Tàu teleport và quay về.");

		// 1. Lưu vị trí cũ của tàu
		Vector3 oldTrainPosition = transform.position;

		// 2. Xác định vị trí mới
		Vector3 newTrainPosition;
		if (teleportTarget != null)
		{
			newTrainPosition = teleportTarget.position;
		}
		else
		{
			// Nếu không có teleportTarget, teleport về startPosition
			newTrainPosition = startPosition;
		}

		// 3. Tính toán khoảng cách dịch chuyển (offset)
		Vector3 teleportOffset = newTrainPosition - oldTrainPosition;

		// 4. Dịch chuyển tàu
		transform.position = newTrainPosition;

		// 5. Đặt trạng thái về Returning
		currentState = TrainState.Returning;

		// 6. Tìm và dịch chuyển TẤT CẢ người chơi đang ở trên tàu
		// (Hàm này chạy trên TẤT CẢ các client)
		PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
		foreach (PlayerMovement player in allPlayers)
		{
			// Kiểm tra xem player có đang ở trên tàu NÀY không
			if (player.IsOnTrain() && player.GetTrainTransformRef() == this.transform)
			{
				// Gọi hàm teleport trên script của player
				// (Hàm này sẽ tự kiểm tra "IsMine" bên trong)
				player.TeleportPlayer(teleportOffset);
			}
		}
	}

	// --- HÀM MỚI ĐỂ ĐỒNG BỘ UI ---
	[PunRPC]
	void SyncStationUI(int stationNumber)
	{
		PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
		foreach (PlayerMovement player in allPlayers)
		{
			player.ShowStationUI(stationNumber);
		}
	}
}