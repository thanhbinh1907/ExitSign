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

	void Start()
	{
		controller = GetComponent<CharacterController>();

		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}

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

		// Nếu không phải "IsMine", thì toàn bộ việc di chuyển
		// và xoay người sẽ do PhotonTransformView lo.
		// Animation sẽ do PhotonAnimatorView lo.
	}

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

	// --- THÊM 3 HÀM SAU ĐÂY VÀO CUỐI FILE ---

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
}