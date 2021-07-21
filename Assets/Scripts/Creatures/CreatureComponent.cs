using Unity.Entities;
using Unity.Mathematics;

namespace Creatures
{
    public struct CreatureComponent : IComponentData, IUnitDataWithPosition
    {
        // Creature Behaviour Rules:
        // the higher movement speed the higher energy loss per step
        //     energy -= (size^2 + speed^2) * energyLossPerStep
        // the higher size the higher energy loss per step, but also the higher energy gain per bite
        //     energy += energyGainPerBite * size^2
        // the higher viewRadius the higher chance to notice food
        
        public int ID;
        public int TargetID; // ID of food target, the creature has no target if it is equal to zero
        public float2 Position;
        public float MovementSpeed; // movement speed of the creature
        public float Size; // size of the creature
        public float Energy; // energy of the creature, can be gained by eating food and can be lost by movement
        public float EnergyAmountForReproduction; // how many energy is necessary for the creature to reproduce
        public float DieChance; // chance to die after reproduction
        public float ViewRadius; // the distance at which the creature can see food

        public float2 MovementDirection;

        public bool IsEating; // the creature is not moving while it's true
        public bool IsDead;

        // determines when the movement direction have to be changed. It is necessary to change the movement direction
        // in order to increase the probability of finding food
        public float RandomDirectionTimer;

        // When the creature reproduces a child it always has this amount of energy to have a chance to continue to live
        public float ReserveEnergyAfterReproduction;

        public const float TimeToChangeMovementDirection = 10.0f;
        public const float EnergyReserveAfterReproduce = 0.30f;
        public const float EnergyLossPerStep = 0.3f;
        public const float EnergyGainPerBite = 10.0f;

        // When the new creature is created it inherits all the parent's parameters with his own
        // mutation within [-RMR, +RMR] range (RMR is RandomMutationRange)
        public const float RandomMutationRange = 0.15f;
        public const float LowSpeed = 1.0f;
        public const float HighSpeed = 20.0f;
        public static float3 LowSpeedColor;
        public static float3 HighSpeedColor;
        public static readonly float3 DeadPosition = new float3(1e+6f, 1e+6f, 0.0f);
        
        public float2 GetPosition()
        {
            return Position;
        }
    }
}