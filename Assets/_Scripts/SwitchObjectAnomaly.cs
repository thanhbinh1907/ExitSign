using UnityEngine;

public class SwitchObjectAnomaly : BaseAnomaly
{
    public GameObject objectA;
    public GameObject objectB;

	public override void ActivateAnomaly()
	{
		objectA.SetActive(false);
		objectB.SetActive(true);
	}

	public override void DeactivateAnomaly()
	{
		objectA.SetActive(true);
		objectB.SetActive(false);
	}
}
