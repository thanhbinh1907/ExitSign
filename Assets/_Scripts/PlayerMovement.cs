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
		// Tính toán train velocity nếu đang ở trên tàu
		if (isOnTrain && transform.parent != null)
		{
			Vector3 currentTrainPosition = transform.parent.position;
			Vector3 delta = currentTrainPosition - lastTrainPosition; // Tính khoảng cách tàu di chuyển

			// KIỂM TRA TELEPORT: 
			// Nếu tàu di chuyển hơn 20 mét TRONG 1 FRAME (delta.sqrMagnitude > 20*20)
			// -> Đây chắc chắn là một cú teleport, không phải di chuyển thường.
			if (delta.sqrMagnitude > 400f && photonView.IsMine && controller.enabled)
			{
				// ---- BƯỚC XỬ LÝ TELEPORT ----
				// 1. Tạm thời TẮT CharacterController
				controller.enabled = false;

				// 2. Di chuyển transform của Player đi một khoảng bằng đúng delta
				transform.position += delta;

				// 3. BẬT lại CharacterController
				controller.enabled = true;

				// 4. Đặt vận tốc tàu bằng 0 cho frame này
				// (vì chúng ta đã tự di chuyển player rồi)
				trainVelocity = Vector3.zero;
			}
			else
			{
				// ---- XỬ LÝ DI CHUYỂN THƯỜNG (mượt) ----
				// Nếu Time.deltaTime rất nhỏ hoặc bằng 0, không chia
				if (Time.deltaTime > float.Epsilon)
				{
					trainVelocity = delta / Time.deltaTime;
				}
				else
				{
					trainVelocity = Vector3.zero;
				}
			}

			// Luôn cập nhật vị trí cuối cùng
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

	// Hàm public để set trạng thái trên tàu
	public void SetOnTrain(bool onTrain)
	{
		isOnTrain = onTrain;

		if (onTrain && transform.parent != null)
		{
			lastTrainPosition = transform.parent.position;
			trainVelocity = Vector3.zero;
		}

		Debug.Log($"Player {name} trạng thái trên tàu: {onTrain}");
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
		if (isOnTrain && transform.parent != null)
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

}