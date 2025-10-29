using UnityEngine;

public class OnOffAnomaly : BaseAnomaly
{
    public override void ActivateAnomaly()
    {
        gameObject.SetActive(true);
	}

	public override void DeactivateAnomaly()
    {
        gameObject.SetActive(false);
	}
}
