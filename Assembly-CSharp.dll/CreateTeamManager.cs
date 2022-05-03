using System;
using System.Collections.Generic;
using System.Text;
using MM2;
using UnityEngine;

public class CreateTeamManager
{
	public CreateTeamManager()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static CreateTeamManager()
	{
	}

	public static void OnGameStart()
	{
		CreateTeamManager.mSteps.Clear();
		for (int i = 0; i < 4; i++)
		{
			CreateTeamManager.mSteps.Add((CreateTeamManager.Step)i, false);
		}
		List<DatabaseEntry> list = App.instance.database.createTeamDefaultsData;
		CreateTeamManager.defaultTeamSettings.Clear();
		CreateTeamManager.defaultPersons.Clear();
		CreateTeamManager.driverData.Clear();
		for (int j = 0; j < list.Count; j++)
		{
			DatabaseEntry databaseEntry = list[j];
			int championshipID = 0;
			bool flag = int.TryParse(databaseEntry.GetStringValue("Championship ID"), out championshipID);
			if (flag)
			{
				CreateTeamManager.TeamDefaults teamDefaults = new CreateTeamManager.TeamDefaults();
				Championship championshipByID = Game.instance.championshipManager.GetChampionshipByID(championshipID);
				teamDefaults.championship = championshipByID;
				teamDefaults.defaultTeamNationality = Nationality.GetNationalityByName(databaseEntry.GetStringValue("Nationality"));
				teamDefaults.defaultLastName = databaseEntry.GetStringValue("Last Name");
				teamDefaults.defaultEngineSupplier = databaseEntry.GetIntValue("Engine Supplier");
				teamDefaults.defaultBrakesSupplier = databaseEntry.GetIntValue("Brakes Supplier");
				teamDefaults.defaultFuelSupplier = databaseEntry.GetIntValue("Fuel Supplier");
				teamDefaults.defaultMaterialsSupplier = databaseEntry.GetIntValue("Materials Supplier");
				teamDefaults.defaultBatterySupplier = databaseEntry.GetIntValue("Battery Supplier");
				teamDefaults.defaultAdvancedERSSupplier = databaseEntry.GetIntValue("Advanced ERS Supplier");
				teamDefaults.defaultPartStatBonusMin = databaseEntry.GetIntValue("Part Stat Bonus Min");
				teamDefaults.defaultPartStatBonusMax = databaseEntry.GetIntValue("Part Stat Bonus Max");
				teamDefaults.defaultPartMaxPerformanceMin = databaseEntry.GetIntValue("Part Max Performance Min");
				teamDefaults.defaultPartMaxPerformanceMax = databaseEntry.GetIntValue("Part Max Performance Max");
				teamDefaults.defaultPartReliabilityMin = (float)databaseEntry.GetIntValue("Part Reliability Min") / 100f;
				teamDefaults.defaultPartReliabilityMax = (float)databaseEntry.GetIntValue("Part Reliability Max") / 100f;
				teamDefaults.defaultPrimaryColor = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Staff Primary Color"));
				teamDefaults.defaultSecondaryColor = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Staff Secondary Color"));
				teamDefaults.defaultLiveryColor.primary = teamDefaults.defaultPrimaryColor;
				teamDefaults.defaultLiveryColor.secondary = teamDefaults.defaultSecondaryColor;
				teamDefaults.defaultLiveryColor.tertiary = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Livery Tertiary Color"));
				teamDefaults.defaultLiveryColor.trim = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Livery Trim Color"));
				teamDefaults.defaultLiveryColor.lightSponsor = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Livery Light Sponsor Color"));
				teamDefaults.defaultLiveryColor.darkSponsor = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Livery Dark Sponsor Color"));
				teamDefaults.defaultCarSponsors[0] = databaseEntry.GetIntValue("Car Sponsor 1 ID");
				teamDefaults.defaultCarSponsors[1] = databaseEntry.GetIntValue("Car Sponsor 2 ID");
				teamDefaults.defaultCarSponsors[2] = databaseEntry.GetIntValue("Car Sponsor 3 ID");
				teamDefaults.defaultCarSponsors[3] = databaseEntry.GetIntValue("Car Sponsor 4 ID");
				teamDefaults.defaultCarSponsors[4] = databaseEntry.GetIntValue("Car Sponsor 5 ID");
				teamDefaults.defaultCarSponsors[5] = databaseEntry.GetIntValue("Car Sponsor 6 ID");
				teamDefaults.defaultCarLiveryID = databaseEntry.GetIntValue("Car Livery ID");
				teamDefaults.defaultTeamLogoStyle = databaseEntry.GetIntValue("Team Logo Style");
				teamDefaults.defaultHatStyle = databaseEntry.GetIntValue("Hat Style");
				teamDefaults.defaultShirtStyle = databaseEntry.GetIntValue("Shirt Style");
				CreateTeamManager.defaultTeamSettings.Add(teamDefaults);
			}
			string stringValue = databaseEntry.GetStringValue("Person Type");
			if (!string.IsNullOrEmpty(stringValue) && stringValue != "0")
			{
				Person person = new Person();
				CreateTeamManager.defaultPersons.Add(person);
				person.gender = PersonManager<Person>.GetGender(databaseEntry.GetStringValue("Gender"));
				person.portrait = PersonManager<Person>.GetPortrait(databaseEntry);
			}
		}
		list = App.instance.database.createTeamDriversData;
		for (int k = 0; k < list.Count; k++)
		{
			DatabaseEntry databaseEntry2 = list[k];
			int intValue = databaseEntry2.GetIntValue("Championship ID");
			if (!CreateTeamManager.driverData.ContainsKey(intValue))
			{
				CreateTeamManager.driverData.Add(intValue, new List<DatabaseEntry>());
			}
			CreateTeamManager.driverData[intValue].Add(databaseEntry2);
		}
	}

