using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = UnityEngine.Random;

[System.Serializable]
public class FoodManager
{
    public static FoodManager Manager;
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Mesh foodMesh;
    [SerializeField] private Material foodMaterial;

    private NativeArray<FoodComponent> foodComponents;
    private float2 halfWorldSize;

    public void Initialize(int foodCount, Vector2 worldSize, Transform parent)
    {
        Manager = this;
        halfWorldSize = new float2(worldSize.x, worldSize.y) * 0.5f;

        EntityManager entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;

        // Prepare food archetype
        EntityArchetype foodArchetype = entityManager.CreateArchetype(
            typeof(FoodComponent),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld)
        );

        NativeArray<Entity> foodEntitiesArray = new NativeArray<Entity>(foodCount, Allocator.Temp);
        foodComponents = new NativeArray<FoodComponent>(foodCount, Allocator.Persistent);
        // Create desired number of entities for further usage
        entityManager.CreateEntity(foodArchetype, foodEntitiesArray);

        // Prepare foodMesh AABB for further usage inside RenderBounds components.
        AABB meshAABB = foodMesh.bounds.ToAABB();

        // Loop through all element inside foodEntitiesArray and initialize all its components
        for (int i = 0; i < foodEntitiesArray.Length; i++)
        {
            Entity foodEntity = foodEntitiesArray[i];

            Translation translationComponent = new Translation()
            {
                Value = GetRandomFoodPosition()
            };

            FoodComponent foodComponent = new FoodComponent()
            {
                Energy = FoodComponent.GetInitialEnergyWithRandom(),
                IsEaten = false,
                RegrowthTimer = 0.0f,
                TranslationComponent = translationComponent
            };

            foodComponents[i] = foodComponent;

            entityManager.SetComponentData(foodEntity, foodComponent);
            // Initialize Translation component with the random position inside world area
            entityManager.SetComponentData(foodEntity, translationComponent);

            // Initialize RenderMesh component with the shared parameters
            entityManager.SetSharedComponentData(foodEntity, new RenderMesh()
            {
                mesh = foodMesh,
                material = foodMaterial
            });

            // Initialize RenderBounds component with the previously prepared AABB object
            entityManager.SetComponentData(foodEntity, new RenderBounds()
            {
                Value = meshAABB
            });
        }

        foodEntitiesArray.Dispose();
    }

    public Vector2 GetRandomWorldPosition()
    {
        return new Vector2(MathHelper.RandomValue() * (halfWorldSize.x - 1),
            MathHelper.RandomValue() * (halfWorldSize.y - 1));
    }

    public float3 GetRandomFoodPosition()
    {
        return new float3(MathHelper.RandomValue() * (halfWorldSize.x - 1),
            MathHelper.RandomValue() * (halfWorldSize.y - 1), 0.0f);
    }

    // private Food CreateFood(Vector2 position, Transform parent)
    // {
    //     GameObject go = GameObject.Instantiate(foodPrefab, position, Quaternion.identity, parent);
    //     return go.GetComponent<Food>();
    // }

    public void UpdateManager()
    {
        var job = new FoodUpdateJob()
        {
            FoodComponents = foodComponents,
            DeltaTime = Time.fixedDeltaTime,
            HalfWorldSize = halfWorldSize
        };

        const int batchSize = 500;
        var jobHandle = job.Schedule(foodComponents.Length, batchSize);
        jobHandle.Complete();
    }

    public void Dispose()
    {
        foodComponents.Dispose();
    }
}