using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class RoomItem : MonoBehaviour
{
	[Header("UI References")]
	public TMP_Text roomNameText;
	public TMP_Text playersText;
	public Image privateIcon;
	public Button joinButton;

	private NetworkManager networkManager;
	private RoomInfo roomInfo;

	public void Setup(string roomName, int playerCount, int maxPlayers, bool isPrivate, NetworkManager manager, RoomInfo info)
	{
		networkManager = manager;
		roomInfo = info;

		Debug.Log($"🏠 Setting up RoomItem: '{roomName}' ({playerCount}/{maxPlayers})");

		// 🔥 DEBUG VISIBILITY ISSUES
		Debug.Log($"🔍 === DEBUGGING VISIBILITY ===");

		// Check GameObject active state
		Debug.Log($"GameObject active: {gameObject.activeInHierarchy}");
		Debug.Log($"GameObject activeSelf: {gameObject.activeSelf}");

		// Check CanvasGroup
		CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup != null)
		{
			Debug.Log($"CanvasGroup found - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}");
			canvasGroup.alpha = 1f; // Force visible
			canvasGroup.interactable = true;
		}

		// Check background Image alpha
		Image backgroundImage = GetComponent<Image>();
		if (backgroundImage != null)
		{
			Debug.Log($"Background Image - Alpha: {backgroundImage.color.a}, Color: {backgroundImage.color}");
			// Force visible background
			Color bgColor = backgroundImage.color;
			bgColor.a = 1f;
			backgroundImage.color = bgColor;
		}
		else
		{
			Debug.LogWarning("⚠️ No background Image found on RoomItem root");
		}

		// Update and check text colors
		if (roomNameText != null)
		{
			roomNameText.text = roomName;
			Debug.Log($"Room name text: '{roomNameText.text}' - Color: {roomNameText.color}, Alpha: {roomNameText.color.a}");

			// Force visible text color
			Color textColor = roomNameText.color;
			textColor.a = 1f;
			if (textColor.r + textColor.g + textColor.b < 0.1f) // If too dark
			{
				textColor = Color.black;
			}
			roomNameText.color = textColor;
		}
		else
		{
			Debug.LogError("❌ roomNameText is NULL!");
		}

		if (playersText != null)
		{
			playersText.text = $"{playerCount}/{maxPlayers}";
			Debug.Log($"Players text: '{playersText.text}' - Color: {playersText.color}, Alpha: {playersText.color.a}");

			// Force visible text color
			Color textColor = playersText.color;
			textColor.a = 1f;
			if (textColor.r + textColor.g + textColor.b < 0.1f)
			{
				textColor = Color.black;
			}
			playersText.color = textColor;
		}
		else
		{
			Debug.LogError("❌ playersText is NULL!");
		}

		// Check button
		if (joinButton != null)
		{
			joinButton.onClick.RemoveAllListeners();
			joinButton.onClick.AddListener(OnJoinClicked);

			// Check button image
			Image buttonImage = joinButton.GetComponent<Image>();
			if (buttonImage != null)
			{
				Debug.Log($"Button Image - Alpha: {buttonImage.color.a}, Color: {buttonImage.color}");
				Color btnColor = buttonImage.color;
				btnColor.a = 1f;
				buttonImage.color = btnColor;
			}

			// Check button text
			TMP_Text buttonText = joinButton.GetComponentInChildren<TMP_Text>();
			if (buttonText != null)
			{
				Debug.Log($"Button Text: '{buttonText.text}' - Color: {buttonText.color}");
				Color btnTextColor = buttonText.color;
				btnTextColor.a = 1f;
				if (btnTextColor.r + btnTextColor.g + btnTextColor.b < 0.1f)
				{
					btnTextColor = Color.white;
				}
				buttonText.color = btnTextColor;
			}
		}

		// Check RectTransform size
		RectTransform rectTransform = GetComponent<RectTransform>();
		if (rectTransform != null)
		{
			Debug.Log($"RectTransform - Size: {rectTransform.sizeDelta}, Scale: {rectTransform.localScale}");

			// Force proper size if too small
			if (rectTransform.sizeDelta.y < 10)
			{
				Debug.LogWarning("⚠️ RoomItem height too small, forcing to 80px");
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 80);
			}

			// Force proper scale
			rectTransform.localScale = Vector3.one;
		}

		Debug.Log($"🎉 RoomItem setup complete for: {roomName}");
	}

	public void OnJoinClicked()
	{
		Debug.Log($"🔘 Join button clicked for room: {roomInfo?.Name ?? "Unknown"}");

		if (networkManager != null && roomInfo != null)
		{
			networkManager.RequestJoinRoom(roomInfo);
		}
		else
		{
			Debug.LogError($"❌ Cannot join room - NetworkManager: {networkManager != null}, RoomInfo: {roomInfo != null}");
		}
	}
}