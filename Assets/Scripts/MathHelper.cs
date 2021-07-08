using UnityEngine;
using System.Runtime.CompilerServices;
using System.Numerics;

public class MathHelper
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetAverage(float[] array)
	{
		int vectorSize = Vector<float>.Count;
		var accVector = Vector<float>.Zero;
		int i;
		for (i = 0; i <= array.Length - vectorSize; i += vectorSize)
		{
			var v = new Vector<float>(array, i);
			accVector = Vector.Add(accVector, v);
		}
		float result = Vector.Dot(accVector, Vector<float>.One);
		for (; i < array.Length; i++)
		{
			result += array[i];
		}
		return result / array.Length;
	}
}
