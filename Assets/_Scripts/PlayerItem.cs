using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
	[Header("UI Elements")]
	public TMP_Text nameText;
	public Image readyIcon;
	public Button kickButton; // Kick button reference

	[Header("Colors")]
	public Color readyColor = Color.green;

	[HideInInspector]
	public string originalPlayerName = "";

	private bool isMasterClient = false;
	private bool isReady = false;
	private bool canShowKickButton = false;

	void Start()
	{
		Debug.Log("🎮 PlayerItem Start() called");

		// Initialize ready icon
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
			Debug.Log("   Ready icon initialized and hidden");
		}

		// Initialize kick button
		InitializeKickButton();

		Debug.Log("✅ PlayerItem initialized successfully");
	}

	void InitializeKickButton()
	{
		// Create kick button if not exists in prefab
		if (kickButton == null)
		{
			Debug.Log("🔧 Creating kick button programmatically");
			CreateKickButtonProgrammatically();
		}
		else
		{
			Debug.Log("🔧 Setting up existing kick button from prefab");
			SetupExistingKickButton();
		}
	}

	void CreateKickButtonProgrammatically()
	{
		// Create kick button GameObject
		GameObject kickButtonObj = new GameObject("KickButton");
		kickButtonObj.transform.SetParent(transform, false);

		// Add Image component with proper sprite
		Image kickImage = kickButtonObj.AddComponent<Image>();
		kickImage.color = new Color(0.9f, 0.3f, 0.3f, 1f); // Bright red background

		// Try to get default UI sprite
		Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
		if (defaultSprite != null)
		{
			kickImage.sprite = defaultSprite;
		}
		kickImage.type = Image.Type.Sliced; // Better scaling
		kickImage.raycastTarget = true;

		// Add Button component
		Button kickBtn = kickButtonObj.AddComponent<Button>();

		// Setup button colors
		ColorBlock colors = kickBtn.colors;
		colors.normalColor = Color.white;
		colors.highlightedColor = new Color(1f, 0.8f, 0.8f, 1f); // Light red on hover
		colors.pressedColor = new Color(0.8f, 0.6f, 0.6f, 1f); // Darker red when pressed
		colors.selectedColor = Color.white;
		colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		colors.colorMultiplier = 1f;
		colors.fadeDuration = 0.1f;
		kickBtn.colors = colors;

		// Add click listener
		kickBtn.onClick.AddListener(OnKickButtonClicked);

		// Setup RectTransform positioning
		RectTransform kickRect = kickButtonObj.GetComponent<RectTransform>();
		kickRect.anchorMin = new Vector2(0.85f, 0.5f);
		kickRect.anchorMax = new Vector2(0.85f, 0.5f);
		kickRect.pivot = new Vector2(0.5f, 0.5f);
		kickRect.anchoredPosition = Vector2.zero;
		kickRect.sizeDelta = new Vector2(36f, 36f); // 36x36px for better visibility

		// Create text child for "X" symbol
		CreateKickButtonText(kickButtonObj);

		// Assign reference
		kickButton = kickBtn;

		// Initially hidden
		kickButtonObj.SetActive(false);

		Debug.Log("✅ Kick button created programmatically");
	}

	void CreateKickButtonText(GameObject kickButtonObj)
	{
		GameObject textObj = new GameObject("Text");
		textObj.transform.SetParent(kickButtonObj.transform, false);

		TMP_Text kickText = textObj.AddComponent<TMP_Text>();
		kickText.text = "✕";
		kickText.color = Color.white;
		kickText.fontSize = 18f;
		kickText.fontStyle = FontStyles.Bold;
		kickText.alignment = TextAlignmentOptions.Center;
		kickText.verticalAlignment = VerticalAlignmentOptions.Middle;
		kickText.raycastTarget = false; // Don't block button clicks

		// Setup text RectTransform to fill button
		RectTransform textRect = textObj.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.anchoredPosition = Vector2.zero;
		textRect.sizeDelta = Vector2.zero;
		textRect.offsetMin = Vector2.zero;
		textRect.offsetMax = Vector2.zero;

		Debug.Log("✅ Kick button text created");
	}

	void SetupExistingKickButton()
	{
		if (kickButton == null) return;

		// Clear existing listeners and add our handler
		kickButton.onClick.RemoveAllListeners();
		kickButton.onClick.AddListener(OnKickButtonClicked);

		// Ensure proper setup
		kickButton.gameObject.SetActive(false);
		kickButton.interactable = true;

		// Force proper colors
		Image kickImage = kickButton.GetComponent<Image>();
		if (kickImage != null)
		{
			kickImage.color = new Color(0.9f, 0.3f, 0.3f, 1f);
			kickImage.raycastTarget = true;
		}

		// Setup text if exists
		TMP_Text kickText = kickButton.GetComponentInChildren<TMP_Text>();
		if (kickText != null)
		{
			kickText.text = "✕";
			kickText.color = Color.white;
			kickText.fontSize = 18f;
			kickText.fontStyle = FontStyles.Bold;
			kickText.raycastTarget = false;
		}

		Debug.Log("✅ Existing kick button setup complete");
	}

	public void SetName(string playerName)
	{
		Debug.Log($"🏷️ Setting player name to: '{playerName}'");

		// Store original name for reference
		originalPlayerName = playerName;

		// Setup parent PlayerItem layout
		SetupPlayerItemLayout();

		// Setup name text with proper positioning
		SetupNameText(playerName);

		// Preserve kick button state during name setup
		PreserveKickButtonState();

		Canvas.ForceUpdateCanvases();

		Debug.Log($"✅ Player name set: '{playerName}' (original: '{originalPlayerName}')");
	}

	void SetupPlayerItemLayout()
	{
		RectTransform rectTransform = GetComponent<RectTransform>();
		if (rectTransform != null)
		{
			rectTransform.anchorMin = new Vector2(0f, 1f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.pivot = new Vector2(0.5f, 1f);
			rectTransform.offsetMin = new Vector2(5f, -65f);  // 5px margin, 65px height
			rectTransform.offsetMax = new Vector2(-5f, 0f);
			rectTransform.localScale = Vector3.one;
		}
	}

	void SetupNameText(string playerName)
	{
		if (nameText != null)
		{
			nameText.text = playerName;

			RectTransform nameRect = nameText.GetComponent<RectTransform>();
			if (nameRect != null)
			{
				// Give name text 60% width to make room for ready icon and kick button
				nameRect.anchorMin = new Vector2(0f, 0f);
				nameRect.anchorMax = new Vector2(0.6f, 1f);
				nameRect.pivot = new Vector2(0f, 0.5f);
				nameRect.anchoredPosition = new Vector2(10f, 0f);
				nameRect.sizeDelta = new Vector2(0f, 0f);
			}

			// Apply text styling
			nameText.fontSize = 16f;
			nameText.color = Color.black;
			nameText.alignment = TextAlignmentOptions.Left;
			nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
			nameText.fontStyle = FontStyles.Normal;
			nameText.raycastTarget = false;
		}
		else
		{
			Debug.LogError("❌ nameText is null in PlayerItem!");
		}
	}

	void PreserveKickButtonState()
	{
		bool wasKickButtonActive = kickButton != null && kickButton.gameObject.activeSelf;
		bool wasKickButtonVisible = canShowKickButton;

		// Activate all children except kick button (preserve its state)
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (kickButton != null && child.gameObject != kickButton.gameObject)
			{
				child.gameObject.SetActive(true);
			}
		}

		// Restore kick button state and re-apply visibility if needed
		if (kickButton != null && wasKickButtonActive && wasKickButtonVisible)
		{
			kickButton.gameObject.SetActive(true);
			ForceKickButtonVisible();
		}
	}

	public void SetAsMasterClient(bool isMaster)
	{
		this.isMasterClient = isMaster;
		Debug.Log($"👑 SetAsMasterClient: {isMaster} for player: '{originalPlayerName}'");

		if (nameText != null)
		{
			if (isMaster)
			{
				// Add master client indicator to name
				if (!nameText.text.Contains("(Chủ phòng)"))
				{
					nameText.text = originalPlayerName + " (Chủ phòng)";
				}
				nameText.color = Color.red;
				nameText.fontStyle = FontStyles.Bold;
			}
			else
			{
				// Reset to original name
				nameText.text = originalPlayerName;
				nameText.color = Color.black;
				nameText.fontStyle = FontStyles.Normal;
			}

			Debug.Log($"🏷️ Name text updated to: '{nameText.text}'");
		}

		Canvas.ForceUpdateCanvases();
	}

	public void SetReady(bool ready)
	{
		this.isReady = ready;
		Debug.Log($"✅ SetReady: {ready} for player: '{originalPlayerName}'");

		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(ready);

			if (ready)
			{
				// Position ready icon at 75% width
				RectTransform readyRect = readyIcon.GetComponent<RectTransform>();
				if (readyRect != null)
				{
					readyRect.anchorMin = new Vector2(0.75f, 0.5f);
					readyRect.anchorMax = new Vector2(0.75f, 0.5f);
					readyRect.pivot = new Vector2(0.5f, 0.5f);
					readyRect.anchoredPosition = Vector2.zero;
					readyRect.sizeDelta = new Vector2(28f, 28f);

					Debug.Log($"✅ Ready icon positioned at 75% width");
				}

				// Apply ready icon properties
				readyIcon.color = readyColor;
				readyIcon.enabled = true;
				readyIcon.raycastTarget = false;

				if (readyIcon.sprite != null)
				{
					readyIcon.preserveAspect = true;
				}

				Debug.Log($"✅ Ready icon SHOWN for: '{originalPlayerName}'");
			}
			else
			{
				Debug.Log($"❌ Ready icon HIDDEN for: '{originalPlayerName}'");
			}
		}
	}

	public void SetKickButtonVisible(bool visible, bool viewerIsMasterClient)
	{
		// Determine if kick button should be shown
		// Show if: viewer is master, this player is not master, not showing kick for self
		canShowKickButton = visible &&
						   viewerIsMasterClient &&
						   !this.isMasterClient &&
						   !string.IsNullOrEmpty(originalPlayerName);

		Debug.Log($"🦵 SetKickButtonVisible for '{originalPlayerName}':");
		Debug.Log($"   visible={visible}, viewerIsMaster={viewerIsMasterClient}");
		Debug.Log($"   thisIsMaster={this.isMasterClient}, canShow={canShowKickButton}");

		if (kickButton != null)
		{
			kickButton.gameObject.SetActive(canShowKickButton);

			if (canShowKickButton)
			{
				ForceKickButtonVisible();
				Debug.Log($"🦵 Kick button SHOWN for: '{originalPlayerName}'");
			}
			else
			{
				Debug.Log($"❌ Kick button HIDDEN for: '{originalPlayerName}'");
			}
		}
		else
		{
			Debug.LogWarning($"⚠️ Kick button reference is null for: '{originalPlayerName}'");
		}
	}

	void ForceKickButtonVisible()
	{
		if (kickButton == null) return;

		Debug.Log($"🔧 Forcing kick button visible for: '{originalPlayerName}'");

		// 1. Position kick button at 90% width (far right)
		RectTransform kickRect = kickButton.GetComponent<RectTransform>();
		if (kickRect != null)
		{
			kickRect.anchorMin = new Vector2(0.9f, 0.5f);
			kickRect.anchorMax = new Vector2(0.9f, 0.5f);
			kickRect.pivot = new Vector2(0.5f, 0.5f);
			kickRect.anchoredPosition = Vector2.zero;
			kickRect.sizeDelta = new Vector2(36f, 36f);

			// Bring to front
			kickRect.SetAsLastSibling();

			Debug.Log($"🔧 Kick button positioned: Size={kickRect.sizeDelta}, Anchor={kickRect.anchorMin}");
		}

		// 2. Ensure button component is properly configured
		Button buttonComponent = kickButton.GetComponent<Button>();
		if (buttonComponent != null)
		{
			buttonComponent.interactable = true;
			buttonComponent.enabled = true;

			// Force proper button colors
			ColorBlock colors = buttonComponent.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1f, 0.8f, 0.8f, 1f);
			colors.pressedColor = new Color(0.8f, 0.6f, 0.6f, 1f);
			colors.colorMultiplier = 1f;
			buttonComponent.colors = colors;
		}

		// 3. Setup image with solid visible color
		Image kickButtonImage = kickButton.GetComponent<Image>();
		if (kickButtonImage != null)
		{
			kickButtonImage.enabled = true;
			kickButtonImage.color = new Color(0.9f, 0.3f, 0.3f, 1f); // Bright red, full alpha
			kickButtonImage.raycastTarget = true;

			// Ensure sprite exists
			if (kickButtonImage.sprite == null)
			{
				kickButtonImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
			}

			Debug.Log($"🔧 Image configured: Color={kickButtonImage.color}, HasSprite={kickButtonImage.sprite != null}");
		}

		// 4. Setup text (X symbol)
		TMP_Text kickButtonText = kickButton.GetComponentInChildren<TMP_Text>();
		if (kickButtonText != null)
		{
			kickButtonText.text = "✕";
			kickButtonText.color = Color.white;
			kickButtonText.fontSize = 18f;
			kickButtonText.fontStyle = FontStyles.Bold;
			kickButtonText.enabled = true;
			kickButtonText.raycastTarget = false; // Don't block button clicks

			// Center text in button
			RectTransform textRect = kickButtonText.GetComponent<RectTransform>();
			if (textRect != null)
			{
				textRect.anchorMin = Vector2.zero;
				textRect.anchorMax = Vector2.one;
				textRect.anchoredPosition = Vector2.zero;	
				textRect.sizeDelta = Vector2.zero;
				textRect.offsetMin = Vector2.zero;
				textRect.offsetMax = Vector2.zero;
			}

			Debug.Log($"🔧 Text configured: '{kickButtonText.text}', Color={kickButtonText.color}");
		}

		// 5. Clear any canvas group blocking
		CanvasGroup canvasGroup = kickButton.GetComponent<CanvasGroup>();
		if (canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
		}

		// 6. Force immediate visual update
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

		Debug.Log($"✅ Kick button forced visible and interactive for: '{originalPlayerName}'");
	}

	void OnKickButtonClicked()
	{
		Debug.Log($"🦵 Kick button clicked for player: '{originalPlayerName}'");

		if (string.IsNullOrEmpty(originalPlayerName))
		{
			Debug.LogError("❌ Cannot kick: originalPlayerName is null or empty!");
			return;
		}

		// Find RoomPanelManager and request kick
		RoomPanelManager roomPanelManager = FindFirstObjectByType<RoomPanelManager>();
		if (roomPanelManager != null)
		{
			roomPanelManager.RequestKickPlayer(originalPlayerName);
			Debug.Log($"✅ Kick request sent for: '{originalPlayerName}'");
		}
		else
		{
			Debug.LogError("❌ Could not find RoomPanelManager to request kick!");
		}
	}

	public void ResetIcons()
	{
		Debug.Log($"🔄 Resetting icons for: '{originalPlayerName}'");

		// Reset ready icon
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
		}

		// Reset kick button
		if (kickButton != null)
		{
			kickButton.gameObject.SetActive(false);
		}

		// Reset text styling
		if (nameText != null)
		{
			nameText.color = Color.black;
			nameText.fontStyle = FontStyles.Normal;
		}

		// Reset internal states
		isReady = false;
		isMasterClient = false;
		canShowKickButton = false;

		Debug.Log($"✅ Icons and states reset for: '{originalPlayerName}'");
	}

	// Debug helper method
	public void DebugKickButtonState()
	{
		if (kickButton == null)
		{
			Debug.Log($"🔍 DEBUG '{originalPlayerName}': Kick button is NULL");
			return;
		}

		Debug.Log($"🔍 DEBUG Kick Button State for '{originalPlayerName}':");
		Debug.Log($"   GameObject.activeInHierarchy: {kickButton.gameObject.activeInHierarchy}");
		Debug.Log($"   GameObject.activeSelf: {kickButton.gameObject.activeSelf}");
		Debug.Log($"   Button.interactable: {kickButton.interactable}");
		Debug.Log($"   Button.enabled: {kickButton.enabled}");

		Image img = kickButton.GetComponent<Image>();
		if (img != null)
		{
			Debug.Log($"   Image.color: {img.color}");
			Debug.Log($"   Image.enabled: {img.enabled}");
			Debug.Log($"   Image.sprite: {(img.sprite != null ? img.sprite.name : "NULL")}");
		}

		RectTransform rect = kickButton.GetComponent<RectTransform>();
		if (rect != null)
		{
			Debug.Log($"   Position: {rect.anchoredPosition}");
			Debug.Log($"   Size: {rect.sizeDelta}");
			Debug.Log($"   Anchors: Min={rect.anchorMin}, Max={rect.anchorMax}");
		}
	}

	void OnValidate()
	{
		// Editor-time validation
		if (Application.isPlaying) return;

		if (nameText == null)
		{
			Debug.LogWarning($"⚠️ PlayerItem '{gameObject.name}': nameText is not assigned!");
		}

		if (readyIcon == null)
		{
			Debug.LogWarning($"⚠️ PlayerItem '{gameObject.name}': readyIcon is not assigned!");
		}
	}
}