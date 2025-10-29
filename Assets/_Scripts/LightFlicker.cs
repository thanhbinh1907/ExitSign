using UnityEngine;
using System.Collections; // Cần thiết cho Coroutine

// Tự động yêu cầu một component Light (Spot Light, Point Light, v.v.)
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
	[Header("Thời gian BẬT")]
	[Tooltip("Thời gian BẬT tối thiểu trước khi chớp")]
	public float minOnTime = 5.0f;
	[Tooltip("Thời gian BẬT tối đa trước khi chớp")]
	public float maxOnTime = 15.0f;

	[Header("Thời gian TẮT (chớp)")]
	[Tooltip("Thời gian TẮT (chớp) tối thiểu")]
	public float minOffTime = 0.05f;
	[Tooltip("Thời gian TẮT (chớp) tối đa")]
	public float maxOffTime = 0.2f;

	private Light _light;

	void Start()
	{
		// Lấy component Light (Spot Light) được gắn cùng
		_light = GetComponent<Light>();

		// Bắt đầu vòng lặp chớp đèn
		StartCoroutine(FlickerRoutine());
	}

	private IEnumerator FlickerRoutine()
	{
		// Vòng lặp này chạy mãi mãi
		while (true)
		{
			// 1. Bật đèn (trạng thái bình thường)
			_light.enabled = true;

			// 2. Đợi một khoảng thời gian ngẫu nhiên
			float onWait = Random.Range(minOnTime, maxOnTime);
			yield return new WaitForSeconds(onWait);

			// 3. Tắt đèn (chớp)
			_light.enabled = false;

			// 4. Đợi một khoảng thời gian TẮT ngẫu nhiên (rất ngắn)
			float offWait = Random.Range(minOffTime, maxOffTime);
			yield return new WaitForSeconds(offWait);

			// Vòng lặp bắt đầu lại...
		}
	}
}