using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class TeamAIController
{
	public TeamAIController()
	{
		this.mScoutingManager = new AIScoutingManager();
		this.mNegotiations = new List<TeamAIController.NegotiationEntry>();
		this.mLastDriverScoutTime = default(DateTime);
		this.mLastEngineerScoutTime = default(DateTime);
		this.mLastMechanicScoutTime = default(DateTime);
		this.mLastHQUpdateTime = default(DateTime);
		this.mLastCarUpdateTime = default(DateTime);
		this.mLastFiringUpdateTime = default(DateTime);
		this.mDrivers = new List<Driver>();
		this.mDriversTeamHasAttemptedToRenewContractWith = new List<Driver>();
		this.mPeopleApproachedAndRejectedBy = new List<Person>();
		this.mImproveCarPartsList = new List<int>();
		this.mImproveCarPartsListOther = new List<int>();
		this.mImproveCarPartsMostRecentParts = new List<CarPart>();
		this.mHQTargetsList = new List<HQsBuildingInfo.Type>();
		this.mHQHistoryList = new List<HQsBuildingInfo.Type>();
		this.mPotentialHQTargets = new List<TeamAIController.HQBuildingValue>();
	}

	public void Start(Team inTeam)
	{
		this.mTeam = inTeam;
		this.mScoutingManager.SetTeam(this.mTeam);
		this.mLastDriverScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-15, 0));
		this.mLastEngineerScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-15, 0));
		this.mLastMechanicScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-15, 0));
		this.mLastCarUpdateTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-7, 0));
		this.mLastHQUpdateTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-7, 0));
		this.mLastFiringUpdateTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-15, 0));
	}

	public void OnDayEnd()
	{
		this.mTeam.financeController.HandleUnallocatedTransactions();
		if (this.mTeam.championship != null)
		{
			this.EvaluateSponsorOffers();
			if (!this.mTeam.championship.InPreseason())
			{
				this.HandleCarUpgrades();
			}
			this.HandleStaffMovement();
			this.HandleHQUpgrades();
		}
	}

	public void HandleEndOfSeason()
	{
		ChampionshipEntry_v1 championshipEntry = this.mTeam.GetChampionshipEntry();
		if (championshipEntry != null)
		{
			this.expectedEndOfSeasonPosition = this.mTeam.startOfSeasonExpectedChampionshipResult;
			this.endOfSeasonPosition = championshipEntry.GetCurrentChampionshipPosition();
		}
	}

	public void OnPreSeasonStart()
	{
		this.HandleCarNewChassis();
	}

	public void OnPreSeasonEnd()
	{
		this.mDriversTeamHasAttemptedToRenewContractWith.Clear();
		this.EvaluateDriverLineUp();
	}

	public void OnRaceEnd()
	{
	}

	public void SetupTeamForEvent()
	{
		this.SelectDriversForEvent();
		this.SelectSponsorForEvent();
		this.SelectTyresForEvent();
		this.SetupCarForEvent();
	}

	public void SelectDriversForEvent()
	{
		this.mTeam.SelectMainDriversForSession();
	}

	public void SelectTyresForEvent()
	{
	}

	public void SetupCarForEvent()
	{
		this.FitPartsOnCars();
	}

	public AIScoutingManager scoutingManager
	{
		get
		{
			return this.mScoutingManager;
		}
	}

	public List<int> improveCarPartsList
	{
		get
		{
			return this.mImproveCarPartsList;
		}
	}

	public List<int> MImproveCarPartsListOther
	{
		get
		{
			return this.mImproveCarPartsListOther;
		}
	}

	public List<CarPart> MImproveCarPartsMostRecentParts
	{
		get
		{
			return this.mImproveCarPartsMostRecentParts;
		}
	}

	public void HandleCarNewChassis()
	{
		if (this.mTeam.carManager.nextYearCarDesign.state != NextYearCarDesign.State.WaitingForDesign)
		{
			return;
		}
		this.GetCarDevMoney();
		CarChassisStats carChassisStats = this.FindSuppliersForNewChassis();
		Finance finance = this.mTeam.financeController.finance;
		Transaction brakesTransaction = carChassisStats.GetBrakesTransaction(this.mTeam);
		Transaction engineTransaction = carChassisStats.GetEngineTransaction(this.mTeam);
		Transaction fuelTransaction = carChassisStats.GetFuelTransaction(this.mTeam);
		Transaction materialTransaction = carChassisStats.GetMaterialTransaction(this.mTeam);
		finance.ProcessTransactions(null, null, false, new Transaction[]
		{
			brakesTransaction,
			engineTransaction,
			fuelTransaction,
			materialTransaction
		});
		this.mTeam.carManager.nextYearCarDesign.StartDesign(carChassisStats);
	}

	public void SetWeightStrippingForEvent(RaceEventDetails inDetails)
	{
		if (!this.mTeam.championship.rules.isWeightStrippingEnabled || this.mTeam.championship.rules.weightStrippingRatio <= 0f)
		{
			return;
		}
		bool flag = this.mTeam.aggression > 0.5f;
		bool flag2 = this.mTeam.championship == Game.instance.championshipManager.GetMainChampionship(this.mTeam.championship.series);
		for (int i = 0; i < CarManager.carCount; i++)
		{
			Car car = this.mTeam.carManager.GetCar(i);
			for (int j = 0; j < car.seriesCurrentParts.Length; j++)
			{
				CarPart carPart = car.seriesCurrentParts[j];
				if (!carPart.isSpecPart)
				{
					CarStats.RelevantToCircuit relevancy = inDetails.circuit.GetRelevancy(CarPart.GetStatForPartType(carPart.GetPartType()));
					float num = 0f;
					switch (relevancy)
					{
					case CarStats.RelevantToCircuit.No:
						num = 0.7f;
						break;
					case CarStats.RelevantToCircuit.Useful:
						num = 0.75f;
						break;
					case CarStats.RelevantToCircuit.VeryUseful:
						num = 0.8f;
						break;
					case CarStats.RelevantToCircuit.VeryImportant:
						num = 0.85f;
						break;
					}
					if (flag)
					{
						num -= 0.05f;
					}
					if (!flag2 || this.mTeam.championship.series == Championship.Series.EnduranceSeries)
					{
						num += 0.1f;
					}
					carPart.stats.SetWeightStripping(Mathf.Max(0f, carPart.reliability - num) * 100f, SessionDetails.SessionType.Practice);
				}
			}
		}
	}

	private void GetCarDevMoney()
	{
		Transaction transaction = new Transaction(Transaction.Group.CarParts, Transaction.Type.Credit, this.mTeam.financeController.moneyForCarDev, Localisation.LocaliseID("PSG_10010580", null));
		this.mTeam.financeController.moneyForCarDev = 0L;
		this.mTeam.financeController.finance.ProcessTransactions(null, null, false, new Transaction[]
		{
			transaction
		});
		this.mTeam.financeController.HandleUnallocatedTransactions();
	}

	public CarChassisStats FindSuppliersForNewChassis()
	{
		List<Supplier> list = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.Engine, this.mTeam, true));
		List<Supplier> list2 = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.Brakes, this.mTeam, true));
		List<Supplier> list3 = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.Materials, this.mTeam, true));
		List<Supplier> list4 = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.Fuel, this.mTeam, true));
		List<Supplier> list5 = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.Battery, this.mTeam, true));
		List<Supplier> list6 = new List<Supplier>(Game.instance.supplierManager.GetSuppliersForTeam(Supplier.SupplierType.ERSAdvanced, this.mTeam, true));
		for (int i = 0; i < list.Count; i++)
		{
			list[i].RollRandomBaseStatModifier();
		}
		for (int j = 0; j < list5.Count; j++)
		{
			list5[j].RollRandomBaseStatModifier();
		}
		for (int k = 0; k < list6.Count; k++)
		{
			list6[k].RollRandomBaseStatModifier();
		}
		if (this.mTeam.championship.rules.isERSAdvancedModeActive)
		{
			return this.ChooseSuppliers(new List<Supplier>[]
			{
				list,
				list2,
				list3,
				list4,
				list6
			});
		}
		if (this.mTeam.championship.rules.isEnergySystemActive && !this.mTeam.championship.rules.shouldChargeUsingStandingsPosition)
		{
			return this.ChooseSuppliers(new List<Supplier>[]
			{
				list,
				list2,
				list3,
				list4,
				list5
			});
		}
		return this.ChooseSuppliers(new List<Supplier>[]
		{
			list,
			list2,
			list3,
			list4
		});
	}

	private CarChassisStats ChooseSuppliers(params List<Supplier>[] inSuppliers)
	{
		long num = this.mTeam.financeController.finance.currentBudget - this.mTeam.financeController.GetRacePaymentValue(TeamFinanceController.RacePaymentType.Medium) * 2L;
		foreach (List<Supplier> list in inSuppliers)
		{
			list.Sort((Supplier x, Supplier y) => x.GetQuality().CompareTo(y.GetQuality()));
		}
		long num2 = 0L;
		int num3 = -1;
		Supplier[] array = new Supplier[inSuppliers.Length];
		bool flag = false;
		while (num2 <= num)
		{
			num3++;
			int[] array2 = new int[inSuppliers.Length];
			long num4 = 0L;
			for (int j = 0; j < inSuppliers.Length; j++)
			{
				array2[j] = Math.Min(num3, inSuppliers[j].Count - 1);
				num4 += (long)inSuppliers[j][array2[j]].GetPrice(this.mTeam);
			}
			if (num4 >= num && num3 > 0)
			{
				break;
			}
			num2 = 0L;
			bool flag2 = true;
			for (int k = 0; k < array.Length; k++)
			{
				array[k] = inSuppliers[k][array2[k]];
				num2 += (long)array[k].GetPrice(this.mTeam);
				if (array2[k] != inSuppliers[k].Count - 1)
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (num3 < 0)
			{
				num3 = 0;
				for (int l = 0; l < array.Length; l++)
				{
					array[l] = inSuppliers[l][num3];
				}
			}
			else
			{
				for (int m = 0; m < inSuppliers.Length; m++)
				{
					if (num3 < inSuppliers[m].Count - 1)
					{
						int num5 = inSuppliers[m][num3 + 1].GetPrice(this.mTeam) - array[m].GetPrice(this.mTeam);
						if (num2 + (long)num5 < num)
						{
							array[m] = inSuppliers[m][num3 + 1];
							num2 += (long)num5;
						}
					}
				}
			}
		}
		CarChassisStats carChassisStats = new CarChassisStats();
		for (int n = 0; n < array.Length; n++)
		{
			switch (array[n].supplierType)
			{
			case Supplier.SupplierType.Engine:
				carChassisStats.supplierEngine = array[n];
				break;
			case Supplier.SupplierType.Brakes:
				carChassisStats.supplierBrakes = array[n];
				break;
			case Supplier.SupplierType.Fuel:
				carChassisStats.supplierFuel = array[n];
				break;
			case Supplier.SupplierType.Materials:
				carChassisStats.supplierMaterials = array[n];
				break;
			case Supplier.SupplierType.Battery:
				carChassisStats.supplierBattery = array[n];
				break;
			case Supplier.SupplierType.ERSAdvanced:
				carChassisStats.supplierERSAdvanced = array[n];
				break;
			}
		}
		carChassisStats.ApplyChampionshipBaseStat(this.mTeam.championship);
		carChassisStats.ApplySupplierStats();
		int randomInc = RandomUtility.GetRandomInc(1, 2);
		List<CarChassisStats.Stats> list2 = new List<CarChassisStats.Stats>();
		list2.Add(CarChassisStats.Stats.TyreWear);
		list2.Add(CarChassisStats.Stats.TyreHeating);
		list2.Add(CarChassisStats.Stats.FuelEfficiency);
		list2.Add(CarChassisStats.Stats.Improvability);
		for (int num6 = 0; num6 < randomInc; num6++)
		{
			CarChassisStats.Stats item = list2[RandomUtility.GetRandom(0, list2.Count)];
			list2.Remove(item);
			switch (item)
			{
			case CarChassisStats.Stats.TyreWear:
				carChassisStats.tyreWear += 4f;
				Mathf.Clamp(carChassisStats.tyreWear, 0f, GameStatsConstants.chassisStatMax);
				break;
			case CarChassisStats.Stats.TyreHeating:
				carChassisStats.tyreHeating += 4f;
				Mathf.Clamp(carChassisStats.tyreHeating, 0f, GameStatsConstants.chassisStatMax);
				break;
			case CarChassisStats.Stats.FuelEfficiency:
				carChassisStats.fuelEfficiency += 4f;
				Mathf.Clamp(carChassisStats.fuelEfficiency, 0f, GameStatsConstants.chassisStatMax);
				break;
			case CarChassisStats.Stats.Improvability:
				carChassisStats.improvability += 4f;
				Mathf.Clamp(carChassisStats.improvability, 0f, GameStatsConstants.chassisStatMax);
				break;
			}
		}
		return carChassisStats;
	}

	public void HandleCarUpgrades()
	{
		float unitAverage = this.mTeam.teamPrincipal.stats.GetUnitAverage();
		int num = (int)((1f - unitAverage) * 7f) + RandomUtility.GetRandom(0, 3);
		if (App.instance.preferencesManager.gamePreferences.GetAIDevDifficulty() == PrefGameAIDevDifficulty.Type.Slowed)
		{
		}
		CarManager carManager = this.mTeam.carManager;
		if ((Game.instance.time.now - this.mLastCarUpdateTime).Days < num)
		{
			this.ImproveCarParts(carManager);
			return;
		}
		long num2 = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceCar);
		if (num2 <= 0L)
		{
			this.ImproveCarParts(carManager);
			return;
		}
		TeamAIWeightings aiWeightings = this.mTeam.aiWeightings;
		Dictionary<CarStats.StatType, TeamAIController.CarUpgradeStatValues> dictionary = new Dictionary<CarStats.StatType, TeamAIController.CarUpgradeStatValues>();
		List<CarPart.PartType> list = new List<CarPart.PartType>(CarPart.GetPartType(this.mTeam.championship.series, false));
		for (CarStats.StatType statType = CarStats.StatType.TopSpeed; statType < CarStats.StatType.Count; statType++)
		{
			CarPart.PartType partForStatType = CarPart.GetPartForStatType(statType, this.mTeam.championship.series);
			if (list.Contains(partForStatType))
			{
				TeamAIController.CarUpgradeStatValues carUpgradeStatValues = new TeamAIController.CarUpgradeStatValues();
				carUpgradeStatValues.weighting = aiWeightings.GetStatWeight(statType);
				carUpgradeStatValues.highestStatOnGrid = carManager.GetCarStatValueOnGrid(statType, CarManager.MedianTypes.Highest);
				carUpgradeStatValues.difference = (carManager.partInventory.GetHighestStatOfType(CarPart.GetPartForStatType(statType, this.mTeam.championship.series), CarPartStats.CarPartStat.MainStat) - carUpgradeStatValues.highestStatOnGrid) * carUpgradeStatValues.weighting;
				carUpgradeStatValues.partType = CarPart.GetPartForStatType(statType, this.mTeam.championship.series);
				List<CarPart> highestLevelPartsOfType = carManager.partInventory.GetHighestLevelPartsOfType(CarPart.GetPartForStatType(statType, this.mTeam.championship.series), false);
				PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][carUpgradeStatValues.partType];
				int num3 = highestLevelPartsOfType[0].stats.level + 1;
				bool flag = partTypeSlotSettings.IsUnlocked(this.mTeam, num3);
				carUpgradeStatValues.isSpecPart = this.mTeam.championship.rules.specParts.Contains(carUpgradeStatValues.partType);
				carUpgradeStatValues.gotTwoPartsOfBestPossibleLevel = (highestLevelPartsOfType.Count >= 3 || (highestLevelPartsOfType.Count >= 2 && !flag) || (highestLevelPartsOfType.Count >= 2 && num3 >= 5));
				carUpgradeStatValues.nPartsOfbestPossibleLevel = highestLevelPartsOfType.Count;
				carUpgradeStatValues.has5ComponentSlots = (carManager.carPartDesign.GetNumberOfSlots(carUpgradeStatValues.partType) == GameStatsConstants.slotCount);
				dictionary.Add(statType, carUpgradeStatValues);
			}
		}
		CarStats.StatType statType2 = CarStats.StatType.Count;
		float num4 = -50f;
		List<CarPart.PartType> list2 = new List<CarPart.PartType>();
		for (CarStats.StatType statType3 = CarStats.StatType.TopSpeed; statType3 < CarStats.StatType.Count; statType3++)
		{
			if (dictionary.ContainsKey(statType3))
			{
				TeamAIController.CarUpgradeStatValues carUpgradeStatValues2 = dictionary[statType3];
				if (carUpgradeStatValues2.difference < num4 && !carUpgradeStatValues2.isSpecPart && !carUpgradeStatValues2.gotTwoPartsOfBestPossibleLevel)
				{
					statType2 = statType3;
					num4 = carUpgradeStatValues2.difference;
					list2.Add(carUpgradeStatValues2.partType);
				}
			}
		}
		if (list2.Count == 0)
		{
			num4 = 0f;
			for (CarStats.StatType statType4 = CarStats.StatType.TopSpeed; statType4 < CarStats.StatType.Count; statType4++)
			{
				if (dictionary.ContainsKey(statType4))
				{
					TeamAIController.CarUpgradeStatValues carUpgradeStatValues3 = dictionary[statType4];
					if (!carUpgradeStatValues3.isSpecPart && ((carUpgradeStatValues3.nPartsOfbestPossibleLevel < 3 && !carUpgradeStatValues3.has5ComponentSlots) || !carUpgradeStatValues3.gotTwoPartsOfBestPossibleLevel) && carUpgradeStatValues3.difference < num4)
					{
						statType2 = statType4;
						num4 = carUpgradeStatValues3.difference;
						list2.Add(carUpgradeStatValues3.partType);
					}
				}
			}
		}
		float num5 = (float)((aiWeightings.mAggressiveness < 0.8f) ? ((aiWeightings.mAggressiveness <= 0.4f) ? 0 : RandomUtility.GetRandom(1, 2)) : RandomUtility.GetRandom(2, 4));
		bool isBanned = carManager.partInventory.GetMostRecentParts(1, CarPart.PartType.None)[0].isBanned;
		if (list2.Count > 0 && carManager.carPartDesign.stage == CarPartDesign.Stage.Idle)
		{
			CarPart.PartType partType = CarPart.GetPartForStatType(statType2, this.mTeam.championship.series);
			List<CarPart> highestLevelPartsOfType2 = carManager.partInventory.GetHighestLevelPartsOfType(partType, true);
			highestLevelPartsOfType2.Sort((CarPart x, CarPart y) => y.stats.statWithPerformance.CompareTo(x.stats.statWithPerformance));
			if (statType2 == CarStats.StatType.Count || (carManager.carPartDesign.lastCarPart != null && carManager.carPartDesign.lastCarPart.GetPartType() == partType))
			{
				partType = list2[RandomUtility.GetRandom(0, list2.Count)];
				highestLevelPartsOfType2 = carManager.partInventory.GetHighestLevelPartsOfType(partType, true);
				highestLevelPartsOfType2.Sort((CarPart x, CarPart y) => y.stats.statWithPerformance.CompareTo(x.stats.statWithPerformance));
			}
			CarPartDesign carPartDesign = carManager.carPartDesign;
			carPartDesign.InitializeNewPart(partType);
			CarPart part = carPartDesign.part;
			Dictionary<int, List<CarPartComponent>> componentsForPartType = carPartDesign.GetComponentsForPartType(partType);
			for (int i = componentsForPartType.Keys.Count - 1; i >= 0; i--)
			{
				if (carPartDesign.HasSlotForLevel(i + 1))
				{
					List<CarPartComponent> list3 = componentsForPartType[i];
					if (this.mTeam.aiWeightings.mAggressiveness > 0.5f)
					{
						TeamAIController.SortPartComponentsForAgressiveTeams(list3);
					}
					else
					{
						TeamAIController.SortPartComponentsForNonAgressiveTeams(list3);
					}
					PartTypeSlotSettings partTypeSlotSettings2 = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][partType];
					if (partTypeSlotSettings2.IsUnlocked(this.mTeam, i))
					{
						for (int j = 0; j < list3.Count; j++)
						{
							CarPartComponent carPartComponent = list3[j];
							if (carPartComponent.IsUnlocked(this.mTeam))
							{
								if (!carPartDesign.HasSlotForLevel(carPartComponent.level))
								{
									break;
								}
								if ((!isBanned && num5 >= carPartComponent.riskLevel) || carPartComponent.riskLevel == 0f || (i == 0 && j == list3.Count - 1))
								{
									carPartDesign.AddComponent(part, carPartComponent);
									num5 -= carPartComponent.riskLevel;
								}
								if (!part.hasComponentSlotsAvailable)
								{
									break;
								}
							}
						}
						if (!part.hasComponentSlotsAvailable)
						{
							break;
						}
					}
				}
			}
			if ((long)carManager.carPartDesign.GetDesignCost() < num2)
			{
				Transaction transaction = new Transaction(Transaction.Group.CarParts, Transaction.Type.Debit, carManager.carPartDesign.GetDesignCost(), carManager.carPartDesign.part.GetPartName());
				this.mTeam.financeController.finance.ProcessTransactions(null, null, false, new Transaction[]
				{
					transaction
				});
				carManager.carPartDesign.StartDesigning();
			}
		}
		this.mLastCarUpdateTime = Game.instance.time.now;
		this.ImproveCarParts(carManager);
	}

	private bool PickRandomPartFromListAndImproveIt(CarManager lCarManager, ref List<CarPart> parts_list, ref List<int> index_list)
	{
		int num = UnityEngine.Random.Range(0, index_list.Count);
		int i = index_list.Count;
		while (i > 0)
		{
			i--;
			CarPart carPart = parts_list[index_list[num]];
			if (carPart.stats.performance < carPart.stats.maxPerformance)
			{
				PartImprovement partImprovement = lCarManager.partImprovement;
				if (partImprovement != null)
				{
					partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Performance, carPart);
					if (carPart.stats.reliability < 1f)
					{
						partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Reliability, carPart);
						partImprovement.SplitMechanics(this.mTeam.aiWeightings.mAggressiveness);
					}
					return true;
				}
			}
			num++;
			if (num >= index_list.Count)
			{
				num = 0;
			}
		}
		return false;
	}

	private void ImproveCarParts(CarManager inCarManager)
	{
		this.FitPartsOnCars();
		PartImprovement partImprovement = inCarManager.partImprovement;
		partImprovement.RemoveAllPartImprove(CarPartStats.CarPartStat.Reliability);
		partImprovement.RemoveAllPartImprove(CarPartStats.CarPartStat.Performance);
		CarPart part = inCarManager.carPartDesign.part;
		if (part != null)
		{
			CarPart highestStatPartOfType = inCarManager.partInventory.GetHighestStatPartOfType(part.GetPartType());
			partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Reliability, highestStatPartOfType);
			partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Performance, highestStatPartOfType);
		}
		float num = Mathf.Lerp(TeamAIController.carImprovementReliabilityMinAggression, TeamAIController.carImprovementReliabilityMaxAggression, this.mTeam.aiWeightings.mAggressiveness);
		Championship championship = this.mTeam.championship;
		Circuit circuit = championship.calendar[championship.eventNumber].circuit;
		partImprovement.SplitMechanics(this.mTeam.aiWeightings.mAggressiveness);
		partImprovement.playerMechanicsPreference = this.mTeam.aiWeightings.mAggressiveness;
		List<CarPart> list = new List<CarPart>(CarManager.carCount * 6);
		for (int i = 0; i < CarManager.carCount; i++)
		{
			Car car = inCarManager.GetCar(i);
			for (int j = 0; j < car.seriesCurrentParts.Length; j++)
			{
				CarPart carPart = car.seriesCurrentParts[j];
				if (carPart.stats.reliability < num)
				{
					partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Reliability, carPart);
					partImprovement.SplitMechanics(0f);
					partImprovement.playerMechanicsPreference = 0f;
				}
				CarStats.RelevantToCircuit relevancy = CarStats.GetRelevancy(Mathf.RoundToInt(circuit.trackStatsCharacteristics.GetStat(CarPart.GetStatForPartType(carPart.GetPartType()))));
				if (relevancy == CarStats.RelevantToCircuit.VeryImportant)
				{
					partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Performance, carPart);
					list.Add(carPart);
				}
			}
		}
		if (!partImprovement.WorkOnStatActive(CarPartStats.CarPartStat.Reliability))
		{
			for (int k = 0; k < list.Count; k++)
			{
				partImprovement.AddPartToImprove(CarPartStats.CarPartStat.Reliability, list[k]);
			}
		}
		for (int l = 0; l < PartImprovement.playerAvailableImprovementTypes.Length; l++)
		{
			if (!partImprovement.WorkOnStatActive(PartImprovement.playerAvailableImprovementTypes[l]))
			{
				partImprovement.AutoFill(PartImprovement.playerAvailableImprovementTypes[l]);
			}
		}
		this.FitPartsWithMinReliability();
	}

	private static void SortPartComponentsForNonAgressiveTeams(List<CarPartComponent> lPartsForLevel)
	{
		lPartsForLevel.Sort((CarPartComponent componentA, CarPartComponent componentB) => componentB.nonAgressiveTeamWeightings.CompareTo(componentA.nonAgressiveTeamWeightings));
	}

	private static void SortPartComponentsForAgressiveTeams(List<CarPartComponent> lPartsForLevel)
	{
		lPartsForLevel.Sort((CarPartComponent componentA, CarPartComponent componentB) => componentB.agressiveTeamWeightings.CompareTo(componentA.agressiveTeamWeightings));
	}

	public void FitPartsOnCars()
	{
		this.mTeam.carManager.AutoFit(this.mTeam.carManager.GetCar(0), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.AllParts);
		this.mTeam.carManager.AutoFit(this.mTeam.carManager.GetCar(1), CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
	}

	public void FitPartsWithMinReliability()
	{
		CarManager carManager = this.mTeam.carManager;
		float num = Mathf.Lerp(TeamAIController.carImprovementReliabilityMinAggression, TeamAIController.carImprovementReliabilityMaxAggression, this.mTeam.aiWeightings.mAggressiveness);
		for (int i = 0; i < CarManager.carCount; i++)
		{
			Car car = carManager.GetCar(i);
			for (int j = 0; j < car.seriesCurrentParts.Length; j++)
			{
				CarPart carPart = car.seriesCurrentParts[j];
				if (carPart.stats.reliability < num)
				{
					CarPart suitablePartForCar = this.GetSuitablePartForCar(carManager, carPart, num);
					if (suitablePartForCar != null)
					{
						car.FitPart(suitablePartForCar);
					}
				}
			}
		}
	}

	private CarPart GetSuitablePartForCar(CarManager inCarManager, CarPart inPartToReplace, float inMinReliability)
	{
		CarPart carPart = null;
		List<CarPart> partInventory = inCarManager.partInventory.GetPartInventory(inPartToReplace);
		for (int i = 0; i < partInventory.Count; i++)
		{
			CarPart carPart2 = partInventory[i];
			if (!carPart2.isBanned && !carPart2.isFitted && carPart2.stats.reliability >= inMinReliability && (carPart == null || carPart.stats.statWithPerformance < carPart2.stats.statWithPerformance))
			{
				carPart = carPart2;
			}
		}
		return carPart;
	}

	public void HandleHQUpgrades()
	{
		float unitAverage = this.mTeam.teamPrincipal.stats.GetUnitAverage();
		int num = 7 + (int)(Mathf.Clamp01(1f - unitAverage) * 3f);
		if ((Game.instance.time.now - this.mLastHQUpdateTime).Days < num)
		{
			return;
		}
		if (this.mTeam.headquarters.GetNumLevelingInProgress() >= TeamAIController.maxActiveHQBuilding)
		{
			return;
		}
		if (this.mTeam.financeController.finance.currentBudget <= 25000000L && this.mTeam.financeController.GetTotalCostPerRace() < -250000L)
		{
			return;
		}
		int count = this.mHQTargetsList.Count;
		if (count == 0)
		{
			this.SelectHQTargetBuilding();
			count = this.mHQTargetsList.Count;
		}
		if (count > 0)
		{
			this.ProcessHQTargetBuilding();
		}
	}

	private void SelectHQTargetBuilding()
	{
		this.mPotentialHQTargets.Clear();
		int count = this.mTeam.headquarters.hqBuildings.Count;
		for (int i = 0; i < count; i++)
		{
			HQsBuilding_v1 hqsBuilding_v = this.mTeam.headquarters.hqBuildings[i];
			if (!hqsBuilding_v.isLeveling && !hqsBuilding_v.isMaxLevel)
			{
				float num = this.GetHQWeightingByCategory(hqsBuilding_v.info.category) * this.GetHQWeightingHistoryByCategory(hqsBuilding_v.info.category);
				float mValue = num * (this.GetBuildingPriorityWeight(hqsBuilding_v) * this.GetBuildingCarStatWeight(hqsBuilding_v));
				this.mPotentialHQTargets.Add(new TeamAIController.HQBuildingValue
				{
					mValue = mValue,
					mBuilding = hqsBuilding_v,
					mbIsUpgrade = hqsBuilding_v.isBuilt
				});
			}
		}
		if (this.mPotentialHQTargets.Count > 0)
		{
			this.mPotentialHQTargets.Sort((TeamAIController.HQBuildingValue x, TeamAIController.HQBuildingValue y) => y.mValue.CompareTo(x.mValue));
			float mValue2 = this.mPotentialHQTargets[0].mValue;
			int count2 = this.mPotentialHQTargets.Count;
			for (int j = count2 - 1; j >= 0; j--)
			{
				TeamAIController.HQBuildingValue hqbuildingValue = this.mPotentialHQTargets[j];
				if (hqbuildingValue.mValue < mValue2)
				{
					this.mPotentialHQTargets.RemoveAt(j);
				}
			}
			TeamAIController.HQBuildingValue hqbuildingValue2 = this.mPotentialHQTargets[RandomUtility.GetRandom(0, this.mPotentialHQTargets.Count - 1)];
			if (hqbuildingValue2.mBuilding.isLocked)
			{
				foreach (HQsDependency hqsDependency in hqbuildingValue2.mBuilding.GetRemainingDependencies())
				{
					HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(hqsDependency.buildingType);
					int num2 = 0;
					if (building.isLeveling)
					{
						num2 += hqsDependency.requiredLevel - building.nextLevel;
					}
					else
					{
						if (!building.isBuilt)
						{
							num2++;
						}
						num2 += hqsDependency.requiredLevel - building.currentLevel;
					}
					for (int l = 0; l < num2; l++)
					{
						this.mHQTargetsList.Add(hqsDependency.buildingType);
					}
				}
			}
			this.mHQTargetsList.Add(hqbuildingValue2.mBuilding.info.type);
		}
	}

	private void ProcessHQTargetBuilding()
	{
		Transaction transaction = null;
		HQsBuilding_v1 hqsBuilding_v = null;
		int index = 0;
		int count = this.mHQTargetsList.Count;
		for (int i = 0; i < count; i++)
		{
			HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(this.mHQTargetsList[i]);
			if (!building.isLeveling && !building.isLocked && (building.CanBuild() || building.CanUpgrade()))
			{
				hqsBuilding_v = building;
				index = i;
				break;
			}
		}
		if (hqsBuilding_v != null)
		{
			long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceHQ);
			if (hqsBuilding_v.CanUpgrade() && hqsBuilding_v.costs.GetUpgradeCost() < (float)num)
			{
				hqsBuilding_v.BeginUpgrade();
				StringVariableParser.buildingInfo = hqsBuilding_v.info;
				StringVariableParser.ordinalNumberString = hqsBuilding_v.nextLevelUI;
				transaction = new Transaction(Transaction.Group.HQ, Transaction.Type.Debit, (int)Math.Round((double)hqsBuilding_v.costs.GetUpgradeCost(), MidpointRounding.AwayFromZero), Localisation.LocaliseID("PSG_10010572", null));
			}
			else if (hqsBuilding_v.CanBuild() && hqsBuilding_v.costs.GetBuildTotalCost() < (float)num)
			{
				hqsBuilding_v.BeginBuilding();
				StringVariableParser.buildingInfo = hqsBuilding_v.info;
				transaction = new Transaction(Transaction.Group.HQ, Transaction.Type.Debit, hqsBuilding_v.info.initialCost, Localisation.LocaliseID("PSG_10010573", null));
			}
			if (transaction != null)
			{
				this.mTeam.financeController.finance.ProcessTransactions(null, null, false, new Transaction[]
				{
					transaction
				});
				this.mLastHQUpdateTime = Game.instance.time.now;
				this.mHQTargetsList.RemoveAt(index);
				this.mHQHistoryList.Add(hqsBuilding_v.info.type);
				if (this.mHQHistoryList.Count > 5)
				{
					this.mHQHistoryList.RemoveAt(0);
				}
			}
		}
	}

	public float GetHQWeightingByCategory(HQsBuildingInfo.Category aCategory)
	{
		switch (aCategory)
		{
		case HQsBuildingInfo.Category.Design:
			return this.mTeam.aiWeightings.mHQDesignWeight;
		case HQsBuildingInfo.Category.Factory:
			return this.mTeam.aiWeightings.mHQFactoryWeight;
		case HQsBuildingInfo.Category.Performance:
			return this.mTeam.aiWeightings.mHQPerformanceWeight;
		case HQsBuildingInfo.Category.Staff:
			return this.mTeam.aiWeightings.mHQStaffWeight;
		case HQsBuildingInfo.Category.Brand:
			return this.mTeam.aiWeightings.mHQBrandWeight;
		default:
			return 0f;
		}
	}

	public float GetHQWeightingHistoryByCategory(HQsBuildingInfo.Category inCategory)
	{
		float num = this.GetHQWeightingByCategory(inCategory);
		int count = this.mHQHistoryList.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.mTeam.headquarters.GetBuilding(this.mHQHistoryList[i]).info.category == inCategory)
			{
				num -= 0.5f;
			}
		}
		return Mathf.Max(num, 0f);
	}

	public float GetBuildingPriorityWeight(HQsBuilding_v1 inBuilding)
	{
		float num = 1f;
		if (inBuilding.isBuilt)
		{
			if (inBuilding.isMaxLevel)
			{
				num = 0f;
			}
			else
			{
				num -= (float)(1 + inBuilding.currentLevel) * 0.15f;
			}
		}
		else if (inBuilding.isLocked)
		{
			foreach (HQsDependency hqsDependency in inBuilding.GetRemainingDependencies())
			{
				HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(hqsDependency.buildingType);
				num -= (float)(hqsDependency.requiredLevel - building.currentLevel + ((!building.isBuilt) ? 1 : 0)) * 0.1f;
			}
		}
		return Mathf.Clamp01(num);
	}

	public float GetBuildingCarStatWeight(HQsBuilding_v1 inBuilding)
	{
		float num = 1f;
		if (inBuilding.info.category == HQsBuildingInfo.Category.Performance)
		{
			CarStats.StatType inStat = CarStats.StatType.Count;
			switch (inBuilding.info.type)
			{
			case HQsBuildingInfo.Type.TelemetryCentre:
				inStat = CarStats.StatType.Acceleration;
				break;
			case HQsBuildingInfo.Type.TestTrack:
				inStat = CarStats.StatType.TopSpeed;
				break;
			case HQsBuildingInfo.Type.WindTunnel:
				inStat = CarStats.StatType.LowSpeedCorners;
				break;
			case HQsBuildingInfo.Type.Simulator:
				inStat = CarStats.StatType.HighSpeedCorners;
				break;
			case HQsBuildingInfo.Type.BrakesResearchFacility:
				inStat = CarStats.StatType.Braking;
				break;
			case HQsBuildingInfo.Type.RideHandlingDevelopment:
				inStat = CarStats.StatType.MediumSpeedCorners;
				break;
			}
			float carStatValueOnGrid = this.mTeam.carManager.GetCarStatValueOnGrid(inStat, CarManager.MedianTypes.Highest);
			CarPart.PartType partForStatType = CarPart.GetPartForStatType(inStat, inBuilding.team.championship.series);
			if (partForStatType == CarPart.PartType.None)
			{
				return 0f;
			}
			float highestStatOfType = this.mTeam.carManager.partInventory.GetHighestStatOfType(partForStatType, CarPartStats.CarPartStat.MainStat);
			num = ((!Mathf.Approximately(carStatValueOnGrid, 0f)) ? Mathf.Clamp01(1f - highestStatOfType / carStatValueOnGrid) : 1f);
			bool flag = this.mTeam.championship.rules.specParts.Contains(CarPart.GetPartForStatType(inStat, inBuilding.team.championship.series));
			num *= ((!flag) ? 1f : 0.01f);
		}
		return Mathf.Clamp01(num);
	}

	public void SelectSponsorForEvent()
	{
		List<SponsorshipDeal> sponsorshipDeals = this.mTeam.sponsorController.sponsorshipDeals;
		if (sponsorshipDeals.Count > 0)
		{
			int expectedModifiedRacePosition = this.GetExpectedModifiedRacePosition();
			SponsorshipDeal closestSponsorshipDeal = this.GetClosestSponsorshipDeal(sponsorshipDeals, expectedModifiedRacePosition);
			this.mTeam.sponsorController.SetWeekendSponsor(closestSponsorshipDeal);
			if (closestSponsorshipDeal != null)
			{
				closestSponsorshipDeal.contract.SetRaceAttended(this.mTeam.championship.GetCurrentEventDetails().circuit);
				closestSponsorshipDeal.contract.lattestRaceAttendedDate = this.mTeam.championship.GetCurrentEventDetails().eventDate;
			}
		}
	}

	private SponsorshipDeal GetClosestSponsorshipDeal(List<SponsorshipDeal> inSponsorshipDeals, int inExpectedPosition)
	{
		SponsorshipDeal closestSponsorshipToTargetPosition = this.GetClosestSponsorshipToTargetPosition(inSponsorshipDeals, inExpectedPosition, true);
		if (closestSponsorshipToTargetPosition != null)
		{
			return closestSponsorshipToTargetPosition;
		}
		return this.GetClosestSponsorshipToTargetPosition(inSponsorshipDeals, inExpectedPosition, false);
	}

	private SponsorshipDeal GetClosestSponsorshipToTargetPosition(List<SponsorshipDeal> inSponsorshipDeals, int inTargetPosition, bool inLowerBound)
	{
		this.mChosenDealsCache.Clear();
		int num = int.MaxValue;
		for (int i = 0; i < inSponsorshipDeals.Count; i++)
		{
			if (inSponsorshipDeals[i].hasRaceBonusReward)
			{
				bool flag;
				if (inLowerBound)
				{
					flag = (inSponsorshipDeals[i].contract.bonusTarget >= inTargetPosition);
				}
				else
				{
					flag = (inSponsorshipDeals[i].contract.bonusTarget <= inTargetPosition);
				}
				if (flag)
				{
					int num2 = Mathf.Abs(inSponsorshipDeals[i].contract.bonusTarget - inTargetPosition);
					if (num2 < num)
					{
						num = num2;
						this.mChosenDealsCache.Clear();
					}
					if (num2 == num)
					{
						this.mChosenDealsCache.Add(inSponsorshipDeals[i]);
					}
				}
			}
		}
		this.mChosenDealsCache.Sort((SponsorshipDeal x, SponsorshipDeal y) => y.GetObjectivesTotalBonus().CompareTo(x.GetObjectivesTotalBonus()));
		if (this.mChosenDealsCache.Count > 0)
		{
			return this.mChosenDealsCache[0];
		}
		return null;
	}

	public void EvaluateSponsorOffers()
	{
		if (Game.instance.time.now > this.mLastCheckedTime)
		{
			this.CheckForNewSponsorDeals();
			this.RequestFunds();
			this.mLastCheckedTime = Game.instance.time.now.AddDays((double)this.mDaysBetweenSponsorChecks);
		}
	}

	private void RequestFunds()
	{
		if (Game.instance.time.now > this.mRequestFundsCooldown && this.mTeam.financeController.finance.currentBudget < 0L)
		{
			if (this.mTeam.chairman.CanRequestFunds() == Chairman.RequestFundsAnswer.Accepted)
			{
				Transaction requestFundsTransaction = this.mTeam.chairman.GetRequestFundsTransaction();
				this.mTeam.financeController.unnallocatedTransactions.Add(requestFundsTransaction);
				this.mTeam.financeController.HandleUnallocatedTransactions();
			}
			this.mRequestFundsCooldown = Game.instance.time.now.AddDays(90.0);
		}

		// decide investment for next year car
		int eventNumber = this.mTeam.championship.eventNumber;
		bool positivRaceCost = this.mTeam.financeController.GetTotalCostPerRace() >= 0L;
		bool meetChairmanExpectation = this.mTeam.GetChampionshipEntry().GetCurrentChampionshipPosition() <= this.mTeam.chairman.expectedTeamChampionshipResult;
		if (eventNumber == 0)
			this.mTeam.financeController.SetCarInvestement(TeamFinanceController.NextYearCarInvestement.Medium);
		else if (positivRaceCost && meetChairmanExpectation)
			this.mTeam.financeController.SetCarInvestement(TeamFinanceController.NextYearCarInvestement.High);
		else if (positivRaceCost || meetChairmanExpectation)
			this.mTeam.financeController.SetCarInvestement(TeamFinanceController.NextYearCarInvestement.Medium);
		else
			this.mTeam.financeController.SetCarInvestement(TeamFinanceController.NextYearCarInvestement.Low);
	}

	private void CheckForNewSponsorDeals()
	{
		SponsorSlot[] slots = this.mTeam.sponsorController.slots;
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].isFreeSlot)
			{
				List<ContractSponsor> map = this.mTeam.sponsorController.sponsorOffers.GetMap((SponsorSlot.SlotType)i);
				if (this.IsListForPerRacePaymentsSponsors(map))
				{
					this.PickBestPerRacePaymentSponsors(map);
				}
				else
				{
					this.PickBestPerRaceBonusSponsors(map);
				}
			}
		}
	}

	private void PickBestPerRacePaymentSponsors(List<ContractSponsor> inSponsorOffers)
	{
		if (inSponsorOffers.Count > 0)
		{
			inSponsorOffers.Sort((ContractSponsor x, ContractSponsor y) => y.GetPotentialValuePerRacePayment().CompareTo(x.GetPotentialValuePerRacePayment()));
			ContractSponsor inContract = inSponsorOffers[0];
			this.mTeam.sponsorController.AddSponsor(inContract, true);
		}
	}

	private void PickBestPerRaceBonusSponsors(List<ContractSponsor> inSponsorOffers)
	{
		if (inSponsorOffers.Count > 0)
		{
			int expectedModifiedRacePosition = this.GetExpectedModifiedRacePosition();
			ContractSponsor closestContractToExpectedRacePosition = this.GetClosestContractToExpectedRacePosition(inSponsorOffers, expectedModifiedRacePosition, true);
			if (closestContractToExpectedRacePosition == null)
			{
				closestContractToExpectedRacePosition = this.GetClosestContractToExpectedRacePosition(inSponsorOffers, expectedModifiedRacePosition, false);
			}
			if (closestContractToExpectedRacePosition != null)
			{
				if (this.WouldChooseThisSponsorOverTheAlreadySignedOnes(closestContractToExpectedRacePosition))
				{
					this.mTeam.sponsorController.AddSponsor(closestContractToExpectedRacePosition, true);
					this.mTeam.sponsorController.CreateNewSessionObjectives();
				}
				else
				{
					ContractSponsor contractSponsor = this.ChooseBestUpFrontPaymentSponsor(inSponsorOffers);
					if (contractSponsor != null)
					{
						this.mTeam.sponsorController.AddSponsor(contractSponsor, true);
						this.mTeam.sponsorController.CreateNewSessionObjectives();
					}
				}
			}
		}
	}

	private ContractSponsor GetClosestContractToExpectedRacePosition(List<ContractSponsor> inOffers, int inTargetPosition, bool inLowerBound)
	{
		this.mChosenOffersCache.Clear();
		int num = int.MaxValue;
		for (int i = 0; i < inOffers.Count; i++)
		{
			if (inOffers[i].bonusValuePerRace > 0)
			{
				bool flag;
				if (inLowerBound)
				{
					flag = (inOffers[i].bonusTarget >= inTargetPosition);
				}
				else
				{
					flag = (inOffers[i].bonusTarget <= inTargetPosition);
				}
				if (flag)
				{
					int num2 = Mathf.Abs(inOffers[i].bonusTarget - inTargetPosition);
					if (num2 < num)
					{
						num = num2;
						this.mChosenDealsCache.Clear();
					}
					if (num2 == num)
					{
						this.mChosenOffersCache.Add(inOffers[i]);
					}
				}
			}
		}
		this.mChosenOffersCache.Sort((ContractSponsor x, ContractSponsor y) => y.GetPotentialValue().CompareTo(x.GetPotentialValue()));
		if (this.mChosenOffersCache.Count > 0)
		{
			return this.mChosenOffersCache[0];
		}
		return null;
	}

	private bool WouldChooseThisSponsorOverTheAlreadySignedOnes(ContractSponsor inContractSponsor)
	{
		List<SponsorshipDeal> sponsorshipDeals = this.mTeam.sponsorController.sponsorshipDeals;
		bool flag = false;
		if (sponsorshipDeals.Count > 0)
		{
			int expectedModifiedRacePosition = this.GetExpectedModifiedRacePosition();
			SponsorshipDeal closestSponsorshipDeal = this.GetClosestSponsorshipDeal(sponsorshipDeals, expectedModifiedRacePosition);
			if (closestSponsorshipDeal != null)
			{
				if (closestSponsorshipDeal.contract.bonusTarget < expectedModifiedRacePosition)
				{
					if (inContractSponsor.bonusTarget > closestSponsorshipDeal.contract.bonusTarget)
					{
						flag = true;
					}
					else if (inContractSponsor.bonusTarget == closestSponsorshipDeal.contract.bonusTarget)
					{
						flag = (inContractSponsor.bonusValuePerRace > closestSponsorshipDeal.GetObjectivesTotalBonus());
					}
				}
				else if (inContractSponsor.bonusTarget >= expectedModifiedRacePosition)
				{
					if (inContractSponsor.bonusTarget < closestSponsorshipDeal.contract.bonusTarget)
					{
						flag = true;
					}
					else if (inContractSponsor.bonusTarget == closestSponsorshipDeal.contract.bonusTarget)
					{
						flag = (inContractSponsor.bonusValuePerRace > closestSponsorshipDeal.GetObjectivesTotalBonus());
					}
				}
				if (flag)
				{
				}
			}
		}
		return flag;
	}

	private ContractSponsor ChooseBestUpFrontPaymentSponsor(List<ContractSponsor> inOffers)
	{
		if (inOffers.Count > 0)
		{
			inOffers.Sort((ContractSponsor x, ContractSponsor y) => y.upfrontValue.CompareTo(x.upfrontValue));
			if (inOffers[0].upfrontValue > 0)
			{
				return inOffers[0];
			}
		}
		return null;
	}

	private int GetExpectedModifiedRacePosition()
	{
		int expectedBestPositionForTeamInRace = this.GetExpectedBestPositionForTeamInRace();
		int num = expectedBestPositionForTeamInRace;
		if (!Mathf.Approximately(this.mTeam.aiWeightings.mAggressiveness, 0.5f))
		{
			if ((double)this.mTeam.aiWeightings.mAggressiveness > 0.5)
			{
				num -= this.mSponsorTargetPositionVariance;
			}
			else if ((double)this.mTeam.aiWeightings.mAggressiveness < 0.5)
			{
				num -= this.mSponsorTargetPositionVariance - 2;
			}
		}
		else
		{
			num -= this.mSponsorTargetPositionVariance - 1;
		}
		return Mathf.Max(1, num);
	}

	private int GetExpectedBestPositionForTeamInRace()
	{
		if (this.mTeam.GetDriver(0).expectedRacePosition < this.mTeam.GetDriver(1).expectedRacePosition)
		{
			return this.mTeam.GetDriver(0).expectedRacePosition;
		}
		return this.mTeam.GetDriver(1).expectedRacePosition;
	}

	private bool IsListForPerRacePaymentsSponsors(List<ContractSponsor> inSponsorOffers)
	{
		for (int i = 0; i < inSponsorOffers.Count; i++)
		{
			if (inSponsorOffers[i].bonusValuePerRace > 0)
			{
				return false;
			}
		}
		return true;
	}

	public void HandleStaffMovement()
	{
		this.mSeasonWeight = 0f;
		if (this.mTeam.championship.IsDateInPreseason(Game.instance.time.now))
		{
			this.mSeasonWeight = 1f;
		}
		this.UpdateChanceOfFiring();
		this.HandleDriverChanges();
		this.HandleEngineerChanges();
		this.HandleMechanicChanges();
		this.HandleTeamPrincipalChanges();
	}

	public void HandleDriverChanges()
	{
		this.mDrivers.Clear();
		this.mPeopleApproachedAndRejectedBy.Clear();
		this.mTeam.contractManager.GetAllDrivers(ref this.mDrivers);
		if (this.mTeam.financeController.finance.currentBudget < 0L && this.mTeam.financeController.GetTotalCostPerRace() < 0L)
		{
			this.mDrivers.Remove(this.mTeam.GetReserveDriver());
		}
		for (int i = 0; i < this.mDrivers.Count; i++)
		{
			Driver lCurDriver = this.mDrivers[i];
			TeamAIController.NegotiationEntry negotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => (x.mPersonToFire != null && x.mPersonToFire.personHired == lCurDriver) || x.mPerson == lCurDriver);
			bool flag = lCurDriver.IsReplacementPerson();
			bool flag2 = flag || (Game.instance.time.now - lCurDriver.contract.startDate).Days > TeamAIController.hireStaffCooldownDays;
			bool flag3 = false;
			bool flag4 = false;
			if (negotiationEntry == null && flag2)
			{
				bool flag5 = false;
				if (flag || this.ShouldFire(lCurDriver))
				{
					flag5 = true;
					flag3 = !flag;
				}
				else if (lCurDriver.WantsToLeave() || lCurDriver.IsOpenToOffers())
				{
					if (this.AllowToLeave(lCurDriver))
					{
						flag5 = true;
						flag4 = true;
					}
				}
				else if (this.mTeam.championship.InPreseason() && Game.instance.time.now.Month > 6 && !lCurDriver.contract.IsContractedForNextSeason())
				{
					if (this.ShouldRenew(lCurDriver))
					{
						this.Renew(lCurDriver);
					}
					else
					{
						flag5 = true;
					}
				}
				if (flag5)
				{
					bool inReplaceWithReserve = false;
					Driver reserveDriver = this.mTeam.GetReserveDriver();
					Driver driver = this.FindReplacementDriver(lCurDriver, flag4, 0.75f);
					if (reserveDriver != null)
					{
						bool flag6 = (Game.instance.time.now - reserveDriver.contract.startDate).Days > TeamAIController.hireStaffCooldownDays;
						bool flag7 = driver == null || reserveDriver.GetStatsForAITeamComparison(this.mTeam) > driver.GetStatsForAITeamComparison(this.mTeam);
						if (lCurDriver.IsMainDriver() && !reserveDriver.IsReplacementPerson() && flag6 && flag7 && !reserveDriver.contractManager.isNegotiating && reserveDriver.GetInterestedToTalkReaction(this.mTeam) == Person.InterestedToTalkResponseType.InterestedToTalk)
						{
							if (!flag4 || this.IsStarRatingThisMuchBetter(reserveDriver, lCurDriver, 0.75f))
							{
								inReplaceWithReserve = true;
								driver = reserveDriver;
							}
							else
							{
								driver = null;
							}
						}
					}
					if (driver != null)
					{
						this.ReplaceDriver(lCurDriver, driver, flag, inReplaceWithReserve);
						if (flag3)
						{
							this.mLastFiringUpdateTime = Game.instance.time.now;
							this.mChanceOfFiring = 0f;
						}
					}
					else if (flag4)
					{
					}
				}
			}
		}
		this.mScoutableDrivers.Clear();
	}

	private Driver FindReplacementDriver(Driver current_driver, bool has_to_be_better, float better_delta = 0f)
	{
		long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
		HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.ScoutingFacility);
		int num2 = 0;
		if (building != null && building.isBuilt)
		{
			num2 = building.currentLevel;
		}
		this.mScoutableDrivers.Clear();
		List<Driver> entityList = Game.instance.driverManager.GetEntityList();
		for (int i = 0; i < entityList.Count; i++)
		{
			Driver driver = entityList[i];
			if (this.IsScoutable(driver) && !driver.Equals(current_driver) && this.PersonNotAlreadyApproached(driver))
			{
				if (!driver.IsFreeAgent() || !this.mTeam.championship.InPreseason() || Game.instance.time.now.Month >= 6 || driver.careerHistory.previousTeam == null || !driver.careerHistory.previousTeam.Equals(this.mTeam))
				{
					if ((driver.GetDriverStats().scoutingLevelRequired == 0 || num2 > driver.GetDriverStats().scoutingLevelRequired) && driver.GetInterestedToTalkReaction(this.mTeam) == Person.InterestedToTalkResponseType.InterestedToTalk && (driver.IsFreeAgent() || num > (long)driver.contract.GetContractTerminationCost()))
					{
						this.mScoutableDrivers.Add(driver);
					}
				}
			}
		}
		if (this.mScoutableDrivers.Count <= 1)
		{
			return null;
		}
		this.mScoutableDrivers.Sort((Driver x, Driver y) => y.GetStatsForAITeamComparison(this.mTeam).CompareTo(x.GetStatsForAITeamComparison(this.mTeam)));
		if (!has_to_be_better)
		{
			return this.mScoutableDrivers[0];
		}
		if (this.IsStarRatingThisMuchBetter(this.mScoutableDrivers[0], current_driver, better_delta))
		{
			return this.mScoutableDrivers[0];
		}
		return null;
	}

	private bool IsStarRatingThisMuchBetter(Driver compare_to, Driver driver, float better_delta)
	{
		return compare_to != null && driver != null && compare_to.GetStatsForAITeamComparison(this.mTeam) - driver.GetStatsForAITeamComparison(this.mTeam) >= better_delta;
	}

	public void EvaluateDriverLineUp()
	{
		this.mDrivers.Clear();
		this.mTeam.contractManager.GetAllDrivers(ref this.mDrivers);
		TeamAIController.DriverEval driverEval = null;
		TeamAIController.DriverEval driverEval2 = null;
		TeamAIController.DriverEval driverEval3 = null;
		int i;
		for (i = 0; i < this.mDrivers.Count; i++)
		{
			TeamAIController.NegotiationEntry negotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => (x.mPersonToFire != null && x.mPersonToFire.personHired == this.mDrivers[i]) || x.mPerson == this.mDrivers[i]);
			bool is_negotiating = negotiationEntry != null;
			if (this.mDrivers[i].IsReserveDriver())
			{
				driverEval3 = new TeamAIController.DriverEval(i, this.mDrivers[i].GetStatsForAITeamComparison(this.mTeam), this.mDrivers[i].personalityTraitController.IsPayDriver(), is_negotiating);
			}
			else if (driverEval == null)
			{
				driverEval = new TeamAIController.DriverEval(i, this.mDrivers[i].GetStatsForAITeamComparison(this.mTeam), this.mDrivers[i].personalityTraitController.IsPayDriver(), is_negotiating);
			}
			else if (driverEval2 == null)
			{
				driverEval2 = new TeamAIController.DriverEval(i, this.mDrivers[i].GetStatsForAITeamComparison(this.mTeam), this.mDrivers[i].personalityTraitController.IsPayDriver(), is_negotiating);
			}
		}
		if (driverEval3 != null && !driverEval3.mIsNegotiating)
		{
			if (!driverEval.mIsNegotiating && !driverEval2.mIsNegotiating)
			{
				bool flag = driverEval3.mStats > driverEval.mStats;
				bool flag2 = driverEval3.mStats > driverEval2.mStats;
				if (flag && flag2)
				{
					if (driverEval.mIsPayDriver && driverEval2.mIsPayDriver)
					{
						if (this.mDrivers[driverEval.mIndex].personalityTraitController.GetTieredPayDriverAmount() > this.mDrivers[driverEval2.mIndex].personalityTraitController.GetTieredPayDriverAmount())
						{
							this.PromoteDriver(this.mDrivers[driverEval2.mIndex], this.mDrivers[driverEval3.mIndex]);
						}
						else
						{
							this.PromoteDriver(this.mDrivers[driverEval.mIndex], this.mDrivers[driverEval3.mIndex]);
						}
					}
					else if (driverEval.mStats > driverEval2.mStats)
					{
						this.PromoteDriver(this.mDrivers[driverEval2.mIndex], this.mDrivers[driverEval3.mIndex]);
					}
					else
					{
						this.PromoteDriver(this.mDrivers[driverEval.mIndex], this.mDrivers[driverEval3.mIndex]);
					}
				}
				else if (!flag && !flag2)
				{
					if (driverEval3.mIsPayDriver && !driverEval.mIsPayDriver && !driverEval2.mIsPayDriver && this.IsPayDriverWorthIt(this.mDrivers[driverEval3.mIndex]))
					{
						if (driverEval.mStats > driverEval2.mStats)
						{
							this.PromoteDriver(this.mDrivers[driverEval2.mIndex], this.mDrivers[driverEval3.mIndex]);
						}
						else
						{
							this.PromoteDriver(this.mDrivers[driverEval.mIndex], this.mDrivers[driverEval3.mIndex]);
						}
					}
				}
				else if (flag)
				{
					this.CompareDriversForPromotion(driverEval3, driverEval, driverEval2);
				}
				else if (flag2)
				{
					this.CompareDriversForPromotion(driverEval3, driverEval2, driverEval);
				}
			}
			else if (!driverEval.mIsNegotiating)
			{
				this.CompareDriversForPromotion(driverEval3, driverEval, driverEval2);
			}
			else if (!driverEval2.mIsNegotiating)
			{
				this.CompareDriversForPromotion(driverEval3, driverEval2, driverEval);
			}
		}
	}

	private void CompareDriversForPromotion(TeamAIController.DriverEval reserve, TeamAIController.DriverEval to_compare, TeamAIController.DriverEval other_driver)
	{
		if (reserve.mStats > to_compare.mStats)
		{
			if ((reserve.mIsPayDriver && to_compare.mIsPayDriver) || (!to_compare.mIsPayDriver && !reserve.mIsPayDriver) || other_driver.mIsPayDriver)
			{
				this.PromoteDriver(this.mDrivers[to_compare.mIndex], this.mDrivers[reserve.mIndex]);
			}
			else if (to_compare.mIsPayDriver && !reserve.mIsPayDriver && !this.IsPayDriverWorthIt(this.mDrivers[to_compare.mIndex]) && reserve.mStats - to_compare.mStats >= 0.75f)
			{
				this.PromoteDriver(this.mDrivers[to_compare.mIndex], this.mDrivers[reserve.mIndex]);
			}
		}
	}

	private void PromoteDriver(Driver Demoted, Driver Promoted)
	{
		ContractManagerTeam contractManager = this.mTeam.contractManager;
		contractManager.PromoteDriver(Demoted, Promoted);
	}

	private bool IsPayDriverWorthIt(Driver pay_driver)
	{
		if (pay_driver != null)
		{
			int tieredPayDriverAmount = pay_driver.personalityTraitController.GetTieredPayDriverAmount();
			int num = Mathf.RoundToInt((float)this.mTeam.financeController.racePayment * 0.1f);
			return tieredPayDriverAmount > num;
		}
		return false;
	}

	private float CalculateYearlyWage(ref ContractDesiredValuesHelper lDesiredValuesHelper, float current_wages)
	{
		if (lDesiredValuesHelper == null)
		{
			return 0f;
		}
		float num = (lDesiredValuesHelper.maximumOfferableWage - lDesiredValuesHelper.minimumOfferableWage) / 10f;
		float baseDesiredWages = lDesiredValuesHelper.baseDesiredWages;
		float num2 = lDesiredValuesHelper.desiredWages;
		if (current_wages - num2 > num)
		{
			return current_wages;
		}
		if (current_wages > num2)
		{
			num2 = current_wages;
		}
		if (num2 < baseDesiredWages)
		{
			return num2 + num;
		}
		int num3 = Mathf.CeilToInt((num2 - baseDesiredWages) / num);
		float num4 = baseDesiredWages + (float)num3 * num;
		if (num4 - num2 <= num * 0.5f)
		{
			num4 += num;
		}
		return Mathf.Min(num4, lDesiredValuesHelper.maximumOfferableWage);
	}

	private void ReplaceDriver(Driver inCurDriver, Driver inNewDriver, bool inIsReplacement, bool inReplaceWithReserve)
	{
		if (!inNewDriver.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inNewDriver);
			contractPerson.job = Contract.Job.Driver;
			Team team = inNewDriver.contract.GetTeam();
			ContractNegotiationScreen.NegotatiationType negotatiationType = (!inNewDriver.IsFreeAgent()) ? ContractNegotiationScreen.NegotatiationType.NewDriver : ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed;
			if (inReplaceWithReserve)
			{
				negotatiationType = ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver;
			}
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inNewDriver,
				mPersonToFire = this.mTeam.contractManager.GetSlotForPerson(inCurDriver),
				mDraftContractPerson = contractPerson,
				mNegotiationType = negotatiationType,
				mTeamAtTimeOfNegotiation = team,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = inIsReplacement
			};
			this.mNegotiations.Add(negotiationEntry);
			inNewDriver.contractManager.StartNegotiation(negotatiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inNewDriver.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.SetProposedStatus(inCurDriver.contract.currentStatus);
			float current_wages = (!inNewDriver.IsFreeAgent()) ? ((float)inNewDriver.contract.yearlyWages) : 0f;
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, current_wages);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inNewDriver.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			if (inIsReplacement && !inReplaceWithReserve)
			{
				this.UpdatePeopleApproached(inNewDriver, flag);
			}
			this.UpdateProposedContractState(inNewDriver, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inNewDriver.contractManager.RemoveDraftProposal();
		}
	}

	private bool ShouldRenew(Driver inDriver)
	{
		return this.mSeasonWeight > 0f && !inDriver.contract.IsContractedForNextSeason() && inDriver.GetInterestedToTalkReaction(this.mTeam) == Person.InterestedToTalkResponseType.InterestedToTalk;
	}

	private void Renew(Driver inDriver)
	{
		if (!inDriver.contract.IsContractedForNextSeason() && !inDriver.contractManager.isNegotiating && !inDriver.contractManager.isConsideringProposal)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inDriver);
			contractPerson.job = Contract.Job.Driver;
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inDriver,
				mPersonToFire = null,
				mDraftContractPerson = contractPerson,
				mNegotiationType = ContractNegotiationScreen.NegotatiationType.RenewDriver,
				mTeamAtTimeOfNegotiation = this.mTeam,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = false
			};
			this.mNegotiations.Add(negotiationEntry);
			inDriver.contractManager.StartNegotiation(negotiationEntry.mNegotiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inDriver.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.SetProposedStatus(inDriver.contract.currentStatus);
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, (float)inDriver.contract.yearlyWages);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inDriver.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			this.UpdateProposedContractState(inDriver, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inDriver.contractManager.RemoveDraftProposal();
		}
	}

	private bool AllowTeamToPoachDriver(Team inTeam, Driver inDriver)
	{
		float num = this.GetBaseStaffRetainChance(inTeam);
		float num2 = (float)inDriver.GetDriverStats().totalStatsMax;
		bool flag = true;
		this.mDrivers.Clear();
		this.mTeam.contractManager.GetAllDrivers(ref this.mDrivers);
		for (int i = 0; i < this.mDrivers.Count; i++)
		{
			Driver driver = this.mDrivers[i];
			if (driver != inDriver && (float)driver.GetDriverStats().totalStatsMax > num2)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			num += 0.2f;
		}
		return RandomUtility.GetRandom01() < num;
	}

	private bool ShouldFire(Driver inDriver)
	{
		return RandomUtility.GetRandom01() < this.mChanceOfFiring * this.mSeasonWeight;
	}

	private bool AllowToLeave(Driver inDriver)
	{
		return RandomUtility.GetRandom01() * this.mSeasonWeight > this.mTeam.aiWeightings.mStaffRetention;
	}

	public void HandleEngineerChanges()
	{
		List<Engineer> allPeopleOnJob = this.mTeam.contractManager.GetAllPeopleOnJob<Engineer>(Contract.Job.EngineerLead);
		this.mPeopleApproachedAndRejectedBy.Clear();
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			Engineer lCurEngineer = allPeopleOnJob[i];
			TeamAIController.NegotiationEntry negotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => (x.mPersonToFire != null && x.mPersonToFire.personHired == lCurEngineer) || x.mPerson == lCurEngineer);
			bool flag = lCurEngineer.IsReplacementPerson();
			bool flag2 = flag || (Game.instance.time.now - lCurEngineer.contract.startDate).Days > TeamAIController.hireStaffCooldownDays;
			bool flag3 = false;
			if (negotiationEntry == null && flag2)
			{
				bool flag4 = false;
				if (flag || this.ShouldFire(lCurEngineer))
				{
					flag4 = true;
					flag3 = !flag;
				}
				else if (lCurEngineer.WantsToLeave())
				{
					if (this.AllowToLeave(lCurEngineer))
					{
						flag4 = true;
					}
				}
				else if (this.mTeam.championship.InPreseason() && Game.instance.time.now.Month > 6 && !lCurEngineer.contract.IsContractedForNextSeason())
				{
					if (this.ShouldRenew(lCurEngineer))
					{
						this.Renew(lCurEngineer);
					}
					else
					{
						flag4 = true;
					}
				}
				if (flag4)
				{
					Engineer engineer = this.FindReplacementEngineer(allPeopleOnJob[i]);
					if (engineer != null)
					{
						this.ReplaceEngineer(lCurEngineer, engineer, flag);
						if (flag3)
						{
							this.mLastFiringUpdateTime = Game.instance.time.now;
							this.mChanceOfFiring = 0f;
						}
					}
				}
			}
		}
		this.mScoutableEngineers.Clear();
	}

	private Engineer FindReplacementEngineer(Engineer current_engineer)
	{
		long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
		List<Engineer> entityList = Game.instance.engineerManager.GetEntityList();
		this.mScoutableEngineers.Clear();
		for (int i = 0; i < entityList.Count; i++)
		{
			Engineer engineer = entityList[i];
			if (this.IsScoutable(engineer) && !current_engineer.Equals(engineer) && this.PersonNotAlreadyApproached(engineer) && engineer.GetInterestedToTalkReaction(this.mTeam) == Person.InterestedToTalkResponseType.InterestedToTalk && (engineer.IsFreeAgent() || num > (long)engineer.contract.GetContractTerminationCost()))
			{
				this.mScoutableEngineers.Add(engineer);
			}
		}
		if (this.mScoutableEngineers.Count > 1)
		{
			Person.SortByAbility<Engineer>(this.mScoutableEngineers, false);
			return this.mScoutableEngineers[0];
		}
		return null;
	}

	private void ReplaceEngineer(Engineer inCurEngineer, Engineer inNewEngineer, bool inIsReplacement)
	{
		if (!inNewEngineer.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inNewEngineer);
			contractPerson.job = Contract.Job.EngineerLead;
			Team team = inNewEngineer.contract.GetTeam();
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inNewEngineer,
				mPersonToFire = this.mTeam.contractManager.GetSlotForPerson(inCurEngineer),
				mDraftContractPerson = contractPerson,
				mNegotiationType = ((!inNewEngineer.IsFreeAgent()) ? ContractNegotiationScreen.NegotatiationType.NewStaffEmployed : ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed),
				mTeamAtTimeOfNegotiation = team,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = inIsReplacement
			};
			this.mNegotiations.Add(negotiationEntry);
			inNewEngineer.contractManager.StartNegotiation(negotiationEntry.mNegotiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inNewEngineer.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.SetProposedStatus(inCurEngineer.contract.currentStatus);
			float current_wages = (!inNewEngineer.IsFreeAgent()) ? ((float)inNewEngineer.contract.yearlyWages) : 0f;
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, current_wages);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inNewEngineer.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			this.UpdatePeopleApproached(inNewEngineer, flag);
			this.UpdateProposedContractState(inNewEngineer, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inNewEngineer.contractManager.RemoveDraftProposal();
		}
	}

	private Engineer ChooseReplacementEngineer(Engineer inEngineer)
	{
		this.mScoutingManager.RemoveEngineerFromScoutingJobs(inEngineer);
		float num = (!inEngineer.IsReplacementPerson()) ? inEngineer.GetStatsValue() : 0f;
		for (int i = 0; i < this.mScoutingManager.engineerScoutingAssigmentsCompleteCount; i++)
		{
			Engineer engineer = this.mScoutingManager.GetCompletedEngineer(i).engineer;
			if (this.IsScoutable(engineer) && !engineer.contractManager.isNegotiating && engineer.GetStatsValue() >= num && this.PersonNotAlreadyApproached(engineer))
			{
				return engineer;
			}
		}
		return null;
	}

	private bool ShouldFire(Engineer inEngineer)
	{
		return RandomUtility.GetRandom01() < this.mChanceOfFiring * this.mSeasonWeight;
	}

	private bool AllowToLeave(Engineer inEngineer)
	{
		return RandomUtility.GetRandom01() * this.mSeasonWeight > this.mTeam.aiWeightings.mStaffRetention;
	}

	private bool ShouldRenew(Engineer inEngineer)
	{
		return this.mSeasonWeight > 0f && !inEngineer.contract.IsContractedForNextSeason();
	}

	private void Renew(Engineer inEngineer)
	{
		if (!inEngineer.contract.IsContractedForNextSeason() && !inEngineer.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inEngineer);
			contractPerson.job = Contract.Job.EngineerLead;
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inEngineer,
				mPersonToFire = null,
				mDraftContractPerson = contractPerson,
				mNegotiationType = ContractNegotiationScreen.NegotatiationType.RenewStaff,
				mTeamAtTimeOfNegotiation = this.mTeam,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = false
			};
			this.mNegotiations.Add(negotiationEntry);
			inEngineer.contractManager.StartNegotiation(negotiationEntry.mNegotiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inEngineer.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, (float)inEngineer.contract.yearlyWages);
			contractPerson.SetProposedStatus(inEngineer.contract.currentStatus);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inEngineer.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			this.UpdateProposedContractState(inEngineer, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inEngineer.contractManager.RemoveDraftProposal();
		}
	}

	public void HandleMechanicChanges()
	{
		List<Mechanic> allPeopleOnJob = this.mTeam.contractManager.GetAllPeopleOnJob<Mechanic>(Contract.Job.Mechanic);
		this.mPeopleApproachedAndRejectedBy.Clear();
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			Mechanic lCurMechanic = allPeopleOnJob[i];
			TeamAIController.NegotiationEntry negotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => (x.mPersonToFire != null && x.mPersonToFire.personHired == lCurMechanic) || x.mPerson == lCurMechanic);
			bool flag = lCurMechanic.IsReplacementPerson();
			bool flag2 = flag || (Game.instance.time.now - lCurMechanic.contract.startDate).Days > TeamAIController.hireStaffCooldownDays;
			bool flag3 = false;
			if (negotiationEntry == null && flag2)
			{
				bool flag4 = false;
				if (flag || this.ShouldFire(lCurMechanic))
				{
					flag4 = true;
					flag3 = !flag;
				}
				else if (lCurMechanic.WantsToLeave())
				{
					if (this.AllowToLeave(lCurMechanic))
					{
						flag4 = true;
					}
				}
				else if (this.mTeam.championship.InPreseason() && Game.instance.time.now.Month > 6 && !lCurMechanic.contract.IsContractedForNextSeason())
				{
					if (this.ShouldRenew(lCurMechanic))
					{
						this.Renew(lCurMechanic);
					}
					else
					{
						flag4 = true;
					}
				}
				if (flag4)
				{
					Mechanic mechanic = this.FindReplacementMechanic(lCurMechanic);
					if (mechanic != null)
					{
						this.ReplaceMechanic(lCurMechanic, mechanic, flag);
						if (flag3)
						{
							this.mLastFiringUpdateTime = Game.instance.time.now;
							this.mChanceOfFiring = 0f;
						}
					}
				}
			}
		}
		this.mScoutableMechanics.Clear();
	}

	private Mechanic FindReplacementMechanic(Mechanic current_mechanic)
	{
		long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
		List<Mechanic> entityList = Game.instance.mechanicManager.GetEntityList();
		this.mScoutableMechanics.Clear();
		for (int i = 0; i < entityList.Count; i++)
		{
			Mechanic mechanic = entityList[i];
			if (this.IsScoutable(mechanic) && !current_mechanic.Equals(mechanic) && this.PersonNotAlreadyApproached(mechanic) && mechanic.GetInterestedToTalkReaction(this.mTeam) == Person.InterestedToTalkResponseType.InterestedToTalk && (mechanic.IsFreeAgent() || num > (long)mechanic.contract.GetContractTerminationCost()))
			{
				this.mScoutableMechanics.Add(mechanic);
			}
		}
		if (this.mScoutableMechanics.Count > 1)
		{
			Person.SortByAbility<Mechanic>(this.mScoutableMechanics, false);
			return this.mScoutableMechanics[0];
		}
		return null;
	}

	private void ReplaceMechanic(Mechanic inCurMechanic, Mechanic inNewMechanic, bool inIsReplacement)
	{
		if (!inNewMechanic.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inNewMechanic);
			contractPerson.job = Contract.Job.Mechanic;
			Team team = inNewMechanic.contract.GetTeam();
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inNewMechanic,
				mPersonToFire = this.mTeam.contractManager.GetSlotForPerson(inCurMechanic),
				mDraftContractPerson = contractPerson,
				mNegotiationType = ((!inNewMechanic.IsFreeAgent()) ? ContractNegotiationScreen.NegotatiationType.NewStaffEmployed : ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed),
				mTeamAtTimeOfNegotiation = team,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = inIsReplacement
			};
			this.mNegotiations.Add(negotiationEntry);
			inNewMechanic.contractManager.StartNegotiation(negotiationEntry.mNegotiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inNewMechanic.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.SetProposedStatus(inCurMechanic.contract.currentStatus);
			float current_wages = (!inNewMechanic.IsFreeAgent()) ? ((float)inNewMechanic.contract.yearlyWages) : 0f;
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, current_wages);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inNewMechanic.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			this.UpdatePeopleApproached(inNewMechanic, flag);
			this.UpdateProposedContractState(inNewMechanic, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inNewMechanic.contractManager.RemoveDraftProposal();
		}
	}

	private Mechanic ChooseReplacementMechanic(Mechanic inMechanic)
	{
		this.mScoutingManager.RemoveMechanicFromScoutingJobs(inMechanic);
		float num = (!inMechanic.IsReplacementPerson()) ? inMechanic.GetStatsValue() : 0f;
		for (int i = 0; i < this.mScoutingManager.mechanicScoutingAssigmentsCompleteCount; i++)
		{
			Mechanic mechanic = this.mScoutingManager.GetCompletedMechanic(i).mechanic;
			if (this.IsScoutable(mechanic) && !mechanic.contractManager.isNegotiating && mechanic.GetStatsValue() >= num && this.PersonNotAlreadyApproached(mechanic))
			{
				return mechanic;
			}
		}
		return null;
	}

	private bool ShouldFire(Mechanic inMechanic)
	{
		return RandomUtility.GetRandom01() < this.mChanceOfFiring * this.mSeasonWeight;
	}

	private bool AllowToLeave(Mechanic inMechanic)
	{
		return RandomUtility.GetRandom01() * this.mSeasonWeight > this.mTeam.aiWeightings.mStaffRetention;
	}

	private bool ShouldRenew(Mechanic inMechanic)
	{
		return this.mSeasonWeight > 0f && !inMechanic.contract.IsContractedForNextSeason();
	}

	private void Renew(Mechanic inMechanic)
	{
		if (!inMechanic.contract.IsContractedForNextSeason() && !inMechanic.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inMechanic);
			contractPerson.job = Contract.Job.Mechanic;
			TeamAIController.NegotiationEntry negotiationEntry = new TeamAIController.NegotiationEntry
			{
				mPerson = inMechanic,
				mPersonToFire = null,
				mDraftContractPerson = contractPerson,
				mNegotiationType = ContractNegotiationScreen.NegotatiationType.RenewStaff,
				mTeamAtTimeOfNegotiation = this.mTeam,
				mLastNegotiatedWith = Game.instance.time.now,
				mIsReplacementPerson = false
			};
			this.mNegotiations.Add(negotiationEntry);
			inMechanic.contractManager.StartNegotiation(negotiationEntry.mNegotiationType, this.mTeam);
			ContractDesiredValuesHelper desiredContractValues = inMechanic.contractManager.contractEvaluation.desiredContractValues;
			contractPerson.hasQualifyingBonus = desiredContractValues.wantsQualifyingBonus;
			contractPerson.hasRaceBonus = desiredContractValues.wantsRaceBonus;
			contractPerson.hasSignOnFee = desiredContractValues.wantSignOnFee;
			contractPerson.signOnFee = (int)desiredContractValues.desiredSignOnFee;
			int num = 0;
			int num2 = 0;
			this.CalculateDesiredBonus(desiredContractValues.desiredQualifyingBonus[0], desiredContractValues.desiredQualifyingBonus[1], out num, out num2);
			contractPerson.qualifyingBonusTargetPosition = num;
			contractPerson.qualifyingBonus = num2;
			this.CalculateDesiredBonus(desiredContractValues.desiredRaceBonus[0], desiredContractValues.desiredRaceBonus[1], out num, out num2);
			contractPerson.raceBonusTargetPosition = num;
			contractPerson.raceBonus = num2;
			contractPerson.buyoutSplit = desiredContractValues.desiredBuyoutSplit;
			contractPerson.SetProposedStatus(inMechanic.contract.currentStatus);
			contractPerson.yearlyWages = (int)this.CalculateYearlyWage(ref desiredContractValues, (float)inMechanic.contract.yearlyWages);
			contractPerson.length = desiredContractValues.desiredContractLength;
			contractPerson.startDate = Game.instance.time.now;
			DateTime endDate = new DateTime(contractPerson.startDate.Year, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				endDate = endDate.AddYears(1);
			}
			if (contractPerson.length == ContractPerson.ContractLength.Medium)
			{
				endDate = endDate.AddYears(1);
			}
			else if (contractPerson.length == ContractPerson.ContractLength.Long)
			{
				endDate = endDate.AddYears(2);
			}
			contractPerson.endDate = endDate;
			bool flag = inMechanic.contractManager.IsAcceptingDraftContractAI(contractPerson, negotiationEntry.mNegotiationType);
			this.UpdateProposedContractState(inMechanic, (!flag) ? ContractManagerPerson.ContractProposalState.ProposalRejected : ContractManagerPerson.ContractProposalState.ProposalAccepted);
			inMechanic.contractManager.RemoveDraftProposal();
		}
	}

	private void HandleTeamPrincipalChanges()
	{
		TeamPrincipal teamPrincipal = this.mTeam.teamPrincipal;
		bool flag = teamPrincipal.IsReplacementPerson();
		if (flag)
		{
			TeamPrincipal teamPrincipal2 = this.ChooseReplacementTeamPrincipal(teamPrincipal);
			if (teamPrincipal2 != null)
			{
				this.ReplaceTeamPrincipal(teamPrincipal, teamPrincipal2);
			}
		}
	}

	private TeamPrincipal ChooseReplacementTeamPrincipal(TeamPrincipal inTeamPrincipalr)
	{
		List<TeamPrincipal> entityList = Game.instance.teamPrincipalManager.GetEntityList();
		for (int i = 0; i < entityList.Count; i++)
		{
			TeamPrincipal teamPrincipal = entityList[i];
			if (teamPrincipal.IsFreeAgent() && teamPrincipal.canJoinTeamAI(this.mTeam))
			{
				return teamPrincipal;
			}
		}
		return null;
	}

	private void ReplaceTeamPrincipal(TeamPrincipal inCurTeamPrincipal, TeamPrincipal inNewTeamPrincipal)
	{
		if (!inNewTeamPrincipal.contractManager.isNegotiating)
		{
			ContractPerson contractPerson = new ContractPerson();
			contractPerson.employeer = this.mTeam;
			contractPerson.SetPerson(inNewTeamPrincipal);
			contractPerson.job = Contract.Job.TeamPrincipal;
			contractPerson.startDate = Game.instance.time.now;
			contractPerson.endDate = new DateTime(contractPerson.startDate.Year + 2, 12, 31);
			if (this.NeedToAddOneYearToContract())
			{
				contractPerson.endDate = contractPerson.endDate.AddYears(1);
			}
			this.mTeam.contractManager.ReplacePersonWithNewOne(contractPerson, inNewTeamPrincipal, inCurTeamPrincipal);
		}
	}

	private void CalculateDesiredBonus(float min, float max, out int target_pos, out int desired_bonus)
	{
		target_pos = UnityEngine.Random.Range(1, 11);
		float t = 1f - (float)target_pos / 10f;
		float num = (float)((int)Mathf.Lerp(min, max, t));
		num += max * 0.5f;
		desired_bonus = (int)num;
	}

	private void UpdatePeopleApproached(Person inPerson, bool inAccepted)
	{
		if (inAccepted)
		{
			this.mPeopleApproachedAndRejectedBy.Clear();
		}
		else
		{
			this.mPeopleApproachedAndRejectedBy.Add(inPerson);
		}
	}

	private bool PersonNotAlreadyApproached(Person inPerson)
	{
		for (int i = 0; i < this.mPeopleApproachedAndRejectedBy.Count; i++)
		{
			if (inPerson.Equals(this.mPeopleApproachedAndRejectedBy[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void UpdateProposedContractState(Person inPerson, ContractManagerPerson.ContractProposalState inContractState)
	{
		TeamAIController.NegotiationEntry lNegotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => x.mPerson == inPerson);
		if (lNegotiationEntry == null)
		{
			return;
		}
		if (inContractState == ContractManagerPerson.ContractProposalState.ProposalAccepted)
		{
			Action inOnTransactionSucess = delegate()
			{
				this.ConfirmNewHire(lNegotiationEntry);
			};
			if (inPerson is Driver)
			{
				switch (lNegotiationEntry.mNegotiationType)
				{
				case ContractNegotiationScreen.NegotatiationType.NewDriver:
				case ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed:
				{
					List<Transaction> list = new List<Transaction>();
					StringVariableParser.stringValue1 = inPerson.shortName;
					Transaction transaction = new Transaction(inPerson.GetTransactionType(), Transaction.Type.Debit, lNegotiationEntry.mDraftContractPerson.amountForContractorToPay, Localisation.LocaliseID("PSG_10010574", null));
					StringVariableParser.stringValue1 = inPerson.shortName;
					Transaction transaction2 = new Transaction(inPerson.GetTransactionType(), Transaction.Type.Debit, lNegotiationEntry.mDraftContractPerson.signOnFee, Localisation.LocaliseID("PSG_10010575", null));
					Finance.AddTransactionsIfNotZero(list, new Transaction[]
					{
						transaction,
						transaction2
					});
					Person personHired = lNegotiationEntry.mPersonToFire.personHired;
					if (personHired != null)
					{
						StringVariableParser.stringValue1 = personHired.shortName;
						Transaction transaction3 = new Transaction(personHired.GetTransactionType(), Transaction.Type.Debit, personHired.contract.GetContractTerminationCost(), Localisation.LocaliseID("PSG_10010574", null));
						Finance.AddTransactionsIfNotZero(list, new Transaction[]
						{
							transaction3
						});
						this.mTeam.financeController.finance.ProcessTransactions(inOnTransactionSucess, null, false, list.ToArray());
					}
					else
					{
						this.mTeam.financeController.finance.ProcessTransactions(inOnTransactionSucess, null, false, list.ToArray());
					}
					break;
				}
				case ContractNegotiationScreen.NegotatiationType.RenewDriver:
					this.mTeam.contractManager.RenewContractForPerson(inPerson, lNegotiationEntry.mDraftContractPerson);
					break;
				case ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver:
					this.ConfirmNewHire(lNegotiationEntry);
					break;
				}
			}
			else
			{
				switch (lNegotiationEntry.mNegotiationType)
				{
				case ContractNegotiationScreen.NegotatiationType.NewStaffEmployed:
				case ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed:
				{
					List<Transaction> list2 = new List<Transaction>();
					StringVariableParser.stringValue1 = inPerson.shortName;
					Transaction transaction4 = new Transaction(inPerson.GetTransactionType(), Transaction.Type.Debit, lNegotiationEntry.mDraftContractPerson.amountForContractorToPay, Localisation.LocaliseID("PSG_10010574", null));
					StringVariableParser.stringValue1 = inPerson.shortName;
					Transaction transaction5 = new Transaction(inPerson.GetTransactionType(), Transaction.Type.Debit, lNegotiationEntry.mDraftContractPerson.signOnFee, Localisation.LocaliseID("PSG_10010575", null));
					Finance.AddTransactionsIfNotZero(list2, new Transaction[]
					{
						transaction4,
						transaction5
					});
					Person personHired2 = lNegotiationEntry.mPersonToFire.personHired;
					if (personHired2 != null)
					{
						StringVariableParser.stringValue1 = personHired2.shortName;
						Transaction transaction6 = new Transaction(personHired2.GetTransactionType(), Transaction.Type.Debit, personHired2.contract.GetContractTerminationCost(), Localisation.LocaliseID("PSG_10010574", null));
						Finance.AddTransactionsIfNotZero(list2, new Transaction[]
						{
							transaction6
						});
						this.mTeam.financeController.finance.ProcessTransactions(inOnTransactionSucess, null, false, list2.ToArray());
					}
					else
					{
						this.mTeam.financeController.finance.ProcessTransactions(inOnTransactionSucess, null, false, list2.ToArray());
					}
					break;
				}
				case ContractNegotiationScreen.NegotatiationType.RenewStaff:
					this.mTeam.contractManager.RenewContractForPerson(inPerson, lNegotiationEntry.mDraftContractPerson);
					break;
				}
			}
		}
		else if (inContractState == ContractManagerPerson.ContractProposalState.ProposalRejected && lNegotiationEntry.mIsReplacementPerson)
		{
			Person personHired3 = lNegotiationEntry.mPersonToFire.personHired;
			if (personHired3 is Driver)
			{
				Driver driver = personHired3 as Driver;
				Driver driver2 = this.FindReplacementDriver(driver, false, 0f);
				if (driver2 != null)
				{
					this.ReplaceDriver(driver, driver2, true, false);
				}
			}
			else if (inPerson is Engineer)
			{
				Engineer engineer = personHired3 as Engineer;
				Engineer engineer2 = this.FindReplacementEngineer(engineer);
				if (engineer2 != null)
				{
					this.ReplaceEngineer(engineer, engineer2, true);
				}
			}
			else if (inPerson is Mechanic)
			{
				Mechanic mechanic = personHired3 as Mechanic;
				Mechanic mechanic2 = this.FindReplacementMechanic(mechanic);
				if (mechanic2 != null)
				{
					this.ReplaceMechanic(mechanic, mechanic2, true);
				}
			}
		}
		this.mNegotiations.Remove(lNegotiationEntry);
	}

	private void ConfirmNewHire(TeamAIController.NegotiationEntry inNegotiationEntry)
	{
		switch (inNegotiationEntry.mNegotiationType)
		{
		case ContractNegotiationScreen.NegotatiationType.NewDriver:
		case ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed:
			this.ConfirmNewHireDriver(inNegotiationEntry);
			break;
		case ContractNegotiationScreen.NegotatiationType.NewStaffEmployed:
		case ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed:
		{
			ContractManagerTeam contractManager = this.mTeam.contractManager;
			contractManager.ReplacePersonWithNewOne(inNegotiationEntry.mDraftContractPerson, inNegotiationEntry.mPerson, inNegotiationEntry.mPersonToFire.personHired);
			break;
		}
		case ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver:
			this.ConfirmPromoteDriver(inNegotiationEntry);
			break;
		}
	}

	private void ConfirmNewHireDriver(TeamAIController.NegotiationEntry inNegotiationEntry)
	{
		ContractManagerTeam contractManager = this.mTeam.contractManager;
		if (inNegotiationEntry.mPersonToFire != null && inNegotiationEntry.mPersonToFire.personHired != null)
		{
			contractManager.ReplaceCurrentDriverWithNewOne(inNegotiationEntry.mDraftContractPerson, inNegotiationEntry.mPerson, inNegotiationEntry.mPersonToFire.personHired);
		}
		else
		{
			StringVariableParser.personReplaced = null;
			contractManager.HireNewDriver(inNegotiationEntry.mDraftContractPerson, inNegotiationEntry.mPerson);
		}
		this.EvaluateDriverLineUp();
	}

	private void ConfirmPromoteDriver(TeamAIController.NegotiationEntry inNegotiationEntry)
	{
		ContractManagerTeam contractManager = this.mTeam.contractManager;
		contractManager.PromoteDriver(inNegotiationEntry.mPersonToFire.personHired, inNegotiationEntry.mPerson);
		this.EvaluateDriverLineUp();
	}

	public void RemovePersonFromNegotations(Person inPerson)
	{
		TeamAIController.NegotiationEntry negotiationEntry = this.mNegotiations.Find((TeamAIController.NegotiationEntry x) => (x.mPersonToFire != null && x.mPersonToFire.personHired == inPerson) || x.mPerson == inPerson);
		if (negotiationEntry != null)
		{
			this.mNegotiations.Remove(negotiationEntry);
		}
	}

	public void ScoutDrivers()
	{
		if ((Game.instance.time.now - this.mLastDriverScoutTime).Days > 7)
		{
			long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
			int num2 = TeamAIController.minScoutDriverAge;
			int num3 = TeamAIController.maxScoutDriverAge;
			HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.ScoutingFacility);
			int num4 = 0;
			if (building != null && building.isBuilt)
			{
				num4 = building.currentLevel;
			}
			List<Driver> list = new List<Driver>();
			List<Driver> entityList = Game.instance.driverManager.GetEntityList();
			for (int i = 0; i < entityList.Count; i++)
			{
				Driver driver = entityList[i];
				if (this.IsScoutable(driver) && (driver.GetDriverStats().scoutingLevelRequired == 0 || num4 > driver.GetDriverStats().scoutingLevelRequired))
				{
					int age = driver.GetAge();
					if (age >= num2 && age <= num3)
					{
						long num5 = 0L;
						if (!driver.IsFreeAgent())
						{
							num5 = (long)driver.contract.GetContractTerminationCost();
						}
						if ((num5 == 0L || num > num5) && !this.mScoutingManager.IsDriverCurrentlyScouted(driver) && !this.mScoutingManager.IsDriverInScoutQueue(driver))
						{
							list.Add(driver);
						}
					}
				}
			}
			if (list.Count > 1)
			{
				Person.SortByRaceCost<Driver>(list, true);
			}
			for (int j = 0; j < list.Count; j++)
			{
				Driver inDriverToScout = list[j];
				this.mScoutingManager.AddScoutingAssignment(inDriverToScout);
			}
			this.mLastDriverScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-5, 5));
		}
		this.mScoutingManager.UpdatedCompletedScoutsList();
	}

	public void ScoutEngineers()
	{
		if ((Game.instance.time.now - this.mLastEngineerScoutTime).Days > 7)
		{
			long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
			int num2 = TeamAIController.minScoutEngineerAge;
			int num3 = TeamAIController.maxScoutEngineerAge;
			List<Engineer> list = new List<Engineer>();
			List<Engineer> entityList = Game.instance.engineerManager.GetEntityList();
			for (int i = 0; i < entityList.Count; i++)
			{
				Engineer engineer = entityList[i];
				if (this.IsScoutable(engineer))
				{
					int age = engineer.GetAge();
					if (age >= num2 && age <= num3)
					{
						long num4 = 0L;
						if (!engineer.IsFreeAgent())
						{
							num4 = (long)engineer.contract.GetContractTerminationCost();
						}
						if (num > num4 && !this.mScoutingManager.IsEngineerCurrentlyScouted(engineer) && !this.mScoutingManager.IsEngineerInScoutQueue(engineer))
						{
							list.Add(engineer);
						}
					}
				}
			}
			if (list.Count > 1)
			{
				Person.SortByRaceCost<Engineer>(list, true);
			}
			for (int j = 0; j < list.Count; j++)
			{
				Engineer inEngineerToScout = list[j];
				this.mScoutingManager.AddScoutingAssignment(inEngineerToScout);
			}
			this.mLastEngineerScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-5, 5));
		}
		this.mScoutingManager.UpdatedCompletedScoutsList();
	}

	public void ScoutMechanics()
	{
		if ((Game.instance.time.now - this.mLastMechanicScoutTime).Days > 7)
		{
			long num = (long)((float)this.mTeam.financeController.finance.currentBudget * this.mTeam.aiWeightings.mFinanceDrivers);
			int num2 = TeamAIController.minScoutMechanicAge;
			int num3 = TeamAIController.maxScoutMechanicAge;
			List<Mechanic> list = new List<Mechanic>();
			List<Mechanic> entityList = Game.instance.mechanicManager.GetEntityList();
			for (int i = 0; i < entityList.Count; i++)
			{
				Mechanic mechanic = entityList[i];
				if (this.IsScoutable(mechanic))
				{
					int age = mechanic.GetAge();
					if (age >= num2 && age <= num3)
					{
						long num4 = 0L;
						if (!mechanic.IsFreeAgent())
						{
							num4 = (long)mechanic.contract.GetContractTerminationCost();
						}
						if (num > num4 && !this.mScoutingManager.IsMechanicCurrentlyScouted(mechanic) && !this.mScoutingManager.IsMechanicInScoutQueue(mechanic))
						{
							list.Add(mechanic);
						}
					}
				}
			}
			if (list.Count > 1)
			{
				Person.SortByRaceCost<Mechanic>(list, true);
			}
			for (int j = 0; j < list.Count; j++)
			{
				Mechanic inMechanicToScout = list[j];
				this.mScoutingManager.AddScoutingAssignment(inMechanicToScout);
			}
			this.mLastMechanicScoutTime = Game.instance.time.now.AddDays((double)RandomUtility.GetRandom(-5, 5));
		}
		this.mScoutingManager.UpdatedCompletedScoutsList();
	}

	public bool IsScoutable(Person inPerson)
	{
		bool flag = !inPerson.IsReplacementPerson() && !inPerson.HasRetired();
		bool flag2 = inPerson.IsFreeAgent();
		bool flag3 = false;
		if (!flag2)
		{
			flag2 = ((Game.instance.time.now - inPerson.contract.startDate).Days > TeamAIController.hireStaffCooldownDays);
			if (inPerson.contract.GetTeam().IsPlayersTeam())
			{
				flag3 = inPerson.IsOpenToOffers();
			}
			else
			{
				flag3 = (inPerson.WantsToLeave() || inPerson.IsOpenToOffers());
			}
		}
		return flag && flag2 && (inPerson.IsFreeAgent() || (inPerson.contract.GetTeam().championship.isChoosable && inPerson.contract.GetTeam() != this.mTeam && flag3));
	}

	public void RemoveDriverFromScoutingJobs(Driver inDriver)
	{
		this.mScoutingManager.RemoveDriverFromScoutingJobs(inDriver);
	}

	public void RemoveEngineerFromScoutingJobs(Engineer inEngineer)
	{
		this.mScoutingManager.RemoveEngineerFromScoutingJobs(inEngineer);
	}

	public void RemoveMechanicFromScoutingJobs(Mechanic inMechanic)
	{
		this.mScoutingManager.RemoveMechanicFromScoutingJobs(inMechanic);
	}

	private bool NeedToAddOneYearToContract()
	{
		bool flag = this.mTeam.championship.HasSeasonEnded();
		flag |= (App.instance.gameStateManager.currentState.type == GameState.Type.PreSeasonState && Game.instance.time.now.Month == 12);
		return flag | (!this.mTeam.championship.GetFirstEventDetails().hasEventEnded && Game.instance.time.now.Year < this.mTeam.championship.GetFirstEventDetails().eventDate.Year);
	}

	private float GetBaseStaffRetainChance(Team inTeam)
	{
		float num = this.mTeam.aiWeightings.mStaffRetention;
		if (inTeam == this.mTeam.rivalTeam)
		{
			num += 0.2f;
		}
		return num;
	}

	private bool AllowTeamToPoachPerson(Team inTeam, Person inPerson)
	{
		float baseStaffRetainChance = this.GetBaseStaffRetainChance(inTeam);
		return RandomUtility.GetRandom01() < baseStaffRetainChance;
	}

	private void UpdateChanceOfFiring()
	{
		if (this.mTeam.championship.IsDateInPreseason(Game.instance.time.now) && Game.instance.time.now.Month > 10 && (Game.instance.time.now - this.mLastFiringUpdateTime).Days > TeamAIController.fireStaffDelayDays)
		{
			int num = this.endOfSeasonPosition - this.expectedEndOfSeasonPosition;
			this.mChanceOfFiring = 0f;
			if (num > 1)
			{
				this.mChanceOfFiring = (float)num * TeamAIController.firingChanceScalar * this.mTeam.aiWeightings.mAggressiveness * (1f - Mathf.Clamp(this.mTeam.aiWeightings.mStaffRetention, 0f, 0.9f));
				this.mChanceOfFiring = Mathf.Clamp01(this.mChanceOfFiring);
			}
		}
	}

	private const float STARS_FOR_BETTER = 0.75f;

	private Team mTeam;

	private AIScoutingManager mScoutingManager;

	private List<TeamAIController.NegotiationEntry> mNegotiations;

	private DateTime mLastDriverScoutTime;

	private DateTime mLastEngineerScoutTime;

	private DateTime mLastMechanicScoutTime;

	private DateTime mLastHQUpdateTime;

	private DateTime mLastCarUpdateTime;

	private DateTime mLastFiringUpdateTime;

	private List<Driver> mDrivers;

	private float mChanceOfFiring;

	private float mSeasonWeight;

	private int expectedEndOfSeasonPosition;

	private int endOfSeasonPosition;

	private List<Driver> mDriversTeamHasAttemptedToRenewContractWith;

	private List<Person> mPeopleApproachedAndRejectedBy;

	private List<int> mImproveCarPartsList;

	private List<int> mImproveCarPartsListOther;

	private List<CarPart> mImproveCarPartsMostRecentParts;

	public List<HQsBuildingInfo.Type> mHQTargetsList;

	public List<HQsBuildingInfo.Type> mHQHistoryList;

	[NonSerialized]
	public List<TeamAIController.HQBuildingValue> mPotentialHQTargets;

	private static readonly int minScoutDriverAge = 21;

	private static readonly int maxScoutDriverAge = 31;

	private static readonly int minScoutEngineerAge = 22;

	private static readonly int maxScoutEngineerAge = 45;

	private static readonly int minScoutMechanicAge = 18;

	private static readonly int maxScoutMechanicAge = 31;

	private static readonly int maxActiveHQBuilding = 2;

	private static readonly int hireStaffCooldownDays = 40;

	private static readonly int fireStaffDelayDays = 14;

	private static readonly float firingChanceScalar = 0.25f;

	private static readonly float carImprovementReliabilityMinAggression = 1f;

	private static readonly float carImprovementReliabilityMaxAggression = 0.65f;

	[NonSerialized]
	private List<SponsorshipDeal> mChosenDealsCache = new List<SponsorshipDeal>();

	[NonSerialized]
	private List<ContractSponsor> mChosenOffersCache = new List<ContractSponsor>();

	[NonSerialized]
	private readonly int mSponsorTargetPositionVariance = 2;

	[NonSerialized]
	private readonly int mDaysBetweenSponsorChecks = 7;

	private DateTime mLastCheckedTime = default(DateTime);

	private DateTime mRequestFundsCooldown = default(DateTime);

	[NonSerialized]
	private List<Driver> mScoutableDrivers = new List<Driver>();

	[NonSerialized]
	private List<Engineer> mScoutableEngineers = new List<Engineer>();

	[NonSerialized]
	private List<Mechanic> mScoutableMechanics = new List<Mechanic>();

	public class HQBuildingValue
	{
		public HQBuildingValue()
		{
		}

		public float mValue;

		public HQsBuilding_v1 mBuilding;

		public bool mbIsUpgrade;
	}

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	public class NegotiationEntry
	{
		public NegotiationEntry()
		{
		}

		public ContractNegotiationScreen.NegotatiationType mNegotiationType;

		public Person mPerson;

		public EmployeeSlot mPersonToFire;

		public ContractPerson mDraftContractPerson;

		public DateTime mLastNegotiatedWith;

		public Team mTeamAtTimeOfNegotiation;

		public bool mIsReplacementPerson;
	}

	private class DriverEval
	{
		public DriverEval(int index, float stats, bool is_pay, bool is_negotiating)
		{
			this.mIndex = index;
			this.mStats = stats;
			this.mIsPayDriver = is_pay;
			this.mIsNegotiating = is_negotiating;
		}

		public int mIndex;

		public float mStats;

		public bool mIsPayDriver;

		public bool mIsNegotiating;
	}

	public class CarUpgradeStatValues
	{
		public CarUpgradeStatValues()
		{
		}

		public CarPart.PartType partType = CarPart.PartType.None;

		public float weighting;

		public float highestStatOnGrid;

		public float difference;

		public bool gotTwoPartsOfBestPossibleLevel;

		public int nPartsOfbestPossibleLevel;

		public bool has5ComponentSlots;

		public bool isSpecPart;
	}
}
