using System;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class AIBlueFlagBehaviour : AIBehaviour
{
	public AIBlueFlagBehaviour()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static AIBlueFlagBehaviour()
	{
	}

	public override void Start(Vehicle inVehicle)
	{
		base.Start(inVehicle);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		this.mRacingVehicle.behaviourManager.SetCanDefendVehicle(false);
		if (this.mRacingVehicle.timer.sessionTime - this.mSessionTimeOfLastMessage > 20f)
		{
			CommentaryManager.SendComment(this.mRacingVehicle, Comment.CommentType.BlueFlags, new object[]
			{
				this.mRacingVehicle.driver
			});
		}
		this.mSessionTimeOfLastMessage = this.mRacingVehicle.timer.sessionTime;
		this.mBlueFlagStateTimer = 0f;
	}

	public override void OnExit()
	{
		base.OnExit();
		this.mRacingVehicle.behaviourManager.SetCanDefendVehicle(true);
	}

	public override void SimulationUpdate()
	{
		this.mBlueFlagStateTimer += GameTimer.simulationDeltaTime;
		if (!AIBlueFlagBehaviour.IsBlueFlagRequired(this.mRacingVehicle))
		{
			if (Game.instance.sessionManager.sessionType != SessionDetails.SessionType.Qualifying && this.mRacingVehicle.performance.IsExperiencingCriticalIssue())
			{
				this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.CriticalIssue);
			}
			else if (this.mBlueFlagStateTimer > 5f)
			{
				this.mRacingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Racing);
			}
		}
		else
		{
			this.mBlueFlagStateTimer = 0f;
		}
	}

	public override bool HandleMessage(Vehicle inSender, AIMessage.Type inType, object inData)
	{
		return false;
	}

	public override void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		base.OnEnterGate(inGateID, inGateType);
	}

	public static bool IsBlueFlagRequired(RacingVehicle inVehicle)
	{
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race)
		{
			bool settingBool = App.instance.preferencesManager.GetSettingBool(Preference.pName.Game_BlueFlags, false);
			if (!settingBool || Game.instance.sessionManager.championship.series == Championship.Series.EnduranceSeries)
			{
				return false;
			}
			if (inVehicle.pathController.GetCurrentPath().pathType == PathController.PathType.Track)
			{
				int count = inVehicle.pathController.nearbyObstacles.Count;
				for (int i = 0; i < count; i++)
				{
					Vehicle vehicle = inVehicle.pathController.nearbyObstacles[i];
					if (vehicle is RacingVehicle && vehicle.pathController.currentPathType == PathController.PathType.Track)
					{
						RacingVehicle racingVehicle = vehicle as RacingVehicle;
						if (inVehicle.timer.lap < racingVehicle.timer.lap && inVehicle.performance.currentPerformance.statsTotal < racingVehicle.performance.currentPerformance.statsTotal && AIBlueFlagBehaviour.IsInBlueFlagZoneOf(racingVehicle, inVehicle))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static bool IsInBlueFlagZoneOf(Vehicle inVehicleBehind, Vehicle inVehicleAhead)
	{
		if (inVehicleBehind.pathController.IsOnComparablePath(inVehicleAhead))
		{
			float pathDistanceToVehicle = inVehicleBehind.pathController.GetPathDistanceToVehicle(inVehicleAhead);
			SessionManager sessionManager = Game.instance.sessionManager;
			if (pathDistanceToVehicle > 1E-45f && !sessionManager.hasSessionEnded)
			{
				if (sessionManager.sessionType == SessionDetails.SessionType.Race)
				{
					GateInfo gateTimer = sessionManager.GetGateTimer(inVehicleBehind.pathController.GetPreviousGate().id);
					float timeGapBetweenVehicles = gateTimer.GetTimeGapBetweenVehicles(inVehicleAhead, inVehicleBehind);
					if (timeGapBetweenVehicles < 2f || inVehicleAhead.performance.IsExperiencingCriticalIssue())
					{
						return true;
					}
				}
				else
				{
					if (inVehicleAhead.pathController.IsOnStraight() && inVehicleAhead.speed < inVehicleBehind.speed)
					{
						float num = inVehicleBehind.speed - inVehicleAhead.speed;
						float num2 = (pathDistanceToVehicle - VehicleConstants.vehicleLength) / num;
						if (num2 < 4f)
						{
							return true;
						}
					}
					bool flag = pathDistanceToVehicle - VehicleConstants.vehicleLength * 6f < 0f;
					if (flag)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override void OnFlagChange(SessionManager.Flag inFlag)
	{
		base.OnFlagChange(inFlag);
	}

	public override bool IsSlowSpeed()
	{
		return true;
	}

	public override AIBehaviourStateManager.Behaviour behaviourType
	{
		get
		{
			return AIBehaviourStateManager.Behaviour.BlueFlag;
		}
	}

	public override SpeedManager.Controller[] speedControllers
	{
		get
		{
			return AIBlueFlagBehaviour.mSpeedControllers;
		}
	}

	public override SteeringManager.Behaviour[] steeringBehaviours
	{
		get
		{
			return AIBlueFlagBehaviour.mSteeringBehaviours;
		}
	}

	private static readonly SpeedManager.Controller[] mSpeedControllers = new SpeedManager.Controller[]
	{
		SpeedManager.Controller.TrackLayout,
		SpeedManager.Controller.Avoidance,
		SpeedManager.Controller.PathType,
		SpeedManager.Controller.SafetyCar,
		SpeedManager.Controller.GroupSpeed,
		SpeedManager.Controller.BlueFlag
	};

	private static readonly SteeringManager.Behaviour[] mSteeringBehaviours = new SteeringManager.Behaviour[]
	{
		SteeringManager.Behaviour.Avoidance,
		SteeringManager.Behaviour.RacingLine,
		SteeringManager.Behaviour.TargetPoint,
		SteeringManager.Behaviour.SlowSpeed
	};

	private float mSessionTimeOfLastMessage;

	private float mBlueFlagStateTimer;
}
