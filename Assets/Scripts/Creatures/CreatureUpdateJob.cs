using Foods;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Creatures
{
    [BurstCompile]
    public struct UpdateCreaturesJob : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkEntityType EntityType;
        public ArchetypeChunkComponentType<CreatureComponent> CreatureType;
        public ArchetypeChunkComponentType<Translation> TranslationType;

        public NativeArray<FoodTracker> FoodTrackersArray;

        [WriteOnly] public NativeQueue<Entity>.ParallelWriter CreaturesToReproduce;
        [WriteOnly] public NativeQueue<Entity>.ParallelWriter CreaturesToFree;
        [ReadOnly] public NativeMultiHashMap<int, FoodTracker> QuadrantMultiHashMap;
        [ReadOnly] public int CellSize;
        [ReadOnly] public int CellYMultiplier;
        [ReadOnly] public float4 WorldArea; // xy - left bottom corner, zw - right upper corner
        [ReadOnly] public Random RandomGenerator;
        [ReadOnly] public float DeltaTime;

        private const float MinFoodEnergy = 0.1f;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkEntities = chunk.GetNativeArray(EntityType);
            var chunkCreatures = chunk.GetNativeArray(CreatureType);
            var chunkTranslations = chunk.GetNativeArray(TranslationType);
            for (var i = 0; i < chunk.Count; i++)
            {
                // Read our components from current chunk
                var creatureEntity = chunkEntities[i];
                var creature = chunkCreatures[i];
                var translation = chunkTranslations[i];

                if (creature.IsDead) continue;

                // Most of creature logic is here
                FoodTracker closestFood;
                float closestDistance = creature.ViewRadius * creature.ViewRadius;
                float3 creaturePosition = translation.Value;

                // If our creature has no target
                if (creature.TargetID == 0)
                {
                    closestFood = default;
                    // check the current quadrant and its neighbours for the closest food
                    for (var x = creaturePosition.x - CellSize; x <= creaturePosition.x + CellSize; x += CellSize)
                    {
                        for (var y = creaturePosition.y - CellSize; y <= creaturePosition.y + CellSize; y += CellSize)
                        {
                            var positionForQuadrant = new float2(x, y);
                            FindClosestFood(creaturePosition.xy, positionForQuadrant, creature.ID, ref closestDistance,
                                ref closestFood);
                        }
                    }
                }
                else
                {
                    closestFood = FoodTrackersArray[creature.TargetID - 1];
                }

                if (creature.TargetID == 0 && closestFood.ID != 0 && creature.TargetID != closestFood.ID)
                {
                    creature.TargetID = closestFood.ID;
                    closestFood.CreatureID = creature.ID;
                    // FoodTrackersArray[closestFood.ID - 1] = closestFood;
                }

                // closestFood.ID is always zero when there is no closest food entity found
                if (closestFood.ID == 0 && creature.TargetID == 0)
                {
                    creature.IsEating = false;
                    creature.RandomDirectionTimer += DeltaTime;
                    if (creature.RandomDirectionTimer >= CreatureComponent.TimeToChangeMovementDirection)
                    {
                        creature.RandomDirectionTimer = 0;
                        creature.MovementDirection = GetRandomMovementDirection();
                    }

                    if (IsPointInsideWorldArea(creaturePosition.xy) == false)
                    {
                        var normal = GetReflectionNormal(creaturePosition.xy);
                        creature.MovementDirection = math.reflect(creature.MovementDirection, normal);
                    }
                }
                else
                {
                    creature.RandomDirectionTimer = 0;

                    var directionNonNormalized = closestFood.Position - creaturePosition.xy;
                    creature.MovementDirection = math.normalize(directionNonNormalized);
                    var distanceToNearestFood = math.lengthsq(directionNonNormalized);

                    if (distanceToNearestFood < FoodComponent.MinDistanceToEat)
                    {
                        creature.IsEating = true;
                        creature.MovementDirection = float2.zero;
                    }
                }

                translation.Value = MoveAlongDirectionOrEat(ref creature, ref closestFood, creaturePosition,
                    creature.MovementDirection);

                if (creature.Energy <= 0.0)
                {
                    creature.IsDead = true;
                    if (creature.TargetID != 0)
                    {
                        // Mark the foodTracker as non targeted
                        closestFood.CreatureID = 0;
                        // FoodTrackersArray[creature.TargetID - 1] = closestFood;
                        creature.TargetID = 0;
                    }

                    // When our creature is dead we need to add it to the CreaturesToFree queue for it to be
                    // released outside the job
                    CreaturesToFree.Enqueue(creatureEntity);
                }

                if (creature.Energy >= creature.EnergyToReproduce + creature.ReproduceReserveEnergy)
                {
                    creature.Energy -= creature.EnergyToReproduce;
                    // When our creature collects enough energy it can reproduce the child, so we add this creature
                    // to CreaturesToReproduce queue to do it later
                    CreaturesToReproduce.Enqueue(creatureEntity);
                }
                
                if (closestFood.ID != 0) FoodTrackersArray[closestFood.ID - 1] = closestFood;

                // Assign the modified components back to the current chunk
                chunkCreatures[i] = creature;
                chunkTranslations[i] = translation;
            }
        }

        /// <summary>
        /// this method is looking for the food entity, the distance from which to the specified creaturePosition is
        /// minimal. This search is within the current quadrant.
        /// </summary>
        /// <param name="creaturePosition">The actual creature position</param>
        /// <param name="positionForQuadrant">This position is used to calculate the hash index of the quadrant</param>
        /// <param name="creatureID">ID of the current creature</param>
        /// <param name="closestDistance">The current closest distance</param>
        /// <param name="closestFood">The current FoodComponent instance</param>
        private void FindClosestFood(float2 creaturePosition, float2 positionForQuadrant, int creatureID,
            ref float closestDistance, ref FoodTracker closestFood)
        {
            int hashKey = GetHashKeyByPoint(positionForQuadrant);
            if (QuadrantMultiHashMap.TryGetFirstValue(hashKey, out var foodTracker, out var iterator))
            {
                do
                {
                    // If and only if the foodTracker is marked as non targeted or targeted by current creature
                    // we can check if the foodTracker is the closest
                    if (foodTracker.CreatureID == 0 || foodTracker.CreatureID == creatureID)
                    {
                        var currentDistance = math.distancesq(foodTracker.Position, creaturePosition);
                        if (currentDistance < closestDistance)
                        {
                            closestDistance = currentDistance;
                            closestFood = foodTracker;
                        }
                    }
                } while (QuadrantMultiHashMap.TryGetNextValue(out foodTracker, ref iterator));
            }
        }

        /// <summary>
        /// This method is used to move the creature or to eat energy from the current food object. If there is no
        /// closest food found the creature will move along the current movement direction
        /// </summary>
        private float3 MoveAlongDirectionOrEat(ref CreatureComponent creature, ref FoodTracker closestFood,
            float3 position, float2 direction)
        {
            var newPosition = position;
            var sizeSquared = creature.Size * creature.Size;

            if (creature.IsEating == false) 
            {
                if (direction.Equals(float2.zero)) creature.MovementDirection = GetRandomMovementDirection();

                newPosition = position + new float3(direction, 0.0f) * (creature.MovementSpeed * DeltaTime);
                var speedSquared = creature.MovementSpeed * creature.MovementSpeed;
                creature.Energy -= (sizeSquared + speedSquared) * CreatureComponent.EnergyLossPerStep;
            }
            else
            {
                if (closestFood.ID == 0) return newPosition;

                var desiredEnergy = CreatureComponent.EnergyAmountPerBite * sizeSquared;

                // If food contains less energy than our creature can eat per bite we have to allow creature
                // to eat only available food energy
                float eatenEnergy = closestFood.Energy < desiredEnergy ? closestFood.Energy : desiredEnergy;
                eatenEnergy = math.max(0.0f, eatenEnergy);

                creature.Energy += eatenEnergy;
                closestFood.Energy -= eatenEnergy;

                if (closestFood.Energy <= MinFoodEnergy)
                {
                    creature.IsEating = false;
                    creature.TargetID = 0;
                    closestFood.Energy = 0.0f;
                    closestFood.CreatureID = 0;
                }
            }

            return newPosition;
        }

        /// <summary>
        /// Returns the hash number calculated for the specified point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private int GetHashKeyByPoint(float2 point)
        {
            return (int) (math.floor(point.x / CellSize) + (CellYMultiplier * math.floor(point.y / CellSize)));
        }

        private bool IsPointInsideWorldArea(float2 point)
        {
            return point.x >= WorldArea.x && point.x <= WorldArea.z && point.y >= WorldArea.y &&
                   point.y <= WorldArea.w;
        }

        /// <summary>
        /// If our creature is outside the WorldArea we have to find the normal vector in order to reflect the movement
        /// direction of the creature. This method returns such a normal vector if specified point is outside the world
        /// area and float2.zero vector otherwise.
        /// </summary>
        /// <param name="point">Point for which we have to find a normal vector</param>
        /// <returns></returns>
        private float2 GetReflectionNormal(float2 point)
        {
            float2 normal = point.x > WorldArea.z ? new float2(-1, 0) : float2.zero;
            normal = point.x < WorldArea.x ? new float2(1, 0) : normal;
            normal = point.y > WorldArea.w ? new float2(0, -1) : normal;
            return point.y < WorldArea.y ? new float2(0, 1) : normal;
        }

        private float2 GetRandomMovementDirection()
        {
            return RandomGenerator.NextFloat2Direction();
        }
    }
}