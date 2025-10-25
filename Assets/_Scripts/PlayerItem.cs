using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
	[Header("UI Elements")]
	public TMP_Text nameText;
	// 🔥 REMOVED: public Image masterClientIcon; - KHÔNG DÙNG MASTER ICON NỮA
	public Image readyIcon;

	[Header("Colors")]
	public Color readyColor = Color.green;

	[HideInInspector]
	public string originalPlayerName = "";

	private bool isMasterClient = false;
	private bool isReady = false;

	void Start()
	{
		// Hide ready icon by default
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
		}

		Debug.Log("🎮 PlayerItem initialized - ready icon hidden, no background, no master icon");
	}

	public void SetName(string playerName)
	{
		Debug.Log($"🏷️ Setting player name to: {playerName}");

		// 🔥 LUU TÊN GỐC
		originalPlayerName = playerName;

		// 🔥 FIX PARENT LAYOUT
		RectTransform rectTransform = GetComponent<RectTransform>();
		if (rectTransform != null)
		{
			rectTransform.anchorMin = new Vector2(0f, 1f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.pivot = new Vector2(0.5f, 1f);
			rectTransform.offsetMin = new Vector2(5f, -60f);  // 5px margin, 60px height
			rectTransform.offsetMax = new Vector2(-5f, 0f);
			rectTransform.localScale = Vector3.one;
		}

		// 🔥 FIX NAME TEXT - NOW HAS MORE SPACE (up to 85% width)
		if (nameText != null)
		{
			nameText.text = playerName;

			RectTransform nameRect = nameText.GetComponent<RectTransform>();
			if (nameRect != null)
			{
				nameRect.anchorMin = new Vector2(0f, 0f);       // Left side
				nameRect.anchorMax = new Vector2(0.85f, 1f);    // 85% width (more space since no master icon)
				nameRect.pivot = new Vector2(0f, 0.5f);         // Left-center pivot
				nameRect.anchoredPosition = new Vector2(10f, 0f); // 10px from left
				nameRect.sizeDelta = new Vector2(0f, 0f);       // Use anchors for size
			}

			// 🔥 FORCE TEXT STYLE
			nameText.fontSize = 16f;
			nameText.color = Color.black;
			nameText.alignment = TextAlignmentOptions.Left;
			nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
			nameText.fontStyle = FontStyles.Normal;
		}
		else
		{
			Debug.LogError("❌ nameText is null in PlayerItem!");
		}

		// Force all children active
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			child.gameObject.SetActive(true);
		}

		// Force canvas update
		Canvas.ForceUpdateCanvases();

		Debug.Log($"✅ Player name set: '{playerName}', stored as originalPlayerName: '{originalPlayerName}'");
	}

	public void SetAsMasterClient(bool isMaster)
	{
		this.isMasterClient = isMaster;
		Debug.Log($"👑 SetAsMasterClient: {isMaster} for player: {originalPlayerName}");

		// 🔥 NO MASTER ICON - ONLY UPDATE NAME TEXT
		if (nameText != null)
		{
			if (isMaster)
			{
				if (!nameText.text.Contains("(Chủ phòng)"))
				{
					nameText.text = originalPlayerName + " (Chủ phòng)";
				}
				nameText.color = Color.red; // Red text for master
				nameText.fontStyle = FontStyles.Bold; // Bold for master
			}
			else
			{
				nameText.text = originalPlayerName; // 🔥 RESET VỀ TÊN GỐC
				nameText.color = Color.black; // Normal color
				nameText.fontStyle = FontStyles.Normal; // Normal style
			}

			Debug.Log($"🏷️ Name text updated to: '{nameText.text}' (original: '{originalPlayerName}')");
		}

		// Force canvas update
		Canvas.ForceUpdateCanvases();
	}

	public void SetReady(bool ready)
	{
		this.isReady = ready;
		Debug.Log($"✅ SetReady: {ready} for player: {originalPlayerName}");

		// 🔥 SHOW/HIDE READY ICON - NOW AT FAR RIGHT (90% position)
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(ready);

			if (ready)
			{
				// 🔥 READY ICON NOW HAS MORE SPACE - POSITION AT 90%
				RectTransform readyRect = readyIcon.GetComponent<RectTransform>();
				if (readyRect != null)
				{
					readyRect.anchorMin = new Vector2(0.9f, 0.5f);     // Single point at 90% width, center height
					readyRect.anchorMax = new Vector2(0.9f, 0.5f);     // Same point - no stretch!
					readyRect.pivot = new Vector2(0.5f, 0.5f);         // Center pivot
					readyRect.anchoredPosition = new Vector2(0f, 0f);  // No offset from anchor
					readyRect.sizeDelta = new Vector2(24f, 24f);       // Slightly bigger: 24x24px

					Debug.Log($"✅ Ready icon positioned: Size={readyRect.sizeDelta}, Anchor={readyRect.anchorMin}");
				}

				// 🔥 FORCE ICON PROPERTIES
				readyIcon.color = readyColor; // Green checkmark
				readyIcon.enabled = true;
				readyIcon.raycastTarget = false;

				// 🔥 ENSURE ICON ASPECT RATIO
				if (readyIcon.sprite != null)
				{
					readyIcon.preserveAspect = true; // Keep original aspect ratio
				}

				Debug.Log($"✅ Ready icon SHOWN for: {originalPlayerName}");
			}
			else
			{
				Debug.Log($"❌ Ready icon HIDDEN for: {originalPlayerName}");
			}
		}
	}

	// 🔥 RESET ICONS METHOD - SIMPLIFIED
	public void ResetIcons()
	{
		Debug.Log($"🔄 Resetting icons for: {originalPlayerName}");

		// 🔥 ONLY RESET READY ICON (no master icon anymore)
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
			Debug.Log($"   Ready icon reset to inactive");
		}

		// Reset text style
		if (nameText != null)
		{
			nameText.color = Color.black;
			nameText.fontStyle = FontStyles.Normal;
		}

		isReady = false;
		isMasterClient = false;

		Debug.Log($"🔄 Icons reset completed for: {originalPlayerName}");
	}
}