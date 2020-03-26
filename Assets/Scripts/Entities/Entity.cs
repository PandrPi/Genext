using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
	// Rules:
	// higher movement speed - higher energy loss per step (energy -= speed^2 * energyPerStep)
	// higher size - higher energy loss per step, but higher energyAmountPerBite ()
	// higher viewRadius - higher chance to notice food
	//


	[SerializeField] private float movementSpeed;		// movement speed of creature
	[SerializeField] private float size;				// size of creature
	[SerializeField] private float energy;				// energy of creature, can be stored by eating food
	[SerializeField] private float energyToReproduce;	// how many energy is needed to reproduce
	[SerializeField] private float dieChance;			// chance to die after reproduce 
	[SerializeField] private float viewRadius;			// The distance at which food can be notised
	[SerializeField] List<Food> foodInFieldView = new List<Food>();

	[SerializeField] private Food currentFoodObject;            // stores the food object that the entity is moving to
	[SerializeField] private Vector2 currentRandomDirection;    // stores random moving direction which entity will move along until entity finds food or die
	[SerializeField] private bool isEating;                     // Entity is not moving while it's true
	[SerializeField] private bool isDead;

	private float randomDirectionTimer;     // timer which determine when random movement direction must be changed
	private float reproduceReserveEnergy;	// after reproduce entity will have this enegry to have a chance to find food

	private Transform myTransform;
	private Rigidbody2D body;
	private CircleCollider2D viewCollider;
	private SpriteRenderer sr;

	private static float ChangeRandomMovementDirectionTimer = 10.0f;
	private static float EnergyReserveAfterReproduce = 0.30f;
	private static float EnergyLossPerStep = 0.3f;
	private static float EnergyAmountPerBite = 1.0f;
	private static float MutationRandomRange = 0.15f; // When new entity will be created it will inherit all parent's param with this mutation [-PRR, +PRR] percents 
	private static float LowSpeed = 1.0f;
	private static Color LowSpeedColor = Color.HSVToRGB(0.6028f, 0.45f, 1.0f);
	private static float HighSpeed = 20.0f;
	private static Color HighSpeedColor = Color.HSVToRGB(1.0f, 0.45f, 1.0f);

	public void InitializeComponents()
	{
		myTransform = transform;
		body = GetComponent<Rigidbody2D>();
		viewCollider = GetComponents<CircleCollider2D>()[1];	// the second collider must be trigger, it's used to see food objects
		sr = GetComponent<SpriteRenderer>();

		InitializeEntity();
	}

	private void FixedUpdate()
	{
		if (!isDead)
		{
			if (currentFoodObject == null)
			{
				if (foodInFieldView.Count > 0)
					currentFoodObject = GetNearestFoodObject();

				// if food is not found get random direction and move in that direction
				if (currentFoodObject == null)
				{
					randomDirectionTimer += Time.fixedDeltaTime;
					if(randomDirectionTimer >= ChangeRandomMovementDirectionTimer)
					{
						randomDirectionTimer = 0;
						currentRandomDirection = Vector2.zero;
					}


					if (World.IsInsideEdgeArea(myTransform.position))
					{
						RaycastHit2D hit = Physics2D.Raycast(myTransform.position, currentRandomDirection, size * 2);

						if (hit.transform != null)
						{
							if (hit.transform.CompareTag(World.WorldEdgeTag))
							{
								currentRandomDirection = Vector2.Reflect(currentRandomDirection, hit.normal);
							}
						}
					}

					if (currentRandomDirection == Vector2.zero)
						currentRandomDirection = Random.insideUnitCircle.normalized;

					MoveAlongDirectionOrEat(currentRandomDirection);
				}
			}
			else
			{

				Transform nearestFoodTrs = currentFoodObject.transform;

				Vector2 directionNonNormalized = nearestFoodTrs.position - myTransform.position;
				Vector2 moveDirection = directionNonNormalized.normalized;

				if(directionNonNormalized.magnitude < currentFoodObject.Size)
				{
					isEating = true;
					currentRandomDirection = Vector2.zero;
				}

				MoveAlongDirectionOrEat(moveDirection);
			}
			if (energy <= 0)
			{
				isDead = true;
				EntityManager.manager.FreeEntity(this);
			}
			if(energy >= energyToReproduce + reproduceReserveEnergy)
			{
				energy -= energyToReproduce;
				Reproduce();
			}
		}
	}

	/// <summary>
	/// Initialize entity parameters and apply mutation
	/// </summary>
	private void InitializeEntity()
	{
		movementSpeed += MathHelper.RandomValue(movementSpeed * MutationRandomRange);
		size += MathHelper.RandomValue(size * MutationRandomRange);
		energy += MathHelper.RandomValue(energy * MutationRandomRange);
		energyToReproduce += MathHelper.RandomValue(energyToReproduce * MutationRandomRange);
		viewRadius += MathHelper.RandomValue(viewRadius * MutationRandomRange);
		dieChance += MathHelper.RandomValue(dieChance * MutationRandomRange);
		reproduceReserveEnergy = energyToReproduce * EnergyReserveAfterReproduce;

		sr.color = Color.Lerp(LowSpeedColor, HighSpeedColor, movementSpeed / (HighSpeed - LowSpeed));
		viewCollider.radius = viewRadius;
		Vector3 scale = Vector2.one * size;
		scale.z = 1;
		myTransform.localScale = scale;
	}

	
	private void Reproduce()
	{
		Entity entity = EntityManager.manager.GetEntity(null);
		entity.movementSpeed = movementSpeed;
		entity.size = size;
		entity.energy = energy;
		entity.energyToReproduce = energyToReproduce;
		entity.dieChance = dieChance;
		entity.viewRadius = viewRadius;
		entity.isDead = false;

		entity.transform.position = myTransform.position;
		entity.InitializeComponents();

		if(Random.value < dieChance)
		{
			energy = float.MinValue;
		}
	}

	private void MoveAlongDirectionOrEat(Vector2 direction)
	{
		if (!isEating)
		{
			Vector2 position = myTransform.position;
			body.MovePosition(position + direction * movementSpeed * Time.fixedDeltaTime);
			energy -= (size * size + movementSpeed) * EnergyLossPerStep;
		}
		else
		{
			float eatenEnergy = currentFoodObject.EatMe(EnergyAmountPerBite * size * size);
			energy += eatenEnergy;
			
			if(eatenEnergy == 0)
			{
				isEating = false;
				currentFoodObject = null;
			}
		}
	}


	/// <summary>
	/// Returns nearest food object in view radius
	/// </summary>
	/// <returns></returns>
	private Food GetNearestFoodObject()
	{
		float minDistance = float.MaxValue;
		int minIndex = -1;
		for(int i = 0; i < foodInFieldView.Count; i++)
		{
			float currentDistance = (foodInFieldView[i].transform.position - myTransform.position).sqrMagnitude; // use sqrMagnitude because it's faster
			if(currentDistance < minDistance)
			{
				minDistance = currentDistance;
				minIndex = i;
			}
		}

		if (minIndex != -1)
			return foodInFieldView[minIndex];
		else
			return null;
	}


	/// <summary>
	/// Is called when new food object enters field of view
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerEnter2D(Collider2D collision)
	{
		Food food = collision.GetComponent<Food>();
		if (food != null)
		{
			foodInFieldView.Add(food);
		}
	}

	/// <summary>
	/// Is called when food object exits field of view
	/// </summary>
	/// <param name="collision"></param>
	private void OnTriggerExit2D(Collider2D collision)
	{
		Food food = collision.GetComponent<Food>();
		if (food != null)
		{
			foodInFieldView.Remove(food);
		}
	}
}
