using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class TrackLayoutSpeedController : SpeedController
{
	public TrackLayoutSpeedController()
	{
	}

	public override float CalculateDesiredSpeed()
	{
		float num = base.vehicle.speed;
		this.mStateTimer += GameTimer.simulationDeltaTime;
		float num2 = 0f;
		if (num > 0f)
		{
			float num3 = this.GetDistanceToNextBrakingGatePoint() / num;
			num2 = (num - this.GetCorningSpeed()) / num3;
			if (this.mState != TrackLayoutSpeedController.State.Braking && num2 > base.vehicle.GetBraking())
			{
				this.SetState(TrackLayoutSpeedController.State.Braking);
			}
		}
		switch (this.mState)
		{
		case TrackLayoutSpeedController.State.Accelerating:
		{
			float acceleration = base.vehicle.GetAcceleration();
			num += acceleration * GameTimer.simulationDeltaTime;
			num = Math.Min(num, base.vehicle.GetTopSpeed());
			break;
		}
		case TrackLayoutSpeedController.State.Braking:
			num2 = Math.Min(num2, base.vehicle.GetBraking());
			num -= num2 * GameTimer.simulationDeltaTime;
			if (num <= this.GetCorningSpeed())
			{
				num = this.GetCorningSpeed();
				this.SetState(TrackLayoutSpeedController.State.Cornering);
			}
			break;
		case TrackLayoutSpeedController.State.Cornering:
			num = this.GetCorningSpeed();
			break;
		}
		return num;
	}

	public float GetCorningSpeed()
	{
		float num = 0f;
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race)
		{
			PathController.Path currentPath = base.vehicle.pathController.GetCurrentPath();
			if (currentPath.pathType == PathController.PathType.Track && !(base.vehicle.behaviourManager.currentBehaviour is AIRaceStartBehaviour))
			{
				float num2 = Math.Abs(currentPath.pathSpace);
				float t = MathsUtility.Clamp01(num2 / 2f);
				float num3 = 0f;
				if (!Game.instance.sessionManager.isRollingOut)
				{
					switch (base.vehicle.championship.series)
					{
					case Championship.Series.SingleSeaterSeries:
						num3 = 9f;
						break;
					case Championship.Series.GTSeries:
						num3 = 3f;
						break;
					case Championship.Series.EnduranceSeries:
						num3 = 10f;
						break;
					}
				}
				DrivingStyle.Mode drivingStyleMode = base.vehicle.performance.drivingStyleMode;
				if (drivingStyleMode != DrivingStyle.Mode.Attack)
				{
					if (drivingStyleMode == DrivingStyle.Mode.Push)
					{
						num3 *= 0.95f;
					}
				}
				else
				{
					num3 *= 0.85f;
				}
				if (base.vehicle.steeringManager.slowSpeedSteeringBehaviour.isActive || base.vehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.BlueFlag)
				{
					num3 *= 1.3f;
				}
				num = EasingUtility.EaseByType(EasingUtility.Easing.OutExp, 0f, GameUtility.MilesPerHourToMetersPerSecond(num3), t);
			}
		}
		float num4 = this.mTargetCorneringSpeed - num;
		return Math.Max(num4, this.mTargetCorneringSpeed * 0.7f);
	}

	public void SetState(TrackLayoutSpeedController.State inState)
	{
		this.mState = inState;
		this.mStateTimer = 0f;
		if (this.mState == TrackLayoutSpeedController.State.Accelerating)
		{
			this.CalculateNextCornerTargetSpeed();
		}
	}

	private float GetDistanceToNextBrakingGatePoint()
	{
		float num = float.MaxValue;
		PathController pathController = base.vehicle.pathController;
		PathData.Gate nextGate = pathController.GetNextGate();
		if (nextGate != null && pathController.nextBrakingGate != null)
		{
			PathData.Gate previousGate = pathController.GetPreviousGate();
			Vector3 position = previousGate.position;
			Vector3 vector = base.vehicle.transform.position - position;
			Vector3 vector2 = Vector3.Project(vector, previousGate.unblendedNormal);
			num = previousGate.distance - vector2.magnitude;
			num += nextGate.distanceToBrakingGate;
		}
		return num;
	}

	public void CalculateNextCornerTargetSpeed()
	{
		this.mTargetCorneringSpeed = float.MaxValue;
		if (base.vehicle.pathController.nextBrakingGate != null)
		{
			PathData.Corner corner = base.vehicle.pathController.nextBrakingGate.corner;
			if (corner != null)
			{
				this.mTargetCorneringSpeed = SessionPerformance.GetSpeedForCorner(base.vehicle.performance.currentPerformance, corner);
			}
		}
	}

	public override void OnEnterPath()
	{
		base.OnEnterPath();
		this.CalculateNextCornerTargetSpeed();
		this.SetState(TrackLayoutSpeedController.State.Accelerating);
	}

	public override void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		base.OnEnterGate(inGateID, inGateType);
		if (inGateType == PathData.GateType.AccelerationZone)
		{
			this.SetState(TrackLayoutSpeedController.State.Accelerating);
		}
	}

	public TrackLayoutSpeedController.State state
	{
		get
		{
			return this.mState;
		}
	}

	private TrackLayoutSpeedController.State mState;

	private float mStateTimer;

	private float mTargetCorneringSpeed;

	public enum State
	{
		Accelerating,
		Braking,
		Cornering
	}
}
