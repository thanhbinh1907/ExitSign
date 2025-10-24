using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class RoomItem : MonoBehaviour
{
	public TMP_Text roomNameText;
	public TMP_Text playersText;
	public Image privateIcon;
	public Button joinButton;

	private NetworkManager lobbyManager;
	private RoomInfo roomInfo;

	public void Setup(string roomName, int currentPlayers, int maxPlayers, bool isPrivate, NetworkManager manager, RoomInfo info)
	{
		roomNameText.text = roomName;
		playersText.text = currentPlayers + " / " + maxPlayers;
		privateIcon.gameObject.SetActive(isPrivate);
		lobbyManager = manager;
		roomInfo = info;
		joinButton.onClick.RemoveAllListeners();
		joinButton.onClick.AddListener(() => {
			lobbyManager.RequestJoinRoom(roomInfo);
		});
	}
}