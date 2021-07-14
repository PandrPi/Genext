using Creatures;
using Foods;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Managers
{
    public class CreatureManagementSystem : ComponentSystem
    {
        public static CreatureManagementSystem Instance;

        private int numberOfLivingCreatures;
        private NativeQueue<Entity> creaturesPool;
        private AABB creatureMeshAABB;
        private NativeQueue<Entity> creaturesToReproduce;
        private NativeQueue<Entity> creaturesToFree;

        private static EntityManager _entityManager;
        private static EntityQuery _creatureEntityQuery;

        private static readonly Color LowSpeedColor = Color.HSVToRGB(0.6028f, 0.45f, 1.0f);
        private static readonly Color HighSpeedColor = Color.HSVToRGB(1.0f, 0.45f, 1.0f);

        public void Initialize(int creaturesNumber, Mesh creatureMesh, Material creatureMaterial)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Prepare AABB instance for our food entities
            creatureMeshAABB = creatureMesh.bounds.ToAABB();

            // Prepare food archetype
            EntityArchetype creatureArchetype = _entityManager.CreateArchetype(
                typeof(CreatureComponent),
                typeof(Translation),
                typeof(RenderMesh),
                typeof(RenderBounds),
                typeof(LocalToWorld)
            );

            creaturesPool = new NativeQueue<Entity>(Allocator.Persistent);
            creaturesToReproduce = new NativeQueue<Entity>(Allocator.Persistent);
            creaturesToFree = new NativeQueue<Entity>(Allocator.Persistent);

            // Create desired number of entities for further usage
            NativeArray<Entity> creatureEntitiesArray = new NativeArray<Entity>(creaturesNumber, Allocator.Temp);
            _entityManager.CreateEntity(creatureArchetype, creatureEntitiesArray);

            // Loop through all elements inside creatureEntitiesArray and initialize all its components
            for (var i = 0; i < creaturesNumber; i++)
            {
                var creatureEntity = creatureEntitiesArray[i];
                creaturesPool.Enqueue(creatureEntity);
                InitializeCreatureEntity(i, creatureEntity, creatureMesh, creatureMaterial);
            }

            creatureEntitiesArray.Dispose();

            _creatureEntityQuery = GetEntityQuery(typeof(CreatureComponent), typeof(Translation));
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Free resources
            creaturesPool.Dispose();
            creaturesToReproduce.Dispose();
            creaturesToFree.Dispose();
        }

        /// <summary>
        /// Initializes creature entity. Sets all necessary components for the entity object.
        /// </summary>
        private void InitializeCreatureEntity(int index, Entity creatureEntity, Mesh creatureMesh,
            Material creatureMaterial)
        {
            var creatureComponent = CreateCreatureComponentWithDefaultParameters();
            creatureComponent.ID = index + 1;
            _entityManager.SetComponentData(creatureEntity, creatureComponent);

            // Initialize Translation component with the DeadPosition
            _entityManager.SetComponentData(creatureEntity, new Translation()
            {
                Value = CreatureComponent.DeadPosition
            });

            // Initialize RenderMesh component with the shared parameters
            _entityManager.SetSharedComponentData(creatureEntity, new RenderMesh()
            {
                mesh = creatureMesh,
                material = creatureMaterial
            });

            // Initialize RenderBounds component with the previously prepared AABB object
            _entityManager.SetComponentData(creatureEntity, new RenderBounds()
            {
                Value = creatureMeshAABB
            });
        }

        /// <summary>
        /// Creates and returns a new CreatureComponent instance with default parameters
        /// </summary>
        private CreatureComponent CreateCreatureComponentWithDefaultParameters()
        {
            return new CreatureComponent()
            {
                MovementSpeed = General.World.Instance.defaultCreatureParameters.movementSpeed,
                Size = General.World.Instance.defaultCreatureParameters.size,
                Energy = General.World.Instance.defaultCreatureParameters.energy,
                EnergyToReproduce = General.World.Instance.defaultCreatureParameters.energyToReproduce,
                DieChance = General.World.Instance.defaultCreatureParameters.dieChance,
                ViewRadius = General.World.Instance.defaultCreatureParameters.viewRadius,
                MovementDirection = float2.zero,
                IsEating = false,
                IsDead = true,
                RandomDirectionTimer = 0.0f,
                ReproduceReserveEnergy = 0.0f
            };
        }

        public void CustomUpdate(float deltaTime)
        {
            var job = new UpdateCreaturesJob()
            {
                EntityType = GetArchetypeChunkEntityType(),
                CreatureType = GetArchetypeChunkComponentType<CreatureComponent>(),
                TranslationType = GetArchetypeChunkComponentType<Translation>(),
                CreaturesToReproduce = creaturesToReproduce.AsParallelWriter(),
                CreaturesToFree = creaturesToFree.AsParallelWriter(),
                FoodTrackersArray = FoodManagementSystem.FoodTrackersArray,
                QuadrantMultiHashMap = FoodManagementSystem.FoodQuadrantMultiHashMap,
                CellSize = FoodManagementSystem.QuadrantCellSize,
                CellYMultiplier = FoodUpdateJob.CellYMultiplier,
                WorldArea = General.World.WorldAreaRect,
                RandomGenerator = General.World.GetRandom(),
                DeltaTime = deltaTime
            };

            var jobHandle = job.ScheduleParallel(_creatureEntityQuery);
            jobHandle.Complete();

            // Process the reproduced creatures
            while (creaturesToFree.Count > 0)
            {
                var creature = creaturesToFree.Dequeue();
                FreeCreature(creature);
            }

            while (creaturesToReproduce.Count > 0)
            {
                var creature = creaturesToReproduce.Dequeue();
                ReproduceCreature(creature);
            }
        }

        protected override void OnUpdate()
        {
            // We have our CustomUpdate method which execute all necessary logic, but we also have not to remove this
            // empty method because this will cause "NotImplemented" exception during the code compilation
        }

        /// <summary>
        /// Prepares the dead creature object for the further usage inside the ObjectPooling algorithm.
        /// </summary>
        public void FreeCreature(Entity creatureEntity)
        {
            _entityManager.SetComponentData(creatureEntity, new Translation()
            {
                Value = CreatureComponent.DeadPosition
            });
            creaturesPool.Enqueue(creatureEntity);
            numberOfLivingCreatures--;
            UIManager.Instance.SetPopulationNumberText(numberOfLivingCreatures);

            if (numberOfLivingCreatures == 0)
            {
                // Our current epoch is ended
                // TODO: Write code that creates a new Creature for new epoch
                // creaturePrefab.GetComponent<Creature>().Reproduce(isNewEpoch: true);
            }
        }

        /// <summary>
        /// Returns the previously prepared CreatureComponent instance if it is available. If there is no available
        /// CreatureComponent instances this method creates and returns a new instance.
        /// </summary>
        public Entity GetCreature()
        {
            if (creaturesPool.Count == 0)
            {
                // Debug.Log("Epoch ended!");
                return Entity.Null;
            }

            var creatureEntity = creaturesPool.Dequeue();
            _entityManager.SetComponentData(creatureEntity, new Translation() {Value = float3.zero});

            // activeCreatures.Add(creatureComponent);
            numberOfLivingCreatures++;
            UIManager.Instance.SetPopulationNumberText(numberOfLivingCreatures);

            return creatureEntity;
        }

        /// <summary>
        /// Creates a child of the parent creature. This child inherits parent's parameters and applies his
        /// own mutation to them.
        /// </summary>
        public void ReproduceCreature(Entity parentEntity)
        {
            var parentCreatureComponent = _entityManager.GetComponentData<CreatureComponent>(parentEntity);
            var childEntity = GetCreature();

            if (childEntity == Entity.Null)
            {
                // Debug.Log("Cannot reproduce a child creature because there is no free creature entity!");
                return;
            }
            var childCreatureComponent = _entityManager.GetComponentData<CreatureComponent>(childEntity);

            _entityManager.SetComponentData(childEntity, new CreatureComponent()
            {
                ID = childCreatureComponent.ID,
                MovementSpeed = parentCreatureComponent.MovementSpeed,
                Size = parentCreatureComponent.Size,
                Energy = parentCreatureComponent.Energy,
                EnergyToReproduce = parentCreatureComponent.EnergyToReproduce,
                DieChance = parentCreatureComponent.DieChance,
                ViewRadius = parentCreatureComponent.ViewRadius,
                IsDead = false
            });

            var parentPosition = _entityManager.GetComponentData<Translation>(parentEntity).Value;
            _entityManager.SetComponentData(childEntity, new Translation() {Value = parentPosition});
            InitializeCreature(childEntity);

            // Creature has chance to die after reproduction process
            if (UnityEngine.Random.value < parentCreatureComponent.DieChance)
            {
                FreeCreature(parentEntity);
            }
        }

        public void InitializeCreature(Entity creatureEntity)
        {
            const float randomMutationRange = CreatureComponent.RandomMutationRange;

            var oldCreature = _entityManager.GetComponentData<CreatureComponent>(creatureEntity);

            var newCreatureComponent = new CreatureComponent()
            {
                ID = oldCreature.ID,
                // MovementSpeed = oldCreature.MovementSpeed,
                MovementSpeed = MathHelper.GetMutatedValue(oldCreature.MovementSpeed, randomMutationRange),
                Size = MathHelper.GetMutatedValue(oldCreature.Size, randomMutationRange),
                Energy = MathHelper.GetMutatedValue(oldCreature.Energy, randomMutationRange),
                EnergyToReproduce = MathHelper.GetMutatedValue(oldCreature.EnergyToReproduce, randomMutationRange),
                ViewRadius = MathHelper.GetMutatedValue(oldCreature.ViewRadius, randomMutationRange),
                DieChance = MathHelper.GetMutatedValue(oldCreature.DieChance, randomMutationRange),
                ReproduceReserveEnergy = oldCreature.EnergyToReproduce * CreatureComponent.EnergyReserveAfterReproduce,

                TargetID = 0,
                MovementDirection = float2.zero,
                IsEating = false,
                IsDead = false,
                RandomDirectionTimer = 0.0f
            };

            _entityManager.SetComponentData(creatureEntity, newCreatureComponent);
        }
    }
}