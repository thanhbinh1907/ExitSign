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

		// 🔥 FIX PARENT LAYOUT FIRST
		RectTransform rectTransform = GetComponent<RectTransform>();
		if (rectTransform != null)
		{
			rectTransform.anchorMin = new Vector2(0f, 1f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.pivot = new Vector2(0.5f, 1f);
			rectTransform.offsetMin = new Vector2(10f, -80f);
			rectTransform.offsetMax = new Vector2(-10f, 0f);
			rectTransform.localScale = Vector3.one;
		}

		// 🔥 FIX CHILD ELEMENTS POSITIONING
		// Fix RoomNameText
		if (roomNameText != null)
		{
			roomNameText.text = roomName;

			RectTransform nameRect = roomNameText.GetComponent<RectTransform>();
			if (nameRect != null)
			{
				nameRect.anchorMin = new Vector2(0f, 0.5f);     // Left-center
				nameRect.anchorMax = new Vector2(0.6f, 0.5f);   // 60% width
				nameRect.pivot = new Vector2(0f, 0.5f);         // Left-center pivot
				nameRect.anchoredPosition = new Vector2(10f, 0f); // 10px from left
				nameRect.sizeDelta = new Vector2(0f, 30f);      // Auto width, 30px height
			}

			// Force text settings
			roomNameText.fontSize = 16f;
			roomNameText.color = Color.black;
			roomNameText.alignment = TextAlignmentOptions.Left;

			Debug.Log($"✅ Room name set: '{roomNameText.text}'");
		}

		// Fix PlayersText  
		if (playersText != null)
		{
			playersText.text = $"{playerCount}/{maxPlayers}";

			RectTransform playersRect = playersText.GetComponent<RectTransform>();
			if (playersRect != null)
			{
				playersRect.anchorMin = new Vector2(0.6f, 0.5f);   // Right side
				playersRect.anchorMax = new Vector2(0.8f, 0.5f);   // 20% width
				playersRect.pivot = new Vector2(0.5f, 0.5f);       // Center pivot
				playersRect.anchoredPosition = new Vector2(0f, 0f);
				playersRect.sizeDelta = new Vector2(0f, 30f);
			}

			playersText.fontSize = 14f;
			playersText.color = Color.blue;
			playersText.alignment = TextAlignmentOptions.Center;

			Debug.Log($"✅ Players text set: '{playersText.text}'");
		}

		// Fix Join Button
		if (joinButton != null)
		{
			joinButton.onClick.RemoveAllListeners();
			joinButton.onClick.AddListener(OnJoinClicked);

			RectTransform buttonRect = joinButton.GetComponent<RectTransform>();
			if (buttonRect != null)
			{
				buttonRect.anchorMin = new Vector2(0.8f, 0.2f);    // Right side, bottom 20%
				buttonRect.anchorMax = new Vector2(0.95f, 0.8f);   // 15% width, 60% height
				buttonRect.pivot = new Vector2(0.5f, 0.5f);        // Center pivot
				buttonRect.anchoredPosition = new Vector2(0f, 0f);
				buttonRect.sizeDelta = new Vector2(0f, 0f);        // Use anchors for size
			}

			// Make sure button is visible
			Image buttonImage = joinButton.GetComponent<Image>();
			if (buttonImage != null)
			{
				buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f); // Blue button
			}

			TMP_Text buttonText = joinButton.GetComponentInChildren<TMP_Text>();
			if (buttonText != null)
			{
				buttonText.text = "Join";
				buttonText.color = Color.white;
				buttonText.fontSize = 12f;
			}

			Debug.Log($"✅ Join button setup complete");
		}

		// Fix Private Icon
		if (privateIcon != null)
		{
			privateIcon.gameObject.SetActive(isPrivate);

			if (isPrivate)
			{
				RectTransform iconRect = privateIcon.GetComponent<RectTransform>();
				if (iconRect != null)
				{
					iconRect.anchorMin = new Vector2(0.5f, 0.5f);
					iconRect.anchorMax = new Vector2(0.6f, 0.5f);
					iconRect.pivot = new Vector2(0.5f, 0.5f);
					iconRect.anchoredPosition = new Vector2(0f, 0f);
					iconRect.sizeDelta = new Vector2(20f, 20f);    // 20x20px icon
				}
			}

			Debug.Log($"✅ Private icon: {(isPrivate ? "shown" : "hidden")}");
		}

		// 🔥 DISABLE LAYOUT GROUP ON ROOT (if exists) to prevent overriding
		LayoutGroup rootLayout = GetComponent<LayoutGroup>();
		if (rootLayout != null)
		{
			Debug.LogWarning("⚠️ Found LayoutGroup on RoomItem root - this may interfere with positioning!");
			// Optionally disable: rootLayout.enabled = false;
		}

		// Force all children to be active
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			child.gameObject.SetActive(true);
		}

		// Force canvas update
		Canvas.ForceUpdateCanvases();

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