using System;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class AIRacingBehaviour : AIBehaviour
{
	public AIRacingBehaviour()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static AIRacingBehaviour()
	{
	}

	public override void Start(Vehicle inVehicle)
	{
		base.Start(inVehicle);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race)
		{
			this.mConfortDistanceToVehicleAhead = VehicleConstants.vehicleLength * RandomUtility.GetRandom(0.5f, 0.85f);
		}
		else
		{
			this.mConfortDistanceToVehicleAhead = VehicleConstants.vehicleLength * 2f;
		}
		if (this.mRacingVehicle != null && (Game.instance.sessionManager.flag == SessionManager.Flag.SafetyCar || Game.instance.sessionManager.flag == SessionManager.Flag.VirtualSafetyCar))
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.SafetyFlag);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void SimulationUpdate()
	{
		if (this.mRacingVehicle.timer.hasSeenChequeredFlag || (this.mRacingVehicle.timer.currentLap.isInLap && !this.mVehicle.pathState.IsInPitlaneArea()))
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.InOutLap);
		}
		if (Game.instance.sessionManager.sessionType != SessionDetails.SessionType.Qualifying && this.mRacingVehicle.performance.IsExperiencingCriticalIssue())
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.CriticalIssue);
		}
		if (AIBlueFlagBehaviour.IsBlueFlagRequired(this.mRacingVehicle))
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.BlueFlag);
		}
		if (AITeamOrderBehaviour.ShouldAllowTeamMateThrough(this.mRacingVehicle))
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.TeamOrder);
		}
	}

	public override bool HandleMessage(Vehicle inSender, AIMessage.Type inType, object inData)
	{
		if (inType == AIMessage.Type.DefendOvertake)
		{
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Defending);
			AIDefendingBehaviour behaviour = this.mRacingVehicle.behaviourManager.GetBehaviour<AIDefendingBehaviour>();
			behaviour.SetTarget(inData as RacingVehicle);
		}
		return false;
	}

	public override void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		base.OnEnterGate(inGateID, inGateType);
		this.CheckForTyreLockUpOpportunity(inGateID, inGateType);
		AIRacingBehaviour.CheckForBehaviour(PathController.PathType.RunWideLane, RunningWideDirector.Behaviour.RunningWide, inGateID, this.mRacingVehicle);
		AIRacingBehaviour.CheckForBehaviour(PathController.PathType.CutCornerLane, RunningWideDirector.Behaviour.CuttingCorner, inGateID, this.mRacingVehicle);
		if (this.mIsSetToCrash)
		{
			this.CheckForCrashOpportunity();
		}
	}

	public static void CheckForBehaviour(PathController.PathType inPath, RunningWideDirector.Behaviour inBehaviour, int inGateID, RacingVehicle inVehicle)
	{
		if (inVehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.Racing && inVehicle.pathController.currentPathType == PathController.PathType.Track)
		{
			TargetPointSteeringBehaviour behaviour = inVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>();
			if (behaviour.state == TargetPointSteeringBehaviour.State.None && Game.instance.sessionManager.circuit.HasPathOfType(inPath))
			{
				bool flag = false;
				PathController.Path path = inVehicle.pathController.GetPath(inPath);
				path.GetClosestPath();
				int num = Game.instance.sessionManager.circuit.GetEntryTrackIDForPathType(inPath, path.pathID) - inGateID;
				int num2 = 0;
				if (inBehaviour != RunningWideDirector.Behaviour.CuttingCorner)
				{
					if (inBehaviour == RunningWideDirector.Behaviour.RunningWide)
					{
						num2 = 20;
					}
				}
				else
				{
					num2 = 10;
				}
				if (num < num2)
				{
					if (inBehaviour != RunningWideDirector.Behaviour.CuttingCorner)
					{
						if (inBehaviour == RunningWideDirector.Behaviour.RunningWide)
						{
							flag = Game.instance.sessionManager.raceDirector.runningWideDirector.CanRunWide(inVehicle, path);
						}
					}
					else
					{
						flag = Game.instance.sessionManager.raceDirector.cutCornersDirector.CanCutCorner(inVehicle, path);
					}
				}
				if (flag)
				{
					path.GetClosestPath();
					behaviour.SetTargetPath(inPath, false);
					if (inBehaviour != RunningWideDirector.Behaviour.CuttingCorner)
					{
						if (inBehaviour == RunningWideDirector.Behaviour.RunningWide)
						{
							Game.instance.sessionManager.raceDirector.runningWideDirector.VehicleSetBehaviour(inVehicle, path);
						}
					}
					else
					{
						Game.instance.sessionManager.raceDirector.cutCornersDirector.VehicleSetBehaviour(inVehicle, path);
					}
				}
			}
		}
	}

	public override void OnEnterCorner(PathData.Corner inCorner)
	{
		base.OnEnterCorner(inCorner);
		if (this.mVehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.Racing)
		{
			this.CheckForSpinOutOpportunity();
			this.CheckForCrashOpportunity();
			this.mCornerCount++;
			if (this.mCornerCount >= this.mCornersUntilNextOvertakeCheck)
			{
				this.CheckForOvertakeOpportunity();
				this.mCornerCount = 0;
				this.mCornersUntilNextOvertakeCheck = this.mVehicle.pathController.GetCurrentPathData().corners.Count / 3;
			}
		}
	}

	public override void OnEnterStraight(PathData.Straight inStraight)
	{
		base.OnEnterStraight(inStraight);
		if (this.mVehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.Racing)
		{
			this.CheckForOvertakeOpportunity();
		}
	}

	private void CheckForTyreLockUpOpportunity(int inGateID, PathData.GateType inGateType)
	{
		if (this.mRacingVehicle.pathController.GetCurrentPath().data.gates[inGateID].isLockUpGate && Game.instance.sessionManager.raceDirector.tyreLockUpDirector.IsTyreLockUpViable(this.mRacingVehicle))
		{
			this.mRacingVehicle.sessionEvents.EventActivated(SessionEvents.EventType.LockUp);
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.TyreLockUp);
			Game.instance.sessionManager.raceDirector.tyreLockUpDirector.OnTyreLockUpIncident(this.mRacingVehicle);
		}
	}

	public void CheckForCrashOpportunity()
	{
		if (this.mRacingVehicle.pathController.GetCurrentPath().pathType == PathController.PathType.Track)
		{
			TargetPointSteeringBehaviour behaviour = this.mRacingVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>();
			if (Game.instance.sessionManager.raceDirector.crashDirector.CalculateCrashChance(this.mRacingVehicle, false) || (this.mIsSetToCrash && behaviour.targetPath != PathController.PathType.CrashLane))
			{
				if (Game.instance.sessionManager.circuit.HasPathOfType(PathController.PathType.CrashLane))
				{
					if (behaviour.SetTargetPath(PathController.PathType.CrashLane, true))
					{
						this.mRacingVehicle.strategy.SetStatus(SessionStrategy.Status.Crashing);
					}
				}
				else
				{
					this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Crashed);
					Game.instance.sessionManager.raceDirector.crashDirector.OnCrashIncident(this.mRacingVehicle);
				}
			}
		}
	}

	private void CheckForSpinOutOpportunity()
	{
		if (Game.instance.sessionManager.raceDirector.spinOutDirector.IsSpinOutViable(this.mRacingVehicle))
		{
			this.mRacingVehicle.sessionEvents.EventActivated(SessionEvents.EventType.SpinOut);
			this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Spin);
			Game.instance.sessionManager.raceDirector.spinOutDirector.OnSpinOutIncident(this.mRacingVehicle);
		}
	}

	private void CheckForOvertakeOpportunity()
	{
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race && this.mRacingVehicle.pathController.GetCurrentPath().pathType == PathController.PathType.Track)
		{
			Vehicle vehicleAheadOnPath = this.mRacingVehicle.pathController.vehicleAheadOnPath;
			if (vehicleAheadOnPath != null && vehicleAheadOnPath is RacingVehicle && this.mVehicle.pathController.IsInOvertakeZoneOf(vehicleAheadOnPath))
			{
				bool flag = OvertakeDirector.CalculateOvertakeAttempt(this.mRacingVehicle, vehicleAheadOnPath as RacingVehicle, this.mVehicle.pathController.GetCurrentCorner(), this.mVehicle.pathController.GetCurrentStraight());
				if (flag)
				{
					AIOvertakingBehaviour behaviour = this.mRacingVehicle.behaviourManager.GetBehaviour<AIOvertakingBehaviour>();
					behaviour.SetTarget(vehicleAheadOnPath as RacingVehicle);
					this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Overtaking);
				}
				else if (vehicleAheadOnPath is RacingVehicle)
				{
					RacingVehicle racingVehicle = vehicleAheadOnPath as RacingVehicle;
					racingVehicle.HandleMessage(this.mRacingVehicle, AIMessage.Type.DefendOvertake, this.mRacingVehicle);
					if (this.mRacingVehicle.driver.contract.GetTeam().IsPlayersTeam())
					{
						this.mRacingVehicle.teamRadio.GetRadioMessage<RadioMessageOvertakes>().SendCantOvertakeMessage(racingVehicle);
					}
				}
			}
		}
	}

	public override bool UseAggressiveOvertaking()
	{
		return Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race;
	}

	public override void OnFlagChange(SessionManager.Flag inFlag)
	{
		base.OnFlagChange(inFlag);
		if ((inFlag == SessionManager.Flag.VirtualSafetyCar || inFlag == SessionManager.Flag.SafetyCar) && !this.mIsSetToCrash && this.mRacingVehicle.strategy.status == SessionStrategy.Status.Crashing)
		{
			this.mRacingVehicle.strategy.SetToNoActionRequired();
			this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().ClearTarget();
		}
	}

	public override float GetComfortDistanceToCarAhead()
	{
		return this.mConfortDistanceToVehicleAhead;
	}

	public override SpeedManager.Controller[] speedControllers
	{
		get
		{
			return AIRacingBehaviour.mSpeedControllers;
		}
	}

	public override SteeringManager.Behaviour[] steeringBehaviours
	{
		get
		{
			return AIRacingBehaviour.mSteeringBehaviours;
		}
	}

	public override AIBehaviourStateManager.Behaviour behaviourType
	{
		get
		{
			return AIBehaviourStateManager.Behaviour.Racing;
		}
	}

	public bool isSetToCrash
	{
		get
		{
			return this.mIsSetToCrash;
		}
		set
		{
			this.mIsSetToCrash = value;
		}
	}

	private const int gatesAheadToRunWide = 20;

	private const int gatesAheadToCutCorner = 10;

	private static readonly SpeedManager.Controller[] mSpeedControllers = new SpeedManager.Controller[]
	{
		SpeedManager.Controller.TrackLayout,
		SpeedManager.Controller.Avoidance,
		SpeedManager.Controller.PathType,
		SpeedManager.Controller.GroupSpeed,
		SpeedManager.Controller.SafetyCar
	};

	private static readonly SteeringManager.Behaviour[] mSteeringBehaviours = new SteeringManager.Behaviour[]
	{
		SteeringManager.Behaviour.Avoidance,
		SteeringManager.Behaviour.RacingLine,
		SteeringManager.Behaviour.TargetPoint
	};

	private bool mIsSetToCrash;

	private int mCornerCount;

	private int mCornersUntilNextOvertakeCheck;

	private float mConfortDistanceToVehicleAhead;
}
