using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class CreatureManager
{
	public static CreatureManager Manager;

	[SerializeField] private GameObject creaturePrefab;
	[SerializeField] private Sprite creatureSprite;
	[SerializeField] private int numberOfPreparedCreatures = 100;
	[SerializeField] private int numberOfLivingCreatures = 0;

	private Queue<Creature> creaturesPool = new Queue<Creature>();
	private HashSet<Creature> activeCreatures = new HashSet<Creature>();

	public Sprite GetCreatureSprite() => creatureSprite;
	
	public void Initialize(Transform parent)
	{
		Manager = this;
		for (int i = 0; i < numberOfPreparedCreatures; i++)
		{
			creaturesPool.Enqueue(CreateCreature(parent));
		}
	}
	
	/// <summary>
	/// Creates a new Creature instance. This method should not be called frequently because of the GC allocations. 
	/// </summary>
	/// <param name="parent">Parent transform ofbject</param>
	/// <returns></returns>
	private Creature CreateCreature(Transform parent)
	{
		GameObject go = Object.Instantiate(creaturePrefab, parent);
		go.SetActive(false);
		return go.GetComponent<Creature>();
	}
	
	/// <summary>
	/// Prepares the dead creature object for the further usage inside the ObjectPooling algorithm.
	/// </summary>
	/// <param name="creature"></param>
	public void FreeCreature(Creature creature)
	{
		creature.gameObject.SetActive(false);
		creaturesPool.Enqueue(creature);
		numberOfLivingCreatures--;
		UIManager.Manager.SetPopulationNumberText(numberOfLivingCreatures);
		activeCreatures.Remove(creature);

		if (numberOfLivingCreatures != 0) return;
		creaturePrefab.GetComponent<Creature>().Reproduce();
	}
	
	/// <summary>
	/// Returns the previously prepared Creature instance if it is available. If there is no available Creature
	/// instances returns the created instance.
	/// </summary>
	/// <param name="parent">Transform parent object</param>
	/// <returns></returns>
	public Creature GetCreature(Transform parent)
	{
		Creature creature;
		if (creaturesPool.Count == 0)
		{
			creature = CreateCreature(parent);
			creaturesPool.Enqueue(creature);
		}
		else
		{
			creature = creaturesPool.Dequeue();
		}

		activeCreatures.Add(creature);
		creature.gameObject.SetActive(true);
		numberOfLivingCreatures++;
		UIManager.Manager.SetPopulationNumberText(numberOfLivingCreatures);

		return creature;
	}

	public void UpdateManager()
	{
		foreach (Creature creature in activeCreatures.ToList())
		{
			creature.UpdateCreature();
		}
	}

	public void Dispose()
	{
		creaturesPool.Clear();
		activeCreatures.Clear();
	}
}
