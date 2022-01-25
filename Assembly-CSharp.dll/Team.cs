using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Team : Entity
{
	public Team()
	{
	}

	public override void OnStart()
	{
		base.OnStart();
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Combine(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnHQsBuildingNotification));
		GameTimer time = Game.instance.time;
		time.OnMonthEnd = (Action)Delegate.Combine(time.OnMonthEnd, new Action(this.financeController.finance.transactionHistory.RemoveOldTransactions));
	}

	public override void OnDestory()
	{
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Remove(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnHQsBuildingNotification));
		GameTimer time = Game.instance.time;
		time.OnMonthEnd = (Action)Delegate.Remove(time.OnMonthEnd, new Action(this.financeController.finance.transactionHistory.RemoveOldTransactions));
		this.headquarters.Destroy();
		this.carManager.Destroy();
		Game.OnNewGame = (Action)Delegate.Remove(Game.OnNewGame, new Action(this.AllocateFundsOnNewGame));
		base.OnDestory();
	}

	public override void OnLoad()
	{
		base.OnLoad();
		this.mDrivers = null;
		if (this.IsPlayersTeam())
		{
			App.instance.teamColorManager.OnLoad(this.colorID);
		}
		this.financeController.finance.transactionHistory.OnLoad();
		this.carManager.OnLoad();
		this.headquarters.OnLoad();
		this.contractManager.OnLoad();
		this.pitCrewController.OnLoad(this);
		this.financeController.OnLoad();
		if (this.sponsorController != null)
		{
			this.sponsorController.OnLoad();
		}
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Remove(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnHQsBuildingNotification));
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Combine(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnHQsBuildingNotification));
		GameTimer time = Game.instance.time;
		time.OnMonthEnd = (Action)Delegate.Remove(time.OnMonthEnd, new Action(this.financeController.finance.transactionHistory.RemoveOldTransactions));
		GameTimer time2 = Game.instance.time;
		time2.OnMonthEnd = (Action)Delegate.Combine(time2.OnMonthEnd, new Action(this.financeController.finance.transactionHistory.RemoveOldTransactions));
		this.ValidateSelectedDrivers();
	}

	public void PostInitialise()
	{
		this.initialTotalFanBase = this.fanBase;
		if (Game.instance.headquartersManager.headquarters.ContainsKey(this.teamID))
		{
			this.headquarters = Game.instance.headquartersManager.headquarters[this.teamID];
		}
		else
		{
			global::Debug.LogErrorFormat("No headquarter setup in the database (HQ) for team ID: {0}", new object[]
			{
				this.teamID
			});
			this.headquarters = Game.instance.headquartersManager.headquarters[0];
		}
		this.perksManager.Start(this);
		this.carManager.Start(this);
		this.contractManager.Start(this);
		this.financeController.Start(this);
		this.headquarters.Start(this);
		this.sponsorController.Start(this);
		this.teamStatistics.Start(this);
		this.teamAIController.Start(this);
		this.pitCrewController.Start(this);
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			this.contractManager.AddEmployeeSlot(Contract.Job.Driver, null);
		}
		Game.OnNewGame = (Action)Delegate.Combine(Game.OnNewGame, new Action(this.AllocateFundsOnNewGame));
		this.UpdateFanBase();
		this.CreateSelectedDrivers();
	}

	private void AllocateFundsOnNewGame()
	{
		this.financeController.AllocateFunds(this.financeController.availableFunds, Game.instance.time.now.AddDays(-3.0));
	}

	public void SetChampionship(Championship inChampionship)
	{
		this.championship = inChampionship;
		this.pitCrewController.CreatePitstopLogs();
	}

	public void OnPreSeasonEnd()
	{
		this.carManager.nextYearCarDesign.DesignCompleted();
		if (!this.IsPlayersTeam())
		{
			this.teamAIController.OnPreSeasonEnd();
		}
	}

	public void OnNewSeasonStart(bool inWasPromoted, bool inWasRelegated)
	{
		if (this.IsPlayersTeam())
		{
			this.votingPower += Game.instance.player.votePowerModifier;
			Driver driver = null;
			if (this.AnyDriverWithTraitSpecialCase(PersonalityTrait.SpecialCaseType.ExtraVotePowerPerSeason, out driver))
			{
				this.votingPower++;
			}
		}
		if (this.history.HasPreviousSeasonHistory() && !inWasPromoted && !inWasRelegated)
		{
			this.startOfSeasonExpectedChampionshipResult = this.history.previousSeasonTeamResult;
		}
		else if (this.history.HasPreviousSeasonHistory())
		{
			if (inWasPromoted)
			{
				int teamEntryCount = this.championship.standings.teamEntryCount;
				this.startOfSeasonExpectedChampionshipResult = Mathf.Clamp(Mathf.RoundToInt(RandomUtility.GetRandom(0.7f, 0.9f) * (float)teamEntryCount), 1, teamEntryCount - 1);
			}
			else if (inWasRelegated)
			{
				this.startOfSeasonExpectedChampionshipResult = RandomUtility.GetRandomInc(1, 3);
			}
			else
			{
				this.startOfSeasonExpectedChampionshipResult = Game.instance.teamManager.CalculateExpectedPositionForChampionship(this);
			}
		}
		else
		{
			this.startOfSeasonExpectedChampionshipResult = Game.instance.teamManager.CalculateExpectedPositionForChampionship(this);
		}
		this.contractManager.SetupContractManagerEvents();
		this.financeController.SetNewSeasonPerRacePayments();
		this.sponsorController.SetNewSeasonSponsorDates();
		this.canRequestFunds = true;
		this.canReceiveFullChairmanPayments = true;
	}

	public float GetStarsStat()
	{
		return this.teamStatistics.GetTeamStars();
	}

	public void NotifyIsOwnedByPlayer()
	{
		this.carManager.NotifyIsOwnedByPlayer();
		this.votingPower += Game.instance.player.votePowerModifier;
		List<Person> allPeopleOnJob = this.contractManager.GetAllPeopleOnJob(Contract.Job.Driver);
		int count = allPeopleOnJob.Count;
		for (int i = 0; i < count; i++)
		{
			Driver driver = (Driver)allPeopleOnJob[i];
			driver.SetBeenScouted();
		}
	}

	public void NotifyChampionshipChanged()
	{
		this.carManager.NotifyChampionshipChanged();
	}

	public void NotifyPlayerLeftTeam()
	{
		this.carManager.DestroyFrontendCars();
	}

	public Driver GetTeamMate(Driver inDriver)
	{
		this.mDriversCache.Clear();
		this.contractManager.GetAllDrivers(ref this.mDriversCache);
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.mDriversCache[i];
			if (driver != null && inDriver != driver)
			{
				return driver;
			}
		}
		return null;
	}

	public Driver GetReserveDriverToReplaceSitOut()
	{
		List<Driver> list = new List<Driver>();
		Driver driver = null;
		this.contractManager.GetAllDrivers(ref list);
		for (int i = 0; i < list.Count; i++)
		{
			Driver driver2 = list[i];
			if (driver2.contract.currentStatus == ContractPerson.Status.Reserve)
			{
				driver = driver2;
			}
		}
		DriverManager driverManager = Game.instance.driverManager;
		if (driver != null)
			driverManager.AddDriverToChampionship(driver, true);
		return driver;
	}

	public Driver GetReserveDriver()
	{
		this.mDriversCache.Clear();
		this.contractManager.GetAllDrivers(ref this.mDriversCache);
		for (int i = 0; i < this.mDriversCache.Count; i++)
		{
			Driver driver = this.mDriversCache[i];
			if (driver.contract.currentStatus == ContractPerson.Status.Reserve)
			{
				return driver;
			}
		}
		return null;
	}

	public Driver GetAnyDriver()
	{
		Driver result = null;
		this.mDriversCache.Clear();
		this.contractManager.GetAllDrivers(ref this.mDriversCache);
		for (int i = 0; i < this.mDriversCache.Count; i++)
		{
			Driver driver = this.mDriversCache[i];
			if (driver != null)
			{
				result = driver;
				if (RandomUtility.GetRandom01() < 0.5f)
				{
					break;
				}
			}
		}
		return result;
	}

	public bool HasInjuriedDriver()
	{
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null && driver.IsInjured())
			{
				return true;
			}
		}
		return false;
	}

	public void AssignDriverToCar(Driver inDriver)
	{
		if (this.championship.series == Championship.Series.EnduranceSeries)
		{
			inDriver.SetCarID((this.GetDriversForCar(0).Length >= 3) ? 1 : 0);
		}
	}

	public void SwapDriver(Driver inDriver, Driver inReplacedDriver)
	{
		EmployeeSlot slotForPerson = this.contractManager.GetSlotForPerson(inDriver);
		EmployeeSlot slotForPerson2 = this.contractManager.GetSlotForPerson(inReplacedDriver);
		EmployeeSlot nextYearDriverSlot = this.contractManager.GetNextYearDriverSlot(slotForPerson.slotID);
		EmployeeSlot nextYearDriverSlot2 = this.contractManager.GetNextYearDriverSlot(slotForPerson2.slotID);
		int carID = inDriver.carID;
		int carID2 = inReplacedDriver.carID;
		inDriver.SetCarID(carID2);
		inReplacedDriver.SetCarID(carID);
		slotForPerson.personHired = inReplacedDriver;
		slotForPerson2.personHired = inDriver;
		bool flag = nextYearDriverSlot.personHired == inDriver;
		bool flag2 = nextYearDriverSlot2.personHired == inReplacedDriver;
		nextYearDriverSlot2.personHired = ((!flag) ? null : inDriver);
		nextYearDriverSlot.personHired = ((!flag2) ? null : inReplacedDriver);
		this.SelectMainDriversForSession();
		Mechanic mechanicOfDriver = this.GetMechanicOfDriver(inDriver);
		Mechanic mechanicOfDriver2 = this.GetMechanicOfDriver(inReplacedDriver);
		if (mechanicOfDriver != null)
		{
			mechanicOfDriver.SetDefaultDriverRelationship();
		}
		if (mechanicOfDriver2 != null)
		{
			mechanicOfDriver2.SetDefaultDriverRelationship();
		}
	}

	public List<Driver> GetDrivers()
	{
		this.mDriversCache.Clear();
		this.contractManager.GetAllDrivers(ref this.mDriversCache);
		return this.mDriversCache;
	}

	public Driver GetDriver(int inIndex)
	{
		if (inIndex < 0)
		{
			global::Debug.LogError("Trying to access and array with a negative index ", null);
			return null;
		}
		this.mDriversCache.Clear();
		this.contractManager.GetAllDrivers(ref this.mDriversCache);
		if (inIndex < this.mDriversCache.Count)
		{
			return this.mDriversCache[inIndex];
		}
		return null;
	}

	public Driver GetDriverForCar(int inCarIndex)
	{
		Driver[] driversForCar = this.GetDriversForCar(inCarIndex);
		if (driversForCar.Length > 0)
		{
			return driversForCar[0];
		}
		return null;
	}

	public Driver[] GetDriversForCar(int inCarIndex)
	{
		this.mDriversCache.Clear();
		bool flag = this.championship.series == Championship.Series.EnduranceSeries;
		if (flag)
		{
			this.contractManager.GetAllDriversForCar(ref this.mDriversCache, inCarIndex);
		}
		else
		{
			List<EmployeeSlot> allEmployeeSlotsForJob = this.contractManager.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
			if (!allEmployeeSlotsForJob[inCarIndex].IsAvailable())
			{
				Driver driver = allEmployeeSlotsForJob[inCarIndex].personHired as Driver;
				Driver reserveDriver = this.GetReserveDriverToReplaceSitOut();
				if (this.contractManager.IsSittingOutEvent(driver) && reserveDriver != null)
				{
					this.mDriversCache.Add(reserveDriver);
				}
				else
				{
					this.mDriversCache.Add(driver);
				}
			}
		}
		return this.mDriversCache.ToArray();
	}

	public Driver GetNextYearDriver(int inIndex)
	{
		List<EmployeeSlot> allEmployeeSlotsForJob = this.contractManager.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
		if (allEmployeeSlotsForJob[inIndex].personHired != null && allEmployeeSlotsForJob[inIndex].personHired.contract.IsContractedForNextSeason())
		{
			return allEmployeeSlotsForJob[inIndex].personHired as Driver;
		}
		return this.contractManager.GetNextYearDriverSlot(inIndex).personHired as Driver;
	}

	public int GetExpectedChampionshipResult()
	{
		if (this.championship.eventNumber != 0)
		{
			this.mCurrentExpectedChampionshipResult = this.GetChampionshipEntry().GetCurrentChampionshipPosition();
		}
		else
		{
			this.mCurrentExpectedChampionshipResult = this.startOfSeasonExpectedChampionshipResult;
		}
		return this.mCurrentExpectedChampionshipResult;
	}

	public bool HasDriver(Driver inDriver)
	{
		List<Person> allPeopleOnJob = this.contractManager.GetAllPeopleOnJob(Contract.Job.Driver);
		bool result = false;
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			if (allPeopleOnJob[i] == inDriver)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public int GetDriverIndex(Driver inDriver)
	{
		this.mEmployeeSlots.Clear();
		this.contractManager.GetAllEmployeeSlotsForJob(Contract.Job.Driver, ref this.mEmployeeSlots);
		if (inDriver != null && inDriver.IsReserveDriver())
		{
			return -1;
		}
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			if (this.mEmployeeSlots[i].personHired != null && this.mEmployeeSlots[i].personHired == inDriver)
			{
				return i;
			}
		}
		return -1;
	}

	public void SelectDriverForSession(Driver inDriver)
	{
		int num = inDriver.carID;
		if (this.championship.series != Championship.Series.EnduranceSeries && inDriver.IsReserveDriver() && this.contractManager.IsAnyDriverSittingOutEvent())
		{
			num = this.contractManager.GetDriverSittingOut().carID;
		}
		if (num < 0)
		{
			for (int i = 0; i < CarManager.carCount; i++)
			{
				if (this.mSelectedSessionDrivers[i].Count == 0)
				{
					num = i;
					break;
				}
			}
		}
		if (!this.mSelectedSessionDrivers[num].Contains(inDriver))
		{
			this.mSelectedSessionDrivers[num].Add(inDriver);
		}
		if (this.mVehicleSessionDrivers[num] == null)
		{
			this.mVehicleSessionDrivers[num] = inDriver;
		}
		this.OnDriverSelectedUpdateVehicles();
	}

	public void SwapVehicleDriverForSession(Driver inDriver, Driver inOldDriver)
	{
		if (inDriver == null || inOldDriver == null)
		{
			global::Debug.LogError("Trying to Swap Vehicle Driver ( Driver is null !!!!)", null);
		}
		for (int i = 0; i < CarManager.carCount; i++)
		{
			if (this.mVehicleSessionDrivers[i] == inOldDriver)
			{
				this.mVehicleSessionDrivers[i] = inDriver;
			}
		}
		this.OnDriverSelectedUpdateVehicles();
	}

	public void DeselectDriverForSession(Driver inDriver)
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			if (this.mSelectedSessionDrivers[i].Contains(inDriver))
			{
				this.mSelectedSessionDrivers[i].Remove(inDriver);
				if (this.mVehicleSessionDrivers[i] == inDriver)
				{
					this.mVehicleSessionDrivers[i] = ((this.mSelectedSessionDrivers[i].Count <= 0) ? null : this.mSelectedSessionDrivers[i][0]);
				}
			}
		}
		this.OnDriverSelectedUpdateVehicles();
	}

	private void OnDriverSelectedUpdateVehicles()
	{
		foreach (RacingVehicle racingVehicle in Game.instance.vehicleManager.GetVehiclesByTeam(this))
		{
			int carID = racingVehicle.carID;
			Driver driver = this.mVehicleSessionDrivers[carID];
			if (driver == null)
			{
				driver = ((this.mSelectedSessionDrivers[carID].Count <= 0) ? this.GetDriversForCar(carID)[0] : this.mSelectedSessionDrivers[carID][0]);
			}
			racingVehicle.SwapDriver(driver);
		}
	}

	public bool IsDriverSelectedForSession(Driver inDriver)
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			if (this.mSelectedSessionDrivers[i].Contains(inDriver))
			{
				return true;
			}
		}
		return false;
	}

	public bool isDriverDrivingForSession(Driver inDriver)
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			if (this.mVehicleSessionDrivers[i] == inDriver)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsWorstDriver(Driver inDriver, params Driver[] inDrivers)
	{
		float total = inDriver.GetDriverStats().GetTotal();
		for (int i = 0; i < inDrivers.Length; i++)
		{
			if (inDrivers[i].GetDriverStats().GetTotal() <= total)
			{
				return false;
			}
		}
		return true;
	}

	public void SelectMainDriversForSession()
	{
		bool flag = Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Qualifying && this.championship.rules.gridSetup == ChampionshipRules.GridSetup.AverageLap;

		bool alreadyOneDriverSittingOut = false;

		// check if a driver is already sitting out
		List<Driver> drivers = this.GetDrivers();
		for (int i = 0; i < drivers.Count; i++)
		{
			if (this.contractManager.IsSittingOutEvent(drivers[i]))
				alreadyOneDriverSittingOut = true;
		}

		for (int i = 0; i < CarManager.carCount; i++)
		{
			Driver[] driversForCar = this.GetDriversForCar(i);
			this.mSelectedSessionDrivers[i].Clear();
			foreach (Driver driver in driversForCar)
			{
				// For AI: First critical injured driver is sitting out
				if (!this.IsPlayersTeam() && driver.IsCriticalInjured() && !alreadyOneDriverSittingOut)
				{
					this.contractManager.SetSittingOutEventDriver(driver);
					alreadyOneDriverSittingOut = true;
				}
				if (this.contractManager.IsSittingOutEvent(driver))
				{
					Driver reserveDriver = this.GetReserveDriverToReplaceSitOut();
					if (reserveDriver != null)
						this.mSelectedSessionDrivers[i].Add(reserveDriver);
					else
						this.mSelectedSessionDrivers[i].Add(driver);
				}
				else if (!flag)
				{
					this.mSelectedSessionDrivers[i].Add(driver);
				}
				else if (this.mSelectedSessionDrivers[i].Count < 2 && !this.IsWorstDriver(driver, driversForCar))
				{
					this.mSelectedSessionDrivers[i].Add(driver);
				}
			}
		}
		for (int k = 0; k < CarManager.carCount; k++)
		{
			int index = 0;
			if (!this.IsPlayersTeam())
			{
				index = RandomUtility.GetRandom(0, this.mSelectedSessionDrivers[k].Count);
			}
			this.mVehicleSessionDrivers[k] = this.mSelectedSessionDrivers[k][index];
		}
	}

	public void ClearSelectedDriversForSession()
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mSelectedSessionDrivers[i].Clear();
			this.mVehicleSessionDrivers[i] = null;
		}
	}

	public bool IsDriverSelectionClear()
	{
		for (int i = 0; i < CarManager.carCount; i++)
		{
			if (this.mSelectedSessionDrivers[i].Count > 0)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsDriverSelectionForSessionComplete()
	{
		bool flag = this.championship.rules.gridSetup == ChampionshipRules.GridSetup.AverageLap && Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Qualifying;
		bool flag2 = this.championship.series == Championship.Series.EnduranceSeries;
		int num = 0;
		for (int i = 0; i < CarManager.carCount; i++)
		{
			num += this.mSelectedSessionDrivers[i].Count;
		}
		if (flag)
		{
			return num == 4;
		}
		return (!flag2) ? (num == 2) : (num == 6);
	}

	public Driver GetSelectedVehicledDriver(int inCarIndex)
	{
		return this.mVehicleSessionDrivers[inCarIndex];
	}

	public Driver[] GetSelectedDriversForCar(int inCarIndex)
	{
		return this.mSelectedSessionDrivers[inCarIndex].ToArray();
	}

	public string GetTeamNameColoured()
	{
		return GameUtility.ColorToRichTextHex(this.GetTeamColor().primaryUIColour.normal) + this.name + "</color>";
	}

	public string GetTeamNameForUI()
	{
		String teamName = this.mShortName;
		String supName = this.carManager.GetCar(0).ChassisStats.supplierEngine.name;
		return (teamName.Equals(supName) ? teamName : teamName + " - " + supName);
	}

	public TeamColor GetTeamColor()
	{
		return App.instance.teamColorManager.GetColor(this.colorID);
	}

	public ChampionshipEntry_v1 GetChampionshipEntry()
	{
		if (this.mChampionshipEntry == null && this.championship != null)
		{
			this.mChampionshipEntry = this.championship.standings.GetEntry(this);
		}
		return this.mChampionshipEntry;
	}

	public void ResetChampionshipEntry()
	{
		this.mChampionshipEntry = null;
	}

	public bool IsInAChampionship()
	{
		return this.GetChampionshipEntry() != null;
	}

	public override void Update()
	{
		this.carManager.Update();
		this.UpdateHeadquarters();
		// for F1 update engineer contribution for next year team chassi
		if (this.championship.series == Championship.Series.SingleSeaterSeries && this.championship.championshipID == 0)
			// TODO: only mid season
			Game.instance.supplierManager.UpdateChassiContribution(this);
	}

	private void UpdateHeadquarters()
	{
		if (!Game.instance.time.isPaused && this.headquarters != null)
		{
			for (int i = this.headquarters.hqBuildings.Count - 1; i >= 0; i--)
			{
				HQsBuilding_v1 hqsBuilding_v = this.headquarters.hqBuildings[i];
				if (hqsBuilding_v != null)
				{
					hqsBuilding_v.UpdateProgress();
				}
			}
		}
	}

	public void UpdateFanBase()
	{
		float num = 0f;
		float num2 = 0f;
		ChampionshipEntry_v1 championshipEntry = this.GetChampionshipEntry();
		if (championshipEntry != null)
		{
			ChampionshipEntry_v1 championshipEntry2 = this.GetDriver(0).GetChampionshipEntry();
			ChampionshipEntry_v1 championshipEntry3 = this.GetDriver(1).GetChampionshipEntry();
			bool flag = true;
			for (int i = 0; i < championshipEntry.races; i++)
			{
				int num3 = championshipEntry.GetExpectedRacePositionForEvent(i) - championshipEntry.GetChampionshipPositionForEvent(i);
				num += (float)num3 / 10f * this.GetMarketability();
				if (championshipEntry2 == null || championshipEntry3 == null)
				{
					flag = false;
				}
				else
				{
					int num4 = championshipEntry2.GetExpectedRacePositionForEvent(i) - championshipEntry2.GetRacePositionForEvent(i);
					int num5 = championshipEntry3.GetExpectedRacePositionForEvent(i) - championshipEntry3.GetRacePositionForEvent(i);
					int num6 = (num4 <= num5) ? num5 : num4;
					int inIndex = (num4 <= num5) ? 1 : 0;
					num2 += (float)num6 * this.GetDriver(inIndex).GetDriverStats().marketability / 5f;
				}
			}
			if (championshipEntry.races > 0)
			{
				num /= (float)championshipEntry.races;
				if (flag)
				{
					num2 /= (float)championshipEntry.races;
				}
			}
			float num7;
			if (flag)
			{
				num7 = num2 + num;
			}
			else
			{
				num7 = num;
			}
			this.fanBase = Mathf.Clamp(this.initialTotalFanBase + this.initialTotalFanBase * num7 * (this.GetMarketability() / 5f), 0.1f, 500f);
		}
	}

	public void IncreaseHistoryStat(History.HistoryStat inHistoryStat, int inIncrease = 1)
	{
		this.history.IncreaseStat(inHistoryStat, inIncrease);
	}

	public void IncreaseStaffHistoryStat(History.HistoryStat inHistoryStat, int inIncrease = 1)
	{
		List<Person> allEmployees = this.contractManager.GetAllEmployees();
		int count = allEmployees.Count;
		for (int i = 0; i < count; i++)
		{
			Person person = allEmployees[i];
			if (!(person is Driver) && !(person is Mechanic))
			{
				person.careerHistory.currentEntry.IncreaseStat(inHistoryStat, inIncrease);
			}
		}
	}

	public void IncreaseDriverHistoryStat(History.HistoryStat inHistoryStat, Driver inDriver, bool inIncludeMechanic = false, int inIncrease = 1)
	{
		if (inIncludeMechanic)
		{
			Mechanic mechanicOfDriver = this.GetMechanicOfDriver(inDriver);
			if (mechanicOfDriver != null)
			{
				mechanicOfDriver.careerHistory.currentEntry.IncreaseStat(inHistoryStat, inIncrease);
			}
		}
		inDriver.careerHistory.currentEntry.IncreaseStat(inHistoryStat, inIncrease);
	}

	public void ProcessSessionResults()
	{
		int highestRankedDriverPositionForTeam = Game.instance.sessionManager.GetHighestRankedDriverPositionForTeam(this);
		SessionDetails.SessionType sessionType = Game.instance.sessionManager.eventDetails.currentSession.sessionType;
		this.sponsorController.ProcessSessionResult(highestRankedDriverPositionForTeam);
		this.contractManager.ProcessSessionResult();
		if (sessionType == SessionDetails.SessionType.Race)
		{
			int positionForEvent = this.GetChampionshipEntry().GetPositionForEvent(this.championship.eventNumber);
			this.chairman.AddRaceHappiness(positionForEvent);
			if (this.chairman.hasMadeUltimatum)
			{
				this.chairman.ultimatum.positionAccomplished = highestRankedDriverPositionForTeam;
			}
			this.UpdateMarketability(positionForEvent);
			this.pitCrewController.OnRaceWeekendEnd();
		}
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null)
			{
				driver.OnSessionEnd();
			}
		}
		Engineer personOnJob = this.contractManager.GetPersonOnJob<Engineer>(Contract.Job.EngineerLead);
		personOnJob.OnSessionEnd();
		this.mMechanics.Clear();
		this.contractManager.GetAllMechanics(ref this.mMechanics);
		int count = this.mMechanics.Count;
		for (int j = 0; j < count; j++)
		{
			this.mMechanics[j].OnSessionEnd();
		}
		this.mMechanics.Clear();
	}

	private void UpdateMarketability(int inTeamPosition)
	{
		float num = Team.mMarketabilityModifier;
		int num2 = this.chairman.expectedTeamChampionshipResult - inTeamPosition;
		if (this.championship.eventNumber > 1)
		{
			int num3 = this.chairman.expectedTeamChampionshipResult - this.GetChampionshipEntry().GetCurrentChampionshipPosition();
			num = ((num2 >= 0 || num3 < 0) ? Team.mMarketabilityModifier : Team.mMarketabilityModifierKeptSameCSPosition);
		}
		float num4 = Mathf.Clamp01((float)Mathf.Abs(num2) / Team.mMarketabilityMaxPositionChange);
		float num5 = ((num2 >= 0) ? Team.mMaxMarketabilityChangePerEvent : Team.mMinMarketabilityChangePerEvent) * num4;
		this.AddToMarketebility(Mathf.Clamp(num5 * num, Team.mMinMarketabilityChangePerEvent, Team.mMaxMarketabilityChangePerEvent));
	}

	public Mechanic GetMechanic(int index)
	{
		this.mMechanics.Clear();
		this.contractManager.GetAllMechanics(ref this.mMechanics);
		if (index < this.mMechanics.Count)
		{
			return this.mMechanics[index];
		}
		return null;
	}

	public Mechanic GetMechanicOfDriver(Driver inDriver)
	{
		if (inDriver.IsReserveDriver())
		{
			inDriver = this.contractManager.GetDriverSittingOut();
		}
		this.mMechanics.Clear();
		this.contractManager.GetAllMechanics(ref this.mMechanics);
		int count = this.mMechanics.Count;
		for (int i = 0; i < count; i++)
		{
			Mechanic mechanic = this.mMechanics[i];
			Driver[] drivers = mechanic.GetDrivers();
			for (int j = 0; j < drivers.Length; j++)
			{
				if (drivers[j] == inDriver)
				{
					return mechanic;
				}
			}
		}
		return null;
	}

	public bool IsPlayersTeam()
	{
		return Game.IsActive() && this == Game.instance.player.team && !(this is NullTeam);
	}

	public void AddToMarketebility(float inValue)
	{
		this.marketability = Mathf.Clamp01(this.marketability + inValue);
	}

	public float GetMarketability()
	{
		bool flag = this.IsPlayersTeam() && Game.instance.player.playerBackStoryType == PlayerBackStory.PlayerBackStoryType.MotorsportLegend;
		return (!flag) ? this.marketability : Mathf.Clamp01(this.marketability + 0.5f);
	}

	public float GetCarMarketability(int inCarID)
	{
		if (this.championship.series == Championship.Series.EnduranceSeries)
		{
			Driver[] driversForCar = this.GetDriversForCar(inCarID);
			float num = 0f;
			for (int i = 0; i < driversForCar.Length; i++)
			{
				num += driversForCar[i].GetDriverStats().marketability;
			}
			return Mathf.Clamp01((driversForCar.Length <= 0) ? 0f : (num / (float)driversForCar.Length));
		}
		return this.GetDriver(inCarID).GetDriverStats().marketability;
	}

	public float GetTeamTotalMarketability()
	{
		return Mathf.Clamp01((this.GetCarMarketability(0) + this.GetCarMarketability(1) + this.GetMarketability()) / 3f);
	}

	private int GetSponsorAppeal()
	{
		int max = (!this.perksManager.CheckPerkUnlocked(TeamPerk.Type.SponsorsLevel5)) ? 4 : 5;
		return Mathf.Clamp(Mathf.RoundToInt(this.GetTeamTotalMarketability() * 5f), 1, max);
	}

	public int GetVacancyAppeal()
	{
		bool flag = this.contractManager.GetSlot(Contract.Job.TeamPrincipal).IsVacant();
		bool flag2 = this.contractManager.GetSlot(Contract.Job.TeamPrincipal).canPlayerApply();
		return (int)(this.teamPrincipal.jobSecurity + ((!flag2) ? 10 : 0) + ((!flag) ? 5 : 0));
	}

	public string GetPressureString()
	{
		switch (this.pressure)
		{
		case 1:
			return Localisation.LocaliseID("PSG_10001437", null);
		case 2:
			return Localisation.LocaliseID("PSG_10001438", null);
		case 3:
			return Localisation.LocaliseID("PSG_10001439", null);
		default:
			return Localisation.LocaliseID("PSG_10001437", null);
		}
	}

	public string GetReputationString()
	{
		if (this.reputation >= 80)
		{
			return "Great";
		}
		if (this.reputation >= 60)
		{
			return "Good";
		}
		if (this.reputation >= 40)
		{
			return "Average";
		}
		if (this.reputation >= 20)
		{
			return "Weak";
		}
		return "Bad";
	}

	public float GetChampionshipExpectation()
	{
		DatabaseEntry inWeightings = App.instance.database.teamExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Championship");
		return this.GetExpectation(inWeightings);
	}

	public float GetRaceExpectation(Circuit inCircuit)
	{
		DatabaseEntry inWeightings = App.instance.database.teamExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Race");
		return this.GetExpectation(inWeightings);
	}

	public float GetPersonExpectation(DatabaseEntry inWeightings, Person inPerson, float inExperienceWeighting, float inQualityWeighting, float inCostWeighting)
	{
		float num = inPerson.GetExperience() * inExperienceWeighting;
		num += inPerson.GetStatsValue() * inQualityWeighting;
		num += (float)inPerson.contract.GetMonthlyWageCost() / (float)inWeightings.GetIntValue("Monthly Wage Scalar") * inCostWeighting;
		return num / (inExperienceWeighting + inQualityWeighting + inCostWeighting);
	}

	public float GetTeamPrincipalExpectation(DatabaseEntry inWeightings)
	{
		Person personOnJob = this.contractManager.GetPersonOnJob(Contract.Job.TeamPrincipal);
		float inExperienceWeighting = (float)inWeightings.GetIntValue("Team Principal Experience") / 100f;
		float inQualityWeighting = (float)inWeightings.GetIntValue("Team Principal Quality") / 100f;
		float inCostWeighting = (float)inWeightings.GetIntValue("Team Principal Cost") / 100f;
		return this.GetPersonExpectation(inWeightings, personOnJob, inExperienceWeighting, inQualityWeighting, inCostWeighting);
	}

	public float GetDriverExpectation(DatabaseEntry inWeightings, Driver inDriver)
	{
		float inExperienceWeighting = (float)inWeightings.GetIntValue("Driver Experience") / 100f;
		float inQualityWeighting = (float)inWeightings.GetIntValue("Driver Quality") / 100f;
		float inCostWeighting = (float)inWeightings.GetIntValue("Driver Cost") / 100f;
		return this.GetPersonExpectation(inWeightings, inDriver, inExperienceWeighting, inQualityWeighting, inCostWeighting);
	}

	public float GetDriversExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		List<EmployeeSlot> allEmployeeSlotsForJob = this.contractManager.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
		for (int i = 0; i < allEmployeeSlotsForJob.Count; i++)
		{
			EmployeeSlot employeeSlot = allEmployeeSlotsForJob[i];
			if (employeeSlot.personHired != null)
			{
				num += this.GetDriverExpectation(inWeightings, employeeSlot.personHired as Driver);
			}
		}
		return num;
	}

	public float GetEngineerExpectation(DatabaseEntry inWeightings, Person inEngineer)
	{
		float inExperienceWeighting = (float)inWeightings.GetIntValue("Engineer Experience") / 100f;
		float inQualityWeighting = (float)inWeightings.GetIntValue("Engineer Quality") / 100f;
		float inCostWeighting = (float)inWeightings.GetIntValue("Engineer Cost") / 100f;
		return this.GetPersonExpectation(inWeightings, inEngineer, inExperienceWeighting, inQualityWeighting, inCostWeighting);
	}

	public float GetMechanicExpectation(DatabaseEntry inWeightings, Person inMechanic)
	{
		float inExperienceWeighting = (float)inWeightings.GetIntValue("Mechanic Experience") / 100f;
		float inQualityWeighting = (float)inWeightings.GetIntValue("Mechanic Quality") / 100f;
		float inCostWeighting = (float)inWeightings.GetIntValue("Mechanic Cost") / 100f;
		return this.GetPersonExpectation(inWeightings, inMechanic, inExperienceWeighting, inQualityWeighting, inCostWeighting);
	}

	public float GetEngineerLeadExpectation(DatabaseEntry inWeightings, Person inEngineerLead)
	{
		float inExperienceWeighting = (float)inWeightings.GetIntValue("Engineer Lead Experience") / 100f;
		float inQualityWeighting = (float)inWeightings.GetIntValue("Engineer Lead Quality") / 100f;
		float inCostWeighting = (float)inWeightings.GetIntValue("Engineer Lead Cost") / 100f;
		return this.GetPersonExpectation(inWeightings, inEngineerLead, inExperienceWeighting, inQualityWeighting, inCostWeighting);
	}

	public float GetEngineerLeadExpectation(DatabaseEntry inWeightings)
	{
		Person personOnJob = this.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		float num = 0f;
		if (personOnJob != null)
		{
			num += this.GetEngineerLeadExpectation(inWeightings, personOnJob);
		}
		return num;
	}

	public float GetMechanicsExpectation(DatabaseEntry inWeightings)
	{
		List<Person> allPeopleOnJob = this.contractManager.GetAllPeopleOnJob(Contract.Job.Mechanic);
		float num = 0f;
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			Person inMechanic = allPeopleOnJob[i];
			num += this.GetMechanicExpectation(inWeightings, inMechanic);
		}
		return num;
	}

	public float GetCarExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		float num2 = (float)inWeightings.GetIntValue("Car Quality") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Car Cost") / 100f;
		for (int i = 0; i < CarManager.carCount; i++)
		{
			Car car = this.carManager.GetCar(i);
			num += car.GetStats().GetStarsRating() / 5f * num2;
			num += num3;
		}
		return num / ((num2 + num3) * (float)CarManager.carCount);
	}

	public float GetHQExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		if (this.headquarters != null)
		{
			float num2 = (float)inWeightings.GetIntValue("Staff Count") / 100f;
			float num3 = (float)inWeightings.GetIntValue("HQ Value") / 100f;
			num += (float)this.headquarters.GetStaffCount() / (float)inWeightings.GetIntValue("Staff Count Scalar") * num2;
			num += (float)this.headquarters.GetHeadquartersValue() / (float)inWeightings.GetIntValue("HQ Value Scalar") * num3;
			num /= num2 + num3;
		}
		return num;
	}

	public float GetFinancialExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		float num2 = (float)inWeightings.GetIntValue("Financial Worth") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Available Funds") / 100f;
		num += (float)(this.financeController.worth / 1000000L) / (float)inWeightings.GetIntValue("Financial Worth Scalar") * num2;
		num += (float)(this.financeController.availableFunds / 100000L) / (float)inWeightings.GetIntValue("Available Funds Scalar") * num3;
		return num / (num2 + num3);
	}

	public float GetStaffExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		num += this.GetDriversExpectation(inWeightings);
		return num + this.GetEngineerLeadExpectation(inWeightings);
	}

	public float GetExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		float num2 = (float)inWeightings.GetIntValue("Financial") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Staff") / 100f;
		float num4 = (float)inWeightings.GetIntValue("Car") / 100f;
		float num5 = (float)inWeightings.GetIntValue("HQ") / 100f;
		num += this.GetFinancialExpectation(inWeightings) * num2;
		num += this.GetHQExpectation(inWeightings) * num5;
		num += this.GetCarExpectation(inWeightings) * num4;
		num += this.GetStaffExpectation(inWeightings) * num3;
		return num / (num2 + num5 + num4 + num3);
	}

	public void EndRaceUpdateMechanicsRelationshipWithDrivers(Driver inDriver, List<RaceEventResults.ResultData> inResultData, Championship inChampionship)
	{
		Mechanic mechanicOfDriver = this.GetMechanicOfDriver(inDriver);
		if (mechanicOfDriver != null)
		{
			int positionForChampionshipClass = RaceEventResults.GetPositionForChampionshipClass(inDriver, inResultData, inChampionship);
			mechanicOfDriver.EndRaceDriverRelationshipUpdate(inDriver, positionForChampionshipClass);
		}
	}

	public void CheckIfDriversPromisedAreFulfilled()
	{
		List<Driver> allPeopleOnJob = this.contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			allPeopleOnJob[i].personalityTraitController.CheckIfFulfilledAnyPromise();
		}
	}

	public void HandleEndOfSeason()
	{
		this.initialTotalFanBase = this.fanBase;
		this.HandleRetirements();
		if (!this.IsPlayersTeam())
		{
			this.teamAIController.HandleEndOfSeason();
		}
		this.chairman.OnSeasonEnd();
		this.pitCrewController.OnSeasonEnd();
	}

	public void HandleRumours(TeamRumourManager inTeamRumourManager)
	{
		DateTime now = Game.instance.time.now;
		ChampionshipEntry_v1 championshipEntry = this.GetChampionshipEntry();
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null)
			{
				if (driver.WantsToRetire(now, driver.GetImprovementRate()))
				{
					inTeamRumourManager.AddRumour(new TeamRumour
					{
						mDate = now,
						mPerson = driver,
						mType = TeamRumour.Type.Retiring
					});
				}
				else if (driver.WantsToLeave())
				{
					inTeamRumourManager.AddRumour(new TeamRumour
					{
						mDate = now,
						mPerson = driver,
						mType = TeamRumour.Type.WantsToLeave
					});
				}
				Mechanic mechanicOfDriver = this.GetMechanicOfDriver(driver);
				if (mechanicOfDriver != null && championshipEntry != null && championshipEntry.races >= Person.minRacesBeforeThinkingAboutJobChange && mechanicOfDriver.GetRelationshipAmmount() < 15f && mechanicOfDriver.GetRelationshipWeeksTogether() > 20)
				{
					inTeamRumourManager.AddRumour(new TeamRumour
					{
						mDate = now,
						mPerson = mechanicOfDriver,
						mType = TeamRumour.Type.UnhappyWithTeammate
					});
				}
			}
		}
	}

	public void OnDayEnd(TeamRumourManager inTeamRumourManager)
	{
		if (!Game.instance.player.IsUnemployed())
		{
			this.HandleRumours(inTeamRumourManager);
		}
		if (!this.IsPlayersTeam())
		{
			this.teamAIController.OnDayEnd();
		}
		this.UpdateFanBase();
	}

	public void OnPreSeasonStart()
	{
		this.carManager.nextYearCarDesign.state = NextYearCarDesign.State.WaitingForDesign;
		if (!this.IsPlayersTeam())
		{
			this.teamAIController.OnPreSeasonStart();
		}
		this.carManager.partImprovement.RemoveAllPartImprove(CarPartStats.CarPartStat.Performance);
		this.carManager.partImprovement.RemoveAllPartImprove(CarPartStats.CarPartStat.Reliability);
	}

	public void HandleDriverRetirementRequest(Driver aDriver)
	{
		if (!Game.instance.player.IsUnemployed())
		{
			Game.instance.dialogSystem.OnRetiredMessages(aDriver);
		}
		this.contractManager.FireDriver(aDriver, Contract.ContractTerminationType.IsRetiring);
		aDriver.Retire();
		this.contractManager.HireReplacementDriver();
		Driver driver = App.instance.regenManager.CreateDriver(RegenManager.RegenType.Random);
		if (driver != null && !aDriver.joinsAnySeries)
		{
			driver.SetPreferedSeries(aDriver.preferedSeries);
		}
		global::Debug.Log(aDriver.name + " has retired -> " + driver.name + " added to Game ( Driver )", null);
	}

	public void HandleEngineerRetirementRequest(Engineer aEngineer)
	{
		Game.instance.dialogSystem.OnRetiredMessages(aEngineer);
		this.contractManager.FirePerson(aEngineer, Contract.ContractTerminationType.Generic);
		aEngineer.Retire();
		this.contractManager.HireReplacementEngineer();
		Engineer engineer = App.instance.regenManager.CreateEngineer(RegenManager.RegenType.Random);
		global::Debug.Log(aEngineer.name + " has retired -> " + engineer.name + " added to Game ( Engineer )", null);
	}

	public void HandleMechanicRetirementRequest(Mechanic aMechanic)
	{
		Game.instance.dialogSystem.OnRetiredMessages(aMechanic);
		this.contractManager.FirePerson(aMechanic, Contract.ContractTerminationType.Generic);
		aMechanic.Retire();
		this.contractManager.HireReplacementMechanic();
		Mechanic mechanic = App.instance.regenManager.CreateMechanic(RegenManager.RegenType.Random);
		global::Debug.Log(aMechanic.name + " has retired -> " + mechanic.name + " added to Game ( Mechanic )", null);
	}

	public void HandleTeamPrincipalRetirementRequest(TeamPrincipal aTeamPrincipal)
	{
		Game.instance.dialogSystem.OnRetiredMessages(aTeamPrincipal);
		this.contractManager.FirePerson(aTeamPrincipal, Contract.ContractTerminationType.Generic);
		aTeamPrincipal.Retire();
		this.contractManager.HireReplacementTeamPrincipal();
		TeamPrincipal teamPrincipal = App.instance.regenManager.CreateTeamPrincipal(RegenManager.RegenType.Random);
		global::Debug.Log(aTeamPrincipal.name + " has retired -> " + teamPrincipal.name + " added to Game ( TeamPrincipal )", null);
	}

	public void HandleChairmanRetirementRequest(Chairman aChairman)
	{
		StringVariableParser.subjectPreviousTeam = aChairman.contract.GetTeam();
		this.contractManager.FirePerson(aChairman, Contract.ContractTerminationType.Generic);
		aChairman.Retire();
		Chairman chairman = this.contractManager.HireReplacementChairman();
		if (this.IsPlayersTeam())
		{
			chairman.playerChosenExpectedTeamChampionshipPosition = aChairman.playerChosenExpectedTeamChampionshipPosition;
		}
		Game.instance.dialogSystem.OnRetiredMessages(aChairman);
		global::Debug.Log(aChairman.name + " has retired -> " + chairman.name + " added to Game ( Chairman )", null);
	}

	public void HandleTeamAssistantRetirementRequest(Assistant inTeamAssistant)
	{
		Game.instance.dialogSystem.OnRetiredMessages(inTeamAssistant);
		this.contractManager.FirePerson(inTeamAssistant, Contract.ContractTerminationType.Generic);
		inTeamAssistant.Retire();
		Assistant assistant = this.contractManager.HireReplacementTeamAssistant(null);
		global::Debug.Log(inTeamAssistant.name + " has retired -> " + assistant.name + " added to Game ( Team Assistant )", null);
	}

	public void HandleScoutRetirementRequest(Scout inScout)
	{
		Game.instance.dialogSystem.OnRetiredMessages(inScout);
		this.contractManager.FirePerson(inScout, Contract.ContractTerminationType.Generic);
		inScout.Retire();
		Scout scout = this.contractManager.HireReplacementScout(null);
		global::Debug.Log(inScout.name + " has retired -> " + scout.name + " added to Game ( Scout )", null);
	}

	public void HandleRetirements()
	{
		DateTime now = Game.instance.time.now;
		List<Driver> allPeopleOnJob = this.contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
		foreach (Driver driver in allPeopleOnJob)
		{
			if (driver == null)
				global::Debug.LogErrorFormat("HandleRetirements for Team {0}: Driver is missing", new object[] { this.GetShortName(false) });
			else
				if (driver.WantsToRetire(now, driver.GetImprovementRate()))
					this.HandleDriverRetirementRequest(driver);
		}
		Engineer personOnJob = this.contractManager.GetPersonOnJob<Engineer>(Contract.Job.EngineerLead);
		if (personOnJob == null)
			global::Debug.LogErrorFormat("HandleRetirements for Team {0}: EngineerLead is missing", new object[] { this.GetShortName(false) });
		else {
			if (personOnJob.WantsToRetire(now, personOnJob.improvementRate))
				this.HandleEngineerRetirementRequest(personOnJob);
		}
		List<Mechanic> allPeopleOnJob2 = this.contractManager.GetAllPeopleOnJob<Mechanic>(Contract.Job.Mechanic);
		foreach (Mechanic mechanic in allPeopleOnJob2)
		{
			if (mechanic == null)
				global::Debug.LogErrorFormat("HandleRetirements for Team {0}: Mechanic is missing", new object[] { this.GetShortName(false) });
			else {
				if (mechanic.WantsToRetire(now, mechanic.improvementRate))
					this.HandleMechanicRetirementRequest(mechanic);
			}
		}
		if (this.chairman == null)
			global::Debug.LogErrorFormat("HandleRetirements for Team {0}: Chairman is missing", new object[] { this.GetShortName(false) });
		else {
			if (this.chairman.WantsToRetire(now, 0f))
				this.HandleChairmanRetirementRequest(this.chairman);
		}
		Scout personOnJob2 = this.contractManager.GetPersonOnJob<Scout>(Contract.Job.Scout);
		if (personOnJob2 == null)
			global::Debug.LogErrorFormat("HandleRetirements for Team {0}: Scout is missing", new object[] { this.GetShortName(false) });
		else {
			if (personOnJob2.WantsToRetire(now, 0f))
				this.HandleScoutRetirementRequest(personOnJob2);
		}
		Assistant personOnJob3 = this.contractManager.GetPersonOnJob<Assistant>(Contract.Job.TeamAssistant);
		if (personOnJob3 == null)
			global::Debug.LogErrorFormat("HandleRetirements for Team {0}: TeamAssistant is missing", new object[] { this.GetShortName(false) });
		else {
			if (personOnJob3.WantsToRetire(now, 0f))
				this.HandleTeamAssistantRetirementRequest(personOnJob3);
		}
		if (this.teamPrincipal == null)
			global::Debug.LogErrorFormat("HandleRetirements for Team {0}: TeamPrincipal is missing", new object[] { this.GetShortName(false) });
		else {
			if (!this.IsPlayersTeam() && this.teamPrincipal.WantsToRetire(now, 0f))
				this.HandleTeamPrincipalRetirementRequest(this.teamPrincipal);
		}
	}

	public void StoreTeamDataBeforeEvent()
	{
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null)
			{
				driver.statsBeforeEvent = new DriverStats(driver.GetDriverStats());
				driver.moraleBeforeEvent = driver.GetMorale();
				Mechanic mechanicOfDriver = this.GetMechanicOfDriver(driver);
				if (mechanicOfDriver != null)
				{
					mechanicOfDriver.driverRelationshipAmountBeforeEvent = mechanicOfDriver.GetRelationshipAmmount();
				}
			}
		}
		this.chairman.happinessBeforeEvent = this.chairman.GetHappiness();
		this.marketabilityBeforeEvent = this.GetTeamTotalMarketability();
		Circuit circuit = this.championship.GetCurrentEventDetails().circuit;
		this.expectedRacePosition = Game.instance.teamManager.CalculateExpectedPositionForRace(this, circuit);
	}

	private void OnHQsBuildingNotification(HQsBuilding_v1.NotificationState inNotificationState, HQsBuilding_v1 inBuilding)
	{
		if (inBuilding.team != null && inBuilding.team.IsPlayersTeam())
		{
			this.CheckIfDriversPromisedAreFulfilled();
		}
	}

	public float GetDriverQuality()
	{
		float result = 0f;
		for (int i = 0; i < Team.mainDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null)
			{
				result = driver.GetDriverStats().GetUnitAverage();
			}
		}
		return result;
	}

	public float GetRelevantStatsSumForCircuit(Circuit inCircuit)
	{
		float num = 0f;
		foreach (CarPart.PartType partType2 in CarPart.GetPartType(this.championship.series, false))
		{
			if (inCircuit.GetRelevancy(CarPart.GetStatForPartType(partType2)) == CarStats.RelevantToCircuit.VeryUseful)
			{
				num += this.carManager.partInventory.GetHighestStatOfType(partType2, CarPartStats.CarPartStat.MainStat);
			}
		}
		return num;
	}

	public bool AllDriversScouted()
	{
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null && !driver.CanShowStats())
			{
				return false;
			}
		}
		return true;
	}

	public string GetTeamStartDescription()
	{
		if (!string.IsNullOrEmpty(this.mCustomStartDescription) && this.mCustomStartDescription != "0")
		{
			return this.mCustomStartDescription;
		}
		return Localisation.LocaliseID(this.startDescription, null);
	}

	public void SetTeamStartDescriptionID(string inID)
	{
		this.startDescription = inID;
	}

	public void ReceiveChampionshipPromotionBonus(Action inTransactionSuccess, Action inTransactionFail)
	{
		Championship nextTierChampionship = this.championship.GetNextTierChampionship();
		if (nextTierChampionship.rules.promotionBonus)
		{
			Transaction transaction = new Transaction(Transaction.Group.PrizeMoney, Transaction.Type.Credit, GameStatsConstants.promotionBonus, "Promotion Bonus");
			this.financeController.finance.ProcessTransactions(inTransactionSuccess, inTransactionFail, true, new Transaction[]
			{
				transaction
			});
		}
		else if (inTransactionSuccess != null)
		{
			inTransactionSuccess();
		}
	}

	public void ReceiveAllChairmanPayments(Chairman.EstimatedPosition inPosition, Action inTransactionSucess, Action inTransactionFail, bool inPlayerInteraction = true)
	{
		if (this.canReceiveAllRaceChairmanPayments())
		{
			long num = (long)MathsUtility.RoundToNearestThousand((int)this.financeController.GetRacePaymentValue((TeamFinanceController.RacePaymentType)inPosition) * this.championship.eventsLeft);
			Transaction transaction = new Transaction(Transaction.Group.ChairmanPayments, Transaction.Type.Credit, num, Localisation.LocaliseID("PSG_10010565", null));
			this.financeController.finance.ProcessTransactions(inTransactionSucess, inTransactionFail, inPlayerInteraction, new Transaction[]
			{
				transaction
			});
			this.financeController.fullChairmanFunds = num;
		}
	}

	public void ApplyChallengeBonusToCarParts()
	{
		if (!Game.instance.challengeManager.IsAttemptingChallenge())
		{
			return;
		}
		float partBonusAITeams = Game.instance.challengeManager.currentChallenge.partBonusAITeams;
		List<CarPart> allParts = this.carManager.partInventory.GetAllParts();
		for (int i = 0; i < allParts.Count; i++)
		{
			CarPart carPart = allParts[i];
			if (!carPart.isSpecPart && !carPart.isBanned)
			{
				carPart.stats.AddToStat(CarPartStats.CarPartStat.MainStat, partBonusAITeams);
			}
		}
	}

	public bool AnyDriverWithTraitSpecialCase(PersonalityTrait.SpecialCaseType inCase, out Driver outDriver)
	{
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = this.GetDriver(i);
			if (driver != null && driver.personalityTraitController.HasSpecialCase(inCase))
			{
				outDriver = driver;
				return true;
			}
		}
		outDriver = null;
		return false;
	}

	private void CreateSelectedDrivers()
	{
		this.mSelectedSessionDrivers.Clear();
		this.mVehicleSessionDrivers.Clear();
		for (int i = 0; i < CarManager.carCount; i++)
		{
			this.mSelectedSessionDrivers.Add(i, new List<Driver>());
			this.mVehicleSessionDrivers.Add(i, null);
		}
	}

	public void ValidateSelectedDrivers()
	{
		if (this.mSelectedDriver != null)
		{
			Driver[] array = this.mSelectedDriver;
			this.mSelectedDriver = null;
			this.mSelectedSessionDrivers.Clear();
			this.mVehicleSessionDrivers.Clear();
			for (int i = 0; i < CarManager.carCount; i++)
			{
				Driver driver = (i >= array.Length || array[i] == null) ? this.GetDriversForCar(i)[0] : array[i];
				this.mSelectedSessionDrivers.Add(i, new List<Driver>());
				this.mSelectedSessionDrivers[i].Add(driver);
				this.mVehicleSessionDrivers.Add(i, driver);
			}
		}
	}

	public bool isCreatedAndManagedByPlayer()
	{
		return this.IsPlayersTeam() && this.isCreatedByPlayer;
	}

	public bool isCreatedAndManagedByPlayerFirstYear()
	{
		return this.IsPlayersTeam() && this.isCreatedByPlayer && Game.instance.time.now.Year == 2016;
	}

	public bool canReceiveAllRaceChairmanPayments()
	{
		return this.isCreatedAndManagedByPlayer() && this.canReceiveFullChairmanPayments;
	}

	public void SetShortName(string inShortName)
	{
		this.mShortName = inShortName;
	}

	public string GetShortName(bool inActualShortName = false)
	{
		if ((!inActualShortName && this.mShortName == string.Empty) || this.isCreatedByPlayer)
		{
			return this.name;
		}
		return this.mShortName;
	}

	public string GetVehicleNameSession(Driver inActiveDriver)
	{
		if (this.championship.series == Championship.Series.EnduranceSeries)
		{
			Driver[] driversForCar = this.GetDriversForCar(inActiveDriver.carID);
			string text = inActiveDriver.lastName + "\n<alpha=#50>";
			bool flag = false;
			for (int i = 0; i < driversForCar.Length; i++)
			{
				if (driversForCar[i] != inActiveDriver)
				{
					text += driversForCar[i].threeLetterLastName;
					if (!flag)
					{
						flag = true;
						text += " / ";
					}
				}
			}
			return text;
		}
		return inActiveDriver.shortName;
	}

	public string GetCarName(Driver[] inDrivers, params Driver[] inActiveDriver)
	{
		if (this.championship.series == Championship.Series.EnduranceSeries)
		{
			Driver[] array = (inDrivers != null) ? inDrivers : this.GetDriversForCar(inActiveDriver[0].carID);
			string text = string.Empty;
			for (int i = 0; i < array.Length; i++)
			{
				bool flag = false;
				for (int j = 0; j < inActiveDriver.Length; j++)
				{
					if (array[i] == inActiveDriver[j])
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					text += array[i].lastName;
				}
				else
				{
					text += string.Format("<alpha=#50>{0}<alpha=#FF>", array[i].threeLetterLastName);
				}
				if (i < array.Length - 1)
				{
					text += " <alpha=#50>/<alpha=#FF> ";
				}
			}
			return text;
		}
		return inActiveDriver[0].lastName;
	}

	public int GetChampionshipRang() {
		int teamLastRank = 12;
		if (this.history.HasPreviousSeasonHistory()) {
			teamLastRank = this.history.previousSeasonTeamResult;
		}
		return teamLastRank;
	}

	public Person teamAssistant
	{
		get
		{
			return this.contractManager.GetPersonOnJob(Contract.Job.TeamAssistant);
		}
	}

	public Chairman chairman
	{
		get
		{
			return this.contractManager.GetPersonOnJob(Contract.Job.Chairman) as Chairman;
		}
	}

	public TeamPrincipal teamPrincipal
	{
		get
		{
			return this.contractManager.GetPersonOnJob(Contract.Job.TeamPrincipal) as TeamPrincipal;
		}
	}

	public Driver[] selectedDrivers
	{
		get
		{
			return this.mSelectedDriver;
		}
	}

	public int sponsorAppeal
	{
		get
		{
			return this.GetSponsorAppeal();
		}
	}

	public string twitterHandle
	{
		get
		{
			return this.name.Replace(" ", string.Empty);
		}
	}

	public string customStartDescription
	{
		set
		{
			this.mCustomStartDescription = value;
		}
	}

	public bool HasAIPitcrew
	{
		get
		{
			return this.pitCrewController.AIPitCrew != null;
		}
	}

	public const int invalidTeamID = -1;

	public static int mainDriverCount = 2;

	public static int maxDriverCount = 6;

	public static readonly int mMinRacesBeforeStaffChangeAllowed;

	public static readonly float mMinMarketabilityChangePerEvent = -0.05f;

	public static readonly float mMaxMarketabilityChangePerEvent = 0.05f;

	public static readonly float mMarketabilityModifier = 1f;

	public static readonly float mMarketabilityModifierKeptSameCSPosition = 0.1f;

	public static readonly float mMarketabilityMaxPositionChange = 5f;

	public Nationality nationality = new Nationality();

	public TeamFinanceController financeController = new TeamFinanceController();

	public YoungDriverProgramme youngDriverProgramme = new YoungDriverProgramme();

	public History history = new History();

	public CarManager carManager = new CarManager();

	public Championship championship;

	public TeamPerkManager perksManager = new TeamPerkManager();

	public SponsorController sponsorController = new SponsorController();

	public ContractManagerTeam contractManager = new ContractManagerTeam();

	public TeamStatistics teamStatistics = new TeamStatistics();

	public TeamAIController teamAIController = new TeamAIController();

	public Headquarters headquarters;

	public TeamAIWeightings aiWeightings;

	public TeamLogo customLogo = new TeamLogo();

	public Investor investor;

	public PitCrewController pitCrewController = new PitCrewController();

	public int teamID;

	public string locationID = string.Empty;

	public int reputation;

	public float marketability;

	public int pressure;

	public float fanBase;

	public float aggression;

	public float initialTotalFanBase;

	public int colorID;

	public int liveryID;

	public int driversHatStyle;

	public int driversBodyStyle;

	public int startOfSeasonExpectedChampionshipResult = RandomUtility.GetRandom(1, 18);

	public int rulesBrokenThisSeason;

	public bool isBlockedByChallenge;

	public bool isCreatedByPlayer;

	public List<PoliticalVote.TeamCharacteristics> votingCharacteristics = new List<PoliticalVote.TeamCharacteristics>();

	public int votingPower;

	public Team rivalTeam;

	public float marketabilityBeforeEvent;

	public int expectedRacePosition;

	public bool canRequestFunds = true;

	public bool canReceiveFullChairmanPayments;

	private string startDescription = string.Empty;

	private string mCustomStartDescription = string.Empty;

	private string mShortName = string.Empty;

	private ChampionshipEntry_v1 mChampionshipEntry;

	private Driver[] mSelectedDriver;

	private Dictionary<int, List<Driver>> mSelectedSessionDrivers = new Dictionary<int, List<Driver>>();

	private Dictionary<int, Driver> mVehicleSessionDrivers = new Dictionary<int, Driver>();

	private int mCurrentExpectedChampionshipResult = RandomUtility.GetRandom(1, 18);

	private List<Mechanic> mMechanics = new List<Mechanic>();

	private List<Driver> mDrivers;

	[NonSerialized]
	private List<Driver> mDriversCache = new List<Driver>();

	private List<EmployeeSlot> mEmployeeSlots = new List<EmployeeSlot>();
}
