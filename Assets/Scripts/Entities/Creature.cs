using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Entity : MonoBehaviour
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
    [SerializeField] private float viewRadius; // The distance at which food can be notised

    // [SerializeField] private List<Food> foodInFieldView = new List<Food>();

    // stores the food object that the entity is moving to
    [SerializeField] private Food currentFoodObject;
    [SerializeField] private Vector2 currentMovementDirection;
    [SerializeField] private float distanceToCurrentFood;

    [SerializeField] private bool isEating; // Entity is not moving while it's true
    [SerializeField] private bool isDead;

    // determines when the movement direction have to be changed
    private float randomDirectionTimer;

    // When the entity reproduces a child it always has this amount of energy to have a chance to go on living
    private float reproduceReserveEnergy;

    private Transform myTransform;
    private Rigidbody2D body;
    private BoxCollider2D viewCollider;
    private SpriteRenderer sr;

    private const float ChangeRandomMovementDirectionTimer = 10.0f;
    private const float EnergyReserveAfterReproduce = 0.30f;
    private const float EnergyLossPerStep = 0.3f;
    private const float EnergyAmountPerBite = 1.0f;

    // When new entity is created it inherits all the parent's parameters with his own mutation within [-PRR, +PRR] range
    private const float MutationRandomRange = 0.15f;
    private const float LowSpeed = 1.0f;
    private static readonly Color LowSpeedColor = Color.HSVToRGB(0.6028f, 0.45f, 1.0f);
    private const float HighSpeed = 20.0f;
    private static readonly Color HighSpeedColor = Color.HSVToRGB(1.0f, 0.45f, 1.0f);

    public void InitializeComponents()
    {
        myTransform = transform;
        body = GetComponent<Rigidbody2D>();
        // the second collider must be trigger, it's used to see food objects
        viewCollider = GetComponents<BoxCollider2D>()[1];
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = EntityManager.Manager.GetEntitySprite();

        InitializeEntity();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (currentFoodObject == null)
        {
            // if (foodInFieldView.Count > 0)
            //     currentFoodObject = GetNearestFoodObject();

            // if food is not found get random direction and move in that direction
            if (currentFoodObject == null)
            {
                randomDirectionTimer += Time.fixedDeltaTime;
                if (randomDirectionTimer >= ChangeRandomMovementDirectionTimer)
                {
                    randomDirectionTimer = 0;
                    currentMovementDirection = Vector2.zero;
                }


                // if (World.IsInsideEdgeArea(myTransform.position))
                // {
                //     RaycastHit2D hit = Physics2D.Raycast(myTransform.position, currentMovementDirection, size * 2);
                //
                //     if (hit.transform != null)
                //     {
                //         if (hit.transform.CompareTag(World.WorldEdgeTag))
                //         {
                //             currentMovementDirection = Vector2.Reflect(currentMovementDirection, hit.normal);
                //         }
                //     }
                // }
                if (World.IsInsideEdgeArea(myTransform.position) == false)
                {
                    Vector2 halfWorldSize = World.Instance.worldSize * 0.5f;
                    Vector2 position = myTransform.position;
                    Vector2 normal = position.x > halfWorldSize.x ? Vector2.left : Vector2.zero;
                    normal = position.x < -halfWorldSize.x ? Vector2.right : normal;
                    normal = position.x < -halfWorldSize.x ? Vector2.right : normal;
                    normal = position.y > halfWorldSize.y ? Vector2.down : normal;
                    normal = position.y < -halfWorldSize.y ? Vector2.up : normal;
                    currentMovementDirection = Vector2.Reflect(currentMovementDirection, normal);
                }

                if (currentMovementDirection == Vector2.zero)
                    currentMovementDirection = Random.insideUnitCircle.normalized;

                MoveAlongDirectionOrEat(currentMovementDirection);
            }
        }
        else
        {
            Transform nearestFoodTrs = currentFoodObject.transform;

            Vector2 directionNonNormalized = nearestFoodTrs.position - myTransform.position;
            Vector2 moveDirection = directionNonNormalized.normalized;
            distanceToCurrentFood = directionNonNormalized.magnitude;

            if (distanceToCurrentFood < currentFoodObject.Size)
            {
                isEating = true;
                currentMovementDirection = Vector2.zero;
            }

            MoveAlongDirectionOrEat(moveDirection);
        }

        if (energy <= 0)
        {
            isDead = true;
            EntityManager.Manager.FreeEntity(this);
        }

        if (energy >= energyToReproduce + reproduceReserveEnergy)
        {
            energy -= energyToReproduce;
            Reproduce();
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
        viewCollider.size = Vector2.one * viewRadius;
        Vector3 scale = Vector2.one * size;
        scale.z = 1;
        myTransform.localScale = scale;
        distanceToCurrentFood = float.MaxValue;
    }


    private void Reproduce()
    {
        Entity entity = EntityManager.Manager.GetEntity(null);
        entity.movementSpeed = movementSpeed;
        entity.size = size;
        entity.energy = energy;
        entity.energyToReproduce = energyToReproduce;
        entity.dieChance = dieChance;
        entity.viewRadius = viewRadius;
        entity.isDead = false;

        entity.transform.position = myTransform.position;
        entity.InitializeComponents();

        if (Random.value < dieChance)
        {
            energy = float.MinValue;
        }
    }

    private void MoveAlongDirectionOrEat(Vector2 direction)
    {
        if (isEating == false)
        {
            Vector2 position = myTransform.position;
            body.MovePosition(position + direction * (movementSpeed * Time.fixedDeltaTime));
            energy -= (size * size + movementSpeed) * EnergyLossPerStep;
        }
        else
        {
            float eatenEnergy = currentFoodObject.EatMe(EnergyAmountPerBite * size * size);
            energy += eatenEnergy;

            if (eatenEnergy == 0)
            {
                isEating = false;
                currentFoodObject = null;
                distanceToCurrentFood = float.MaxValue;
            }
        }
    }


    // /// <summary>
    // /// Returns nearest food object in view radius
    // /// </summary>
    // /// <returns></returns>
    // private Food GetNearestFoodObject()
    // {
    //     float minDistance = float.MaxValue;
    //     int minIndex = -1;
    //     for (int i = 0; i < foodInFieldView.Count; i++)
    //     {
    //         // use sqrMagnitude because it's faster
    //         float currentDistance = (foodInFieldView[i].transform.position - myTransform.position).sqrMagnitude;
    //         if (currentDistance < minDistance)
    //         {
    //             minDistance = currentDistance;
    //             minIndex = i;
    //         }
    //     }
    //
    //     return minIndex != -1 ? foodInFieldView[minIndex] : null;
    // }


    /// <summary>
    /// Is called when new food object enters field of view
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Food food = collision.GetComponent<Food>();
        // if (food != null)
        // {
        //     foodInFieldView.Add(food);
        // }
        float currentDistance = (collision.transform.position - myTransform.position).magnitude;
        if (currentDistance < distanceToCurrentFood)
        {
            currentFoodObject = collision.GetComponent<Food>();
        }
    }

    // /// <summary>
    // /// Is called when food object exits field of view
    // /// </summary>
    // /// <param name="collision"></param>
    // private void OnTriggerExit2D(Collider2D collision)
    // {
    //     Food food = collision.GetComponent<Food>();
    //     if (food != null)
    //     {
    //         foodInFieldView.Remove(food);
    //     }
    // }
}