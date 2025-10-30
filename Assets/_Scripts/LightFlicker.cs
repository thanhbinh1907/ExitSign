using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(AudioSource))]
public class LightFlicker : MonoBehaviour
{
	[Header("Thời gian BẬT")]
	public float minOnTime = 5.0f;
	public float maxOnTime = 15.0f;

	[Header("Thời gian TẮT (chớp)")]
	public float minOffTime = 0.05f;
	public float maxOffTime = 0.2f;

	[Header("Âm thanh")]
	public AudioClip flickerSound; // Kéo file âm thanh chớp đèn vào đây

	private Light _light;
	private AudioSource _audioSource;

	void Start()
	{
		_light = GetComponent<Light>();
		_audioSource = GetComponent<AudioSource>();

		if (_audioSource != null)
		{
			_audioSource.playOnAwake = false;

			// THAY ĐỔI QUAN TRỌNG:
			// Đặt clip và cho phép lặp lại (loop)
			// đề phòng trường hợp file âm thanh ngắn hơn thời gian chớp
			_audioSource.clip = flickerSound;
			_audioSource.loop = true;
		}

		StartCoroutine(FlickerRoutine());
	}

	private IEnumerator FlickerRoutine()
	{
		while (true)
		{
			// 1. Trạng thái BẬT
			_light.enabled = true;

			// Đợi một khoảng thời gian BẬT ngẫu nhiên
			float onWait = Random.Range(minOnTime, maxOnTime);
			yield return new WaitForSeconds(onWait);

			// --- HÀNH ĐỘNG CHỚP BẮT ĐẦU ---

			// 2. Tắt đèn
			_light.enabled = false;

			// 3. Phát âm thanh (chỉ phát khi đèn tắt)
			if (_audioSource != null)
			{
				_audioSource.Play();
			}

			// 4. Đợi một khoảng thời gian TẮT ngẫu nhiên
			float offWait = Random.Range(minOffTime, maxOffTime);
			yield return new WaitForSeconds(offWait);

			// 5. Dừng âm thanh (NGAY TRƯỚC KHI BẬT LẠI ĐÈN)
			if (_audioSource != null)
			{
				_audioSource.Stop();
			}

			// --- Vòng lặp quay lại Bước 1 (Bật đèn) ---
		}
	}
}