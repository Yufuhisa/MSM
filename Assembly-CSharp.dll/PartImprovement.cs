using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class PartImprovement
{
	public PartImprovement()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static PartImprovement()
	{
	}

	public void Start(Team inTeam)
	{
		this.mTeam = inTeam;
		this.AssignChiefMechanics();
		for (int i = 0; i < PartImprovement.allImprovementTypes.Length; i++)
		{
			CarPartStats.CarPartStat carPartStat = PartImprovement.allImprovementTypes[i];
			this.mechanics.Add((int)carPartStat, 0);
			this.partsToImprove.Add((int)carPartStat, new List<CarPart>());
			this.partWorkStartDate.Add((int)carPartStat, default(DateTime));
			this.partWorkEndDate.Add((int)carPartStat, default(DateTime));
			this.endDateCalendarEvents.Add((int)carPartStat, null);
		}
	}

	public void Destroy()
	{
		SafeAction.NullAnAction(ref this.OnItemListsChangedForUI);
	}

	public void AddPartToImprove(CarPartStats.CarPartStat inStackType, CarPart inPart)
	{
		if (inStackType == CarPartStats.CarPartStat.Condition)
		{
			return;
		}
		if (inPart.isBanned)
		{
			return;
		}
		if (inStackType == CarPartStats.CarPartStat.Reliability && inPart.stats.GetStat(inStackType) >= inPart.stats.maxReliability)
		{
			return;
		}
		if (inStackType == CarPartStats.CarPartStat.Performance && inPart.stats.performance >= inPart.stats.maxPerformance)
		{
			return;
		}
		if (this.mTeam.IsPlayersTeam())
		{
			Game.instance.achievementData.hasUpgradedCarPartThisSeason = true;
		}
		List<CarPart> list = this.partsToImprove[(int)inStackType];
		if (list.Count < this.GetPartSlotsCount() && !list.Contains(inPart))
		{
			list.Add(inPart);
			this.SetStartDate(inStackType);
			this.SetRefreshEndDatesFlag();
		}
		this.UpdateMechanicsDistribution();
		if (this.OnItemListsChangedForUI != null && this.mTeam.IsPlayersTeam())
		{
			this.OnItemListsChangedForUI.Invoke();
		}
	}

	public void FixCondition()
	{
		List<CarPart> allParts = this.mTeam.carManager.partInventory.GetAllParts();
		float num = 0f;
		for (int i = 0; i < allParts.Count; i++)
		{
			CarPart carPart = allParts[i];
			num += carPart.partCondition.normalizedCondition;
		}
		if (num < (float)allParts.Count)
		{
			this.mConditionWorkStartDate = Game.instance.time.now;
			float t = 1f - num / 12f;
			float num2 = Mathf.Lerp(GameStatsConstants.minConditionTimeToFix, GameStatsConstants.maxConditionTimeToFix, t);
			this.mConditionWorkEndDate = this.mConditionWorkStartDate.AddHours((double)num2);
			if (this.mTeam == Game.instance.player.team)
			{
				if (!this.mIsFixingCondition)
				{
					CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
					{
						showOnCalendar = true,
						category = CalendarEventCategory.Design,
						triggerDate = this.mConditionWorkEndDate,
						triggerState = GameState.Type.FrontendState,
						interruptGameTime = true,
						OnEventTrigger = MMAction.CreateFromAction(new Action(new SendWorkDoneMessage(this, CarPartStats.CarPartStat.Condition).Execute)),
						displayEffect = new TeamDisplayEffect
						{
							changeDisplay = true,
							changeInterrupt = true,
							team = this.mTeam
						}
					};
					calendarEvent_v.SetDynamicDescription("PSG_10009238");
					this.mConditionCalendarEvent = calendarEvent_v;
					Game.instance.calendar.AddEvent(this.mConditionCalendarEvent);
				}
				else if (this.mConditionCalendarEvent != null)
				{
					Game.instance.calendar.ChangeEventTriggerDate(this.mConditionCalendarEvent, this.mConditionWorkEndDate);
				}
			}
			this.mIsFixingCondition = true;
		}
	}

	private void SetStartDate(CarPartStats.CarPartStat inStackType)
	{
		this.partWorkStartDate[(int)inStackType] = Game.instance.time.now;
	}

	private void SetRefreshEndDatesFlag()
	{
		this.mRefreshEndDate = true;
	}

	private void SetEndDate(CarPartStats.CarPartStat inStackType)
	{
		this.partWorkEndDate[(int)inStackType] = this.GetWorkEndDate(inStackType);
		bool flag = this.partWorkEndDate[(int)inStackType] != Game.instance.time.now;
		if (this.mTeam == Game.instance.player.team)
		{
			if (flag && this.endDateCalendarEvents[(int)inStackType] == null)
			{
				CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
				{
					showOnCalendar = true,
					category = CalendarEventCategory.Design,
					triggerDate = this.partWorkEndDate[(int)inStackType],
					triggerState = GameState.Type.FrontendState,
					interruptGameTime = false,
					OnEventTrigger = MMAction.CreateFromAction(new Action(new ImprovePartsCommand(this, this.mTeam.championship, inStackType).Execute)),
					displayEffect = new TeamDisplayEffect
					{
						changeDisplay = true,
						changeInterrupt = false,
						team = this.mTeam
					}
				};
				StringVariableParser.partStat = inStackType;
				calendarEvent_v.SetDynamicDescription("PSG_10009153");
				Game.instance.calendar.AddEvent(calendarEvent_v);
				this.endDateCalendarEvents[(int)inStackType] = calendarEvent_v;
			}
			else if (flag)
			{
				Game.instance.calendar.ChangeEventTriggerDate(this.endDateCalendarEvents[(int)inStackType], this.partWorkEndDate[(int)inStackType]);
			}
			else if (this.endDateCalendarEvents[(int)inStackType] != null)
			{
				Game.instance.calendar.RemoveEvent(this.endDateCalendarEvents[(int)inStackType]);
				this.endDateCalendarEvents[(int)inStackType] = null;
			}
		}
	}

	public void AutoFill(CarPartStats.CarPartStat inStackType)
	{
		List<CarPart> mostRecentParts = this.mTeam.carManager.partInventory.GetMostRecentParts(this.mTeam.carManager.partInventory.PartCount(), CarPart.PartType.None);
		for (int i = 0; i < mostRecentParts.Count; i++)
		{
			this.AddPartToImprove(inStackType, mostRecentParts[i]);
		}
	}

	public void RemoveAllPartImprove(CarPartStats.CarPartStat inStackType)
	{
		this.partsToImprove[(int)inStackType].Clear();
		if (this.OnItemListsChangedForUI != null && this.mTeam.IsPlayersTeam())
		{
			this.OnItemListsChangedForUI.Invoke();
		}
		this.SetStartDate(inStackType);
		this.SetRefreshEndDatesFlag();
		this.UpdateMechanicsDistribution();
	}

	public void RemovePartImprove(CarPartStats.CarPartStat inStackType, CarPart inPart)
	{
		this.RemovePartImprove(inStackType, inPart, false);
	}

	public void RemovePartImprove(CarPartStats.CarPartStat inStackType, CarPart inPart, bool inDoNotMessagePlayer)
	{
		List<CarPart> list = this.partsToImprove[(int)inStackType];
		if (list.Contains(inPart))
		{
			list.Remove(inPart);
			this.SetStartDate(inStackType);
			this.SetRefreshEndDatesFlag();
		}
		this.UpdateMechanicsDistribution();
		if (!inDoNotMessagePlayer && this.mTeam == Game.instance.player.team && list.Count == 0 && !SimulationUtility.IsEventState() && !this.FixingCondition())
		{
			if (this.MechanicsIdle())
			{
				Game.instance.dialogSystem.OnMechanicsIdle(null);
			}
			else
			{
				this.SendWorkDoneMessage(inStackType);
			}
		}
		if (this.OnItemListsChangedForUI != null && this.mTeam.IsPlayersTeam())
		{
			this.OnItemListsChangedForUI.Invoke();
		}
	}

	private void UpdateMechanicsDistribution()
	{
		bool flag = this.partsToImprove[1].Count == 0;
		bool flag2 = this.partsToImprove[3].Count == 0;
		if (flag && !flag2)
		{
			this.SplitMechanics(1f);
		}
		else if (!flag && flag2)
		{
			this.SplitMechanics(0f);
		}
		else
		{
			this.SplitMechanics(this.mPlayerMechanicsPreference);
		}
	}

	public void SendWorkDoneMessage(CarPartStats.CarPartStat inStackType)
	{
		if (!Game.instance.player.IsUnemployed())
		{
			switch (inStackType)
			{
			case CarPartStats.CarPartStat.Reliability:
				Game.instance.dialogSystem.OnReliabilityStackEmpty(null);
				break;
			case CarPartStats.CarPartStat.Condition:
				Game.instance.dialogSystem.OnConditionStackEmpty(null);
				break;
			case CarPartStats.CarPartStat.Performance:
				Game.instance.dialogSystem.OnPerformanceStackEmpty(null);
				break;
			}
		}
	}

	public bool MechanicsIdle()
	{
		bool result = true;
		for (int i = 0; i < PartImprovement.playerAvailableImprovementTypes.Length; i++)
		{
			CarPartStats.CarPartStat carPartStat = PartImprovement.playerAvailableImprovementTypes[i];
			if (this.partsToImprove[(int)carPartStat].Count > 0)
			{
				result = false;
			}
		}
		return result;
	}

	public int GetMechanicsAssigned()
	{
		return this.mechanics[1] + this.mechanics[3];
	}

	public int GetTotalMechanics()
	{
		this.mFactory = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.Factory);
		return this.mFactory.staffNumber;
	}

	public void SplitMechanics(float inNormalizedValue)
	{
		int num = Mathf.RoundToInt(inNormalizedValue * (float)this.GetTotalMechanics());
		int num2 = this.GetTotalMechanics() - num;
		this.mechanics[3] = num;
		this.mechanics[1] = num2;
		this.mNormalizedMechanicDistribution = inNormalizedValue;
		this.SetRefreshEndDatesFlag();
	}

	public bool FixingCondition()
	{
		return this.mIsFixingCondition;
	}

	public void AssignChiefMechanics()
	{
		List<Person> allPeopleOnJob = this.mTeam.contractManager.GetAllPeopleOnJob(Contract.Job.Mechanic);
		if (this.mechanicOnPerformance == null || this.mechanicOnPerformance.contract.job == Contract.Job.Unemployed)
		{
			for (int i = 0; i < allPeopleOnJob.Count; i++)
			{
				if (this.mechanicOnReliability == null || allPeopleOnJob[i] != this.mechanicOnReliability)
				{
					this.mechanicOnPerformance = allPeopleOnJob[i];
				}
			}
		}
		if (this.mechanicOnReliability == null || this.mechanicOnReliability.contract.job == Contract.Job.Unemployed)
		{
			for (int j = 0; j < allPeopleOnJob.Count; j++)
			{
				if (this.mechanicOnPerformance == null || allPeopleOnJob[j] != this.mechanicOnPerformance)
				{
					this.mechanicOnReliability = allPeopleOnJob[j];
				}
			}
		}
		if (Game.IsActive())
		{
			this.SetRefreshEndDatesFlag();
		}
	}

	public void SwapChiefMechanics()
	{
		Person person = this.mechanicOnPerformance;
		Person person2 = this.mechanicOnReliability;
		this.mechanicOnPerformance = person2;
		this.mechanicOnReliability = person;
		this.SetRefreshEndDatesFlag();
	}

	public Person GetChiefMechanic(CarPartStats.CarPartStat inStat)
	{
		switch (inStat)
		{
		case CarPartStats.CarPartStat.Reliability:
			return this.mechanicOnReliability;
		case CarPartStats.CarPartStat.Performance:
			return this.mechanicOnPerformance;
		}
		return null;
	}

	public void Update()
	{
		if (!Game.instance.time.isPaused && !this.FixingCondition())
		{
			int hour = Game.instance.time.now.Hour;
			if (hour >= 9 && hour < 18 && Game.instance.time.now.DayOfWeek != null && Game.instance.time.now.DayOfWeek != 6)
			{
				for (int i = 0; i < PartImprovement.allImprovementTypes.Length; i++)
				{
					CarPartStats.CarPartStat targetStat = PartImprovement.allImprovementTypes[i];
					this.ImproveParts(this.mTeam.championship, targetStat, false);
				}
			}
		}
		else if (!Game.instance.time.isPaused)
		{
			this.UpdateConditionFix(0f);
		}
	}

	public void UpdateConditionFix(float inNormalizedValue = 0f)
	{
		float num = inNormalizedValue;
		if (inNormalizedValue == 0f)
		{
			num = this.GetNormalizedTimeToFinishWork(CarPartStats.CarPartStat.Condition);
		}
		List<CarPart> allParts = this.mTeam.carManager.partInventory.GetAllParts();
		for (int i = 0; i < allParts.Count; i++)
		{
			CarPart carPart = allParts[i];
			carPart.partCondition.FixConditionAfterRace(num);
		}
		if (num >= 1f)
		{
			this.mIsFixingCondition = false;
		}
	}

	public void RefreshEndDates()
	{
		if (this.mRefreshEndDate)
		{
			this.UpdateAllPartTypesEndDate();
			this.mRefreshEndDate = false;
		}
	}

	public void ImproveParts(Championship inChampionship, CarPartStats.CarPartStat targetStat, bool inFinishWork)
	{
		this.partsDoneCache.Clear();
		List<CarPart> list = this.partsToImprove[(int)targetStat];
		if (list.Count == 0)
		{
			return;
		}
		float num = (this.GetWorkRate(targetStat) + this.GetChiefMechanicWorkRate(targetStat)) * Game.instance.time.cachedDeltaTime;
		float num2 = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			CarPart carPart = list[i];
			if (PartImprovement.AnyPartNeedsWork(targetStat, new CarPart[]
			{
				carPart
			}))
			{
				switch (targetStat)
				{
				case CarPartStats.CarPartStat.Reliability:
				{
					float num3 = carPart.stats.maxReliability;
					float num4 = num3 - carPart.stats.GetStat(targetStat);
					num4 = Mathf.Clamp01(num4);
					num2 += num4;
					break;
				}
				case CarPartStats.CarPartStat.Performance:
				{
					float num3 = carPart.stats.maxPerformance;
					float num4 = num3 - carPart.stats.GetStat(targetStat);
					num4 = ((num4 >= 0f) ? num4 : 0f);
					num2 += num4;
					break;
				}
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			CarPart carPart2 = list[j];
			if (!PartImprovement.AnyPartNeedsWork(targetStat, new CarPart[]
			{
				carPart2
			}))
			{
				this.partsDoneCache.Add(carPart2);
			}
			else
			{
				switch (targetStat)
				{
				case CarPartStats.CarPartStat.Reliability:
				{
					float num3 = carPart2.stats.maxReliability;
					float num5 = num3 - carPart2.stats.GetStat(targetStat);
					carPart2.stats.AddToStat(targetStat, num5 / num2 * num);
					if (inFinishWork || Mathf.Approximately(carPart2.stats.GetStat(targetStat), num3))
					{
						carPart2.stats.SetStat(targetStat, num3);
						this.partsDoneCache.Add(carPart2);
					}
					carPart2.stats.partCondition.SetCondition(carPart2.stats.reliability);
					break;
				}
				case CarPartStats.CarPartStat.Performance:
				{
					float num3 = carPart2.stats.maxPerformance;
					float num5 = num3 - carPart2.stats.GetStat(targetStat);
					carPart2.stats.AddToStat(targetStat, num5 / num2 * num);
					if (inFinishWork || Mathf.Approximately(carPart2.stats.GetStat(targetStat), num3))
					{
						carPart2.stats.SetStat(targetStat, num3);
						this.partsDoneCache.Add(carPart2);
					}
					break;
				}
				}
			}
		}
		if (this.partsDoneCache.Count == list.Count)
		{
			for (int k = 0; k < this.partsDoneCache.Count; k++)
			{
				this.RemovePartImprove(targetStat, this.partsDoneCache[k]);
			}
			if (!this.mTeam.IsPlayersTeam())
			{
				this.mTeam.teamAIController.FitPartsOnCars();
			}
		}
		if (num2 != 0f && this.OnItemListsChangedForUI != null && this.mTeam.IsPlayersTeam())
		{
			this.OnItemListsChangedForUI.Invoke();
		}
	}

	public float GetWorkRate(CarPartStats.CarPartStat inStat)
	{
		float num = 0f;
		if (this.mechanics[(int)inStat] <= 0)
		{
			return num;
		}
		switch (inStat)
		{
		case CarPartStats.CarPartStat.Reliability:
		{
			float b = 0.12f;
			float a = 0f;
			float num2 = 40f;
			float t = (float)this.mechanics[(int)inStat] / num2;
			num = Mathf.Lerp(a, b, t);
			break;
		}
		case CarPartStats.CarPartStat.Performance:
		{
			float b2 = 8f;
			float a2 = 0f;
			float num3 = 40f;
			float t2 = (float)this.mechanics[(int)inStat] / num3;
			num = Mathf.Lerp(a2, b2, t2);
			break;
		}
		}
		int num4 = 9;
		return num / (float)(num4 * 60 * 60);
	}

	public float GetChiefMechanicWorkRate(CarPartStats.CarPartStat inStat)
	{
		float num = 0f;
		switch (inStat)
		{
		case CarPartStats.CarPartStat.Reliability:
		{
			float t = ((Mechanic)this.mechanicOnReliability).stats.reliability / 20f;
			num = Mathf.Lerp(0f, 0.01f, t);
			break;
		}
		case CarPartStats.CarPartStat.Performance:
		{
			float t2 = ((Mechanic)this.mechanicOnPerformance).stats.performance / 20f;
			num = Mathf.Lerp(0f, 1f, t2);
			break;
		}
		}
		int num2 = 9;
		return num / (float)(num2 * 60 * 60);
	}

	public DateTime GetWorkEndDate(CarPartStats.CarPartStat inStat)
	{
		double num = 0.0;
		if (this.GetWorkRate(inStat) + this.GetChiefMechanicWorkRate(inStat) == 0f)
		{
			return Game.instance.time.now;
		}
		switch (inStat)
		{
		case CarPartStats.CarPartStat.Reliability:
			foreach (CarPart carPart in this.partsToImprove[(int)inStat])
			{
				float num2 = carPart.stats.maxReliability;
				float num3 = num2 - carPart.stats.reliability;
				num += (double)((num3 <= 0f) ? 0f : num3);
			}
			num /= (double)(this.GetWorkRate(inStat) + this.GetChiefMechanicWorkRate(inStat));
			break;
		case CarPartStats.CarPartStat.Performance:
			foreach (CarPart carPart2 in this.partsToImprove[(int)inStat])
			{
				float num2 = carPart2.stats.maxPerformance;
				float num4 = num2 - carPart2.stats.performance;
				num += (double)((num4 <= 0f) ? 0f : num4);
			}
			num /= (double)(this.GetWorkRate(inStat) + this.GetChiefMechanicWorkRate(inStat));
			break;
		}
		if (num == 0.0)
		{
			return Game.instance.time.now;
		}
		double totalSecondsToEndDate = this.GetTotalSecondsToEndDate(num);
		return Game.instance.time.now.AddSeconds(totalSecondsToEndDate);
	}

	private double GetTotalSecondsToEndDate(double inWorkSeconds)
	{
		double num = 0.0;
		int num2 = 9;
		int num3 = 18;
		DateTime dateTime = Game.instance.time.now;
		DateTime dateTime2;
		dateTime2..ctor(dateTime.Year, dateTime.Month, dateTime.Day, num2, 0, 0);
		DateTime dateTime3;
		dateTime3..ctor(dateTime.Year, dateTime.Month, dateTime.Day, num3, 0, 0);
		DateTime dateTime4;
		dateTime4..ctor(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
		DateTime dateTime5 = dateTime4.AddDays(1.0);
		double num4 = (dateTime2 - dateTime).TotalSeconds;
		TimeSpan timeSpan;
		timeSpan..ctor(num3 - num2, 0, 0);
		double totalSeconds = timeSpan.TotalSeconds;
		double num5 = (dateTime3 - dateTime).TotalSeconds;
		num5 = ((num5 <= totalSeconds) ? num5 : totalSeconds);
		num4 = ((num4 <= 0.0) ? 0.0 : num4);
		double num6 = 86400.0;
		double totalSeconds2 = (dateTime5 - dateTime).TotalSeconds;
		DayOfWeek dayOfWeek = dateTime.DayOfWeek;
		if (dayOfWeek != null && dayOfWeek != 6)
		{
			inWorkSeconds -= num5;
		}
		if (inWorkSeconds <= 0.0)
		{
			return num4 + num5 + inWorkSeconds;
		}
		num += totalSeconds2;
		dateTime = dateTime5;
		while (inWorkSeconds > 0.0)
		{
			dayOfWeek = dateTime.DayOfWeek;
			if (dayOfWeek != null && dayOfWeek != 6)
			{
				inWorkSeconds -= totalSeconds;
			}
			if (inWorkSeconds <= 0.0)
			{
				double num7 = (double)(num2 * 60 * 60);
				num += totalSeconds + inWorkSeconds + num7;
			}
			else
			{
				num += num6;
			}
			dateTime = dateTime.AddDays(1.0);
		}
		return num;
	}

	public float GetNormalizedTimeToFinishWork(CarPartStats.CarPartStat inStat)
	{
		if (inStat == CarPartStats.CarPartStat.Condition)
		{
			return 1f - (float)((Game.instance.time.now - this.mConditionWorkEndDate).TotalHours / (this.mConditionWorkStartDate - this.mConditionWorkEndDate).TotalHours);
		}
		TimeSpan timeSpan = this.partWorkEndDate[(int)inStat] - this.partWorkStartDate[(int)inStat];
		TimeSpan timeSpan2 = this.partWorkEndDate[(int)inStat] - Game.instance.time.now;
		if (timeSpan.TotalSeconds > 0.0)
		{
			return Mathf.Clamp01(1f - (float)(timeSpan2.TotalSeconds / timeSpan.TotalSeconds));
		}
		return 0f;
	}

	public TimeSpan GetTimeToFinishWork(CarPartStats.CarPartStat inStat)
	{
		TimeSpan result = default(TimeSpan);
		if (inStat == CarPartStats.CarPartStat.Condition)
		{
			result = this.mConditionWorkEndDate - Game.instance.time.now;
		}
		else
		{
			result = this.partWorkEndDate[(int)inStat] - Game.instance.time.now;
		}
		if (result.Ticks < 0L)
		{
			return default(TimeSpan);
		}
		return result;
	}

	public bool HasAvailableSlot(CarPartStats.CarPartStat inStat)
	{
		return this.partsToImprove[(int)inStat].Count < this.GetPartSlotsCount();
	}

	public bool IsWorkingOnParts()
	{
		return this.partsToImprove[1].Count > 0 || this.partsToImprove[3].Count > 0;
	}

	public static bool AnyPartNeedsWork(CarPartStats.CarPartStat inStat, params CarPart[] inParts)
	{
		foreach (CarPart carPart in inParts)
		{
			if (carPart != null)
			{
				switch (inStat)
				{
				case CarPartStats.CarPartStat.Reliability:
				{
					float num = carPart.stats.maxReliability;
					if (carPart.stats.GetStat(inStat) < num)
					{
						return true;
					}
					break;
				}
				case CarPartStats.CarPartStat.Performance:
				{
					float num = carPart.stats.maxPerformance;
					if (carPart.stats.GetStat(inStat) < num)
					{
						return true;
					}
					break;
				}
				}
			}
		}
		return false;
	}

	public bool WorkOnStatActive(CarPartStats.CarPartStat inStat)
	{
		return this.GetChiefMechanicWorkRate(inStat) + this.GetWorkRate(inStat) > 0f && this.partsToImprove[(int)inStat].Count != 0 && this.GetNormalizedTimeToFinishWork(inStat) < 1f;
	}

	public int GetPartSlotsCount()
	{
		HQsBuilding_v1 building = this.mTeam.headquarters.GetBuilding(HQsBuildingInfo.Type.Factory);
		float num = 3f;
		return Mathf.RoundToInt(Mathf.Lerp((float)this.GetPartSlotsMinimumCount(), (float)this.GetPartSlotsMaximumCount(), (float)building.currentLevel / num));
	}

	public int GetPartSlotsMinimumCount()
	{
		return 2;
	}

	public int GetPartSlotsMaximumCount()
	{
		return 8;
	}

	public void UpdateAllPartTypesEndDate()
	{
		for (int i = 0; i < PartImprovement.allImprovementTypes.Length; i++)
		{
			this.SetEndDate(PartImprovement.allImprovementTypes[i]);
		}
	}

	public float normalizedMechanicDistribution
	{
		get
		{
			return this.mNormalizedMechanicDistribution;
		}
		set
		{
			this.mNormalizedMechanicDistribution = value;
		}
	}

	public float playerMechanicsPreference
	{
		set
		{
			this.mPlayerMechanicsPreference = value;
		}
	}

	public const int workSeconds = 32400;

	public Action OnItemListsChangedForUI;

	public Dictionary<int, List<CarPart>> partsToImprove = new Dictionary<int, List<CarPart>>();

	public Dictionary<int, DateTime> partWorkStartDate = new Dictionary<int, DateTime>();

	public Dictionary<int, DateTime> partWorkEndDate = new Dictionary<int, DateTime>();

	public Dictionary<int, CalendarEvent_v1> endDateCalendarEvents = new Dictionary<int, CalendarEvent_v1>();

	public Dictionary<int, int> mechanics = new Dictionary<int, int>();

	public Person mechanicOnPerformance;

	public Person mechanicOnReliability;

	private bool mIsFixingCondition;

	private Team mTeam;

	private HQsBuilding_v1 mFactory;

	private float mPlayerMechanicsPreference = 0.5f;

	private float mNormalizedMechanicDistribution = 0.5f;

	private bool mRefreshEndDate;

	private CalendarEvent_v1 mConditionCalendarEvent;

	[NonSerialized]
	private List<CarPart> partsDoneCache = new List<CarPart>();

	private DateTime mConditionWorkStartDate = default(DateTime);

	private DateTime mConditionWorkEndDate = default(DateTime);

	public static CarPartStats.CarPartStat[] playerAvailableImprovementTypes = new CarPartStats.CarPartStat[]
	{
		CarPartStats.CarPartStat.Reliability,
		CarPartStats.CarPartStat.Performance
	};

	public static CarPartStats.CarPartStat[] allImprovementTypes = new CarPartStats.CarPartStat[]
	{
		CarPartStats.CarPartStat.Reliability,
		CarPartStats.CarPartStat.Performance
	};
}
