using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
	[Header("UI Elements")]
	public TMP_Text nameText;
	public Image masterClientIcon; // Icon hiện khi là chủ phòng
	public Image readyIcon;        // Icon hiện khi ready (tùy chọn)
	public Image backgroundImage;  // Background để highlight

	[Header("Colors")]
	public Color normalColor = Color.white;
	public Color masterClientColor = Color.yellow;
	public Color readyColor = Color.green;

	private bool isMasterClient = false;
	private bool isReady = false;

	public void SetName(string playerName)
	{
		if (nameText != null)
		{
			nameText.text = playerName;
		}
	}

	public void SetAsMasterClient(bool isMaster)
	{
		this.isMasterClient = isMaster;

		// Hiện/ẩn icon chủ phòng
		if (masterClientIcon != null)
		{
			masterClientIcon.gameObject.SetActive(isMaster);
		}

		// Thay đổi màu background
		if (backgroundImage != null)
		{
			backgroundImage.color = isMaster ? masterClientColor : normalColor;
		}

		// Thêm text (Chủ phòng)
		if (nameText != null && isMaster)
		{
			if (!nameText.text.Contains("(Chủ phòng)"))
			{
				nameText.text += " (Chủ phòng)";
			}
		}
	}

	public void SetReady(bool ready)
	{
		this.isReady = ready;

		// Hiện/ẩn ready icon
		if (readyIcon != null)
		{
			readyIcon.gameObject.SetActive(ready);
		}

		// Cập nhật màu nếu không phải master client
		if (backgroundImage != null && !isMasterClient)
		{
			backgroundImage.color = ready ? readyColor : normalColor;
		}
	}

	public bool GetReadyStatus()
	{
		return isReady;
	}

	public bool IsMasterClient()
	{
		return isMasterClient;
	}
}