using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Mechanic : Person
{
	public Mechanic()
	{
	}

	public override float GetReputationValue()
	{
		float num = 0f;
		num += this.stats.reliability;
		num += this.stats.performance;
		num += this.stats.concentration;
		num += this.stats.speed;
		num += this.stats.pitStops;
		num += this.stats.leadership;
		return num / 6f;
	}

	public override void OnLoad()
	{
		base.OnLoad();
		if (this.mDictDriversRelationships.Count == 0 && this.mDriversRelationships != null)
		{
			this.mDictDriversRelationships = this.mDriversRelationships.GetDictionary();
		}
		if (this.mDictRelationshipModificationHistory.Count == 0 && this.mRelationshipModificationHistory != null)
		{
			this.mDictRelationshipModificationHistory = this.mRelationshipModificationHistory.GetDictionary();
		}
		this.mDriversRelationships = null;
		this.mRelationshipModificationHistory = null;
	}

	public override bool IsReplacementPerson()
	{
		MechanicManager mechanicManager = Game.instance.mechanicManager;
		return mechanicManager.IsReplacementPerson(this);
	}

	public override int GetPersonIndexInManager()
	{
		MechanicManager mechanicManager = Game.instance.mechanicManager;
		return mechanicManager.GetPersonIndex(this);
	}

	public float GetImprovementRate()
	{
		return this.improvementRate;
	}

	public float GetImprovementRateForAge(DateTime inDate)
	{
		return base.GetImprovementRateForAge(inDate, this.peakAge, this.peakDuration, this.improvementRate);
	}

	public void UpdateStatsForAge(MechanicStats inAccStats, float inTimePassed)
	{
		if (Game.instance.mechanicManager.ageMechanicStatProgression != null)
		{
			inAccStats.ApplyStatsProgression(Game.instance.mechanicManager.ageMechanicStatProgression, inTimePassed * this.GetImprovementRateForAge(Game.instance.time.now));
		}
	}

	public void CalculateAccumulatedStatsForDay(MechanicStats inAccStats)
	{
		float improvementRateForAge = this.GetImprovementRateForAge(Game.instance.time.now);
		if (Game.instance.mechanicManager.ageMechanicStatProgression != null)
		{
			inAccStats.ApplyStatsProgression(Game.instance.mechanicManager.ageMechanicStatProgression, PersonConstants.statIncreaseTimePerDay * improvementRateForAge);
		}
		Team team = this.contract.GetTeam();
		if (team != null && team.headquarters != null)
		{
			int count = team.headquarters.hqBuildings.Count;
			for (int i = 0; i < count; i++)
			{
				HQsBuilding_v1 hqsBuilding_v = team.headquarters.hqBuildings[i];
				if (hqsBuilding_v.activeMechanicStatProgression)
				{
					this.CalculateAccumulatedStatsForBuilding(improvementRateForAge, inAccStats, hqsBuilding_v);
				}
			}
		}
		if (improvementRateForAge < 0f)
		{
			inAccStats.MinToZero();
		}
		else if (Game.instance.mechanicManager.maxMechanicStatProgressionPerDay != null)
		{
			inAccStats.LimitToDailyMax(Game.instance.mechanicManager.maxMechanicStatProgressionPerDay);
		}
	}

	public void CalculateAccumulatedStatsForBuilding(float inImprovementRateForAge, MechanicStats inAccStats, HQsBuilding_v1 inBuilding)
	{
		float num = inImprovementRateForAge;
		if (inImprovementRateForAge < 0f)
		{
			num = (1f + inImprovementRateForAge * this.negativeImprovementHQScalar) * this.negativeImprovementHQOverallScalar;
			num = Mathf.Max(0f, num);
			num = Math.Min(this.negativeMaxImprovementHQ, num);
		}
		inAccStats.ApplyStatsProgression(inBuilding.mechanicStatProgression, PersonConstants.statIncreaseTimePerDay * num);
	}

	private void UpdateAccumulatedStats()
	{
		this.lastAccumulatedStats.Clear();
		this.CalculateAccumulatedStatsForDay(this.lastAccumulatedStats);
		if (Game.instance.mechanicManager.maxMechanicStatProgressionPerDay != null)
		{
			this.lastAccumulatedStats.LimitToDailyMax(Game.instance.mechanicManager.maxMechanicStatProgressionPerDay);
		}
		this.stats += this.lastAccumulatedStats;
		this.stats.ClampStats();
	}

	public override PersonStats GetStats()
	{
		return this.stats;
	}

	public override float GetStatsValue()
	{
		return this.stats.GetUnitAverage();
	}

	public float GetPitStrategyAddedErrorChance()
	{
		return Mathf.Lerp(0f, 0f, this.stats.GetConcentrationNormalized());
	}

	public float GetPitStrategyAddedErrorChanceForDisplay()
	{
		return Mathf.Lerp(0f, 0f, 1f - this.stats.GetConcentrationNormalized());
	}

	public Driver GetDriver()
	{
		Driver[] drivers = this.GetDrivers();
		if (drivers != null && drivers.Length > 0)
		{
			return drivers[0];
		}
		return null;
	}

	public Driver GetRandomDriver()
	{
		Driver[] drivers = this.GetDrivers();
		if (drivers != null && drivers.Length > 0)
		{
			return drivers[RandomUtility.GetRandom(0, drivers.Length - 1)];
		}
		return null;
	}

	public Driver[] GetDrivers()
	{
		if (this.IsFreeAgent())
		{
			return null;
		}
		this.mRelationshipDriversCache.Clear();
		Team team = this.contract.GetTeam();
		if (team.championship.series == Championship.Series.EnduranceSeries)
		{
			return team.GetDriversForCar(this.driver);
		}
		this.mRelationshipDriversCache.Add(team.GetDriver(this.driver));
		return this.mRelationshipDriversCache.ToArray();
	}

	public void SetDefaultDriverRelationship()
	{
		Driver[] drivers = this.GetDrivers();
		for (int i = 0; i < drivers.Length; i++)
		{
			string name = drivers[i].name;
			this.GenerateDriverRelationship(name, 0, 0f);
		}
	}

	public void SetDriverRelationshipByDriverIndex(int inDriverIndex, int inWeeksTogether, float inRelationshipAmount)
	{
		Driver[] drivers = this.GetDrivers();
		Driver driver = (inDriverIndex < 0 || inDriverIndex >= drivers.Length) ? null : drivers[inDriverIndex];
		global::Debug.Assert(driver != null, "SetDriverRelationshipByDriverIndex - Driver is null - Driver Index -> " + inDriverIndex.ToString());
		if (driver != null)
		{
			this.SetDriverRelationship(driver.name, inWeeksTogether, inRelationshipAmount);
		}
	}

	public void SetDriverRelationship(string inDriverName, int inWeeksTogether, float inRelationshipAmount)
	{
		if (!string.IsNullOrEmpty(inDriverName))
		{
			if (!this.mDictDriversRelationships.ContainsKey(inDriverName))
			{
				this.GenerateDriverRelationship(inDriverName, inWeeksTogether, inRelationshipAmount);
			}
			else
			{
				Mechanic.DriverRelationship driverRelationship = this.mDictDriversRelationships[inDriverName];
				driverRelationship.numberOfWeeks = inWeeksTogether;
				driverRelationship.relationshipAmount = inRelationshipAmount;
				if (!this.mDictRelationshipModificationHistory.ContainsKey(inDriverName))
				{
					this.mDictRelationshipModificationHistory.Add(inDriverName, new StatModificationHistory());
				}
			}
		}
	}

	private Mechanic.DriverRelationship GenerateDriverRelationship(string inDriverName, int inWeeksTogether, float inRelationshipAmount)
	{
		if (!string.IsNullOrEmpty(inDriverName) && !this.mDictDriversRelationships.ContainsKey(inDriverName))
		{
			Mechanic.DriverRelationship driverRelationship = new Mechanic.DriverRelationship();
			driverRelationship.numberOfWeeks = inWeeksTogether;
			driverRelationship.relationshipAmount = inRelationshipAmount;
			this.mDictDriversRelationships.Add(inDriverName, driverRelationship);
			if (!this.mDictRelationshipModificationHistory.ContainsKey(inDriverName))
			{
				this.mDictRelationshipModificationHistory.Add(inDriverName, new StatModificationHistory());
			}
			return driverRelationship;
		}
		return null;
	}

	public Mechanic.DriverRelationship GetRelationshipWithDriver(Driver inDriver)
	{
		if (!this.mDictDriversRelationships.ContainsKey(inDriver.name))
		{
			return null;
		}
		return this.mDictDriversRelationships[inDriver.name];
	}

	public Mechanic.DriverRelationship GetModifiedRelationshipWithDriver(Driver inDriver, bool inCheckAchievements = true)
	{
		Mechanic.DriverRelationship relationshipWithDriver = this.GetRelationshipWithDriver(inDriver);
		Mechanic.DriverRelationship driverRelationship = new Mechanic.DriverRelationship(relationshipWithDriver);
		float singleModifierForStat = inDriver.personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.MechanicRelationship);
		driverRelationship.relationshipAmount = Mathf.Clamp(driverRelationship.relationshipAmount + singleModifierForStat, 0f, 100f);
		if (inCheckAchievements)
		{
			Game.instance.mechanicManager.UpdateDriverMechanicRelationshipAchievements(driverRelationship, inDriver, this.contract.GetTeam());
		}
		return driverRelationship;
	}

	public StatModificationHistory GetRelationshipModificationHistoryWithDriver(Driver inDriver)
	{
		if (!this.mDictRelationshipModificationHistory.ContainsKey(inDriver.name))
		{
			return null;
		}
		return this.mDictRelationshipModificationHistory[inDriver.name];
	}

	public void EndRaceDriverRelationshipUpdate(Driver inDriver, int inRacePosition)
	{
		Mechanic.DriverRelationship relationshipWithDriver = this.GetRelationshipWithDriver(inDriver);
		Game.instance.teamManager.CalculateDriverExpectedPositionsInChampionship(inDriver.contract.GetTeam().championship);
		int raceExpectedPosition = inDriver.GetRaceExpectedPosition();
		int num = raceExpectedPosition - inRacePosition;
		num = ((num < 0) ? num : (num + 1));
		float num2 = Mathf.Clamp(Mathf.Abs((float)num / this.positionRange), 0f, 1f);
		float num4;
		if (num >= 0)
		{
			float num3 = Mathf.Max(5f, this.endRaceRelationshipIncreaseRate * (this.stats.speed / 20f));
			num4 = num3 * num2;
			relationshipWithDriver.relationshipAmount = Math.Min(100f, relationshipWithDriver.relationshipAmount + num4);
		}
		else
		{
			num4 = -(this.endRaceRelationshipDecreaseRate * num2);
			relationshipWithDriver.relationshipAmount = Math.Max(0f, relationshipWithDriver.relationshipAmount + num4);
		}
		StatModificationHistory relationshipModificationHistoryWithDriver = this.GetRelationshipModificationHistoryWithDriver(inDriver);
		relationshipModificationHistoryWithDriver.AddStatModificationEntry(Game.instance.sessionManager.eventDetails.circuit.locationNameID, num4, false);
	}

	public void ModifyCurrentDriversRelationship(float inModifier, string inRelationshipModifierName)
	{
		foreach (Driver inDriver in this.GetDrivers())
		{
			this.ModifyDriverRelationship(inDriver, inModifier, inRelationshipModifierName);
		}
	}

	public void ModifyDriverRelationship(Driver inDriver, float inModifier, string inRelationshipModifierName)
	{
		if (!this.IsFreeAgent())
		{
			Mechanic.DriverRelationship relationshipWithDriver = this.GetRelationshipWithDriver(inDriver);
			relationshipWithDriver.relationshipAmount = Mathf.Clamp(relationshipWithDriver.relationshipAmount + inModifier, 0f, 100f);
			StatModificationHistory relationshipModificationHistoryWithDriver = this.GetRelationshipModificationHistoryWithDriver(inDriver);
			relationshipModificationHistoryWithDriver.AddStatModificationEntry(inRelationshipModifierName, inModifier, false);
		}
	}

	private void IncreaseDriverRelationships()
	{
		if (!this.IsFreeAgent())
		{
			foreach (Driver inDriver in this.GetDrivers())
			{
				Mechanic.DriverRelationship relationshipWithDriver = this.GetRelationshipWithDriver(inDriver);
				if (relationshipWithDriver != null)
				{
					relationshipWithDriver.numberOfWeeks++;
					relationshipWithDriver.relationshipAmount = Math.Min(100f, relationshipWithDriver.relationshipAmount + this.weeklyRelationshipIncreaseRate);
					relationshipWithDriver.relationshipAmountAfterDecay = this.mechanicRelationshipInvalidDecay;
				}
			}
		}
	}

	private void DecreaseDriverRelationships()
	{
		Driver[] drivers = this.GetDrivers();
		List<string> list = new List<string>();
		if (drivers != null)
		{
			for (int i = 0; i < drivers.Length; i++)
			{
				list.Add(drivers[i].name);
			}
		}
		Dictionary<string, Mechanic.DriverRelationship>.Enumerator enumerator = this.mDictDriversRelationships.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (drivers != null)
			{
				List<string> list2 = list;
				KeyValuePair<string, Mechanic.DriverRelationship> keyValuePair = enumerator.Current;
				if (list2.Contains(keyValuePair.Key))
				{
					continue;
				}
			}
			KeyValuePair<string, Mechanic.DriverRelationship> keyValuePair2 = enumerator.Current;
			Mechanic.DriverRelationship value = keyValuePair2.Value;
			if (value.relationshipAmount > 0f && value.relationshipAmountAfterDecay == this.mechanicRelationshipInvalidDecay)
			{
				value.relationshipAmountAfterDecay = Mathf.Clamp(value.relationshipAmount * this.maxMechanicRelationshipDecayPercent, 0f, 100f);
			}
			if (value.relationshipAmount > value.relationshipAmountAfterDecay)
			{
				value.relationshipAmount = Mathf.Clamp(Mathf.Max(value.relationshipAmount - this.weeklyRelationshipDecreaseRate, value.relationshipAmountAfterDecay), 0f, 100f);
			}
		}
	}

	public bool IsBonusLevelUnlocked(int inLevel)
	{
		if (inLevel == 1)
		{
			return this.GetRelationshipAmmount() >= (float)this.bonusOne.bonusUnlockAt;
		}
		return inLevel == 2 && this.GetRelationshipAmmount() >= (float)this.bonusTwo.bonusUnlockAt;
	}

	public float GetRelationshipAmmount()
	{
		float num = 0f;
		if (!this.IsFreeAgent())
		{
			Driver[] drivers = this.GetDrivers();
			for (int i = 0; i < drivers.Length; i++)
			{
				num += this.GetModifiedRelationshipWithDriver(drivers[i], true).relationshipAmount;
			}
			if (drivers.Length > 0)
			{
				return num / (float)drivers.Length;
			}
		}
		return num;
	}

	public int GetRelationshipWeeksTogether()
	{
		int num = 0;
		if (!this.IsFreeAgent())
		{
			Driver[] drivers = this.GetDrivers();
			for (int i = 0; i < drivers.Length; i++)
			{
				num += this.GetModifiedRelationshipWithDriver(drivers[i], true).numberOfWeeks;
			}
			if (drivers.Length > 0)
			{
				return Mathf.RoundToInt((float)num / (float)drivers.Length);
			}
		}
		return num;
	}

	public bool isDriverRelationshipActive(Mechanic.DriverRelationship inDriverRelationship)
	{
		if (!this.IsFreeAgent())
		{
			Driver[] drivers = this.GetDrivers();
			for (int i = 0; i < drivers.Length; i++)
			{
				Mechanic.DriverRelationship relationshipWithDriver = this.GetRelationshipWithDriver(drivers[i]);
				if (inDriverRelationship == relationshipWithDriver)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void OnSessionEnd()
	{
		this.UpdateAccumulatedStats();
	}

	public void OnDayEnd()
	{
		this.UpdateAccumulatedStats();
		this.stats.UpdatePotentialWithPeakAge(this.peakAge);
	}

	public void OnWeekEnd()
	{
		this.DecreaseDriverRelationships();
		this.IncreaseDriverRelationships();
	}

	public override float GetExpectation(DatabaseEntry inWeightings)
	{
		float num = (float)inWeightings.GetIntValue("Experience") / 100f;
		float num2 = (float)inWeightings.GetIntValue("Quality") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Team Reputation") / 100f;
		float num4 = 0f;
		float num5 = this.GetExperience() * num;
		float num6 = this.stats.GetUnitAverage() * num2;
		if (this.contract.job != Contract.Job.Unemployed)
		{
			num4 += (float)this.contract.GetTeam().reputation / 100f * num3;
		}
		float num7 = num5 + num6 + num4;
		return num7 / (num + num2 + num3);
	}

	public override float GetAchievements(DatabaseEntry inWeightings)
	{
		float num = (float)inWeightings.GetIntValue("Mechanic Results") / 100f;
		float num2 = (float)inWeightings.GetIntValue("Mechanic Qualifying") / 100f;
		float num3 = (float)inWeightings.GetIntValue("Mechanic DNF") / 100f;
		float num4 = (float)inWeightings.GetIntValue("Mechanic Performance") / 100f;
		float num5 = (float)inWeightings.GetIntValue("Mechanic Reliability") / 100f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = 0f;
		float num11 = 0f;
		if (this.contract.job != Contract.Job.Unemployed)
		{
			Car car = this.contract.GetTeam().carManager.GetCar(this.driver);
			if (car != null)
			{
				num7 += car.GetPerformance() * num4 / 500f;
				num8 += car.GetReliability() * num5 / 500f;
			}
			Driver[] drivers = this.GetDrivers();
			if (drivers != null)
			{
				foreach (Driver driver in drivers)
				{
					ChampionshipEntry_v1 championshipEntry = driver.GetChampionshipEntry();
					if (championshipEntry != null && championshipEntry.races > 0)
					{
						num9 += (float)championshipEntry.GetCurrentPoints() / (float)championshipEntry.races / (float)championshipEntry.championship.rules.GetPointsForPosition(1);
						num10 += (float)championshipEntry.GetNumberOfPoles() / (float)championshipEntry.races;
						num11 += (float)championshipEntry.DNFs / (float)championshipEntry.races;
					}
				}
				if (drivers.Length > 0)
				{
					num9 /= (float)drivers.Length;
					num10 /= (float)drivers.Length;
					num11 /= (float)drivers.Length;
				}
			}
		}
		num6 += num7 + num8 + num9 * num + num10 * num2 - num11 * num3;
		return num6 / (num4 + num5 + num + num2);
	}

	public override bool WantsToRetire()
	{
		return base.WantsToRetire(Game.instance.time.now, this.improvementRate);
	}

	public Dictionary<string, Mechanic.DriverRelationship> allDriverRelationships
	{
		get
		{
			return this.mDictDriversRelationships;
		}
	}

	public const float minPitStopAddedError = 0f;

	public const float maxPitStopAddedError = 0f;

	public MechanicStats stats = new MechanicStats();

	public MechanicStats lastAccumulatedStats = new MechanicStats();

	public int driver;

	public float improvementRate = RandomUtility.GetRandom(0.1f, 1f);

	public MechanicBonus bonusOne;

	public MechanicBonus bonusTwo;

	private Map<string, Mechanic.DriverRelationship> mDriversRelationships;

	private Map<string, StatModificationHistory> mRelationshipModificationHistory;

	private Dictionary<string, Mechanic.DriverRelationship> mDictDriversRelationships = new Dictionary<string, Mechanic.DriverRelationship>();

	private Dictionary<string, StatModificationHistory> mDictRelationshipModificationHistory = new Dictionary<string, StatModificationHistory>();

	private readonly float weeklyRelationshipIncreaseRate = 2f;

	private readonly float weeklyRelationshipDecreaseRate = 2f;

	private readonly float endRaceRelationshipIncreaseRate = 15f;

	private readonly float endRaceRelationshipDecreaseRate = 15f;

	private readonly float positionRange = 5f;

	private readonly float negativeImprovementHQScalar = 0.9f;

	private readonly float negativeImprovementHQOverallScalar = 0.03f;

	private readonly float negativeMaxImprovementHQ = 0.75f;

	private readonly float maxMechanicRelationshipDecayPercent = 0.5f;

	private readonly float mechanicRelationshipInvalidDecay = -1f;

	public float driverRelationshipAmountBeforeEvent;

	private List<Driver> mRelationshipDriversCache = new List<Driver>();

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	public class DriverRelationship
	{
		public DriverRelationship()
		{
		}

		public DriverRelationship(Mechanic.DriverRelationship inDriverRelationship)
		{
			this.relationshipAmount = inDriverRelationship.relationshipAmount;
			this.numberOfWeeks = inDriverRelationship.numberOfWeeks;
		}

		public bool IsAtMax()
		{
			return this.relationshipAmount >= 99.995f;
		}

		public float relationshipAmount;

		public float relationshipAmountAfterDecay = -1f;

		public int numberOfWeeks;
	}
}
