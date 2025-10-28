using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
	[Header("UI Components")]
	public TextMeshProUGUI promptText;
	public CanvasGroup canvasGroup;

	[Header("Animation Settings")]
	public float fadeSpeed = 3f;
	public bool pulseEffect = true;

	private bool isVisible = false;
	private float targetAlpha = 0f;

	void Start()
	{
		if (canvasGroup == null)
			canvasGroup = GetComponent<CanvasGroup>();

		if (promptText == null)
			promptText = GetComponentInChildren<TextMeshProUGUI>();

		// Bắt đầu với alpha = 0
		canvasGroup.alpha = 0f;
	}

	void Update()
	{
		// Smooth fade in/out
		canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

		// Pulse effect khi hiển thị
		if (isVisible && pulseEffect && promptText != null)
		{
			float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f; // 0-1 pulse
			promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, 0.7f + pulse * 0.3f);
		}
	}

	public void ShowPrompt()
	{
		isVisible = true;
		targetAlpha = 1f;
		gameObject.SetActive(true);
	}

	public void HidePrompt()
	{
		isVisible = false;
		targetAlpha = 0f;

		// Tắt GameObject sau khi fade out xong
		Invoke("DisableGameObject", 1f / fadeSpeed);
	}

	void DisableGameObject()
	{
		if (!isVisible && canvasGroup.alpha <= 0.01f)
		{
			gameObject.SetActive(false);
		}
	}
}