using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class GameTimer : InstanceCounter
{
	public GameTimer()
	{
		this.SetSpeed(GameTimer.Speed.Slow);
	}

	public static float simulationDeltaTime
	{
		get
		{
			if (Game.IsActive() && Game.instance.time.isPaused)
			{
				return 0f;
			}
			return GameTimer.baseSimulationDeltaTime;
		}
	}

	public static float deltaTime
	{
		get
		{
			return Mathf.Min(Time.unscaledDeltaTime, GameTimer.maxDeltaTimeRecip);
		}
	}

	public void OnLoad()
	{
		this.SetSimSkipSpeed(this.GetSavedSpeed());
	}

	public void OnDestroy()
	{
		SafeAction.NullAnAction(ref this.OnYearEnd);
		SafeAction.NullAnAction(ref this.OnMonthEnd);
		SafeAction.NullAnAction(ref this.OnHourEnd);
		SafeAction.NullAnAction(ref this.OnDayEnd);
		SafeAction.NullAnAction(ref this.OnWeekEnd);
		SafeAction.NullAnAction(ref this.OnPause);
		SafeAction.NullAnAction(ref this.OnPlay);
		SafeAction.NullAnAction(ref this.OnChangeSpeed);
		SafeAction.NullAnAction(ref this.OnSkipTargetReached);
		SafeAction.NullAnAction<GameTimer.TimeState>(ref this.OnChangeTimeState);
	}

	public void Update()
	{
		GameTimer.TimeState timeState = this.mTimeState;
		if (timeState != GameTimer.TimeState.Standard)
		{
			if (timeState == GameTimer.TimeState.Skip)
			{
				this.UpdateSkip();
			}
		}
		else if (this.isPaused)
		{
			this.mDeltaTime = 0f;
		}
		else
		{
			this.UpdateRegular();
		}
	}

	private void UpdateSkip()
	{
		GameState currentState = App.instance.gameStateManager.currentState;
		CalendarEvent_v1 nextPausableEventForGameState = Game.instance.calendar.GetNextPausableEventForGameState((!(currentState is PreSeasonState)) ? GameState.Type.FrontendState : GameState.Type.PreSeasonState, true);
		if (nextPausableEventForGameState != null && currentState.IsFrontend())
		{
			this.UnPause(GameTimer.PauseType.Game);
			DateTime triggerDate = nextPausableEventForGameState.triggerDate;
			float normalisedTimeToNextEvent = Game.instance.calendar.GetNormalisedTimeToNextEvent();
			float num = EasingUtility.EaseByType(EasingUtility.Easing.InOutQuad, GameTimer.minSkipSpeed, GameTimer.maxSkipSpeed, normalisedTimeToNextEvent);
			if (currentState.type == GameState.Type.PreSeasonState)
			{
				num = this.mPreSeasonSkipSpeed;
			}
			if (num < GameTimer.debugSkipSpeed)
			{
				num = GameTimer.debugSkipSpeed;
			}
			this.mDeltaTime = GameTimer.deltaTime * num;
			DateTime inTime = this.mNow;
			if (this.mNow.AddSeconds((double)this.mDeltaTime) >= triggerDate && !Game.instance.tutorialSystem.isTutorialOnScreen)
			{
				if (this.OnSkipTargetReached != null)
				{
					this.OnSkipTargetReached();
				}
				this.Pause(GameTimer.PauseType.Game);
				this.SetTime(triggerDate);
				Game.instance.calendar.Update();
				this.SetTimeState(GameTimer.TimeState.Standard);
				if (!Game.instance.player.IsUnemployed() && Game.instance.stateInfo.isReadyToGoToRace)
				{
					UIManager.instance.ChangeScreen("HomeScreen", UIManager.ScreenTransition.None, 0f, null, UIManager.NavigationType.Normal);
				}
			}
			else
			{
				this.SetTime(this.mNow.AddSeconds((double)this.mDeltaTime));
			}
			this.TriggerActions(inTime);
		}
		else
		{
			this.Pause(GameTimer.PauseType.Game);
		}
	}

	private void UpdateRegular()
	{
		GameState currentState = App.instance.gameStateManager.currentState;
		this.mDeltaTime = 0f;
		if (currentState.IsSimulation())
		{
			this.mDeltaTime = GameTimer.simulationDeltaTime;
		}
		else
		{
			this.mDeltaTime = GameTimer.deltaTime * this.GetTimeScale();
		}
		DateTime inTime = this.mNow;
		this.SetTime(this.mNow.AddSeconds((double)this.mDeltaTime));
		this.TriggerActions(inTime);
		if (Game.instance.stateInfo.isReadyToGoToRace)
		{
			this.mDeltaTime = 0f;
			this.Pause(GameTimer.PauseType.Game);
		}
	}

	private void TriggerActions(DateTime inTime)
	{
		DateTime dateTime = inTime;
		if (dateTime.Year != this.mNow.Year && this.OnYearEnd != null)
		{
			this.OnYearEnd();
		}
		if (dateTime.Month != this.mNow.Month && this.OnMonthEnd != null)
		{
			this.OnMonthEnd();
		}
		if (dateTime.Hour != this.mNow.Hour && this.OnHourEnd != null)
		{
			this.OnHourEnd();
		}
		if (dateTime.Day != this.mNow.Day && this.OnDayEnd != null)
		{
			this.OnDayEnd();
		}
		if (dateTime.DayOfWeek == DayOfWeek.Sunday && this.mNow.DayOfWeek == DayOfWeek.Monday && this.OnWeekEnd != null)
		{
			this.OnWeekEnd();
		}
	}

	public void SetTimeState(GameTimer.TimeState inState)
	{
		this.mTimeState = inState;
		if (this.OnChangeTimeState != null)
		{
			this.OnChangeTimeState(this.mTimeState);
		}
	}

	public void UpdateInput()
	{
		if (this.CanUseShortcutsToChangeSpeedOrPause())
		{
			GameState currentState = App.instance.gameStateManager.currentState;
			if (currentState is SessionState)
			{
				if (InputManager.instance.GetInput(KeyBinding.Name.Speedx1))
				{
					if (currentState is SkipSessionState)
					{
						this.SetSimSkipSpeed(GameTimer.SimSkipSpeed.Slow);
					}
					else
					{
						this.SetSpeed(GameTimer.Speed.Slow);
					}
				}
				else if (InputManager.instance.GetInput(KeyBinding.Name.Speedx2))
				{
					if (currentState is SkipSessionState)
					{
						this.SetSimSkipSpeed(GameTimer.SimSkipSpeed.Medium);
					}
					else
					{
						this.SetSpeed(GameTimer.Speed.Medium);
					}
				}
				else if (InputManager.instance.GetInput(KeyBinding.Name.Speedx3))
				{
					if (currentState is SkipSessionState)
					{
						this.SetSimSkipSpeed(GameTimer.SimSkipSpeed.Fast);
					}
					else
					{
						this.SetSpeed(GameTimer.Speed.Fast);
					}
				}
			}
			if (InputManager.instance.GetInput(KeyBinding.Name.Pause))
			{
				bool flag = currentState is FrontendState || currentState is SessionState;
				if (currentState is SkipSessionState)
				{
					this.PauseOrPlaySkipSim();
				}
				else if (flag)
				{
					if (currentState is FrontendState)
					{
						if (!this.isPaused)
						{
							this.Pause(GameTimer.PauseType.Game);
						}
					}
					else if (this.isPaused)
					{
						this.UnPause(GameTimer.PauseType.Game);
						scSoundManager.Instance.UnPause(false);
					}
					else
					{
						this.Pause(GameTimer.PauseType.Game);
						scSoundManager.Instance.Pause(true, false, false);
					}
				}
			}
		}
	}

	private bool CanUseShortcutsToChangeSpeedOrPause()
	{
		if (Game.instance.tutorialSystem.isShortcutInputBlocked)
		{
			return false;
		}
		GameState currentState = App.instance.gameStateManager.currentState;
		UIScreen currentScreen = UIManager.instance.currentScreen;
		if (currentScreen is CreatePlayerScreen || currentScreen is ChooseSeriesScreen || currentScreen is ChooseTeamScreen)
		{
			return false;
		}
		if (currentState is SessionState && !(currentScreen is SessionHUD) && !(currentScreen is SkipSessionScreen))
		{
			return false;
		}
		if (this.inGamePauseMenu2 == null && UIManager.InstanceExists)
		{
			this.inGamePauseMenu2 = UIManager.instance.dialogBoxManager.GetDialog<InGamePauseMenu>();
		}
		return !this.inGamePauseMenu2.isActiveAndEnabled && !UIManager.instance.dialogBoxManager.GetDialog<SaveLoadDialog>().isActiveAndEnabled;
	}

	private float GetTimeScale()
	{
		GameState currentState = App.instance.gameStateManager.currentState;
		if (currentState.IsFrontend())
		{
			return this.speedMultipliers[0, (int)this.mSpeed];
		}
		if (currentState.IsEvent())
		{
			return this.speedMultipliers[1, (int)this.mSpeed];
		}
		return 1f;
	}

	public float GetSimulationTimeScale()
	{
		if (this.mSpeed == GameTimer.Speed.Medium && GameTimer.debugSimSpeed != 0f)
		{
			return GameTimer.debugSimSpeed;
		}
		if (Game.instance.sessionManager.fasterSimSpeedActive)
		{
			return Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.GetSimulationTimeScale();
		}
		if (Game.instance.sessionManager.isSkippingSession)
		{
			return GameTimer.skipSimSpeed[(int)this.mSkipSessionSpeed];
		}
		return this.speedMultipliers[2, (int)this.mSpeed];
	}

	public void SetSpeed(GameTimer.Speed inSpeed)
	{
		this.mSpeed = inSpeed;
		if (this.OnChangeSpeed != null)
		{
			this.OnChangeSpeed();
		}
		if (this.isPaused)
		{
			scSoundManager.Instance.UnPause(false);
			this.UnPause(GameTimer.PauseType.Game);
		}
	}

	public void SetSpeedDontUnpause(GameTimer.Speed inSpeed)
	{
		this.mSpeed = inSpeed;
	}

	public void Pause(GameTimer.PauseType inPauseType)
	{
		if (inPauseType == GameTimer.PauseType.Tutorial || inPauseType == GameTimer.PauseType.Dilemma)
		{
			scSoundManager.Instance.Pause(false, false, true);
			if (inPauseType == GameTimer.PauseType.Tutorial)
			{
				scSoundManager.Instance.StartTutorialPaused(true);
			}
			else
			{
				scSoundManager.Instance.StartTutorialPaused(false);
			}
		}
		this.SetTimeState(GameTimer.TimeState.Standard);
		bool isPaused = this.isPaused;
		this.mPauseState[(int)inPauseType] = true;
		this.UpdatePauseState(isPaused);
	}

	public void UnPause(GameTimer.PauseType inPauseType)
	{
		bool isPaused = this.isPaused;
		if (inPauseType == GameTimer.PauseType.Tutorial || inPauseType == GameTimer.PauseType.Dilemma)
		{
			scSoundManager.Instance.UnPause(true);
			scSoundManager.Instance.EndTutorialPaused();
		}
		if (inPauseType == GameTimer.PauseType.Game)
		{
			for (int i = 0; i < this.mPauseState.Length; i++)
			{
				this.mPauseState[i] = false;
			}
		}
		else
		{
			this.mPauseState[(int)inPauseType] = false;
		}
		this.UpdatePauseState(isPaused);
	}

	public bool IsPauseTypeActive(GameTimer.PauseType inPauseType)
	{
		return this.mPauseState[(int)inPauseType];
	}

	private void UpdatePauseState(bool inWasPaused)
	{
		if (!this.isPaused && inWasPaused)
		{
			Time.timeScale = 1f;
			if (this.OnPlay != null)
			{
				this.OnPlay();
			}
		}
		else if (this.isPaused && !inWasPaused)
		{
			if (App.instance.gameStateManager.currentState.IsSimulation())
			{
				Time.timeScale = 0f;
			}
			else
			{
				Time.timeScale = 1f;
			}
			if (this.OnPause != null)
			{
				this.OnPause();
			}
		}
	}

	public void SetTime(DateTime inTime)
	{
		this.mNow = inTime;
	}

	public void AddDays(int inDays)
	{
		this.SetTime(this.mNow.AddDays((double)inDays));
	}

	public void AddHours(int inHours)
	{
		this.SetTime(this.mNow.AddHours((double)inHours));
	}

	public void AddMinutes(int inMinutes)
	{
		this.SetTime(this.mNow.AddMinutes((double)inMinutes));
	}

	public void SetFrontendTimeScaleForSpeedBand(GameTimer.SpeedMode speedMode, GameTimer.Speed speedBand, float speedMultiplier)
	{
		this.speedMultipliers[(int)speedMode, (int)speedBand] = speedMultiplier;
	}

	public float GetFrontendTimeScaleForSpeedBand(GameTimer.SpeedMode speedMode, GameTimer.Speed speedBand)
	{
		return this.speedMultipliers[(int)speedMode, (int)speedBand];
	}

	public GameTimer.SimSkipSpeed GetSavedSpeed()
	{
		if (PlayerPrefs.HasKey("mSkipSessionSpeed"))
		{
			return (GameTimer.SimSkipSpeed)PlayerPrefs.GetInt("mSkipSessionSpeed", 1);
		}
		return GameTimer.SimSkipSpeed.Slow;
	}

	public void PauseOrPlaySkipSim()
	{
		if (this.mSkipSessionSpeed == GameTimer.SimSkipSpeed.Pause)
		{
			this.mSkipSessionSpeed = this.GetSavedSpeed();
		}
		else
		{
			this.mSkipSessionSpeed = GameTimer.SimSkipSpeed.Pause;
		}
	}

	public void PlaySkipSim()
	{
		if (this.mSkipSessionSpeed == GameTimer.SimSkipSpeed.Pause)
		{
			this.mSkipSessionSpeed = this.GetSavedSpeed();
		}
	}

	public void IncreaseSkipSimSpeed()
	{
		if (this.mSkipSessionSpeed == GameTimer.SimSkipSpeed.Fast)
		{
			this.mSkipSessionSpeed = GameTimer.SimSkipSpeed.Slow;
		}
		else
		{
			this.mSkipSessionSpeed++;
		}
		this.SetSimSkipSpeed(this.mSkipSessionSpeed);
	}

	public void SetSimSkipSpeed(GameTimer.SimSkipSpeed inSpeed)
	{
		this.mSkipSessionSpeed = inSpeed;
		PlayerPrefs.SetInt("mSkipSessionSpeed", (int)this.mSkipSessionSpeed);
	}

	public int monthsLeftInThisYear
	{
		get
		{
			return 12 - this.mNow.Month;
		}
	}

	public DateTime now
	{
		get
		{
			return this.mNow;
		}
	}

	public GameTimer.Speed speed
	{
		get
		{
			return this.mSpeed;
		}
	}

	public GameTimer.SimSkipSpeed simSkipSpeed
	{
		get
		{
			return this.mSkipSessionSpeed;
		}
	}

	public bool isPaused
	{
		get
		{
			for (int i = 0; i < this.mPauseState.Length; i++)
			{
				if (this.mPauseState[i])
				{
					return true;
				}
			}
			return false;
		}
	}

	public float cachedDeltaTime
	{
		get
		{
			return this.mDeltaTime;
		}
	}

	public GameTimer.TimeState timeState
	{
		get
		{
			return this.mTimeState;
		}
	}

	public Action OnYearEnd;

	public Action OnMonthEnd;

	public Action OnHourEnd;

	public Action OnDayEnd;

	public Action OnWeekEnd;

	public Action OnPause;

	public Action OnPlay;

	public Action OnChangeSpeed;

	public Action OnSkipTargetReached;

	public Action<GameTimer.TimeState> OnChangeTimeState;

	public static float baseSimulationDeltaTime = 0.033333335f;

	private static float maxDeltaTimeRecip = 0.06666667f;

	public static float totalSimulationDeltaTimeCurrentFrame = 0f;

	public static float debugSimSpeed = 0f;

	public static float[] skipSimSpeed = new float[]
	{
		0f,
		20f,
		30f,
		40f
	};

	public static float[] skipSimEventSpeeds = new float[]
	{
		0f,
		1.75f,
		2.75f,
		3.75f
	};

	public static float debugSkipSpeed = 0f;

	public static float minSkipSpeed = 60000f;

	public static float maxSkipSpeed = 120000f;

	public static DateTime gameStartDate = new DateTime(2016, 3, 1);

	private readonly float mPreSeasonSkipSpeed = 300000f;

	private DateTime mNow = GameTimer.gameStartDate;

	private float mDeltaTime;

	[NonSerialized]
	private GameTimer.SimSkipSpeed mSkipSessionSpeed = GameTimer.SimSkipSpeed.Slow;

	[NonSerialized]
	private GameTimer.Speed mSpeed = GameTimer.Speed.Medium;

	private GameTimer.TimeState mTimeState;

	[NonSerialized]
	private float[,] speedMultipliers = new float[,]
	{
		{
			250f,
			2500f,
			25000f
		},
		{
			1f,
			10f,
			50f
		},
		{
			1.75f,
			2.75f,
			3.75f
		}
	};

	private bool[] mPauseState = new bool[5];

	[NonSerialized]
	private InGamePauseMenu inGamePauseMenu2;

	public enum Speed
	{
		Slow,
		Medium,
		Fast
	}

	public enum SimSkipSpeed
	{
		Pause,
		Slow,
		Medium,
		Fast
	}

	public enum SpeedMode
	{
		Frontend,
		Event,
		Simulation
	}

	public enum TimeState
	{
		Standard,
		Skip
	}

	public enum PauseType
	{
		Game,
		App,
		UI,
		Tutorial,
		Dilemma,
		Count
	}
}
