using UnityEngine;

public class SimpleMagicFlower : MagicFlower
{
	[SerializeField] private ParticleSystem waterEffect;

	public override void Water()
	{
		if (waterEffect != null)
		{
			waterEffect.Play();
		}

		Debug.Log($"{flowerName} has been watered!");
	}
}