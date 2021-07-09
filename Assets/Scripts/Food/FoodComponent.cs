using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct FoodComponent : IComponentData
{
    public float Energy;
    public bool IsEaten;
    public float RegrowthTimer;
    public Translation TranslationComponent;

    private const float InitialEnergy = 500.0f;
    private const float ParameterRandomRange = 0.2f;
    public const float TimeToRegrowth = 30.0f;
    private static readonly float3 EatenPosition = new float3(1e+6f, 1e+6f, 0.0f);

    /// <summary>
    /// Returns the initial food energy plus some random value inside [-ParameterRandomRange, ParameterRandomRange]
    /// range percentages of InitialEnergy.
    /// </summary>
    /// <returns></returns>
    public static float GetInitialEnergyWithRandom()
    {
        return InitialEnergy + MathHelper.RandomValue(InitialEnergy * ParameterRandomRange);
    }

    /// <summary>
    /// Use to determine the amount of eaten energy 
    /// </summary>
    /// <param name="energyPerBite">How many energy needs to subtract</param>
    /// <returns></returns>
    public float EatMe(float energyPerBite)
    {
        Energy -= energyPerBite;

        if ((Energy <= 0) == false) return energyPerBite;
        // The food is eaten
        Energy = 0;
        IsEaten = true;
        TranslationComponent.Value = EatenPosition;

        return 0;
    }

    /// <summary>
    /// This method grows the current food object so that it can be eaten by Creature in the future.
    /// </summary>
    public void Regrowth(float3 position)
    {
        IsEaten = false;
        RegrowthTimer = 0.0f;
        Energy = GetInitialEnergyWithRandom();
        TranslationComponent.Value = position;
    }
}