using UnityEngine;
using Photon.Pun; // 1. Thêm thư viện Photon

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviourPun // 2. Kế thừa từ MonoBehaviourPun
{
	[Header("Player Settings")]
	public float walkSpeed = 5.0f;
	public float runSpeed = 10.0f;
	public float gravity = -9.81f;

	[Header("Camera Settings")]
	public Transform playerCamera; // Gán camera của người chơi vào đây
	public float mouseSensitivity = 100.0f;

	[Header("Animation")]
	public Animator animator; // Gán Animator component vào đây
	public Transform playerModel;

	private CharacterController controller;
	private Vector3 playerVelocity;
	private float xRotation = 0f;

	private bool isWalking = false;
	private bool isRunning = false;

	// Thêm biến để theo dõi trạng thái trên tàu
	private bool isOnTrain = false;
	private Vector3 lastTrainPosition;
	private Vector3 trainVelocity;

	[Header("UI")]
	public StationDisplay stationDisplay;

	// NEW: Tham chiếu tới transform của tàu 
	private Transform trainTransformRef = null;

	// --- CODE KIỂM TRA THẮNG THUA ---
	[Header("Game State")]
	private GameUIManager gameUIManager; // Tham chiếu đến UI của player này
	private StationAnomalyManager[] allStationManagers;
	private bool hasFinishedGame = false; // Đảm bảo chỉ trigger 1 lần

	void Start()
	{
		controller = GetComponent<CharacterController>();

		// --- LOGIC TÌM UI VÀ TRẠM ---
		if (photonView.IsMine)
		{
			// TÌM UI CỦA CHÍNH MÌNH (vì nó là con của prefab này)
			gameUIManager = GetComponentInChildren<GameUIManager>();
			if (gameUIManager == null)
			{
				Debug.LogError("PlayerMovement: Không tìm thấy GameUIManager trong Children!");
			}

			// Tìm tất cả các trạm (chỉ cần tìm 1 lần)
			allStationManagers = FindObjectsOfType<StationAnomalyManager>();
		}
		// --- KẾT THÚC LOGIC TÌM UI ---

		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}

		// Tải độ nhạy đã lưu
		mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100.0f);

		// 3. Gộp logic từ cả hai file vào đây
		if (photonView.IsMine)
		{
			// Nếu là nhân vật CỦA TÔI:
			// Khóa con trỏ chuột
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			// --- PHẦN CODE ẨN MODEL (từ PlayerSetup) ---
			int localPlayerLayer = LayerMask.NameToLayer("LocalPlayerBody");
			if (playerCamera != null)
			{
				playerCamera.GetComponent<Camera>().cullingMask &= ~(1 << localPlayerLayer);
			}
			if (playerModel != null)
			{
				SetLayerRecursively(playerModel.gameObject, localPlayerLayer);
			}
		}
		else
		{
			// Nếu là nhân vật CỦA NGƯỜI KHÁC:
			// Tắt camera
			playerCamera.gameObject.SetActive(false);

			// Tắt CharacterController cho remote players
			if (controller != null)
			{
				controller.enabled = false;
			}

			// Tắt AudioListener
			AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
			if (audioListener != null)
			{
				audioListener.enabled = false;
			}
		}
	}

	void Update()
	{
		// Nếu game đang tạm dừng (pause), không làm gì cả
		if (Time.timeScale == 0f)
		{
			return;
		}

		// Nếu game đã kết thúc (biến hasFinishedGame được set bởi RPC)
		// thì không cho di chuyển nữa
		if (hasFinishedGame)
		{
			return;
		}

		// Tính toán train velocity nếu đang ở trên tàu và có reference tới tàu
		if (isOnTrain && trainTransformRef != null)
		{
			Vector3 currentTrainPosition = trainTransformRef.position;
			trainVelocity = (currentTrainPosition - lastTrainPosition) / Time.deltaTime;
			lastTrainPosition = currentTrainPosition;
		}

		// 4. DÒNG CODE QUAN TRỌNG NHẤT
		// Chỉ chạy code di chuyển, xoay camera, và cập nhật animation
		// NẾU ĐÂY LÀ NHÂN VẬT CỦA TÔI
		if (photonView.IsMine)
		{
			HandleMovement();
			HandleMouseLook();
			UpdateAnimations();
		}
	}

	// --- LOGIC VA CHẠM VÀ KẾT THÚC GAME ---

	// CharacterController dùng hàm này thay vì OnTriggerEnter
	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		// Chỉ chạy nếu là player của tôi VÀ game chưa kết thúc
		if (hasFinishedGame || !photonView.IsMine)
		{
			return;
		}

		// Kiểm tra xem có va chạm với trigger cuối game không (bằng Tag)
		if (hit.gameObject.CompareTag(EndGameTrigger.EndGameTriggerTag))
		{
			Debug.Log("Local Player chạm vào EndGameTrigger. Gửi RPC tới tất cả...");

			// 1. Đánh dấu *chính mình* đã xong để không gửi RPC 2 lần
			hasFinishedGame = true;

			// 2. Kiểm tra điều kiện thắng/thua (chỉ chạy ở máy local)
			bool didWin = CheckGameEndCondition_GetResult();

			// 3. Gửi RPC cho TẤT CẢ MỌI NGƯỜI (kể cả mình) để hiển thị màn hình
			photonView.RPC("Rpc_EndGameSync", RpcTarget.All, didWin);
		}
	}

	// Hàm này chỉ kiểm tra và trả về kết quả
	private bool CheckGameEndCondition_GetResult()
	{
		if (allStationManagers == null || allStationManagers.Length == 0)
		{
			Debug.LogWarning("Không tìm thấy StationAnomalyManager nào, mặc định là THẮNG.");
			return true; // Thắng
		}

		foreach (StationAnomalyManager manager in allStationManagers)
		{
			if (!manager.isStationNormal)
			{
				// TÌM THẤY MỘT ANOMALY CÒN SÓT LẠI!
				Debug.Log($"THUA: Tìm thấy anomaly tại {manager.gameObject.name}!");
				return false; // Thua
			}
		}

		// Nếu vòng lặp chạy hết, nghĩa là tất cả đều bình thường
		Debug.Log("THẮNG: Tất cả anomaly đã được xử lý!");
		return true; // Thắng
	}

	// HÀM RPC ĐỂ ĐỒNG BỘ KẾT THÚC GAME
	// Hàm này sẽ chạy trên máy của TẤT CẢ người chơi
	[PunRPC]
	public void Rpc_EndGameSync(bool didWin)
	{
		// 1. Đánh dấu game đã kết thúc cho tất cả mọi người
		// (để ngăn người khác cũng gửi RPC nếu họ về đích sau 0.1s)
		hasFinishedGame = true;

		// 2. Tìm GameUIManager của player (trên máy của họ)
		if (gameUIManager == null)
		{
			gameUIManager = GetComponentInChildren<GameUIManager>();
		}

		if (gameUIManager == null)
		{
			Debug.LogError($"RPC_EndGameSync: Không tìm thấy GameUIManager cho player {photonView.Owner.NickName}");
			return;
		}

		// 3. Hiển thị màn hình tương ứng
		if (didWin)
		{
			Debug.Log($"RPC: Hiển thị WIN screen cho {photonView.Owner.NickName}");
			gameUIManager.ShowWinScreen();
		}
		else
		{
			Debug.Log($"RPC: Hiển thị LOSE screen cho {photonView.Owner.NickName}");
			gameUIManager.ShowLoseScreen();
		}
	}

	// --- CÁC HÀM CƠ BẢN ---

	// Hàm public để set trạng thái trên tàu (giữ để tương thích)
	public void SetOnTrain(bool onTrain)
	{
		// Nếu không có trainTransformRef thì chỉ bật trạng thái onTrain mà không có tham chiếu tàu
		isOnTrain = onTrain;

		if (onTrain)
		{
			// Nếu không có train reference, dùng vị trí hiện tại làm lastTrainPosition
			if (trainTransformRef == null)
			{
				lastTrainPosition = transform.position;
				trainVelocity = Vector3.zero;
			}
		}
		else
		{
			// Clear reference khi rời tàu
			trainTransformRef = null;
		}

		Debug.Log($"Player {name} trạng thái trên tàu: {onTrain}");
	}

	// NEW: Hàm để nhận transform của tàu (không parent player)
	public void SetTrainTransform(Transform train, bool onTrain)
	{
		isOnTrain = onTrain;
		trainTransformRef = onTrain ? train : null;

		if (onTrain && trainTransformRef != null)
		{
			lastTrainPosition = trainTransformRef.position;
			trainVelocity = Vector3.zero;
		}
		else if (!onTrain)
		{
			trainTransformRef = null;
		}

		Debug.Log($"Player {name} SetTrainTransform called. OnTrain={onTrain}, TrainRef={(trainTransformRef != null ? trainTransformRef.name : "null")}");
	}

	// Hàm xử lý di chuyển bằng bàn phím (có điều chỉnh cho train movement)
	void HandleMovement()
	{
		if (controller.isGrounded && playerVelocity.y < 0)
		{
			playerVelocity.y = -2f;
		}

		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		bool isMoving = (x != 0 || z != 0);
		bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);

		if (isMoving)
		{
			if (isShiftHeld)
			{
				isRunning = true;
				isWalking = false;
			}
			else
			{
				isWalking = true;
				isRunning = false;
			}
		}
		else
		{
			isWalking = false;
			isRunning = false;
		}

		// Di chuyển thông thường
		Vector3 move = transform.right * x + transform.forward * z;
		float currentSpeed = isRunning ? runSpeed : walkSpeed;

		// QUAN TRỌNG: Nếu đang ở trên tàu, thêm train velocity
		if (isOnTrain && trainTransformRef != null)
		{
			// Thêm chuyển động của tàu vào movement của player
			Vector3 trainMovement = trainVelocity * Time.deltaTime;
			controller.Move(move.normalized * currentSpeed * Time.deltaTime + trainMovement);
		}
		else
		{
			// Di chuyển bình thường khi không ở trên tàu
			controller.Move(move.normalized * currentSpeed * Time.deltaTime);
		}

		// Áp dụng gravity
		if (isOnTrain)
		{
			// Giảm gravity khi ở trên tàu để tránh rơi xuống
			playerVelocity.y += (gravity * 0.3f) * Time.deltaTime;
		}
		else
		{
			// Gravity bình thường
			playerVelocity.y += gravity * Time.deltaTime;
		}

		controller.Move(playerVelocity * Time.deltaTime);
	}

	// Hàm cập nhật animations (không thay đổi)
	void UpdateAnimations()
	{
		if (animator != null)
		{
			animator.SetBool("isWalking", isWalking);
			animator.SetBool("isRunning", isRunning);
		}
	}

	// Hàm xử lý xoay camera bằng chuột (không thay đổi)
	void HandleMouseLook()
	{
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);

		playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
		transform.Rotate(Vector3.up * mouseX);
	}

	void SetLayerRecursively(GameObject obj, int newLayer)
	{
		if (obj == null) return;
		obj.layer = newLayer;
		foreach (Transform child in obj.transform)
		{
			if (child == null) continue;
			SetLayerRecursively(child.gameObject, newLayer);
		}
	}

	// --- CÁC HÀM TIỆN ÍCH KHÁC ---

	// Hàm này dùng để SubwayController kiểm tra
	public bool IsOnTrain()
	{
		return isOnTrain;
	}

	// Hàm này dùng để SubwayController kiểm tra
	public Transform GetTrainTransformRef()
	{
		return trainTransformRef;
	}

	// Hàm này dịch chuyển người chơi bằng CharacterController
	public void TeleportPlayer(Vector3 offset)
	{
		// Chỉ client của chính mình mới có quyền di chuyển controller
		if (photonView.IsMine)
		{
			Debug.Log($"TeleportPlayer được gọi. Di chuyển bằng offset: {offset}");

			// Tắt CharacterController để dịch chuyển
			if (controller != null)
			{
				controller.enabled = false;
			}

			// Di chuyển transform
			transform.position += offset;

			// Bật lại CharacterController
			if (controller != null)
			{
				controller.enabled = true;
			}

			// CẬP NHẬT QUAN TRỌNG:
			// Đặt lại lastTrainPosition về vị trí MỚI của tàu
			// để frame sau không bị tính velocity sai
			if (isOnTrain && trainTransformRef != null)
			{
				lastTrainPosition = trainTransformRef.position;
				trainVelocity = Vector3.zero; // Reset vận tốc
			}
		}
	}
	[PunRPC]
	public void ShowStationUI(int stationNumber)
	{
		// Hàm này được gọi trên TẤT CẢ các client
		// Nhưng chúng ta chỉ muốn player "của mình" (local) hiển thị UI
		if (photonView.IsMine && stationDisplay != null)
		{
			Debug.Log($"RPC: Hiển thị Station {stationNumber} cho local player.");
			stationDisplay.ShowStation(stationNumber);
		}
	}

	// HÀM MỚI ĐỂ PAUSEMANAGER GỌI
	public void UpdateSensitivity(float newSensitivity)
	{
		mouseSensitivity = newSensitivity;
	}
}