using Creatures;
using Foods;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Managers
{
    public class CreatureManagementSystem : ComponentSystem
    {
        public static CreatureManagementSystem Instance;

        private int totalCreaturesNumber;
        private int numberOfLivingCreatures;
        private AABB creatureMeshAABB;
        private NativeQueue<Entity> creaturesPool;
        private NativeQueue<Entity> creaturesForReproduction;
        private NativeQueue<Entity> creaturesForRelease;

        private static EntityManager _entityManager;
        private static EntityQuery _creatureEntityQuery;

        private static readonly Color LowSpeedColor = Color.HSVToRGB(0.6028f, 0.45f, 1.0f);
        private static readonly Color HighSpeedColor = Color.HSVToRGB(1.0f, 0.45f, 1.0f);

        public void Initialize(int creaturesNumber, Mesh creatureMesh, Material creatureMaterial)
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            totalCreaturesNumber = creaturesNumber;
            // Prepare AABB instance for our food entities
            creatureMeshAABB = creatureMesh.bounds.ToAABB();

            // Prepare creature archetype
            EntityArchetype creatureArchetype = _entityManager.CreateArchetype(
                typeof(CreatureComponent),
                typeof(Translation),
                typeof(Scale),
                typeof(RenderMesh),
                typeof(RenderBounds),
                typeof(LocalToWorld)
            );

            creaturesPool = new NativeQueue<Entity>(Allocator.Persistent);
            creaturesForReproduction = new NativeQueue<Entity>(Allocator.Persistent);
            creaturesForRelease = new NativeQueue<Entity>(Allocator.Persistent);

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

            // Release array resources
            creatureEntitiesArray.Dispose();

            // Initialize creatures query
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
            creaturesForReproduction.Dispose();
            creaturesForRelease.Dispose();
        }

        /// <summary>
        /// Initializes creature entity and all necessary components for this entity.
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

            // Initialize Scale component with the creature size
            _entityManager.SetComponentData(creatureEntity, new Scale()
            {
                Value = creatureComponent.Size
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
        private static CreatureComponent CreateCreatureComponentWithDefaultParameters()
        {
            var defaultCreatureParameters = General.World.Instance.defaultCreatureParameters;
            return new CreatureComponent()
            {
                MovementSpeed = defaultCreatureParameters.movementSpeed,
                Size = defaultCreatureParameters.size,
                Energy = defaultCreatureParameters.energy,
                EnergyAmountForReproduction = defaultCreatureParameters.energyToReproduce,
                DieChance = defaultCreatureParameters.dieChance,
                ViewRadius = defaultCreatureParameters.viewRadius,
                MovementDirection = float2.zero,
                IsEating = false,
                IsDead = true,
                RandomDirectionTimer = 0.0f,
                ReserveEnergyAfterReproduction = 0.0f
            };
        }

        public void CustomUpdate(float deltaTime)
        {
            var job = new CreatureProcessingJob()
            {
                EntityType = GetArchetypeChunkEntityType(),
                CreatureType = GetArchetypeChunkComponentType<CreatureComponent>(),
                TranslationType = GetArchetypeChunkComponentType<Translation>(),
                CreaturesForReproduction = creaturesForReproduction.AsParallelWriter(),
                CreaturesForRelease = creaturesForRelease.AsParallelWriter(),
                FoodTrackersArray = FoodManagementSystem.FoodTrackersArray,
                FoodQuadrantMultiHashMap = FoodManagementSystem.FoodQuadrantMultiHashMap,
                CellSize = FoodManagementSystem.QuadrantCellSize,
                CellYMultiplier = FoodProcessingJob.CellYMultiplier,
                WorldArea = General.World.WorldAreaRect,
                RandomGenerator = General.World.GetRandom(),
                DeltaTime = deltaTime
            };

            var jobHandle = job.Schedule(_creatureEntityQuery);
            jobHandle.Complete();

            // Process the creatures for release
            while (creaturesForRelease.Count > 0)
            {
                var creature = creaturesForRelease.Dequeue();
                FreeCreature(creature);
            }

            // Process the creatures for reproduction
            while (creaturesForReproduction.Count > 0)
            {
                var creature = creaturesForReproduction.Dequeue();
                ReproduceCreature(creature);
            }
        }

        protected override void OnUpdate()
        {
            // We have our CustomUpdate method which executes all necessary logic, but we also have not to remove this
            // empty method because this will cause "NotImplemented" exception during the code compilation
        }

        /// <summary>
        /// Prepares the dead creature entity for the further usage inside the ObjectPooling algorithm.
        /// </summary>
        private void FreeCreature(Entity creatureEntity)
        {
            var childCreatureComponent = _entityManager.GetComponentData<CreatureComponent>(creatureEntity);
            // If some foodTracker object is marked as targeted by the childCreatureComponent we have to reset 
            // the CreatureID of that foodTracker object
            if (childCreatureComponent.TargetID != 0)
            {
                var foodTracker = FoodManagementSystem.FoodTrackersArray[childCreatureComponent.TargetID - 1];
                if (foodTracker.CreatureID == childCreatureComponent.ID)
                {
                    foodTracker.CreatureID = 0;
                    FoodManagementSystem.FoodTrackersArray[childCreatureComponent.TargetID - 1] = foodTracker;
                }
            }

            _entityManager.SetComponentData(creatureEntity, new Translation()
            {
                Value = CreatureComponent.DeadPosition
            });
            creaturesPool.Enqueue(creatureEntity);
            numberOfLivingCreatures = totalCreaturesNumber - creaturesPool.Count;
            UIManager.Instance.SetPopulationNumberText(numberOfLivingCreatures);

            if (numberOfLivingCreatures == 0)
            {
                // Our current epoch is ended
                // TODO: Write code that creates a new Creature for new epoch
                // creaturePrefab.GetComponent<Creature>().Reproduce(isNewEpoch: true);
            }
        }

        /// <summary>
        /// Returns the previously prepared creature entity if it is available. If there is no available
        /// creature entities this method returns Entity.Null object.
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

            return creatureEntity;
        }

        /// <summary>
        /// Creates a child of the parent creature. This child creature inherits parent's parameters and applies his
        /// own mutation to these parameters.
        /// </summary>
        private void ReproduceCreature(Entity parentEntity)
        {
            var parentCreatureComponent = _entityManager.GetComponentData<CreatureComponent>(parentEntity);
            var childEntity = GetCreature();

            if (childEntity == Entity.Null)
            {
                // Debug.Log("Cannot reproduce a child creature because there is no free creature entity!");
                return;
            }

            var childCreatureComponent = _entityManager.GetComponentData<CreatureComponent>(childEntity);
            var parentPosition = _entityManager.GetComponentData<Translation>(parentEntity).Value;
            _entityManager.SetComponentData(childEntity, new Translation() {Value = parentPosition});
            InitializeCreature(childEntity, new CreatureComponent()
            {
                ID = childCreatureComponent.ID,
                MovementSpeed = parentCreatureComponent.MovementSpeed,
                Size = parentCreatureComponent.Size,
                Energy = parentCreatureComponent.Energy,
                EnergyAmountForReproduction = parentCreatureComponent.EnergyAmountForReproduction,
                DieChance = parentCreatureComponent.DieChance,
                ViewRadius = parentCreatureComponent.ViewRadius,
            });

            // If some foodTracker object is marked as targeted by the childCreatureComponent we have to reset 
            // the CreatureID of that foodTracker object
            if (childCreatureComponent.TargetID != 0)
            {
                var foodTracker = FoodManagementSystem.FoodTrackersArray[childCreatureComponent.TargetID - 1];
                if (foodTracker.CreatureID == childCreatureComponent.ID)
                {
                    foodTracker.CreatureID = 0;
                    FoodManagementSystem.FoodTrackersArray[childCreatureComponent.TargetID - 1] = foodTracker;
                }
            }

            // Creature has chance to die after reproduction process
            if (UnityEngine.Random.value < parentCreatureComponent.DieChance)
            {
                FreeCreature(parentEntity);
            }
        }

        /// <summary>
        /// Initializes and makes specified creature entity active (living). 
        /// </summary>
        /// <param name="creatureEntity">Creature entity that will be initialized</param>
        /// <param name="oldCreature">CreatureComponent that contains parent parameters. This parameter is used only
        /// by ReproduceCreature method </param>
        public void InitializeCreature(Entity creatureEntity, CreatureComponent oldCreature = default)
        {
            const float randomMutationRange = CreatureComponent.RandomMutationRange;

            if (oldCreature.ID == 0) oldCreature = _entityManager.GetComponentData<CreatureComponent>(creatureEntity);

            var newCreatureComponent = new CreatureComponent()
            {
                ID = oldCreature.ID,
                MovementSpeed = MathHelper.GetMutatedValue(oldCreature.MovementSpeed, randomMutationRange),
                Size = MathHelper.GetMutatedValue(oldCreature.Size, randomMutationRange),
                Energy = MathHelper.GetMutatedValue(oldCreature.Energy, randomMutationRange),
                EnergyAmountForReproduction =
                    MathHelper.GetMutatedValue(oldCreature.EnergyAmountForReproduction, randomMutationRange),
                ViewRadius = MathHelper.GetMutatedValue(oldCreature.ViewRadius, randomMutationRange),
                DieChance = MathHelper.GetMutatedValue(oldCreature.DieChance, randomMutationRange),
                ReserveEnergyAfterReproduction = oldCreature.EnergyAmountForReproduction *
                                                 CreatureComponent.EnergyReserveAfterReproduce,

                TargetID = 0,
                MovementDirection = float2.zero,
                IsEating = false,
                IsDead = false,
                RandomDirectionTimer = 0.0f
            };

            numberOfLivingCreatures = totalCreaturesNumber - creaturesPool.Count;
            UIManager.Instance.SetPopulationNumberText(numberOfLivingCreatures);

            _entityManager.SetComponentData(creatureEntity, newCreatureComponent);
            _entityManager.SetComponentData(creatureEntity, new Scale() {Value = newCreatureComponent.Size});
        }
    }
}