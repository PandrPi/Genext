using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
	public float Size = 3;
	[SerializeField] private float Energy = 20;
	[SerializeField] private float RegrowthTimer = 0.0f;
	[SerializeField] private bool isDead;

	private Transform myTransform;
	private SpriteRenderer sr;
	private CircleCollider2D circleCollider;

	private static float InitialEnergy = 500.0f;
	private static float ParameterRandomRange = 0.1f;
	private static float TimeToRegrowth = 30.0f;
	private static int FramesToSkip = 2;

	private void Start()
	{
		myTransform = transform;
		sr = GetComponent<SpriteRenderer>();
		circleCollider = GetComponent<CircleCollider2D>();
		Regrowth();
		Size = sr.bounds.size.x;
	}

	private void FixedUpdate()
	{
		if(Time.frameCount % FramesToSkip == 0)
		{
			if (!isDead)
			{
				if (Energy <= 0)
				{
					isDead = true;
					sr.enabled = false;
					circleCollider.enabled = false;
				}
			}
			else
			{
				RegrowthTimer += Time.fixedDeltaTime * FramesToSkip;
				if (RegrowthTimer >= TimeToRegrowth)
				{
					Regrowth();
				}
			}
		}
	}

	private void Regrowth()
	{
		myTransform.position = FoodManager.manager.GetRandomWorldPosition();
		Energy = InitialEnergy + MathHelper.RandomValue(InitialEnergy * ParameterRandomRange);
		RegrowthTimer = 0;
		isDead = false;
		sr.enabled = true;
		circleCollider.enabled = true;
	}

	public void SetFoodSpriteColor(Color color)
	{
		sr.color = color;
	}


	/// <summary>
	/// Use to determine the amount of eaten energy 
	/// </summary>
	/// <param name="energyPerBite">How many energy needs to subtract</param>
	/// <returns></returns>
	public float EatMe(float energyPerBite)
	{
		if (Energy <= 0)
		{
			Energy = 0;
			return 0;
		}
		if(Energy < energyPerBite)
		{
			float temp = Energy;
			Energy = 0;
			return temp;
		}
		else
		{
			Energy -= energyPerBite;
			return energyPerBite;
		}
	}
}
