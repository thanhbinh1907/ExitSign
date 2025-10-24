using UnityEngine;
using Photon.Pun;

// Yêu cầu phải có CharacterController để tắt/mở
[RequireComponent(typeof(CharacterController))]
public class PlayerSetup : MonoBehaviourPun
{
	[Header("Network Setup References")]
	public Transform playerCamera;  // Camera của người chơi
	public Transform playerModel;   // Object chứa model 3D

	private CharacterController controller;

	void Start()
	{
		controller = GetComponent<CharacterController>();

		if (photonView.IsMine)
		{
			// Nếu là nhân vật CỦA TÔI:
			// Khóa con trỏ chuột
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			// --- PHẦN CODE ẨN MODEL ---
			// 1. Lấy ID của layer "LocalPlayerBody"
			int localPlayerLayer = LayerMask.NameToLayer("LocalPlayerBody");

			// 2. Bảo camera BỎ QUA (không vẽ) layer đó
			if (playerCamera != null)
			{
				playerCamera.GetComponent<Camera>().cullingMask &= ~(1 << localPlayerLayer);
			}

			// 3. Đặt model của CHÍNH MÌNH vào layer đó
			if (playerModel != null)
			{
				SetLayerRecursively(playerModel.gameObject, localPlayerLayer);
			}
		}
		else
		{
			// Nếu là nhân vật CỦA NGƯỜI KHÁC:
			// Tắt camera đi
			if (playerCamera != null)	
			{
				playerCamera.gameObject.SetActive(false);
			}

			// Tắt CharacterController đi 
			if (controller != null)
			{
				controller.enabled = false;
			}

			// Tắt AudioListener (nếu có trên camera)
			if (playerCamera != null)
			{
				AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
				if (audioListener != null)
				{
					audioListener.enabled = false;
				}
			}
		}
	}

	// Hàm này dùng để đổi layer của một object và tất cả con của nó
	void SetLayerRecursively(GameObject obj, int newLayer)
	{
		if (obj == null)
		{
			return;
		}

		obj.layer = newLayer;

		foreach (Transform child in obj.transform)
		{
			if (child == null)
			{
				continue;
			}
			SetLayerRecursively(child.gameObject, newLayer);
		}
	}
}