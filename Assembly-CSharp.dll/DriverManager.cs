using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class DriverManager : PersonManager<Driver>
{
	public DriverManager()
	{
	}

	protected override void AddPeopleFromDatabase(Database database)
	{
		List<DatabaseEntry> driverData = database.driverData;
		for (int i = 0; i < driverData.Count; i++)
		{
			this.AddDriverToDatabase(driverData[i]);
		}
		for (int j = 0; j < driverData.Count; j++)
		{
			this.LoadPersonalityTraits(base.GetEntity(j), driverData[j]);
		}
		this.SetNullDriverData();
		this.SetDriverStatProgression();
	}

	public void OnSeasonStart()
	{
		for (int i = 0; i < base.count; i++)
		{
			base.GetEntity(i).OnSeasonStart();
		}
	}

	private void SetNullDriverData()
	{
		NullTeam nullTeam = Game.instance.teamManager.nullTeam;
		nullTeam.contractManager.AddEmployeeSlot(Contract.Job.Driver, this.nullDriver);
		nullTeam.contractManager.AddEmployeeSlot(Contract.Job.Driver, this.nullDriver);
	}

	private void SetDriverStatProgression()
	{
		this.ageDriverStatProgression = Game.instance.driverStatsProgressionManager.GetDriverStatsProgression("Age");
		this.maxDriverStatProgressionPerDay = Game.instance.driverStatsProgressionManager.GetDriverStatsProgression("Max Per Day");
		this.practiceDriverStatProgression = Game.instance.driverStatsProgressionManager.GetDriverStatsProgression("Practice");
		this.qualifyingDriverStatProgression = Game.instance.driverStatsProgressionManager.GetDriverStatsProgression("Qualifying");
		this.raceDriverStatProgression = Game.instance.driverStatsProgressionManager.GetDriverStatsProgression("Race");
	}

	public Driver AddDriverToDatabase(DatabaseEntry inEntry)
	{
		EntityManager entityManager = Game.instance.entityManager;
		Driver driver = entityManager.CreateEntity<Driver>();
		base.SetPersonalDetails(driver, inEntry);
		this.SetPersonalData(driver, inEntry);
		this.SetContractData(driver, inEntry);
		this.SetHistoryData(driver, inEntry);
		this.SetStatsData(driver, inEntry);
		base.SetPortraitData(driver, inEntry);
		if (driver.contract.job != Contract.Job.Unemployed)
		{
			this.AddDriverToChampionship(driver, false);
		}
		base.AddEntity(driver);
		return driver;
	}

	public void SetPersonalData(Driver inDriver, DatabaseEntry inData)
	{
		inDriver.weight = inData.GetIntValue("Weight");
		inDriver.driverNumber = inData.GetIntValue("Driver Number");
		inDriver.SetMorale((float)inData.GetIntValue("Morale") / 100f);
		inDriver.obedience = (float)inData.GetIntValue("Obedience") / 100f;
		inDriver.popularity.chairman = (float)inData.GetIntValue("Chairman Popularity") / 100f;
		inDriver.popularity.fans = (float)inData.GetIntValue("Fans Popularity") / 100f;
		inDriver.popularity.sponsors = (float)inData.GetIntValue("Sponsor Popularity") / 100f;
		inDriver.contractManager.contractPatience = inData.GetIntValue("Patience");
		inDriver.desiredChampionships = inData.GetIntValue("Desired Championships");
		inDriver.SetDesiredWins(inData.GetIntValue("Desired Wins"));
		inDriver.SetDesiredEarnings((long)inData.GetIntValue("Desired Earnings") * 1000L);
		inDriver.desiredBudget = (long)inData.GetIntValue("Desired Budget") * 1000L;
		string stringValue = inData.GetStringValue("Series Preference");
		if (stringValue != "Any")
		{
			string[] array = stringValue.Split(new char[]
			{
				';'
			});
			List<Championship.Series> list = new List<Championship.Series>();
			for (int i = 0; i < array.Length; i++)
			{
				list.Add((Championship.Series)((int)Enum.Parse(typeof(Championship.Series), array[i])));
			}
			inDriver.SetPreferedSeries(list);
		}
		inDriver.SetCarID(inData.GetIntValue("Car") - 1);
	}

	public void SetContractData(Driver inDriver, DatabaseEntry inData)
	{
		TeamManager teamManager = Game.instance.teamManager;
		ContractPerson contract = inDriver.contract;
		int num = inData.GetIntValue("Team") - 2;
		if (num >= 0)
		{
			contract.employeer = teamManager.GetEntity(num);
			List<EmployeeSlot> allEmployeeSlotsForJob = contract.GetTeam().contractManager.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
			string stringValue = inData.GetStringValue("Status");
			bool flag = false;
			if (stringValue == "Equal")
			{
				contract.SetCurrentStatus(ContractPerson.Status.Equal);
				contract.SetProposedStatus(ContractPerson.Status.Equal);
			}
			else if (stringValue == "One")
			{
				contract.SetCurrentStatus(ContractPerson.Status.One);
				contract.SetProposedStatus(ContractPerson.Status.One);
				if (allEmployeeSlotsForJob[0].personHired == null)
				{
					allEmployeeSlotsForJob[0].personHired = inDriver;
					flag = true;
				}
				inDriver.mCarID = 0;
			}
			else if (stringValue == "Two")
			{
				contract.SetCurrentStatus(ContractPerson.Status.Two);
				contract.SetProposedStatus(ContractPerson.Status.Two);
				if (allEmployeeSlotsForJob[1].personHired == null)
				{
					allEmployeeSlotsForJob[1].personHired = inDriver;
					flag = true;
				}
				inDriver.mCarID = 1;
			}
			else if (stringValue == "Reserve")
			{
				contract.SetCurrentStatus(ContractPerson.Status.Reserve);
				contract.SetProposedStatus(ContractPerson.Status.Reserve);
				if (allEmployeeSlotsForJob[2].personHired == null)
				{
					allEmployeeSlotsForJob[2].personHired = inDriver;
					flag = true;
				}
				inDriver.mCarID = -1;
			}
			// Set Driver Slot depending on carID
			if (!flag && inDriver.mCarID >= 0 && inDriver.mCarID <= 1) {
				if (allEmployeeSlotsForJob[inDriver.mCarID].IsAvailable())
				{
					allEmployeeSlotsForJob[inDriver.mCarID].personHired = inDriver;
					flag = true;
				}
			}
			if (!flag)
			{
				for (int i = 0; i < allEmployeeSlotsForJob.Count; i++)
				{
					EmployeeSlot employeeSlot = allEmployeeSlotsForJob[i];
					if (employeeSlot.IsAvailable())
					{
						employeeSlot.personHired = inDriver;
						if (i >= 1)
							inDriver.mCarID = -1;
						else
							inDriver.mCarID = i;
						break;
					}
				}
			}
			contract.SetPerson(inDriver);
			contract.job = Contract.Job.Driver;
			contract.SetContractState(Contract.ContractStatus.OnGoing);
			int intValue = inData.GetIntValue("Contract Start");
			DateTime startDate = new DateTime(intValue, 1, 1);
			int intValue2 = inData.GetIntValue("Contract End");
			DateTime endDate = new DateTime(intValue2, 12, 31);
			contract.startDate = startDate;
			contract.endDate = endDate;
			contract.optionClauseEndDate = Game.instance.time.now.AddHours(1.0);
			int num2 = 1000000;
			contract.yearlyWages = Mathf.RoundToInt(inData.GetFloatValue("Wages") * (float)num2);
			contract.qualifyingBonus = Mathf.RoundToInt(inData.GetFloatValue("Qualifying Bonus Amount") * (float)num2);
			contract.qualifyingBonusTargetPosition = Mathf.RoundToInt(inData.GetFloatValue("Qualifying Bonus Position"));
			contract.hasQualifyingBonus = (contract.qualifyingBonus > 0);
			contract.raceBonus = Mathf.RoundToInt(inData.GetFloatValue("Race Bonus Amount") * (float)num2);
			contract.raceBonusTargetPosition = Mathf.RoundToInt(inData.GetFloatValue("Race Bonus Position"));
			contract.hasRaceBonus = (contract.raceBonus > 0);
			contract.championBonus = Mathf.RoundToInt(inData.GetFloatValue("Champion Bonus") * (float)num2);
			contract.payDriver = Mathf.RoundToInt(inData.GetFloatValue("Pay Driver") * (float)num2);
			contract.hasSignOnFee = (contract.signOnFee > 0);
			contract.GetTeam().contractManager.AddSignedContract(contract);
		}
		else
		{
			contract.job = Contract.Job.Unemployed;
			int num3 = 1000000;
			contract.yearlyWages = Mathf.RoundToInt(0.1f * (float)num3);
			contract.SetPerson(inDriver);
		}
	}

	public void SetHistoryData(Driver inDriver, DatabaseEntry inData)
	{
	}

	public void SetStatsData(Driver inDriver, DatabaseEntry inData)
	{
		DriverStats driverStats = new DriverStats();
		driverStats.GenerateRandomStatRanges();
		driverStats.braking = inData.GetFloatValue("Braking");
		driverStats.cornering = inData.GetFloatValue("Cornering");
		driverStats.smoothness = inData.GetFloatValue("Smoothness");
		driverStats.overtaking = inData.GetFloatValue("Overtaking");
		driverStats.consistency = inData.GetFloatValue("Consistency");
		driverStats.adaptability = inData.GetFloatValue("Adaptability");
		driverStats.fitness = inData.GetFloatValue("Fitness");
		driverStats.feedback = inData.GetFloatValue("Feedback");
		driverStats.focus = inData.GetFloatValue("Focus");
		driverStats.balance = (float)inData.GetIntValue("Driving Style") / 100f;
		driverStats.experience = (float)inData.GetIntValue("Experience") / 100f;
		driverStats.marketability = (float)inData.GetIntValue("Marketability") / 100f;
		driverStats.favouriteBrakesSupplier = inData.GetIntValue("Brake Supplier Preference");
		driverStats.fame = inData.GetIntValue("Fame");
		driverStats.scoutingLevelRequired = inData.GetIntValue("Scouting Level");
		driverStats.GenerateImprovementRates();
		inDriver.SetImprovementRate((float)inData.GetIntValue("Improvement Rate") / 100f);
		inDriver.SetDriverStats(driverStats, inData.GetIntValue("Potential"));
		inDriver.driverStamina.SetOptimalZone(inData.GetFloatValue("Optimal Zone"));
	}

	public void LoadPersonalityTraits(Driver inDriver, DatabaseEntry inData)
	{
		string[] array = inData.GetStringValue("Traits").Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (!(text == string.Empty))
			{
				int num;
				if (!int.TryParse(text, out num))
				{
					global::Debug.LogWarningFormat("Driver with name {0} has invalid personality trait assigned, with ID {1}", new object[]
					{
						inDriver.name,
						text
					});
				}
				else if (num != 0)
				{
					PersonalityTraitData personalityTraitData;
					if (!Game.instance.personalityTraitManager.personalityTraits.TryGetValue(num, out personalityTraitData))
					{
						global::Debug.LogWarningFormat("Driver with name {0} has invalid personality trait assigned, with ID {1}", new object[]
						{
							inDriver.name,
							num
						});
					}
					else
					{
						List<PersonalityTrait> allPersonalityTraits = inDriver.personalityTraitController.GetAllPersonalityTraits();
						for (int j = 0; j < allPersonalityTraits.Count; j++)
						{
							if (allPersonalityTraits[j].data.IsTraitOpposite(personalityTraitData))
							{
								global::Debug.LogWarningFormat("Driver with name {0} has invalid opposite personality traits assigned, with ID {1} and ID {2}", new object[]
								{
									inDriver.name,
									num,
									allPersonalityTraits[j].data.ID
								});
							}
						}
						inDriver.personalityTraitController.AddPersonalityTrait(personalityTraitData, false);
						if (inDriver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.WIllNotRenewContract) && !inDriver.IsFreeAgent() && inDriver.IsMainDriver())
						{
							int inIndex = (inDriver.contract.GetTeam().GetDriverIndex(inDriver) != 0) ? 0 : 1;
							Driver driver = inDriver.contract.GetTeam().GetDriver(inIndex);
							inDriver.SetRivalDriver(driver);
						}
					}
				}
			}
		}
		inDriver.personalityTraitController.SetFirstCooldownDate();
	}

	public void AddDriverToChampionship(Driver inDriver, bool addRegardless = false)
	{
		Championship championship = inDriver.contract.GetTeam().championship;
		if (inDriver.IsMainDriver() || addRegardless || championship.series == Championship.Series.EnduranceSeries)
		{
			ChampionshipEntry_v1 championshipEntry = inDriver.GetChampionshipEntry();
			bool flag = championshipEntry != null && championship != null && championship.standings.isEntryInactive(championshipEntry);
			if (championshipEntry == null || championshipEntry.championship != championship || flag)
			{
				championship.standings.AddEntry(inDriver, championship);
			}
		}
	}

	public void RemoveDriverEntryFromChampionship(Driver inDriver)
	{
		ChampionshipEntry_v1 championshipEntry = inDriver.GetChampionshipEntry();
		if (championshipEntry != null && championshipEntry.races <= 0)
		{
			championshipEntry.championship.standings.RemoveEntry(inDriver);
		}
		inDriver.ResetChampionshipEntry();
	}

	public void PopulateWithRandomData(Driver inDriver, DatabaseEntry inEntry, RegenManager.RegenType inType, DatabaseEntry inConfigEntry)
	{
		this.GenerateRandomPersonalData(inDriver, inEntry);
		this.GenerateRandomContractData(inDriver);
		this.GenerateRandomHistoryData(inDriver);
		this.GenerateRandomStatsData(inDriver, inEntry, inType, inConfigEntry);
		if (inType != RegenManager.RegenType.Replacement)
		{
			this.GenerateRandomPersonalityTraitData(inDriver);
		}
	}

	public void PopulateWithData(Driver inDriver, Person inPerson, DatabaseEntry inEntry, RegenManager.RegenType inType)
	{
		this.GenerateRandomPersonalData(inDriver, inEntry);
		this.GenerateRandomContractData(inDriver);
		this.GenerateRandomHistoryData(inDriver);
		if (inType != RegenManager.RegenType.Replacement)
		{
			this.GenerateRandomPersonalityTraitData(inDriver);
		}
		if (inPerson is Driver)
		{
			this.GenerateRandomStatsDataFromParent(inDriver, inEntry, inPerson as Driver, inType);
		}
		else
		{
			this.GenerateRandomStatsData(inDriver, inEntry, inType, null);
		}
	}

	private void CreateReplacementDriversPool()
	{
		for (int i = 0; i < GameStatsConstants.replacementPeopleCount; i++)
		{
			this.CreateReplacement();
		}
	}

	public Driver CreateReplacement()
	{
		Driver driver = App.instance.regenManager.CreateDriver(RegenManager.RegenType.Replacement);
		driver.contract.SetPerson(driver);
		this.mReplacementPeople.Add(driver);
		return driver;
	}

	public void RegenerateAllUnemployedDrivers()
	{
		List<Driver> entityList = base.GetEntityList();
		List<int> list = new List<int>();
		for (int i = 0; i < entityList.Count; i++)
		{
			if (entityList[i].IsFreeAgent())
			{
				list.Add(i);
			}
		}
		for (int j = list.Count - 1; j >= 0; j--)
		{
			PersonManager<Driver>.RetireFreeAgentPerson(entityList[list[j]]);
		}
	}

	public Driver GetReplacementDriver(bool moveToEndOfList = false)
	{
		int i = 0;
		while (i < this.mReplacementPeople.Count)
		{
			if (this.mReplacementPeople[i].IsFreeAgent())
			{
				if (moveToEndOfList)
				{
					return GameUtility.MoveObjectToEndOfList<Driver>(ref this.mReplacementPeople, this.mReplacementPeople[i]);
				}
				return this.mReplacementPeople[i];
			}
			else
			{
				i++;
			}
		}
		return this.CreateReplacement();
	}

	public Driver RetireReplacement(Driver inReplacement)
	{
		if (this.mReplacementPeople.Contains(inReplacement))
		{
			this.mReplacementPeople.Remove(inReplacement);
		}
		return this.CreateReplacement();
	}

	private void GenerateRandomPersonalityTraitData(Driver inDriver)
	{
		inDriver.personalityTraitController.AssignRandomPersonalityTraits();
	}

	private void GenerateRandomPersonalData(Driver inDriver, DatabaseEntry inEntry)
	{
		inDriver.weight = this.GenerateWeight(inDriver.gender, inEntry);
		inDriver.driverNumber = RandomUtility.GetRandom(inEntry.GetIntValue("Driver Number Min"), inEntry.GetIntValue("Driver Number Max"));
		inDriver.SetMorale((float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Morale")) / 100f);
		inDriver.obedience = RandomUtility.GetRandom(0f, (float)inEntry.GetIntValue("Obedience")) / 100f;
		inDriver.popularity.chairman = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Chairman Popularity")) / 100f;
		inDriver.popularity.fans = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Fans Popularity")) / 100f;
		inDriver.popularity.sponsors = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Sponsor Popularity")) / 100f;
		inDriver.contractManager.contractPatience = RandomUtility.GetRandom(0, inEntry.GetIntValue("Patience"));
	}

	private int GenerateWeight(Person.Gender inGender, DatabaseEntry inEntry)
	{
		if (inGender != Person.Gender.Male)
		{
			if (inGender == Person.Gender.Female)
			{
				return Mathf.RoundToInt(Mathf.Clamp(RandomUtility.GetRandomNormallyDistributed((float)inEntry.GetIntValue("Female Mean Weight"), (float)inEntry.GetIntValue("Female StdDev Weight")), (float)inEntry.GetIntValue("Weight Female Min"), (float)inEntry.GetIntValue("Weight Female Max")));
			}
		}
		return Mathf.RoundToInt(Mathf.Clamp(RandomUtility.GetRandomNormallyDistributed((float)inEntry.GetIntValue("Male Mean Weight"), (float)inEntry.GetIntValue("Male StdDev Weight")), (float)inEntry.GetIntValue("Weight Male Min"), (float)inEntry.GetIntValue("Weight Male Max")));
	}

	private void GenerateRandomContractData(Driver inDriver)
	{
		ContractPerson contract = inDriver.contract;
		contract.job = Contract.Job.Unemployed;
	}

	private void GenerateRandomHistoryData(Driver inDriver)
	{
		inDriver.GenerateCareerHistory();
	}

	private void GenerateRandomStatsData(Driver inDriver, DatabaseEntry inEntry, RegenManager.RegenType inType, DatabaseEntry inConfigEntry)
	{
		DriverStats driverStats = new DriverStats();
		driverStats.GenerateRandomStatRanges();
		if (inConfigEntry != null)
		{
			driverStats.GenerateRandomFromPool(inEntry, inConfigEntry.GetIntValue("Bias"), inConfigEntry.GetIntValue("Range"));
		}
		else
		{
			switch (inType)
			{
			case RegenManager.RegenType.Random:
				driverStats.GenerateRandomFromPool(inEntry, 0, 180);
				goto IL_BA;
			case RegenManager.RegenType.Good:
				driverStats.GenerateRandomFromPool(inEntry, 10, 180);
				goto IL_BA;
			case RegenManager.RegenType.Poor:
				driverStats.GenerateRandomFromPool(inEntry, 0, 90);
				goto IL_BA;
			case RegenManager.RegenType.Average:
				driverStats.GenerateRandomFromPool(inEntry, 5, 90);
				goto IL_BA;
			case RegenManager.RegenType.Replacement:
				driverStats.GenerateRandomFromPool(inEntry, 0, 45);
				goto IL_BA;
			}
			driverStats.GenerateRandom(inEntry);
		}
		IL_BA:
		driverStats.balance = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Driving Style")) / 100f;
		driverStats.experience = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Experience")) / 100f;
		driverStats.marketability = ((inType != RegenManager.RegenType.Replacement) ? ((float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Marketability")) / 100f) : 0f);
		driverStats.favouriteBrakesSupplier = RandomUtility.GetRandom(0, inEntry.GetIntValue("Brake Supplier Preference"));
		inDriver.desiredChampionships = RandomUtility.GetRandom(inEntry.GetIntValue("GoalChampionshipsMin"), inEntry.GetIntValue("GoalChampionshipsMax"));
		inDriver.SetDesiredWins(RandomUtility.GetRandom(inEntry.GetIntValue("DesiredWinsMin"), inEntry.GetIntValue("DesiredWinsMax")));
		inDriver.SetDesiredEarnings(0L);
		inDriver.desiredBudget = (long)inEntry.GetIntValue("DesiredBudget") * 1000L;
		inDriver.SetImprovementRate((float)RandomUtility.GetRandom(inEntry.GetIntValue("Improvement Rate Min"), inEntry.GetIntValue("Improvement Rate Max")) / 100f);
		inDriver.SetPeakDuration(RandomUtility.GetRandom(inEntry.GetIntValue("Peak Duration Min"), inEntry.GetIntValue("Peak Duration Max")));
		inDriver.peakAge = inDriver.CalculatePeakAge(inEntry.GetIntValue("Peak Age Min"), inEntry.GetIntValue("Peak Age Max"));
		int inPotential = 0;
		if (inType != RegenManager.RegenType.Replacement)
		{
			inPotential = Mathf.Min(RandomUtility.GetRandom(inEntry.GetIntValue("Potential Min"), inEntry.GetIntValue("Potential Max")), driverStats.GetMaxPotential());
		}
		if (inDriver.driverStamina != null)
		{
			inDriver.driverStamina.SetOptimalZone(RandomUtility.GetRandom(inEntry.GetFloatValue("Optimal Zone Min"), inEntry.GetFloatValue("Optimal Zone Max")));
		}
		driverStats.GenerateImprovementRates();
		inDriver.SetDriverStats(driverStats, inPotential);
	}

	private void GenerateRandomStatsDataFromParent(Driver inDriver, DatabaseEntry inEntry, Driver inParent, RegenManager.RegenType inType)
	{
		DriverStats driverStats = new DriverStats();
		driverStats.GenerateRandomStatRanges();
		switch (inType)
		{
		case RegenManager.RegenType.Random:
			driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 0, 180);
			goto IL_BA;
		case RegenManager.RegenType.Good:
			driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 10, 180);
			goto IL_BA;
		case RegenManager.RegenType.Poor:
			driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 0, 90);
			goto IL_BA;
		case RegenManager.RegenType.Average:
			driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 5, 90);
			goto IL_BA;
		case RegenManager.RegenType.Replacement:
			driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 0, 45);
			goto IL_BA;
		}
		driverStats.GenerateFromParent(inParent.GetDriverStats(), inEntry, 0, 180);
		IL_BA:
		driverStats.balance = RandomUtility.GetRandom(Math.Max(0f, inParent.GetDriverStats().balance - 0.25f), Math.Min(1f, inParent.GetDriverStats().balance + 0.25f));
		driverStats.experience = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Experience")) / 100f;
		driverStats.marketability = (float)RandomUtility.GetRandom(0, inEntry.GetIntValue("Marketability")) / 100f;
		driverStats.favouriteBrakesSupplier = RandomUtility.GetRandom(0, inEntry.GetIntValue("Brake Supplier Preference"));
		inDriver.desiredChampionships = RandomUtility.GetRandom(inEntry.GetIntValue("GoalChampionshipsMin"), inEntry.GetIntValue("GoalChampionshipsMax"));
		inDriver.SetDesiredWins(RandomUtility.GetRandom(inEntry.GetIntValue("DesiredWinsMin"), inEntry.GetIntValue("DesiredWinsMax")));
		inDriver.SetDesiredEarnings(0L);
		inDriver.desiredBudget = (long)inEntry.GetIntValue("DesiredBudget") * 1000L;
		inDriver.SetImprovementRate((float)RandomUtility.GetRandom(inEntry.GetIntValue("Improvement Rate Min"), inEntry.GetIntValue("Improvement Rate Max")) / 100f);
		inDriver.SetPeakDuration(RandomUtility.GetRandom(inEntry.GetIntValue("Peak Duration Min"), inEntry.GetIntValue("Peak Duration Max")));
		inDriver.peakAge = inDriver.CalculatePeakAge(inEntry.GetIntValue("Peak Age Min"), inEntry.GetIntValue("Peak Age Max"));
		int inPotential = 0;
		if (inType != RegenManager.RegenType.Replacement)
		{
			inPotential = Mathf.Min(RandomUtility.GetRandom(inEntry.GetIntValue("Potential Min"), inEntry.GetIntValue("Potential Max")), driverStats.GetMaxPotential());
		}
		driverStats.GenerateImprovementRates();
		inDriver.SetDriverStats(driverStats, inPotential);
	}

	private Driver FindRandomRetired()
	{
		List<Driver> list = base.GetEntityList().FindAll((Driver curDriver) => curDriver.HasRetired());
		if (list.Count > 0)
		{
			return list[RandomUtility.GetRandom(0, list.Count)];
		}
		return null;
	}

	public override void OnStartingGame()
	{
		this.AddActions();
		this.CreateReplacementDriversPool();
	}

	public void OnLoad()
	{
		this.AddActions();
		this.SetDriverStatProgression();
	}

	public static List<Driver> GetChampionshipsDriverStandingsOnAbility(Championship inChampionship, params Driver[] inAdditionalDrivers)
	{
		List<Driver> list = new List<Driver>();
		list.AddRange(inAdditionalDrivers);
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			list.AddRange(entity.GetDrivers());
		}
		List<Driver> list2 = new List<Driver>();
		while (list.Count != 0)
		{
			Driver driver = null;
			for (int j = 0; j < list.Count; j++)
			{
				Driver driver2 = list[j];
				if (driver2 == null)
				{
					list.Remove(driver2);
					j--;
				}
				else if (driver == null || driver.GetDriverStats().GetAbility() < driver2.GetDriverStats().GetAbility())
				{
					driver = driver2;
				}
			}
			list2.Add(driver);
			list.Remove(driver);
		}
		return list2;
	}

	private static void AddDriver(ref List<Driver> inList, Driver inDriver)
	{
		if (inDriver != null)
		{
			inList.Add(inDriver);
		}
	}

	public static Driver GetDriverWithBestExpectedRaceResult(Championship inChampionship)
	{
		Driver driver = null;
		for (int i = 0; i < inChampionship.standings.driverEntryCount; i++)
		{
			Driver entity = inChampionship.standings.GetDriverEntry(i).GetEntity<Driver>();
			if (!entity.IsFreeAgent())
			{
				if (driver == null || driver.expectedRacePosition > entity.expectedRacePosition)
				{
					driver = entity;
				}
			}
		}
		return driver;
	}

	public static Driver GetDriverWithExpectedRaceResult(int inExpectation, Championship inChampionship)
	{
		for (int i = 0; i < inChampionship.standings.driverEntryCount; i++)
		{
			Driver entity = inChampionship.standings.GetDriverEntry(i).GetEntity<Driver>();
			if (entity.expectedRacePosition == inExpectation)
			{
				return entity;
			}
		}
		return null;
	}

	public static Driver GetDriverOfTheSeason(Championship inChampionship)
	{
		int num = 0;
		Driver driver = null;
		int driverEntryCount = inChampionship.standings.driverEntryCount;
		for (int i = 0; i < driverEntryCount; i++)
		{
			ChampionshipEntry_v1 driverEntry = inChampionship.standings.GetDriverEntry(i);
			Driver entity = driverEntry.GetEntity<Driver>();
			int num2 = entity.startOfSeasonExpectedChampionshipPosition - driverEntry.GetCurrentChampionshipPosition();
			if (driver == null || (driverEntry.GetCurrentPoints() > 0 && num2 > num))
			{
				driver = entity;
				num = num2;
			}
		}
		return driver;
	}

	public void OnDayEnd()
	{
		List<Driver> entityList = base.GetEntityList();
		int count = entityList.Count;
		for (int i = 0; i < count; i++)
		{
			entityList[i].OnDayEnd();
		}
	}

	public void AddActions()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Combine(time.OnDayEnd, new Action(this.OnDayEnd));
		GameTimer time2 = Game.instance.time;
		time2.OnYearEnd = (Action)Delegate.Combine(time2.OnYearEnd, new Action(base.OnYearEnd));
	}

	public void RemoveActions()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Remove(time.OnDayEnd, new Action(this.OnDayEnd));
		GameTimer time2 = Game.instance.time;
		time2.OnYearEnd = (Action)Delegate.Remove(time2.OnYearEnd, new Action(base.OnYearEnd));
	}

	public NullDriver nullDriver = new NullDriver();

	public DriverStatsProgression ageDriverStatProgression;

	public DriverStatsProgression maxDriverStatProgressionPerDay;

	public DriverStatsProgression raceDriverStatProgression;

	public DriverStatsProgression qualifyingDriverStatProgression;

	public DriverStatsProgression practiceDriverStatProgression;

	private static IEnumerable<Driver> mDriversCache;
}