	public static void ResetAllStaticReferences()
	{
		CreateTeamManager.mNewTeam = null;
		CreateTeamManager.mSelectedTeam = null;
		CreateTeamManager.mTeamColor = null;
		CreateTeamManager.mChampionship = null;
		CreateTeamManager.mTeams.Clear();
		CreateTeamManager.driverData.Clear();
		CreateTeamManager.defaultTeamSettings.Clear();
		CreateTeamManager.defaultPersons.Clear();
		CreateTeamManager.defaultSettings = null;
	}

	public static void StartCreateNewTeam()
	{
		switch (CreateTeamManager.state)
		{
		case CreateTeamManager.State.Iddle:
			CreateTeamManager.state = CreateTeamManager.State.CreatingTeam;
			break;
		case CreateTeamManager.State.CreatingTeam:
			return;
		case CreateTeamManager.State.Hold:
			CreateTeamManager.state = CreateTeamManager.State.CreatingTeam;
			break;
		}
	}

	public static void SelectChampionship(Championship inChampionship)
	{
		if (inChampionship == null || CreateTeamManager.mChampionship == inChampionship || CreateTeamManager.state != CreateTeamManager.State.CreatingTeam)
		{
			return;
		}
		CreateTeamManager.mChampionship = inChampionship;
		CreateTeamManager.mTeams.Clear();
		int teamEntryCount = CreateTeamManager.mChampionship.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			CreateTeamManager.mTeams.Add(CreateTeamManager.mChampionship.standings.GetTeamEntry(i).GetEntity<Team>());
		}
		CreateTeamManager.mTeams.Sort((Team x, Team y) => x.GetStarsStat().CompareTo(y.GetStarsStat()));
		CreateTeamManager.mTeams.Reverse();
		Team inTeam = CreateTeamManager.mTeams[CreateTeamManager.mTeams.Count - RandomUtility.GetRandom(1, 3)];
		CreateTeamManager.CreateNewTeam(inTeam);
	}

	public static void CreateNewTeam(Team inTeam)
	{
		if (inTeam == null)
		{
			return;
		}
		CreateTeamManager.ResetSteps();
		CreateTeamManager.mSelectedTeam = inTeam;
		CreateTeamManager.mNewTeam = new Team();
		CreateTeamManager.mTeamColor = new TeamColor();
		CreateTeamManager.defaultSettings = CreateTeamManager.GetTeamDefaultsForChampionship(CreateTeamManager.mChampionship);
		CreateTeamManager.state = CreateTeamManager.State.CreatingTeam;
		string text = Localisation.LocaliseID(CreateTeamManager.defaultSettings.defaultLastName, null);
		if (text.Contains("String in database"))
		{
			text = string.Empty;
		}
		CreateTeamManager.SetTeamName(Game.instance.player.firstName, text);
		CreateTeamManager.SetTeamLogo(CreateTeamManager.defaultSettings.defaultTeamLogoStyle);
		CreateTeamManager.SetTeamNationality(CreateTeamManager.defaultSettings.defaultTeamNationality);
		TeamColor teamColor = CreateTeamManager.mSelectedTeam.GetTeamColor();
		CreateTeamManager.SetUIColors(teamColor.primaryUIColour, teamColor.secondaryUIColour);
		CreateTeamManager.SetLiveryColors(teamColor.carColor, CreateTeamManager.defaultSettings.defaultLiveryColor);
		CreateTeamManager.SetStaffColors(CreateTeamManager.defaultSettings.defaultPrimaryColor, CreateTeamManager.defaultSettings.defaultSecondaryColor);
		CreateTeamManager.SetHatStyle(CreateTeamManager.defaultSettings.defaultHatStyle);
		CreateTeamManager.SetBodyStyle(CreateTeamManager.defaultSettings.defaultShirtStyle);
		CreateTeamManager.mTeamColor.customLogoColor.primary = CreateTeamManager.mTeamColor.staffColor.primary;
		CreateTeamManager.mTeamColor.customLogoColor.secondary = CreateTeamManager.mTeamColor.staffColor.secondary;
		CreateTeamManager.mNewTeam.championship = CreateTeamManager.mSelectedTeam.championship;
		CreateTeamManager.mNewTeam.carManager.Start(CreateTeamManager.mNewTeam);
		CreateTeamManager.mNewTeam.carManager.partInventory = CreateTeamManager.mSelectedTeam.carManager.partInventory;
		CreateTeamManager.mNewTeam.liveryID = CreateTeamManager.defaultSettings.defaultCarLiveryID;
		CreateTeamManager.mTeamColor.liveryEditorOptions = teamColor.liveryEditorOptions;
		CreateTeamManager.mTeamColor.darkSponsorOptions = teamColor.darkSponsorOptions;
		CreateTeamManager.mTeamColor.lighSponsorOptions = teamColor.lighSponsorOptions;
	}

	public static void SetTeamName(string inFirstName, string inLastName)
	{
		using (GameUtility.StringBuilderWrapper builderSafe = GameUtility.GlobalStringBuilderPool.GetBuilderSafe())
		{
			StringBuilder stringBuilder = builderSafe.stringBuilder;
			stringBuilder.Append(inFirstName);
			stringBuilder.Append(' ');
			stringBuilder.Append(inLastName);
			CreateTeamManager.mNewTeam.name = stringBuilder.ToString();
			CreateTeamManager.teamFirstName = inFirstName;
			CreateTeamManager.teamLastName = inLastName;
		}
	}

	public static void SetTeamNationality(Nationality inNationality)
	{
		CreateTeamManager.mNewTeam.nationality = inNationality;
	}

	public static void SetTeamLogo(int inPresetID)
	{
		CreateTeamManager.mNewTeam.customLogo.styleID = inPresetID;
	}

	public static void SetUIColors(TeamColor.UIColour inPrimaryUIColor, TeamColor.UIColour inSecondaryUIColor)
	{
		CreateTeamManager.mTeamColor.primaryUIColour = inPrimaryUIColor;
		CreateTeamManager.mTeamColor.secondaryUIColour = inSecondaryUIColor;
	}

	public static void SetLiveryColors(Color inCarColor, TeamColor.LiveryColour inLiveryColor)
	{
		CreateTeamManager.mTeamColor.carColor = inCarColor;
		CreateTeamManager.mTeamColor.livery = inLiveryColor;
	}

	public static void SetStaffColors(Color inPrimaryColor, Color inSecondaryColor)
	{
		CreateTeamManager.mTeamColor.staffColor.primary = inPrimaryColor;
		CreateTeamManager.mTeamColor.staffColor.secondary = inSecondaryColor;
		CreateTeamManager.mTeamColor.helmetColor.primary = inPrimaryColor;
		CreateTeamManager.mTeamColor.helmetColor.secondary = inSecondaryColor;
		CreateTeamManager.mTeamColor.helmetColor.tertiary = Color.white;
	}

	public static void SetHatStyle(int inHatStyle)
	{
		CreateTeamManager.mNewTeam.driversHatStyle = inHatStyle;
	}

	public static void SetBodyStyle(int inBodyStyle)
	{
		CreateTeamManager.mNewTeam.driversBodyStyle = inBodyStyle;
	}

	public static Team CompleteCreateNewTeam(Investor inInvestor)
	{
		CreateTeamManager.mSelectedTeam.name = CreateTeamManager.mNewTeam.name;
		TeamColor teamColor = CreateTeamManager.mSelectedTeam.GetTeamColor();
		teamColor.primaryUIColour = CreateTeamManager.mTeamColor.primaryUIColour;
		teamColor.secondaryUIColour = CreateTeamManager.mTeamColor.secondaryUIColour;
		teamColor.staffColor = CreateTeamManager.mTeamColor.staffColor;
		teamColor.carColor = CreateTeamManager.mTeamColor.staffColor.primary;
		teamColor.livery = CreateTeamManager.mTeamColor.livery;
		teamColor.helmetColor = CreateTeamManager.mTeamColor.helmetColor;
		teamColor.customLogoColor = CreateTeamManager.mTeamColor.customLogoColor;
		teamColor.primaryUIColour.normal = CreateTeamManager.mTeamColor.staffColor.primary;
		teamColor.secondaryUIColour.normal = CreateTeamManager.mTeamColor.staffColor.secondary;
		CreateTeamManager.mSelectedTeam.driversHatStyle = CreateTeamManager.mNewTeam.driversHatStyle;
		CreateTeamManager.mSelectedTeam.driversBodyStyle = CreateTeamManager.mNewTeam.driversBodyStyle;
		CreateTeamManager.mSelectedTeam.nationality = CreateTeamManager.mNewTeam.nationality;
		CreateTeamManager.mSelectedTeam.locationID = CreateTeamManager.mSelectedTeam.nationality.countryID;
		CreateTeamManager.mSelectedTeam.rivalTeam = CreateTeamManager.mNewTeam.rivalTeam;
		CreateTeamManager.mSelectedTeam.liveryID = CreateTeamManager.mNewTeam.liveryID;
		CreateTeamManager.mSelectedTeam.customLogo.styleID = CreateTeamManager.mNewTeam.customLogo.styleID;
		CreateTeamManager.mSelectedTeam.customLogo.teamFirstName = CreateTeamManager.teamFirstName;
		CreateTeamManager.mSelectedTeam.customLogo.teamLasttName = CreateTeamManager.teamLastName;
		CreateTeamManager.mSelectedTeam.investor = inInvestor;
		CreateTeamManager.mSelectedTeam.marketability = 0f;
		CreateTeamManager.mSelectedTeam.chairman.ResetHappiness();
		CreateTeamManager.mSelectedTeam.headquarters.nationality = CreateTeamManager.mSelectedTeam.nationality;
		CreateTeamManager.ResetTeam(CreateTeamManager.mSelectedTeam);
		CreateTeamManager.mSelectedTeam.investor.ApplyImpactsToTeam(CreateTeamManager.mSelectedTeam);
		CreateTeamManager.mSelectedTeam.isCreatedByPlayer = true;
		Game.instance.player.hasCreatedTeam = true;
		Game.instance.player.createdTeamID = CreateTeamManager.mSelectedTeam.teamID;
		App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Create_A_Team);
		Team result = CreateTeamManager.mSelectedTeam;
		CreateTeamManager.mNewTeam = null;
		CreateTeamManager.mSelectedTeam = null;
		CreateTeamManager.mTeamColor = null;
		CreateTeamManager.state = CreateTeamManager.State.Iddle;
		return result;
	}

	public static bool GetStep(CreateTeamManager.Step inStep)
	{
		return CreateTeamManager.mSteps.ContainsKey(inStep) && CreateTeamManager.mSteps[inStep];
	}

	public static void SetStep(CreateTeamManager.Step inStep, bool inValue)
	{
		if (CreateTeamManager.mSteps.ContainsKey(inStep))
		{
			CreateTeamManager.mSteps[inStep] = inValue;
		}
	}

	public static void Reset()
	{
		if (CreateTeamManager.state == CreateTeamManager.State.CreatingTeam)
		{
			CreateTeamManager.state = CreateTeamManager.State.Hold;
		}
	}

	public static void ResetSteps()
	{
		for (int i = 0; i < 4; i++)
		{
			CreateTeamManager.mSteps[(CreateTeamManager.Step)i] = false;
		}
	}

	private static void ResetTeam(Team inTeam)
	{
		CreateTeamManager.TeamDefaults teamDefaultsForChampionship = CreateTeamManager.GetTeamDefaultsForChampionship(inTeam.championship);
		List<Driver> allPeopleOnJob = inTeam.contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
		int count = allPeopleOnJob.Count;
		for (int i = 0; i < count; i++)
		{
			Driver driver = allPeopleOnJob[i];
			inTeam.contractManager.FireDriver(driver, Contract.ContractTerminationType.Generic);
			driver.careerHistory.RemoveHistory(driver.careerHistory.currentEntry);
		}
		count = CreateTeamManager.driverData[inTeam.championship.championshipID].Count;
		for (int j = 0; j < count; j++)
		{
			DatabaseEntry databaseEntry = CreateTeamManager.driverData[inTeam.championship.championshipID][j];
			databaseEntry.AddEntry("Team", inTeam.teamID + 2);
			Driver inDriver = Game.instance.driverManager.AddDriverToDatabase(databaseEntry);
			Game.instance.driverManager.LoadPersonalityTraits(inDriver, databaseEntry);
		}
		List<Person> allEmployees = inTeam.contractManager.GetAllEmployees();
		count = allEmployees.Count;
		for (int k = 0; k < count; k++)
		{
			Person person = allEmployees[k];
			if (person is Mechanic)
			{
				inTeam.contractManager.FirePerson(person, Contract.ContractTerminationType.Generic);
				inTeam.contractManager.HireReplacementMechanic();
				person.careerHistory.RemoveHistory(person.careerHistory.currentEntry);
			}
			else if (person is Engineer)
			{
				inTeam.contractManager.FirePerson(person, Contract.ContractTerminationType.Generic);
				inTeam.contractManager.HireReplacementEngineer();
				person.careerHistory.RemoveHistory(person.careerHistory.currentEntry);
			}
			else if (person is Assistant)
			{
				Assistant assistant = person as Assistant;
				inTeam.contractManager.FirePerson(assistant, Contract.ContractTerminationType.Generic);
				assistant.Retire();
				inTeam.contractManager.HireReplacementTeamAssistant(inTeam.nationality);
			}
			else if (person is Scout)
			{
				Scout scout = person as Scout;
				inTeam.contractManager.FirePerson(scout, Contract.ContractTerminationType.Generic);
				scout.Retire();
				inTeam.contractManager.HireReplacementScout(inTeam.nationality);
			}
		}
		inTeam.perksManager.Reset();
		inTeam.headquarters.ResetHeadquarters();
		inTeam.sponsorController.ClearAllSponsors();
		inTeam.sponsorController.ClearAllSponsorOffers();
		CarChassisStats carChassisStats = new CarChassisStats();
		carChassisStats.supplierEngine = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultEngineSupplier);
		carChassisStats.supplierBrakes = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultBrakesSupplier);
		carChassisStats.supplierFuel = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultFuelSupplier);
		carChassisStats.supplierMaterials = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultMaterialsSupplier);
		carChassisStats.supplierBattery = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultBatterySupplier);
		carChassisStats.supplierERSAdvanced = Game.instance.supplierManager.GetSupplierByID(teamDefaultsForChampionship.defaultAdvancedERSSupplier);
		carChassisStats.supplierEngine.RollRandomBaseStatModifier();
		carChassisStats.supplierBattery.RollRandomBaseStatModifier();
		carChassisStats.supplierERSAdvanced.RollRandomBaseStatModifier();
		carChassisStats.ApplyChampionshipBaseStat(inTeam.championship);
		carChassisStats.ApplySupplierStats();
		for (int l = 0; l < CarManager.carCount; l++)
		{
			inTeam.carManager.GetCar(l).ChassisStats = carChassisStats;
		}
		CreateTeamManager.ResetTeamCarParts(inTeam);
		inTeam.financeController.availableFunds = inTeam.investor.startingMoney;
		inTeam.startOfSeasonExpectedChampionshipResult = Game.instance.teamManager.CalculateExpectedPositionForChampionship(inTeam);
		inTeam.championship.standings.UpdateStandings();
	}

	private static void ResetTeamCarParts(Team inTeam)
	{
		CarManager carManager = inTeam.carManager;
		CarPart.PartType[] partType = CarPart.GetPartType(inTeam.championship.series, false);
		CreateTeamManager.TeamDefaults teamDefaultsForChampionship = CreateTeamManager.GetTeamDefaultsForChampionship(inTeam.championship);
		foreach (CarPart.PartType partType2 in partType)
		{
			if (!inTeam.championship.rules.specParts.Contains(partType2))
			{
				PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[inTeam.championship.championshipID][partType2];
				List<CarPart> partInventory = carManager.partInventory.GetPartInventory(partType2);
				for (int j = 0; j < partInventory.Count; j++)
				{
					CarPart carPart = partInventory[j];
					carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, (float)(partTypeSlotSettings.baseMinStat + RandomUtility.GetRandomInc(teamDefaultsForChampionship.defaultPartStatBonusMin, teamDefaultsForChampionship.defaultPartStatBonusMax)));
					carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, RandomUtility.GetRandom(teamDefaultsForChampionship.defaultPartReliabilityMin, teamDefaultsForChampionship.defaultPartReliabilityMax));
					carPart.stats.partCondition.redZone = GameStatsConstants.initialRedZone;
					carPart.stats.maxPerformance = (float)RandomUtility.GetRandomInc(teamDefaultsForChampionship.defaultPartMaxPerformanceMin, teamDefaultsForChampionship.defaultPartMaxPerformanceMax);
					carPart.stats.SetMaxReliability(GameStatsConstants.initialMaxReliabilityValue);
					carPart.buildDate = Game.instance.time.now.AddDays(-1.0);
				}
			}
		}
		carManager.AutofitBothCars();
		carManager.carPartDesign.SetSeasonStartingStats();
	}

	public static CreateTeamManager.TeamDefaults GetTeamDefaultsForChampionship(Championship inChampionship)
	{
		int count = CreateTeamManager.defaultTeamSettings.Count;
		for (int i = 0; i < count; i++)
		{
			CreateTeamManager.TeamDefaults teamDefaults = CreateTeamManager.defaultTeamSettings[i];
			if (teamDefaults.championship == inChampionship)
			{
				return teamDefaults;
			}
		}
		return null;
	}

	public static Team newTeam
	{
		get
		{
			return CreateTeamManager.mNewTeam;
		}
	}

	public static TeamColor newTeamColor
	{
		get
		{
			return CreateTeamManager.mTeamColor;
		}
	}

	public static bool isCreatingTeam
	{
		get
		{
			return CreateTeamManager.state == CreateTeamManager.State.CreatingTeam;
		}
	}

	public static Championship championship
	{
		get
		{
			return CreateTeamManager.mChampionship;
		}
	}

	public static Dictionary<CreateTeamManager.Step, bool> mSteps = new Dictionary<CreateTeamManager.Step, bool>();

	private static Team mNewTeam = null;

	private static Team mSelectedTeam = null;

	private static TeamColor mTeamColor = null;

	private static Championship mChampionship = null;

	private static List<Team> mTeams = new List<Team>();

	public static Dictionary<int, List<DatabaseEntry>> driverData = new Dictionary<int, List<DatabaseEntry>>();

	public static List<CreateTeamManager.TeamDefaults> defaultTeamSettings = new List<CreateTeamManager.TeamDefaults>();

	public static List<Person> defaultPersons = new List<Person>();

	public static CreateTeamManager.TeamDefaults defaultSettings = null;

	public static string teamFirstName = string.Empty;

	public static string teamLastName = string.Empty;

	public static CreateTeamManager.State state = CreateTeamManager.State.Iddle;

	public enum State
	{
		Iddle,
		CreatingTeam,
		Hold
	}

	public enum Step
	{
		PickTeamName,
		PickTeamLogo,
		PickTeamUniform,
		PickCarLivery,
		Count
	}

	public class TeamDefaults
	{
		public TeamDefaults()
		{
		}

		public Championship championship;

		public string defaultLastName = string.Empty;

		public int defaultEngineSupplier;

		public int defaultBrakesSupplier;

		public int defaultFuelSupplier;

		public int defaultMaterialsSupplier;

		public int defaultBatterySupplier;

		public int defaultAdvancedERSSupplier;

		public int defaultPartStatBonusMin;

		public int defaultPartStatBonusMax;

		public int defaultPartMaxPerformanceMin;

		public int defaultPartMaxPerformanceMax;

		public float defaultPartReliabilityMin;

		public float defaultPartReliabilityMax;

		public Color defaultPrimaryColor = Color.white;

		public Color defaultSecondaryColor = Color.white;

		public TeamColor.LiveryColour defaultLiveryColor = new TeamColor.LiveryColour();

		public int defaultTeamLogoStyle;

		public int defaultHatStyle;

		public int defaultShirtStyle;

		public int defaultCarLiveryID;

		public Nationality defaultTeamNationality;

		public int[] defaultCarSponsors = new int[6];
	}
}
