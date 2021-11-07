using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CriticalCarPartPerformance : PerformanceImpact
{
	public CriticalCarPartPerformance()
	{
	}

	public override void PrepareForSession()
	{
		base.PrepareForSession();
		Circuit circuit = Game.instance.sessionManager.eventDetails.circuit;
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			CarStats.StatType inStat = (CarStats.StatType)i;
			CarStats.RelevantToCircuit relevancy = CarStats.GetRelevancy(Mathf.RoundToInt(circuit.trackStatsCharacteristics.GetStat(inStat)));
			if (relevancy == CarStats.RelevantToCircuit.VeryImportant)
			{
				num++;
			}
		}
		CarManager carManager = this.mVehicle.driver.contract.GetTeam().carManager;
		Car car = this.mVehicle.car;
		float num2 = 0f;
		for (int j = 0; j < 6; j++)
		{
			CarStats.StatType inStat2 = (CarStats.StatType)j;
			CarStats.RelevantToCircuit relevancy2 = CarStats.GetRelevancy(Mathf.RoundToInt(circuit.trackStatsCharacteristics.GetStat(inStat2)));
			if (relevancy2 == CarStats.RelevantToCircuit.VeryImportant)
			{
				int num3 = carManager.GetCarStatRankOnGrid(inStat2, car) - 1;
				float num4 = this.mCarPerformance.criticalCarPart.timeCostForRank[num3];
				num4 /= (float)num;
				num2 += num4;
			}
		}
		base.IncreaseTimeCost(num2);
	}

	public override void SimulationUpdate(float inDeltaTime)
	{
		base.SimulationUpdate(inDeltaTime);
	}
}
