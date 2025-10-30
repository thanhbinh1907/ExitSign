using UnityEngine;
using TMPro;
using System.Collections;

public class StationDisplay : MonoBehaviour
{
	// Kéo TextMeshPro UI (TMP_Text) vào đây
	public TMP_Text stationText;
	private Coroutine displayCoroutine;

	void Start()
	{
		// Ẩn text khi bắt đầu
		if (stationText != null)
		{
			stationText.gameObject.SetActive(false);
		}
	}

	// Hàm này được gọi từ PlayerMovement
	public void ShowStation(int stationNumber)
	{
		if (stationText == null) return;

		// Dừng coroutine cũ nếu đang chạy
		if (displayCoroutine != null)
		{
			StopCoroutine(displayCoroutine);
		}

		// Bắt đầu coroutine mới
		displayCoroutine = StartCoroutine(ShowStationRoutine(stationNumber));
	}

	private IEnumerator ShowStationRoutine(int stationNumber)
	{
		// Hiển thị text
		stationText.text = "Station " + stationNumber;
		stationText.gameObject.SetActive(true);

		// Đợi 3 giây
		yield return new WaitForSeconds(3f);

		// Ẩn text
		stationText.gameObject.SetActive(false);
		displayCoroutine = null;
	}
}