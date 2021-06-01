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
				string stringValue2 = databaseEntry.GetStringValue("BatteryType");
				supplier.advancedBatteryType = (Supplier.AdvancedERSBatteryType)((!string.IsNullOrEmpty(stringValue2)) ? ((int)Enum.Parse(typeof(Supplier.AdvancedERSBatteryType), stringValue2)) : 0);
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
				this.mSuppliers.Add(supplier);
			}
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

	public List<Supplier> GetSupplierList(Supplier.SupplierType inType)
	{
		switch (inType)
		{
		case Supplier.SupplierType.Engine:
			return this.engineSuppliers;
		case Supplier.SupplierType.Brakes:
			return this.brakesSuppliers;
		case Supplier.SupplierType.Fuel:
			return this.fuelSuppliers;
		case Supplier.SupplierType.Materials:
			return this.materialsSuppliers;
		case Supplier.SupplierType.Battery:
			return this.batterySuppliers;
		case Supplier.SupplierType.ERSAdvanced:
			return this.ersAdvancedSuppliers;
		default:
			return null;
		}
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
		int num = championshipID + 1;
		List<Supplier> list = new List<Supplier>();
		switch (inSupplierType)
		{
		case Supplier.SupplierType.Engine:
			list = this.engineSuppliers;
			break;
		case Supplier.SupplierType.Brakes:
			list = this.brakesSuppliers;
			break;
		case Supplier.SupplierType.Fuel:
			list = this.fuelSuppliers;
			break;
		case Supplier.SupplierType.Materials:
			list = this.materialsSuppliers;
			break;
		case Supplier.SupplierType.Battery:
			list = this.batterySuppliers;
			break;
		case Supplier.SupplierType.ERSAdvanced:
			list = this.ersAdvancedSuppliers;
			break;
		}
		List<Supplier> list2 = new List<Supplier>();
		for (int i = list.Count - 1; i >= 0; i--)
		{
			bool flag = true;
			if (checkCanBuy)
			{
				flag = list[i].CanTeamBuyThis(inTeam);
			}
			if (flag && list[i].tier == num)
			{
				list2.Add(list[i]);
			}
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

	public List<Supplier> engineSuppliers = new List<Supplier>();

	public List<Supplier> brakesSuppliers = new List<Supplier>();

	public List<Supplier> materialsSuppliers = new List<Supplier>();

	public List<Supplier> fuelSuppliers = new List<Supplier>();

	public List<Supplier> batterySuppliers = new List<Supplier>();

	public List<Supplier> ersAdvancedSuppliers = new List<Supplier>();

	private List<Supplier> mSuppliers = new List<Supplier>();
}
