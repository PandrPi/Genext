using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

[System.Serializable]
public class FoodManager
{
	public static FoodManager manager;
	[SerializeField] private GameObject foodPrefab;

	private List<Food> foodList = new List<Food>();
	private Vector2 halfWorldSize;

	public void Initialize(int foodCount, Vector2 halfWorldSize, Transform parent)
	{
		manager = this;
		this.halfWorldSize = halfWorldSize;
		for (int i = 0; i < foodCount; i++)
		{
			Vector2 position = GetRandomWorldPosition();
			foodList.Add(CreateFood(position, parent));
		}
	}

	public Vector2 GetRandomWorldPosition()
	{
		return new Vector2(MathHelper.RandomValue() * (halfWorldSize.x - 1), MathHelper.RandomValue() * (halfWorldSize.y - 1));
	}

	private Food CreateFood(Vector2 position, Transform parent)
	{
		GameObject go = GameObject.Instantiate(foodPrefab, position, Quaternion.identity, parent);
		return go.GetComponent<Food>();
	}

	public void Dispose()
	{

	}
}
