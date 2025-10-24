using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
	[Header("UI Elements")]
	public TMP_Text nameText;
	public Image masterClientIcon;
	public Image readyIcon;
	// 🔥 REMOVED: public Image backgroundImage; - KHÔNG DÙNG BACKGROUND NỮA

	[Header("Colors")]
	public Color readyColor = Color.green;

	[HideInInspector]
	public string originalPlayerName = "";

	private bool isMasterClient = false;
	private bool isReady = false;

	void Start()
	{
		// 🔥 REMOVED: SetupBackground() - KHÔNG SETUP BACKGROUND NỮA

		// Hide icons by default
		if (masterClientIcon != null)
		{
			masterClientIcon.gameObject.SetActive(false);
		}

		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
		}

		Debug.Log("🎮 PlayerItem initialized - icons hidden, no background");
	}

	// 🔥 REMOVED: SetupBackground() method - KHÔNG CẦN NỮA

	public void SetName(string playerName)
	{
		Debug.Log($"🏷️ Setting player name to: {playerName}");

		// 🔥 LUU TÊN GỐC
		originalPlayerName = playerName;

		// 🔥 FIX PARENT LAYOUT (same as RoomItem)
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

		// 🔥 FIX NAME TEXT (same style as RoomItem)
		if (nameText != null)
		{
			nameText.text = playerName;

			RectTransform nameRect = nameText.GetComponent<RectTransform>();
			if (nameRect != null)
			{
				nameRect.anchorMin = new Vector2(0f, 0f);       // Left side
				nameRect.anchorMax = new Vector2(0.7f, 1f);     // 70% width
				nameRect.pivot = new Vector2(0f, 0.5f);         // Left-center pivot
				nameRect.anchoredPosition = new Vector2(10f, 0f); // 10px from left
				nameRect.sizeDelta = new Vector2(0f, 0f);       // Use anchors for size
			}

			// 🔥 FORCE TEXT STYLE (same as RoomItem)
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

		// 🔥 REMOVED: Background color updates - KHÔNG DÙNG BACKGROUND

		// 🔥 SHOW/HIDE MASTER CLIENT ICON - FIX LOGIC
		if (masterClientIcon != null)
		{
			Debug.Log($"🔍 Master icon GameObject before: Active={masterClientIcon.gameObject.activeInHierarchy}, SelfActive={masterClientIcon.gameObject.activeSelf}");

			masterClientIcon.gameObject.SetActive(isMaster);

			Debug.Log($"🔍 Master icon GameObject after: Active={masterClientIcon.gameObject.activeInHierarchy}, SelfActive={masterClientIcon.gameObject.activeSelf}");

			if (isMaster)
			{
				// 🔥 FORCE ICON VISIBLE AND POSITION
				masterClientIcon.gameObject.SetActive(true); // Double-check active

				// Position master client icon (right side)
				RectTransform iconRect = masterClientIcon.GetComponent<RectTransform>();
				if (iconRect != null)
				{
					iconRect.anchorMin = new Vector2(0.7f, 0.2f);   // Right side
					iconRect.anchorMax = new Vector2(0.85f, 0.8f);  // 15% width
					iconRect.pivot = new Vector2(0.5f, 0.5f);       // Center pivot
					iconRect.anchoredPosition = new Vector2(0f, 0f);
					iconRect.sizeDelta = new Vector2(0f, 0f);       // Use anchors

					Debug.Log($"📐 Master icon positioned: {iconRect.anchoredPosition}, size: {iconRect.sizeDelta}");
				}

				// 🔥 FORCE ICON COLOR AND PROPERTIES
				masterClientIcon.color = Color.red; // Red crown icon
				masterClientIcon.raycastTarget = false; // Don't block clicks

				// 🔥 MAKE SURE ICON IS REALLY VISIBLE
				masterClientIcon.enabled = true;

				Debug.Log($"👑 Master client icon FORCED VISIBLE for: {originalPlayerName}");
				Debug.Log($"    Color: {masterClientIcon.color}, Enabled: {masterClientIcon.enabled}");
			}
			else
			{
				Debug.Log($"👤 Master client icon HIDDEN for: {originalPlayerName}");
			}
		}
		else
		{
			Debug.LogError($"❌ masterClientIcon is NULL for player: {originalPlayerName}");
		}

		// 🔥 UPDATE NAME TEXT FOR MASTER
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

		// 🔥 FORCE CANVAS UPDATE TO REFRESH VISUALS
		Canvas.ForceUpdateCanvases();

		// 🔥 DEBUG FINAL STATE
		if (masterClientIcon != null)
		{
			Debug.Log($"🔍 FINAL Master icon state: Active={masterClientIcon.gameObject.activeInHierarchy}, Color={masterClientIcon.color}, Enabled={masterClientIcon.enabled}");
		}
	}

	public void SetReady(bool ready)
	{
		this.isReady = ready;
		Debug.Log($"✅ SetReady: {ready} for player: {originalPlayerName}");

		// 🔥 SHOW/HIDE READY ICON
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(ready);

			if (ready)
			{
				// Position ready icon (far right)
				RectTransform readyRect = readyIcon.GetComponent<RectTransform>();
				if (readyRect != null)
				{
					readyRect.anchorMin = new Vector2(0.85f, 0.2f);  // Far right
					readyRect.anchorMax = new Vector2(0.95f, 0.8f);  // 10% width
					readyRect.pivot = new Vector2(0.5f, 0.5f);
					readyRect.anchoredPosition = new Vector2(0f, 0f);
					readyRect.sizeDelta = new Vector2(0f, 0f);
				}

				readyIcon.color = readyColor; // Green checkmark
				readyIcon.enabled = true;
				readyIcon.raycastTarget = false;

				Debug.Log($"✅ Ready icon SHOWN for: {originalPlayerName}");
			}
			else
			{
				Debug.Log($"❌ Ready icon HIDDEN for: {originalPlayerName}");
			}
		}

		// 🔥 REMOVED: Background color changes - KHÔNG DÙNG BACKGROUND
	}

	// 🔥 RESET ICONS METHOD
	public void ResetIcons()
	{
		Debug.Log($"🔄 Resetting icons for: {originalPlayerName}");

		if (masterClientIcon != null)
		{
			masterClientIcon.gameObject.SetActive(false);
			Debug.Log($"   Master icon reset to inactive");
		}

		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(false);
			Debug.Log($"   Ready icon reset to inactive");
		}

		// 🔥 REMOVED: Background reset - KHÔNG DÙNG BACKGROUND

		// Reset text style
		if (nameText != null)
		{
			nameText.color = Color.black;
			nameText.fontStyle = FontStyles.Normal;
		}

		isReady = false;
		isMasterClient = false;

		Debug.Log($"🔄 Icons and styling reset completed for: {originalPlayerName}");
	}
}