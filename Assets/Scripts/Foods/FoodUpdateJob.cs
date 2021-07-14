using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foods
{
    [BurstCompile]
    public struct FoodUpdateJob : IJobChunk
    {
        public ArchetypeChunkComponentType<FoodComponent> FoodType;
        public ArchetypeChunkComponentType<Translation> TranslationType;

        public NativeArray<FoodTracker> FoodTrackersArray;
        
        [WriteOnly] public NativeMultiHashMap<int, FoodTracker>.ParallelWriter QuadrantMultiHashMap;
        [ReadOnly] public int CellSize;
        [ReadOnly] public float2 HalfWorldSizeParam;
        [ReadOnly] public float DeltaTime;
        public Random RandomGenerator;

        public const int CellYMultiplier = 1000;

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
                        food.IsEaten = false;
                        food.RegrowthTimer = 0.0f;
                        food.Energy = GetInitialEnergyWithRandom();
                        var randomPosition = GetRandomFoodPosition();
                        translation.Value = randomPosition;
                        
                        tracker.Energy = food.Energy;
                        tracker.Position = randomPosition.xy;
                        FoodTrackersArray[food.ID - 1] = tracker;
                    }
                }
                else
                {
                    food.Energy = tracker.Energy;

                    if (food.Energy <= 0)
                    {
                        tracker.Position = FoodComponent.EatenPosition.xy;
                        translation.Value = FoodComponent.EatenPosition;
                        food.IsEaten = true;
                    }
                }
                int hashKey = GetHashKeyByPoint(translation.Value.xy);
                QuadrantMultiHashMap.Add(hashKey, tracker);

                // Assign the modified components back to the current chunk
                chunkFoods[i] = food;
                chunkTranslations[i] = translation;
            }
        }

        /// <summary>
        /// Returns the initial food energy plus some random value inside [-ParameterRandomRange, ParameterRandomRange]
        /// range percentages of InitialEnergy.
        /// </summary>
        private float GetInitialEnergyWithRandom()
        {
            const float initialEnergy = FoodComponent.InitialEnergy;
            float randomValue = RandomValue(initialEnergy * FoodComponent.ParameterRandomRange);
            return initialEnergy + randomValue;
        }

        private float RandomValue(float range)
        {
            return (RandomGenerator.NextFloat() * range * 2.0f) - range;
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
        /// Returns the random position inside the world area
        /// </summary>
        /// <returns></returns>
        private float3 GetRandomFoodPosition()
        {
            return new float3(RandomValue(1.0f) * (HalfWorldSizeParam.x - 1),
                RandomValue(1.0f) * (HalfWorldSizeParam.y - 1), 0.0f);
        }
    }
}