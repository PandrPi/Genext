using Foods;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Creatures
{
    [BurstCompile]
    public struct CreatureProcessingJob : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkEntityType EntityType;
        public ArchetypeChunkComponentType<CreatureComponent> CreatureType;
        public ArchetypeChunkComponentType<Translation> TranslationType;

        [NativeDisableParallelForRestriction] public NativeArray<FoodTracker> FoodTrackersArray;
        // public NativeArray<FoodTracker> FoodTrackersArray;

        [WriteOnly] public NativeMultiHashMap<int, CreatureComponent>.ParallelWriter QuadrantMultiHashMap;
        [WriteOnly] public NativeQueue<Entity>.ParallelWriter CreaturesForReproduction;
        [WriteOnly] public NativeQueue<Entity>.ParallelWriter CreaturesForRelease;
        [ReadOnly] public NativeMultiHashMap<int, FoodTracker> FoodQuadrantMultiHashMap;
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

                // There is no need to update the creature when it is dead
                if (creature.IsDead) continue;

                FoodTracker closestFood = default;
                // We square the ViewRadius here in order not to square it multiple times in the future.
                // It is necessary for us to use lengthsq and distancesq methods because they are faster than
                // length and distance methods
                var closestDistance = creature.ViewRadius * creature.ViewRadius;
                var creaturePosition = translation.Value;

                // The higher ViewRadius creature have the more neighbour quadrants we have to check
                var neighbourQuadrantsNumber = CellSize * (int) math.ceil(creature.ViewRadius / CellSize);
                // Check the current quadrant and all its neighbours to find the closest food
                for (var x = creaturePosition.x - neighbourQuadrantsNumber;
                    x <= creaturePosition.x + neighbourQuadrantsNumber;
                    x += CellSize)
                {
                    for (var y = creaturePosition.y - neighbourQuadrantsNumber;
                        y <= creaturePosition.y + neighbourQuadrantsNumber;
                        y += CellSize)
                    {
                        var positionForQuadrant = new float2(x, y);
                        FindClosestFood(creaturePosition.xy, positionForQuadrant, creature.ID, ref closestDistance,
                            ref closestFood);
                    }
                }

                // When our closestFood object is not equal to the food object from the previous frame we have
                // to release the previous food by setting its CreatureID to zero
                if (creature.TargetID != 0 && creature.TargetID != closestFood.ID)
                {
                    var previousFood = FoodTrackersArray[creature.TargetID - 1];
                    previousFood.CreatureID = 0;
                    FoodTrackersArray[creature.TargetID - 1] = previousFood;
                }

                closestFood.CreatureID = creature.ID;
                creature.TargetID = closestFood.ID;

                // closestFood.ID is always zero when there is no closest food entity found, so we move the creature
                // along some random direction
                if (closestFood.ID == 0)
                {
                    creature.IsEating = false;
                    creature.RandomDirectionTimer += DeltaTime;
                    if (creature.RandomDirectionTimer >= CreatureComponent.TimeToChangeMovementDirection)
                    {
                        creature.RandomDirectionTimer = 0;
                        creature.MovementDirection = GetRandomMovementDirection();
                    }
                }
                else
                {
                    // If there is a closestFood object which is not the default we calculate distance and
                    // movement direction to the closestFood

                    var directionNonNormalized = closestFood.Position - creaturePosition.xy;
                    creature.MovementDirection = math.normalize(directionNonNormalized);
                    var distanceToNearestFood = math.lengthsq(directionNonNormalized);

                    // If the creature is close enough it can start eating the closest food
                    if (distanceToNearestFood < FoodComponent.MinDistanceToEat)
                    {
                        creature.IsEating = true;
                        creature.MovementDirection = float2.zero;
                    }
                }

                translation.Value = MoveAlongDirectionOrEat(ref creature, ref closestFood, creaturePosition,
                    creature.MovementDirection);
                creature.Position = translation.Value.xy;
                
                QuadrantMultiHashMap.Add(GetHashKeyByPoint(creature.Position), creature);

                // If the creature energy is too low we kill this creature
                if (creature.Energy <= 0.0)
                {
                    creature.IsDead = true;
                    // We have to release the food object
                    closestFood.CreatureID = 0;

                    // When our creature is dead we need to add it to the CreaturesForRelease queue for it to be
                    // released outside the job
                    CreaturesForRelease.Enqueue(creatureEntity);
                }

                // If the creature has enough energy for reproduction
                if (creature.Energy >= creature.EnergyAmountForReproduction + creature.ReserveEnergyAfterReproduction)
                {
                    creature.Energy -= creature.EnergyAmountForReproduction;
                    // When our creature collects enough energy it can reproduce the child, so we add this creature
                    // to CreaturesForReproduction queue to do it later
                    CreaturesForReproduction.Enqueue(creatureEntity);
                }

                // We have to apply all changes which we made to the closestFood object
                if (closestFood.ID != 0) FoodTrackersArray[closestFood.ID - 1] = closestFood;

                // Assign the modified components back to the current chunk
                chunkCreatures[i] = creature;
                chunkTranslations[i] = translation;
            }
        }

        /// <summary>
        /// This method is looking for the food entity, the distance from which to the specified creaturePosition is
        /// the smallest. This search is processed within the current quadrant which is determined by the hash value
        /// of positionForQuadrant parameter.
        /// </summary>
        /// <param name="creaturePosition">The actual creature position</param>
        /// <param name="positionForQuadrant">This position is used to calculate the hash index of the quadrant</param>
        /// <param name="creatureID">ID of the creature</param>
        /// <param name="closestDistance">The current closest distance</param>
        /// <param name="closestFood">The current FoodComponent instance</param>
        private void FindClosestFood(float2 creaturePosition, float2 positionForQuadrant, int creatureID,
            ref float closestDistance, ref FoodTracker closestFood)
        {
            int hashKey = GetHashKeyByPoint(positionForQuadrant);
            if (FoodQuadrantMultiHashMap.TryGetFirstValue(hashKey, out var foodTracker, out var iterator))
            {
                do
                {
                    var actualFoodTrackerData = FoodTrackersArray[foodTracker.ID - 1];
                    
                    // We can check whether the foodTracker is the closest if and only if the foodTracker
                    // is marked as non targeted or targeted by the current creature
                    if (actualFoodTrackerData.CreatureID == 0 || actualFoodTrackerData.CreatureID == creatureID)
                    {
                        var currentDistance = math.distancesq(foodTracker.Position, creaturePosition);
                        if (currentDistance < closestDistance)
                        {
                            closestDistance = currentDistance;
                            closestFood = foodTracker;
                        }
                    }
                } while (FoodQuadrantMultiHashMap.TryGetNextValue(out foodTracker, ref iterator));
            }
        }

        /// <summary>
        /// This method is used to move the creature or to eat energy from the closest food object. If there is no
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

                if (IsPointInsideWorldArea(newPosition.xy) == false)
                {
                    var normal = GetReflectionNormal(newPosition.xy);
                    creature.MovementDirection = math.reflect(creature.MovementDirection, normal);
                }
            }
            else
            {
                if (closestFood.ID == 0) return newPosition;

                var desiredEnergy = CreatureComponent.EnergyGainPerBite * sizeSquared;

                // If the closestFood contains less energy than our creature can eat per bite we have to allow creature
                // to eat only available food energy
                float eatenEnergy = closestFood.Energy < desiredEnergy ? closestFood.Energy : desiredEnergy;
                eatenEnergy = math.max(0.0f, eatenEnergy);

                creature.Energy += eatenEnergy;
                closestFood.Energy -= eatenEnergy;

                if (closestFood.Energy <= MinFoodEnergy)
                {
                    creature.IsEating = false;
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

        /// <summary>
        /// Determines whether the specified point is inside world area and returns the result
        /// </summary>
        /// <param name="point">Point that method checks</param>
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
        private float2 GetReflectionNormal(float2 point)
        {
            float2 normal = point.x > WorldArea.z ? new float2(-1, 0) : float2.zero;
            normal = point.x < WorldArea.x ? new float2(1, 0) : normal;
            normal = point.y > WorldArea.w ? new float2(0, -1) : normal;
            return point.y < WorldArea.y ? new float2(0, 1) : normal;
        }

        /// <summary>
        /// This method calculates and returns some random movement direction
        /// </summary>
        private float2 GetRandomMovementDirection()
        {
            return RandomGenerator.NextFloat2Direction();
        }
    }
}