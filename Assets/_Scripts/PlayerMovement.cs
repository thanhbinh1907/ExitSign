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

			// Tắt CharacterController
			controller.enabled = false;

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

	// Hàm xử lý di chuyển bằng bàn phím (không thay đổi)
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

		Vector3 move = transform.right * x + transform.forward * z;
		float currentSpeed = isRunning ? runSpeed : walkSpeed;
		controller.Move(move.normalized * currentSpeed * Time.deltaTime);

		playerVelocity.y += gravity * Time.deltaTime;
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