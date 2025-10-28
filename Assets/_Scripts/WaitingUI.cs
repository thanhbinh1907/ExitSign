using UnityEngine;
using TMPro;

public class WaitingUI : MonoBehaviour
{
	[Header("UI Components")]
	public TextMeshProUGUI waitingText;
	public TextMeshProUGUI playerCountText; // Hiển thị "2/4 players on train"

	[Header("Animation")]
	public float dotSpeed = 0.5f; // Tốc độ animation dots

	private string baseText = "Waiting for all players";
	private float timer = 0f;

	void Update()
	{
		if (waitingText != null)
		{
			// Tạo hiệu ứng dots animation
			timer += Time.deltaTime;
			int dotCount = Mathf.FloorToInt(timer / dotSpeed) % 4; // 0-3 dots

			string dots = "";
			for (int i = 0; i < dotCount; i++)
			{
				dots += ".";
			}

			waitingText.text = baseText + dots;
		}
	}

	// Hàm để cập nhật số lượng players
	public void UpdatePlayerCount(int playersOnTrain, int totalPlayers)
	{
		if (playerCountText != null)
		{
			playerCountText.text = $"{playersOnTrain}/{totalPlayers} players on train";
		}
	}
}