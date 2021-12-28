using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Driver : Person
{
	public Driver()
	{
	}

	public override void OnStart()
	{
		base.OnStart();
		this.personalityTraitController = new PersonalityTraitController_v2(this);
		this.driverStamina = new DriverStamina();
		this.driverForm = new DriverForm();
		this.mDriverRivalries.Start(this);
	}

	public override void Setup()
	{
		base.Setup();
		this.driverStamina.OnStart(this);
		this.driverForm.OnStart(this);
	}

	public override PersonStats GetStats()
	{
		DriverStats driverStats = null;
		if (this.personalityTraitController != null)
		{
			driverStats = this.personalityTraitController.GetDriverStatsModifier();
		}
		else
		{
			global::Debug.LogErrorFormat("{0} does not have  personality trait controller", new object[]
			{
				this.name
			});
		}
		this.mModifiedStats.Clear();
		if (driverStats != null)
		{
			this.mModifiedStats.Add(driverStats);
		}
		this.mModifiedStats.Add(this.mStats);
		this.mModifiedStats.marketability += driverStats.marketability;
		this.mModifiedStats.marketability += this.mStats.marketability;
		if (this.IsPlayersDriver())
		{
			this.mModifiedStats.feedback += Game.instance.player.driverFeedBackStatModifier;
		}
		this.mModifiedStats.ClampStats();
		return this.mModifiedStats;
	}

	public override void OnLoad()
	{
		base.OnLoad();
		if (this.driverStamina == null)
		{
			this.driverStamina = new DriverStamina();
			this.driverStamina.OnStart(this);
		}
		if (this.driverForm == null)
		{
			this.driverForm = new DriverForm();
			this.driverForm.OnStart(this);
		}
		if (this.mPreferedSeries != Championship.Series.Count)
		{
			if (!this.HasPreferedSeries(this.mPreferedSeries, true))
			{
				this.mDriverPreferedSeries.Add(this.mPreferedSeries);
			}
			this.mPreferedSeries = Championship.Series.Count;
		}
		this.mDriverPreferedSeries = new List<Championship.Series>(new HashSet<Championship.Series>(this.mDriverPreferedSeries));
		this.personalityTraitController.OnLoad();
		this.mDriverRivalries.OnLoad();
		this.driverStamina.OnLoad();
	}

	public void PrepareForSession()
	{
		this.driverStamina.Reset();
	}

	public float GetStatsForAITeamComparison(Team inTeam)
	{
		this.mStatsForAITeamEval.Clear();
		this.mStatsForAITeamEval.Add(this.mStats);
		this.personalityTraitController.GetDriverPermanentStatsModifier(ref this.mStatsForAITeamEval);
		if (inTeam != null && inTeam.championship.series == Championship.Series.EnduranceSeries)
		{
			this.mStatsForAITeamEval.Multiply(0.5f);
			this.mStatsForAITeamEval.fitness *= 2f;
			this.mStatsForAITeamEval.focus *= 2f;
		}
		return this.mStatsForAITeamEval.GetAbility();
	}

	public void GetTraitRefreshTypes(out bool OutOnEnterGate, out bool OutOnNewSetup, out bool OutOnNewLap)
	{
		if (this.personalityTraitController != null)
		{
			if (this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.P1InRace) || this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.P2InRace) || this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.WetSession) || this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.OneOnOne))
			{
				OutOnEnterGate = true;
			}
			else
			{
				OutOnEnterGate = false;
			}
			if (this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.IntermediateTyres))
			{
				OutOnNewSetup = true;
			}
			else
			{
				OutOnNewSetup = false;
			}
			if (this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.RaceLap1))
			{
				OutOnNewLap = true;
			}
			else
			{
				OutOnNewLap = false;
			}
		}
		else
		{
			OutOnEnterGate = false;
			OutOnNewSetup = false;
			OutOnNewLap = false;
		}
	}

	public DriverStats GetDriverStats()
	{
		return (DriverStats)this.GetStats();
	}

	public void SetDriverStats(DriverStats inDriverStats, int inPotential)
	{
		this.mStats = inDriverStats;
		this.SetPotential(inPotential);
		this.mStats.UpdatePotentialWithPeakAge(this.peakAge);
		this.mModifiedStats.Copy(this.mStats);
	}

	public void ResetChampionshipEntry()
	{
		this.mChampionshipEntry = null;
	}

	public ChampionshipEntry_v1 GetChampionshipEntry()
	{
		if (!this.IsFreeAgent())
		{
			Championship championship = this.contract.GetTeam().championship;
			if (this.mChampionshipEntry == null || this.mChampionshipEntry.championship != championship)
			{
				this.mChampionshipEntry = championship.standings.GetEntry(this);
			}
		}
		return this.mChampionshipEntry;
	}

	public bool IsInAChampionship()
	{
		return this.GetChampionshipEntry() != null;
	}

	public bool IsMainDriver()
	{
		return this.contract.currentStatus != ContractPerson.Status.Reserve;
	}

	public bool IsReserveDriver()
	{
		return this.contract.currentStatus == ContractPerson.Status.Reserve;
	}

	public bool IsPlayersDriver()
	{
		return this.contract != null && this.contract.GetTeam().IsPlayersTeam();
	}

	public Driver GetRivalDriver()
	{
		return this.mDriverRivalries.currentRival;
	}

	public void SetRivalDriver(Driver inDriver)
	{
		this.mDriverRivalries.SetRivalDriver(inDriver);
	}

	public void DebugPickRandomRival()
	{
		this.mDriverRivalries.PickRandomRival();
	}

	public string GetPotentialString()
	{
		float potentialValue = this.GetPotentialValue();
		if (potentialValue >= 18f)
		{
			return "World Class";
		}
		if (potentialValue >= 15f)
		{
			return "Great";
		}
		if (potentialValue >= 12f)
		{
			return "Good";
		}
		if (potentialValue >= 8f)
		{
			return "Average";
		}
		return "Bad";
	}

	public override float GetReputationValue()
	{
		float num = 0f;
		num += this.mStats.braking;
		num += this.mStats.cornering;
		num += this.mStats.smoothness;
		num += this.mStats.overtaking;
		num += this.mStats.consistency;
		num += this.mStats.adaptability;
		num += this.mStats.fitness;
		num += this.mStats.feedback;
		num += this.mStats.focus;
		return num / 9f;
	}

	public void ReduceInjuriesTraitTime()
	{
		List<PersonalityTrait> allPersonalityTraits = this.personalityTraitController.GetAllPersonalityTraits();
		for (int i = 0; i < allPersonalityTraits.Count; i++)
		{
			if (allPersonalityTraits[i].data.IsPartOfSet(GameStatsConstants.injuryTraits))
			{
				allPersonalityTraits[i].traitEndTime = allPersonalityTraits[i].traitEndTime.AddDays((double)(-(double)GameStatsConstants.daysRecoveredFromSittingOut));
			}
		}
	}

	public bool IsInjured()
	{
		return this.personalityTraitController.HasTrait(false, GameStatsConstants.injuryTraits);
	}

	public bool IsCriticalInjured()
	{
		return this.personalityTraitController.HasTrait(false, GameStatsConstants.criticalInjuryTraits);
	}

	public override bool IsReplacementPerson()
	{
		if (!this.mHasCachedReplacementDriverInfo)
		{
			this.mHasCachedReplacementDriverInfo = true;
			DriverManager driverManager = Game.instance.driverManager;
			this.mIsReplacementDriver = driverManager.IsReplacementPerson(this);
		}
		return this.mIsReplacementDriver;
	}

	public override int GetPersonIndexInManager()
	{
		DriverManager driverManager = Game.instance.driverManager;
		return driverManager.GetPersonIndex(this);
	}

	private float GetPotentialValue()
	{
		float num = 0f;
		num += this.mStats.braking;
		num += this.mStats.cornering;
		num += this.mStats.smoothness;
		num += this.mStats.overtaking;
		num += this.mStats.consistency;
		num += this.mStats.adaptability;
		num += this.mStats.fitness;
		num += this.mStats.feedback;
		num += this.mStats.focus;
		num += this.mModifiedPotential;
		return num * 0.1f;
	}

	public void SetPotential(int newPotential)
	{
		this.mPotential = (float)Math.Min(newPotential, this.mStats.GetMaxPotential());
		this.mModifiedPotential = this.mPotential;
		this.UpdateModifiedPotentialValue(0f);
	}

	public void UpdateModifiedPotentialValue(float inPotentialModifier)
	{
		this.mModifiedPotential += inPotentialModifier;
		this.mModifiedPotential = Mathf.Clamp(this.mModifiedPotential, 0f, (float)this.mStats.GetMaxPotential());
		this.mStats.SetMaxFromPotential((int)this.mModifiedPotential);
		this.mModifiedStats.totalStatsMax = this.mStats.totalStatsMax;
	}

	public float GetImprovementRateForAge(DateTime inDate)
	{
		if (this.IsReserveDriver())
		{
			return base.GetImprovementRateForAgeWithBonus(inDate, this.peakAge, this.peakDuration, this.GetImprovementRate());
		}
		return base.GetImprovementRateForAge(inDate, this.peakAge, this.peakDuration, this.GetImprovementRate());
	}

	public override float GetStatsValue()
	{
		return this.mStats.GetUnitAverage();
	}

	public void SetBeenScouted()
	{
		this.mHasBeenScouted = true;
	}

	public int GetChampionshipExpectedPosition()
	{
		return this.expectedChampionshipPosition;
	}

	public void SetLastRaceExpectedPosition()
	{
		this.mLastRaceExpectedRacePosition = this.expectedRacePosition;
	}

	public int GetRaceExpectedPosition()
	{
		return this.expectedRacePosition;
	}

	public override bool CanShowStats()
	{
		if (!Game.IsActive())
		{
			return false;
		}
		if (this.contract.GetTeam() == Game.instance.player.team || this.IsReplacementPerson())
		{
			return true;
		}
		if (this.mStats.fame != -1 && !this.mHasBeenScouted && !Game.instance.player.IsUnemployed())
		{
			Team team = Game.instance.player.team;
			HQsBuilding_v1 building = team.headquarters.GetBuilding(HQsBuildingInfo.Type.ScoutingFacility);
			if (building != null && building.isBuilt)
			{
				return building.currentLevel >= this.mStats.fame;
			}
		}
		return this.mHasBeenScouted;
	}

	public override ContractNegotiationScreen.NegotatiationType GetNecessaryNegotiationType()
	{
		if (this.IsFreeAgent())
		{
			return ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed;
		}
		if (this.contract.GetTeam() == Game.instance.player.team)
		{
			return ContractNegotiationScreen.NegotatiationType.RenewDriver;
		}
		return ContractNegotiationScreen.NegotatiationType.NewDriver;
	}

	public override ContractNegotiationScreen.NegotatiationType GetNecessaryNegotiationType(Team team)
	{
		if (this.IsFreeAgent())
		{
			return ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed;
		}
		if (this.contract.GetTeam() == team)
		{
			return ContractNegotiationScreen.NegotatiationType.RenewDriver;
		}
		return ContractNegotiationScreen.NegotatiationType.NewDriver;
	}

	public override float GetMorale()
	{
		float value = base.GetMorale() + this.personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.Morale);
		return Mathf.Clamp01(value);
	}

	public override void ModifyMorale(float inModifierValue, string inModifierNameID, bool inOverwriteEntryWithSameName = false)
	{
		base.ModifyMorale(inModifierValue, inModifierNameID, inOverwriteEntryWithSameName);
		this.UpdateDriverMoraleAchievements();
		if (this.GetMorale() < this.lowMoraleOpenToOffersAmount)
		{
			if (!this.IsOpenToOffers() && inModifierValue < 0f)
			{
				this.lowMoraleStartTime = Game.instance.time.now;
				if (this.contract.GetTeam().IsPlayersTeam())
				{
					Game.instance.dialogSystem.OnOpenToOffersMessages(this);
				}
			}
		}
		else if (this.GetMorale() > this.lowMoraleStopListeningToOffersAmount && this.IsOpenToOffers() && inModifierValue > 0f)
		{
			this.lowMoraleStartTime = Game.instance.time.now.AddDays((double)(-(double)(this.lowMoraleOpenToOffersDays + 1)));
			if (this.contract.GetTeam().IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnStopListeningToOffersMessages(this);
			}
		}
		if (!this.IsFreeAgent() && this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.ChairmansHappinessMirrorsDriver))
		{
			StringVariableParser.stringValue1 = Localisation.LocaliseID(inModifierNameID, null);
			this.contract.GetTeam().chairman.ModifyHappiness((float)Mathf.RoundToInt(inModifierValue * 50f), Localisation.LocaliseID("PSG_10012661", null));
		}
	}

	public override void SetMorale(float inMoraleValue)
	{
		base.SetMorale(inMoraleValue);
		this.UpdateDriverMoraleAchievements();
	}

	public void SetImprovementRate(float newImprovementRate)
	{
		this.mImprovementRate = newImprovementRate;
	}

	public float GetImprovementRate()
	{
		float singleModifierForStat = this.personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.Improveability);
		if (!this.IsFreeAgent())
		{
			Team team = this.contract.GetTeam();
			if (team.investor != null && team.investor.hasDriverImprovementRate)
			{
				return this.mImprovementRate * team.investor.driverImprovementRateMultiplier + singleModifierForStat;
			}
		}
		return this.mImprovementRate + singleModifierForStat;
	}

	public void SetDesiredWins(int inDesiredWins)
	{
		this.mDesiredWins = inDesiredWins;
	}

	public int GetDesiredWins()
	{
		int num = Mathf.RoundToInt(this.personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.DesiredWins));
		return Mathf.Max(0, this.mDesiredWins + num);
	}

	public void SetDesiredEarnings(long inDesiredEarnings)
	{
		this.mDesiredEarnings = inDesiredEarnings;
	}

	public long GetDesiredEarnings()
	{
		long num = (long)this.personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.DesiredEarnings);
		long num2 = this.mDesiredEarnings + num;
		if (num2 < 0L)
		{
			num2 = 0L;
		}
		return num2;
	}

	public void SetPreferedSeries(Championship.Series inSeries)
	{
		this.mDriverPreferedSeries.Clear();
		this.mDriverPreferedSeries.Add(inSeries);
		this.mJoinsAnySeries = this.CanJoinAnySeries();
	}

	public void SetPreferedSeries(List<Championship.Series> inSeries)
	{
		this.mDriverPreferedSeries = inSeries;
		this.mJoinsAnySeries = this.CanJoinAnySeries();
	}

	public void AddPreferedSeries(Championship.Series inSeries)
	{
		if (!this.HasPreferedSeries(inSeries, false))
		{
			this.mDriverPreferedSeries.Add(inSeries);
			this.mJoinsAnySeries = this.CanJoinAnySeries();
		}
	}

	public void RemovePreferedSeries(Championship.Series inSeries)
	{
		if (this.HasPreferedSeries(inSeries, false))
		{
			this.mDriverPreferedSeries.Remove(inSeries);
			this.mJoinsAnySeries = this.CanJoinAnySeries();
		}
	}

	public bool HasPreferedSeries(Championship.Series inSeries, bool inCheckAnySeries = true)
	{
		if (inCheckAnySeries && this.mJoinsAnySeries)
		{
			return true;
		}
		for (int i = 0; i < this.mDriverPreferedSeries.Count; i++)
		{
			if (this.mDriverPreferedSeries[i] == inSeries)
			{
				return true;
			}
		}
		return false;
	}

	private bool CanJoinAnySeries()
	{
		for (int i = 0; i < 3; i++)
		{
			if (!this.HasPreferedSeries((Championship.Series)i, false))
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateDriverMoraleAchievements()
	{
		if (this.IsPlayersDriver())
		{
			if (this.GetMorale() == 1f)
			{
				App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Max_Morale_Driver);
			}
			if (this.GetMorale() <= 0.01f)
			{
				App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Min_Morale_Driver);
			}
		}
	}

	public void SetCarID(int inCarID)
	{
		this.mCarID = Mathf.Clamp(inCarID, -1, 1);
	}

	public void UpdateMoraleWithPromotionDemotion()
	{
		if (this.mLastMoraleBonusDate == DateTime.MaxValue || (Game.instance.time.now - this.mLastMoraleBonusDate).TotalDays >= (double)this.moraleBonusCooldownDays)
		{
			if (this.contract.currentStatus == ContractPerson.Status.Reserve)
			{
				this.ModifyMorale(this.moraleDemotionBonus, "PSG_10010970", false);
			}
			else
			{
				this.ModifyMorale(this.moralePromotionBonus, "PSG_10010969", false);
			}
			this.mLastMoraleBonusDate = Game.instance.time.now;
		}
	}

	public void UpdateMoraleStatusAgainstWhatWasPromised()
	{
		if (this.mLastMoraleBonusDate == DateTime.MaxValue || (Game.instance.time.now - this.mLastMoraleBonusDate).TotalDays >= (double)this.moraleBonusCooldownDays)
		{
			ContractPerson.Status proposedStatus = this.contract.proposedStatus;
			ContractPerson.Status currentStatus = this.contract.currentStatus;
			bool flag = proposedStatus == ContractPerson.Status.Equal || proposedStatus == ContractPerson.Status.One || proposedStatus == ContractPerson.Status.Two;
			bool flag2 = currentStatus == ContractPerson.Status.Equal || currentStatus == ContractPerson.Status.One || currentStatus == ContractPerson.Status.Two;
			if (flag && !flag2)
			{
				this.ModifyMorale(this.moraleWorseContractBonus, "PSG_10010968", false);
			}
			else if (!flag && flag2)
			{
				this.ModifyMorale(this.moraleBetterContractBonus, "PSG_10010971", false);
			}
			this.mLastMoraleBonusDate = Game.instance.time.now;
		}
	}

	public void UpdateMoraleOnFired()
	{
		this.ModifyMorale(this.moraleFiredBonus, "PSG_10007187", false);
	}

	public void UpdateMoraleOnContractSigned()
	{
		this.ModifyMorale(this.moraleSignedContractBonus, "PSG_10011134", false);
	}

	public bool canBePromoted
	{
		get
		{
			return !this.IsReplacementPerson();
		}
	}

	public bool hasBeenScouted
	{
		get
		{
			return this.mHasBeenScouted;
		}
	}

	public int lastRaceExpectedRacePosition
	{
		get
		{
			return this.mLastRaceExpectedRacePosition;
		}
	}

	public int daysToScoutShort
	{
		get
		{
			return this.mDaysToScoutShort;
		}
	}

	public int daysToScoutLong
	{
		get
		{
			return this.mDaysToScoutLong;
		}
	}

	public Person celebrity
	{
		get
		{
			return this.mCelebrity;
		}
		set
		{
			this.mCelebrity = value;
		}
	}

	public List<Championship.Series> preferedSeries
	{
		get
		{
			return this.mDriverPreferedSeries;
		}
	}

	public bool joinsAnySeries
	{
		get
		{
			return this.mJoinsAnySeries;
		}
		set
		{
			this.mJoinsAnySeries = value;
		}
	}

	private int GetCarID()
	{
		if (this.IsReserveDriver())
		{
			return -1;
		}
		Team team = this.contract.GetTeam();
		if (team.GetDriver(0) == this)
		{
			return 0;
		}
		return 1;
	}

	public int carID
	{
		get
		{
			if (this.mCarID >= 0)
			{
				return this.mCarID;
			}
			return this.GetCarID();
		}
	}

	public static void SortListByAbilityStars(List<Driver> inDriversList)
	{
		inDriversList.Sort(delegate(Driver X, Driver Y)
		{
			if (!X.CanShowStats() && !Y.CanShowStats())
			{
				return 0;
			}
			if (!X.CanShowStats())
			{
				return -1;
			}
			if (!Y.CanShowStats())
			{
				return 1;
			}
			return X.mStats.GetAbility().CompareTo(Y.mStats.GetAbility());
		});
	}

	public static void SortListByPotential(List<Driver> inDriversList)
	{
		inDriversList.Sort((Driver X, Driver Y) => X.GetDriverStats().GetAbilityPotential().CompareTo(Y.GetDriverStats().GetAbilityPotential()));
	}

	public void UpdateStatsForAge(DriverStats inAccStats, float inTimePassed)
	{
		if (Game.instance.driverManager.ageDriverStatProgression != null)
		{
			inAccStats.ApplyStatsProgression(Game.instance.driverManager.ageDriverStatProgression, inTimePassed * this.GetImprovementRateForAge(Game.instance.time.now));
		}
	}

	public void AccumulateDailyStats(DriverStatsProgression inStatsProgression, string inStatIdentifier)
	{
		this.AccumulateDailyStats(this.accumulatedStats, inStatsProgression, inStatIdentifier);
	}

	public void AccumulateDailyStats(DriverStats inStatsAccumulate, DriverStatsProgression inStatsProgression, string inStatIdentifier)
	{
		float improvementRateForAge = this.GetImprovementRateForAge(Game.instance.time.now);
		if (this.contract.GetTeam() != Game.instance.player.team || this.contract.GetTeam() == Game.instance.player.team)
		{
		}
		inStatsAccumulate.ApplyStatsProgression(inStatsProgression, PersonConstants.statIncreaseTimePerDay * improvementRateForAge);
		if (this.contract.GetTeam() == Game.instance.player.team)
		{
		}
	}

	public void OnSessionEnd()
	{
		RaceEventDetails eventDetails = Game.instance.sessionManager.eventDetails;
		SessionDetails.SessionType sessionType = eventDetails.currentSession.sessionType;
		if (sessionType == SessionDetails.SessionType.Race && this.IsMainDriver())
		{
			this.mDriverRivalries.UpdateRivalries();
			this.personalityTraitController.CheckPersonalityTraitEventTrigger(PersonalityTraitData.EventTriggerType.PostRace);
			this.personalityTraitController.CheckIfCarPartPromiseIsFulfilled();
		}
		this.UpdateStatsForAge(this.accumulatedStats, PersonConstants.statIncreaseTimePerDay);
		this.AddTraitsImprovementBonus(this.accumulatedStats, PersonConstants.statIncreaseTimePerDay);
		if (Game.instance.driverManager.maxDriverStatProgressionPerDay != null)
		{
			this.accumulatedStats.LimitToDailyMax(Game.instance.driverManager.maxDriverStatProgressionPerDay);
		}
		if (this == this.contract.GetTeam().GetReserveDriver())
		{
			RaceEventResults.ResultData resultForDriver = eventDetails.results.GetResultsForSession(SessionDetails.SessionType.Practice).GetResultForDriver(this);
			if (resultForDriver != null)
			{
				this.accumulatedStats += this.accumulatedStats;
			}
		}
		float total = this.accumulatedStats.GetTotal();
		if (total < 0f || (total > 0f && this.mStats.GetPotential() > 0f))
		{
			this.mStats += this.accumulatedStats;
			this.mPotential -= total;
			this.mPotential = Mathf.Max(0f, this.mPotential);
			this.mModifiedPotential -= total;
		}
		this.accumulatedStats.Clear();
		ChampionshipRules rules = this.contract.GetTeam().championship.rules;
		if (sessionType == SessionDetails.SessionType.Qualifying && (!rules.qualifyingBasedActive || (rules.gridSetup == ChampionshipRules.GridSetup.QualifyingBased3Sessions && !eventDetails.IsLastSessionOfType())))
		{
			return;
		}
		float inOverallWeight = 1f;
		switch (sessionType)
		{
		case SessionDetails.SessionType.Practice:
			inOverallWeight = this.moralePracticeWeight;
			break;
		case SessionDetails.SessionType.Qualifying:
			inOverallWeight = this.moraleQualifyingWeight;
			break;
		case SessionDetails.SessionType.Race:
			inOverallWeight = this.moraleRaceWeight;
			break;
		}
		RaceEventResults.SessonResultData resultsForSession = Game.instance.sessionManager.eventDetails.results.GetResultsForSession(sessionType);
		RaceEventResults.ResultData resultForDriver2 = resultsForSession.GetResultForDriver(this);
		bool flag = this.contract.GetTeam().championship.series == Championship.Series.EnduranceSeries;
		if (resultForDriver2 != null)
		{
			string locationNameID = Game.instance.sessionManager.eventDetails.circuit.locationNameID;
			int inPosition = resultForDriver2.position;
			if (flag)
			{
				int num = 1;
				for (int i = 0; i < resultsForSession.resultData.Count; i++)
				{
					if (resultsForSession.resultData[i].team.championship == resultForDriver2.team.championship)
					{
						if (resultsForSession.resultData[i] == resultForDriver2)
						{
							break;
						}
						num++;
					}
				}
				inPosition = num;
			}
			this.UpdateSessionMorale(locationNameID, inOverallWeight, inPosition, resultsForSession.resultData.Count, sessionType);
		}
		else if (flag && sessionType == SessionDetails.SessionType.Qualifying)
		{
			this.ModifyMorale(-0.05f, Localisation.LocaliseID("PSG_10013878", null), true);
		}
		if (sessionType == SessionDetails.SessionType.Race)
		{
			this.UpdateContractMoralePerRace();
		}
	}

	public void CalculateAccumulatedStatsForDay(DriverStats inAccStats)
	{
		float improvementRateForAge = this.GetImprovementRateForAge(Game.instance.time.now);
		if (Game.instance.driverManager.ageDriverStatProgression != null)
		{
			inAccStats.ApplyStatsProgression(Game.instance.driverManager.ageDriverStatProgression, PersonConstants.statIncreaseTimePerDay * improvementRateForAge);
		}
		Team team = this.contract.GetTeam();
		if (team != null && team.headquarters != null)
		{
			int count = team.headquarters.hqBuildings.Count;
			for (int i = 0; i < count; i++)
			{
				HQsBuilding_v1 hqsBuilding_v = team.headquarters.hqBuildings[i];
				if (hqsBuilding_v.activeDriverStatProgression)
				{
					this.CalculateAccumulatedStatsForBuilding(improvementRateForAge, inAccStats, hqsBuilding_v);
				}
			}
			for (int j = 0; j < Team.maxDriverCount; j++)
			{
				Driver driver = team.GetDriver(j);
				if (driver != null)
				{
					List<PersonalityTrait> allPersonalityTraits = driver.personalityTraitController.GetAllPersonalityTraits();
					for (int k = 0; k < allPersonalityTraits.Count; k++)
					{
						PersonalityTrait personalityTrait = allPersonalityTraits[k];
						bool flag = this != driver && (personalityTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityBoost) || personalityTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityDebuff));
						if (flag && personalityTrait.teamDailyImprovementModifier != 0f)
						{
							this.AddTraitImprovementBonus(personalityTrait, inAccStats, PersonConstants.statIncreaseTimePerDay);
						}
					}
				}
			}
		}
		if (this.IsPlayersDriver() && Game.instance.player.driverImprovementRateModifier != 0f)
		{
			this.CalculateAccumulatedStatsForBackstory(Game.instance.player.driverImprovementRateModifier, inAccStats);
		}
		if (improvementRateForAge < 0f)
		{
			inAccStats.MinToZero();
		}
		else if (Game.instance.driverManager.maxDriverStatProgressionPerDay != null)
		{
			inAccStats.LimitToDailyMax(Game.instance.driverManager.maxDriverStatProgressionPerDay);
		}
	}

	public void CalculateAccumulatedStatsForBuilding(float inImprovementRateForAge, DriverStats inAccStats, HQsBuilding_v1 inBuilding)
	{
		float num = inImprovementRateForAge;
		if (inImprovementRateForAge < 0f)
		{
			num = (1f + inImprovementRateForAge * this.negativeImprovementHQScalar) * this.negativeImprovementHQOverallScalar;
			num = Mathf.Max(0f, num);
			num = Math.Min(this.negativeMaxImprovementHQ, num);
		}
		inAccStats.ApplyStatsProgression(inBuilding.driverStatProgression, PersonConstants.statIncreaseTimePerDay * num);
	}

	public void CalculateAccumulatedStatsForBackstory(float inImprovementRate, DriverStats inAccStats)
	{
		inAccStats.ApplyStatsProgression(Game.instance.driverManager.ageDriverStatProgression, PersonConstants.statIncreaseTimePerDay * inImprovementRate);
	}

	public void OnDayEnd()
	{
		this.personalityTraitController.UpdatePersonalityTraits();
		this.accumulatedStats.CopyImprovementRates(this.mStats);
		this.CalculateAccumulatedStatsForDay(this.accumulatedStats);
		this.AddTraitsImprovementBonus(this.accumulatedStats, PersonConstants.statIncreaseTimePerDay);
		float total = this.accumulatedStats.GetTotal();
		if (total < 0f || (total > 0f && this.mStats.GetPotential() > 0f))
		{
			this.mStats += this.accumulatedStats;
			this.mPotential -= total;
			this.mPotential = Mathf.Max(0f, this.mPotential);
			this.mModifiedPotential -= total;
		}
		this.mStats.UpdatePotentialWithPeakAge(this.peakAge);
		this.lastAccumulatedStats = new DriverStats(this.accumulatedStats);
		this.accumulatedStats.Clear();
	}

	public void AddTraitsImprovementBonus(DriverStats inAccumulatedStats, float inRate)
	{
		if (!this.IsFreeAgent())
		{
			Team team = this.contract.GetTeam();
			if (!(team is NullTeam))
			{
				for (int i = 0; i < Team.maxDriverCount; i++)
				{
					Driver driver = team.GetDriver(i);
					if (driver != null)
					{
						List<PersonalityTrait> allPersonalityTraits = driver.personalityTraitController.GetAllPersonalityTraits();
						for (int j = 0; j < allPersonalityTraits.Count; j++)
						{
							PersonalityTrait personalityTrait = allPersonalityTraits[j];
							bool flag = driver != this && (personalityTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityBoost) || personalityTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityDebuff));
							if (flag)
							{
								this.AddTraitImprovementBonus(personalityTrait, inAccumulatedStats, inRate);
							}
						}
					}
				}
			}
		}
	}

	public void AddTraitImprovementBonus(PersonalityTrait inTrait, DriverStats inAccumulatedStats, float inDailyRate)
	{
		if (inTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityBoost) || inTrait.HasSpecialCase(PersonalityTrait.SpecialCaseType.MentorImproveabilityDebuff))
		{
			float num = inTrait.teamDailyImprovementModifier * inDailyRate;
			inAccumulatedStats.braking += num;
			inAccumulatedStats.cornering += num;
			inAccumulatedStats.smoothness += num;
			inAccumulatedStats.overtaking += num;
			inAccumulatedStats.consistency += num;
			inAccumulatedStats.adaptability += num;
			inAccumulatedStats.fitness += num;
			inAccumulatedStats.feedback += num;
			inAccumulatedStats.focus += num;
		}
	}

	public override bool HasAchievedCareerGoals()
	{
		bool flag = this.careerHistory.GetTotalCareerChampionships() >= this.desiredChampionships;
		bool flag2 = this.careerHistory.GetTotalCareerWins() >= this.GetDesiredWins();
		bool flag3 = (long)this.contract.yearlyWages >= this.desiredBudget;
		return flag && flag2 && flag3;
	}

	public override float GetExpectation(DatabaseEntry inWeightings)
	{
		float num = (float)inWeightings.GetIntValue("Experience") / 100f;
		float num2 = (float)inWeightings.GetIntValue("Quality") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Team Reputation") / 100f;
		float num4 = 0f;
		float num5 = this.GetExperience() * num;
		float num6 = this.mStats.GetUnitAverage() * num2;
		if (this.contract.job != Contract.Job.Unemployed)
		{
			num4 += (float)this.contract.GetTeam().reputation / 100f * num3;
		}
		float num7 = num5 + num6 + num4;
		return num7 / (num + num2 + num3);
	}

	public float GetRaceExpectation(DatabaseEntry inWeightings, CarStats inTrackCharacteristics)
	{
		float num = (float)inWeightings.GetIntValue("Driver Car") / 100f;
		float num2 = (float)inWeightings.GetIntValue("Experience") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Quality") / 100f;
		float num4 = (float)inWeightings.GetIntValue("Team Reputation") / 100f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = this.GetExperience() * num2;
		float num8 = this.mStats.GetUnitAverageForTrack(inTrackCharacteristics) * num3;
		if (this.contract.job != Contract.Job.Unemployed)
		{
			num6 = this.contract.GetTeam().carManager.GetCarForDriver(this).GetStatsForTrack(inTrackCharacteristics).GetStarsRating() / 5f * num;
			num5 = (float)this.contract.GetTeam().reputation / 100f * num4;
		}
		float num9 = num7 + num6 + num8 + num5;
		return num9 / (num2 + num + num3 + num4);
	}

	public override float GetAchievements(DatabaseEntry inWeightings)
	{
		float num = (float)inWeightings.GetIntValue("Driver Results") / 100f;
		float num2 = (float)inWeightings.GetIntValue("Driver Qualifying") / 100f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		ChampionshipEntry_v1 championshipEntry = this.GetChampionshipEntry();
		if (championshipEntry != null && championshipEntry.races > 0)
		{
			num4 = (float)championshipEntry.GetCurrentPoints() / (float)championshipEntry.races / (float)championshipEntry.championship.rules.GetPointsForPosition(1);
			num5 = (float)championshipEntry.GetNumberOfPoles() / (float)championshipEntry.races;
		}
		num3 += num4 * num + num5 * num2;
		return num3 / (num + num2);
	}

	public void UpdateContractMoralePerRace()
	{
		ContractPerson.Status proposedStatus = this.contract.proposedStatus;
		ContractPerson.Status currentStatus = this.contract.currentStatus;
		bool flag = proposedStatus == ContractPerson.Status.Equal || proposedStatus == ContractPerson.Status.One || proposedStatus == ContractPerson.Status.Two;
		bool flag2 = currentStatus == ContractPerson.Status.Equal || currentStatus == ContractPerson.Status.One || currentStatus == ContractPerson.Status.Two;
		if (flag && !flag2)
		{
			this.ModifyMorale(this.moraleWorseContractPerRace, "PSG_10011120", true);
		}
		else if (!flag && flag2)
		{
			this.ModifyMorale(this.moraleBetterContractPerRace, "PSG_10011121", true);
		}
	}

	public void UpdateSessionMorale(string inUpdateHistoryEntryName, float inOverallWeight, int inPosition, int inNumEntries, SessionDetails.SessionType inSessionType)
	{
		bool isRace = inSessionType == SessionDetails.SessionType.Race;
		
		// morale change for absolute position
		// Podidium +3% (moraleSessionPodiumBonus)
		// expectedPosition +/-2.5% (moraleFailedExpectedPositionBonus + moraleAchieveExpectedPositionBonus)
		bool expectedPosition = inPosition <= this.expectedRacePosition;
		float moralePosChange = (!isRace) ? 0f : ((!expectedPosition) ? this.moraleFailedExpectedPositionBonus : this.moraleAchieveExpectedPositionBonus);
		// if podium gives podiumBonus insteat
		if (isRace && inPosition <= 3)
			moralePosChange = this.moraleSessionPodiumBonus;
		
		// calculate career bonus, depending how close driver is to desired wins
		// career bonus/malus only for main drivers
		float careerMoraleBonus = 0f;
		if (isRace && !this.IsReserveDriver())
		{
			int totalCareerWins = this.careerHistory.GetTotalCareerWins();
			int totalCareerChampionships = this.careerHistory.GetTotalCareerChampionships();
			int num7 = totalCareerWins - this.GetDesiredWins();
			int num8 = totalCareerChampionships - this.desiredChampionships;
			careerMoraleBonus += (float)num7 / 100f;
			careerMoraleBonus += (float)num8 / 100f;
			careerMoraleBonus *= this.moraleGoalsWeight;
		}
		
		float positionMoraleBonus = 0f;
		int diffExpectedPosition = this.expectedRacePosition - inPosition;
		// relative position to expected position
		positionMoraleBonus += (float)diffExpectedPosition / (float)inNumEntries;
		positionMoraleBonus *= this.moraleRacePerformanceWeight;
		
		// lower morale change if still within expected championship postion, even when race postion is less than expected
		float keepChampionshipModifier = this.moraleChampionshipPositionNormalModifier;
		if (this.contract.GetTeam().championship.eventNumber > 1)
		{
			int num6 = this.startOfSeasonExpectedChampionshipPosition - this.GetChampionshipEntry().GetCurrentChampionshipPosition();
			keepChampionshipModifier = ((diffExpectedPosition >= 0 || num6 < 0) ? this.moraleChampionshipPositionNormalModifier : this.moraleKeptChampionshipExpectedPositionModifier);
		}
		
		careerMoraleBonus *= keepChampionshipModifier;
		positionMoraleBonus *= keepChampionshipModifier;
		
		float calculatedChange = Mathf.Clamp(careerMoraleBonus + positionMoraleBonus + moralePosChange, this.moraleMinSessionChange, this.moraleMaxSessionChange);
		float weightedChange = calculatedChange * inOverallWeight;
		
		this.ModifyMorale(weightedChange, inUpdateHistoryEntryName, true);
	}

	public void OnSeasonStart()
	{
		this.driverForm.OnSeasonStart();
	}

	public override bool WantsToLeave()
	{
		ChampionshipEntry_v1 championshipEntry = this.GetChampionshipEntry();
		if (championshipEntry != null && championshipEntry.races >= Person.minRacesBeforeThinkingAboutJobChange)
		{
			int num = 0;
			for (int i = 0; i < championshipEntry.races; i++)
			{
				int expectedRacePositionForEvent = championshipEntry.GetExpectedRacePositionForEvent(i);
				int racePositionForEvent = championshipEntry.GetRacePositionForEvent(i);
				num += racePositionForEvent - expectedRacePositionForEvent;
			}
			if (num > championshipEntry.races * 2)
			{
				Car lCurCar = this.contract.GetTeam().carManager.GetCarForDriver(this);
				if (lCurCar != null)
				{
					List<Car> overralBestCarsOfChampionship = this.contract.GetTeam().championship.GetOverralBestCarsOfChampionship();
					int num2 = overralBestCarsOfChampionship.FindIndex((Car x) => x == lCurCar);
					if (num2 < championshipEntry.GetCurrentChampionshipPosition())
					{
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}

	public override bool IsOpenToOffers()
	{
		return (Game.instance.time.now - this.lowMoraleStartTime).Days <= this.lowMoraleOpenToOffersDays;
	}

	public override bool WantsToRetire()
	{
		return base.WantsToRetire(Game.instance.time.now, this.GetImprovementRate());
	}

	protected override bool HasRivalInTeam(Team inTeam)
	{
		return this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.WillNotJoinRival) && this.personalityTraitController.HasRivalInTeam(inTeam);
	}

	protected override bool IsRivalOfTeam(Team inTeam)
	{
		List<Driver> list = new List<Driver>();
		inTeam.contractManager.GetAllDrivers(ref list);
		bool result = false;
		for (int i = 0; i < list.Count; i++)
		{
			PersonalityTraitController_v2 personalityTraitController_v = list[i].personalityTraitController;
			if (personalityTraitController_v.HasSpecialCase(PersonalityTrait.SpecialCaseType.WillNotJoinRival) && personalityTraitController_v.IsDriverRival(this))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	protected override bool WontRenewContract(Team inTeam)
	{
		// morale check
		if (this.GetMorale() < 0.4f) {
			int teamLastRank = 12;
			if (inTeam != null && inTeam.history.HasPreviousSeasonHistory()) {
				teamLastRank = inTeam.history.previousSeasonTeamResult;
			}
			// if team is not top 3 -> 65% do not renew
			if (teamLastRank > 3 && RandomUtility.GetRandom01() < 0.65f) {
				return true;
			}
		}
		// personality trait check
		if (this.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.WIllNotRenewContract))
			return true;
		// rival check
		if (this.personalityTraitController.HasRivalInTeam(inTeam))
			return true;
		return false;
	}

	public bool HasFightWithTeammate(Driver inTeammate)
	{
		return this.personalityTraitController.HasFightWithTeammate(inTeammate);
	}

	public bool IsAmbitiousDriver()
	{
		return this.personalityTraitController.HasTrait(false, GameStatsConstants.ambitiousTraitIDS);
	}

	public bool IsSlackerDriver()
	{
		return this.personalityTraitController.HasTrait(false, GameStatsConstants.slackerTraitIDS);
	}

	public bool IsUnpredictableDriver()
	{
		return this.personalityTraitController.HasTrait(false, GameStatsConstants.unpredictableTraitIDS);
	}

	public override bool IsReadyToRetire()
	{
		if (!base.HasRetired())
		{
			if (base.TimeSincePeakAge().Days > 0 && this.HasAchievedCareerGoals())
			{
				return true;
			}
			if (this.GetImprovementRateForAge(Game.instance.time.now) < -0.25f)
			{
				return true;
			}
		}
		return false;
	}

	public void SimulateStats()
	{
		string outFile = string.Concat(new string[]
		{
			Application.dataPath,
			"/../Stats/",
			base.firstName,
			base.lastName,
			".csv"
		});
		DriverStatsSimulation.RunSimulation(outFile, this, new DriverStatsSimulationScenario[]
		{
			new DriverStatsSimulationScenario(240, new DriverStatsSimulationScenarioEntry[]
			{
				new DriverStatsSimulationScenarioEntry("Age", 1, 1f, 0, true)
			}),
			new DriverStatsSimulationScenario(240, new DriverStatsSimulationScenarioEntry[]
			{
				new DriverStatsSimulationScenarioEntry("Age", 1, 1f, 0, true),
				new DriverStatsSimulationScenarioEntry("Test Track", 7, 1f, 0, false),
				new DriverStatsSimulationScenarioEntry("Simulator", 4, 1f, 0, false)
			}),
			new DriverStatsSimulationScenario(240, new DriverStatsSimulationScenarioEntry[]
			{
				new DriverStatsSimulationScenarioEntry("Age", 1, 1f, 0, true),
				new DriverStatsSimulationScenarioEntry("Test Track", 7, 1f, 0, false),
				new DriverStatsSimulationScenarioEntry("Simulator", 4, 1f, 0, false),
				new DriverStatsSimulationScenarioEntry("Medical Center", 3, 1f, 0, false),
				new DriverStatsSimulationScenarioEntry("Telemetry Center", 6, 1f, 0, false)
			})
		});
	}

	public bool HasSuperLizens() {
		int idSuperLizens = 500;
		return this.personalityTraitController.HasTrait(false, new int[] {idSuperLizens});
	}

	public DriverCareerForm careerForm = new DriverCareerForm();

	public DriverMentalState mentalState = new DriverMentalState();

	public CarOpinion carOpinion = new CarOpinion();

	public DriverStamina driverStamina;

	public DriverForm driverForm;

	public int driverNumber;

	public int desiredChampionships = RandomUtility.GetRandom(1, 4);

	public long desiredBudget = (long)RandomUtility.GetRandom(100, 10000) * 1000L;

	private int mDesiredWins = RandomUtility.GetRandom(1, 100);

	private long mDesiredEarnings = (long)RandomUtility.GetRandom(100, 1000) * 1000L;

	public int startOfSeasonExpectedChampionshipPosition;

	public int expectedChampionshipPosition;

	public int expectedRacePosition;

	public PersonalityTraitController_v2 personalityTraitController;

	[NonSerialized]
	private ChampionshipEntry_v1 mChampionshipEntry;

	private DriverStats accumulatedStats = new DriverStats();

	public DriverStats lastAccumulatedStats = new DriverStats();

	public DriverStats statsBeforeEvent;

	public float moraleBeforeEvent;

	public float championshipExpectation;

	public float raceExpectation;

	private bool mJoinsAnySeries = true;

	private Championship.Series mPreferedSeries;

	private List<Championship.Series> mDriverPreferedSeries = new List<Championship.Series>
	{
		Championship.Series.SingleSeaterSeries
	};

	private DriverStats mStats = new DriverStats();

	private DriverStats mModifiedStats = new DriverStats();

	private float mImprovementRate = RandomUtility.GetRandom(0.1f, 1f);

	private float mPotential;

	private float mModifiedPotential;

	private bool mHasBeenScouted;

	private bool mIsReplacementDriver;

	private bool mHasCachedReplacementDriverInfo;

	private int mLastRaceExpectedRacePosition;

	private Person mCelebrity;

	private DriverRivalries mDriverRivalries = new DriverRivalries();

	private int mDaysToScoutShort = RandomUtility.GetRandomInc(10, 20);

	private int mDaysToScoutLong = RandomUtility.GetRandomInc(20, 45);

	private DateTime lowMoraleStartTime = default(DateTime);

	private DateTime mLastMoraleBonusDate = default(DateTime);

	public int mCarID = -1;

	[NonSerialized]
	private DriverStats mStatsForAITeamEval = new DriverStats();

	private readonly int moraleBonusCooldownDays = 30;

	private readonly float moralePromotionBonus = 0.4f;

	private readonly float moraleDemotionBonus = -0.4f;

	private readonly float moraleBetterContractBonus = 0.4f;

	private readonly float moraleWorseContractBonus = -0.4f;

	private readonly float moraleSignedContractBonus = 0.4f;

	private readonly float moraleFiredBonus = -0.4f;

	private readonly float moraleBetterContractPerRace = 0.025f;

	private readonly float moraleWorseContractPerRace = -0.025f;

	private readonly int lowMoraleOpenToOffersDays = 50;

	private readonly float lowMoraleOpenToOffersAmount = 0.1f;

	private readonly float lowMoraleStopListeningToOffersAmount = 0.3f;

	private readonly float moraleAchieveExpectedPositionBonus = 0.025f;

	private readonly float moraleFailedExpectedPositionBonus = -0.025f;

	private readonly float moraleSessionPodiumBonus = 0.03f;

	private readonly float moraleChampionshipPositionNormalModifier = 1f;

	private readonly float moraleKeptChampionshipExpectedPositionModifier = 0.1f;

	// min and max morale change for each session (training, qualifying and race), before session type weighting!)
	private readonly float moraleMinSessionChange = -0.1f;
	private readonly float moraleMaxSessionChange = 0.1f;

	// morale change weight for each session type (all together should be 1, than min/max sessionChange will be absoulte for each race weekend)
	private readonly float moralePracticeWeight = 0.0f;
	private readonly float moraleQualifyingWeight = 0.25f;
	private readonly float moraleRaceWeight = 0.75f;

	private readonly float moraleRacePerformanceWeight = 0.25f;

	private readonly float moraleGoalsWeight = 0.01f;

	private readonly float negativeImprovementHQScalar = 0.9f;

	private readonly float negativeImprovementHQOverallScalar = 0.03f;

	private readonly float negativeMaxImprovementHQ = 0.75f;
}
