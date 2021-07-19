using UnityEngine;
using System.Runtime.CompilerServices;

public static class MathHelper
{
    /// <summary>
	/// Returns random value in range [-1, 1]
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float RandomValue()
	{
		return (Random.value * 2.0f) - 1.0f;
	}

	/// <summary>
	/// Returns random value in desired range
	/// </summary>
	/// <param name="range">Range of random value</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float RandomValue(float range)
	{
		return (Random.value * range * 2.0f) - range;
	}

	/// <summary>
	/// Calculates and returns the mutated value
	/// </summary>
	/// <param name="value">Original value</param>
	/// <param name="mutationRange">Mutation range</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetMutatedValue(float value, float mutationRange)
	{
		return value + RandomValue(value * mutationRange);
	}
}
