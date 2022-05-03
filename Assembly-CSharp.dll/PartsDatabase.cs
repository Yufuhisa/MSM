using System;
using System.Collections.Generic;
using UnityEngine;

public class PartsDatabase
{
	public PartsDatabase()
	{
	}

	public static void SetCarPartsFromDatabase(Database database)
	{
		List<DatabaseEntry> carPartsData = database.carPartsData;
		TeamManager teamManager = Game.instance.teamManager;
		for (int i = 0; i < carPartsData.Count; i++)
		{
			DatabaseEntry databaseEntry = carPartsData[i];
			string[] array = databaseEntry.GetStringValue("Team").Split(new char[]
			{
				';'
			});
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = array[j].Trim();
				if (!(array[j] == string.Empty))
				{
					Team entity = teamManager.GetEntity(int.Parse(array[j]) - 2);
					CarManager carManager = entity.carManager;
					CarPart.PartType[] partType = CarPart.GetPartType(entity.championship.series, false);
					CarPart.PartType partType2 = PartsDatabase.GetPartType(databaseEntry.GetStringValue("Part Type"));
					bool flag = true;
					for (int k = 0; k < partType.Length; k++)
					{
						if (partType[k] == partType2)
						{
							flag = false;
						}
					}
					if (!flag)
					{
						if (!entity.championship.rules.specParts.Contains(partType2))
						{
							PartsDatabase.LoadPartData(carManager, partType2, entity.championship, databaseEntry);
						}
					}
				}
			}
		}
		for (int l = 0; l < teamManager.count; l++)
		{
			Team entity2 = teamManager.GetEntity(l);
			entity2.carManager.AutofitBothCars();
			entity2.carManager.carPartDesign.SetSeasonStartingStats();
		}
	}

	private static void LoadPartData(CarManager inCarManager, CarPart.PartType inType, Championship inChampionship, DatabaseEntry inData)
	{
		CarPart carPart = CarPart.CreatePartEntity(inType, inCarManager.team.championship);
		// initialize basic part stats
		inCarManager.SetPartBasicStats(carPart);

		float inValue = 0f;
		switch (carPart.stats.statType)
		{
		case CarStats.StatType.TopSpeed:
			inValue = inData.GetFloatValue("TS");
			break;
		case CarStats.StatType.Acceleration:
			inValue = inData.GetFloatValue("ACC");
			break;
		case CarStats.StatType.Braking:
			inValue = inData.GetFloatValue("DEC");
			break;
		case CarStats.StatType.LowSpeedCorners:
			inValue = inData.GetFloatValue("LSC");
			break;
		case CarStats.StatType.MediumSpeedCorners:
			inValue = inData.GetFloatValue("MSC");
			break;
		case CarStats.StatType.HighSpeedCorners:
			inValue = inData.GetFloatValue("HSC");
			break;
		}

		// override performance and reliability for engines with supplier values
		if (inType == CarPart.PartType.Engine) {
			int performanceEnigne = inCarManager.GetCar(0).ChassisStats.supplierEngine.randomEngineLevelModifier;
			int performanceModFuel = inCarManager.GetCar(0).ChassisStats.supplierFuel.randomEngineLevelModifier;
			int statWithPerformance = (performanceEnigne + performanceModFuel);
			carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, statWithPerformance);

			float maxReliablityEnigne = inCarManager.GetCar(0).ChassisStats.supplierEngine.maxReliablity;
			float maxReliablityModFuel = inCarManager.GetCar(0).ChassisStats.supplierFuel.maxReliablity;
			carPart.stats.SetMaxReliability(maxReliablityEnigne + maxReliablityModFuel);
			// initial engine parts have already max reliability
			carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, maxReliablityEnigne + maxReliablityModFuel);
		}
		else {
			carPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, inValue);
			carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, inData.GetFloatValue("Reliability") / 100f);
			carPart.stats.SetMaxReliability(inData.GetFloatValue("Max Reliability") / 100f);
		}

		carPart.PostStatsSetup(inChampionship);
		inCarManager.partInventory.AddPart(carPart);
	}

	public static CarPart.PartType GetPartType(string inString)
	{
		if (inString != null)
		{
			switch (inString)
			{
				case "Engine":
					return CarPart.PartType.Engine;
				case "Gearbox":
					return CarPart.PartType.Gearbox;
				case "Brakes":
					return CarPart.PartType.Brakes;
				case "Suspension":
					return CarPart.PartType.Suspension;
				case "Front Wing":
					return CarPart.PartType.FrontWing;
				case "Rear Wing":
					return CarPart.PartType.RearWing;
				case "Rear Wing GT":
					return CarPart.PartType.RearWingGT;
				case "Engine GT":
					return CarPart.PartType.EngineGT;
				case "Brakes GT":
					return CarPart.PartType.BrakesGT;
				case "Gearbox GT":
					return CarPart.PartType.GearboxGT;
				case "Suspension GT":
					return CarPart.PartType.SuspensionGT;
				case "EngineGET":
					return CarPart.PartType.EngineGET;
				case "GearboxGET":
					return CarPart.PartType.GearboxGET;
				case "BrakesGET":
					return CarPart.PartType.BrakesGET;
				case "SuspensionGET":
					return CarPart.PartType.SuspensionGET;
				case "Front WingGET":
					return CarPart.PartType.FrontWingGET;
				case "Rear WingGET":
					return CarPart.PartType.RearWingGET;
			}
		}
		return CarPart.PartType.None;
	}

	private static List<CarPartComponent> GetComponentsFromIDs(string inData)
	{
		List<CarPartComponent> list = new List<CarPartComponent>();
		string[] array = inData.Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = array[i].Trim();
			if (!(array[i] == string.Empty))
			{
				int num = int.Parse(array[i]);
				if (App.instance.componentsManager.components.ContainsKey(num))
				{
					list.Add(App.instance.componentsManager.components[num]);
				}
			}
		}
		return list;
	}
}
