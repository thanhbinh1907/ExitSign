using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
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

	private CharacterController controller;
	private Vector3 playerVelocity;
	private float xRotation = 0f; // Biến lưu góc xoay lên/xuống của camera
	
	// Animation variables
	private bool isWalking = false;
	private bool isRunning = false;

	void Start()
	{
		controller = GetComponent<CharacterController>();

		// Tự động lấy Animator nếu chưa được gán
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}

		// Khóa con trỏ chuột vào giữa màn hình và ẩn nó đi
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update()
	{
		HandleMovement();
		HandleMouseLook();
		UpdateAnimations();
	}

	// Hàm xử lý di chuyển bằng bàn phím
	void HandleMovement()
	{
		// Kiểm tra xem nhân vật có đang đứng trên mặt đất không
		if (controller.isGrounded && playerVelocity.y < 0)
		{
			playerVelocity.y = -2f;
		}

		// Lấy input từ phím W,A,S,D
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		// Kiểm tra xem có di chuyển không
		bool isMoving = (x != 0 || z != 0);

		// Kiểm tra xem có nhấn Shift không (để chạy)
		bool isShiftHeld = Input.GetKey(KeyCode.LeftShift);

		// Cập nhật animation states
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

		// Tạo vector di chuyển dựa trên hướng hiện tại của nhân vật
		Vector3 move = transform.right * x + transform.forward * z;

		// Chọn tốc độ di chuyển dựa trên trạng thái
		float currentSpeed = isRunning ? runSpeed : walkSpeed;

		// Thực hiện di chuyển
		controller.Move(move.normalized * currentSpeed * Time.deltaTime);

		// Áp dụng trọng lực
		playerVelocity.y += gravity * Time.deltaTime;
		controller.Move(playerVelocity * Time.deltaTime);
	}

	// Hàm cập nhật animations
	void UpdateAnimations()
	{
		if (animator != null)
		{
			animator.SetBool("isWalking", isWalking);
			animator.SetBool("isRunning", isRunning);
		}
	}

	// Hàm xử lý xoay camera bằng chuột
	void HandleMouseLook()
	{
		// Lấy input từ chuột
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		// Xoay lên/xuống (Pitch)
		xRotation -= mouseY;
		// Giới hạn góc nhìn, không cho phép lộn ngược camera
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);

		// Áp dụng góc xoay lên/xuống cho CHỈ CAMERA
		playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

		// Xoay trái/phải (Yaw)
		// Áp dụng góc xoay trái/phải cho CẢ NGƯỜI NHÂN VẬT
		transform.Rotate(Vector3.up * mouseX);
	}
}