using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UISetupHelper : MonoBehaviour
{
	[Header("Auto Setup")]
	public bool autoSetupOnStart = true;

	[Header("References")]
	public MainMenuManager mainMenuManager;
	public NetworkManager networkManager;

	void Start()
	{
		if (autoSetupOnStart)
		{
			AutoSetupUI();
		}
	}

	public void AutoSetupUI()
	{
		// Tự động tìm và gán references
		if (mainMenuManager == null)
			mainMenuManager = FindObjectOfType<MainMenuManager>();

		if (networkManager == null)
			networkManager = FindObjectOfType<NetworkManager>();

		// Setup button events
		SetupButtonEvents();

		// Setup toggle events
		SetupToggleEvents();

		Debug.Log("UI Setup completed!");
	}

	void SetupButtonEvents()
	{
		// Tìm và setup Multiplayer button
		Button multiplayerBtn = GameObject.Find("MultiplayerButton")?.GetComponent<Button>();
		if (multiplayerBtn != null && mainMenuManager != null)
		{
			multiplayerBtn.onClick.RemoveAllListeners();
			multiplayerBtn.onClick.AddListener(mainMenuManager.OnMultiplayerClick);
		}

		// Tìm và setup Create Room button trong lobby
		Button createRoomBtn = GameObject.Find("CreateRoomButton")?.GetComponent<Button>();
		if (createRoomBtn != null && networkManager != null)
		{
			createRoomBtn.onClick.RemoveAllListeners();
			createRoomBtn.onClick.AddListener(networkManager.ShowCreateRoomPanel);
		}

		// Setup Back button
		Button backBtn = GameObject.Find("BackButton")?.GetComponent<Button>();
		if (backBtn != null && mainMenuManager != null)
		{
			backBtn.onClick.RemoveAllListeners();
			backBtn.onClick.AddListener(mainMenuManager.BackToMainMenu);
		}
	}

	void SetupToggleEvents()
	{
		// Setup Private toggle
		Toggle privateToggle = GameObject.Find("PrivateToggle")?.GetComponent<Toggle>();
		if (privateToggle != null && networkManager != null)
		{
			privateToggle.onValueChanged.RemoveAllListeners();
			privateToggle.onValueChanged.AddListener(networkManager.OnPrivateToggleChanged);
		}
	}
}