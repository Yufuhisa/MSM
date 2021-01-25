using System;
using System.Collections.Generic;
using UnityEngine;

public static class RandomUtility
{

	public static float GetRandomDistribution(float inMinValue, float inMaxValue, int inNumberOfRolls, System.Random inRandom)
	{
		global::Debug.Assert(inNumberOfRolls > 0, "Jason - Cannot roll 0 times for GetRandomDistribution");
		float num = 0f;
		for (int i = 0; i < inNumberOfRolls; i++)
		{
			num = RandomUtility.GetRandom(inMinValue, inMaxValue, inRandom);
		}
		return num / (float)inNumberOfRolls;
	}

	public static float GetRandomDistribution(float inMinValue, float inMaxValue, int inNumberOfRolls)
	{
		return RandomUtility.GetRandomDistribution(inMinValue, inMaxValue, inNumberOfRolls, RandomUtility.globalRandomInstance);
	}

	public static float GetRandomNormallyDistributed(float inMean, float inStdDev)
	{
		return inMean + RandomUtility.GetRandomNormallyDistributed01() * inStdDev;
	}

	public static float GetRandomNormallyDistributed01()
	{
		return Mathf.Sqrt(-2f * Mathf.Log(RandomUtility.GetRandom01())) * Mathf.Sin(6.2831855f * RandomUtility.GetRandom01());
	}

	public static float GetRandom01(System.Random inRandom)
	{
		return (float)inRandom.NextDouble();
	}

	public static float GetRandom01()
	{
		return RandomUtility.GetRandom01(RandomUtility.globalRandomInstance);
	}

	public static int GetRandom(int inMin, int inMax, System.Random inRandom)
	{
		if (inMin > inMax)
		{
			int num = inMax;
			inMax = num;
			inMin = inMax;
		}
		return inRandom.Next(inMin, inMax);
	}

	public static int GetRandom(int inMin, int inMax)
	{
		return RandomUtility.GetRandom(inMin, inMax, RandomUtility.globalRandomInstance);
	}

	public static int GetRandomInc(int inMin, int inMax, System.Random inRandom)
	{
		if (inMin > inMax)
		{
			int num = inMax;
			inMax = num;
			inMin = inMax;
		}
		return inRandom.Next(inMin, inMax + 1);
	}

	public static int GetRandomInc(int inMin, int inMax)
	{
		return RandomUtility.GetRandomInc(inMin, inMax, RandomUtility.globalRandomInstance);
	}

	public static float GetRandom(float inMin, float inMax, System.Random inRandom)
	{
		if (inMin > inMax)
		{
			float num = inMax;
			inMax = num;
			inMin = inMax;
		}
		return inMin + (inMax - inMin) * RandomUtility.GetRandom01(inRandom);
	}

	public static float GetRandom(float inMin, float inMax)
	{
		return RandomUtility.GetRandom(inMin, inMax, RandomUtility.globalRandomInstance);
	}

	public static float GetRandomRoundedToStep(float inMin, float inMax, int nSteps)
	{
		global::Debug.AssertFormat(nSteps > 1, "Jason - Number of steps {0} is too few for GetRandomRoundedToStep method.", new object[]
		{
			nSteps
		});
		if (inMin > inMax)
		{
			float num = inMin;
			inMin = inMax;
			inMax = num;
		}
		float num2 = inMax - inMin;
		float num3 = inMin + num2 * Mathf.Round(RandomUtility.GetRandom01() * (float)nSteps) / (float)nSteps;
		global::Debug.AssertFormat(num3 <= inMax && num3 >= inMin, "Jason - Error in GetRandomRoundedToStep. Output {0} is outside requested range {1} - {2}", new object[]
		{
			num3,
			inMin,
			inMax
		});
		return num3;
	}

	public static bool GetCoinFlip()
	{
		return RandomUtility.GetRandom01() > 0.5f;
	}

	public static Color GetRandomColor(System.Random inRandom)
	{
		return new Color(RandomUtility.GetRandom01(inRandom), RandomUtility.GetRandom01(inRandom), RandomUtility.GetRandom01(inRandom), 1f);
	}

	public static Color GetRandomColor()
	{
		return RandomUtility.GetRandomColor(RandomUtility.globalRandomInstance);
	}

	public static void Shuffle<T>(ref List<T> inList)
	{
		int count = inList.Count;
		for (int i = 0; i < count; i++)
		{
			int random = RandomUtility.GetRandom(0, count);
			T value = inList[random];
			inList[random] = inList[i];
			inList[i] = value;
		}
	}

	private static System.Random globalRandomInstance = new System.Random();
}
