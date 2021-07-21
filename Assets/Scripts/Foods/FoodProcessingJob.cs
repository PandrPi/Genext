using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foods
{
    [BurstCompile]
    public struct FoodProcessingJob : IJobChunk
    {
        public ArchetypeChunkComponentType<FoodComponent> FoodType;
        public ArchetypeChunkComponentType<Translation> TranslationType;

        public NativeArray<FoodTracker> FoodTrackersArray;
        
        [WriteOnly] public NativeMultiHashMap<int, FoodTracker>.ParallelWriter QuadrantMultiHashMap;
        [ReadOnly] public int CellSize;
        [ReadOnly] public float2 HalfWorldSize;
        [ReadOnly] public float DeltaTime;
        public Random RandomGenerator;

        public const int CellYMultiplier = 1000;
        private const float MinFoodEnergy = 0.1f;
        private const int RandomRangeMin = 1;
        private const int RandomRangeMax = 1000000000;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkFoods = chunk.GetNativeArray(FoodType);
            var chunkTranslations = chunk.GetNativeArray(TranslationType);
            for (var i = 0; i < chunk.Count; i++)
            {
                // Read our components from current chunk
                var food = chunkFoods[i];
                var translation = chunkTranslations[i];
                var tracker = FoodTrackersArray[food.ID - 1];

                if (food.IsEaten)
                {
                    food.RegrowthTimer += DeltaTime;

                    if (food.RegrowthTimer >= FoodComponent.TimeToRegrowth)
                    {
                        // Regrow our food entity
                        food.RegrowthTimer = 0.0f;
                        food.IsEaten = false;
                        
                        food.Energy = GetInitialEnergyWithRandom(food.ID);
                        var randomPosition = GetRandomFoodPosition(food.ID);
                        translation.Value = randomPosition;
                        
                        tracker.Energy = food.Energy;
                        tracker.Position = randomPosition.xy;
                        tracker.CreatureID = 0;
                        
                        int hashKey = GetHashKeyByPoint(tracker.Position);
                        QuadrantMultiHashMap.Add(hashKey, tracker);
                    }
                }
                else
                {
                    food.Energy = tracker.Energy;

                    if (food.Energy <= MinFoodEnergy)
                    {
                        tracker.Position = FoodComponent.EatenPosition.xy;
                        tracker.CreatureID = 0;
                        tracker.Energy = food.Energy = 0;
                        translation.Value = FoodComponent.EatenPosition;
                        food.IsEaten = true;
                    }
                    else{
                        int hashKey = GetHashKeyByPoint(tracker.Position);
                        QuadrantMultiHashMap.Add(hashKey, tracker);
                    }
                }
                

                // Assign the modified components back to the current chunk
                chunkFoods[i] = food;
                chunkTranslations[i] = translation;
                FoodTrackersArray[food.ID - 1] = tracker;
            }
        }

        /// <summary>
        /// Returns the initial food energy plus some random value inside [-ParameterRandomRange, ParameterRandomRange]
        /// range percentages of InitialEnergy.
        /// </summary>
        private float GetInitialEnergyWithRandom(int foodID)
        {
            const float initialEnergy = FoodComponent.InitialEnergy;
            float randomValue = RandomValue(initialEnergy * FoodComponent.ParameterRandomRange, foodID);
            return initialEnergy + randomValue;
        }

        /// <summary>
        /// Returns the hash number calculated for the specified point
        /// </summary>
        private int GetHashKeyByPoint(float2 point)
        {
            return (int) (math.floor(point.x / CellSize) + (CellYMultiplier * math.floor(point.y / CellSize)));
        }

        /// <summary>
        /// Returns a random number within [-range; +range] range
        /// </summary>
        /// <param name="range">Desired range</param>
        /// <param name="foodID">Food ID is used to make the Random more random</param>
        private float RandomValue(float range, int foodID)
        {
            var seed = (uint)(RandomGenerator.NextInt(RandomRangeMin, RandomRangeMax) + foodID);
            return (new Random(seed).NextFloat() * range * 2.0f) - range;
        }

        /// <summary>
        /// Returns the random position inside the world area
        /// </summary>
        private float3 GetRandomFoodPosition(int foodID)
        {
            return new float3(RandomValue(1.0f, foodID) * (HalfWorldSize.x - 1),
                RandomValue(1.0f, foodID) * (HalfWorldSize.y - 1), 0.0f);
        }
    }
}