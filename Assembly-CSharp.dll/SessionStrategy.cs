using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class SessionStrategy : InstanceCounter
{
	public SessionStrategy()
	{
	}

	public void Start(RacingVehicle inVehicle)
	{
		this.mVehicle = inVehicle;
		this.mTargetPointSteeringBehaviour = this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>();
		Circuit circuit = Game.instance.sessionManager.eventDetails.circuit;
		TyreSet.Compound inCompound = TyreSet.Compound.Soft;
		TyreSet.Compound inCompound2 = TyreSet.Compound.Medium;
		TyreSet.Compound inCompound3 = TyreSet.Compound.Hard;
		this.mChampionship = this.mVehicle.driver.contract.GetTeam().championship;
		ChampionshipRules rules = this.mChampionship.rules;
		if (circuit != null)
		{
			inCompound = circuit.firstTyreOption;
			inCompound2 = circuit.secondTyreOption;
			inCompound3 = circuit.thirdTyreOption;
		}
		this.CreateTyres(ref this.mFirstTyreOption, inCompound, SessionStrategy.TyreOption.First, Game.instance.persistentEventData.GetTyreCountForOption(this.mVehicle, SessionStrategy.TyreOption.First));
		this.CreateTyres(ref this.mSecondTyreOption, inCompound2, SessionStrategy.TyreOption.Second, Game.instance.persistentEventData.GetTyreCountForOption(this.mVehicle, SessionStrategy.TyreOption.Second));
		if (rules.compoundsAvailable > 2)
		{
			this.CreateTyres(ref this.mThirdTyreOption, inCompound3, SessionStrategy.TyreOption.Third, Game.instance.persistentEventData.GetTyreCountForOption(this.mVehicle, SessionStrategy.TyreOption.Third));
		}
		int wetWeatherTyreCount = DesignDataManager.instance.tyreData.wetWeatherTyreCount;
		this.CreateTyres(ref this.mIntermediates, TyreSet.Compound.Intermediate, SessionStrategy.TyreOption.Intermediates, wetWeatherTyreCount);
		this.CreateTyres(ref this.mWets, TyreSet.Compound.Wet, SessionStrategy.TyreOption.Wets, wetWeatherTyreCount);
		this.mLockedTyres = Game.instance.sessionManager.raceDirector.tyreConfiscationDirector.GetLockedTyres(this.mVehicle);
		if (!Game.IsSimulatingSeason && Game.instance.sessionManager.IsPlayerChampionship())
		{
			Game.instance.persistentEventData.LoadTyreData(this.mVehicle);
		}
		this.CreateTyreStrategyOptions();
		this.mUsesAIForStrategy = this.mVehicle.driver.personalityTraitController.UsesAIForStrategy(this.mVehicle);
	}

	public void OnLoad()
	{
		this.mRefreshCarCrashData = true;
		this.CreateTyreStrategyOptions();
		this.mUsesAIForStrategy = this.mVehicle.driver.personalityTraitController.UsesAIForStrategy(this.mVehicle);
	}

	public void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		if (Game.instance.sessionManager.eventDetails.currentSession.sessionType == SessionDetails.SessionType.Race)
		{
			if (inGateID % 5 == 0)
			{
				this.UpdateDrivingStyleAndEngineModes();
			}
			if (this.mVehicle.pathController.GetCurrentPath().pathType == PathController.PathType.Track)
			{
				int num = Game.instance.sessionManager.circuit.pitlaneEntryTrackPathID - 5;
				if (inGateID == num || (inGateType == PathData.GateType.Sector && inGateID != 0))
				{
					if (inGateID == num)
					{
						RacingVehicle vehicleTeamMate = Game.instance.vehicleManager.GetVehicleTeamMate(this.mVehicle);
						if (!this.mVehicle.performance.IsExperiencingCriticalIssue() && (!vehicleTeamMate.isPlayerDriver || Game.instance.sessionManager.isUsingAIForPlayerDrivers) && !vehicleTeamMate.behaviourManager.isOutOfRace && vehicleTeamMate.pathState.IsInPitlaneArea() && vehicleTeamMate.setup.PitTimeLeft() > 20f)
						{
							this.CancelPit();
							return;
						}
					}
					if (Game.instance.sessionManager.GetLapsRemaining() > 1 && (!this.mVehicle.isPlayerDriver || Game.instance.sessionManager.isUsingAIForPlayerDrivers) && this.DoesVehicleNeedToPit(inGateID == num) && !Game.instance.sessionManager.isRollingOut)
					{
						this.PlanPitstop();
					}
				}
			}
		}
		else if (this.mVehicle.pathController.GetCurrentPath().pathType == PathController.PathType.Track)
		{
			int num2 = Game.instance.sessionManager.circuit.pitlaneEntryTrackPathID - 5;
			if (inGateType == PathData.GateType.Sector || inGateID == num2)
			{
				bool flag = false;
				if (this.mVehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.InOutLap && this.mVehicle.timer.HasSetLapTime())
				{
					PrefGameAIStrategyDifficulty.Type aistrategyDifficulty = App.instance.preferencesManager.gamePreferences.GetAIStrategyDifficulty();
					if (aistrategyDifficulty == PrefGameAIStrategyDifficulty.Type.Realistic && !Game.instance.challengeManager.IsAttemptingChallenge())
					{
						if (this.mVehicle.strategy.ShouldPitForDifferentTyreTread(this.mVehicle.setup.currentSetup.tyreSet, 0f, true))
						{
							flag = true;
						}
					}
					else if (!this.mVehicle.sessionAIOrderController.IsDriverOnIdealTyreTread())
					{
						flag = true;
					}
				}
				if ((this.HasCompletedOrderedLapCount() || flag || this.mVehicle.setup.tyreSet.GetCondition() < 0.2f) && !this.mVehicle.strategy.IsGoingToPit() && (!this.mVehicle.isPlayerDriver || Game.instance.sessionManager.isUsingAIForPlayerDrivers))
				{
					this.ReturnToGarage();
				}
			}
		}
	}

	private bool DoesVehicleNeedToPit(bool inIsPitlaneEntryGate)
	{
		if (this.mVehicle.behaviourManager.isOutOfRace)
		{
			return false;
		}
		int lapsRemaining = Game.instance.sessionManager.GetLapsRemaining();
		if (lapsRemaining <= 1 || (Game.instance.sessionManager.championship.series == Championship.Series.EnduranceSeries && lapsRemaining <= 3))
		{
			return false;
		}
		float mAggressiveness = this.mVehicle.driver.contract.GetTeam().aiWeightings.mAggressiveness;
		float num = this.CalculateMinimumDistanceRequiredForNextValidPitEntry();
		float time = Game.instance.sessionManager.time;
		TyreSet tyreSet = this.mVehicle.setup.currentSetup.tyreSet;
		float optimalTyreDistance = tyreSet.GetOptimalTyreDistance();
		float trackLengthMiles = Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles;
		float num2 = GameUtility.MilesToMeters(trackLengthMiles);
		float num3 = optimalTyreDistance / num2;
		float num4 = num;
		if (this.mStintStrategyType == SessionStrategy.StintStrategyType.Conservative)
		{
			CircuitScene circuit = Game.instance.sessionManager.circuit;
			num4 += circuit.GetTrackPath().data.length;
		}
		if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries && time < 900f && time > 300f && tyreSet.GetCondition() < 0.5f)
		{
			if (num3 < (float)lapsRemaining)
			{
				this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitTyreChange;
				return true;
			}
		}
		else if (optimalTyreDistance < num4 && lapsRemaining > 2)
		{
			this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitTyreChange;
			return true;
		}
		if (tyreSet.hasLostWheel || tyreSet.hasWrongCompoundFitted)
		{
			this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitTyreChange;
			return true;
		}
		PrefGameAIStrategyDifficulty.Type aistrategyDifficulty = App.instance.preferencesManager.gamePreferences.GetAIStrategyDifficulty();
		if (aistrategyDifficulty == PrefGameAIStrategyDifficulty.Type.Realistic && !Game.instance.challengeManager.IsAttemptingChallenge() && lapsRemaining > 2)
		{
			TyrePerformanceDesignData tyrePerformanceData = DesignDataManager.instance.GetDesignData().GetTyrePerformanceData(SessionStrategy.TyreOption.First);
			int num5 = (tyrePerformanceData.lowPerformanceLapCount + tyrePerformanceData.mediumPerformanceLapCount + tyrePerformanceData.highPerformanceLapCount) / 2;
			float random = RandomUtility.GetRandom01();
			bool flag = random < 0.2f && lapsRemaining == num5 + 1;
			bool flag2 = random < 0.8f && lapsRemaining == num5;
			bool flag3 = lapsRemaining == num5 - 1;
			if (flag || flag2 || flag3)
			{
				float num6 = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles);
				float num7 = num6 * (float)Mathf.Max(0, lapsRemaining - 1);
				if (tyreSet.GetOptimalTyreDistance() < num7)
				{
					this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitTyreChange;
					return true;
				}
			}
		}
		if (inIsPitlaneEntryGate && lapsRemaining > 1 && this.ShouldPitForDifferentTyreTread(tyreSet, 0f, true))
		{
			int num8 = Game.instance.sessionManager.lap;
			RacingVehicle leader = Game.instance.sessionManager.GetLeader();
			if (leader != null && leader.pathController.distanceAlongTrackPath01 > 0.75f)
			{
				num8++;
			}
			TyreSet.Tread tread = tyreSet.GetTread();
			TyreSet.Tread recommendedTreadForLap = SessionStrategy.GetRecommendedTreadForLap(num8 - 1);
			TyreSet.Tread recommendedTreadForLap2 = SessionStrategy.GetRecommendedTreadForLap(num8);
			TyreSet.Tread recommendedTreadForLap3 = SessionStrategy.GetRecommendedTreadForLap(num8 + 1);
			float random2 = RandomUtility.GetRandom01();
			bool flag4 = tread != TyreSet.Tread.Slick && this.mVehicle.standingsPosition > 5 && tread == recommendedTreadForLap2 && tread != recommendedTreadForLap3 && random2 < 0.2f;
			bool flag5 = tread != recommendedTreadForLap2 && random2 < 0.8f;
			bool flag6 = num8 > 1 && recommendedTreadForLap != tread && recommendedTreadForLap2 != tread;
			if (flag4 || flag5 || flag6)
			{
				TyreSet.Tread treadWithLeastTimeCost = this.mVehicle.performance.tyrePerformance.GetTreadWithLeastTimeCost();
				if (tread != treadWithLeastTimeCost)
				{
					this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitTyreThreadChange;
					return true;
				}
			}
		}
		ChampionshipRules rules = this.mVehicle.championship.rules;
		if (rules.isRefuelingOn)
		{
			float fuelDistance = this.mVehicle.performance.fuel.GetFuelDistance();
			if (fuelDistance < num || (fuelDistance <= 1f && lapsRemaining > 1))
			{
				this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitRefuel;
				return true;
			}
		}
		if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries)
		{
			float num10 = this.mVehicle.championship.rules.drivingTimeEndurance - 0.03f;
			bool flag7 = !this.mVehicle.timer.IsDriverAbleToFinishTimedRace(this.mVehicle.driver) && (this.mVehicle.driver.driverStamina.IsInDangerZone() || this.mVehicle.timer.GetNormalizedTimeDriven(this.mVehicle.driver) > num10);
			if (flag7)
			{
				Driver driverSwapTargetDriver = this.GetDriverSwapTargetDriver();
				if (driverSwapTargetDriver != null && driverSwapTargetDriver != this.mVehicle.driver)
				{
					this.mReasonForPreviousPit = SessionStints.ReasonForStint.DriverSwap;
					return true;
				}
			}
		}
		if ((Game.instance.sessionManager.raceDirector.retirementDirector.ShouldFixParts(this.mVehicle) || this.mVehicle.performance.carConditionPerformance.isExperiencingCriticalIssue) && this.mVehicle.championship.series != Championship.Series.EnduranceSeries)
		{
			if (lapsRemaining >= 2 || (lapsRemaining < 2 && mAggressiveness <= 0.6f))
			{
				for (int i = 0; i < this.mVehicle.car.seriesCurrentParts.Length; i++)
				{
					CarPart carPart = this.mVehicle.car.seriesCurrentParts[i];
					if (carPart.partCondition.condition <= 0.1f)
					{
						// most part types cant be repaired
						switch (carPart.GetPartType())
                        {
							case CarPart.PartType.Engine:
							case CarPart.PartType.Brakes:
							case CarPart.PartType.Gearbox:
							case CarPart.PartType.Suspension:
								break;
							default:
								this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitConditionFix;
								return true;
						}
					}
				}
			}
		}
		else if (Game.instance.sessionManager.raceDirector.retirementDirector.ShouldFixParts(this.mVehicle) && this.mVehicle.championship.series == Championship.Series.EnduranceSeries && (time >= 900f || (time >= 720f && mAggressiveness <= 0.6f)))
		{
			for (int j = 0; j < this.mVehicle.car.seriesCurrentParts.Length; j++)
			{
				CarPart carPart2 = this.mVehicle.car.seriesCurrentParts[j];
				if (carPart2.partCondition.condition <= 0.1f)
				{
					this.mReasonForPreviousPit = SessionStints.ReasonForStint.PitConditionFix;
					global::Debug.Log(this.mVehicle.driver.name + " Pitted To Fix Part with " + (time / 60f).ToString() + " to go.", null);
					return true;
				}
			}
		}
		return false;
	}

	private float CalculateMinimumDistanceRequiredForNextValidPitEntry()
	{
		CircuitScene circuit = Game.instance.sessionManager.circuit;
		int num = circuit.pitlaneEntryTrackPathID - 5;
		int id = this.mVehicle.pathController.GetPreviousGate().id;
		float num2 = PathUtility.GetDistanceBetweenGates(this.mVehicle.pathController.GetCurrentPathData(), id, num);
		num2 += PathUtility.GetDistanceBetweenGates(this.mVehicle.pathController.GetCurrentPathData(), num, 0);
		return num2 + circuit.GetTrackPath().data.length;
	}

	private void PlanPitstop()
	{
		if (this.mVehicle.behaviourManager.isOutOfRace)
		{
			return;
		}
		TyreSet tyreSet = this.mVehicle.setup.tyreSet;
		if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries && !tyreSet.hasLostWheel && !tyreSet.hasWrongCompoundFitted)
		{
			int num = (int)TyreSet.CalculateLapRangeOfTyre(tyreSet, GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles));
			num = Mathf.FloorToInt((float)num * tyreSet.GetCondition() - (float)num * tyreSet.GetCliffCondition());
			bool flag = this.mVehicle.performance.fuel.fuelTankLapCountCapacity < num;
			if (!flag || this.mReasonForPreviousPit == SessionStints.ReasonForStint.PitTyreChange || this.mReasonForPreviousPit == SessionStints.ReasonForStint.PitTyreThreadChange)
			{
				this.SetAIPitOrder(SessionStrategy.PitOrder.Tyres);
			}
		}
		else
		{
			this.SetAIPitOrder(SessionStrategy.PitOrder.Tyres);
		}
		ChampionshipRules rules = this.mVehicle.championship.rules;
		if (rules.isRefuelingOn)
		{
			this.SetAIPitOrder(SessionStrategy.PitOrder.Refuel);
		}
		this.SetAIPartFixing();
		float mAggressiveness = this.mVehicle.driver.contract.GetTeam().aiWeightings.mAggressiveness;
		float num2 = (mAggressiveness >= 0.5f) ? 0.2f : 0.1f;
		float num3 = (mAggressiveness >= 0.5f) ? 0.3f : 0.4f;
		float random = RandomUtility.GetRandom01();
		if (random < num2)
		{
			this.SetPitStrategy(SessionStrategy.PitStrategy.Fast);
		}
		else if (random < num3 && this.mVehicle.standingsPosition <= this.mVehicle.driver.expectedRacePosition)
		{
			this.SetPitStrategy(SessionStrategy.PitStrategy.Safe);
		}
		else
		{
			this.SetPitStrategy(SessionStrategy.PitStrategy.Balanced);
		}
		if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries)
		{
			this.SetAIPitOrder(SessionStrategy.PitOrder.ChangeDriver);
		}
		this.Pit();
	}

	private void SetAIDriverSwaping()
	{
		Driver driver = null;
		float num = (!this.mPitOrders.Contains(SessionStrategy.PitOrder.Tyres)) ? this.mVehicle.driver.driverStamina.dangerZone : (this.mVehicle.driver.driverStamina.dangerZone + 0.6f);
		bool flag = this.mVehicle.timer.IsDriverAbleToFinishTimedRace(this.mVehicle.driver) && this.mVehicle.driver.driverStamina.CanDriverGetToEndOfTimedRace();
		float num2 = this.mVehicle.championship.rules.drivingTimeEndurance - 0.1f;
		bool flag2 = !flag && (this.mVehicle.driver.driverStamina.currentStamina < num || this.mVehicle.timer.GetNormalizedTimeDriven(this.mVehicle.driver) > num2);
		if (flag2)
		{
			driver = this.GetDriverSwapTargetDriver();
		}
		if (driver != null && driver != this.mVehicle.driver)
		{
			this.mVehicle.setup.SetTargetDriver(driver);
		}
		else
		{
			this.mVehicle.setup.SetTargetDriver(this.mVehicle.driver);
		}
	}

	private Driver GetDriverSwapTargetDriver()
	{
		Driver[] driversForCar = this.mVehicle.driversForCar;
		Driver driver = null;
		Driver driver2 = null;
		float num = Mathf.Min(0.4f, 1f - Game.instance.sessionManager.GetNormalizedSessionTime());
		foreach (Driver driver3 in driversForCar)
		{
			if (driver3.driverStamina.CanDriverGetToEndOfTimedRace() && this.mVehicle.timer.IsDriverAbleToFinishTimedRace(driver3) && (driver2 == null || driver3.GetDriverStats().GetTotal() > driver2.GetDriverStats().GetTotal()))
			{
				driver2 = driver3;
			}
			if (driver3.driverStamina.currentStamina - driver3.driverStamina.dangerZone > num || Mathf.Approximately(this.mVehicle.timer.GetNormalizedTimeDriven(driver3), 0f))
			{
				if (driver == null)
				{
					driver = driver3;
				}
				else
				{
					float num2 = this.mVehicle.championship.rules.drivingTimeEndurance - 0.05f;
					if (this.mVehicle.timer.GetNormalizedTimeDriven(driver3) < this.mVehicle.timer.GetNormalizedTimeDriven(driver))
					{
						driver = driver3;
					}
					else if (driver3.driverStamina.currentStamina > driver.driverStamina.currentStamina && this.mVehicle.timer.GetNormalizedTimeDriven(driver3) < num2)
					{
						driver = driver3;
					}
				}
			}
		}
		if (driver2 != null)
		{
			return driver2;
		}
		if (driver != null)
		{
			return driver;
		}
		return null;
	}

	private void UpdateDrivingStyleAndEngineModes()
	{
		this.mUsesAIForStrategy = this.mVehicle.driver.personalityTraitController.UsesAIForStrategy(this.mVehicle);
		if (this.mUsesAIForStrategy || !this.mVehicle.isPlayerDriver || Game.instance.sessionManager.isUsingAIForPlayerDrivers)
		{
			if (this.mVehicle.pathState.IsInPitlaneArea() || this.mVehicle.timer.hasSeenChequeredFlag)
			{
				this.mVehicle.performance.drivingStyle.SetDrivingStyle(DrivingStyle.Mode.BackUp);
				this.mVehicle.performance.fuel.SetEngineMode(Fuel.EngineMode.Low, false);
			}
			else
			{
				this.mVehicle.performance.drivingStyle.SetRacingAIDrivingStyle();
				this.mVehicle.performance.fuel.SetRacingAIEngineMode();
			}
			if (Game.instance.sessionManager.isSafetyCarFlag)
			{
				this.mVehicle.performance.drivingStyle.SetDrivingStyle(DrivingStyle.Mode.BackUp);
				this.mVehicle.performance.fuel.SetEngineMode(Fuel.EngineMode.Low, false);
			}
			if (this.mVehicle.car.seriesCurrentParts[1].partCondition.IsOnRed())
			{
				this.mVehicle.performance.fuel.SetEngineMode(Fuel.EngineMode.Low, false);
			}
		}
	}

	public void OnSafetyCarEvent()
	{
		if (!this.mVehicle.isPlayerDriver || Game.instance.sessionManager.isUsingAIForPlayerDrivers)
		{
			bool flag = false;
			float mAggressiveness = this.mVehicle.driver.contract.GetTeam().aiWeightings.mAggressiveness;
			float num = Mathf.Clamp01(1f - this.mVehicle.pathController.GetRaceDistanceTraveled01());
			float num2 = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles) * (float)Game.instance.sessionManager.lapCount;
			float num3 = num2 * num;
			float num4 = (mAggressiveness >= 0.5f) ? 0.55f : 0.35f;
			TyreSet tyreSet = this.mVehicle.setup.currentSetup.tyreSet;
			if (tyreSet.GetCondition() < num4 && tyreSet.GetOptimalTyreDistance() < num3)
			{
				flag = true;
			}
			ChampionshipRules rules = this.mVehicle.championship.rules;
			if (rules.isRefuelingOn)
			{
				float fuelDistance = this.mVehicle.performance.fuel.GetFuelDistance();
				if (this.mVehicle.performance.fuel.GetNormalisedFuelLevel() < num4 && fuelDistance < num3)
				{
					flag = true;
				}
			}
			if (flag)
			{
				this.PlanPitstop();
			}
		}
	}

	private void CreateTyres(ref TyreSet[] inTyres, TyreSet.Compound inCompound, SessionStrategy.TyreOption inTyreOption, int inCount)
	{
		inTyres = new TyreSet[inCount];
		for (int i = 0; i < inCount; i++)
		{
			inTyres[i] = TyreSet.CreateTyreSet(inCompound);
			inTyres[i].Start(this.mVehicle);
		}
		this.SetPerformanceLevel(ref inTyres, inTyreOption, null);
	}

	public void RefreshAllTyresPerformanceLevel(Driver inDriver = null)
	{
		this.SetPerformanceLevel(ref this.mFirstTyreOption, SessionStrategy.TyreOption.First, inDriver);
		this.SetPerformanceLevel(ref this.mSecondTyreOption, SessionStrategy.TyreOption.Second, inDriver);
		if (this.mThirdTyreOption != null)
		{
			this.SetPerformanceLevel(ref this.mThirdTyreOption, SessionStrategy.TyreOption.Third, inDriver);
		}
		this.SetPerformanceLevel(ref this.mIntermediates, SessionStrategy.TyreOption.Intermediates, inDriver);
		this.SetPerformanceLevel(ref this.mWets, SessionStrategy.TyreOption.Wets, inDriver);
	}

	private void SetPerformanceLevel(ref TyreSet[] inTyres, SessionStrategy.TyreOption inTyreOption, Driver inDriver = null)
	{
		TyrePerformanceDesignData tyrePerformanceData = DesignDataManager.instance.GetDesignData().GetTyrePerformanceData(inTyreOption);
		int highPerformanceLapCount = tyrePerformanceData.highPerformanceLapCount;
		int num = tyrePerformanceData.mediumPerformanceLapCount;
		int lowPerformanceLapCount = tyrePerformanceData.lowPerformanceLapCount;
		RaceDirector raceDirector = Game.instance.sessionManager.raceDirector;
		DriverStats driverStats = this.mVehicle.driver.GetDriverStats();
		if (inDriver != null)
		{
			driverStats = inDriver.GetDriverStats();
		}
		float num2 = Mathf.Clamp01((driverStats.smoothness - raceDirector.lowestDriverStats.smoothness) / (raceDirector.highestDriverStats.smoothness - raceDirector.lowestDriverStats.smoothness));
		if (num2 < 0.33f)
		{
			num -= tyrePerformanceData.driverSmoothnessLapCountDecrease;
		}
		else if (num2 > 0.66f)
		{
			num += tyrePerformanceData.driverSmoothnessLapCountIncrease;
		}
		Circuit circuit = Game.instance.sessionManager.eventDetails.circuit;
		switch (circuit.tyreWearRate)
		{
		case Circuit.Rate.VeryLow:
		case Circuit.Rate.Low:
			num += tyrePerformanceData.circuitLowTyreWearLapCountIncrease;
			break;
		case Circuit.Rate.High:
		case Circuit.Rate.VeryHigh:
			num -= tyrePerformanceData.circuitHighTyreWearLapCountDecrease;
			break;
		}
		CarManager carManager = this.mVehicle.driver.contract.GetTeam().carManager;
		float carChassisStatValueOnGrid = carManager.GetCarChassisStatValueOnGrid(CarChassisStats.Stats.TyreWear, CarManager.MedianTypes.Highest);
		float carChassisStatValueOnGrid2 = carManager.GetCarChassisStatValueOnGrid(CarChassisStats.Stats.TyreWear, CarManager.MedianTypes.Lowest);
		float num3 = this.mVehicle.car.ChassisStats.GetStat(CarChassisStats.Stats.TyreWear, true, this.mVehicle.car);
		List<BonusChassisStats> activePartBonus = this.mVehicle.car.GetActivePartBonus<BonusChassisStats>(this.mVehicle, CarPart.PartType.None);
		float num4 = 0f;
		for (int i = 0; i < activePartBonus.Count; i++)
		{
			if (activePartBonus[i].stat == CarChassisStats.Stats.TyreWear)
			{
				num4 += activePartBonus[i].bonusValue;
			}
		}
		num3 += GameStatsConstants.chassisStatMax * num4;
		float num5 = Mathf.Clamp01((num3 - carChassisStatValueOnGrid2) / (carChassisStatValueOnGrid - carChassisStatValueOnGrid2));
		if (num5 < 0.33f)
		{
			num -= DesignDataManager.instance.GetDesignData().carChassis.tyreWearLapCountDecrease;
		}
		else if (num5 > 0.66f)
		{
			num += DesignDataManager.instance.GetDesignData().carChassis.tyreWearLapCountIncrease;
		}
		for (int j = 0; j < inTyres.Length; j++)
		{
			inTyres[j].highPerformanceRange.SetMaxLaps(highPerformanceLapCount);
			inTyres[j].highPerformanceRange.SetTimeCost(tyrePerformanceData.highPerformanceTimeCost);
			inTyres[j].mediumPerformanceRange.SetMaxLaps(num);
			inTyres[j].mediumPerformanceRange.SetTimeCost(tyrePerformanceData.mediumPerformanceTimeCost);
			inTyres[j].lowPerformanceRange.SetMaxLaps(lowPerformanceLapCount);
			inTyres[j].lowPerformanceRange.SetTimeCost(tyrePerformanceData.lowPerformanceTimeCost);
		}
	}

	public void LogTyreToHistory()
	{
		this.mTyreHistory.Add(this.mVehicle.setup.tyreSet.GetCompound());
	}

	private void RefreshCarCrashDataAfterLoad()
	{
		if (this.mRefreshCarCrashData && Game.instance.sessionManager.circuit != null)
		{
			if (this.mStatus == SessionStrategy.Status.Crashing && !this.mVehicle.behaviourManager.isOutOfRace)
			{
				this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().ClearTarget();
				if (this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().SetTargetPath(PathController.PathType.CrashLane, true))
				{
					this.SetStatus(SessionStrategy.Status.Crashing);
				}
			}
			this.mRefreshCarCrashData = false;
		}
	}

	public void SimulationUpdate()
	{
		this.RefreshCarCrashDataAfterLoad();
		if (this.mVehicle.pathController.currentPathType == PathController.PathType.Track)
		{
			this.ApplyQueueOrders();
		}
		if (this.mTargetPointSteeringBehaviour == null)
		{
			this.mTargetPointSteeringBehaviour = this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>();
		}
		switch (this.mStatus)
		{
		case SessionStrategy.Status.NoActionRequired:
			{
			SessionDetails.SessionType sessionType = Game.instance.sessionManager.eventDetails.currentSession.sessionType;
			if (sessionType != SessionDetails.SessionType.Race && this.mVehicle.isPlayerDriver && !Game.instance.sessionManager.isUsingAIForPlayerDrivers && (this.mVehicle.performance.fuel.IsOutOfFuel() || this.HasCompletedOrderedLapCount()))
			{
				this.mVehicle.strategy.ReturnToGarage();
			}
			if (this.mVehicle.timer.hasSeenChequeredFlag)
			{
				this.mVehicle.strategy.ReturnToGarage();
			}
			break;
			}
		case SessionStrategy.Status.Pitting:
			if (Game.instance.sessionManager.flag == SessionManager.Flag.Chequered)
			{
				this.CancelPit();
			}
			if (this.mVehicle.pathController.currentPathType == PathController.PathType.Track && this.mTargetPointSteeringBehaviour.state == TargetPointSteeringBehaviour.State.None)
			{
				this.mTargetPointSteeringBehaviour.SetTargetPath(PathController.PathType.PitlaneEntry, true);
			}
			break;
		case SessionStrategy.Status.WaitingForSetupCompletion:
			this.WaitForSetupCompletion();
			break;
		case SessionStrategy.Status.PitThruPenalty:
			if (this.mVehicle.pathController.currentPathType == PathController.PathType.Track && this.mTargetPointSteeringBehaviour.state == TargetPointSteeringBehaviour.State.None)
			{
				this.mTargetPointSteeringBehaviour.SetTargetPath(PathController.PathType.PitlaneEntry, true);
			}
			break;
		}
	}

	private void WaitForSetupCompletion()
	{
		if (this.mVehicle.setup.state == SessionSetup.State.Setup)
		{
			this.SendOutOnTrack();
			this.SetStatus(SessionStrategy.Status.NoActionRequired);
		}
	}

	public bool HasCompletedOrderedLapCount()
	{
		return this.mOrderedLapCount != 0 && this.mOrderedLapCount == this.mVehicle.timer.lapData.Count;
	}

	public void PitLaneDriveTrough()
	{
		this.mPitlaneDriveTrough++;
		if (this.mStatus == SessionStrategy.Status.NoActionRequired && Game.instance.sessionManager.flag == SessionManager.Flag.Green)
		{
			this.SetStatus(SessionStrategy.Status.PitThruPenalty);
		}
	}

	public void OnFlagChanged(SessionManager.Flag inFlag)
	{
		if (this.mStatus == SessionStrategy.Status.PitThruPenalty && inFlag != SessionManager.Flag.Green && this.mVehicle.pathController.currentPathType == PathController.PathType.Track)
		{
			this.SetStatus(SessionStrategy.Status.NoActionRequired);
		}
		else if (inFlag == SessionManager.Flag.Green && this.mVehicle.pathController.currentPathType != PathController.PathType.PitboxEntry && this.mPitlaneDriveTrough > 0)
		{
			this.SetStatus(SessionStrategy.Status.PitThruPenalty);
		}
	}

	public void SetStatus(SessionStrategy.Status inStatus)
	{
		if (inStatus == SessionStrategy.Status.WaitingForSetupCompletion)
		{
			this.RefreshPitOrders();
		}
		if (inStatus == SessionStrategy.Status.NoActionRequired)
		{
			if (this.mStatus == SessionStrategy.Status.ReturningToGarage && this.mVehicle.timer.currentLap.isInLap)
			{
				this.mVehicle.timer.currentLap.isInLap = false;
			}
			if (this.mVehicle.pathController.currentPathType != PathController.PathType.PitboxEntry && this.mPitlaneDriveTrough > 0 && Game.instance.sessionManager.flag == SessionManager.Flag.Green)
			{
				this.SetStatus(SessionStrategy.Status.PitThruPenalty);
				return;
			}
		}
		this.mPreviousStatus = this.mStatus;
		this.mStatus = inStatus;
		if (this.IsGoingToPit())
		{
			if (this.mVehicle.pathController.currentPathType == PathController.PathType.Track)
			{
				this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().SetTargetPath(PathController.PathType.PitlaneEntry, true);
			}
			if (this.mStatus == SessionStrategy.Status.ReturningToGarage && !this.mVehicle.behaviourManager.isOutOfRace && this.mVehicle.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.InOutLap)
			{
				this.mVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.InOutLap);
				this.mVehicle.timer.currentLap.isInLap = true;
			}
		}
		else if (this.mStatus != SessionStrategy.Status.Crashing)
		{
			this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().ClearTarget();
		}
	}

	public void SetPitStrategy(SessionStrategy.PitStrategy pitStrategy)
	{
		this.mPitStrategy = pitStrategy;
	}

	public bool IsGoingToPit()
	{
		return this.mStatus == SessionStrategy.Status.Pitting || this.mStatus == SessionStrategy.Status.ReturningToGarage;
	}

	public bool HasQueuedOrderToPit()
	{
		return this.mQueuedStatus == SessionStrategy.Status.Pitting || this.mQueuedStatus == SessionStrategy.Status.ReturningToGarage;
	}

	public void SendOutOnTrack()
	{
		this.mVehicle.pathState.ChangeState(PathStateManager.StateType.GarageExit);
	}

	public void ReturnToGarage()
	{
		this.SetStatus(SessionStrategy.Status.ReturningToGarage);
	}

	public bool HasPitOrder(SessionStrategy.PitOrder inOrder)
	{
		for (int i = 0; i < this.mPitOrders.Count; i++)
		{
			if (inOrder == this.mPitOrders[i])
			{
				return true;
			}
		}
		return false;
	}

	public void SetAIPitOrder(SessionStrategy.PitOrder inOrder)
	{
		this.mPitOrders.Add(inOrder);
		this.ActivateOrder(inOrder);
	}

	private void ActivateOrder(SessionStrategy.PitOrder inOrder)
	{
		switch (inOrder)
		{
		case SessionStrategy.PitOrder.Tyres:
			this.SetAITargetTyres();
			break;
		case SessionStrategy.PitOrder.Refuel:
			this.SetAIFuelTarget();
			break;
		case SessionStrategy.PitOrder.FixPart:
			this.SetAIPartFixing();
			break;
		case SessionStrategy.PitOrder.Recharge:
		{
			int b = Mathf.FloorToInt((1f - this.mVehicle.ERSController.normalizedCharge) * 4f);
			this.mVehicle.setup.SetTargetBatteryCharge(Mathf.Min(RandomUtility.GetRandomInc(1, 4), b));
			break;
		}
		case SessionStrategy.PitOrder.ChangeDriver:
			this.SetAIDriverSwaping();
			break;
		case SessionStrategy.PitOrder.CancelPit:
			this.CancelPit();
			break;
		}
	}

	public void RefreshPitOrders()
	{
		for (int i = 0; i < this.mPitOrders.Count; i++)
		{
			this.ActivateOrder(this.mPitOrders[i]);
		}
	}

	private void SetAIPartFixing()
	{
		if (!Game.instance.sessionManager.raceDirector.retirementDirector.ShouldFixParts(this.mVehicle) && !this.mVehicle.performance.carConditionPerformance.isExperiencingCriticalIssue)
		{
			return;
		}
		if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries && Game.instance.sessionManager.time < 600f)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < this.mVehicle.car.seriesCurrentParts.Length; i++)
		{
			CarPart carPart = this.mVehicle.car.seriesCurrentParts[i];
			if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries)
			{
				if (carPart.partCondition.IsOnRed() || carPart.partCondition.condition <= 0.1f)
				{
					carPart.partCondition.SetRepairInPit(true);
					flag = true;
					break;
				}
			}
			else if (
				(this.mVehicle.performance.carConditionPerformance.LikelyToNeedRepairByDistance(carPart.GetPartType()) || carPart.partCondition.condition <= 0.1f)
				&& carPart.GetPartType() != CarPart.PartType.Engine
				&& carPart.GetPartType() != CarPart.PartType.Brakes
				&& carPart.GetPartType() != CarPart.PartType.Gearbox
				&& carPart.GetPartType() != CarPart.PartType.Suspension
				)
			{
				carPart.partCondition.SetRepairInPit(true);
				flag = true;
			}
		}
		if (flag && this.mVehicle.championship.series == Championship.Series.EnduranceSeries)
		{
			for (int j = 0; j < this.mVehicle.car.seriesCurrentParts.Length; j++)
			{
				CarPart carPart2 = this.mVehicle.car.seriesCurrentParts[j];
				if (this.mVehicle.performance.carConditionPerformance.LikelyToNeedRepairByDistance(carPart2.GetPartType()) || carPart2.partCondition.condition <= 0.1f)
				{
					carPart2.partCondition.SetRepairInPit(true);
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.mVehicle.setup.SetRepair();
		}
	}

	private void SetAIFuelTarget()
	{
		SessionManager sessionManager = Game.instance.sessionManager;
		if (sessionManager.eventDetails.currentSession.sessionType == SessionDetails.SessionType.Race)
		{
			this.SetAIFuelStrategy();
		}
		else
		{
			this.mVehicle.setup.SetTargetFuelLevel(3);
		}
	}

	public void Pit()
	{
		if (this.mStatus == SessionStrategy.Status.Crashing)
		{
			this.mQueuedStatus = SessionStrategy.Status.Crashing;
			this.SetStatus(SessionStrategy.Status.NoActionRequired);
		}
		if (this.mVehicle.behaviourManager.isRetired)
		{
			this.SetStatus(SessionStrategy.Status.ReturningToGarage);
		}
		else
		{
			this.SetStatus(SessionStrategy.Status.Pitting);
		}
	}

	public void CancelPit()
	{
		TyreSet tyreSet = this.mVehicle.setup.currentSetup.tyreSet;
		if (MathsUtility.ApproximatelyZero(tyreSet.GetCondition()))
		{
			return;
		}
		this.mVehicle.setup.sessionPitStop.ResetPitStopSetup();
		this.mPitOrders.Clear();
		this.SetStatus(SessionStrategy.Status.NoActionRequired);
	}

	public void OnExitPitlane()
	{
		this.mPitOrders.Clear();
		if (this.mStatus == SessionStrategy.Status.PitThruPenalty && this.mPitlaneDriveTrough > 0)
		{
			this.mPitlaneDriveTrough--;
		}
		this.SetStatus(SessionStrategy.Status.NoActionRequired);
		CommentaryManager.SendComment(this.mVehicle, Comment.CommentType.DriverExitingPitlane, new object[]
		{
			this.mVehicle.driver
		});
		this.SetStrategyType();
	}

	public void SetStrategyType()
	{
		float mAggressiveness = this.mVehicle.driver.contract.GetTeam().aiWeightings.mAggressiveness;
		float num = (mAggressiveness <= 0.5f) ? 0.3f : 0.15f;
		this.mStintStrategyType = ((RandomUtility.GetRandom01() >= num) ? SessionStrategy.StintStrategyType.Normal : SessionStrategy.StintStrategyType.Conservative);
	}

	public void ApplyQueueOrders()
	{
		if (this.mStatus == SessionStrategy.Status.NoActionRequired && this.mQueuedStatus != SessionStrategy.Status.NoActionRequired)
		{
			this.SetStatus(this.mQueuedStatus);
			this.RemoveQueuedOrder();
		}
	}

	public void RemoveQueuedOrder()
	{
		this.mQueuedStatus = SessionStrategy.Status.NoActionRequired;
	}

	public void SetToNoActionRequired()
	{
		this.SetStatus(SessionStrategy.Status.NoActionRequired);
	}

	public TyreSet[] GetTyres(SessionStrategy.TyreOption inTyreOption)
	{
		TyreSet[] result = null;
		switch (inTyreOption)
		{
		case SessionStrategy.TyreOption.First:
			result = this.mFirstTyreOption;
			break;
		case SessionStrategy.TyreOption.Second:
			result = this.mSecondTyreOption;
			break;
		case SessionStrategy.TyreOption.Third:
			result = this.mThirdTyreOption;
			break;
		case SessionStrategy.TyreOption.Intermediates:
			result = this.mIntermediates;
			break;
		case SessionStrategy.TyreOption.Wets:
			result = this.mWets;
			break;
		}
		return result;
	}

	public TyreSet GetTyre(SessionStrategy.TyreOption inTyreOption, int inIndex)
	{
		TyreSet[] tyres = this.GetTyres(inTyreOption);
		if (inIndex >= tyres.Length)
		{
			global::Debug.LogError("Trying to retrieve tyre index greater than available tyres", null);
			return tyres[tyres.Length - 1];
		}
		return tyres[inIndex];
	}

	public int GetTyreIndex(TyreSet inTyreSet = null)
	{
		if (inTyreSet == null)
		{
			inTyreSet = this.mVehicle.setup.currentSetup.tyreSet;
		}
		TyreSet[] tyres = this.GetTyres(this.GetOptionFromCompound(inTyreSet.GetCompound()));
		for (int i = 0; i < tyres.Length; i++)
		{
			if (tyres[i] == inTyreSet)
			{
				return i;
			}
		}
		return -1;
	}

	public TyreSet GetTyre(TyreSet.Compound inTyreCompound, int inIndex)
	{
		return this.GetTyre(this.GetOptionFromCompound(inTyreCompound), inIndex);
	}

	public int GetTyreCount(SessionStrategy.TyreOption inTyreOption)
	{
		TyreSet[] tyres = this.GetTyres(inTyreOption);
		return tyres.Length;
	}

	public TyreSet CheckReturningLockedTyre(TyreSet inTyreSet)
	{
		if (this.lockedTyres == inTyreSet)
		{
			return this.ReplaceTyreSet(inTyreSet);
		}
		return inTyreSet;
	}

	public TyreSet ReplaceTyreSet(TyreSet inTyreSet)
	{
		TyreSet tyreInBestCondition = this.GetTyreInBestCondition(this.GetOptionFromCompound(inTyreSet.GetCompound()), inTyreSet);
		if (tyreInBestCondition != inTyreSet)
		{
			return tyreInBestCondition;
		}
		if (inTyreSet.GetTread() == TyreSet.Tread.Slick)
		{
			if (this.GetOptionFromCompound(inTyreSet.GetCompound()) == SessionStrategy.TyreOption.First)
			{
				return this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
			}
			return this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
		}
		else
		{
			if (inTyreSet.GetCompound() == TyreSet.Compound.Intermediate)
			{
				return this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
			}
			return this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
		}
	}

	public int GetMintConditionTyreCount(SessionStrategy.TyreOption inTyreOption)
	{
		TyreSet[] tyres = this.GetTyres(inTyreOption);
		int num = 0;
		for (int i = 0; i < tyres.Length; i++)
		{
			if (Mathf.Approximately(tyres[i].GetCondition(), 1f))
			{
				num++;
			}
		}
		return num;
	}

	public TyreSet.Compound GetTyreCompound(SessionStrategy.TyreOption inTyreOption)
	{
		return this.GetTyre(inTyreOption, 0).GetCompound();
	}

	public static TyreSet.Tread GetRecommendedTreadRightNow()
	{
		return SessionStrategy.GetRecommendedTreadForTime(Game.instance.sessionManager.GetNormalizedSessionTime());
	}

	public static TyreSet.Tread GetRecommendedTreadForLap(int inLap)
	{
		float inNormalizedTime = Mathf.Clamp01((float)inLap / (float)Game.instance.sessionManager.lapCount);
		return SessionStrategy.GetRecommendedTreadForTime(inNormalizedTime);
	}

	public static TyreSet.Tread GetRecommendedTreadForTime(float inNormalizedTime)
	{
		TyreSet.Tread result = TyreSet.Tread.Slick;
		SessionWeatherDetails currentSessionWeather = Game.instance.sessionManager.currentSessionWeather;
		float num = currentSessionWeather.sessionWaterLevelCurve.curve.Evaluate(inNormalizedTime);
		if (num > DesignDataManager.instance.tyreData.maxLightTreadSurfaceWaterRange)
		{
			result = TyreSet.Tread.HeavyTread;
		}
		else if (num > DesignDataManager.instance.tyreData.maxSlickTreadSurfaceWaterRange)
		{
			result = TyreSet.Tread.LightTread;
		}
		return result;
	}

	public TyreSet GetTyreInBestCondition(SessionStrategy.TyreOption inTyreOption, TyreSet inTyreSetToIgnore = null)
	{
		TyreSet[] tyres = this.GetTyres(inTyreOption);
		TyreSet tyreSet = tyres[0];
		if (tyreSet == inTyreSetToIgnore || tyreSet.hasLostWheel || tyreSet.hasWrongCompoundFitted)
		{
			if (tyres.Length < 2)
			{
				tyreSet = tyres[0];
			}
			else
			{
				tyreSet = tyres[1];
			}
		}
		foreach (TyreSet tyreSet2 in tyres)
		{
			if (tyreSet2 != this.mVehicle.setup.tyreSet || (!tyreSet2.hasLostWheel && !tyreSet2.hasWrongCompoundFitted))
			{
				if (tyreSet != tyreSet2 && tyreSet2 != inTyreSetToIgnore && tyreSet2.GetCondition() > tyreSet.GetCondition())
				{
					tyreSet = tyreSet2;
				}
			}
		}
		return tyreSet;
	}

	public TyreSet GetTyreInBestCondition(TyreSet.Compound inCompound)
	{
		return this.GetTyreInBestCondition(this.GetOptionFromCompound(inCompound), null);
	}

	public SessionStrategy.TyreOption GetOptionFromCompound(TyreSet.Compound inCompound)
	{
		ChampionshipRules rules = this.mChampionship.rules;
		if (inCompound == this.GetTyreCompound(SessionStrategy.TyreOption.First))
		{
			return SessionStrategy.TyreOption.First;
		}
		if (inCompound == this.GetTyreCompound(SessionStrategy.TyreOption.Second))
		{
			return SessionStrategy.TyreOption.Second;
		}
		if (rules.compoundsAvailable > 2 && inCompound == this.GetTyreCompound(SessionStrategy.TyreOption.Third))
		{
			return SessionStrategy.TyreOption.Third;
		}
		if (inCompound == TyreSet.Compound.Intermediate)
		{
			return SessionStrategy.TyreOption.Intermediates;
		}
		if (inCompound == TyreSet.Compound.Wet)
		{
			return SessionStrategy.TyreOption.Wets;
		}
		global::Debug.LogWarningFormat("Cannot get the option number from the given tyre compound:{0}", new object[]
		{
			inCompound.ToString()
		});
		return SessionStrategy.TyreOption.First;
	}

	public void SetAITyresToStartOn()
	{
		this.mVehicle.setup.SetTargetTyres(this.GetBestTyresForAI(0f), true);
		this.mVehicle.setup.InstantlyChangeTyres(true);
	}

	public void SetAIFuelStrategy()
	{
		int num = this.CalculateDesiredFuelLaps();
		if (Game.instance.sessionManager.isSessionActive)
		{
			this.mVehicle.setup.SetTargetFuelLevel(num);
		}
		else
		{
			this.mVehicle.performance.fuel.SetFuelLevel(num + 2, 0, true);
		}
	}

	public int CalculateDesiredFuelLaps()
	{
		ChampionshipRules rules = this.mVehicle.driver.contract.GetTeam().championship.rules;
		int num7;
		if (rules.isRefuelingOn)
		{
			float num = Mathf.Clamp01(1f - this.mVehicle.pathController.GetRaceDistanceTraveled01());
			int num2 = Mathf.CeilToInt(num / rules.fuelLimitForRaceDistanceNormalized);
			bool flag = MathsUtility.ApproximatelyZero(Mathf.Clamp01(1f - num));
			TyreSet tyreSet = this.mVehicle.setup.tyreSet;
			if (tyreSet != this.mVehicle.setup.targetSetup.tyreSet)
			{
				tyreSet = this.mVehicle.setup.targetSetup.tyreSet;
			}
			int num3;
			if (num2 == 0)
			{
				num3 = Game.instance.sessionManager.GetLapsRemaining() + 1;
			}
			else
			{
				float num4 = num / (float)num2;
				int num5;
				if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race)
				{
					num5 = Game.instance.sessionManager.lapCount;
				}
				else
				{
					num5 = DesignDataManager.CalculateRaceLapCount(this.mVehicle.driver.contract.GetTeam().championship, Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles, false);
				}
				if (flag)
				{
					float mAggressiveness = this.mVehicle.driver.contract.GetTeam().aiWeightings.mAggressiveness;
					bool flag2 = tyreSet.GetCompound() != this.GetTyreCompound(SessionStrategy.TyreOption.First);
					bool flag3 = !flag2 && mAggressiveness >= 0.8f;
					if (flag3)
					{
						num3 = Mathf.FloorToInt((float)num5 * num4);
					}
					else if (flag2)
					{
						num3 = this.mVehicle.performance.fuel.fuelTankLapCountCapacity;
					}
					else
					{
						num3 = Mathf.CeilToInt((float)num5 * num4);
					}
				}
				else
				{
					num3 = Mathf.CeilToInt((float)num5 * num4);
					num3 = Mathf.Max(num3, Mathf.Min(this.mVehicle.performance.fuel.fuelTankLapCountCapacity, Game.instance.sessionManager.GetLapsRemaining()));
				}
			}
			int num6 = (int)TyreSet.CalculateLapRangeOfTyre(tyreSet, GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles));
			num6 = Mathf.CeilToInt((float)num6 - (float)num6 * tyreSet.GetCliffCondition());
			if (flag)
			{
				num7 = Mathf.Max(num6, num3);
			}
			else
			{
				num7 = Mathf.Min(num6, num3);
			}
			if (this.mVehicle.championship.series == Championship.Series.EnduranceSeries)
			{
				Driver driver = this.mVehicle.setup.targetSetup.driver;
				if (driver == null)
				{
					driver = this.mVehicle.driver;
				}
				int a;
				if (this.mVehicle.timer.fastestLap != null)
				{
					a = Mathf.CeilToInt(driver.driverStamina.GetTimeToDepleteStamina(DrivingStyle.Mode.Conserve) / this.mVehicle.timer.fastestLap.time);
				}
				else
				{
					a = Mathf.CeilToInt(driver.driverStamina.GetTimeToDepleteStamina(DrivingStyle.Mode.Conserve) / Game.instance.sessionManager.eventDetails.circuit.bestPossibleLapTime);
				}
				num7 = Mathf.Min(a, num7);
			}
		}
		else
		{
			num7 = Game.instance.sessionManager.GetLapsRemaining() + 1;
		}
		return num7;
	}

	public void SetAIFuelForPracticeAndQualifying()
	{
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Practice)
		{
			TyreSet tyreSet = this.mVehicle.setup.tyreSet;
			ChampionshipRules rules = this.mVehicle.championship.rules;
			int num;
			if (rules.compoundsAvailable == 2)
			{
				if (tyreSet.GetCompound() == this.GetTyreCompound(SessionStrategy.TyreOption.First))
				{
					num = 5;
				}
				else
				{
					num = 7;
				}
			}
			else if (tyreSet.GetCompound() == this.GetTyreCompound(SessionStrategy.TyreOption.First))
			{
				num = 5;
			}
			else if (tyreSet.GetCompound() == this.GetTyreCompound(SessionStrategy.TyreOption.Second))
			{
				num = 6;
			}
			else
			{
				num = 7;
			}
			this.mVehicle.performance.fuel.SetFuelLevel(num, 0, true);
			this.mVehicle.strategy.SetOrderedLapCount(num - 2);
		}
		else if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Qualifying)
		{
			this.mVehicle.performance.fuel.SetFuelLevel(3, 0, true);
			this.mVehicle.strategy.SetOrderedLapCount(1);
		}
	}

	private void SetAITargetTyres()
	{
		TyreSet bestTyresForAI = this.GetBestTyresForAI(0f);
		if (this.mVehicle.championship.series != Championship.Series.EnduranceSeries)
		{
			this.mVehicle.setup.SetTargetTyres(bestTyresForAI, false);
		}
		else if (bestTyresForAI.GetTread() != this.mVehicle.setup.tyreSet.GetTread() || this.mVehicle.setup.tyreSet.GetCondition() < 0.6f || this.mVehicle.setup.tyreSet.hasLostWheel || this.mVehicle.setup.tyreSet.hasWrongCompoundFitted)
		{
			this.mVehicle.setup.SetTargetTyres(bestTyresForAI, false);
		}
		this.SetAIFuelTarget();
	}

	public void SetAITargetDriversPracticeAndQualifying()
	{
		Driver driver = null;
		if (Game.instance.sessionManager.championship.rules.gridSetup == ChampionshipRules.GridSetup.AverageLap)
		{
			SessionDetails.SessionType sessionType = Game.instance.sessionManager.sessionType;
			if (sessionType != SessionDetails.SessionType.Practice)
			{
				if (sessionType == SessionDetails.SessionType.Qualifying)
				{
					if (!this.mVehicle.timer.isAverageLapTimeSet)
					{
						if (this.mVehicle.timer.averageLapDriversSet == 0)
						{
							driver = this.mVehicle.driver;
						}
						else
						{
							driver = ((!MathsUtility.ApproximatelyZero(this.mVehicle.timer.averageLapDriverLapTimes[0])) ? this.mVehicle.driversForCar[1] : this.mVehicle.driversForCar[0]);
						}
					}
					else
					{
						driver = ((this.mVehicle.timer.averageLapDriverLapTimes[0] <= this.mVehicle.timer.averageLapDriverLapTimes[1]) ? this.mVehicle.driversForCar[1] : this.mVehicle.driversForCar[0]);
					}
				}
			}
			else
			{
				driver = this.mVehicle.driversForCar[RandomUtility.GetRandom(0, this.mVehicle.driversForCar.Length - 1)];
			}
		}
		if (driver != null && this.mVehicle.driver != driver)
		{
			this.mVehicle.setup.InstantlyChangeDriver(driver);
		}
	}

	public TyreSet GetBestTyresForAI(float inNormalizedTime = 0f)
	{
		float inNormalizedTime2 = Game.instance.sessionManager.GetNormalizedSessionTime();
		if (inNormalizedTime != 0f)
		{
			inNormalizedTime2 = inNormalizedTime;
		}
		return this.GetBestTyresForTime(inNormalizedTime2);
	}

	public TyreSet GetBestTyresForTime(float inNormalizedTime)
	{
		TyreSet result = null;
		switch (Game.instance.sessionManager.sessionType)
		{
		case SessionDetails.SessionType.Practice:
			result = this.GetTyresForPractice(inNormalizedTime);
			break;
		case SessionDetails.SessionType.Qualifying:
			result = this.GetTyresForQualifying(inNormalizedTime);
			break;
		case SessionDetails.SessionType.Race:
			if (this.mVehicle.pathState.currentState == null || this.mVehicle.pathState.currentState.stateType == PathStateManager.StateType.Grid)
			{
				result = this.GetTyresForRaceStart();
			}
			else
			{
				result = this.GetTyresForRace(inNormalizedTime, true, TyreSet.Tread.Slick);
			}
			break;
		}
		return result;
	}

	private TyreSet GetTreadedTyre(TyreSet.Tread inTread, SessionDetails.SessionType inSessionType, float inNormalizedTime)
	{
		TyreSet result = null;
		if (inTread != TyreSet.Tread.LightTread)
		{
			if (inTread == TyreSet.Tread.HeavyTread)
			{
				result = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
				if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Wets) < 3)
				{
					result = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
					if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Intermediates) < 3)
					{
						result = this.GetSlickTyre(inSessionType, inNormalizedTime);
					}
				}
			}
		}
		else
		{
			result = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
			if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Intermediates) < 3)
			{
				result = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
				if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Wets) < 3)
				{
					result = this.GetSlickTyre(inSessionType, inNormalizedTime);
				}
			}
		}
		return result;
	}

	private TyreSet GetSlickTyre(SessionDetails.SessionType inSessionType, float inNormalizedTime)
	{
		ChampionshipRules rules = this.mChampionship.rules;
		TyreSet result = null;
		if (inSessionType == SessionDetails.SessionType.Practice)
		{
			bool[] array = new bool[rules.compoundsAvailable];
			int num = 0;
			for (int i = 0; i < rules.compoundsAvailable; i++)
			{
				array[i] = true;
				if (array[i] && i < rules.compoundsAvailable - 1 && (this.GetMintConditionTyreCount((SessionStrategy.TyreOption)i) <= 2 || this.GetTyreCount((SessionStrategy.TyreOption)i) <= 3))
				{
					array[i] = false;
				}
				if (array[i])
				{
					num++;
				}
			}
			SessionStrategy.TyreOption[] array2 = new SessionStrategy.TyreOption[num];
			int num2 = 0;
			for (int j = 0; j < rules.compoundsAvailable; j++)
			{
				if (array[j])
				{
					array2[num2] = (SessionStrategy.TyreOption)j;
					num2++;
				}
			}
			TyreSet[] array3 = new TyreSet[array2.Length];
			for (int k = 0; k < array2.Length; k++)
			{
				array3[k] = this.GetTyreInBestCondition(array2[k], null);
			}
			TyreSet tyreSet = array3[0];
			bool flag = tyreSet.GetCondition() > 0.5f;
			for (int l = 1; l < array3.Length; l++)
			{
				if (array3[l].GetCondition() < 0.5f)
				{
					flag = false;
				}
				if (tyreSet.GetCondition() < array3[l].GetCondition())
				{
					tyreSet = array3[l];
				}
			}
			if (flag)
			{
				int random = RandomUtility.GetRandom(0, array3.Length);
				result = array3[random];
			}
			else
			{
				result = tyreSet;
			}
		}
		return result;
	}

	private TyreSet GetTyresForPractice(float inNormalizedTime)
	{
		TyreSet.Tread recommendedTreadForTime = SessionStrategy.GetRecommendedTreadForTime(inNormalizedTime);
		TyreSet result;
		if (recommendedTreadForTime == TyreSet.Tread.Slick)
		{
			result = this.GetSlickTyre(SessionDetails.SessionType.Practice, inNormalizedTime);
		}
		else
		{
			result = this.GetTreadedTyre(recommendedTreadForTime, SessionDetails.SessionType.Practice, inNormalizedTime);
		}
		return result;
	}

	private TyreSet GetTyresForQualifying(float inNormalizedTime)
	{
		TyreSet bestTyresForRightNow = this.GetBestTyresForRightNow();
		return this.CheckReturningLockedTyre(bestTyresForRightNow);
	}

	private TyreSet GetTyresForRaceStart()
	{
		TyreSet tyreSet = null;
		TyreSet.Tread treadWithLeastTimeCost = this.mVehicle.performance.tyrePerformance.GetTreadWithLeastTimeCost();
		if (treadWithLeastTimeCost == TyreSet.Tread.Slick)
		{
			ChampionshipRules rules = this.mChampionship.rules;
			if (rules.isRefuelingOn && this.mVehicle.standingsPosition > 12)
			{
				TyreSet tyreInBestCondition = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
				if (this.mVehicle.performance.fuel.fuelTankLapDistanceCapacity < tyreInBestCondition.GetOptimalTyreDistance())
				{
					tyreSet = ((RandomUtility.GetRandom01() >= 0.3f) ? tyreInBestCondition : this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null));
				}
			}
			if (tyreSet == null)
			{
				if (this.mVehicle.standingsPosition < 8)
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
					if (tyreSet.GetCondition() < 0.9f)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
					}
				}
				else if (this.mVehicle.standingsPosition < 12)
				{
					if (RandomUtility.GetRandom01() < 0.5f)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
					}
					else
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
					}
				}
				else if (rules.compoundsAvailable > 2)
				{
					float random = RandomUtility.GetRandom01();
					if (random < 0.1f)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
					}
					else if (random < 0.3f)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
					}
					else
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Third, null);
					}
				}
				else if (RandomUtility.GetRandom01() < 0.3f)
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
				}
				else
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
				}
			}
			bool flag = false;
			PrefGameAIStrategyDifficulty.Type aistrategyDifficulty = App.instance.preferencesManager.gamePreferences.GetAIStrategyDifficulty();
			if (aistrategyDifficulty == PrefGameAIStrategyDifficulty.Type.Realistic && !Game.instance.challengeManager.IsAttemptingChallenge())
			{
				float num = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles);
				int num2 = Mathf.RoundToInt(tyreSet.GetOptimalTyreDistance() / num);
				flag = (this.GetLapsUntilDifferentTyreTreadRequired(tyreSet) < num2);
			}
			if (tyreSet.GetCondition() < 0.65f || flag)
			{
				tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
				if (tyreSet.GetCondition() < 0.65f)
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
					if (tyreSet.GetCondition() < 0.65f && rules.compoundsAvailable > 2)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Third, null);
					}
				}
			}
		}
		else if (treadWithLeastTimeCost == TyreSet.Tread.LightTread)
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
		}
		else if (treadWithLeastTimeCost == TyreSet.Tread.HeavyTread)
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
		}
		return tyreSet;
	}

	public TyreSet GetTyresForRace(float inNormalizedTime, bool inCalculateTread = true, TyreSet.Tread inTread = TyreSet.Tread.Slick)
	{
		TyreSet tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
		TyreSet.Tread tread = (!inCalculateTread) ? inTread : this.mVehicle.performance.tyrePerformance.GetTreadWithLeastTimeCost();
		if (tread == TyreSet.Tread.Slick)
		{
			int num = Mathf.RoundToInt((float)Game.instance.sessionManager.lapCount * (1f - inNormalizedTime));
			if (this.mVehicle.championship.rules.isRefuelingOn)
			{
				num = Mathf.Min(this.mVehicle.performance.fuel.fuelTankLapCountCapacity, num);
			}
			float length = this.mVehicle.pathController.GetPathData(PathController.PathType.Track).length;
			float inDistanceRemaining = length * (float)num;
			TyreStrategyOption tyreStrategyOption = null;
			TyreStrategyOption tyreStrategyOption2 = null;
			TyreStrategyOption tyreStrategyOption3 = null;
			for (int i = 0; i < this.mTyreStrategyOption.Length; i++)
			{
				this.mTyreStrategyOption[i].CalculateEstimatedStrategy(inDistanceRemaining);
				if (this.mTyreStrategyOption[i].isValidOption)
				{
					if (tyreStrategyOption == null || this.mTyreStrategyOption[i].timeCost < tyreStrategyOption.timeCost)
					{
						tyreStrategyOption = this.mTyreStrategyOption[i];
					}
					else if (tyreStrategyOption2 == null || this.mTyreStrategyOption[i].timeCost < tyreStrategyOption2.timeCost)
					{
						tyreStrategyOption2 = this.mTyreStrategyOption[i];
					}
					else if (tyreStrategyOption3 == null || this.mTyreStrategyOption[i].timeCost < tyreStrategyOption3.timeCost)
					{
						tyreStrategyOption3 = this.mTyreStrategyOption[i];
					}
				}
			}
			TyreStrategyOption tyreStrategyOption4 = tyreStrategyOption;
			float random = RandomUtility.GetRandom01();
			if (random < 0.05f && tyreStrategyOption3 != null)
			{
				tyreStrategyOption4 = tyreStrategyOption3;
			}
			else if (random < 0.2f && tyreStrategyOption2 != null)
			{
				tyreStrategyOption4 = tyreStrategyOption2;
			}
			if (tyreStrategyOption4 != null)
			{
				tyreSet = tyreStrategyOption4.GetSelectedTyre();
			}
			PrefGameAIStrategyDifficulty.Type aistrategyDifficulty = App.instance.preferencesManager.gamePreferences.GetAIStrategyDifficulty();
			if (aistrategyDifficulty == PrefGameAIStrategyDifficulty.Type.Realistic && !Game.instance.challengeManager.IsAttemptingChallenge())
			{
				float num2 = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles);
				int num3 = Mathf.RoundToInt(tyreSet.GetOptimalTyreDistance() / num2);
				if (this.GetLapsUntilDifferentTyreTreadRequired(tyreSet) < num3)
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
					if (tyreSet.GetCondition() < 0.65f)
					{
						tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
						if (tyreSet.GetCondition() < 0.65f && this.mChampionship.rules.compoundsAvailable > 2)
						{
							tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Third, null);
						}
					}
				}
			}
		}
		else if (tread == TyreSet.Tread.LightTread)
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
		}
		else if (tread == TyreSet.Tread.HeavyTread)
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
		}
		return tyreSet;
	}

	public TyreSet GetBestTyresForRightNow()
	{
		ChampionshipRules rules = this.mChampionship.rules;
		TyreSet.Tread recommendedTreadRightNow = SessionStrategy.GetRecommendedTreadRightNow();
		TyreSet tyreSet = null;
		TyreSet.Tread tread = recommendedTreadRightNow;
		if (tread != TyreSet.Tread.LightTread)
		{
			if (tread == TyreSet.Tread.HeavyTread)
			{
				tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Wets, null);
				if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Wets) <= 2)
				{
					tyreSet = null;
				}
			}
		}
		else
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Intermediates, null);
			if (this.GetMintConditionTyreCount(SessionStrategy.TyreOption.Intermediates) <= 2)
			{
				tyreSet = null;
			}
		}
		if (tyreSet == null)
		{
			tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.First, null);
			if (tyreSet.GetCondition() < 0.65f)
			{
				tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Second, null);
				if (tyreSet.GetCondition() < 0.65f && rules.compoundsAvailable > 2)
				{
					tyreSet = this.GetTyreInBestCondition(SessionStrategy.TyreOption.Third, null);
				}
			}
		}
		return tyreSet;
	}

	private int GetLapsUntilDifferentTyreTreadRequired(TyreSet inTyreSet)
	{
		float num = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles);
		int num2 = Mathf.RoundToInt(inTyreSet.GetOptimalTyreDistance() / num);
		float normalizedSessionTime = Game.instance.sessionManager.GetNormalizedSessionTime();
		for (int i = 0; i < num2; i++)
		{
			float inSessionTime = normalizedSessionTime + (float)i / (float)Game.instance.sessionManager.lapCount;
			if (this.ShouldPitForDifferentTyreTread(inTyreSet, inSessionTime, false))
			{
				return i;
			}
		}
		return int.MaxValue;
	}

	public bool ShouldPitForDifferentTyreTread(TyreSet inTyreSet, float inSessionTime = 0f, bool inMakeCheckForFutureStops = true)
	{
		if (MathsUtility.ApproximatelyZero(inSessionTime))
		{
			inSessionTime = Game.instance.sessionManager.GetNormalizedSessionTime();
		}
		SessionWeatherDetails currentSessionWeather = Game.instance.sessionManager.currentSessionWeather;
		TyreSet.Tread tread = inTyreSet.GetTread();
		int lapsRemaining = Game.instance.sessionManager.GetLapsRemaining();
		int num = Mathf.Min(lapsRemaining, 6);
		float wrongTreadForWaterLevelTimeCost = inTyreSet.GetWrongTreadForWaterLevelTimeCost();
		float num2 = 0f;
		float num3 = 30f;
		int num4 = 0;
		RacingVehicle leader = Game.instance.sessionManager.GetLeader();
		if (leader != null && leader.pathController.distanceAlongTrackPath01 > 0.75f)
		{
			num4++;
		}
		for (int i = num4; i < num; i++)
		{
			float num5 = inSessionTime + (float)i / (float)Game.instance.sessionManager.lapCount;
			if (MathsUtility.Approximately(num5, 1f, 0.001f) || num5 > 1f)
			{
				break;
			}
			float inNormalizedTrackWater = currentSessionWeather.sessionWaterLevelCurve.curve.Evaluate(num5);
			float performanceForSurfaceWater = TyrePerformance.GetPerformanceForSurfaceWater(this.mVehicle, tread, inNormalizedTrackWater);
			float num6 = 1f - performanceForSurfaceWater;
			num2 += num6;
			float num7 = wrongTreadForWaterLevelTimeCost * num2;
			if (i > num4 + 1 && ((num6 < 0.25f && num7 < num3) || num2 < 0.1f))
			{
				return false;
			}
			if (num7 > num3)
			{
				if (inMakeCheckForFutureStops && i > 2)
				{
					TyreSet bestTyresForTime = this.GetBestTyresForTime(inSessionTime);
					for (int j = num4 + 1; j <= i; j++)
					{
						num5 = inSessionTime + (float)j / (float)Game.instance.sessionManager.lapCount;
						if (this.ShouldPitForDifferentTyreTread(bestTyresForTime, num5, false))
						{
							return false;
						}
					}
				}
				return true;
			}
		}
		return false;
	}

	public void SetTeamOrders(SessionStrategy.TeamOrders inTeamOrders)
	{
		if (this.mTeamOrders == inTeamOrders)
		{
			return;
		}
		this.mTeamOrders = inTeamOrders;
		this.mUsedTeamOrders = true;
	}

	private void CreateTyreStrategyOptions()
	{
		ChampionshipRules rules = this.mChampionship.rules;
		int num = 12;
		if (rules.compoundsAvailable == 3)
		{
			num = 36;
		}
		this.mTyreStrategyOption = new TyreStrategyOption[num];
		for (int i = 0; i < this.mTyreStrategyOption.Length; i++)
		{
			this.mTyreStrategyOption[i] = new TyreStrategyOption();
			this.mTyreStrategyOption[i].SetStrategy(this);
		}
		this.mTyreStrategyOption[0].Create(new SessionStrategy.TyreOption[1]);
		this.mTyreStrategyOption[1].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.Second
		});
		this.mTyreStrategyOption[2].Create(new SessionStrategy.TyreOption[2]);
		this.mTyreStrategyOption[3].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.Second,
			SessionStrategy.TyreOption.Second
		});
		this.mTyreStrategyOption[4].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.First,
			SessionStrategy.TyreOption.Second
		});
		TyreStrategyOption tyreStrategyOption = this.mTyreStrategyOption[5];
		SessionStrategy.TyreOption[] array = new SessionStrategy.TyreOption[2];
		array[0] = SessionStrategy.TyreOption.Second;
		tyreStrategyOption.Create(array);
		this.mTyreStrategyOption[6].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.First,
			SessionStrategy.TyreOption.Second,
			SessionStrategy.TyreOption.Second
		});
		TyreStrategyOption tyreStrategyOption2 = this.mTyreStrategyOption[9];
		SessionStrategy.TyreOption[] array2 = new SessionStrategy.TyreOption[3];
		array2[1] = SessionStrategy.TyreOption.Second;
		tyreStrategyOption2.Create(array2);
		this.mTyreStrategyOption[10].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.First,
			SessionStrategy.TyreOption.First,
			SessionStrategy.TyreOption.Second
		});
		TyreStrategyOption tyreStrategyOption3 = this.mTyreStrategyOption[7];
		SessionStrategy.TyreOption[] array3 = new SessionStrategy.TyreOption[3];
		array3[0] = SessionStrategy.TyreOption.Second;
		array3[1] = SessionStrategy.TyreOption.Second;
		tyreStrategyOption3.Create(array3);
		this.mTyreStrategyOption[8].Create(new SessionStrategy.TyreOption[]
		{
			SessionStrategy.TyreOption.Second,
			SessionStrategy.TyreOption.First,
			SessionStrategy.TyreOption.Second
		});
		TyreStrategyOption tyreStrategyOption4 = this.mTyreStrategyOption[11];
		SessionStrategy.TyreOption[] array4 = new SessionStrategy.TyreOption[3];
		array4[0] = SessionStrategy.TyreOption.Second;
		tyreStrategyOption4.Create(array4);
		if (rules.compoundsAvailable == 3)
		{
			this.mTyreStrategyOption[12].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[13].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[14].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third
			});
			TyreStrategyOption tyreStrategyOption5 = this.mTyreStrategyOption[15];
			SessionStrategy.TyreOption[] array5 = new SessionStrategy.TyreOption[2];
			array5[0] = SessionStrategy.TyreOption.Third;
			tyreStrategyOption5.Create(array5);
			this.mTyreStrategyOption[16].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[17].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second
			});
			this.mTyreStrategyOption[18].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[19].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second
			});
			this.mTyreStrategyOption[20].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Second
			});
			TyreStrategyOption tyreStrategyOption6 = this.mTyreStrategyOption[21];
			SessionStrategy.TyreOption[] array6 = new SessionStrategy.TyreOption[3];
			array6[0] = SessionStrategy.TyreOption.Third;
			array6[1] = SessionStrategy.TyreOption.Second;
			tyreStrategyOption6.Create(array6);
			this.mTyreStrategyOption[22].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third
			});
			TyreStrategyOption tyreStrategyOption7 = this.mTyreStrategyOption[23];
			SessionStrategy.TyreOption[] array7 = new SessionStrategy.TyreOption[3];
			array7[0] = SessionStrategy.TyreOption.Second;
			array7[1] = SessionStrategy.TyreOption.Third;
			tyreStrategyOption7.Create(array7);
			this.mTyreStrategyOption[24].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third
			});
			TyreStrategyOption tyreStrategyOption8 = this.mTyreStrategyOption[25];
			SessionStrategy.TyreOption[] array8 = new SessionStrategy.TyreOption[3];
			array8[0] = SessionStrategy.TyreOption.Third;
			tyreStrategyOption8.Create(array8);
			TyreStrategyOption tyreStrategyOption9 = this.mTyreStrategyOption[26];
			SessionStrategy.TyreOption[] array9 = new SessionStrategy.TyreOption[3];
			array9[1] = SessionStrategy.TyreOption.Third;
			tyreStrategyOption9.Create(array9);
			this.mTyreStrategyOption[27].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[28].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.First,
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Third
			});
			TyreStrategyOption tyreStrategyOption10 = this.mTyreStrategyOption[29];
			SessionStrategy.TyreOption[] array10 = new SessionStrategy.TyreOption[3];
			array10[0] = SessionStrategy.TyreOption.Third;
			array10[1] = SessionStrategy.TyreOption.Third;
			tyreStrategyOption10.Create(array10);
			this.mTyreStrategyOption[30].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[31].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Second
			});
			this.mTyreStrategyOption[32].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second
			});
			this.mTyreStrategyOption[33].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[34].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Second,
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Third
			});
			this.mTyreStrategyOption[35].Create(new SessionStrategy.TyreOption[]
			{
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Third,
				SessionStrategy.TyreOption.Second
			});
		}
	}

	public void SetOrderedLapCount(int inLapCount)
	{
		this.mOrderedLapCount = this.mVehicle.timer.lapData.Count + inLapCount;
	}

	public SessionStrategy.Status status
	{
		get
		{
			return this.mStatus;
		}
	}

	public SessionStrategy.PitStrategy pitStrategy
	{
		get
		{
			return this.mPitStrategy;
		}
	}

	public List<TyreSet.Compound> tyreHistory
	{
		get
		{
			return this.mTyreHistory;
		}
	}

	public SessionStrategy.TeamOrders teamOrders
	{
		get
		{
			return this.mTeamOrders;
		}
	}

	public List<SessionStrategy.PitOrder> pitOrders
	{
		get
		{
			return this.mPitOrders;
		}
	}

	public bool isServingPitLanePenalty
	{
		get
		{
			return this.mPitlaneDriveTrough > 0;
		}
	}

	public SessionStrategy.Status previousStatus
	{
		get
		{
			return this.mPreviousStatus;
		}
	}

	public SessionStrategy.StintStrategyType stintStrategyType
	{
		get
		{
			return this.mStintStrategyType;
		}
	}

	public bool usedTeamOrders
	{
		get
		{
			return this.mUsedTeamOrders;
		}
	}

	public SessionStints.ReasonForStint reasonForPreviousPit
	{
		get
		{
			return this.mReasonForPreviousPit;
		}
	}

	public TyreSet lockedTyres
	{
		get
		{
			return this.mLockedTyres;
		}
	}

	private const float lapDistanceMultiplier = 0.6666667f;

	private RacingVehicle mVehicle;

	private SessionStrategy.Status mStatus;

	private SessionStrategy.Status mQueuedStatus;

	private SessionStrategy.Status mPreviousStatus;

	private SessionStrategy.PitStrategy mPitStrategy = SessionStrategy.PitStrategy.Balanced;

	private SessionStrategy.TeamOrders mTeamOrders;

	private SessionStrategy.StintStrategyType mStintStrategyType;

	private TyreSet[] mFirstTyreOption;

	private TyreSet[] mSecondTyreOption;

	private TyreSet[] mThirdTyreOption;

	private TyreSet[] mIntermediates;

	private TyreSet[] mWets;

	private TyreSet mLockedTyres;

	private List<SessionStrategy.PitOrder> mPitOrders = new List<SessionStrategy.PitOrder>();

	private List<TyreSet.Compound> mTyreHistory = new List<TyreSet.Compound>();

	private Championship mChampionship;

	private bool mRefreshCarCrashData;

	private bool mUsedTeamOrders;

	private TyreStrategyOption[] mTyreStrategyOption;

	private int mOrderedLapCount;

	private int mPitlaneDriveTrough;

	private SessionStints.ReasonForStint mReasonForPreviousPit = SessionStints.ReasonForStint.None;

	private bool mUsesAIForStrategy;

	private TargetPointSteeringBehaviour mTargetPointSteeringBehaviour;

	public enum Status
	{
		NoActionRequired,
		Pitting,
		ReturningToGarage,
		WaitingForSetupCompletion,
		PitThruPenalty,
		Crashing
	}

	public enum PitStrategy
	{
		[LocalisationID("PSG_10005763")]
		Safe,
		[LocalisationID("PSG_10005764")]
		Balanced,
		[LocalisationID("PSG_10005765")]
		Fast
	}

	public enum TyreOption
	{
		First,
		Second,
		Third,
		Intermediates,
		Wets
	}

	public enum PitOrder
	{
		Tyres,
		Refuel,
		FixPart,
		Recharge,
		ChangeDriver,
		CancelPit
	}

	public enum TeamOrders
	{
		Race,
		AllowTeamMateThrough
	}

	public enum StintStrategyType
	{
		Normal,
		Conservative
	}
}
