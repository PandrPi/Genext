using Unity.Entities;
using Unity.Mathematics;

namespace Creatures
{
    public struct CreatureComponent : IComponentData
    {
        // Rules:
        // higher movement speed - higher energy loss per step (energy -= speed^2 * energyPerStep)
        // higher size - higher energy loss per step, but higher energyAmountPerBite ()
        // higher viewRadius - higher chance to notice food
        
        public int ID;
        public int TargetID; // ID of food target, the creature has no target if it is equal to zero
        public float MovementSpeed; // movement speed of creature
        public float Size; // size of creature
        public float Energy; // energy of creature, can be stored by eating food
        public float EnergyToReproduce; // how many energy is needed to reproduce
        public float DieChance; // chance to die after reproduce 
        public float ViewRadius; // The distance at which food can be noticed

        public float2 MovementDirection;

        public bool IsEating; // Creature is not moving while it's true
        public bool IsDead;

        // determines when the movement direction have to be changed
        public float RandomDirectionTimer;

        // When the creature reproduces a child it always has this amount of energy to have a chance to go on living
        public float ReproduceReserveEnergy;

        public const float TimeToChangeMovementDirection = 10.0f;
        public const float EnergyReserveAfterReproduce = 0.30f;
        public const float EnergyLossPerStep = 0.3f;
        public const float EnergyAmountPerBite = 10.0f;

        // When the new creature is created it inherits all the parent's parameters with his own
        // mutation within [-RMR, +RMR] range (RMR is RandomMutationRange)
        public const float RandomMutationRange = 0.15f;
        public const float LowSpeed = 1.0f;
        public const float HighSpeed = 20.0f;
        public static float3 LowSpeedColor;
        public static float3 HighSpeedColor;
        public static readonly float3 DeadPosition = new float3(1e+6f, 1e+6f, 0.0f);
    }
}