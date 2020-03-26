using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EntityManager
{
	public static EntityManager manager;

	[SerializeField] private GameObject entityPrefab;
	[SerializeField] private int numberOfPrecreatedEntities = 100;
	[SerializeField] private int numberOfLivingEntities = 0;

	private Queue<Entity> entityList = new Queue<Entity>();

	public void Initialize(Transform parent)
	{
		manager = this;
		for (int i = 0; i < numberOfPrecreatedEntities; i++)
		{
			entityList.Enqueue(CreateEntity(parent));
		}
	}

	private Entity CreateEntity(Transform parent)
	{
		GameObject go = GameObject.Instantiate(entityPrefab, parent);
		go.SetActive(false);
		return go.GetComponent<Entity>();
	}

	public void FreeEntity(Entity entity)
	{
		entity.gameObject.SetActive(false);
		entityList.Enqueue(entity);
		numberOfLivingEntities--;
		InterfaceManager.manager.SetPopulationNumberText(numberOfLivingEntities);
	}

	public Entity GetEntity(Transform parent)
	{
		Entity entity;
		if (entityList.Count == 0)
		{
			entity = CreateEntity(parent);
			entityList.Enqueue(entity);
		}
		else
		{
			entity = entityList.Dequeue();
		}
		entity.gameObject.SetActive(true);
		numberOfLivingEntities++;
		InterfaceManager.manager.SetPopulationNumberText(numberOfLivingEntities);

		return entity;
	}

	public void Dispose()
	{

	}
}
