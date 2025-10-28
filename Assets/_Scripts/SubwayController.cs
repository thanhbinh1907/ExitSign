using UnityEngine;
using Photon.Pun; // 1. Thêm thư viện Photon

// 2. Yêu cầu phải có PhotonView
[RequireComponent(typeof(PhotonView))]
public class TrainController : MonoBehaviourPun
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

	private Vector3 startPosition; // Vị trí ban đầu của tàu
	private TrainState currentState = TrainState.Idle;

	void Start()
	{
		// Lưu lại vị trí ban đầu khi game bắt đầu
		startPosition = transform.position;
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

	// --- CÁC HÀM RPC ĐỒNG BỘ ---

	[PunRPC]
	void StartTrainRPC()
	{
		Debug.Log("RPC: Tàu bắt đầu di chuyển.");
		currentState = TrainState.MovingForward;
	}

	[PunRPC]
	void TeleportAndReturnRPC()
	{
		Debug.Log("RPC: Dịch chuyển và quay về.");

		// 1. Dịch chuyển tàu ngay lập tức đến đầu đường hầm
		transform.position = teleportTarget.position;

		// 2. Đặt trạng thái để tàu chạy về
		currentState = TrainState.Returning;
	}
}