using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class SupplierManager : InstanceCounter
{
	public SupplierManager()
	{
	}

	public void LoadFromDatabase(Database database)
	{
		global::Debug.Assert(this.engineSuppliers.Count == 0 && this.brakesSuppliers.Count == 0 && this.fuelSuppliers.Count == 0, "Loading from database when content is already loaded; this will work but indicates that the game is loading in a strange unintended way.");
		this.engineSuppliers.Clear();
		this.brakesSuppliers.Clear();
		this.materialsSuppliers.Clear();
		this.fuelSuppliers.Clear();
		this.batterySuppliers.Clear();
		this.ersAdvancedSuppliers.Clear();
		this.LoadSuppliers(database, new string[0]);
	}

	private void LoadSuppliers(Database database, params string[] inExcludedSupplierTypes)
	{
		List<string> list = new List<string>(inExcludedSupplierTypes);
		List<DatabaseEntry> suppliersData = database.suppliersData;
		for (int i = 0; i < suppliersData.Count; i++)
		{
			DatabaseEntry databaseEntry = suppliersData[i];
			string stringValue = databaseEntry.GetStringValue("Part Type");
			if (!list.Contains(stringValue))
			{
				int num = 1000000;
				Supplier supplier = new Supplier();
				supplier.id = databaseEntry.GetIntValue("ID");
				supplier.startYear = databaseEntry.GetIntValue("Start Year");
				supplier.endYear = databaseEntry.GetIntValue("End Year");
				supplier.model = databaseEntry.GetStringValue("Model");
				supplier.minRang = databaseEntry.GetIntValue("Min Rank");
				supplier.teamID = databaseEntry.GetIntValue("Team ID");
				supplier.logoIndex = databaseEntry.GetIntValue("Logo ID");
				supplier.name = databaseEntry.GetStringValue("Company Name");
				supplier.price = Mathf.RoundToInt(databaseEntry.GetFloatValue("Price") * (float)num);
				supplier.minEngineLevelModifier = databaseEntry.GetIntValue("Starting Level Min");
				supplier.maxEngineLevelModifier = databaseEntry.GetIntValue("Starting Level Max");
				supplier.minHarvestEfficiencyModifier = databaseEntry.GetFloatValue("Harvest Efficiency Min");
				supplier.maxHarvestEfficiencyModifier = databaseEntry.GetFloatValue("Harvest Efficiency Max");
				supplier.hybridGates = databaseEntry.GetIntValue("Hybrid Gates");
				supplier.powerGates = databaseEntry.GetIntValue("Power Gates");
				supplier.chargeSize = databaseEntry.GetIntValue("Charge Size");
				supplier.tier = databaseEntry.GetIntValue("Tier");
				supplier.descriptionID = databaseEntry.GetStringValue("Description");
				supplier.maxReliablity = databaseEntry.GetFloatValue("Max Reliability") / 100f;
				string stringValue2 = databaseEntry.GetStringValue("BatteryType");
				supplier.advancedBatteryType = (Supplier.AdvancedERSBatteryType)((!string.IsNullOrEmpty(stringValue2)) ? ((int)Enum.Parse(typeof(Supplier.AdvancedERSBatteryType), stringValue2)) : 0);
				supplier.numContracts = databaseEntry.GetIntValue("Contracts");
				this.LoadTeamDiscounts(supplier, databaseEntry);
				this.LoadTeamsThatCannotBuy(supplier, databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.TyreWear, "Tyre Wear", databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.FuelEfficiency, "Fuel Efficiency", databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.TyreHeating, "Tyre Heating", databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.Improvability, "Improveability", databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.StartingCharge, "Starting Charge", databaseEntry);
				this.AddStat(supplier, CarChassisStats.Stats.HarvestEfficiency, "Harvest Efficiency Min", databaseEntry);
				this.AddBoundaries(supplier, databaseEntry, true);
				this.AddBoundaries(supplier, databaseEntry, false);
				string text = stringValue;
				if (text != null)
				{
					switch (text)
					{
					case "Engine":
						supplier.supplierType = Supplier.SupplierType.Engine;
						this.engineSuppliers.Add(supplier);
						break;
					case "ECU":
						supplier.supplierType = Supplier.SupplierType.Brakes;
						this.brakesSuppliers.Add(supplier);
						break;
					case "Fuel":
						supplier.supplierType = Supplier.SupplierType.Fuel;
						this.fuelSuppliers.Add(supplier);
						break;
					case "ChassisMaterials":
						supplier.supplierType = Supplier.SupplierType.Materials;
						this.materialsSuppliers.Add(supplier);
						break;
					case "Battery":
						supplier.supplierType = Supplier.SupplierType.Battery;
						this.batterySuppliers.Add(supplier);
						break;
					case "ERS":
						supplier.supplierType = Supplier.SupplierType.ERSAdvanced;
						this.ersAdvancedSuppliers.Add(supplier);
						break;
					}
				}
				supplier.RollRandomBaseStatModifier2();
				this.mSuppliers.Add(supplier);
			}
		}
	}

	public void rollRandomBaseStatModifiers(int inSupplierTier) {
		for (int i = 0; i < this.mSuppliers.Count; i++)
		{
			if (this.mSuppliers[i].tier == inSupplierTier)
			{
				this.mSuppliers[i].RollRandomBaseStatModifier2();
			}
		}
	}

	public void ResetContractsForTier(int inSupplierTier) {
		for (int i = 0; i < this.mSuppliers.Count; i++) {
			if (mSuppliers[i].tier == inSupplierTier)
				mSuppliers[i].curContracts = 0;
		}
	}

	public Supplier GetSupplierByID(int inID)
	{
		for (int i = 0; i < this.mSuppliers.Count; i++)
		{
			if (this.mSuppliers[i].id == inID)
			{
				return this.mSuppliers[i];
			}
		}
		global::Debug.LogErrorFormat("No supplier of ID {0} in the database", new object[]
		{
			inID
		});
		return null;
	}

	// get the team chassi supplier
	private Supplier getChassiSupplierForTeam(Team inTeam) {
		List<Supplier> chassiSupplier = this.GetSupplierList(Supplier.SupplierType.Materials);
		for (int i = 0; i < chassiSupplier.Count; i++) {
			if (chassiSupplier[i].teamID == (inTeam.teamID + 2)) // Team IDs in Database files are 2 higher than in Team Class, whyever
				return chassiSupplier[i];
		}
		return null;
	}

	// initialize chassi development for next season
	// Team.championship.OnSeasonStart() <- call on game start and PreSeasonEnd
	public void InitializeChasiDevelopment(Team inTeam) {
		// get Team chassi supplier
		Supplier chassiSupplier = getChassiSupplierForTeam(inTeam);
		if (chassiSupplier == null) {
			global::Debug.LogErrorFormat("No ChassiSupplier for Team {0} found", new object[] { inTeam.GetShortName(false) });
			return;
		}
		// initialize development boni
		chassiSupplier.chassiDevelopmentEngineerBonus = 0f;
		chassiSupplier.chassiDevelopmentTestDriverBonus = 0f;
		chassiSupplier.chassiDevelopmentInvestedMoney = 0f;
		chassiSupplier.chassiDevelopmentLastUpdate = Game.instance.time.now;
	}

	// update mid season chassi development
	// called by Team.Update()
	public void UpdateChassiContribution(Team inTeam) {
		DateTime now = Game.instance.time.now;
		DateTime seasonStart = inTeam.championship.calendar[0].eventDate.AddDays(-11.0);
		DateTime seasonEnd = inTeam.championship.currentSeasonEndDate;

		// if not in season -> do nothing
		if (now < seasonStart || now > seasonEnd) {
			return;
		}

		// get Team chassi supplier
		Supplier chassiSupplier = getChassiSupplierForTeam(inTeam);
		if (chassiSupplier == null) {
			global::Debug.LogErrorFormat("No ChassiSupplier for Team {0} found", new object[] { inTeam.GetShortName(false) });
			return;
		}

		if (chassiSupplier.chassiDevelopmentLastUpdate < seasonStart)
			chassiSupplier.chassiDevelopmentLastUpdate = seasonStart;

		// check if last update was at least 1 day ago
		float daysSinceLastUpdate = (now - chassiSupplier.chassiDevelopmentLastUpdate).Days;
		if (daysSinceLastUpdate <= 0) {
			return;
		}

		// calculate development time for this season
		float seasonDays = (seasonEnd - seasonStart).Days;
		float devTime = daysSinceLastUpdate / seasonDays;

		// Calculate Engineer Bonus
		Engineer engineer = inTeam.contractManager.GetPersonOnJob<Engineer>(Contract.Job.EngineerLead);
		if (engineer != null) {
			float engineerBonus = engineer.stats.GetTotal() / 6 * devTime;
			chassiSupplier.chassiDevelopmentEngineerBonus += engineerBonus;
		}
		// Test Driver - Feedback
		Driver testDriver = inTeam.GetReserveDriver();
		if (testDriver != null) {
			float testDriverBonus = testDriver.GetDriverStats().feedback * devTime;
			chassiSupplier.chassiDevelopmentTestDriverBonus += testDriverBonus;
		}

		chassiSupplier.chassiDevelopmentLastUpdate = Game.instance.time.now;
	}

	private void distributePointsToChassiSupplier(Supplier inSupplier, float inPoints) {
		// 25% for each attribute, but at least 1
		float tyreWear    = Convert.ToSingle(Math.Min(Math.Round(inPoints / 4f, MidpointRounding.AwayFromZero), 1f));
		float tyreHeating = Convert.ToSingle(Math.Min(Math.Round(inPoints / 4f, MidpointRounding.AwayFromZero), 1f));

		// reduce remaining points for distributed values
		inPoints = inPoints - tyreWear - tyreHeating;

		// randomly assing remaining points for first attribute
		float addPointsForWear = RandomUtility.GetRandom(0f,inPoints);
		float addPointsForHeating = inPoints - addPointsForWear;

		// cap tyreWear to 10 points
		if (tyreWear + addPointsForWear > 10f) {
			addPointsForHeating += (tyreWear + addPointsForWear) - 10f;
			addPointsForWear = 10f - tyreWear;
		}

		// cap tyreHeating to 10 points
		if (tyreHeating + addPointsForHeating > 10f) {
			addPointsForWear += (tyreHeating + addPointsForHeating) - 10f;
			addPointsForHeating = 10f - tyreHeating;
		}

		// assing remaining points for second attribute
		tyreWear += addPointsForWear;
		tyreHeating += addPointsForHeating;

		// update chassi suppliert values
		inSupplier.supplierStats.Remove(CarChassisStats.Stats.TyreWear);
		inSupplier.supplierStats.Remove(CarChassisStats.Stats.TyreHeating);
		inSupplier.supplierStats.Add(CarChassisStats.Stats.TyreWear, tyreWear);
		inSupplier.supplierStats.Add(CarChassisStats.Stats.TyreHeating, tyreHeating);
	}

	// calculate points for default chassi for next season
	private void UpdateDefaultChassisOnSeasonEnd() {
		List<Supplier> defaultChassiSupplier = new List<Supplier>();

		defaultChassiSupplier.Add(this.GetSupplierByID(319)); // Lola
		defaultChassiSupplier.Add(this.GetSupplierByID(320)); // dallara
		defaultChassiSupplier.Add(this.GetSupplierByID(321)); // reynard

		Supplier top = defaultChassiSupplier[RandomUtility.GetRandom(0,2)];
		defaultChassiSupplier.Remove(top);

		Supplier mid = defaultChassiSupplier[RandomUtility.GetRandom(0,1)];
		defaultChassiSupplier.Remove(mid);

		Supplier bottom = defaultChassiSupplier[0];

		top.price    = 7000000;
		mid.price    = 5000000;
		bottom.price = 3000000;

		this.distributePointsToChassiSupplier(top, 8f);
		this.distributePointsToChassiSupplier(mid, 5f);
		this.distributePointsToChassiSupplier(bottom, 3f);
	}

	// calculate team chassis
	// called by Championship.OnSeasonEnd()
	public void UpdateTeamChassi(ChampionshipStandings teamStandings) {
		// create default chassis for next season
		Game.instance.supplierManager.UpdateDefaultChassisOnSeasonEnd();

		// calculate team chassis
		List<Team> list = teamStandings.GetTeamList();
		foreach(Team team in list) {
			Game.instance.supplierManager.UpdateChassiContributionOnSeasonEnd(team);
		}

		// check if/which AI has no team chassi -> has to select default chassi
		if (RandomUtility.GetRandom01() <= SupplierManager.AI_NO_TEAMCHASSI_CHANCE) {
			Team teamWithNowOwnChassi;
			if (RandomUtility.GetRandom01() > 0.5f)
				teamWithNowOwnChassi = teamStandings.GetTeamList()[teamStandings.GetTeamList().Count - 1];
			else
				teamWithNowOwnChassi = teamStandings.GetTeamList()[teamStandings.GetTeamList().Count - 2];
			if (teamWithNowOwnChassi != null && !teamWithNowOwnChassi.IsPlayersTeam()) {
				this.getChassiSupplierForTeam(teamWithNowOwnChassi).chassiIsAvailable = false;
			}
		}
	}

	// Update invested money for car development
	private void UpdateChassiContributionOnSeasonEnd(Team inTeam) {
		// get Team chassi supplier
		Supplier chassiSupplier = this.getChassiSupplierForTeam(inTeam);
		if (chassiSupplier == null) {
			global::Debug.LogErrorFormat("No ChassiSupplier for Team {0} found", new object[] { inTeam.GetShortName(false) });
			return;
		}

		// mark team chassi initially as available
		chassiSupplier.chassiIsAvailable = true;

		// calculate chassi points from investment (everything above 10Mio, up to 10Mio total = 20 Points)
		float investment = (float)inTeam.financeController.moneyForCarDev;
		chassiSupplier.chassiDevelopmentInvestedMoney = (investment - 10000000f) / 10000000f * 20f;

		// cost for team chassi is always 2 Mio (materials and production)
		chassiSupplier.price = 2000000;

		// calculate total points for team chassi
		float teamChassiPoints = 0f;
		teamChassiPoints += chassiSupplier.chassiDevelopmentEngineerBonus * 0.5f;
		teamChassiPoints += chassiSupplier.chassiDevelopmentTestDriverBonus * 0.15f;
		teamChassiPoints += chassiSupplier.chassiDevelopmentInvestedMoney * 0.35f;

		this.distributePointsToChassiSupplier(chassiSupplier, teamChassiPoints);

		if (inTeam.IsPlayersTeam()) {
			// Player Team needs at least 12.000.000 Investment for Team Chassi to be available
			if (inTeam.financeController.moneyForCarDev < 12000000L)
				chassiSupplier.chassiIsAvailable = false;
			// For Player everything above 10 Mio is used up for development (yes, player only, AI gets everything)
			inTeam.financeController.moneyForCarDev = 10000000L;
		}
	}

	public List<Supplier> GetSupplierList(Supplier.SupplierType inType)
	{

		int selectionYear = Game.instance.time.now.Year + 1; // select supplier in december of year before next season
		List<Supplier> typeSupplier;
		List<Supplier> returnSupplier = new List<Supplier>();

		switch (inType)
		{
		case Supplier.SupplierType.Engine:
			typeSupplier = this.engineSuppliers;
			break;
		case Supplier.SupplierType.Brakes:
			typeSupplier = this.brakesSuppliers;
			break;
		case Supplier.SupplierType.Fuel:
			typeSupplier = this.fuelSuppliers;
			break;
		case Supplier.SupplierType.Materials:
			typeSupplier = this.materialsSuppliers;
			break;
		case Supplier.SupplierType.Battery:
			typeSupplier = this.batterySuppliers;
			break;
		case Supplier.SupplierType.ERSAdvanced:
			typeSupplier = this.ersAdvancedSuppliers;
			break;
		default:
			return null;
		}

		for (int i = 0; i < typeSupplier.Count; i++) {
			if ((typeSupplier[i].startYear <= selectionYear) && (selectionYear <= typeSupplier[i].endYear))
				returnSupplier.Add(typeSupplier[i]);
		}

		return returnSupplier;
	}

	private void AddBoundaries(Supplier inSupplier, DatabaseEntry data, bool inIsMinBoundary)
	{
		string text = (!inIsMinBoundary) ? data.GetStringValue("Max Boundary") : data.GetStringValue("Min Boundary");
		string[] array = text.Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
			string[] array2 = array[i].Split(new char[]
			{
				'='
			});
			if (array2.Length >= 2)
			{
				array2[0] = array2[0].Trim();
				array2[1] = array2[1].Trim();
				Supplier.CarAspect carAspect = Supplier.CarAspect.NoseHeight;
				string text2 = array2[0];
				if (text2 != null)
				{
					if (text2 == "RearPackage")
						carAspect = Supplier.CarAspect.RearPackage;
					else if (text2 == "NoseHeight")
						carAspect = Supplier.CarAspect.NoseHeight;
				}
				if (inIsMinBoundary)
				{
					inSupplier.carAspectMinBoundary.Add(carAspect, float.Parse(array2[1]));
				}
				else
				{
					inSupplier.carAspectMaxBoundary.Add(carAspect, float.Parse(array2[1]));
				}
			}
		}
	}

	private void AddStat(Supplier inSupplier, CarChassisStats.Stats inStat, string inStatName, DatabaseEntry data)
	{
		if (data.GetStringValue(inStatName) != "NA")
		{
			inSupplier.supplierStats.Add(inStat, data.GetFloatValue(inStatName));
		}
	}

	private void LoadTeamDiscounts(Supplier inSupplier, DatabaseEntry inData)
	{
		string stringValue = inData.GetStringValue("Team Discount");
		string stringValue2 = inData.GetStringValue("Discount Amount");
		string[] array = stringValue.Split(new char[]
		{
			';'
		});
		string[] array2 = stringValue2.Split(new char[]
		{
			';'
		});
		GameUtility.Assert(array.Length == array2.Length, "SupplierManager needs same number of Team Discount entries and Discount Amount in database Part Suppliers", null);
		for (int i = 0; i < array.Length; i++)
		{
			int num;
			if (int.TryParse(array[i], out num))
			{
				if (num >= 2)
				{
					int num2;
					if (int.TryParse(array2[i], out num2))
					{
						if (num2 != 0)
						{
							inSupplier.AddTeamDiscount(num - 2, (float)num2);
						}
					}
				}
			}
		}
	}

	private void LoadTeamsThatCannotBuy(Supplier inSupplier, DatabaseEntry inData)
	{
		string stringValue = inData.GetStringValue("Teams That Cannot Buy This");
		string[] array = stringValue.Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			int num;
			if (int.TryParse(array[i], out num))
			{
				if (num >= 2)
				{
					inSupplier.AddTeamsThatCannotBuy(num - 2);
				}
			}
		}
	}

	public List<Supplier> GetSuppliersForTeam(Supplier.SupplierType inSupplierType, Team inTeam, bool checkCanBuy)
	{
		int championshipID = inTeam.championship.championshipID;
		int tier = championshipID + 1;
		List<Supplier> list = this.GetSupplierList(inSupplierType);

		List<Supplier> list2 = new List<Supplier>();
		for (int i = list.Count - 1; i >= 0; i--)
		{
			bool flag = true;
			// check if team is allowed to buy from this supplier
			if (checkCanBuy && !list[i].CanTeamBuyThis(inTeam))
				flag = false;
			// for chassi, filter non availble team chassi for AI (player should at least see it, even if not available to select)
			if (!inTeam.IsPlayersTeam() && !list[i].chassiIsAvailable)
				flag = false;
			// for AI hide suppliers with already max number of contracts
			if (!inTeam.IsPlayersTeam() && list[i].curContracts >= list[i].numContracts)
				flag = false;
			// for engines in tier 1 check if team rang ist high enough for this supplier
			int teamLastRank = inTeam.GetChampionshipRang();
			if (inSupplierType == Supplier.SupplierType.Engine && list[i].tier == 1 && list[i].minRang != 0 && list[i].minRang < teamLastRank)
				flag = false;

			if (flag && list[i].tier == tier)
				list2.Add(list[i]);
		}
		return list2;
	}

	public void OnLoad()
	{
		this.SetTierToList(this.engineSuppliers);
		this.SetTierToList(this.fuelSuppliers);
		this.SetTierToList(this.brakesSuppliers);
		this.SetTierToList(this.materialsSuppliers);
		if (this.batterySuppliers.Count == 0)
		{
			this.LoadSuppliers(App.instance.database, new string[]
			{
				"ChassisMaterials",
				"Fuel",
				"Engine",
				"ECU",
				"ERS"
			});
		}
		if (this.ersAdvancedSuppliers.Count <= 10)
		{
			this.ersAdvancedSuppliers.Clear();
			this.LoadSuppliers(App.instance.database, new string[]
			{
				"ChassisMaterials",
				"Fuel",
				"Engine",
				"ECU",
				"Battery"
			});
		}
	}

	private void SetTierToList(List<Supplier> inSupplierList)
	{
		for (int i = 0; i < inSupplierList.Count; i++)
		{
			if (inSupplierList[i].tier == 0)
			{
				int num = i % 3 + 1;
				inSupplierList[i].tier = num;
				inSupplierList[i].price = 1000000 * (4 - num);
			}
		}
	}

	private static readonly float AI_NO_TEAMCHASSI_CHANCE = 0.5f;

	private List<Supplier> engineSuppliers = new List<Supplier>();

	private List<Supplier> brakesSuppliers = new List<Supplier>();

	private List<Supplier> materialsSuppliers = new List<Supplier>();

	private List<Supplier> fuelSuppliers = new List<Supplier>();

	private List<Supplier> batterySuppliers = new List<Supplier>();

	private List<Supplier> ersAdvancedSuppliers = new List<Supplier>();

	private List<Supplier> mSuppliers = new List<Supplier>();
}
