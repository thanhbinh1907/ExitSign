using UnityEngine;

// Gắn script này lên trigger zone ở cuối game (khu vực màu trắng)
// Đảm bảo nó có Box Collider với "Is Trigger" = true
public class EndGameTrigger : MonoBehaviour
{
	// Chúng ta sẽ dùng Tag để nhận diện trigger này
	public const string EndGameTriggerTag = "EndGameTrigger";

	void Start()
	{
		// Tự động gán Tag nếu bạn quên
		if (!gameObject.CompareTag(EndGameTriggerTag))
		{
			gameObject.tag = EndGameTriggerTag;
			Debug.LogWarning($"EndGameTrigger: Đã tự động set tag thành '{EndGameTriggerTag}'. Bạn nên set tag này trong Inspector!");
		}
	}
}