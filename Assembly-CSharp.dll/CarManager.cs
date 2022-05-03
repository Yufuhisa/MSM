using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarManager
{
	public CarManager()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static CarManager()
	{
	}

	public FrontendCar frontendCar
	{
		get
		{
			if (this.mFrontendCarThisYear == null)
			{
				this.CreateThisYearFrontendCar();
			}
			return this.mFrontendCarThisYear;
		}
	}

	public FrontendCar nextFrontendCar
	{
		get
		{
			if (this.mFrontendCarNextYear == null)
			{
				this.CreateNextYearFrontendCar();
			}
			return this.mFrontendCarNextYear;
		}
	}

	public void Start(Team inTeam)
	{
		this.mTeam = inTeam;
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mCar[i] = new Car();
			this.mNextCar[i] = new Car();
			this.mCar[i].identifier = i;
			this.mNextCar[i].identifier = i;
			this.mCar[i].Start(this);
			this.mNextCar[i].Start(this);
			if (Game.instance.chassisManager.chassisStats.ContainsKey(this.mTeam.teamID))
			{
				this.mCar[i].ChassisStats = Game.instance.chassisManager.chassisStats[this.mTeam.teamID];
			}
			else
			{
				global::Debug.LogErrorFormat("No chassis stats setup in the database (Chassis) for team ID: {0}", new object[]
				{
					this.mTeam.teamID
				});
				this.mCar[i].ChassisStats = Game.instance.chassisManager.chassisStats[0];
			}
		}
		this.nextYearCarDesign.Start(this.mTeam);
		this.carPartDesign.Start(this.mTeam);
		this.partImprovement.Start(this.mTeam);
		this.partInventory.OnStart();
	}

	public void ResetSessionSetupCarStatsContribution()
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.GetCar(i).ChassisStats.ResetSessionSetupChanges();
		}
	}

	public void ResetWeightStripping()
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.GetCar(i).ResetWeightStripping();
		}
	}

	public void ApplyNewCarDesigns(CarChassisStats inChassisStats)
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.UnfitAllParts(this.mCar[i]);
			this.mCar[i].Destroy();
			this.mNextCar[i].Destroy();
		}
		for (int j = 0; j < CarManager.carCount; j++)
		{
			this.mCar[j] = new Car();
			this.mNextCar[j] = new Car();
			this.mCar[j].identifier = j;
			this.mNextCar[j].identifier = j;
			this.mCar[j].Start(this);
			this.mNextCar[j].Start(this);
			this.mCar[j].ChassisStats = inChassisStats;
		}
		this.AutofitBothCars();
	}

	public void NotifyIsOwnedByPlayer()
	{
		this.CreateFrontendCars();
	}

	public void NotifyChampionshipChanged()
	{
		if (this.mTeam == Game.instance.player.team)
		{
			this.CreateNextYearFrontendCar();
		}
	}

	public int GetMechanicsAssignedToCar(Car inCar)
	{
		if (this.mTeam.headquarters != null)
		{
			int num = 0;
			float num2;
			if (num > 0)
			{
				num2 = (float)Mathf.RoundToInt(this.factoryStaffAllocation * (float)num) / (float)num;
			}
			else
			{
				num2 = 0.5f;
			}
			if (num > 0)
			{
				if (inCar == this.GetCar(0))
				{
					return Mathf.RoundToInt((float)num * num2);
				}
				return Mathf.RoundToInt((float)num * (1f - num2));
			}
		}
		return 0;
	}

	public void OnLoad()
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mCar[i].OnLoad();
			this.mNextCar[i].OnLoad();
		}
		if (this.partInventory != null)
		{
			this.partInventory.OnLoad(this.mTeam.championship);
		}
		if (this.mTeam.IsPlayersTeam())
		{
			if (this.mFrontendCarThisYear != null && this.mFrontendCarThisYear.data != null)
			{
				this.mFrontendCarThisYear.OnLoad(this.mTeam, App.instance.frontendCarManager);
			}
			else
			{
				this.CreateThisYearFrontendCar();
			}
			if (this.mFrontendCarNextYear != null && this.mFrontendCarNextYear.data != null)
			{
				this.mFrontendCarNextYear.OnLoad(this.mTeam, App.instance.frontendCarManager);
			}
			else
			{
				this.CreateNextYearFrontendCar();
			}
		}
		if (this.mFrontendCarThisYear != null && this.mFrontendCarThisYear.data != null)
		{
			this.mFrontendCarThisYear.data.colourData = this.mTeam.GetTeamColor().livery;
		}
	}

	public void Destroy()
	{
		this.partImprovement.Destroy();
		this.partInventory.Destroy();
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mCar[i].Destroy();
			this.mNextCar[i].Destroy();
			this.mCar[i] = null;
			this.mNextCar[i] = null;
		}
		this.DestroyFrontendCars();
	}

	public void Update()
	{
		if (!Game.instance.time.isPaused)
		{
			this.carPartDesign.Update();
			this.partImprovement.Update();
			if (this.mTeam == Game.instance.player.team)
			{
				this.nextYearCarDesign.Update();
			}
		}
		this.partImprovement.RefreshEndDates();
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mCar[i].Update();
		}
	}

	public bool BothCarsReadyForEvent()
	{
		bool flag = false;
		for (int i = 0; i < CarManager.carCount; i++)
		{
			Car car = this.GetCar(i);
			flag = this.CarReadyForEvent(car);
			if (!flag)
			{
				break;
			}
		}
		return flag;
	}

	public bool CarReadyForEvent(Car inCar)
	{
		for (int i = 0; i < inCar.seriesCurrentParts.Length; i++)
		{
			if (inCar.seriesCurrentParts[i] == null)
			{
				return false;
			}
		}
		return true;
	}

	public int GetNumberUnfittedParts(Car inCar)
	{
		int num = 0;
		for (int i = 0; i < inCar.seriesCurrentParts.Length; i++)
		{
			if (inCar.seriesCurrentParts[i] == null)
			{
				num++;
			}
		}
		return num;
	}

	public void UnfitAllParts(Car inCar)
	{
		for (int i = 0; i < inCar.seriesCurrentParts.Length; i++)
		{
			if (inCar.seriesCurrentParts[i] != null)
			{
				inCar.UnfitPart(inCar.seriesCurrentParts[i]);
			}
		}
	}

	public void AutoFit(Car inCar, CarManager.AutofitOptions inOption, CarManager.AutofitAvailabilityOption inAvailabilityOption)
	{
		// outdated and replaced by AutofitBothCars()
	}

	public bool PartHasBestStatOfTeam(CarPart inPart, CarStats.StatType inStat)
	{
		List<CarPart> list = this.partInventory.GetPartInventory(inPart.GetPartType());
		float statWithPerformance = inPart.stats.statWithPerformance;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].stats.statWithPerformance > statWithPerformance)
			{
				return false;
			}
		}
		return true;
	}

	public bool PartHasBestStatOnGrid(CarPart inPart, CarStats.StatType inStat)
	{
		List<CarPart> list = new List<CarPart>();
		for (int i = 0; i < this.team.championship.standings.teamEntryCount; i++)
		{
			ChampionshipEntry_v1 teamEntry = this.team.championship.standings.GetTeamEntry(i);
			list.AddRange(teamEntry.GetEntity<Team>().carManager.partInventory.GetPartInventory(inPart.GetPartType()));
		}
		float num = Mathf.Round(inPart.stats.statWithPerformance);
		for (int j = 0; j < list.Count; j++)
		{
			if (Mathf.Round(list[j].stats.statWithPerformance) > num)
			{
				return false;
			}
		}
		return true;
	}

	public float GetCarStatValueOnGrid(CarStats.StatType inStat, CarManager.MedianTypes inType)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < this.team.championship.standings.teamEntryCount; i++)
		{
			ChampionshipEntry_v1 teamEntry = this.team.championship.standings.GetTeamEntry(i);
			Team entity = teamEntry.GetEntity<Team>();
			for (int j = 0; j < Team.mainDriverCount; j++)
			{
				Car car = entity.carManager.GetCar(j);
				float stat = car.GetStats().GetStat(inStat);
				if (num < stat)
				{
					num = stat;
				}
				if (num2 == 0f || num2 > stat)
				{
					num2 = stat;
				}
				num3 += stat;
				num4++;
			}
		}
		switch (inType)
		{
		case CarManager.MedianTypes.Highest:
			return num;
		case CarManager.MedianTypes.Average:
			return num3 / (float)num4;
		case CarManager.MedianTypes.Lowest:
			return num2;
		default:
			return 0f;
		}
	}

	public int GetCarStatRankOnGrid(CarStats.StatType inStat, Car inCar)
	{
		int num = 1;
		float stat = inCar.GetStats().GetStat(inStat);
		for (int i = 0; i < this.team.championship.standings.teamEntryCount; i++)
		{
			ChampionshipEntry_v1 teamEntry = this.team.championship.standings.GetTeamEntry(i);
			Team entity = teamEntry.GetEntity<Team>();
			for (int j = 0; j < Team.mainDriverCount; j++)
			{
				Car car = entity.carManager.GetCar(j);
				if (car != inCar && car.GetStats().GetStat(inStat) > stat)
				{
					num++;
				}
			}
		}
		return num;
	}

	public float GetCarChassisStatValueOnGrid(CarChassisStats.Stats inStat, CarManager.MedianTypes inType)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < this.team.championship.standings.teamEntryCount; i++)
		{
			ChampionshipEntry_v1 teamEntry = this.team.championship.standings.GetTeamEntry(i);
			Team entity = teamEntry.GetEntity<Team>();
			for (int j = 0; j < Team.mainDriverCount; j++)
			{
				Car car = entity.carManager.GetCar(j);
				float stat = car.ChassisStats.GetStat(inStat, false, null);
				if (num < stat)
				{
					num = stat;
				}
				if (num2 == 0f || num2 > stat)
				{
					num2 = stat;
				}
				num3 += stat;
				num4++;
			}
		}
		switch (inType)
		{
		case CarManager.MedianTypes.Highest:
			return num;
		case CarManager.MedianTypes.Average:
			return num3 / (float)num4;
		case CarManager.MedianTypes.Lowest:
			return num2;
		default:
			return 0f;
		}
	}

	public Car GetCarOfBestStat(CarStats.StatType inStat)
	{
		if (this.mCar[1].GetStats().GetStat(inStat) > this.mCar[0].GetStats().GetStat(inStat))
		{
			return this.mCar[1];
		}
		return this.mCar[0];
	}

	public Car GetCarWithHighestTotalStats()
	{
		if (this.mCar[1].GetStats().statsTotal > this.mCar[0].GetStats().statsTotal)
		{
			return this.mCar[1];
		}
		return this.mCar[0];
	}

	public Car GetCar(int inIndex)
	{
		return this.mCar[inIndex];
	}

	public Car GetNextCar(int inIndex)
	{
		return this.mNextCar[inIndex];
	}

	public Car GetCarForDriver(Driver inDriver)
	{
		if (!inDriver.IsReserveDriver())
		{
			if (inDriver.carID < 0)
			{
				int driverIndex = this.mTeam.GetDriverIndex(inDriver);
				return this.mCar[driverIndex];
			}
			return this.mCar[inDriver.carID];
		}
		else
		{
			Driver driverSittingOut = this.mTeam.contractManager.GetDriverSittingOut();
			if (driverSittingOut == null)
			{
				return null;
			}
			if (driverSittingOut.carID < 0)
			{
				int driverIndex2 = this.mTeam.GetDriverIndex(driverSittingOut);
				return this.mCar[driverIndex2];
			}
			return this.mCar[driverSittingOut.carID];
		}
	}

	public static List<Car> GetOverralBestCarsOfChampionship(Championship inChampionship)
	{
		CarManager.mResultCache.Clear();
		CarManager.mAllCarsCache.Clear();
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			CarManager.mAllCarsCache.Add(entity.carManager.GetCar(0));
			CarManager.mAllCarsCache.Add(entity.carManager.GetCar(1));
		}
		while (CarManager.mAllCarsCache.Count > 0)
		{
			Car car = null;
			for (int j = 0; j < CarManager.mAllCarsCache.Count; j++)
			{
				Car car2 = CarManager.mAllCarsCache[j];
				if (car == null)
				{
					car = car2;
				}
				else
				{
					CarStats stats = car2.GetStats();
					if (stats.statsTotal > car.GetStats().statsTotal)
					{
						car = car2;
					}
				}
			}
			CarManager.mResultCache.Add(car);
			CarManager.mAllCarsCache.Remove(car);
		}
		return CarManager.mResultCache;
	}

	public static List<Car> GetCarStandingsOnStat(CarStats.StatType inStat, Championship inChampionship, params Team[] inTeamsToIgnore)
	{
		List<Car> list = new List<Car>();
		List<Car> list2 = new List<Car>();
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			bool flag = false;
			for (int j = 0; j < inTeamsToIgnore.Length; j++)
			{
				if (entity == inTeamsToIgnore[j])
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(entity.carManager.GetCar(0));
				list2.Add(entity.carManager.GetCar(1));
			}
		}
		while (list2.Count > 0)
		{
			Car car = null;
			for (int k = 0; k < list2.Count; k++)
			{
				Car car2 = list2[k];
				if (car == null)
				{
					car = car2;
				}
				else
				{
					CarStats stats = car2.GetStats();
					if (stats.GetStat(inStat) > car.GetStats().GetStat(inStat))
					{
						car = car2;
					}
				}
			}
			list.Add(car);
			list2.Remove(car);
		}
		return list;
	}

	public void ResetParts(CarStats inStatRankings)
	{
		foreach (CarPart.PartType partType2 in CarPart.GetPartType(this.mTeam.championship.series, false))
		{
			this.partInventory.DestroyParts(partType2);
			for (int j = 0; j < 2; j++)
			{
				CarPart carPart = CarPart.CreatePartEntity(partType2, this.mTeam.championship);
				this.SetPartBasicStats(carPart);
				float num = GameStatsConstants.partResetMaxBonus * inStatRankings.GetStat(CarPart.GetStatForPartType(partType2));
				carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, (float)this.mTeam.championship.rules.partStatSeasonMinValue[partType2] + num + (float)RandomUtility.GetRandom(0, 5));
				this.partInventory.AddPart(carPart);
				carPart.PostStatsSetup(this.mTeam.championship);
			}
		}
		this.AutofitBothCars();
	}
	
	private CarPart GetBestPartOfType(CarPart.PartType inPartType) {

		// calulcate reliability value the team strifes for
		float teamMinReliability = GameStatsConstants.targetReliabilityMax - (GameStatsConstants.targetReliabilityMax - GameStatsConstants.targetReliabilityMin) * this.mTeam.customAggressiveness;

		CarPart bestPerformancePart;
		CarPart bestReliabilityPart;

		// get available parts for part type
		List<CarPart> list = this.partInventory.GetPartInventory(inPartType);

		bestPerformancePart = null;
		bestReliabilityPart = null;

		for (int j = 0; j < list.Count; j++)
		{
			if (this.mTeam.championship.championshipID == 0) {
				global::Debug.LogErrorFormat("GetBestPartOfType Team {0} TileList MinRel {7} Part {5} Stats: Tier {6} Performance {1}/{2}, Reliability {3}/{4}", new object[] {
					this.mTeam.GetShortName()
					, list[j].stats.statWithPerformance.ToString("##0")
					, (list[j].stats.stat + list[j].stats.maxPerformance).ToString("##0")
					, (list[j].stats.reliability * 100f).ToString("##0")
					, (list[j].stats.GetMaxReliability() * 100f).ToString("##0")
					, list[j].GetPartType()
					, list[j].stats.level
					, teamMinReliability
				});
			}

			// skip used parts
			if (list[j].isFitted)
				continue;

			// if part has teamMinReliability check for bestPerformancePart
			if (list[j].reliability >= teamMinReliability)
			{
				if (bestPerformancePart == null || list[j].stats.statWithPerformance > bestPerformancePart.stats.statWithPerformance)
					bestPerformancePart = list[j];
			}
			// check for bestReliabilityPart
			if (bestReliabilityPart == null || list[j].reliability > bestReliabilityPart.reliability)
				bestReliabilityPart = list[j];
		}

		if (this.mTeam.championship.championshipID == 0) {
			if (bestPerformancePart == null)
				global::Debug.LogErrorFormat("GetBestPartOfType Team {0} bestPerf: not found", new object[] {
					this.mTeam.GetShortName()
				});
			else
				global::Debug.LogErrorFormat("GetBestPartOfType Team {0} bestPerf: MinRel {7} Part {5} Stats: Tier {6} Performance {1}/{2}, Reliability {3}/{4}", new object[] {
					this.mTeam.GetShortName()
					, bestPerformancePart.stats.statWithPerformance.ToString("##0")
					, (bestPerformancePart.stats.stat + bestPerformancePart.stats.maxPerformance).ToString("##0")
					, (bestPerformancePart.stats.reliability * 100f).ToString("##0")
					, (bestPerformancePart.stats.GetMaxReliability() * 100f).ToString("##0")
					, bestPerformancePart.GetPartType()
					, bestPerformancePart.stats.level
					, teamMinReliability
				});
			if (bestReliabilityPart == null)
				global::Debug.LogErrorFormat("GetBestPartOfType Team {0} bestRel: not found", new object[] {
					this.mTeam.GetShortName()
				});
			else
				global::Debug.LogErrorFormat("GetBestPartOfType Team {0} bestRel: MinRel {7} Part {5} Stats: Tier {6} Performance {1}/{2}, Reliability {3}/{4}", new object[] {
					this.mTeam.GetShortName()
					, bestReliabilityPart.stats.statWithPerformance.ToString("##0")
					, (bestReliabilityPart.stats.stat + bestReliabilityPart.stats.maxPerformance).ToString("##0")
					, (bestReliabilityPart.stats.reliability * 100f).ToString("##0")
					, (bestReliabilityPart.stats.GetMaxReliability() * 100f).ToString("##0")
					, bestReliabilityPart.GetPartType()
					, bestReliabilityPart.stats.level
					, teamMinReliability
				});
		}

		// return bestPerformancePart if available, otherwise bestReliabilityPart
		if (bestPerformancePart != null)
			return bestPerformancePart;
		else if (bestReliabilityPart != null)
			return bestReliabilityPart;

		return null;
	}

	public void AutofitBothCars()
	{
		// set first and second car for fitting
		Car firstCar = this.GetCar(0);
		Car secondCar = this.GetCar(1);

		// check if one car has driver with Contract Status One (otherwise it will be seen as equal)
		int firstCarNum = -1; // -1 == both cars will be fitted equally (as much as possible)
		if (this.mTeam.GetDriverForCar(0) != null && this.mTeam.GetDriverForCar(0).contract.currentStatus == ContractPerson.Status.One)
			firstCarNum = 0;
		else if (this.mTeam.GetDriverForCar(1) != null && this.mTeam.GetDriverForCar(1).contract.currentStatus == ContractPerson.Status.One)
			firstCarNum = 1;

		// if car number 2 (index 1) is first car, switch cars for fitting
		if (firstCarNum == 1) {
			firstCar = this.GetCar(1);
			secondCar = this.GetCar(0);
		}

		// remove all parts from cars first
		this.UnfitAllParts(firstCar);
		this.UnfitAllParts(secondCar);

		// find best part for all part types
		CarPart.PartType[] partType = CarPart.GetPartType(this.mTeam.championship.series, false);
		for (int i = 0; i < partType.Length; i++)
		{
			// fit parts of partType for both cars
			CarPart part = this.GetBestPartOfType(partType[i]);
			if (part != null)
				firstCar.FitPart(part);

			part = this.GetBestPartOfType(partType[i]);
			if (part != null)
				secondCar.FitPart(part);

			// if both cars are considered equal and first has higher overal value than second, switch first and second Car for next part Type
			if (firstCarNum == -1 && firstCar.GetStats().statsTotal > secondCar.GetStats().statsTotal) {
				Car switchCar = firstCar;
				firstCar = secondCar;
				secondCar = firstCar;
			}
		}
	}

	public void AdaptPartsForNewSeason(CarStats inStats = null)
	{
		List<CarPart> list = new List<CarPart>();
		CarPart.PartType[] partType = CarPart.GetPartType(this.mTeam.championship.series, false);

		float[] partTypeMaxReliability = new float[(int)CarPart.PartType.Last];
		for (int i = 0; i < partTypeMaxReliability.Length; i++)
			partTypeMaxReliability[i] = 0.74f;

		// find best parts as next season starting parts
		// and calculate mean reliability for those parts for next season starting reliability
		foreach (CarPart.PartType inType in partType)
		{
			float sumReliability = 0f;
			float divReliability = 0f;

			for (int i = 0; i < CarManager.carCount; i++)
			{
				CarPart highestStatPartOfType = this.partInventory.GetHighestStatPartOfType(inType);
				if (highestStatPartOfType != null)
				{
					sumReliability += highestStatPartOfType.stats.GetStat(CarPartStats.CarPartStat.Reliability);
					divReliability += 1f;
					this.partInventory.RemovePart(highestStatPartOfType);
					list.Add(highestStatPartOfType);
				}
			}
			if (divReliability > 0f)
				partTypeMaxReliability[(int)inType] = sumReliability / divReliability;
			if (partTypeMaxReliability[(int)inType] < 0.74f)
				partTypeMaxReliability[(int)inType] = 0.74f;
		}

		this.partInventory.DestroyAllParts();
		Engineer personOnJob = this.mTeam.contractManager.GetPersonOnJob<Engineer>(Contract.Job.EngineerLead);
		for (int k = 0; k < list.Count; k++)
		{
			CarPart carPart = list[k];
			global::Debug.Assert(!carPart.isBanned, "Banned part got through to next season, this should not happen ever.");
			if (!this.mTeam.championship.rules.specParts.Contains(carPart.GetPartType()))
			{
				this.SetPartBasicStats(carPart);
				float startPerformance;
				float startReliability;
				float startMaxReliability = GameStatsConstants.initialMaxReliabilityValue;
				float statBonusEngineer;
				if (carPart.GetPartType() == CarPart.PartType.Engine) {
					// engine performance is decided by suppliers
					int performanceEnigne = this.mTeam.carManager.GetCar(0).ChassisStats.supplierEngine.randomEngineLevelModifier;
					int performanceModFuel = this.mTeam.carManager.GetCar(0).ChassisStats.supplierFuel.randomEngineLevelModifier;
					startPerformance = (performanceEnigne + performanceModFuel);
					statBonusEngineer = 0f;
					// engine maxReliability is decided by suppliers as well
					float maxReliablityEnigne = this.team.carManager.GetCar(0).ChassisStats.supplierEngine.maxReliablity;
					float maxReliablityModFuel = this.team.carManager.GetCar(0).ChassisStats.supplierFuel.maxReliablity;
					startMaxReliability = maxReliablityEnigne + maxReliablityModFuel;
					// engine reliability starts as max value
					startReliability = startMaxReliability;
				}
				else
				{
					startPerformance = carPart.stats.statWithPerformance;
					statBonusEngineer = personOnJob.stats.partContributionStats.GetStat(CarPart.GetStatForPartType(carPart.GetPartType()));
					// start with 65%
					// add reliability of old part above 74% / 3 (a value between 0% and 8%)
					startReliability = 0.65f + ((partTypeMaxReliability[(int)carPart.GetPartType()] - 0.74f) / 3f);
				}
				if (inStats == null)
				{
					carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, startPerformance + statBonusEngineer);
					carPart.stats.SetMaxReliability(startMaxReliability);
					carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, startReliability);
					// reset performance development
					carPart.stats.SetStat(CarPartStats.CarPartStat.Performance, 0f);
				}
				else
				{
					CarStats.StatType statForPartType = CarPart.GetStatForPartType(carPart.GetPartType());
					carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, inStats.GetStat(statForPartType) + statBonusEngineer);
					carPart.stats.SetMaxReliability(startMaxReliability);
					carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, startReliability);
				}
			}
			this.partInventory.AddPart(carPart);
		}
		this.AutofitBothCars();
	}

	public void SetPartBasicStats(CarPart inPart)
	{
		inPart.stats.level = 0;
		inPart.stats.SetMaxReliability(GameStatsConstants.initialMaxReliabilityValue);
		inPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, GameStatsConstants.initialReliabilityValue);
		inPart.stats.SetStat(CarPartStats.CarPartStat.Performance, 0f);
		inPart.stats.maxPerformance = GameStatsConstants.baseCarPartPerformance;
		inPart.stats.rulesRisk = 0f;
		inPart.partCondition.redZone = GameStatsConstants.initialRedZone;
		inPart.components = new List<CarPartComponent>();
		inPart.buildDate = Game.instance.time.now;
	}

	private void CreateFrontendCars()
	{
		this.CreateThisYearFrontendCar();
		this.CreateNextYearFrontendCar();
	}

	private void CreateThisYearFrontendCar()
	{
		if (!Game.IsActive() || this.mTeam.IsPlayersTeam())
		{
			this.CreateFrontendCar(ref this.mFrontendCarThisYear, this.mCar[0], this.mCar[1]);
		}
		else
		{
			this.CreateFrontendCarForAITeam(ref this.mFrontendCarThisYear);
		}
	}

	private void CreateNextYearFrontendCar()
	{
		if (!this.mTeam.IsPlayersTeam())
		{
			this.CreateFrontendCarForAITeam(ref this.mFrontendCarNextYear);
			return;
		}
		if (this.mNextCar[0] != null && this.mNextCar[0].seriesCurrentParts[0] != null)
		{
			this.CreateFrontendCar(ref this.mFrontendCarNextYear, this.mNextCar[0], this.mNextCar[1]);
		}
		else
		{
			if (this.mFrontendCarNextYear == null || this.mFrontendCarNextYear.gameObject == null)
			{
				this.mFrontendCarNextYear = new FrontendCar();
				this.mFrontendCarNextYear.Start(this.mTeam.teamID, this.mTeam.championship.championshipID, App.instance.frontendCarManager);
			}
			this.mFrontendCarNextYear.Setup(null, new TeamColor(), LiveryData.defaultLivery, App.instance.carPartModelDatabase, FrontendCarData.BlendShapeData.defaultBlendShapeData, new SponsorSlot[6], this.mTeam.championship.championshipID);
			this.mFrontendCarNextYear.ForceFitRandomChangeablePartsForChampionship(this.mTeam.championship.championshipID, App.instance.carPartModelDatabase);
			this.UpdateFrontendCarWheels(this.mFrontendCarNextYear, this.mTeam.championship.championshipID);
		}
	}

	private void CreateFrontendCar(ref FrontendCar frontend_car, Car car0, Car car1)
	{
		if (car0 != null && car1 != null)
		{
			CarPart.PartType[] partType = CarPart.GetPartType(this.mTeam.championship.series, false);
			CarPart[] array = new CarPart[partType.Length];
			CarPart[] seriesCurrentParts = car0.seriesCurrentParts;
			CarPart[] seriesCurrentParts2 = car1.seriesCurrentParts;
			for (int i = 0; i < partType.Length; i++)
			{
				CarPart.PartType partType2 = partType[i];
				CarPart carPart;
				if (seriesCurrentParts[i] == null && seriesCurrentParts2[i] != null)
				{
					carPart = seriesCurrentParts2[i];
				}
				else if (seriesCurrentParts[i] != null && seriesCurrentParts2[i] == null)
				{
					carPart = seriesCurrentParts[i];
				}
				else
				{
					carPart = this.partInventory.GetHighestStatPartOfType(partType2);
				}
				if (carPart == null)
				{
					global::Debug.LogError("Trying to construct Frontend Car but one of the cars is missing a part: " + partType2.ToString(), null);
					array = null;
					break;
				}
				array[i] = carPart;
			}
			if (frontend_car == null || frontend_car.gameObject == null)
			{
				frontend_car = new FrontendCar();
				frontend_car.Start(this.mTeam.teamID, this.mTeam.championship.championshipID, App.instance.frontendCarManager);
			}
			frontend_car.Setup(array, App.instance.teamColorManager.GetColor(this.mTeam.colorID), Game.instance.liveryManager.GetLivery(this.mTeam.liveryID), App.instance.carPartModelDatabase, FrontendCarData.BlendShapeData.defaultBlendShapeData, this.mTeam.sponsorController.slots, this.mTeam.championship.championshipID);
			if (array == null)
			{
				frontend_car.ForceFitRandomChangeablePartsForChampionship(this.mTeam.championship.championshipID, App.instance.carPartModelDatabase);
			}
			this.UpdateFrontendCarWheels(frontend_car, this.mTeam.championship.championshipID);
		}
	}

	public void CreateFrontendCarForAITeam(ref FrontendCar frontend_car)
	{
		if (frontend_car == null)
		{
			frontend_car = new FrontendCar();
		}
		frontend_car.data.colourData = App.instance.teamColorManager.GetColor(this.mTeam.colorID).livery;
		frontend_car.data.liveryData = Game.instance.liveryManager.GetLivery(this.mTeam.liveryID);
		frontend_car.data.blendShapeData = FrontendCarData.BlendShapeData.defaultBlendShapeData;
		frontend_car.data.modelData = FrontendCar.DefaultPartsForChampionship(this.mTeam.championship.championshipID, App.instance.carPartModelDatabase);
		this.mTeam.sponsorController.RefreshSponsorsInFrontendCarData(ref frontend_car);
	}

	public void UpdateThisYearFrontendCarWheels(int championshipID)
	{
		this.UpdateFrontendCarWheels(this.mFrontendCarThisYear, championshipID);
	}

	public void UpdateNextYearFrontendCarWheels(int championshipID)
	{
		this.UpdateFrontendCarWheels(this.mFrontendCarNextYear, championshipID);
	}

	private void UpdateFrontendCarWheels(FrontendCar car, int championshipID)
	{
		if (car != null)
		{
			int wheelID = Car.GetWheelID(this.mTeam.championship.championshipID);
			car.SetWheelModel(wheelID, this.mTeam.championship.championshipID, App.instance.carPartModelDatabase);
		}
	}

	public void FitPartToFrontendCar(CarPart inPart)
	{
		if (this.mTeam.IsPlayersTeam())
		{
			CarPart highestStatPartOfType = this.partInventory.GetHighestStatPartOfType(inPart.GetPartType());
			if (this.mFrontendCarThisYear == null || this.mFrontendCarNextYear == null)
			{
				return;
			}
			if (App.instance.carPartModelDatabase.IsPartValidInChampionship(highestStatPartOfType.modelId, this.mTeam.championship.championshipID))
			{
				this.mFrontendCarThisYear.FitPart(highestStatPartOfType, App.instance.carPartModelDatabase, false);
				this.mFrontendCarNextYear.FitPart(highestStatPartOfType, App.instance.carPartModelDatabase, false);
				this.mFrontendCarNextYear.SetupBasedOnCurrentData();
			}
		}
	}

	public void UpdateThisYearFrontendCarWithNextYears()
	{
		if (this.mFrontendCarThisYear != null && this.mFrontendCarNextYear != null)
		{
			FrontendCarData data = this.mFrontendCarNextYear.data;
			this.mFrontendCarThisYear.data.CopyRequiredData(ref data);
			this.mFrontendCarThisYear.SetData(this.mFrontendCarThisYear.data);
			TeamColor.LiveryColour liveryColour = new TeamColor.LiveryColour();
			Championship.Series series = this.mTeam.championship.series;
			if (series != Championship.Series.SingleSeaterSeries)
			{
				if (series == Championship.Series.GTSeries)
				{
					liveryColour.SetAllColours(Color.black);
				}
			}
			else
			{
				liveryColour.SetAllColours(Color.white);
			}
			this.mFrontendCarNextYear.SetColours(liveryColour);
			if (this.mTeam != null && this.mTeam.IsPlayersTeam())
			{
				this.mTeam.sponsorController.RefreshSponsorsOnFrontendCar(this.mFrontendCarThisYear);
			}
		}
	}

	public void DestroyFrontendCars()
	{
		if (this.mFrontendCarThisYear != null)
		{
			this.mFrontendCarThisYear.Destroy();
			this.mFrontendCarThisYear = null;
		}
		if (this.mFrontendCarNextYear != null)
		{
			this.mFrontendCarNextYear.Destroy();
			this.mFrontendCarNextYear = null;
		}
	}

	public List<CarPart> GetPartsInCarsOfType(CarPart.PartType inType)
	{
		List<CarPart> list = new List<CarPart>();
		for (int i = 0; i < CarManager.carCount; i++)
		{
			CarPart part = this.GetCar(i).GetPart(inType);
			if (part != null)
			{
				list.Add(part);
			}
		}
		return list;
	}

	public Team team
	{
		get
		{
			return this.mTeam;
		}
	}

	public static int carCount = 2;

	public CarManagerAIController developmentAIController = new CarManagerAIController();

	public NextYearCarDesign nextYearCarDesign = new NextYearCarDesign();

	public CarPartDesign carPartDesign = new CarPartDesign();

	public CarPartInventory partInventory = new CarPartInventory();

	public PartImprovement partImprovement = new PartImprovement();

	public float designStaffAllocation = 0.5f;

	public float factoryStaffAllocation = 0.5f;

	public WorkingHours designStaffWorkingHours = WorkingHours.Average;

	public WorkingHours factoryStaffWorkingHours = WorkingHours.Average;

	private Team mTeam;

	private Car[] mCar = new Car[CarManager.carCount];

	private Car[] mNextCar = new Car[CarManager.carCount];

	private FrontendCar mFrontendCarThisYear;

	private FrontendCar mFrontendCarNextYear;

	private static List<Car> mResultCache = new List<Car>();

	private static List<Car> mAllCarsCache = new List<Car>();

	public enum AutofitAvailabilityOption
	{
		AllParts,
		UnfitedParts
	}

	public enum AutofitOptions
	{
		Performance,
		Reliability
	}

	public enum MedianTypes
	{
		Highest,
		Average,
		Lowest
	}
}
