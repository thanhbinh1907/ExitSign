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
		// Lưu references
		networkManager = manager;
		roomInfo = info;

		Debug.Log($"🏠 Setting up RoomItem: {roomName} ({playerCount}/{maxPlayers}) Private: {isPrivate}");

		// Update UI với null checks
		if (roomNameText != null)
		{
			roomNameText.text = roomName;
			Debug.Log($"✅ Room name set: {roomNameText.text}");
		}
		else
		{
			Debug.LogError("❌ roomNameText is null in RoomItem!");
		}

		if (playersText != null)
		{
			playersText.text = $"{playerCount}/{maxPlayers}";
			Debug.Log($"✅ Players text set: {playersText.text}");
		}
		else
		{
			Debug.LogError("❌ playersText is null in RoomItem!");
		}

		if (privateIcon != null)
		{
			privateIcon.gameObject.SetActive(isPrivate);
			Debug.Log($"✅ Private icon: {(isPrivate ? "shown" : "hidden")}");
		}
		else
		{
			Debug.LogWarning("⚠️ privateIcon is null");
		}

		if (joinButton != null)
		{
			joinButton.onClick.RemoveAllListeners();
			joinButton.onClick.AddListener(OnJoinClicked);
			Debug.Log($"✅ Join button setup complete");
		}
		else
		{
			Debug.LogError("❌ joinButton is null in RoomItem!");
		}

		// 🔥 THÊM DEBUG POSITION - ĐOẠN NÀY QUAN TRỌNG
		Debug.Log($"🔍 Checking GameObject state and position...");

		RectTransform rectTransform = GetComponent<RectTransform>();
		Debug.Log($"🔍 RectTransform found: {rectTransform != null}");

		if (rectTransform != null)
		{
			Debug.Log($"📐 Position: {rectTransform.anchoredPosition}");
			Debug.Log($"📐 Size: {rectTransform.sizeDelta}");
			Debug.Log($"📐 Scale: {rectTransform.localScale}");
			Debug.Log($"📐 Active in Hierarchy: {gameObject.activeInHierarchy}");
			Debug.Log($"📐 Active Self: {gameObject.activeSelf}");
			Debug.Log($"📐 Parent: {transform.parent?.name ?? "null"}");
			Debug.Log($"📐 Sibling Index: {transform.GetSiblingIndex()}");

			// Check parent hierarchy
			Transform current = transform.parent;
			int level = 0;
			while (current != null && level < 5)
			{
				Debug.Log($"📐 Parent Level {level}: {current.name} - Active: {current.gameObject.activeInHierarchy}");
				current = current.parent;
				level++;
			}

			// Force settings
			rectTransform.localScale = Vector3.one;
			gameObject.SetActive(true);

			Debug.Log($"📐 After force - Active: {gameObject.activeInHierarchy}, Scale: {rectTransform.localScale}");
		}
		else
		{
			Debug.LogError("❌ RectTransform is NULL!");
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