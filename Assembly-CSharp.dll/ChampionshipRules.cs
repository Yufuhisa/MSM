using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ChampionshipRules : Entity, fsISerializationCallbacks
{
	public ChampionshipRules()
	{
	}

	public override void OnLoad()
	{
		base.OnLoad();
		HashSet<CarPart.PartType> hashSet = new HashSet<CarPart.PartType>(this.specParts);
		this.specParts = new List<CarPart.PartType>(hashSet);
		this.ValidateChampionshipRules();
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.HasImpactOfType<PoliticalImpactGridSettings>() && App.instance.votesManager.votes.ContainsKey(politicalVote.ID))
			{
				PoliticalVote politicalVote2 = App.instance.votesManager.votes[politicalVote.ID].Clone();
				this.mRules[i] = politicalVote2;
				PoliticalImpactGridSettings impactOfType = politicalVote2.GetImpactOfType<PoliticalImpactGridSettings>();
				this.gridSetup = impactOfType.GetSetup(impactOfType.impactType);
			}
		}
		if (this.gridSetup == ChampionshipRules.GridSetup.AverageLap && this.mChampionship.series != Championship.Series.EnduranceSeries)
		{
			PoliticalVote politicalVote3 = App.instance.votesManager.votes[67].Clone();
			this.AddRule(politicalVote3);
			politicalVote3.ApplyImpacts(this);
		}
	}

	public void AddRule(PoliticalVote inRule)
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.group == inRule.group)
			{
				this.mRules.Remove(politicalVote);
				i--;
			}
		}
		this.mRules.Add(inRule);
	}

	public int GetQualifyingSessionCount()
	{
		if (this.qualifyingBasedActive)
		{
			return (this.gridSetup != ChampionshipRules.GridSetup.AverageLap && this.gridSetup != ChampionshipRules.GridSetup.QualifyingBased) ? 3 : 1;
		}
		return 1;
	}

	public void GenerateDefaultParts(CarPart.PartType inPartType)
	{
		int teamEntryCount = this.championship.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.championship.standings.GetTeamEntry(i).GetEntity<Team>();
			this.ApplyDefaultPart(entity, inPartType);
		}
	}

	public void ApplyDefaultPart(Team inTeam, CarPart.PartType inPartType)
	{
		CarManager carManager = inTeam.carManager;
		carManager.UnfitAllParts(carManager.GetCar(0));
		carManager.UnfitAllParts(carManager.GetCar(1));
		CarPartInventory partInventory = inTeam.carManager.partInventory;
		partInventory.DestroyParts(inPartType);
		partInventory.AddPart(this.GetDefaultPart(inPartType, inTeam));
		partInventory.AddPart(this.GetDefaultPart(inPartType, inTeam));
		carManager.AutoFit(carManager.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		carManager.AutoFit(carManager.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
	}

	private CarPart GetDefaultPart(CarPart.PartType inType, Team inTeam)
	{
		CarPart carPart = CarPart.CreatePartEntity(inType, this.championship);
		carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, (float)(GameStatsConstants.specPartValues[Mathf.Min(this.championship.championshipID, GameStatsConstants.specPartValues.Length - 1)] + RandomUtility.GetRandom(0, 10)));
		carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, 0.8f);
		carPart.stats.partCondition.SetCondition(0.8f);
		carPart.stats.partCondition.redZone = GameStatsConstants.initialRedZone;
		carPart.stats.maxReliability = 1f;
		carPart.stats.maxPerformance = 0f;
		carPart.buildDate = Game.instance.time.now;
		carPart.stats.level = 0;
		carPart.PostStatsSetup(this.championship);
		return carPart;
	}

	public long GetPrizeMoney(int inIndex, int inPrizeFund)
	{
		if (inIndex < this.prizePoolPercentage.Count)
		{
			double num = (double)(this.prizePoolPercentage[inIndex] / 100f * (float)inPrizeFund);
			return (long)num;
		}
		return 0L;
	}

	public void ApplySpecParts()
	{
		int teamEntryCount = this.championship.standings.teamEntryCount;
		this.championship.ResetPartTypeStatsProgression(this.specParts.ToArray());
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.championship.standings.GetTeamEntry(i).GetEntity<Team>();
			this.ApplySpecPart(entity);
		}
	}

	public void ApplySpecPart(Team inTeam)
	{
		CarManager carManager = inTeam.carManager;
		carManager.UnfitAllParts(carManager.GetCar(0));
		carManager.UnfitAllParts(carManager.GetCar(1));
		CarPartInventory partInventory = inTeam.carManager.partInventory;
		for (int i = 0; i < this.specParts.Count; i++)
		{
			partInventory.DestroyParts(this.specParts[i]);
			partInventory.AddPart(this.GetSpecPart(this.specParts[i], inTeam));
			partInventory.AddPart(this.GetSpecPart(this.specParts[i], inTeam));
		}
		carManager.AutoFit(carManager.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		carManager.AutoFit(carManager.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
	}

	private CarPart GetSpecPart(CarPart.PartType inType, Team inTeam)
	{
		CarPart carPart = CarPart.CreatePartEntity(inType, this.championship);
		carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, (float)GameStatsConstants.specPartValues[Mathf.Min(this.championship.championshipID, GameStatsConstants.specPartValues.Length - 1)]);
		carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, 0.8f);
		carPart.stats.partCondition.SetCondition(0.8f);
		carPart.stats.partCondition.redZone = GameStatsConstants.initialRedZone;
		carPart.stats.maxReliability = 1f;
		carPart.stats.maxPerformance = 0f;
		carPart.buildDate = Game.instance.time.now;
		carPart.stats.level = -1;
		carPart.PostStatsSetup(this.championship);
		return carPart;
	}

	public void ActivateRulesThatAffectCalendar()
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			politicalVote.ApplyImpactsForCalendar(this);
		}
	}

	public void ActivateRules()
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			politicalVote.ApplyImpacts(this);
		}
	}

	public PoliticalVote GetVoteForGroup(string inGroup)
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.group == inGroup)
			{
				return politicalVote;
			}
		}
		return null;
	}

	public bool HasActiveRule(int inID)
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.ID == inID)
			{
				return true;
			}
		}
		return false;
	}

	public PoliticalVote GetVoteByID(int inID)
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.ID == inID)
			{
				return politicalVote;
			}
		}
		return null;
	}

	public void ClearTrackRules()
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.HasImpactOfType<PoliticalImpactChangeTrack>())
			{
				this.mRules.Remove(politicalVote);
			}
		}
	}

	public void ApplySimulationSettings()
	{
		if (this.practiceSettings != null)
		{
			this.practiceSettings.Apply(this);
		}
		if (this.qualifyingSettings != null)
		{
			this.qualifyingSettings.Apply(this);
		}
		if (this.raceSettings != null)
		{
			this.raceSettings.Apply(this);
		}
		else
		{
			this.raceSettings = Game.instance.simulationSettingsManager.raceSettings[ChampionshipRules.SessionLength.Medium];
			this.raceSettings.Apply(this);
		}
	}

	public ChampionshipRules Clone()
	{
		object obj = base.MemberwiseClone();
		ChampionshipRules championshipRules = (ChampionshipRules)obj;
		championshipRules.mRules = this.mRules.ConvertAll<PoliticalVote>((PoliticalVote vote) => vote.Clone());
		championshipRules.specParts = new List<CarPart.PartType>(this.specParts);
		championshipRules.practiceDuration = new List<float>(this.practiceDuration);
		championshipRules.qualifyingDuration = new List<float>(this.qualifyingDuration);
		championshipRules.raceLength = new List<ChampionshipRules.SessionLength>(this.raceLength);
		championshipRules.prizePoolPercentage = new List<float>(this.prizePoolPercentage);
		championshipRules.partStatSeasonMinValue = new Dictionary<CarPart.PartType, int>(this.partStatSeasonMinValue);
		championshipRules.partStatSeasonMaxValue = new Dictionary<CarPart.PartType, int>(this.partStatSeasonMaxValue);
		championshipRules.points = new List<int>(this.points);
		return championshipRules;
	}

	public int CalculateRaceLapsForCircuit(float inLapDistance, int inRaceIndex)
	{
		return Mathf.CeilToInt(48f / inLapDistance);
	}

	public int GetPointsForPosition(int inPosition)
	{
		inPosition--;
		if (inPosition < 0)
		{
			global::Debug.LogError(string.Format("Invalid position. Position needs to go from 1 to {0}", this.points.Count), null);
		}
		if (inPosition >= 0 && inPosition < this.points.Count)
		{
			return this.points[inPosition];
		}
		return 0;
	}

	public int GetPointsScoringPositions()
	{
		return this.points.Count;
	}

	public void SetToDebugRules()
	{
		this.practiceDuration.Clear();
		this.qualifyingDuration.Clear();
		this.raceLength.Clear();
		this.practiceDuration.Add(GameUtility.MinutesToSeconds(2f));
		this.qualifyingDuration.Add(GameUtility.MinutesToSeconds(2f));
		this.raceLength.Add(ChampionshipRules.SessionLength.Short);
	}

	public void SetRaceDistance_Debug(float distanceKM)
	{
	}

	public float GetMaximumCarPartStatValue(CarPart.PartType inType)
	{
		return (float)this.partStatSeasonMaxValue[inType];
	}

	public float GetMinimumCarPartStatValue(CarPart.PartType inType)
	{
		return (float)this.partStatSeasonMinValue[inType];
	}

	public float GetMaximumCarStatValue()
	{
		float num = 0f;
		foreach (int num2 in this.partStatSeasonMaxValue.Values)
		{
			num += (float)num2;
		}
		return num;
	}

	public float GetMinimumCarStatValue()
	{
		float num = 0f;
		foreach (int num2 in this.partStatSeasonMinValue.Values)
		{
			num += (float)num2;
		}
		return num;
	}

	public List<CarPart.PartType> GetNonSpecParts()
	{
		List<CarPart.PartType> list = new List<CarPart.PartType>();
		foreach (CarPart.PartType partType2 in CarPart.GetPartType(this.championship.series, false))
		{
			if (!this.specParts.Contains(partType2))
			{
				list.Add(partType2);
			}
		}
		return list;
	}

	public List<PoliticalVote> GetRuleChangesForNextSeason()
	{
		List<PoliticalVote> list = new List<PoliticalVote>();
		int count = this.mChampionship.nextYearsRules.votedRules.Count;
		for (int i = 0; i < count; i++)
		{
			PoliticalVote politicalVote = this.mChampionship.nextYearsRules.votedRules[i];
			if (!this.mChampionship.rules.HasActiveRule(politicalVote.ID))
			{
				list.Add(politicalVote);
			}
		}
		return list;
	}

	public void ValidateChampionshipRules()
	{
		string[] array = new string[]
		{
			"PromotionBonus",
			"LastPlaceBonus",
			"EnergyRecoverySystem",
			"HybridPower",
			"ChargeBasedOnStandings",
			"Sprinklers",
			"TimedRaces",
			"RaceStart",
			"WeightStripping"
		};
		int[] array2 = new int[]
		{
			81,
			75,
			76,
			80,
			95,
			72,
			96,
			101,
			121
		};
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (this.GetVoteForGroup(text) == null)
			{
				int num = array2[i];
				if (!App.instance.votesManager.votes.ContainsKey(num))
				{
					global::Debug.LogWarningFormat("Championship {0} references invalid rule {1}; ignoring rule", new object[]
					{
						this.mChampionship.championshipID,
						num
					});
				}
				else
				{
					PoliticalVote politicalVote = App.instance.votesManager.votes[num].Clone();
					politicalVote.Initialize(this.mChampionship);
					this.AddRule(politicalVote);
					if (text == "WeightStripping")
					{
						politicalVote.ApplyImpacts(this);
					}
				}
			}
		}
	}

	public float GetRaceLength()
	{
		return DesignDataManager.GetRaceLengthFromPrefSettingsInMinutes() * 60f;
	}

	public void SetCoreRules(string[] inRules)
	{
		for (int i = 0; i < inRules.Length; i++)
		{
			this.coreRuleIDS.Add(int.Parse(inRules[i]));
		}
	}

	public void SetRestrictedRules(string[] inRules)
	{
		for (int i = 0; i < inRules.Length; i++)
		{
			this.restrictedRuleIDS.Add(int.Parse(inRules[i]));
		}
	}

	public void CopyConcurrentRules(ChampionshipRules inRules)
	{
		for (int i = 0; i < inRules.votedRules.Count; i++)
		{
			PoliticalVote politicalVote = inRules.votedRules[i];
			if (PoliticalVote.groupsNotAvailableForNonMainConcurrentChampionships.Contains(politicalVote.group))
			{
				this.AddRule(politicalVote);
			}
		}
	}

	public void InitializeConcurrentRules()
	{
		for (int i = 0; i < this.mRules.Count; i++)
		{
			PoliticalVote politicalVote = this.mRules[i];
			if (politicalVote.championship != this.mChampionship)
			{
				PoliticalVote politicalVote2 = politicalVote.Clone();
				politicalVote2.Initialize(this.mChampionship);
				this.mRules[i] = politicalVote2;
			}
		}
	}

	public List<RaceEventCalendarData> calendar
	{
		get
		{
			return this.mChampionship.calendarData;
		}
	}

	public Championship championship
	{
		get
		{
			return this.mChampionship;
		}
		set
		{
			this.mChampionship = value;
		}
	}

	public List<PoliticalVote> votedRules
	{
		get
		{
			return this.mRules;
		}
	}

	public bool qualifyingBasedActive
	{
		get
		{
			return this.gridSetup == ChampionshipRules.GridSetup.AverageLap || this.gridSetup == ChampionshipRules.GridSetup.QualifyingBased || this.gridSetup == ChampionshipRules.GridSetup.QualifyingBased3Sessions;
		}
	}

	public void OnBeforeSerialize(Type storageType)
	{
	}

	public void OnAfterSerialize(Type storageType, ref fsData data)
	{
	}

	public void OnBeforeDeserialize(Type storageType, ref fsData data)
	{
	}

	public void OnAfterDeserialize(Type storageType)
	{
		if (this.fuelLimitForRaceDistanceNormalized == 0f)
		{
			this.fuelLimitForRaceDistanceNormalized = 1f;
		}
	}

	public float speedBonusNormalized
	{
		get
		{
			return (this.tyreSpeedBonus + this.tyreSupplierBonus) / 45f;
		}
	}

	public const float maxTyreSpeedBonus = 45f;

	public string ruleSetName = string.Empty;

	public List<float> practiceDuration = new List<float>();

	public List<float> qualifyingDuration = new List<float>();

	public List<ChampionshipRules.SessionLength> raceLength = new List<ChampionshipRules.SessionLength>();

	public List<float> prizePoolPercentage = new List<float>();

	public DateTime carDevelopmenStartDate = default(DateTime);

	public Dictionary<CarPart.PartType, int> partStatSeasonMinValue = new Dictionary<CarPart.PartType, int>();

	public Dictionary<CarPart.PartType, int> partStatSeasonMaxValue = new Dictionary<CarPart.PartType, int>();

	public string tyreSupplier = string.Empty;

	public int tyreSupplierID;

	public ChampionshipRules.TyreType tyreType;

	public int maxSlickTyresPerEvent = 15;

	public ChampionshipRules.CompoundChoice compoundChoice;

	public int wetWeatherTyreCount = 5;

	public int compoundsAvailable = 3;

	public float pitlaneSpeed;

	public float tyreSpeedBonus;

	public float tyreSupplierBonus;

	public ChampionshipRules.TyreWearRate tyreWearRate;

	public ChampionshipRules.EnergySystemBattery batterySize;

	public bool isEnergySystemActive;

	public bool isHybridModeActive;

	public bool shouldChargeUsingStandingsPosition;

	public bool isERSAdvancedModeActive;

	public bool isSprinklingSystemOn;

	public bool staffTransferWindowPreseason;

	public bool driverAidsOn;

	public bool isRefuelingOn;

	public float fuelLimitForRaceDistanceNormalized;

	public ChampionshipRules.SafetyCarUsage safetyCarUsage = ChampionshipRules.SafetyCarUsage.Both;

	public ChampionshipRules.GridSetup gridSetup;

	public ChampionshipRules.PitStopCrewSize pitCrewSize;

	public List<int> points = new List<int>();

	public bool finalRacePointsDouble;

	public int fastestLapPointBonus;

	public int polePositionPointBonus;

	public ChampionshipRules.MaxFinancialBudget maxFinancialBudget;

	public int maxDriverBudget;

	public int maxHQBudget;

	public int maxCarPartsBudget;

	public int maxNextYearCarBudget;

	public int maxNextYearDrivers;

	public int maxTravelBudget;

	public bool promotionBonus;

	public bool lastPlaceBonus;

	public SimulationSettings practiceSettings;

	public SimulationSettings qualifyingSettings;

	public SimulationSettings raceSettings;

	public List<CarPart.PartType> specParts = new List<CarPart.PartType>();

	public ChampionshipRules.RaceType raceType;

	public int raceLengthInHours;

	public List<int> coreRuleIDS = new List<int>();

	public List<int> restrictedRuleIDS = new List<int>();

	public ChampionshipRules.RaceStart raceStart;

	public float drivingTimeEndurance = 0.4f;

	public float weightStrippingRatio;

	public bool isWeightStrippingEnabled;

	private List<PoliticalVote> mRules = new List<PoliticalVote>();

	private Championship mChampionship;

	public enum RaceType
	{
		Laps,
		Time
	}

	public enum RaceStart
	{
		StandingStart,
		RollingStart
	}

	public enum MaxFinancialBudget
	{
		None,
		Low,
		Medium,
		High
	}

	public enum TyreWearRate
	{
		Low,
		High
	}

	public enum CompoundChoice
	{
		Free,
		Locked
	}

	public enum TyreType
	{
		Normal,
		Wide,
		Grooved,
		Low,
		Road
	}

	public enum SessionLength
	{
		Short,
		Medium,
		Long
	}

	public enum SafetyCarUsage
	{
		RealSafetyCar,
		VirtualSafetyCar,
		Both
	}

	public enum GridSetup
	{
		QualifyingBased,
		QualifyingBased3Sessions,
		Random,
		InvertedDriverChampionship,
		AverageLap
	}

	public enum PitStopCrewSize
	{
		Small,
		Large,
		SemiSequential
	}

	public enum EnergySystemBattery
	{
		Small,
		Large
	}
}
