using UnityEngine;

public class AudioAnomaly : BaseAnomaly
{
	// Kéo component AudioSource của radio vào đây
	public AudioSource radioAudioSource;

	public override void ActivateAnomaly()
	{
		// Khi kích hoạt, bật nhạc
		if (radioAudioSource != null)
		{
			radioAudioSource.Play();
		}
	}

	public override void DeactivateAnomaly()
	{
		// Khi reset, tắt nhạc
		if (radioAudioSource != null)
		{
			radioAudioSource.Stop();
		}
	}
}