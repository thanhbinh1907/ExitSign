using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(AudioSource))] // <-- THÊM DÒNG NÀY
public class LightFlicker : MonoBehaviour
{
	[Header("Thời gian BẬT")]
	public float minOnTime = 5.0f;
	public float maxOnTime = 15.0f;

	[Header("Thời gian TẮT (chớp)")]
	public float minOffTime = 0.05f;
	public float maxOffTime = 0.2f;

	[Header("Âm thanh")] // <-- THÊM PHẦN NÀY
	public AudioClip flickerSound;

	private Light _light;
	private AudioSource _audioSource; // <-- THÊM DÒNG NÀY

	void Start()
	{
		_light = GetComponent<Light>();
		_audioSource = GetComponent<AudioSource>(); // <-- THÊM DÒNG NÀY
		_audioSource.playOnAwake = false; // Đảm bảo nó không tự phát
	}

	private IEnumerator FlickerRoutine()
	{
		while (true)
		{
			// 1. Bật đèn
			_light.enabled = true;
			float onWait = Random.Range(minOnTime, maxOnTime);
			yield return new WaitForSeconds(onWait);

			// 2. Tắt đèn
			_light.enabled = false;

			// 3. PHÁT ÂM THANH KHI TẮT
			if (flickerSound != null)
			{
				_audioSource.PlayOneShot(flickerSound); // <-- THÊM DÒNG NÀY
			}

			// 4. Đợi
			float offWait = Random.Range(minOffTime, maxOffTime);
			yield return new WaitForSeconds(offWait);
		}
	}
}