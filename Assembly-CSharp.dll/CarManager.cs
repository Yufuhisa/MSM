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
			this.AutoFit(this.mCar[j], CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		}
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
		this.UnfitAllParts(inCar);
		bool flag = false;
		if (inAvailabilityOption != CarManager.AutofitAvailabilityOption.AllParts)
		{
			if (inAvailabilityOption == CarManager.AutofitAvailabilityOption.UnfitedParts)
			{
				flag = true;
			}
		}
		else
		{
			flag = false;
		}
		CarPartStats.CarPartStat carPartStat = CarPartStats.CarPartStat.MainStat;
		CarPartStats.CarPartStat inStat = CarPartStats.CarPartStat.Reliability;
		if (inOption != CarManager.AutofitOptions.Performance)
		{
			if (inOption == CarManager.AutofitOptions.Reliability)
			{
				carPartStat = CarPartStats.CarPartStat.Reliability;
				inStat = CarPartStats.CarPartStat.MainStat;
			}
		}
		else
		{
			carPartStat = CarPartStats.CarPartStat.MainStat;
			inStat = CarPartStats.CarPartStat.Reliability;
		}
		CarPart.PartType[] partType = CarPart.GetPartType(this.mTeam.championship.series, false);
		for (int i = 0; i < partType.Length; i++)
		{
			List<CarPart> list = this.partInventory.GetPartInventory(partType[i]);
			CarPart carPart = inCar.seriesCurrentParts[i];
			for (int j = 0; j < list.Count; j++)
			{
				if (!flag || !list[j].isFitted)
				{
					bool flag2 = false;
					bool flag3 = false;
					if (carPart != null)
					{
						CarPartStats.CarPartStat carPartStat2 = carPartStat;
						if (carPartStat2 != CarPartStats.CarPartStat.MainStat)
						{
							if (carPartStat2 == CarPartStats.CarPartStat.Reliability)
							{
								flag2 = (carPart.stats.GetStat(carPartStat) < list[j].stats.GetStat(carPartStat));
								flag3 = (carPart.stats.GetStat(carPartStat) == list[j].stats.GetStat(carPartStat) && carPart.stats.statWithPerformance < list[j].stats.statWithPerformance);
							}
						}
						else
						{
							flag2 = (carPart.stats.statWithPerformance < list[j].stats.statWithPerformance);
							flag3 = (carPart.stats.statWithPerformance == list[j].stats.statWithPerformance && carPart.stats.GetStat(inStat) < list[j].stats.GetStat(inStat));
						}
					}
					if ((carPart == null || flag2 || flag3) && inCar.FitPart(list[j]))
					{
						carPart = list[j];
					}
				}
			}
		}
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
		this.AutoFit(this.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.AutoFit(this.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.SetMechanicsContribution(this.partInventory.GetAllParts());
	}

	public void AutofitBothCars()
	{
		this.UnfitAllParts(this.GetCar(0));
		this.UnfitAllParts(this.GetCar(1));
		this.AutoFit(this.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.AutoFit(this.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
	}

	public void AdaptPartsForNewSeason(CarStats inStats = null)
	{
		List<CarPart> list = new List<CarPart>();
		CarPart.PartType[] partType = CarPart.GetPartType(this.mTeam.championship.series, false);
		for (int i = 0; i < CarManager.carCount; i++)
		{
			foreach (CarPart.PartType inType in partType)
			{
				CarPart highestStatPartOfType = this.partInventory.GetHighestStatPartOfType(inType);
				if (highestStatPartOfType != null)
				{
					this.partInventory.RemovePart(highestStatPartOfType);
					list.Add(highestStatPartOfType);
				}
			}
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
				float statWithPerformance;
				float statBonusEngineer;
				if (carPart.GetPartType() == CarPart.PartType.Engine) {
					// engine performance is decided by suppliers
					int performanceEnigne = this.mTeam.carManager.GetCar(0).ChassisStats.supplierEngine.randomEngineLevelModifier;
					int performanceModFuel = this.mTeam.carManager.GetCar(0).ChassisStats.supplierFuel.randomEngineLevelModifier;
					statWithPerformance = (performanceEnigne + performanceModFuel);
					statBonusEngineer = 0f;
				}
				else
				{
					statWithPerformance = carPart.stats.statWithPerformance;
					statBonusEngineer = personOnJob.stats.partContributionStats.GetStat(CarPart.GetStatForPartType(carPart.GetPartType()));
				}
				if (inStats == null)
				{
					carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, statWithPerformance + statBonusEngineer);
				}
				else
				{
					CarStats.StatType statForPartType = CarPart.GetStatForPartType(carPart.GetPartType());
					carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, inStats.GetStat(statForPartType) + statBonusEngineer);
				}
			}
			this.partInventory.AddPart(carPart);
		}
		this.AutoFit(this.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.AutoFit(this.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.SetMechanicsContribution(list);
	}

	private void SetMechanicsContribution(List<CarPart> parts)
	{
		Mechanic mechanicOfDriver = this.mTeam.GetMechanicOfDriver(this.mTeam.GetDriverForCar(0));
		Mechanic mechanicOfDriver2 = this.mTeam.GetMechanicOfDriver(this.mTeam.GetDriverForCar(1));
		for (int i = 0; i < parts.Count; i++)
		{
			CarPart carPart = parts[i];
			Mechanic mechanic;
			if (carPart.fittedCar.identifier == 0)
			{
				mechanic = mechanicOfDriver;
			}
			else
			{
				mechanic = mechanicOfDriver2;
			}
			carPart.stats.maxPerformance = (float)Mathf.FloorToInt(mechanic.stats.performance);
		}
	}

	public void SetPartBasicStats(CarPart inPart)
	{
		inPart.stats.level = 0;
		inPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, GameStatsConstants.initialReliabilityValue);
		inPart.stats.SetMaxReliability(GameStatsConstants.initialMaxReliabilityValue);
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