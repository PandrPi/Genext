using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct FoodUpdateJob : IJobParallelFor
{
    public NativeArray<FoodComponent> FoodComponents;
    public float DeltaTime;
    public float2 HalfWorldSize;

    public void Execute(int index)
    {
        FoodComponent foodComponent = FoodComponents[index];

        if (foodComponent.IsEaten != true) return;

        foodComponent.RegrowthTimer += DeltaTime;

        if (foodComponent.RegrowthTimer >= FoodComponent.TimeToRegrowth)
        {
            foodComponent.Regrowth(GetRandomFoodPosition());
        }
    }

    /// <summary>
    /// Returns the random position inside the world area
    /// </summary>
    /// <returns></returns>
    private float3 GetRandomFoodPosition()
    {
        return new float3(MathHelper.RandomValue() * (HalfWorldSize.x - 1),
            MathHelper.RandomValue() * (HalfWorldSize.y - 1), 0.0f);
    }
}