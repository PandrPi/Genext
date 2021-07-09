using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Creature : MonoBehaviour
{
    // Rules:
    // higher movement speed - higher energy loss per step (energy -= speed^2 * energyPerStep)
    // higher size - higher energy loss per step, but higher energyAmountPerBite ()
    // higher viewRadius - higher chance to notice food
    //


    [SerializeField] private float movementSpeed; // movement speed of creature
    [SerializeField] private float size; // size of creature
    [SerializeField] private float energy; // energy of creature, can be stored by eating food
    [SerializeField] private float energyToReproduce; // how many energy is needed to reproduce
    [SerializeField] private float dieChance; // chance to die after reproduce 
    [SerializeField] private float viewRadius; // The distance at which food can be noticed

    // [SerializeField] private List<Food> foodInFieldView = new List<Food>();

    // stores the food object that the creature is moving to
    [SerializeField] private Food currentFoodObject;
    [SerializeField] private float distanceToCurrentFood;
    [SerializeField] private Food nextFoodObject;
    [SerializeField] private float distanceToNextFood;

    [SerializeField] private Vector2 currentMovementDirection;

    [SerializeField] private bool isEating; // Creature is not moving while it's true
    [SerializeField] private bool isDead;

    // determines when the movement direction have to be changed
    private float randomDirectionTimer;

    // When the creature reproduces a child it always has this amount of energy to have a chance to go on living
    private float reproduceReserveEnergy;

    private Transform myTransform;
    private Rigidbody2D body;
    private CircleCollider2D viewCollider;
    private SpriteRenderer sr;

    private const float ChangeRandomMovementDirectionTimer = 10.0f;
    private const float EnergyReserveAfterReproduce = 0.30f;
    private const float EnergyLossPerStep = 0.3f;
    private const float EnergyAmountPerBite = 1.0f;

    // When new creature is created it inherits all the parent's parameters with his own mutation within [-PRR, +PRR] range
    private const float MutationRandomRange = 0.15f;
    private const float LowSpeed = 1.0f;
    private static readonly Color LowSpeedColor = Color.HSVToRGB(0.6028f, 0.45f, 1.0f);
    private const float HighSpeed = 20.0f;
    private static readonly Color HighSpeedColor = Color.HSVToRGB(1.0f, 0.45f, 1.0f);

    /// <summary>
    /// Initializes Creature's components. This method should be called only after the Creature instance is created.
    /// </summary>
    private void InitializeComponents()
    {
        myTransform = transform;
        body = GetComponent<Rigidbody2D>();
        // the second collider must be trigger, it's used to see food objects
        viewCollider = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = CreatureManager.Manager.GetCreatureSprite();
    }

    private void Awake()
    {
        InitializeComponents();
    }

    public void UpdateCreature()
    {
        if (isDead) return;

        Vector3 myTransformPosition = myTransform.position;

        if (currentFoodObject is null)
        {
            randomDirectionTimer += Time.fixedDeltaTime;
            if (randomDirectionTimer >= ChangeRandomMovementDirectionTimer)
            {
                randomDirectionTimer = 0;
                currentMovementDirection = Random.insideUnitCircle.normalized;
            }
            
            if (World.IsInsideEdgeArea(myTransform.position) == false)
            {
                Vector2 halfWorldSize = World.Instance.worldSize * 0.5f;
                Vector2 normal = myTransformPosition.x > halfWorldSize.x ? Vector2.left : Vector2.zero;
                normal = myTransformPosition.x < -halfWorldSize.x ? Vector2.right : normal;
                normal = myTransformPosition.x < -halfWorldSize.x ? Vector2.right : normal;
                normal = myTransformPosition.y > halfWorldSize.y ? Vector2.down : normal;
                normal = myTransformPosition.y < -halfWorldSize.y ? Vector2.up : normal;
                currentMovementDirection = Vector2.Reflect(currentMovementDirection, normal);
            }
        }
        else
        {
            randomDirectionTimer = 0;

            Transform nearestFoodTrs = currentFoodObject.transform;

            Vector2 directionNonNormalized = nearestFoodTrs.position - myTransformPosition;
            currentMovementDirection = directionNonNormalized.normalized;
            distanceToCurrentFood = directionNonNormalized.sqrMagnitude;
            if (nextFoodObject is null == false)
                distanceToNextFood = (nextFoodObject.transform.position - myTransformPosition).sqrMagnitude;

            if (distanceToCurrentFood < currentFoodObject.size)
            {
                isEating = true;
                currentMovementDirection = Vector2.zero;
            }
        }

        MoveAlongDirectionOrEat(currentMovementDirection);

        if (energy <= 0.0 && isDead == false)
        {
            isDead = true;
            CreatureManager.Manager.FreeCreature(this);
        }

        if (energy >= energyToReproduce + reproduceReserveEnergy && isDead == false)
        {
            energy -= energyToReproduce;
            Reproduce();
        }
    }

    /// <summary>
    /// Initializes creature parameters (DNA) and applies mutations to them
    /// </summary>
    public void InitializeCreature()
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
        myTransform.localScale = new Vector3(size, size, 1.0f);
        ;
        distanceToCurrentFood = float.MaxValue;
        distanceToNextFood = float.MaxValue;
        isDead = false;
    }

    /// <summary>
    /// Creates a child of the current creature. This child inherits parent's parameters and applies his own mutation
    /// to them.
    /// </summary>
    public void Reproduce()
    {
        Creature creature = CreatureManager.Manager.GetCreature(null);
        creature.movementSpeed = movementSpeed;
        creature.size = size;
        creature.energy = energy;
        creature.energyToReproduce = energyToReproduce;
        creature.dieChance = dieChance;
        creature.viewRadius = viewRadius;
        creature.isDead = false;

        creature.transform.position = myTransform.position;
        creature.InitializeCreature();

        // Creature has chance to die after reproduction process
        if (Random.value < dieChance)
        {
            energy = float.MinValue;
        }
    }

    /// <summary>
    /// Swaps the current food object with the next food object
    /// </summary>
    private void SwapFoodObjects()
    {
        distanceToCurrentFood = distanceToNextFood;
        currentFoodObject = nextFoodObject;
        distanceToNextFood = float.MaxValue;
        nextFoodObject = null;
    }

    /// <summary>
    /// This method is used to move the creature or to eat energy from the current food object
    /// </summary>
    /// <param name="direction"></param>
    private void MoveAlongDirectionOrEat(Vector2 direction)
    {
        if (isEating == false)
        {
            if (direction == Vector2.zero) currentMovementDirection = Random.insideUnitCircle.normalized;

            Vector2 position = myTransform.position;
            body.MovePosition(position + direction * (movementSpeed * Time.fixedDeltaTime));
            energy -= (size * size + movementSpeed) * EnergyLossPerStep;
        }
        else
        {
            if (currentFoodObject is null)
            {
                SwapFoodObjects();
                return;
            }

            float eatenEnergy = currentFoodObject.EatMe(EnergyAmountPerBite * size * size);
            energy += eatenEnergy;

            if (eatenEnergy == 0)
            {
                isEating = false;
                SwapFoodObjects();
            }
        }
    }

    /// <summary>
    /// This method is used when the food object enters the field of view of the creature. It determines whether
    /// the entered food object should me set as currentFoodObject or nextFoodObject
    /// </summary>
    /// <param name="collision"></param>
    private void MarkFoodAsNoticed(Collider2D collision)
    {
        float currentDistance = (collision.transform.position - myTransform.position).sqrMagnitude;

        if (currentDistance < distanceToCurrentFood)
        {
            currentFoodObject = collision.GetComponent<Food>();
            if (currentFoodObject is null)
            {
                SwapFoodObjects();
            }
        }
        else if (currentDistance < distanceToNextFood)
        {
            nextFoodObject = collision.GetComponent<Food>();
        }
    }

    /// <summary>
    /// This method is called when food object enters the field of view of the creature
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        MarkFoodAsNoticed(collision);
    }

    /// <summary>
    /// This method is called when food object exits the field of view of the creature
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerExit2D(Collider2D collision)
    {
        MarkFoodAsNoticed(collision);
    }
}