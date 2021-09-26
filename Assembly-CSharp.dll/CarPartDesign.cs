using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarPartDesign
{
	public CarPartDesign()
	{
	}

	public void Start(Team team)
	{
		this.mTeam = team;
	}

	public Dictionary<int, List<CarPartComponent>> GetComponentsForPartType(CarPart.PartType inPartType)
	{
		switch (inPartType)
		{
		case CarPart.PartType.Brakes:
			return this.brakeComponents;
		case CarPart.PartType.Engine:
			return this.engineComponents;
		case CarPart.PartType.FrontWing:
			return this.frontWingComponents;
		case CarPart.PartType.Gearbox:
			return this.gearboxComponents;
		case CarPart.PartType.RearWing:
			return this.rearWingComponents;
		case CarPart.PartType.Suspension:
			return this.suspensionComponents;
		case CarPart.PartType.RearWingGT:
			return this.rearWingGTComponents;
		case CarPart.PartType.BrakesGT:
			return this.brakeGTComponents;
		case CarPart.PartType.EngineGT:
			return this.engineGTComponents;
		case CarPart.PartType.GearboxGT:
			return this.gearboxGTComponents;
		case CarPart.PartType.SuspensionGT:
			return this.suspensionGTComponents;
		case CarPart.PartType.BrakesGET:
			return this.brakeComponentsGET;
		case CarPart.PartType.EngineGET:
			return this.engineComponentsGET;
		case CarPart.PartType.FrontWingGET:
			return this.frontWingComponentsGET;
		case CarPart.PartType.GearboxGET:
			return this.gearboxComponentsGET;
		case CarPart.PartType.RearWingGET:
			return this.rearWingComponentsGET;
		case CarPart.PartType.SuspensionGET:
			return this.suspensionComponentsGET;
		default:
			return null;
		}
	}

	public int GetComponentsAvailableCount(CarPart.PartType inPartType, int inLevel)
	{
		Dictionary<int, List<CarPartComponent>> componentsForPartType = this.GetComponentsForPartType(inPartType);
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][inPartType];
		Engineer engineer = (Engineer)this.mTeam.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		int num = 0;
		for (int i = 0; i < componentsForPartType.Keys.Count; i++)
		{
			if (i <= inLevel && partTypeSlotSettings.IsUnlocked(this.mTeam, i))
			{
				if (engineer.availableComponents.Count > i && engineer.availableComponents[i].IsUnlocked(this.mTeam))
				{
					num++;
				}
				for (int j = 0; j < componentsForPartType[i].Count; j++)
				{
					CarPartComponent carPartComponent = componentsForPartType[i][j];
					if (carPartComponent.IsUnlocked(this.mTeam))
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	public int GetComponentsCountForPartType(CarPart.PartType inPartType)
	{
		Dictionary<int, List<CarPartComponent>> componentsForPartType = this.GetComponentsForPartType(inPartType);
		Engineer engineer = (Engineer)this.mTeam.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		int num = 0;
		for (int i = 0; i < componentsForPartType.Keys.Count; i++)
		{
			if (i < engineer.availableComponents.Count)
			{
				num++;
			}
			for (int j = 0; j < componentsForPartType[i].Count; j++)
			{
				num++;
			}
		}
		return num;
	}

	public void OnNewSeasonStart()
	{
		this.ChooseComponentsForSeason();
		this.SetSeasonStartingStats();
	}

	public void SetSeasonStartingStats()
	{
		foreach (CarPart.PartType partType2 in CarPart.GetPartType(this.mTeam.championship.series, false))
		{
			CarPart partWithHighestStat = this.mTeam.carManager.partInventory.GetPartWithHighestStat(partType2, CarPartStats.CarPartStat.MainStat);
			global::Debug.Assert(partWithHighestStat != null, string.Format("{0} Original part is null, maybe theres no part being added from the database? Continuing with dummy data", partType2));
			if (partWithHighestStat == null)
			{
				this.mTeam.carManager.ResetParts(new CarStats());
			}
			if (partWithHighestStat != null)
			{
				this.seasonPartStartingStat[partType2] = (int)partWithHighestStat.stats.statWithPerformance;
			}
			else
			{
				this.seasonPartStartingStat[partType2] = 0;
			}
		}
	}

	private void ChooseComponentsForSeason()
	{
		foreach (CarPart.PartType partType2 in CarPart.GetPartType(this.mTeam.championship.series, false))
		{
			for (int j = 1; j <= 5; j++)
			{
				this.SetComponents(this.GetComponentsForPartType(partType2), partType2, j);
			}
		}
	}

	private void SetComponents(Dictionary<int, List<CarPartComponent>> inDictionary, CarPart.PartType inType, int inLevel)
	{
		List<CarPartComponent> componentsOfLevel = App.instance.componentsManager.GetComponentsOfLevel(inType, inLevel, new CarPartComponent.ComponentType[]
		{
			CarPartComponent.ComponentType.Stock,
			CarPartComponent.ComponentType.Risky
		});
		inDictionary[inLevel - 1] = this.ChooseComponents(componentsOfLevel);
	}

	private List<CarPartComponent> ChooseComponents(List<CarPartComponent> inAllComponents)
	{
		List<CarPartComponent> list = new List<CarPartComponent>();
		while (list.Count < 3)
		{
			CarPartComponent carPartComponent = inAllComponents[RandomUtility.GetRandom(0, inAllComponents.Count)];
			if (!list.Contains(carPartComponent))
			{
				list.Add(carPartComponent);
			}
		}
		return list;
	}

	public void Reset()
	{
		this.mStage = CarPartDesign.Stage.Idle;
		this.mExtraCopies = 0;
		this.mComponentTimeDaysBonus = 0f;
		this.mImidiateFinish = false;
		if (this.mCarPart != null)
		{
			this.mCarPart.DestroyCarPart();
			this.mCarPart = null;
		}
		this.startDate = Game.instance.time.now;
		this.endDate = Game.instance.time.now;
		this.mCalendarEvent = null;
		if (this.belongsToPlayer)
		{
			StringVariableParser.partWithRandomComponent = null;
			StringVariableParser.randomComponent = null;
		}
	}

	public void Cancel()
	{
		int inAmount = this.GetDesignCost() / 2;
		Transaction transaction = new Transaction(Transaction.Group.CarParts, Transaction.Type.Credit, inAmount, Localisation.LocaliseID("PSG_10002082", null));
		Game.instance.player.team.financeController.finance.ProcessTransactions(null, null, false, new Transaction[]
		{
			transaction
		});
		if (this.mCalendarEvent != null)
		{
			Game.instance.calendar.RemoveEvent(this.mCalendarEvent);
		}
		this.Reset();
		this.SetPartNotificationProgress(0f);
	}

	public void InitializeNewPart(CarPart.PartType inType)
	{
		this.Reset();
		this.componentSlots.Clear();
		this.componentBonusSlots.Clear();
		this.componentBonusSlotsLevel.Clear();
		int numberOfSlots = this.GetNumberOfSlots(inType);
		for (int i = 0; i < numberOfSlots; i++)
		{
			this.componentSlots.Add(null);
		}
		this.mCarPart = CarPart.CreatePartEntity(inType, this.mTeam.championship);
		this.mCarPart.components = new List<CarPartComponent>(this.componentSlots);
		this.mCarPart.PostStatsSetup(this.mTeam.championship);
		this.SetBaseStats(this.mCarPart);
	}

	public void SetCarPart(CarPart inCarPart)
	{
		this.mCarPart = inCarPart;
	}

	public int GetNumberOfSlots(CarPart.PartType inType)
	{
		int num = this.mTeam.carManager.partInventory.GetHighestLevel(inType, true).stats.level + 1;
		if (Game.instance.dilemmaSystem.carPartsLeveledUp.Contains(inType))
		{
			num++;
		}
		num = Mathf.Clamp(num, 1, GameStatsConstants.slotCount);
		if (this.mAllPartsUnlocked)
		{
			num = GameStatsConstants.slotCount;
		}
		return num;
	}

	private void SetBaseStats(CarPart inPart)
	{
		Engineer engineer = (Engineer)this.mTeam.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		inPart.stats = new CarPartStats(inPart);
		int num = this.seasonPartStartingStat[inPart.GetPartType()];
		float inValue = (float)(num + Mathf.FloorToInt(engineer.stats.partContributionStats.GetStat(inPart.stats.statType)));
		inPart.stats.level = this.GetLevelFromComponents(inPart);
		inPart.stats.maxPerformance = GameStatsConstants.baseCarPartPerformance;
		inPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, GameStatsConstants.initialReliabilityValue);
		inPart.stats.partCondition.redZone = GameStatsConstants.initialRedZone;
		inPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, inValue);
		// calculate max reliability (for engine control)
		if (inPart.GetPartType() != CarPart.PartType.Engine)
			inPart.stats.SetMaxReliability(GameStatsConstants.initialMaxReliabilityValue);
		else
		{
			float maxReliablityEnigne = this.team.carManager.GetCar(0).ChassisStats.supplierEngine.maxReliablity;
			float maxReliablityModFuel = this.team.carManager.GetCar(0).ChassisStats.supplierFuel.maxReliablity;
			inPart.stats.SetMaxReliability(maxReliablityEnigne + maxReliablityModFuel);
		}
	}

	private int GetLevelFromComponents(CarPart inPart)
	{
		int num = 0;
		for (int i = 0; i < inPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = inPart.components[i];
			if (carPartComponent != null)
			{
				num += carPartComponent.level;
			}
		}
		if (num >= 15)
		{
			return 5;
		}
		if (num >= 10)
		{
			return 4;
		}
		if (num >= 6)
		{
			return 3;
		}
		if (num >= 3)
		{
			return 2;
		}
		if (num >= 1)
		{
			return 1;
		}
		return 0;
	}

	public bool HasComponent(CarPartComponent inComponent)
	{
		return this.componentBonusSlots.Contains(inComponent) || this.componentSlots.Contains(inComponent);
	}

	public void AddSlot(int inLevelSlot)
	{
		this.componentBonusSlotsLevel.Add(inLevelSlot);
		this.componentBonusSlots.Add(null);
	}

	public void RemoveSlot(int inLevelSlot)
	{
		int num = this.componentBonusSlotsLevel.IndexOf(inLevelSlot);
		if (this.componentBonusSlots[num] != null)
		{
			this.RemoveComponent(this.mCarPart, this.componentBonusSlots[num]);
		}
		this.componentBonusSlotsLevel.RemoveAt(num);
		this.componentBonusSlots.RemoveAt(num);
	}

	public void AddComponent(CarPart inPart, CarPartComponent inComponent)
	{
		List<CarPartComponent> list = null;
		int num = inComponent.level - 1;
		int num2 = 0;
		for (int i = 0; i < this.componentSlots.Count; i++)
		{
			if (i >= num && this.componentSlots[i] == null)
			{
				num2 = i;
				list = this.componentSlots;
				break;
			}
		}
		for (int j = 0; j < this.componentBonusSlots.Count; j++)
		{
			if (this.componentBonusSlotsLevel[j] >= num + 1 && this.componentBonusSlots[j] == null)
			{
				num2 = j;
				list = this.componentBonusSlots;
				break;
			}
		}
		list[num2] = inComponent;
		list[num2].OnSelect(this, inPart);
		List<CarPartComponent> list2 = new List<CarPartComponent>();
		list2.AddRange(this.componentSlots);
		list2.AddRange(this.componentBonusSlots);
		inPart.components = new List<CarPartComponent>(list2);
		this.ApplyComponents(inPart);
		this.DesignModified();
	}

	public void RemoveComponent(CarPart inPart, CarPartComponent inComponent)
	{
		inComponent.OnDeSelect(this, inPart);
		List<CarPartComponent> list = this.componentSlots;
		if (this.componentBonusSlots.Contains(inComponent))
		{
			list = this.componentBonusSlots;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == inComponent)
			{
				list[i] = null;
				this.MoveComponentsToLowerCostSlot();
				break;
			}
		}
		List<CarPartComponent> list2 = new List<CarPartComponent>();
		list2.AddRange(this.componentSlots);
		list2.AddRange(this.componentBonusSlots);
		inPart.components = new List<CarPartComponent>(list2);
		this.ApplyComponents(inPart);
		this.DesignModified();
	}

	public void ApplyComponents(CarPart inPart)
	{
		this.mComponentTimeDaysBonus = 0f;
		this.SetBaseStats(inPart);
		inPart.AddAllComponentStats();
		inPart.components.ForEach(delegate(CarPartComponent component)
		{
			if (component != null)
			{
				component.ApplyBonus(this, inPart);
			}
		});
		if (inPart.stats.GetStat(CarPartStats.CarPartStat.MainStat) < 1f)
		{
			inPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, 1f);
		}
	}

	private void MoveComponentsToLowerCostSlot()
	{
		List<CarPartComponent> list = new List<CarPartComponent>();
		for (int i = 0; i < this.componentSlots.Count; i++)
		{
			if (this.componentSlots[i] != null)
			{
				list.Add(this.componentSlots[i]);
				this.componentSlots[i] = null;
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			CarPartComponent carPartComponent = list[j];
			int num = carPartComponent.level - 1;
			for (int k = num; k < this.componentSlots.Count; k++)
			{
				if (this.componentSlots[k] == null)
				{
					this.componentSlots[k] = carPartComponent;
					break;
				}
			}
		}
	}

	private void DesignModified()
	{
		if (this.OnDesignModified != null)
		{
			this.OnDesignModified.Invoke();
		}
	}

	public bool HasSlotForLevel(int inLevel)
	{
		for (int i = inLevel - 1; i < this.componentSlots.Count; i++)
		{
			if (this.componentSlots[i] == null)
			{
				return true;
			}
		}
		for (int j = 0; j < this.componentBonusSlots.Count; j++)
		{
			if (this.componentBonusSlots[j] == null && this.componentBonusSlotsLevel[j] >= inLevel)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasComponentOfLevel(int inLevel)
	{
		for (int i = inLevel - 1; i < this.componentSlots.Count; i++)
		{
			if (this.componentSlots[i] != null && this.componentSlots[i].level == inLevel)
			{
				return true;
			}
		}
		for (int j = 0; j < this.componentBonusSlots.Count; j++)
		{
			if (this.componentBonusSlots[j] != null && this.componentBonusSlotsLevel[j] == inLevel)
			{
				return true;
			}
		}
		return false;
	}

	public void StartDesigning()
	{
		this.mCarPart.components.ForEach(delegate(CarPartComponent component)
		{
			if (component != null)
			{
				component.OnPartBuildStart(this, this.mCarPart);
			}
		});
		this.ApplyComponents(this.mCarPart);
		this.mStage = CarPartDesign.Stage.Designing;
		this.startDate = Game.instance.time.now;
		this.endDate = this.startDate.Add(this.GetDesignDuration());
		StringVariableParser.carPart = this.mCarPart;
		StringVariableParser.partFrontendUI = this.mCarPart.GetPartType();
		this.mCalendarEvent = new CalendarEvent_v1
		{
			category = CalendarEventCategory.Design,
			showOnCalendar = this.belongsToPlayer,
			interruptGameTime = this.belongsToPlayer,
			triggerDate = this.endDate,
			triggerState = GameState.Type.FrontendState,
			OnEventTrigger = MMAction.CreateFromAction(new Action(this.PartComplete)),
			displayEffect = new TeamDisplayEffect
			{
				changeDisplay = true,
				changeInterrupt = true,
				team = this.mTeam
			}
		};
		this.mCalendarEvent.SetDynamicDescription("PSG_10009151");
		if (this.belongsToPlayer)
		{
			FeedbackPopup.Open(Localisation.LocaliseID("PSG_10010523", null), Localisation.LocaliseID("PSG_10010524", null));
			this.SetPartNotificationProgress(0.001f);
		}
		Game.instance.calendar.AddEvent(this.mCalendarEvent);
	}

	public void Update()
	{
		if (this.mStage == CarPartDesign.Stage.Designing)
		{
			DateTime dateTime = this.endDate;
			this.SetPartNotificationProgress(this.GetCreationTimeElapsedNormalised());
			if (dateTime != this.endDate)
			{
				Game.instance.calendar.ChangeEventTriggerDate(this.mCalendarEvent, this.endDate);
			}
		}
	}

	public void FinishPartImmediatly()
	{
		if (this.mStage == CarPartDesign.Stage.Designing && this.mCalendarEvent != null)
		{
			Game.instance.calendar.RemoveEvent(this.mCalendarEvent);
			if (this.mCalendarEvent != null)
			{
				this.mImidiateFinish = true;
				this.mCalendarEvent.OnEventTrigger.Invoke();
			}
		}
	}

	private void SetPartNotificationProgress(float inProgress)
	{
		if (this.belongsToPlayer)
		{
			if (this.mNotification == null)
			{
				this.mNotification = Game.instance.notificationManager.GetNotification("PartDesigning");
			}
			this.mNotification.SetProgress(inProgress);
		}
		else if (this.mNotification != null)
		{
			this.mNotification.SetProgress(0f);
			this.mNotification = null;
		}
	}

	public void BuildTwoParts(int inValue)
	{
		this.mExtraCopies += inValue;
		this.mExtraCopies = Mathf.Max(this.mExtraCopies, 0);
	}

	private void PartComplete()
	{
		global::Debug.Assert(this.mStage == CarPartDesign.Stage.Designing);
		if (this.mStage != CarPartDesign.Stage.Designing)
		{
			return;
		}
		this.mStage = CarPartDesign.Stage.Idle;
		int num = 1 + this.mExtraCopies;
		for (int i = 0; i < num; i++)
		{
			this.CloneAndCreateNewPart(this.mCarPart);
		}
		this.mCarPart.buildDate = Game.instance.time.now;
		this.mLastCarPart = this.mCarPart;
		Game.instance.dilemmaSystem.carPartsLeveledUp.Remove(this.mCarPart.GetPartType());
		if (this.belongsToPlayer)
		{
			this.UpdatePartBuiltAchievements();
			StringVariableParser.partForUI = this.mCarPart;
			StringVariableParser.partWithRandomComponent = this.mCarPart;
			StringVariableParser.randomComponent = this.mRandomComponent;
			this.SetPartNotificationProgress(0f);
			if (!this.mImidiateFinish)
			{
				FeedbackPopup.Open(Localisation.LocaliseID("PSG_10010525", null), Localisation.LocaliseID("PSG_10010526", null));
				this.SendCarPartBuiltMessage(this.mCarPart);
			}
			this.mRandomComponent = null;
		}
		else
		{
			this.mTeam.teamAIController.FitPartsOnCars();
		}
		if (this.OnPartBuilt != null)
		{
			this.OnPartBuilt.Invoke();
		}
		this.Reset();
	}

	public CarPart CloneAndCreateNewPart(CarPart inPart)
	{
		CarPart carPart = CarPart.CreatePartEntity(inPart.GetPartType(), this.mTeam.championship);
		carPart.components = new List<CarPartComponent>(inPart.components);
		this.ApplyComponents(carPart);
		carPart.buildDate = Game.instance.time.now;
		carPart.OnPartBuilt(this);
		carPart.PostStatsSetup(this.mTeam.championship);
		this.mTeam.carManager.partInventory.AddPart(carPart);
		Notification notification = Game.instance.notificationManager.GetNotification(inPart.GetPartType().ToString());
		if (!this.mImidiateFinish && this.belongsToPlayer)
		{
			Notification notification2 = Game.instance.notificationManager.CreateNotification(carPart.name, notification);
			notification2.IncrementCount();
		}
		return carPart;
	}

	private void SendCarPartBuiltMessage(CarPart inPart)
	{
		Game.instance.dialogSystem.OnPartBuilt(inPart);
	}

	public int GetComponentDesignCostBonus()
	{
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		float num = 0f;
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = this.mCarPart.components[i];
			if (carPartComponent != null)
			{
				if (carPartComponent.cost != 0f)
				{
					num += carPartComponent.cost;
				}
				else if (carPartComponent.componentType != CarPartComponent.ComponentType.Engineer)
				{
					num += partTypeSlotSettings.costPerLevel[carPartComponent.level - 1];
				}
			}
		}
		return Mathf.RoundToInt(num);
	}

	public int GetDesignCost()
	{
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		float num = partTypeSlotSettings.materialsCost;
		num += (float)this.GetComponentDesignCostBonus();
		num = Math.Max(0f, num);
		return Mathf.RoundToInt(num);
	}

	public TimeSpan GetComponentsDesignDurationBonus()
	{
		float num = 0f;
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = this.mCarPart.components[i];
			if (carPartComponent != null && carPartComponent.isRandomComponent)
			{
				carPartComponent.isRandomComponent = false;
			}
			else
			{
				num += this.GetComponentDesignDurationBonus(carPartComponent);
			}
		}
		num += this.mComponentTimeDaysBonus;
		int num2 = Mathf.RoundToInt((num - (float)((int)num)) * 24f);
		return new TimeSpan((int)num, num2, 0, 0);
	}

	public float GetComponentDesignDurationBonus(CarPartComponent inComponent)
	{
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		float num = 0f;
		if (inComponent != null)
		{
			if (inComponent.productionTime != 0f)
			{
				num += inComponent.productionTime;
			}
			else if (inComponent.componentType != CarPartComponent.ComponentType.Engineer)
			{
				num += partTypeSlotSettings.timePerLevel[inComponent.level - 1];
			}
		}
		return num;
	}

	public void SetComponentsTimeBonus(float inDays)
	{
		this.mComponentTimeDaysBonus += inDays;
	}

	public TimeSpan GetDesignDuration()
	{
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		float num = partTypeSlotSettings.buildTimeDays;
		HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.DesignCentre);
		if (building != null && building.isBuilt)
		{
			num += HQsBuilding_v1.designCentrePartDaysPerLevel[building.currentLevel];
		}
		num += (float)this.GetComponentsDesignDurationBonus().TotalDays;
		num = Math.Max(0f, num);
		int num2 = Mathf.RoundToInt((num - (float)((int)num)) * 24f);
		TimeSpan result = new TimeSpan((int)num, num2, 0, 0);
		if (this.mTeam.IsPlayersTeam())
		{
			result = result.Subtract(Game.instance.player.designPartTimeModifier);
		}
		return result;
	}

	public string GetCostBreakdown()
	{
		string text = GameUtility.ColorToRichTextHex(UIConstants.positiveColor);
		string text2 = GameUtility.ColorToRichTextHex(UIConstants.negativeColor);
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		string text3 = string.Empty;
		StringVariableParser.ordinalNumberString = text2 + GameUtility.GetCurrencyString((long)((int)partTypeSlotSettings.materialsCost), 0) + "</color>";
		text3 += Localisation.LocaliseID("PSG_10010514", null);
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			float num = 0f;
			CarPartComponent carPartComponent = this.mCarPart.components[i];
			if (carPartComponent != null)
			{
				if (carPartComponent.cost != 0f)
				{
					num = carPartComponent.cost;
				}
				else if (carPartComponent.componentType != CarPartComponent.ComponentType.Engineer)
				{
					num = partTypeSlotSettings.costPerLevel[carPartComponent.level - 1];
				}
				if (num != 0f)
				{
					if (num > 0f)
					{
						StringVariableParser.ordinalNumberString = text2 + GameUtility.GetCurrencyString((long)((int)num), 0) + "</color>";
						text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010723", null);
					}
					else
					{
						StringVariableParser.ordinalNumberString = text + GameUtility.GetCurrencyString((long)((int)num), 0) + "</color>";
						text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010723", null);
					}
				}
			}
		}
		return text3;
	}

	public string GetDesignTimeBreakdown()
	{
		string text = GameUtility.ColorToRichTextHex(UIConstants.positiveColor);
		string text2 = GameUtility.ColorToRichTextHex(UIConstants.negativeColor);
		PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.mTeam.championship.championshipID][this.mCarPart.GetPartType()];
		string text3 = string.Empty;
		StringVariableParser.ordinalNumberString = text2 + partTypeSlotSettings.buildTimeDays;
		text3 += Localisation.LocaliseID("PSG_10010519", null);
		HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.DesignCentre);
		if (building != null && building.isBuilt)
		{
			float num = Mathf.Abs(HQsBuilding_v1.designCentrePartDaysPerLevel[building.currentLevel]);
			if (num != 0f)
			{
				StringVariableParser.ordinalNumberString = text + num.ToString("0.#") + "</color>";
				text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10012148", null);
			}
		}
		if (this.mTeam.IsPlayersTeam() && Game.instance.player.playerBackStoryType == PlayerBackStory.PlayerBackStoryType.ExEngineer)
		{
			StringVariableParser.ordinalNumberString = text + Game.instance.player.designPartTimeModifier.TotalDays;
			text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010518", null);
		}
		if (this.mComponentTimeDaysBonus != 0f)
		{
			if (this.mComponentTimeDaysBonus > 0f)
			{
				StringVariableParser.ordinalNumberString = text2 + this.mComponentTimeDaysBonus;
				text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010520", null);
			}
			else
			{
				StringVariableParser.ordinalNumberString = text + this.mComponentTimeDaysBonus;
				text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010521", null);
			}
		}
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			float num2 = 0f;
			CarPartComponent carPartComponent = this.mCarPart.components[i];
			if (carPartComponent != null)
			{
				if (carPartComponent.productionTime != 0f)
				{
					num2 = carPartComponent.productionTime;
				}
				else if (carPartComponent.componentType != CarPartComponent.ComponentType.Engineer)
				{
					num2 = partTypeSlotSettings.timePerLevel[carPartComponent.level - 1];
				}
				if (num2 != 0f)
				{
					if (num2 > 0f)
					{
						StringVariableParser.ordinalNumberString = text2 + num2;
						text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010520", null);
					}
					else
					{
						StringVariableParser.ordinalNumberString = text + num2;
						text3 = text3 + "\n" + Localisation.LocaliseID("PSG_10010521", null);
					}
				}
			}
		}
		return text3;
	}

	public float GetCreationTimeElapsedNormalised()
	{
		double totalSeconds = (this.endDate - this.startDate).TotalSeconds;
		double totalSeconds2 = (this.endDate - Game.instance.time.now).TotalSeconds;
		return Mathf.Clamp01((float)((totalSeconds - totalSeconds2) / totalSeconds));
	}

	public string GetTimeRemainingString()
	{
		string result = string.Empty;
		if (this.remainingTime.Days > 0)
		{
			StringVariableParser.intValue1 = this.remainingTime.Days;
			StringVariableParser.intValue2 = this.remainingTime.Hours;
			result = Localisation.LocaliseID("PSG_10010636", null);
		}
		else
		{
			StringVariableParser.intValue1 = this.remainingTime.Hours;
			result = Localisation.LocaliseID("PSG_10010540", null);
		}
		return result;
	}

	public string GetPerformanceBreakdown()
	{
		int num = this.seasonPartStartingStat[this.mCarPart.GetPartType()];
		Engineer engineer = (Engineer)this.mTeam.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		string text = string.Empty;
		StringVariableParser.ordinalNumberString = num.ToString("N0");
		text += Localisation.LocaliseID("PSG_10010514", null);
		StringVariableParser.ordinalNumberString = Mathf.FloorToInt(engineer.stats.partContributionStats.GetStat(this.mCarPart.stats.statType)).ToString("N0");
		text = text + "\n" + Localisation.LocaliseID("PSG_10010515", null);
		if (this.mCarPart.stats.performance != 0f)
		{
			StringVariableParser.ordinalNumberString = this.mCarPart.stats.performance.ToString("N0");
			text = text + "\n" + Localisation.LocaliseID("PSG_10010637", null);
		}
		float num2 = 0f;
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			if (this.mCarPart.components[i] != null)
			{
				num2 += this.mCarPart.components[i].statBoost;
			}
		}
		if (num2 != 0f)
		{
			StringVariableParser.ordinalNumberString = num2.ToString("N0");
			text = text + "\n" + Localisation.LocaliseID("PSG_10010516", null);
		}
		return text;
	}

	public string GetReliabilityBreakdown()
	{
		string text = string.Empty;
		StringVariableParser.ordinalNumberString = GameStatsConstants.initialReliabilityValue.ToString("P0");
		text += Localisation.LocaliseID("PSG_10010514", null);
		float num = 0f;
		for (int i = 0; i < this.mCarPart.components.Count; i++)
		{
			if (this.mCarPart.components[i] != null)
			{
				num += this.mCarPart.components[i].reliabilityBoost;
			}
		}
		if (num != 0f)
		{
			StringVariableParser.ordinalNumberString = num.ToString("P0");
			text = text + "\n" + Localisation.LocaliseID("PSG_10010516", null);
		}
		return text;
	}

	private void UpdatePartBuiltAchievements()
	{
		if (this.GetLevelFromComponents(this.mCarPart) == 5)
		{
			App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Build_Level_5_Part);
		}
	}

	public bool belongsToPlayer
	{
		get
		{
			return this.mTeam == Game.instance.player.team;
		}
	}

	public CarPartDesign.Stage stage
	{
		get
		{
			return this.mStage;
		}
	}

	public CarPart part
	{
		get
		{
			return this.mCarPart;
		}
	}

	public CarPart lastCarPart
	{
		get
		{
			return this.mLastCarPart;
		}
		set
		{
			this.mLastCarPart = value;
		}
	}

	public TimeSpan remainingTime
	{
		get
		{
			return this.endDate - Game.instance.time.now;
		}
	}

	public CalendarEvent_v1 buildEvent
	{
		get
		{
			return this.mCalendarEvent;
		}
	}

	public bool allPartsUnlocked
	{
		set
		{
			this.mAllPartsUnlocked = value;
		}
	}

	public CarPartComponent randomComponent
	{
		get
		{
			return this.mRandomComponent;
		}
		set
		{
			this.mRandomComponent = value;
		}
	}

	public Team team
	{
		get
		{
			return this.mTeam;
		}
	}

	public Action OnDesignModified;

	public Action OnPartBuilt;

	private CarPartDesign.Stage mStage;

	private CarPart mCarPart;

	private CarPart mLastCarPart;

	private int mExtraCopies;

	private bool mBuildTwoCopies;

	public List<CarPartComponent> componentSlots = new List<CarPartComponent>();

	public List<CarPartComponent> componentBonusSlots = new List<CarPartComponent>();

	public List<int> componentBonusSlotsLevel = new List<int>();

	public DateTime startDate;

	public DateTime endDate;

	private bool mAllPartsUnlocked;

	private float mComponentTimeDaysBonus;

	private Notification mNotification;

	private CalendarEvent_v1 mCalendarEvent;

	private Team mTeam;

	private CarPartComponent mRandomComponent;

	private Dictionary<CarPart.PartType, int> seasonPartStartingStat = new Dictionary<CarPart.PartType, int>();

	private Dictionary<int, List<CarPartComponent>> brakeComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> engineComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> frontWingComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> rearWingComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> suspensionComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> gearboxComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> engineGTComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> rearWingGTComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> brakeGTComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> suspensionGTComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> gearboxGTComponents = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> brakeComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> engineComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> frontWingComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> rearWingComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> suspensionComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private Dictionary<int, List<CarPartComponent>> gearboxComponentsGET = new Dictionary<int, List<CarPartComponent>>();

	private bool mImidiateFinish;

	public enum Stage
	{
		Idle,
		Designing
	}
}
