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

	private Vector3 startPosition; // Vị trí ban đầu của tàu
	private TrainState currentState = TrainState.Idle;

	// Reference đến TrainControlButton để reset khi cần
	private TrainControlButton controlButton;

	void Start()
	{
		// Lưu lại vị trí ban đầu khi game bắt đầu
		startPosition = transform.position;

		// Tìm control button
		controlButton = FindObjectOfType<TrainControlButton>();

		foreach (TrainDoor door in trainDoors)
		{
			door.Open();
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

				// --- THÊM CODE MỞ CỬA ---
				foreach (TrainDoor door in trainDoors)
				{
					door.Open();
				}

				// THÔNG BÁO CHO CONTROL BUTTON RESET
				if (controlButton != null)
				{
					controlButton.ResetButton();
				}
			}
		}
	}

	// Hàm này được gọi khi tàu va chạm với Trigger
	private void OnTriggerEnter(Collider other)
	{
		// Kiểm tra: Tàu phải đang đi tới VÀ va chạm đúng trigger
		if (currentState == TrainState.MovingForward && other.CompareTag(endTriggerTag))
		{
			// QUAN TRỌNG: Chỉ MasterClient mới được quyền
			// gửi lệnh dịch chuyển để tránh 2 người gửi cùng lúc
			if (PhotonNetwork.IsMasterClient)
			{
				Debug.Log("Tàu chạm EndTrigger. Gửi lệnh dịch chuyển về...");

				// Gửi RPC cho TẤT CẢ mọi người
				photonView.RPC("TeleportAndReturnRPC", RpcTarget.All);
			}
		}
	}

	// --- CÁC HÀM ĐỂ GỌI TỪ BÊN NGOÀI (NÚT BẤM) ---

	// Hàm này sẽ được nút bấm (Button) gọi
	public void TryStartTrain()
	{
		// Chỉ cho phép chạy khi tàu đang đứng yên
		if (currentState == TrainState.Idle)
		{
			Debug.Log("Nút bấm được nhấn. Gửi lệnh cho tàu chạy...");

			// Gửi RPC để TẤT CẢ client cùng chạy tàu
			photonView.RPC("StartTrainRPC", RpcTarget.All);
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
		Debug.Log("RPC: Tàu bắt đầu di chuyển.");
		currentState = TrainState.MovingForward;

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
}