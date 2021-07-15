using Unity.Entities;
using Unity.Mathematics;

namespace Foods
{
    public struct FoodTracker : IComponentData
    {
        public int ID;
        public int CreatureID; // ID of the creature whose target is current FoodTracker component
        public float Energy;
        public float2 Position;

        private const string ToStringFormat = "ID: {0}; CreatureID: {1}; Energy: {2}; Position: {3}";

        public override string ToString()
        {
            return string.Format(ToStringFormat, ID, CreatureID, Energy, Position);
        }
    }
    public struct FoodComponent : IComponentData
    {
        public int ID;
        public float Energy;
        public bool IsEaten;
        public float RegrowthTimer;

        public const float InitialEnergy = 500.0f;
        public const float ParameterRandomRange = 0.2f;
        public const float TimeToRegrowth = 30.0f;
        public const float MinDistanceToEat = 0.5f; // Represents the minimal distance at which the Food can be eaten
        public static readonly float3 EatenPosition = new float3(1e+6f, 1e+6f, 0.0f);

        /// <summary>
        /// Calculates and returns the InitialEnergy value plus some random
        /// </summary>
        public static float GetInitialEnergyWithRandom()
        {
            return InitialEnergy + MathHelper.RandomValue(InitialEnergy * ParameterRandomRange);
        }
    }
}