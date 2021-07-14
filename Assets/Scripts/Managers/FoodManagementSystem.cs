using Foods;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Managers
{
    public class FoodManagementSystem : ComponentSystem
    {
        public static FoodManagementSystem Instance { get; private set; }
        public static NativeMultiHashMap<int, FoodTracker> FoodQuadrantMultiHashMap;
        public static NativeArray<FoodTracker> FoodTrackersArray;
        public static int QuadrantCellSize;
        private static float2 _halfWorldSize;
        private static int _foodNumber;
        
        private AABB foodMeshAABB;

        private static EntityManager _entityManager;
        private static EntityQuery _foodEntityQuery;

        public void Initialize(int foodsNumber, Vector2 worldSize, Mesh foodMesh, Material foodMaterial,
            int quadrantCellSize)
        {
            _foodNumber = foodsNumber;
            _halfWorldSize = new float2(worldSize.x, worldSize.y) * 0.5f;
            QuadrantCellSize = quadrantCellSize;

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            FoodQuadrantMultiHashMap = new NativeMultiHashMap<int, FoodTracker>(_foodNumber, Allocator.Persistent);
            FoodTrackersArray = new NativeArray<FoodTracker>(foodsNumber, Allocator.Persistent);

            // Prepare AABB instance for our food entities
            foodMeshAABB = foodMesh.bounds.ToAABB();

            // Prepare food archetype
            EntityArchetype foodArchetype = _entityManager.CreateArchetype(
                typeof(FoodComponent),
                typeof(Translation),
                typeof(RenderMesh),
                typeof(RenderBounds),
                typeof(LocalToWorld)
            );
            NativeArray<Entity> foodEntitiesArray = new NativeArray<Entity>(foodsNumber, Allocator.Temp);
            // Create desired number of entities for further usage
            _entityManager.CreateEntity(foodArchetype, foodEntitiesArray);

            // Loop through all elements inside foodEntitiesArray and initialize all its components
            for (int i = 0; i < _foodNumber; i++)
            {
                InitializeFoodEntity(i, foodEntitiesArray[i], foodMesh, foodMaterial);
                FoodTrackersArray[i] = new FoodTracker()
                {
                    ID = i + 1
                };
            }

            foodEntitiesArray.Dispose();

            _foodEntityQuery = GetEntityQuery(typeof(FoodComponent), typeof(Translation));
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            FoodQuadrantMultiHashMap.Dispose();
            FoodTrackersArray.Dispose();
        }

        private void InitializeFoodEntity(int index, Entity foodEntity, Mesh foodMesh, Material foodMaterial)
        {
            _entityManager.SetComponentData(foodEntity, new FoodComponent()
            {
                ID = index + 1,
                Energy = FoodComponent.GetInitialEnergyWithRandom(),
                IsEaten = true,
                RegrowthTimer = FoodComponent.TimeToRegrowth,
            });
            
            // Initialize Translation component with the EatenPosition
            _entityManager.SetComponentData(foodEntity, new Translation()
            {
                Value = FoodComponent.EatenPosition
            });
            
            // Initialize RenderMesh component with the shared parameters
            _entityManager.SetSharedComponentData(foodEntity, new RenderMesh()
            {
                mesh = foodMesh,
                material = foodMaterial
            });
            
            // Initialize RenderBounds component with the previously prepared AABB object
            _entityManager.SetComponentData(foodEntity, new RenderBounds()
            {
                Value = foodMeshAABB
            });
        }

        public void CustomUpdate(float deltaTime)
        {
            FoodQuadrantMultiHashMap.Clear();

            var job = new FoodUpdateJob()
            {
                FoodType = GetArchetypeChunkComponentType<FoodComponent>(),
                TranslationType = GetArchetypeChunkComponentType<Translation>(),
                QuadrantMultiHashMap = FoodQuadrantMultiHashMap.AsParallelWriter(),
                FoodTrackersArray = FoodTrackersArray,
                CellSize = QuadrantCellSize,
                HalfWorldSizeParam = _halfWorldSize,
                RandomGenerator = General.World.GetRandom(),
                DeltaTime = deltaTime
            };

            var jobHandle = job.Schedule(_foodEntityQuery);
            jobHandle.Complete();
        }

        protected override void OnUpdate()
        {
            // We have our CustomUpdate method which execute all necessary logic, but we also have not to remove this
            // empty method because this will cause "NotImplemented" exception during the code compilation
        }
    }
}