using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class NextYearCarDesign
{
	public CarChassisStats chassisStats
	{
		get
		{
			return this.mChassisStats;
		}
	}

	public void Start(Team team)
	{
		this.mTeam = team;
	}

	public void Update()
	{
		if (this.mTeam.IsPlayersTeam() && this.state == NextYearCarDesign.State.Designing)
		{
			if (this.mNotification == null)
			{
				this.mNotification = Game.instance.notificationManager.GetNotification("CarDesigning");
			}
			this.mNotification.SetProgress(this.GetNormalizedTimeElapsed());
		}
	}

	public float GetNormalizedTimeElapsed()
	{
		double totalSeconds = (this.designEndDate - this.designStartDate).TotalSeconds;
		double totalSeconds2 = (this.designEndDate - Game.instance.time.now).TotalSeconds;
		return Mathf.Clamp01(1f - (float)(totalSeconds2 / totalSeconds));
	}

	public void StartDesign(CarChassisStats inNewChassis)
	{
		if (this.mTeam.IsPlayersTeam())
		{
			this.mNotification = Game.instance.notificationManager.GetNotification("CarDesigning");
			this.mNotification.ResetCount();
			this.mNotification.ResetProgress();
		}
		this.mEngineModifier = inNewChassis.supplierEngine.randomEngineLevelModifier;
		this.designStartDate = Game.instance.time.now;
		this.designEndDate = this.mTeam.championship.carDevelopmenEndDate;
		this.mChassisStats = inNewChassis;
		CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
		{
			showOnCalendar = true,
			category = CalendarEventCategory.Design,
			triggerDate = this.designEndDate,
			triggerState = GameState.Type.PreSeasonState,
			interruptGameTime = true,
			uiState = CalendarEvent.UIState.TimeBarFillTarget,
			displayEffect = new TeamDisplayEffect
			{
				changeDisplay = true,
				changeInterrupt = true,
				team = this.mTeam
			}
		};
		calendarEvent_v.SetDynamicDescription("PSG_10009152");
		Game.instance.calendar.AddEvent(calendarEvent_v);
		this.state = NextYearCarDesign.State.Designing;
		this.UpdatePartsReliability();
	}

	private void UpdatePartsReliability()
	{
		float num = (float)Mathf.RoundToInt(this.mChassisStats.improvability / GameStatsConstants.chassisStatMax * 5f) * 0.05f;
		List<CarPart> allParts = this.mTeam.carManager.partInventory.GetAllParts();
		List<CarPart.PartType> nonSpecParts = this.mTeam.championship.rules.GetNonSpecParts();
		for (int i = 0; i < allParts.Count; i++)
		{
			CarPart carPart = allParts[i];
			if (nonSpecParts.Contains(carPart.GetPartType()))
			{
				float inValue = carPart.stats.reliability + num;
				carPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, inValue);
			}
		}
	}

	public void DesignCompleted()
	{
		if (this.mChassisStats == null)
		{
			return;
		}
		if (this.mTeam.IsPlayersTeam())
		{
			this.mNotification = Game.instance.notificationManager.GetNotification("CarDesigning");
			this.mNotification.ResetProgress();
		}
		this.mTeam.carManager.ApplyNewCarDesigns(this.mChassisStats);
		this.mChassisStats = null;
		this.state = NextYearCarDesign.State.Complete;
		if (!this.mTeam.championship.rules.specParts.Contains(CarPart.PartType.Engine))
		{
			List<CarPart> partInventory = this.mTeam.carManager.partInventory.GetPartInventory(CarPart.PartType.Engine);
			for (int i = 0; i < partInventory.Count; i++)
			{
				float statWithPerformance = partInventory[i].stats.statWithPerformance;
				partInventory[i].stats.SetStat(CarPartStats.CarPartStat.Performance, 0f);
				partInventory[i].stats.maxPerformance = (float)RandomUtility.GetRandomInc(5, 10);
				partInventory[i].stats.SetStat(CarPartStats.CarPartStat.MainStat, statWithPerformance + (float)this.mEngineModifier);
			}
		}
	}

	public NextYearCarDesign.State state = NextYearCarDesign.State.WaitingForDesign;

	public DateTime designStartDate = default(DateTime);

	public DateTime designEndDate = default(DateTime);

	private Team mTeam;

	private CarChassisStats mChassisStats;

	private Notification mNotification;

	private int mEngineModifier;

	public enum State
	{
		Designing,
		WaitingForDesign,
		Complete
	}
}
