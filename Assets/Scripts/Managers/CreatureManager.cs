using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CreatureManager
{
	public static CreatureManager Manager;

	[SerializeField] private GameObject entityPrefab;
	[SerializeField] private Sprite entitySprite;
	[SerializeField] private int numberOfPreparedEntities = 100;
	[SerializeField] private int numberOfLivingEntities = 0;

	private Queue<Creature> entityList = new Queue<Creature>();

	public Sprite GetEntitySprite() => entitySprite;
	
	public void Initialize(Transform parent)
	{
		Manager = this;
		for (int i = 0; i < numberOfPreparedEntities; i++)
		{
			entityList.Enqueue(CreateEntity(parent));
		}
	}

	private Creature CreateEntity(Transform parent)
	{
		GameObject go = GameObject.Instantiate(entityPrefab, parent);
		go.SetActive(false);
		return go.GetComponent<Creature>();
	}

	public void FreeEntity(Creature creature)
	{
		creature.gameObject.SetActive(false);
		entityList.Enqueue(creature);
		numberOfLivingEntities--;
		InterfaceManager.manager.SetPopulationNumberText(numberOfLivingEntities);
	}

	public Creature GetEntity(Transform parent)
	{
		Creature creature;
		if (entityList.Count == 0)
		{
			creature = CreateEntity(parent);
			entityList.Enqueue(creature);
		}
		else
		{
			creature = entityList.Dequeue();
		}
		creature.gameObject.SetActive(true);
		numberOfLivingEntities++;
		InterfaceManager.manager.SetPopulationNumberText(numberOfLivingEntities);

		return creature;
	}

	public void Dispose()
	{
		entityList.Clear();
	}
}
