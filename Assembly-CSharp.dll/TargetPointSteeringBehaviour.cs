using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class TargetPointSteeringBehaviour : SteeringBehaviour
{
	public override void Start(SteeringManager inSteeringManager, Vehicle inVehicle)
	{
		base.Start(inSteeringManager, inVehicle);
		base.SetActive(false);
	}

	public void OnLoad()
	{
		this.mTargetPointPath.OnLoad();
		if (this.mBlendLength == 0f && this.mState == TargetPointSteeringBehaviour.State.WaitingToBlend)
		{
			this.SetState(TargetPointSteeringBehaviour.State.None);
		}
	}

	public override void SimulationUpdate(SteeringContextMap inInterestMap, SteeringContextMap inDangerMap)
	{
		if (base.isActive && this.mState != TargetPointSteeringBehaviour.State.None)
		{
			float distanceBetweenGates = PathUtility.GetDistanceBetweenGates(base.vehicle.pathController.GetCurrentPath().data, base.vehicle.pathController.GetCurrentPath().nextGateIdAccessor, this.mTargetPointPath.previousGateIdAccessor);
			if (this.mWaitToGetCloseToPath)
			{
				if (distanceBetweenGates <= this.mBlendLength)
				{
					this.mWaitToGetCloseToPath = false;
				}
			}
			else
			{
				switch (this.mState)
				{
				case TargetPointSteeringBehaviour.State.WaitingToBlend:
					if (distanceBetweenGates < this.mBlendLength)
					{
						this.SetState(TargetPointSteeringBehaviour.State.Blending);
					}
					break;
				case TargetPointSteeringBehaviour.State.Blending:
					if (distanceBetweenGates <= this.mHalfBlendLength)
					{
						this.SetState(TargetPointSteeringBehaviour.State.Finishing);
					}
					else if (this.mUseTargetPosition)
					{
						this.SetTargetPosition(inInterestMap);
					}
					else
					{
						float t = Mathf.Clamp01(distanceBetweenGates / this.mHalfBlendLength) * 2f;
						float inPathSpacePosition = Mathf.Lerp(this.mPathSpaceOnBlendStart, this.mPathSpace, t);
						inInterestMap.WritePathWideSlots(inPathSpacePosition, SteeringConstants.targetPointSlotValue);
					}
					break;
				case TargetPointSteeringBehaviour.State.Finishing:
					if (this.mUseTargetPosition)
					{
						this.SetTargetPosition(inInterestMap);
					}
					else
					{
						inInterestMap.WritePathWideSlots(this.mPathSpace, SteeringConstants.targetPointSlotValue);
					}
					if (distanceBetweenGates <= 1f)
					{
						this.SetState(TargetPointSteeringBehaviour.State.Finished);
					}
					break;
				}
			}
		}
	}

	private void SetTargetPosition(SteeringContextMap inInterestMap)
	{
		this.mPathSpace = PathController.ConvertWorldSpaceToPathSpace(base.vehicle.pathController.GetPath(PathController.PathType.Track), this.mTargetPosition, this.mTargetSplinePosition);
		inInterestMap.WritePathWideSlots(this.mPathSpace, SteeringConstants.targetPointSlotValue);
	}

	public bool SetTargetPath(PathController.PathType inPathType, bool inUpdateClosestPath = true)
	{
		if (this.mState != TargetPointSteeringBehaviour.State.None)
		{
			return false;
		}
		PathController.Path currentPath = base.vehicle.pathController.GetCurrentPath();
		if (inUpdateClosestPath)
		{
			base.vehicle.pathController.UpdatePathSelected(inPathType, -1);
		}
		PathController.Path path = base.vehicle.pathController.GetPath(inPathType);
		Vector3 position = path.data.gates[0].position;
		float inBlendLength = 0f;
		switch (inPathType)
		{
		case PathController.PathType.PitlaneEntry:
			inBlendLength = 30f;
			break;
		case PathController.PathType.PitboxEntry:
		case PathController.PathType.GarageEntry:
			inBlendLength = 10f;
			break;
		case PathController.PathType.CrashLane:
		case PathController.PathType.RunWideLane:
		case PathController.PathType.CutCornerLane:
			inBlendLength = 20f;
			break;
		}
		this.SetTarget(inPathType, currentPath, position, inBlendLength, TargetPointSteeringBehaviour.TargetResult.PathChange);
		return true;
	}

	private void SetTarget(PathController.PathType inPathType, PathController.Path inPath, Vector3 inTargetPosition, float inBlendLength, TargetPointSteeringBehaviour.TargetResult inTargetResult)
	{
		this.mTargetPath = inPathType;
		if (this.mState == TargetPointSteeringBehaviour.State.None)
		{
			float distanceAlongPath = base.vehicle.pathController.GetDistanceAlongPath(base.vehicle.pathController.currentPathType);
			this.mTargetResult = inTargetResult;
			this.mTargetPointPath.CopyPath(inPath);
			PathController.UpdatePathToNearestGate(this.mTargetPointPath, inTargetPosition, null);
			PathController.CalculatePathPositionData(this.mTargetPointPath, inTargetPosition);
			if ((inPathType == PathController.PathType.PitboxEntry || inPathType == PathController.PathType.GarageEntry) && this.mTargetPointPath.previousGateIdAccessor < base.vehicle.pathController.GetCurrentPath().nextGateIdAccessor)
			{
				this.mTargetPointPath.previousGateIdAccessor = base.vehicle.pathController.GetCurrentPath().nextGateIdAccessor;
			}
			this.mHalfBlendLength = inBlendLength * 0.5f;
			this.mBlendLength = inBlendLength;
			if (this.mTargetPath == PathController.PathType.RunWideLane || this.mTargetPath == PathController.PathType.CutCornerLane || this.mTargetPath == PathController.PathType.CrashLane)
			{
				PathController.Path path = base.vehicle.pathController.GetPath(PathController.PathType.Track);
				int num = 0;
				int num2 = 0;
				PathController.CalculatePreviousAndNextGate(path, inTargetPosition, ref num, ref num2, null);
				PathData.Gate gate = path.data.gates[num];
				path.data.racingLineSpline.FindSplinePositionForPoint(gate.position, gate.racingLineStart, gate.racingLineEnd, out this.mTargetSplinePosition, -1);
				this.mTargetPosition = inTargetPosition;
				this.mUseTargetPosition = true;
			}
			else
			{
				this.mPathSpace = 0f;
				this.mUseTargetPosition = false;
			}
			this.mWaitToGetCloseToPath = ((this.mTargetPath == PathController.PathType.PitlaneEntry || this.mTargetPath == PathController.PathType.RunWideLane || this.mTargetPath == PathController.PathType.CutCornerLane || this.mTargetPath == PathController.PathType.CrashLane) && distanceAlongPath >= this.mBlendLength);
			if (object.Equals(inBlendLength, 0f))
			{
				this.SetState(TargetPointSteeringBehaviour.State.Finishing);
			}
			else
			{
				this.SetState(TargetPointSteeringBehaviour.State.WaitingToBlend);
			}
			base.SetActive(true);
		}
	}

	public void ClearTarget()
	{
		this.SetState(TargetPointSteeringBehaviour.State.None);
	}

	private void SetState(TargetPointSteeringBehaviour.State inState)
	{
		this.mState = inState;
		switch (this.mState)
		{
		case TargetPointSteeringBehaviour.State.None:
			base.SetActive(false);
			break;
		case TargetPointSteeringBehaviour.State.Blending:
			this.mPathSpaceOnBlendStart = base.vehicle.pathController.GetCurrentPath().pathSpace;
			break;
		case TargetPointSteeringBehaviour.State.Finished:
			if (this.mTargetResult == TargetPointSteeringBehaviour.TargetResult.PathChange)
			{
				PathController.PathType pathType = base.vehicle.pathController.GetCurrentPath().pathType;
				if (pathType == PathController.PathType.Track)
				{
					switch (this.mTargetPath)
					{
					case PathController.PathType.CrashLane:
						base.vehicle.pathController.EnterCrashLane();
						break;
					case PathController.PathType.RunWideLane:
						base.vehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.RuningWide);
						base.racingVehicle.performance.UpdateRacingPerformance();
						base.vehicle.pathController.EnterPath(this.mTargetPath);
						break;
					case PathController.PathType.CutCornerLane:
						base.vehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.CuttingCorners);
						base.racingVehicle.performance.UpdateRacingPerformance();
						base.vehicle.pathController.EnterPath(this.mTargetPath);
						break;
					default:
						base.vehicle.pathController.EnterPitlane();
						break;
					}
				}
				else if (pathType == PathController.PathType.Pitlane)
				{
					if (base.vehicle is RacingVehicle)
					{
						RacingVehicle racingVehicle = (RacingVehicle)base.vehicle;
						if (racingVehicle.strategy.status == SessionStrategy.Status.Pitting)
						{
							base.vehicle.pathState.ChangeState(PathStateManager.StateType.PitboxEntry);
						}
						else if (racingVehicle.strategy.status == SessionStrategy.Status.ReturningToGarage)
						{
							base.vehicle.pathState.ChangeState(PathStateManager.StateType.GarageEntry);
						}
					}
					if (base.vehicle is SafetyVehicle)
					{
						SafetyVehicle safetyVehicle = (SafetyVehicle)base.vehicle;
						if (safetyVehicle.behaviourManager.currentBehaviour is AISafetyCarRolloutBehaviour || safetyVehicle.behaviourManager.GetBehaviour<AISafetyCarBehaviour>().state == AISafetyCarBehaviour.SafetyCarState.Ending)
						{
							base.vehicle.pathState.ChangeState(PathStateManager.StateType.GarageEntry);
						}
					}
				}
			}
			this.SetState(TargetPointSteeringBehaviour.State.None);
			break;
		}
	}

	public TargetPointSteeringBehaviour.State state
	{
		get
		{
			return this.mState;
		}
	}

	public TargetPointSteeringBehaviour.TargetResult targetResult
	{
		get
		{
			return this.mTargetResult;
		}
	}

	public PathController.PathType targetPath
	{
		get
		{
			return this.mTargetPath;
		}
	}

	private TargetPointSteeringBehaviour.State mState;

	private TargetPointSteeringBehaviour.TargetResult mTargetResult;

	private PathController.PathType mTargetPath = PathController.PathType.Count;

	private PathController.Path mTargetPointPath = new PathController.Path();

	private Vector3 mTargetPosition = default(Vector3);

	private PathSpline.SplinePosition mTargetSplinePosition = default(PathSpline.SplinePosition);

	private bool mUseTargetPosition;

	private float mBlendLength;

	private float mHalfBlendLength;

	private float mPathSpace;

	private float mPathSpaceOnBlendStart;

	private bool mWaitToGetCloseToPath;

	public enum State
	{
		None,
		WaitingToBlend,
		Blending,
		Finishing,
		Finished
	}

	public enum TargetResult
	{
		None,
		ZeroSpeed,
		PathChange
	}
}
