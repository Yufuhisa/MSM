using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class RaceDirector : InstanceCounter
{
	public RaceDirector()
	{
	}

	public void Reset()
	{
		this.mHighestDriverStats = null;
		this.mLowestDriverStats = null;
		this.mHighestCarStats = new CarStats();
		this.mLowestCarStats = new CarStats();
		this.mAverageCarStats = new CarStats();
		this.mLowestCarStats.topSpeed = float.MaxValue;
		this.mLowestCarStats.acceleration = float.MaxValue;
		this.mLowestCarStats.braking = float.MaxValue;
		this.mLowestCarStats.lowSpeedCorners = float.MaxValue;
		this.mLowestCarStats.mediumSpeedCorners = float.MaxValue;
		this.mLowestCarStats.highSpeedCorners = float.MaxValue;
	}

	public void OnSessionStarting()
	{
		this.mSessionManager = Game.instance.sessionManager;
		this.mCrashDirector.OnSessionStarting();
		this.mRetireDirector.OnSessionStarting();
		this.mPenaltyDirector.OnSessionStarting();
		this.mSprinklersDirector.OnSessionStarting();
		this.mRetirementDirector.OnSessionStarting();
		this.mRunningWideDirector.OnSessionStarting();
		this.mCutCornerDirector.OnSessionStarting();
		this.mSessionSimSpeedDirector.OnSessionStarting();
		this.mDriverFormDirector.OnSessionStarting();
		this.mPitstopDirector.OnSessionStarting();
	}

	public void OnSessionEnd()
	{
		this.mCrashDirector.OnSessionEnd();
		this.mRetirementDirector.OnSessionEnd();
	}

	public void OnLoad()
	{
		if (this.mSprinklersDirector == null)
		{
			this.mSprinklersDirector = new SprinklersDirector();
			this.mSprinklersDirector.OnSessionStarting();
		}
		if (this.mRetirementDirector == null)
		{
			this.mRetirementDirector = new RetirementDirector();
			this.mRetirementDirector.OnSessionStarting();
		}
		if (this.mRunningWideDirector == null)
		{
			this.mRunningWideDirector = new RunningWideDirector();
		}
		if (this.mCutCornerDirector == null)
		{
			this.mCutCornerDirector = new CuttingCornersDirector();
		}
		if (this.mSessionSimSpeedDirector == null)
		{
			this.mSessionSimSpeedDirector = new SessionSimSpeedDirector();
		}
		this.mSessionSimSpeedDirector.OnLoad();
		if (this.mDriverFormDirector == null)
		{
			this.mDriverFormDirector = new DriverFormDirector();
			this.mDriverFormDirector.OnSessionStarting();
		}
		if (this.mPitstopDirector == null)
		{
			this.mPitstopDirector = new PitstopDirector();
			this.mPitstopDirector.OnSessionStarting();
		}
		this.mCrashDirector.OnLoad();
		this.mRunningWideDirector.OnLoad();
		this.mCutCornerDirector.OnLoad();
	}

	public void OnVehicleCollision(Vehicle inVehicle, Vehicle inObstacle)
	{
		this.ProcessCollisionDamage(inVehicle, inObstacle);
	}

	private void ProcessCollisionDamage(Vehicle inVehicle, Vehicle inObstacle)
	{
		bool flag = this.VehicleInCorrectStateForCollisionDamage(inVehicle) || this.VehicleInCorrectStateForCollisionDamage(inObstacle);
		bool flag2 = Game.instance.sessionManager.eventDetails.currentSession.sessionType == SessionDetails.SessionType.Race;
		if (!flag2 || !flag || (inVehicle.behaviourManager.isOutOfRace && inObstacle.behaviourManager.isOutOfRace))
		{
			return;
		}
		RacingVehicle vehicle = inVehicle as RacingVehicle;
		RacingVehicle obstacle = inObstacle as RacingVehicle;
		if (vehicle == null || obstacle == null)
		{
			return;
		}

		// calculate damage on main vehicle (30% wings, 10% suspension, 60% tyres)
		float damageType = RandomUtility.GetRandom01();
		if (damageType < 0.3f) {
			if (inObstacle == inVehicle.pathController.vehicleAheadOnPath)
				vehicle.car.OnCollision(CarPart.PartType.FrontWing);
			else
				vehicle.car.OnCollision(CarPart.PartType.RearWing);
		}
		else if (damageType < 0.4f) {
			vehicle.car.OnCollision(CarPart.PartType.Suspension);
		}
		else {
			TyreSet vehicleTyreSet = vehicle.setup.tyreSet;
			float tyreDamage = 0.2f - (0.2f * RandomUtility.GetRandom01()); // 20%-40% condition loss
			vehicle.setup.tyreSet.SetCondition(vehicle.setup.tyreSet.GetCondition() - tyreDamage);
		}

		// calculate damage on obstacle vehicle (same as main vehicle)
		damageType = RandomUtility.GetRandom01();
		if (damageType < 0.3f) {
			if (inObstacle == inVehicle.pathController.vehicleAheadOnPath)
				obstacle.car.OnCollision(CarPart.PartType.RearWing);
			else
				obstacle.car.OnCollision(CarPart.PartType.FrontWing);
		}
		else if (damageType < 0.4f) {
			obstacle.car.OnCollision(CarPart.PartType.Suspension);
		}
		else {
			TyreSet vehicleTyreSet = obstacle.setup.tyreSet;
			float tyreDamage = 0.2f - (0.2f * RandomUtility.GetRandom01()); // 20%-40% condition loss
			obstacle.setup.tyreSet.SetCondition(obstacle.setup.tyreSet.GetCondition() - tyreDamage);
		}

		// check for time penalty
		if (!vehicle.behaviourManager.isOutOfRace || RandomUtility.GetRandom01() < 0.02f)
		{
			this.mPenaltyDirector.ApplyPenaltyIfViable(vehicle, Penalty.PenaltyCause.Collision);
		}

		// create race event and commentary
		obstacle.timer.currentLap.AddEvent(LapDetailsData.LapEvents.Collision);
		vehicle.timer.currentLap.AddEvent(LapDetailsData.LapEvents.Collision);
		CommentaryManager.SendComment(vehicle, Comment.CommentType.Collision, new object[]
		{
			vehicle.driver,
			obstacle.driver
		});

		vehicle.sessionData.crashVictim = obstacle.driver;
		vehicle.sessionData.driverCausedCrash = true;
		if (vehicle.standingsPosition == 1)
		{
			vehicle.sessionData.crashedWhenInFirstPlace = true;
		}
		obstacle.sessionData.crashDriver = vehicle.driver;
		obstacle.sessionData.driverCrashedInto = true;
		if (vehicle.isPlayerDriver)
		{
			Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.PlayerDriverCollision, vehicle);
			vehicle.teamRadio.GetRadioMessage<RadioMessageCollision>().SendMessageCollisionCause(obstacle);
		}
		if (obstacle.isPlayerDriver)
		{
			Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.PlayerDriverCollision, vehicle);
			obstacle.teamRadio.GetRadioMessage<RadioMessageCollision>().SendMessageCollisionVictim(vehicle);
		}
	}

	private bool VehicleInCorrectStateForCollisionDamage(Vehicle inVehicle)
	{
		switch (inVehicle.pathController.GetCurrentPath().pathType)
		{
			case PathController.PathType.Pitlane:
			case PathController.PathType.PitlaneEntry:
			case PathController.PathType.PitlaneExit:
			case PathController.PathType.PitboxEntry:
			case PathController.PathType.PitboxExit:
			case PathController.PathType.GarageEntry:
			case PathController.PathType.GarageExit:
			case PathController.PathType.CrashLane:
			case PathController.PathType.RunWideLane:
			case PathController.PathType.CutCornerLane:
				return false;
			default:
				switch (inVehicle.behaviourManager.currentBehaviour.behaviourType)
				{
					case AIBehaviourStateManager.Behaviour.Crashing:
					case AIBehaviourStateManager.Behaviour.TyreLockUp:
					case AIBehaviourStateManager.Behaviour.Spin:
						return false;
				}
				return true;
		}
	}

	public void SetYellowFlag(int inSector)
	{
		this.SetYellowFlag(inSector, (float)RandomUtility.GetRandom(10, 20));
	}

	public void SetYellowFlag(int inSector, float inDuration)
	{
		if (Game.instance.sessionManager.flag == SessionManager.Flag.Green && !Game.instance.sessionManager.IsSessionEnding())
		{
			Game.instance.sessionManager.yellowFlagSector = inSector;
			Game.instance.sessionManager.SetDesiredFlag(SessionManager.Flag.Yellow);
			this.mYellowFlagDuration = inDuration;
		}
	}

	public void SimulationUpdate()
	{
		this.mCrashDirector.SimulationUpdate();
		this.mSpinOutDirector.SimulationUpdate();
		this.mTyreLockUpDirector.SimulationUpdate();
		this.mSprinklersDirector.SimulationUpdate();
		this.mRunningWideDirector.SimulationUpdate();
		this.mCutCornerDirector.SimulationUpdate();
		this.mDriverFormDirector.SimulationUpdate();
		if (this.mSessionManager.flag == SessionManager.Flag.Yellow)
		{
			this.mYellowFlagDuration -= GameTimer.simulationDeltaTime;
			if (this.mYellowFlagDuration <= 0f)
			{
				this.ResumeGreenFlag();
			}
		}
	}

	public void ResumeGreenFlag()
	{
		this.mSessionManager.SetDesiredFlag(SessionManager.Flag.Green);
	}

	public void CalculateDriverStats(List<Championship> inChampionships)
	{
		this.mHighestDriverStats = new DriverStats();
		this.mLowestDriverStats = new DriverStats();
		for (int i = 0; i < inChampionships.Count; i++)
		{
			this.CalculateDriverStats(inChampionships[i], false);
		}
	}

	public void CalculateDriverStats(Championship inChampionship, bool inResetDriverStats = true)
	{
		if (inResetDriverStats)
		{
			this.mHighestDriverStats = new DriverStats();
			this.mLowestDriverStats = new DriverStats();
		}
		int driverEntryCount = inChampionship.standings.driverEntryCount;
		for (int i = 0; i < driverEntryCount; i++)
		{
			Driver entity = inChampionship.standings.GetDriverEntry(i).GetEntity<Driver>();
			DriverStats driverStats = entity.GetDriverStats();
			this.SetHighestDriverStats(driverStats);
			this.SetLowestDriverStats(driverStats);
		}
	}

	private void SetHighestDriverStats(DriverStats inStats)
	{
		if (inStats.braking > this.mHighestDriverStats.braking)
		{
			this.mHighestDriverStats.braking = inStats.braking;
		}
		if (inStats.cornering > this.mHighestDriverStats.cornering)
		{
			this.mHighestDriverStats.cornering = inStats.cornering;
		}
		if (inStats.smoothness > this.mHighestDriverStats.smoothness)
		{
			this.mHighestDriverStats.smoothness = inStats.smoothness;
		}
		if (inStats.overtaking > this.mHighestDriverStats.overtaking)
		{
			this.mHighestDriverStats.overtaking = inStats.overtaking;
		}
		if (inStats.consistency > this.mHighestDriverStats.consistency)
		{
			this.mHighestDriverStats.consistency = inStats.consistency;
		}
		if (inStats.adaptability > this.mHighestDriverStats.adaptability)
		{
			this.mHighestDriverStats.adaptability = inStats.adaptability;
		}
		if (inStats.fitness > this.mHighestDriverStats.fitness)
		{
			this.mHighestDriverStats.fitness = inStats.fitness;
		}
		if (inStats.feedback > this.mHighestDriverStats.feedback)
		{
			this.mHighestDriverStats.feedback = inStats.feedback;
		}
		if (inStats.focus > this.mHighestDriverStats.focus)
		{
			this.mHighestDriverStats.focus = inStats.focus;
		}
	}

	private void SetLowestDriverStats(DriverStats inStats)
	{
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.braking) || inStats.braking < this.mLowestDriverStats.braking)
		{
			this.mLowestDriverStats.braking = inStats.braking;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.cornering) || inStats.cornering < this.mLowestDriverStats.cornering)
		{
			this.mLowestDriverStats.cornering = inStats.cornering;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.smoothness) || inStats.smoothness < this.mLowestDriverStats.smoothness)
		{
			this.mLowestDriverStats.smoothness = inStats.smoothness;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.overtaking) || inStats.overtaking < this.mLowestDriverStats.overtaking)
		{
			this.mLowestDriverStats.overtaking = inStats.overtaking;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.consistency) || inStats.consistency < this.mLowestDriverStats.consistency)
		{
			this.mLowestDriverStats.consistency = inStats.consistency;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.adaptability) || inStats.adaptability < this.mLowestDriverStats.adaptability)
		{
			this.mLowestDriverStats.adaptability = inStats.adaptability;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.fitness) || inStats.fitness < this.mLowestDriverStats.fitness)
		{
			this.mLowestDriverStats.fitness = inStats.fitness;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.feedback) || inStats.feedback < this.mLowestDriverStats.feedback)
		{
			this.mLowestDriverStats.feedback = inStats.feedback;
		}
		if (MathsUtility.ApproximatelyZero(this.mLowestDriverStats.focus) || inStats.focus < this.mLowestDriverStats.focus)
		{
			this.mLowestDriverStats.focus = inStats.focus;
		}
	}

	public void RegisterCarPerformanceStats(CarStats inCarStats)
	{
		if (inCarStats.topSpeed > this.mHighestCarStats.topSpeed)
		{
			this.mHighestCarStats.topSpeed = inCarStats.topSpeed;
		}
		else if (inCarStats.topSpeed < this.mLowestCarStats.topSpeed)
		{
			this.mLowestCarStats.topSpeed = inCarStats.topSpeed;
		}
		this.mAverageCarStats.topSpeed = Mathf.Lerp(this.mLowestCarStats.topSpeed, this.mHighestCarStats.topSpeed, 0.5f);
		if (inCarStats.acceleration > this.mHighestCarStats.acceleration)
		{
			this.mHighestCarStats.acceleration = inCarStats.acceleration;
		}
		else if (inCarStats.acceleration < this.mLowestCarStats.acceleration)
		{
			this.mLowestCarStats.acceleration = inCarStats.acceleration;
		}
		this.mAverageCarStats.acceleration = Mathf.Lerp(this.mLowestCarStats.acceleration, this.mHighestCarStats.acceleration, 0.5f);
		if (inCarStats.braking > this.mHighestCarStats.braking)
		{
			this.mHighestCarStats.braking = inCarStats.braking;
		}
		else if (inCarStats.braking < this.mLowestCarStats.braking)
		{
			this.mLowestCarStats.braking = inCarStats.braking;
		}
		this.mAverageCarStats.braking = Mathf.Lerp(this.mLowestCarStats.braking, this.mHighestCarStats.braking, 0.5f);
		if (inCarStats.highSpeedCorners > this.mHighestCarStats.highSpeedCorners)
		{
			this.mHighestCarStats.highSpeedCorners = inCarStats.highSpeedCorners;
		}
		else if (inCarStats.highSpeedCorners < this.mLowestCarStats.highSpeedCorners)
		{
			this.mLowestCarStats.highSpeedCorners = inCarStats.highSpeedCorners;
		}
		this.mAverageCarStats.highSpeedCorners = Mathf.Lerp(this.mLowestCarStats.highSpeedCorners, this.mHighestCarStats.highSpeedCorners, 0.5f);
		if (inCarStats.mediumSpeedCorners > this.mHighestCarStats.mediumSpeedCorners)
		{
			this.mHighestCarStats.mediumSpeedCorners = inCarStats.mediumSpeedCorners;
		}
		else if (inCarStats.mediumSpeedCorners < this.mLowestCarStats.mediumSpeedCorners)
		{
			this.mLowestCarStats.mediumSpeedCorners = inCarStats.mediumSpeedCorners;
		}
		this.mAverageCarStats.mediumSpeedCorners = Mathf.Lerp(this.mLowestCarStats.mediumSpeedCorners, this.mHighestCarStats.mediumSpeedCorners, 0.5f);
		if (inCarStats.lowSpeedCorners > this.mHighestCarStats.lowSpeedCorners)
		{
			this.mHighestCarStats.lowSpeedCorners = inCarStats.lowSpeedCorners;
		}
		else if (inCarStats.lowSpeedCorners < this.mLowestCarStats.lowSpeedCorners)
		{
			this.mLowestCarStats.lowSpeedCorners = inCarStats.lowSpeedCorners;
		}
		this.mAverageCarStats.lowSpeedCorners = Mathf.Lerp(this.mLowestCarStats.lowSpeedCorners, this.mHighestCarStats.lowSpeedCorners, 0.5f);
	}

	public DriverStats highestDriverStats
	{
		get
		{
			return this.mHighestDriverStats;
		}
	}

	public DriverStats lowestDriverStats
	{
		get
		{
			return this.mLowestDriverStats;
		}
	}

	public CarStats highestCarStats
	{
		get
		{
			return this.mHighestCarStats;
		}
	}

	public CarStats lowestCarStats
	{
		get
		{
			return this.mLowestCarStats;
		}
	}

	public CarStats averageCarStats
	{
		get
		{
			return this.mAverageCarStats;
		}
	}

	public bool overtakingEnabled
	{
		get
		{
			return this.mOvertakingEnabled;
		}
	}

	public CrashDirector crashDirector
	{
		get
		{
			return this.mCrashDirector;
		}
	}

	public RetireDirector retireDirector
	{
		get
		{
			return this.mRetireDirector;
		}
	}

	public PenaltyDirector penaltyDirector
	{
		get
		{
			return this.mPenaltyDirector;
		}
	}

	public SpinOutDirector spinOutDirector
	{
		get
		{
			return this.mSpinOutDirector;
		}
	}

	public TyreLockUpDirector tyreLockUpDirector
	{
		get
		{
			return this.mTyreLockUpDirector;
		}
	}

	public SprinklersDirector sprinklersDirector
	{
		get
		{
			return this.mSprinklersDirector;
		}
	}

	public RetirementDirector retirementDirector
	{
		get
		{
			return this.mRetirementDirector;
		}
	}

	public TyreConfiscationDirector tyreConfiscationDirector
	{
		get
		{
			return this.mTyreConfiscationDirector;
		}
	}

	public RunningWideDirector runningWideDirector
	{
		get
		{
			return this.mRunningWideDirector;
		}
	}

	public CuttingCornersDirector cutCornersDirector
	{
		get
		{
			return this.mCutCornerDirector;
		}
	}

	public SessionSimSpeedDirector sessionSimSpeedDirector
	{
		get
		{
			return this.mSessionSimSpeedDirector;
		}
	}

	public PitstopDirector pitstopDirector
	{
		get
		{
			return this.mPitstopDirector;
		}
	}

	private DriverStats mHighestDriverStats;

	private DriverStats mLowestDriverStats;

	private CarStats mHighestCarStats = new CarStats();

	private CarStats mLowestCarStats = new CarStats();

	private CarStats mAverageCarStats = new CarStats();

	private float mYellowFlagDuration;

	private SessionManager mSessionManager;

	private bool mOvertakingEnabled = true;

	private SprinklersDirector mSprinklersDirector = new SprinklersDirector();

	private PenaltyDirector mPenaltyDirector = new PenaltyDirector();

	private CrashDirector mCrashDirector = new CrashDirector();

	private RetireDirector mRetireDirector = new RetireDirector();

	private SpinOutDirector mSpinOutDirector = new SpinOutDirector();

	private TyreLockUpDirector mTyreLockUpDirector = new TyreLockUpDirector();

	private RetirementDirector mRetirementDirector = new RetirementDirector();

	private TyreConfiscationDirector mTyreConfiscationDirector = new TyreConfiscationDirector();

	private RunningWideDirector mRunningWideDirector = new RunningWideDirector();

	private CuttingCornersDirector mCutCornerDirector = new CuttingCornersDirector();

	private SessionSimSpeedDirector mSessionSimSpeedDirector = new SessionSimSpeedDirector();

	private DriverFormDirector mDriverFormDirector = new DriverFormDirector();

	private PitstopDirector mPitstopDirector = new PitstopDirector();
}
