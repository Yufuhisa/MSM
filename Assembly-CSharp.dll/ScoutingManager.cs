using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ScoutingManager : InstanceCounter
{
	public ScoutingManager()
	{
	}

	public void OnStartingGame()
	{
		this.SetupManager();
	}

	public void OnLoad()
	{
		this.SetupManager();
	}

	public void OnUnload()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Remove(time.OnDayEnd, new Action(this.UpdatedCompletedScoutsList));
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Remove(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnScoutingFacilityBuilt));
	}

	private void SetupManager()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Combine(time.OnDayEnd, new Action(this.UpdatedCompletedScoutsList));
		HQsBuilding_v1.OnBuildingNotification = (Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>)Delegate.Combine(HQsBuilding_v1.OnBuildingNotification, new Action<HQsBuilding_v1.NotificationState, HQsBuilding_v1>(this.OnScoutingFacilityBuilt));
	}

	public void UpdateUnlockedScoutingSlots()
	{
		HQsBuilding_v1 building = Game.instance.player.team.headquarters.GetBuilding(HQsBuildingInfo.Type.ScoutingFacility);
		if (building != null)
		{
			this.mMaxScoutingLevelFacility = building.info.maxLevel + 1;
			if (building.state != HQsBuilding_v1.BuildingState.NotBuilt)
			{
				this.mUnlockedScoutingSlots = building.currentLevel + 1;
			}
			else
			{
				this.mUnlockedScoutingSlots = 0;
			}
		}
		else
		{
			this.mUnlockedScoutingSlots = 0;
		}
	}

	private bool CanStartNewAssignment()
	{
		return this.mQueuedScoutingEntries.Count > 0 && this.mCurrentScoutingEntries.Count < this.mBaseScoutingSlotsCount + this.mUnlockedScoutingSlots;
	}

	private int GetScoutingTime(Driver inDriver)
	{
		if (!inDriver.IsFreeAgent() && inDriver.IsMainDriver())
		{
			return inDriver.daysToScoutShort;
		}
		return inDriver.daysToScoutLong;
	}

	public void AddScoutingAssignment(Driver inDriverToScout)
	{
		ScoutingManager.ScoutingEntry scoutingEntry = new ScoutingManager.ScoutingEntry();
		scoutingEntry.driver = inDriverToScout;
		scoutingEntry.scoutingDays = this.GetScoutingTime(inDriverToScout);
		this.mQueuedScoutingEntries.Add(scoutingEntry);
		if (this.CanStartNewAssignment())
		{
			this.StartScoutingNextDriver();
		}
		FeedbackPopup.Open(Localisation.LocaliseID("PSG_10007497", null), Localisation.LocaliseID("PSG_10007496", null));
	}

	private void StartScoutingNextDriver()
	{
		ScoutingManager.ScoutingEntry scoutingEntry = this.mQueuedScoutingEntries[0];
		this.mQueuedScoutingEntries.RemoveAt(0);
		StringVariableParser.selectedDriver = scoutingEntry.driver;
		CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
		{
			category = CalendarEventCategory.Scouting,
			showOnCalendar = true,
			triggerDate = Game.instance.time.now.AddDays((double)scoutingEntry.scoutingDays),
			triggerState = GameState.Type.FrontendState,
			OnEventTrigger = MMAction.CreateFromAction(new Action(new FinishScoutingDriverCommand(this, scoutingEntry).Execute)),
			displayEffect = new ScoutingDisplayEffect
			{
				changeDisplay = true,
				changeInterrupt = true,
				team = Game.instance.player.team
			}
		};
		calendarEvent_v.SetDynamicDescription("PSG_10009156");
		scoutingEntry.calendarEvent = calendarEvent_v;
		this.mCurrentScoutingEntries.Add(scoutingEntry);
		Game.instance.calendar.AddEvent(calendarEvent_v);
		StringVariableParser.selectedDriver = null;
	}

	public void StopAllScoutingJobs()
	{
		this.mQueuedScoutingEntries.Clear();
		for (int i = 0; i < this.mCurrentScoutingEntries.Count; i++)
		{
			if (this.mCurrentScoutingEntries[i].calendarEvent != null)
			{
				Game.instance.calendar.RemoveEvent(this.mCurrentScoutingEntries[i].calendarEvent);
			}
		}
		this.mCurrentScoutingEntries.Clear();
	}

	public void RemoveDriverFromScoutingJobs(Driver inDriver)
	{
		ScoutingManager.ScoutingEntry scoutingEntryForDriver = this.GetScoutingEntryForDriver(inDriver, this.mQueuedScoutingEntries);
		if (scoutingEntryForDriver != null)
		{
			this.mQueuedScoutingEntries.Remove(scoutingEntryForDriver);
		}
		else
		{
			scoutingEntryForDriver = this.GetScoutingEntryForDriver(inDriver, this.mCurrentScoutingEntries);
			Game.instance.calendar.RemoveEvent(scoutingEntryForDriver.calendarEvent);
			this.mCurrentScoutingEntries.Remove(scoutingEntryForDriver);
			if (this.CanStartNewAssignment())
			{
				this.StartScoutingNextDriver();
			}
		}
	}

	public void FinishScoutingDriver(ScoutingManager.ScoutingEntry inEntry)
	{
		this.MessageEndOfScouting(inEntry);
		Driver driver = inEntry.driver;
		driver.SetBeenScouted();
		this.mCurrentScoutingEntries.Remove(inEntry);
		ScoutingManager.CompletedScoutEntry completedScoutEntry = new ScoutingManager.CompletedScoutEntry();
		completedScoutEntry.timeCompleted = Game.instance.time.now;
		completedScoutEntry.driver = driver;
		this.mScoutingAssignmentsComplete.Add(completedScoutEntry);
		Notification notification = Game.instance.notificationManager.GetNotification("ScoutingComplete");
		notification.IncrementCount();
		if (!UIManager.instance.IsScreenOpen("ScoutingScreen"))
		{
			Notification notification2 = Game.instance.notificationManager.GetNotification("Scouting");
			notification2.IncrementCount();
		}
		if (UIManager.instance.IsScreenOpen("DriverScreen"))
		{
			UIManager.instance.RefreshCurrentPage(driver);
		}
		else if (UIManager.instance.IsScreenOpen("ScoutingScreen"))
		{
			ScoutingScreen screen = UIManager.instance.GetScreen<ScoutingScreen>();
			screen.Refresh();
		}
		else if (UIManager.instance.IsScreenOpen("TeamScreen"))
		{
			TeamScreen screen2 = UIManager.instance.GetScreen<TeamScreen>();
			screen2.UpdateAbility();
		}
		if (this.CanStartNewAssignment())
		{
			this.StartScoutingNextDriver();
		}
		this.UpdateScoutResultAchievements(completedScoutEntry.driver);
	}

	private void MessageEndOfScouting(ScoutingManager.ScoutingEntry inEntry)
	{
		Person personOnJob = Game.instance.player.team.contractManager.GetPersonOnJob(Contract.Job.Scout);
		StringVariableParser.subject = inEntry.driver;
		Game.instance.dialogSystem.OnScoutingCompleted(personOnJob);
	}

	public bool IsDriverCurrentlyScouted(Driver inDriver)
	{
		for (int i = 0; i < this.mCurrentScoutingEntries.Count; i++)
		{
			if (this.mCurrentScoutingEntries[i].driver == inDriver)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDriverInScoutQueue(Driver inDriver)
	{
		for (int i = 0; i < this.mQueuedScoutingEntries.Count; i++)
		{
			if (this.mQueuedScoutingEntries[i].driver == inDriver)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsScouting()
	{
		return this.mCurrentScoutingEntries.Count > 0;
	}

	private void UpdatedCompletedScoutsList()
	{
		int num = this.mScoutingAssignmentsComplete.Count;
		DateTime now = Game.instance.time.now;
		Notification notification = Game.instance.notificationManager.GetNotification("ScoutingComplete");
		for (int i = 0; i < num; i++)
		{
			ScoutingManager.CompletedScoutEntry completedScoutEntry = this.mScoutingAssignmentsComplete[i];
			if ((now - completedScoutEntry.timeCompleted).TotalDays <= 30.0)
			{
				break;
			}
			this.mScoutingAssignmentsComplete.RemoveAt(i);
			num--;
			i--;
			notification.DecrementCount();
		}
		if (UIManager.instance.IsScreenOpen("ScoutingScreen"))
		{
			ScoutingScreen screen = UIManager.instance.GetScreen<ScoutingScreen>();
			screen.Refresh();
		}
	}

	private ScoutingManager.ScoutingEntry GetScoutingEntryForDriver(Driver inDriver, List<ScoutingManager.ScoutingEntry> inListToSearch)
	{
		for (int i = 0; i < inListToSearch.Count; i++)
		{
			if (inListToSearch[i].driver == inDriver)
			{
				return inListToSearch[i];
			}
		}
		return null;
	}

	public float GetTimeLeftForScoutingDriverNormalized(Driver inDriver)
	{
		TimeSpan timeLeftForScoutingDriver = this.GetTimeLeftForScoutingDriver(inDriver);
		ScoutingManager.ScoutingEntry scoutingEntryForDriver = this.GetScoutingEntryForDriver(inDriver, this.mQueuedScoutingEntries);
		if (scoutingEntryForDriver == null)
		{
			scoutingEntryForDriver = this.GetScoutingEntryForDriver(inDriver, this.mCurrentScoutingEntries);
		}
		float num = (float)scoutingEntryForDriver.scoutingDays * 86400f;
		return Mathf.Clamp01((num - (float)timeLeftForScoutingDriver.TotalSeconds) / num);
	}

	public TimeSpan GetTimeLeftForScoutingDriver(Driver inDriver)
	{
		ScoutingManager.ScoutingEntry scoutingEntryForDriver = this.GetScoutingEntryForDriver(inDriver, this.mQueuedScoutingEntries);
		TimeSpan result = default(TimeSpan);
		if (scoutingEntryForDriver != null)
		{
			result = new TimeSpan(scoutingEntryForDriver.scoutingDays, 0, 0, 0);
		}
		else
		{
			ScoutingManager.ScoutingEntry scoutingEntryForDriver2 = this.GetScoutingEntryForDriver(inDriver, this.mCurrentScoutingEntries);
			result = scoutingEntryForDriver2.calendarEvent.triggerDate.Subtract(Game.instance.time.now);
		}
		return result;
	}

	public ScoutingManager.ScoutingEntry GetDriverInQueue(int index)
	{
		return this.mQueuedScoutingEntries[index];
	}

	public ScoutingManager.ScoutingEntry GetCurrentScoutingEntry(int index)
	{
		return this.mCurrentScoutingEntries[index];
	}

	public ScoutingManager.CompletedScoutEntry GetCompletedDriver(int index)
	{
		return this.mScoutingAssignmentsComplete[index];
	}

	public int GetDriverPositionInQueue(Driver inDriver)
	{
		int num = -1;
		for (int i = 0; i < this.mQueuedScoutingEntries.Count; i++)
		{
			if (this.mQueuedScoutingEntries[i].driver == inDriver)
			{
				num = i;
			}
		}
		return num + 1;
	}

	private void UpdateScoutResultAchievements(Driver inScoutedDriver)
	{
		/*
		if (inScoutedDriver.GetPotentialString().Equals("world class", 5) && inScoutedDriver.GetAge() < 18)
		{
			App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Scout_Five_Star_Pot_Youngster);
		}
		*/
		if (inScoutedDriver.GetDriverStats().GetAbility() >= 4.5f)
		{
			App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Scout_Five_Star_Driver);
		}
	}

	private void OnScoutingFacilityBuilt(HQsBuilding_v1.NotificationState inNotificationState, HQsBuilding_v1 inBuilding)
	{
		if (inBuilding.team != null && inBuilding.team.IsPlayersTeam() && inBuilding.info.type == HQsBuildingInfo.Type.ScoutingFacility)
		{
			bool flag = inNotificationState == HQsBuilding_v1.NotificationState.BuildComplete;
			flag |= (inNotificationState == HQsBuilding_v1.NotificationState.UpgradeComplete);
			if (flag)
			{
				int currentLevel = inBuilding.currentLevel;
				List<Driver> entityList = Game.instance.driverManager.GetEntityList();
				int num = 0;
				for (int i = 0; i < entityList.Count; i++)
				{
					if (currentLevel == entityList[i].GetDriverStats().scoutingLevelRequired - 1)
					{
						num++;
					}
				}
				Notification notification = Game.instance.notificationManager.GetNotification("ScoutingNewEntitiesToScout");
				for (int j = 0; j < num; j++)
				{
					notification.IncrementCount();
				}
				this.UpdateUnlockedScoutingSlots();
				if (this.CanStartNewAssignment())
				{
					this.StartScoutingNextDriver();
				}
			}
		}
	}

	public bool IsSlotEmpty(int inSlotIndex)
	{
		return inSlotIndex >= this.mCurrentScoutingEntries.Count;
	}

	public bool IsSlotLocked(int inSlotIndex)
	{
		return inSlotIndex >= this.mBaseScoutingSlotsCount + this.mUnlockedScoutingSlots;
	}

	public int baseScoutingSlotsCount
	{
		get
		{
			return this.mBaseScoutingSlotsCount;
		}
	}

	public Person scoutingTarget
	{
		get
		{
			return this.mCurrentScoutingEntries[0].driver;
		}
	}

	public int scoutingAssignmentsCount
	{
		get
		{
			return this.mQueuedScoutingEntries.Count;
		}
	}

	public int currentScoutingsCount
	{
		get
		{
			return this.mCurrentScoutingEntries.Count;
		}
	}

	public int scoutingAssigmentsCompleteCount
	{
		get
		{
			return this.mScoutingAssignmentsComplete.Count;
		}
	}

	public int totalScoutingSlots
	{
		get
		{
			return this.mBaseScoutingSlotsCount + this.mMaxScoutingLevelFacility;
		}
	}

	private readonly int mBaseScoutingSlotsCount = 3;

	private int mUnlockedScoutingSlots;

	private int mMaxScoutingLevelFacility = 3;

	private List<ScoutingManager.ScoutingEntry> mCurrentScoutingEntries = new List<ScoutingManager.ScoutingEntry>();

	private List<ScoutingManager.ScoutingEntry> mQueuedScoutingEntries = new List<ScoutingManager.ScoutingEntry>();

	private List<ScoutingManager.CompletedScoutEntry> mScoutingAssignmentsComplete = new List<ScoutingManager.CompletedScoutEntry>();

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	public class CompletedScoutEntry
	{
		public CompletedScoutEntry()
		{
		}

		public DateTime timeCompleted;

		public Driver driver;
	}

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	public class ScoutingEntry
	{
		public ScoutingEntry()
		{
		}

		public CalendarEvent_v1 calendarEvent;

		public int scoutingDays;

		public Driver driver;
	}
}
