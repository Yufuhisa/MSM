using System;
using FullSerializer;
using UnityEngine;
using System.Collections.Generic;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CriticalCarPartPerformance : PerformanceImpact
{
	public CriticalCarPartPerformance()
	{
	}

	public override void PrepareForSession()
	{
		base.PrepareForSession();

		// get cars
		List<Team> teamList = this.mVehicle.championship.standings.GetTeamList();
		List<Car> carList = new List<Car>();
		for (int i = 0; i < teamList.Count; i++) {
			carList.Add(teamList[i].carManager.GetCar(0));
			carList.Add(teamList[i].carManager.GetCar(1));
		}

		// get stat list
		Circuit circuit = Game.instance.sessionManager.eventDetails.circuit;
		float[] statWeights = new float[(int)CarStats.StatType.Count];
		float statWeightsSum = 0;
		for (int i = 0; i < (int)CarStats.StatType.Count; i++) {
			statWeights[i] = (float)CarStats.GetRelevancy(Mathf.RoundToInt(circuit.trackStatsCharacteristics.GetStat((CarStats.StatType)i)));
			// Engine is weighted higher than other parts
			if ((CarStats.StatType)i == CarStats.StatType.TopSpeed) {
				if (statWeights[i] == 2f)
					statWeights[i] = 3f;
				else if (statWeights[i] == 3f)
					statWeights[i] = 6f;
			}
			statWeightsSum += statWeights[i];
		}

		float bestCarStats = 0;
		float thisCarStats = 0;
		float currCarStats = 0;

		for (int i = 0; i < carList.Count; i++) {
			currCarStats = 0;
			for (int j = 0; j < (int)CarStats.StatType.Count; j++) {
				currCarStats += carList[i].GetStats().GetStat((CarStats.StatType)j) * statWeights[j] / statWeightsSum;
			}
			if (currCarStats > bestCarStats)
				bestCarStats = currCarStats;
			if (carList[i] == this.mVehicle.car)
				thisCarStats = currCarStats;
		}

		// calculate time cost (1.5 second for 100 points below best car)
		float timeCost = (bestCarStats - thisCarStats) * 1.5f / 100f;
		base.IncreaseTimeCost(timeCost);
/*
		global::Debug.LogErrorFormat("Calculate Performance Time Cost, for Driver {0}, TimeCoastDriver {1} CarStats {2} BestCarStats {3}", new object[] {
			this.mVehicle.GetName()
		  , timeCost.ToString("#0.00")
		  , bestCarStats.ToString("##00.00")
		  , thisCarStats.ToString("##00.00")
		});
*/
	}

	public override void SimulationUpdate(float inDeltaTime)
	{
		base.SimulationUpdate(inDeltaTime);
	}
}
