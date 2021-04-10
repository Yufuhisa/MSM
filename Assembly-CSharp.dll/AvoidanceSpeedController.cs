using System;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class AvoidanceSpeedController : SpeedController
{
	public AvoidanceSpeedController()
	{
	}

	public override void Start(Vehicle inVehicle)
	{
		base.Start(inVehicle);
	}

	public override float CalculateDesiredSpeed()
	{
		float num = base.vehicle.GetTopSpeed();
		if (Game.instance.sessionManager.isRollingOut && base.vehicle is RacingVehicle)
		{
			float num2 = 1f;
			Vehicle vehicle;
			if (base.racingVehicle.standingsPosition == 1)
			{
				vehicle = Game.instance.vehicleManager.safetyVehicle;
				num2 = 3f;
				if (vehicle.pathController.currentPathType != PathController.PathType.Track || vehicle.speed < 10f)
				{
					return GameUtility.MilesPerHourToMetersPerSecond(145f);
				}
			}
			else
			{
				vehicle = Game.instance.sessionManager.standings[base.racingVehicle.standingsPosition - 2];
			}
			float distanceAlongPath = base.racingVehicle.pathController.GetDistanceAlongPath(PathController.PathType.Track);
			float distanceAlongPath2 = vehicle.pathController.GetDistanceAlongPath(PathController.PathType.Track);
			float num3 = distanceAlongPath - distanceAlongPath2;
			if (num3 > -2f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 0.8f;
			}
			if (num3 > -3.5f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 0.85f;
			}
			if (num3 > -5f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 0.99f;
			}
			if (num3 > -10f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 1.05f;
			}
			if (num3 > -20f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 1.1f;
			}
			if (num3 > -40f * num2)
			{
				return vehicle.speedManager.desiredSpeed * 1.15f;
			}
			return num;
		}
		else
		{
			if (Game.instance.sessionManager.isSkippingSession)
			{
				return num;
			}
			if (!base.vehicle.pathState.IsInState(PathStateManager.StateType.Garage) && !base.vehicle.pathState.IsInState(PathStateManager.StateType.GarageExit) && !base.vehicle.pathState.IsInState(PathStateManager.StateType.PitboxEntry) && !base.vehicle.pathState.IsInState(PathStateManager.StateType.PitboxExit))
			{
				int count = base.vehicle.pathController.nearbyObstacles.Count;
				float comfortDistanceToCarAhead = base.vehicle.behaviourManager.currentBehaviour.GetComfortDistanceToCarAhead();
				float num4 = comfortDistanceToCarAhead * 1.25f;
				float vehicleLength = VehicleConstants.vehicleLength;
				if (Game.instance.sessionManager.sessionType != SessionDetails.SessionType.Race && base.vehicle.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.InOutLap)
				{
					return num;
				}
				for (int i = 0; i < count; i++)
				{
					Vehicle vehicle2 = base.vehicle.pathController.nearbyObstacles[i];
					if (vehicle2.pathController.currentPathType == base.vehicle.pathController.currentPathType)
					{
						if (vehicle2.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.BlueFlag)
						{
							if (base.vehicle.pathController.IsBehindVehicle(vehicle2) && !base.vehicle.pathController.IsBesideVehicle(vehicle2))
							{
								float pathSpace = vehicle2.pathController.GetCurrentPath().pathSpace;
								float num5 = Math.Abs(base.vehicle.pathController.GetCurrentPath().pathSpace - pathSpace);
								float num6 = base.vehicle.GetPathSpaceWidth() * 1.5f;
								if (!base.vehicle.CanPassVehicle(vehicle2) || num5 < num6)
								{
									float num7 = Math.Abs(base.vehicle.pathController.GetPathDistanceToVehicle(vehicle2));
									num7 -= VehicleConstants.vehicleLength * 0.5f;
									if (num7 < num4)
									{
										if (vehicle2.speed < base.vehicle.speed)
										{
											float num8 = base.vehicle.speed - vehicle2.speed;
											float num9 = (num7 - comfortDistanceToCarAhead) / num8;
											float num10 = num8 / base.vehicle.GetBraking();
											if (num9 < num10)
											{
												float braking = num8 / num9;
												base.vehicle.SetBraking(braking);
												num = Math.Min(num, vehicle2.speed);
											}
										}
										float num11 = 1f - MathsUtility.Clamp01((num4 - num7) / (num4 - comfortDistanceToCarAhead));
										base.vehicle.SetMaxSpeed(vehicle2.speed + (base.vehicle.speed - vehicle2.speed) * num11);
										if (num7 < comfortDistanceToCarAhead)
										{
											float num12 = vehicle2.speed - Math.Abs(num7 - comfortDistanceToCarAhead);
											num = Math.Min(num, num12);
											base.vehicle.SetMaxSpeed(vehicle2.speed);
										}
									}
									if (num7 < vehicleLength && base.vehicle.pathController.IsInCorner() && !base.vehicle.behaviourManager.CanAttackVehicle())
									{
										num = Math.Min(num, base.vehicle.speed - GameUtility.MilesPerHourToMetersPerSecond(20f));
										base.vehicle.SetMaxSpeed(base.vehicle.speed);
									}
								}
							}
						}
					}
				}
			}
			return num;
		}
	}
}
