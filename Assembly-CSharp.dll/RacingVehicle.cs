using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class RacingVehicle : Vehicle
{
	public RacingVehicle(int inID, int inCarID, Driver inDriver, Car inCar, UnityVehicleManager inUnityVehicleManager)
	{
		this.id = inID;
		this.carID = inCarID;
		this.driver = inDriver;
		this.mIsPlayerDriver = this.driver.IsPlayersDriver();
		this.SetupDriversForCar();
		base.championship = this.driver.contract.GetTeam().championship;
		this.car = inCar;
		this.name = this.driver.shortName;
		this.enabled = true;
		base.unityVehicleManager = inUnityVehicleManager;
		this.CacheDriversNames();
		if (!SimulationUtility.IsEventState())
		{
			return;
		}
		this.driver.driverForm.OnRaceDrivingStintStart(-1f);
		base.pathState.Start(this);
		this.mERSController.Start(this);
		base.performance.Start(this);
		base.steeringManager.Start(this);
		base.speedManager.Start(this);
		base.pathController.Start(this);
		base.behaviourManager.Start(this);
		this.mPartFailure.Start(this);
		this.mSessionEvents.Start(this);
		this.mStrategy.Start(this);
		this.mPracticeKnowledge.Start(this);
		this.mSetup.Start(this);
		this.mTimer.Start(this);
		this.mStats.Start(this);
		this.mStints.Start(this);
		this.mTeamRadio.Start(this);
		this.mBonuses.Start(this);
		this.mSessionPenalty.Start(this);
		this.mSessionAIOrderController2.Start(this);
		base.throttle.SetValue(0f, AnimatedFloat.Animation.DontAnimate);
		if (base.unityVehicleManager != null)
		{
			base.unityVehicle = base.unityVehicleManager.CreateCar(this.name, this);
		}
		base.CreateCollisionBounds();
		this.mSessionData.racedWithWorstCar = (this.car.carManager.GetCarWithHighestTotalStats() != this.car);
		base.performance.bonuses.RefreshCarPartBonuses();
	}

	public override void OnLoad(UnityVehicleManager unityVehicleManager)
	{
		base.OnLoad(unityVehicleManager);
		base.championship = this.driver.contract.GetTeam().championship;
		if (this.mSessionAIOrderController2 == null)
		{
			this.mSessionAIOrderController2 = new SessionAIOrderController();
			this.mSessionAIOrderController2.Start(this);
		}
		if (this.mSessionEvents == null)
		{
			this.mSessionEvents = new SessionEvents();
			this.mSessionEvents.Start(this);
		}
		if (this.mERSController == null)
		{
			this.mERSController = new ERSController();
			this.mERSController.Start(this);
		}
		this.mStrategy.OnLoad();
		this.mTeamRadio.OnLoad();
		this.mPartFailure.OnLoad(this);
		this.mSetup.OnLoad();
		this.timer.OnLoad();
		this.mIsPlayerDriver = this.driver.IsPlayersDriver();
		this.SetupDriversForCar();
		this.mERSController.OnLoad();
		this.CacheDriversNames();
	}

	public void SwapDriver(Driver inDriver)
	{
		bool flag = inDriver != this.driver;
		this.driver.driverForm.OnRaceDrivingStintEnd(-1f);
		this.driver.driverStamina.SetState(DriverStamina.DriverState.Resting);
		this.driver = inDriver;
		this.driver.driverStamina.SetState(DriverStamina.DriverState.Driving);
		this.driver.driverForm.OnRaceDrivingStintStart(-1f);
		this.SetupDriversForCar();
		this.setup.SetCurrentDriver(this.driver);
		if (flag && this.OnDriverChange != null)
		{
			this.OnDriverChange.Invoke();
		}
		this.mIsPlayerDriver = this.driver.IsPlayersDriver();
		this.mRefreshTyrePerformance = true;
		if (!Game.instance.sessionManager.isSessionActive)
		{
			this.strategy.RefreshAllTyresPerformanceLevel(null);
		}
		this.CacheDriversNames();
	}

	public void OnSessionEnd()
	{
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race)
		{
			for (int i = 0; i < this.mDriversForCar.Length; i++)
			{
				this.mDriversForCar[i].driverForm.RecordAverageForm(base.championship.GetCurrentEventDetails());
				this.mDriversForCar[i].driverStamina.Reset();
			}
		}
	}

	public void SetupDriversForCar()
	{
		this.mDriversForCar = this.driver.contract.GetTeam().GetSelectedDriversForCar(this.carID);
	}

	public override void Destroy()
	{
		SafeAction.NullAnAction(ref this.OnDriverChange);
		Game.instance.persistentEventData.SaveTyreData(this);
		if (base.unityVehicleManager != null)
		{
			base.unityVehicleManager.DestroyCar(base.unityVehicle);
			base.unityVehicle = null;
			base.unityVehicleManager = null;
		}
	}

	public override void Hide()
	{
		if (base.unityVehicle != null)
		{
			base.unityVehicle.gameObject.SetActive(false);
		}
	}

	public override void UnHide()
	{
		if (base.unityVehicle != null)
		{
			base.unityVehicle.gameObject.SetActive(true);
		}
	}

	public override void Update()
	{
		base.Update();
		this.mSetup.Update();
		this.CalculateLapsBehindLeader();
	}

	public override void SimulationUpdate()
	{
		base.SimulationUpdate();
		this.mTimeUntilNextPreviousPositionUpdate -= GameTimer.simulationDeltaTime;
		this.mTimer.SimulationUpdate();
		this.mPathController.SimulationUpdate();
		this.mPathStateManager.SimulationUpdate();
		if (Game.instance.sessionManager.isSessionActive)
		{
			this.mStrategy.SimulationUpdate();
		}
		this.mSetup.SimulationUpdate();
		this.mSessionPenalty.SimulationUpdate();
		if (!Game.instance.sessionManager.isSkippingSession)
		{
			this.mTeamRadio.SimulationUpdate();
		}
		if (this.movementEnabled)
		{
			base.UpdateSteering();
			base.UpdateSpeed();
			base.throttle.Update();
		}
		else
		{
			base.velocity = Vector3.zero;
			this.speed = 0f;
		}
		base.pathController.UpdatePathPositionData();
		this.practiceKnowledge.UpdateBonuses();
		if (Game.instance.sessionManager.isSessionActive && Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race && !Game.instance.sessionManager.isRollingOut)
		{
			this.mERSController.SimulationUpdate();
		}
		if (this.mRefreshTyrePerformance)
		{
			this.mRefreshTyrePerformance = false;
			this.strategy.RefreshAllTyresPerformanceLevel(null);
		}
	}

	public override float GetTopSpeed()
	{
		return base.performance.GetTopSpeed();
	}

	public override float GetBraking()
	{
		float num = Math.Max(base.performance.GetBraking(), base.braking);
		if (base.behaviourManager.currentBehaviour is AIOvertakingBehaviour)
		{
			num *= 1.05f;
		}
		else if (base.behaviourManager.currentBehaviour is AISpinBehaviour || base.behaviourManager.currentBehaviour is AICrashingBehaviour)
		{
			num = base.braking;
		}
		return num;
	}

	public override float GetAcceleration()
	{
		return Math.Min(base.performance.GetAcceleration(), base.acceleration);
	}

	public override void SetBraking(float inValue)
	{
		base.SetBraking(inValue);
	}

	public override void SetAcceleration(float inValue)
	{
		base.SetAcceleration(inValue);
	}

	public override void SetMaxSpeed(float inValue)
	{
		base.SetMaxSpeed(inValue);
	}

	public override bool HandleMessage(Vehicle inSender, AIMessage.Type inType, object inData)
	{
		return this.mBehaviourManager.HandleMessage(inSender, inType, inData);
	}

	public override void OnEnterPath(PathController.PathType inPath)
	{
		base.OnEnterPath(inPath);
		
		// if enter Track from PitlaneExit disable collision, most of the times (95%)
		if (this.mPathController.currentPathType == PathController.PathType.PitlaneExit && inPath == PathController.PathType.Track && RandomUtility.GetRandom01() < 0.95f)
			this.mCollisionCooldown = 5f;
		
		switch (this.strategy.status)
		{
		case SessionStrategy.Status.Pitting:
		case SessionStrategy.Status.ReturningToGarage:
			if (inPath == PathController.PathType.PitlaneEntry)
			{
				this.timer.currentLap.AddEvent(LapDetailsData.LapEvents.PitStop);
			}
			break;
		case SessionStrategy.Status.PitThruPenalty:
			if (inPath == PathController.PathType.PitlaneEntry)
			{
				this.timer.currentLap.AddEvent(LapDetailsData.LapEvents.PenaltyDriveTrought);
			}
			break;
		}
	}

	public override bool HasRunWide()
	{
		return false;
	}

	public override bool HasStopped()
	{
		return this.speed < 3f;
	}

	public override void OnFlagChange(SessionManager.Flag inFlag)
	{
		base.OnFlagChange(inFlag);
		this.strategy.OnFlagChanged(inFlag);
		this.mERSController.OnFlagChanged(inFlag);
		if (this.mIsPlayerDriver)
		{
			RadioMessageSafetyCar radioMessage = this.teamRadio.GetRadioMessage<RadioMessageSafetyCar>();
			if (radioMessage != null)
			{
				radioMessage.OnFlagChange();
			}
		}
	}

	public void SetConcurrentStandingsPosition(int inPosition)
	{
		this.mConcurrentStandingsPosition = inPosition;
	}

	public void SetStandingsPosition(int inPosition)
	{
		this.HandleCommentary(inPosition);
		if (Game.instance.sessionManager.isSessionActive)
		{
			if (this.mPreviousStandingsPosition != inPosition && this.mTimeUntilNextPreviousPositionUpdate < 0f)
			{
				this.mPreviousStandingsPosition = this.mStandingsPosition;
				this.mTimeUntilNextPreviousPositionUpdate = 60f;
			}
		}
		else
		{
			this.mOvertakes = 0;
			this.mPreviousStandingsPosition = inPosition;
		}
		this.RecordOvertakes(inPosition);
		this.mStandingsPosition = inPosition;
	}

	private void RecordOvertakes(int inPosition)
	{
		if (base.pathController.currentPathType == PathController.PathType.Track)
		{
			if (inPosition < this.mStandingsPosition)
			{
				RacingVehicle racingVehicle = Game.instance.sessionManager.standings[Mathf.Clamp(inPosition + 1, 0, Game.instance.sessionManager.standings.Count - 1)];
				if (racingVehicle != this && racingVehicle.pathController.currentPathType == PathController.PathType.Track)
				{
					this.mOvertakes++;
				}
			}
			else if (inPosition > this.mStandingsPosition)
			{
				this.mOvertakes--;
			}
		}
	}

	public void HandleCommentary(int inPosition)
	{
		if (this.mPreviousStandingsPositionForCommentary == 0)
		{
			this.mPreviousStandingsPositionForCommentary = inPosition;
		}
		if (!this.mWaitingForCooldown || this.mStandingsPosition != inPosition)
		{
			if (!this.mWaitingForCooldown)
			{
				this.mPreviousStandingsPositionForCommentary = this.mStandingsPosition;
			}
			this.mCommentaryCooldown = 5f;
			this.mWaitingForCooldown = true;
		}
		this.mCommentaryCooldown -= GameTimer.deltaTime;
		if (Game.instance.sessionManager.isSessionActive && this.mPreviousStandingsPositionForCommentary != this.mStandingsPosition)
		{
			bool flag = this.mPreviousStandingsPositionForCommentary > this.mStandingsPosition;
			bool flag2 = this.mPreviousStandingsPositionForCommentary < this.mStandingsPosition;
			int num = this.mPreviousStandingsPositionForCommentary - this.mStandingsPosition;
			if (flag)
			{
				if (this.mIsPlayerDriver)
				{
					Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.PositionGainedOvertake, this);
				}
				if (this.mCommentaryCooldown <= 0f && this.timer.fastestLap != null)
				{
					if (this.mStandingsPosition == 1)
					{
						CommentaryManager.SendComment(this, Comment.CommentType.New1stPlaceDriver, new object[]
						{
							this.driver,
							this.behind.driver
						});
					}
					else
					{
						DialogQuery dialogQuery = new DialogQuery();
						dialogQuery.AddCriteria("DriversOvertaken", num.ToString());
						CommentaryManager.SendComment(this, Comment.CommentType.Overtakes, dialogQuery, new object[]
						{
							this.driver,
							this.behind.driver
						});
					}
					this.mWaitingForCooldown = false;
				}
			}
			if (flag2)
			{
				if (this.mIsPlayerDriver)
				{
					Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.PositionLostOvertaken, this);
				}
				if (this.mCommentaryCooldown <= 0f)
				{
					CommentaryManager.SendComment(this, Comment.CommentType.DriverDropsPosition, new object[]
					{
						this.driver
					});
					StringVariableParser.playerDriversDropedPositionCount = 0;
					this.mWaitingForCooldown = false;
				}
			}
		}
	}

	public override void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		base.OnEnterGate(inGateID, inGateType);
		this.mBehaviourManager.OnEnterGate(inGateID, inGateType);
		this.mSessionEvents.OnEnterGate(inGateID, inGateType);
		this.mStrategy.OnEnterGate(inGateID, inGateType);
		bool flag = inGateID == Game.instance.sessionManager.circuit.pitlaneEntryTrackPathID - 15;
		if (inGateID % GameStatsConstants.aiOrderRefreshGateRate == 0 || flag)
		{
			this.mSessionAIOrderController2.UpdateAIOrders(flag);
		}
		this.mERSController.OnEnterGate(inGateID, inGateType);
	}

	public override void OnEnterStraight(PathData.Straight inStraight)
	{
		base.OnEnterStraight(inStraight);
		this.mBehaviourManager.OnEnterStraight(inStraight);
	}

	public override void OnEnterCorner(PathData.Corner inCorner)
	{
		base.OnEnterCorner(inCorner);
		this.mBehaviourManager.OnEnterCorner(inCorner);
	}

	public override bool CanPassVehicle(Vehicle inVehicle)
	{
		if (this.mTimer.hasSeenChequeredFlag)
		{
			return inVehicle.performance.IsExperiencingCriticalIssue();
		}
		bool flag = base.behaviourManager.CanAttackVehicle();
		if (!flag && inVehicle is RacingVehicle)
		{
			flag = !((RacingVehicle)inVehicle).behaviourManager.CanDefendVehicle();
		}
		if (this.mIsPlayerDriver && inVehicle is RacingVehicle)
		{
			RacingVehicle racingVehicle = inVehicle as RacingVehicle;
			if (racingVehicle.isPlayerDriver && this.strategy.teamOrders == SessionStrategy.TeamOrders.AllowTeamMateThrough)
			{
				if (racingVehicle.performance.IsExperiencingCriticalIssue())
				{
					return flag;
				}
				if (this.timer.lap == racingVehicle.timer.lap)
				{
					return false;
				}
			}
		}
		return flag;
	}

	public override bool IsLightOn()
	{
		if (base.championship.series == Championship.Series.GTSeries)
		{
			return this.mPedalState == Vehicle.PedalState.Braking;
		}
		bool result = false;
		if (base.performance.fuel.engineMode == Fuel.EngineMode.Low)
		{
			result = true;
		}
		if (base.performance.IsExperiencingCriticalIssue())
		{
			result = true;
		}
		return result;
	}

	public void LogSpeedTrapSpeed()
	{
		this.mSpeedTrapSpeed = this.speed;
	}

	private void CalculateLapsBehindLeader()
	{
		this.mLapsBehindLeader = 0;
		SessionManager sessionManager = Game.instance.sessionManager;
		if (sessionManager.sessionType == SessionDetails.SessionType.Race)
		{
			RacingVehicle racingVehicle = sessionManager.GetLeader();
			if (racingVehicle != null)
			{
				this.mLapsBehindLeader = (int)((racingVehicle.timer.sessionDistanceTraveled - this.timer.sessionDistanceTraveled) / (float)base.pathController.GetPath(PathController.PathType.Track).data.gates.Count);
				this.mLapsBehindLeader = Mathf.Max(this.mLapsBehindLeader, 0);
			}
		}
	}

	public int GetLapsBehindLeader()
	{
		return this.mLapsBehindLeader;
	}

	private void CacheDriversNames()
	{
		this.mDriverNameCache = this.driver.contract.GetTeam().GetVehicleNameSession(this.driver);
	}

	public string GetName()
	{
		return this.mDriverNameCache;
	}

	public SessionTimer timer
	{
		get
		{
			return this.mTimer;
		}
	}

	public SessionStats stats
	{
		get
		{
			return this.mStats;
		}
	}

	public SessionStrategy strategy
	{
		get
		{
			return this.mStrategy;
		}
	}

	public SessionSetup setup
	{
		get
		{
			return this.mSetup;
		}
	}

	public SessionStints stints
	{
		get
		{
			return this.mStints;
		}
	}

	public SessionCarBonuses bonuses
	{
		get
		{
			return this.mBonuses;
		}
	}

	public int previousPositionForCommentary
	{
		get
		{
			return this.mPreviousStandingsPositionForCommentary;
		}
	}

	public int standingsPosition
	{
		get
		{
			return this.mStandingsPosition;
		}
	}

	public int previousStandingsPosition
	{
		get
		{
			return this.mPreviousStandingsPosition;
		}
	}

	public int sessionSetupCount
	{
		get
		{
			return this.mSessionSetupCount;
		}
	}

	public bool isFavourite
	{
		get
		{
			return this.mFavourite;
		}
	}

	public PracticeKnowledge practiceKnowledge
	{
		get
		{
			return this.mPracticeKnowledge;
		}
	}

	public TeamRadio teamRadio
	{
		get
		{
			return this.mTeamRadio;
		}
	}

	public SessionPartFailure partFailure
	{
		get
		{
			return this.mPartFailure;
		}
	}

	public SessionPenalty sessionPenalty
	{
		get
		{
			return this.mSessionPenalty;
		}
	}

	public int penaltiesCount
	{
		get
		{
			return this.mSessionPenalty.penalties.Count;
		}
	}

	public SessionAIOrderController sessionAIOrderController
	{
		get
		{
			return this.mSessionAIOrderController2;
		}
	}

	public SessionEvents sessionEvents
	{
		get
		{
			return this.mSessionEvents;
		}
	}

	public float speedTrapSpeed
	{
		get
		{
			return this.mSpeedTrapSpeed;
		}
	}

	public ERSController ERSController
	{
		get
		{
			return this.mERSController;
		}
	}

	public RaceEventResults.ResultData sessionData
	{
		get
		{
			if (this.mSessionData == null)
			{
				this.mSessionData = new RaceEventResults.ResultData();
			}
			return this.mSessionData;
		}
		set
		{
			this.mSessionData = value;
		}
	}

	public int concurrentStandingsPosition
	{
		get
		{
			return this.mConcurrentStandingsPosition;
		}
	}

	public Driver[] driversForCar
	{
		get
		{
			return this.mDriversForCar;
		}
	}

	public bool isPlayerDriver
	{
		get
		{
			return this.mIsPlayerDriver;
		}
	}

	public int overtakesDelta
	{
		get
		{
			return this.mOvertakes;
		}
	}

	public Action OnDriverChange;

	public Driver driver;

	public RacingVehicle leader;

	public RacingVehicle ahead;

	public RacingVehicle behind;

	public RaceEventResults.ResultData resultData;

	public int carID;

	public Car car;

	private SessionTimer mTimer = new SessionTimer();

	private SessionStats mStats = new SessionStats();

	private SessionStrategy mStrategy = new SessionStrategy();

	private SessionSetup mSetup = new SessionSetup();

	private SessionStints mStints = new SessionStints();

	private SessionPartFailure mPartFailure = new SessionPartFailure();

	private SessionCarBonuses mBonuses = new SessionCarBonuses();

	private SessionEvents mSessionEvents = new SessionEvents();

	private PracticeKnowledge mPracticeKnowledge = new PracticeKnowledge();

	private SessionPenalty mSessionPenalty = new SessionPenalty();

	private ERSController mERSController = new ERSController();

	[NonSerialized]
	private SessionAIOrderController mSessionAIOrderController2 = new SessionAIOrderController();

	private TeamRadio mTeamRadio = new TeamRadio();

	private int mConcurrentStandingsPosition;

	private int mStandingsPosition;

	private int mPreviousStandingsPosition;

	private int mPreviousStandingsPositionForCommentary;

	private float mTimeUntilNextPreviousPositionUpdate;

	private int mLapsBehindLeader;

	private int mSessionSetupCount;

	private int mOvertakes;

	private bool mFavourite;

	private float mCommentaryCooldown;

	private bool mWaitingForCooldown;

	private float mSpeedTrapSpeed;

	private bool mIsPlayerDriver;

	private RaceEventResults.ResultData mSessionData = new RaceEventResults.ResultData();

	private Driver[] mDriversForCar;

	private bool mRefreshTyrePerformance;

	private string mDriverNameCache = string.Empty;
}
