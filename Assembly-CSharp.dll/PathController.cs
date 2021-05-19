using System;
using System.Collections.Generic;
using Dest.Math;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class PathController : InstanceCounter
{
	public PathController()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static PathController()
	{
	}

	public void Start(Vehicle inVehicle)
	{
		this.mVehicle = inVehicle;
		if (!Game.IsSimulatingSeason && Game.instance.sessionManager.IsPlayerChampionship())
		{
			int num = 11;
			for (int i = 0; i < num; i++)
			{
				PathController.Path path = new PathController.Path();
				path.racingLinePosition.id = -1;
				path.centerLinePosition.id = -1;
				this.mPath[i] = path;
				path.vehicle = this.mVehicle;
				path.pathType = (PathController.PathType)i;
			}
			this.SetCurrentPath(PathController.PathType.Track);
		}
		this.SetupDistanceToVehicleArray();
		this.mCachedSessionType = Game.instance.sessionManager.sessionType;
	}

	private void SetupDistanceToVehicleArray()
	{
		this.mDistanceToVehicle = new float[25];
		for (int i = 0; i < this.mDistanceToVehicle.Length; i++)
		{
			this.mDistanceToVehicle[i] = float.MaxValue;
		}
	}

	public void OnLoad()
	{
		this.SetupDistanceToVehicleArray();
		this.GetCurrentPath();
		for (int i = 0; i < this.mPath.Length; i++)
		{
			this.mPath[i].OnLoad();
		}
		if (this.mPath.Length != 11)
		{
			PathController.Path[] array = new PathController.Path[11];
			for (int j = 0; j < this.mPath.Length; j++)
			{
				array[j] = this.mPath[j];
			}
			for (int k = 0; k < array.Length; k++)
			{
				if (array[k] == null)
				{
					PathController.Path path = new PathController.Path();
					path.racingLinePosition.id = -1;
					path.centerLinePosition.id = -1;
					array[k] = path;
					path.vehicle = this.mVehicle;
					path.pathType = (PathController.PathType)k;
				}
			}
			this.mPath = array;
		}
		this.mCachedSessionType = Game.instance.sessionManager.sessionType;
	}

	public void SimulationUpdate()
	{
		this.mDistanceSinceLastGate += this.mVehicle.speed * GameTimer.simulationDeltaTime;
		this.mDistanceAlongTrackPath01 = this.CalculateDistanceAlongPath01(PathController.PathType.Track);
		this.UpdateDistancesToObstacles();
		this.UpdateCachedData();
		this.CheckForPassedGate();
		if (!Game.instance.sessionManager.isSkippingSession)
		{
			this.FindNearbyObstacles();
		}
	}

	private void UpdateCachedData()
	{
		int count = Game.instance.sessionManager.standings.Count;
		for (int i = 0; i < count; i++)
		{
			Vehicle vehicle = Game.instance.sessionManager.standings[i];
			if (vehicle != this.mVehicle)
			{
				this.mIsInComparablePath[vehicle.id] = this.CalculateIsOnComparablePath(vehicle);
			}
		}
		SafetyVehicle safetyVehicle = Game.instance.vehicleManager.safetyVehicle;
		if (safetyVehicle.id == 0)
		{
			safetyVehicle.id = Game.instance.vehicleManager.vehicleCount;
		}
		this.mIsInComparablePath[safetyVehicle.id] = this.CalculateIsOnComparablePath(safetyVehicle);
	}

	private void UpdateDistancesToObstacles()
	{
		int count = this.nearbyObstacles.Count;
		this.vehicleAheadOnPath = null;
		float num = float.MaxValue;
		this.vehicleBehindOnPath = null;
		float num2 = float.MinValue;
		PathController.Path currentPath = this.GetCurrentPath();
		PathData data = currentPath.data;
		for (int i = 0; i < count; i++)
		{
			Vehicle vehicle = this.nearbyObstacles[i];
			float num3 = this.CalculatePathDistanceToVehicle(currentPath, data, vehicle);
			this.mDistanceToVehicle[vehicle.id] = num3;
			if (num3 >= 0f && num3 < num)
			{
				num = num3;
				this.vehicleAheadOnPath = vehicle;
			}
			else if (num3 < 0f && num3 > num2)
			{
				num2 = num3;
				this.vehicleBehindOnPath = vehicle;
			}
		}
	}

	public void UpdatePathPositionData()
	{
		PathController.CalculatePathPositionData(this.GetCurrentPath(), this.vehicle.transform.position);
	}

	public static void CalculatePathPositionData(PathController.Path inPath, Vector3 inPosition)
	{
		if (inPath.previousGateIdAccessor >= 0 && inPath.previousGateIdAccessor < inPath.data.gates.Count)
		{
			PathData.Gate gate = inPath.data.gates[inPath.previousGateIdAccessor];
			inPath.data.racingLineSpline.FindSplinePositionForPoint(inPosition, gate.racingLineStart, gate.racingLineEnd, out inPath.racingLinePosition, inPath.racingLinePosition.id);
			inPath.data.centerLineSpline.FindSplinePositionForPoint(inPath.racingLinePosition.position, gate.centerLineStart, gate.centerLineEnd, out inPath.centerLinePosition, inPath.centerLinePosition.id);
			PathController.mVehiclePathSpacePlane.Normal = inPath.racingLinePosition.forward;
			PathController.mVehiclePathSpacePlane.Constant = Vector3.Dot(PathController.mVehiclePathSpacePlane.Normal, inPath.racingLinePosition.position);
			inPath.centerLinePosition.position = PathController.mVehiclePathSpacePlane.Project(inPath.centerLinePosition.position);
			inPath.centerLinePosition.forward = inPath.racingLinePosition.forward;
			inPath.centerLinePosition.right = inPath.racingLinePosition.right;
			inPath.pathSpace = PathController.ConvertWorldSpaceToPathSpace(inPath, inPosition);
		}
	}

	public void SetCurrentPath(PathController.PathType inPath)
	{
		if (this.mCurrentPathType == PathController.PathType.Track)
		{
			this.mGateIDsPassedWhileOffTrack.Clear();
		}
		this.mCurrentPathType = inPath;
		this.mCurrentPathTypeInt = (int)inPath;
		PathController.Path currentPath = this.GetCurrentPath();
		int nextGateIdAccessor = currentPath.nextGateIdAccessor;
		PathController.UpdatePathToNearestGate(currentPath, this.vehicle.transform.position, null);
		if (currentPath.nextGateIdAccessor == -1)
		{
			int num = currentPath.data.gates.Count - 1;
			currentPath.previousGateIdAccessor = num - 1;
			currentPath.nextGateIdAccessor = num;
		}
		if (currentPath.data.gates[currentPath.nextGateIdAccessor].gateType == PathData.GateType.BrakingZone)
		{
			this.mNextBrakingGateId = currentPath.nextGateIdAccessor;
		}
		else
		{
			this.FindNextBrakingGate();
		}
		currentPath.racingLinePosition.id = -1;
		currentPath.centerLinePosition.id = -1;
		this.UpdatePathPositionData();
		this.mVehicle.OnEnterPath(inPath);
		if (this.mCachedSessionType == SessionDetails.SessionType.Race && Game.instance.sessionManager.isSessionActive && nextGateIdAccessor != currentPath.nextGateIdAccessor && this.mCurrentPathType == PathController.PathType.Track)
		{
			this.CheckPassedTrackGates(nextGateIdAccessor, currentPath);
		}
	}

	public bool IsOnPitlanePath()
	{
		bool result = false;
		switch (this.mVehicle.pathController.currentPathType)
		{
		case PathController.PathType.Pitlane:
		case PathController.PathType.PitlaneEntry:
		case PathController.PathType.PitlaneExit:
		case PathController.PathType.PitboxEntry:
		case PathController.PathType.PitboxExit:
			result = true;
			break;
		}
		return result;
	}

	public static void UpdatePathToNearestGate(PathController.Path inPath, Vector3 inPosition, Vehicle inVehicle = null)
	{
		int previousGateIdAccessor = -1;
		int nextGateIdAccessor = -1;
		PathController.CalculatePreviousAndNextGate(inPath, inPosition, ref previousGateIdAccessor, ref nextGateIdAccessor, inVehicle);
		inPath.previousGateIdAccessor = previousGateIdAccessor;
		inPath.nextGateIdAccessor = nextGateIdAccessor;
	}

	public static void CalculatePreviousAndNextGate(PathController.Path inPath, Vector3 inPosition, ref int refPreviousGateID, ref int refNextGateID, Vehicle inVehicle = null)
	{
		PathData.Gate gate = null;
		float num = float.MaxValue;
		List<PathData.Gate> gates = inPath.data.gates;
		int count = gates.Count;
		CircuitScene circuitScene = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (inVehicle != null)
		{
			circuitScene = Game.instance.sessionManager.circuit;
			flag = inVehicle.pathController.IsOnPitlanePath();
			flag2 = (inVehicle.pathController.currentPathType == PathController.PathType.RunWideLane);
			flag3 = (inVehicle.pathController.currentPathType == PathController.PathType.CutCornerLane);
		}
		bool flag4 = circuitScene != null;
		bool flag5 = inVehicle != null;
		bool flag6 = inPath.pathType == PathController.PathType.Track;
		for (int i = 0; i < count; i++)
		{
			bool flag7 = true;
			if (flag5 && flag4 && flag6)
			{
				if (flag)
				{
					flag7 = PathUtility.IsGateInBetweenOthersInclusive(circuitScene.pitlaneEntryTrackPathID, circuitScene.pitlaneExitTrackPathID, i);
				}
				else if (flag2)
				{
					PathController.Path path = inVehicle.pathController.GetPath(PathController.PathType.RunWideLane);
					flag7 = PathUtility.IsGateInBetweenOthersInclusive(circuitScene.GetEntryTrackIDForPathType(PathController.PathType.RunWideLane, path.pathID), circuitScene.GetExitTrackIDForPathType(PathController.PathType.RunWideLane, path.pathID), i);
				}
				else if (flag3)
				{
					PathController.Path path2 = inVehicle.pathController.GetPath(PathController.PathType.CutCornerLane);
					flag7 = PathUtility.IsGateInBetweenOthersInclusive(circuitScene.GetEntryTrackIDForPathType(PathController.PathType.CutCornerLane, path2.pathID), circuitScene.GetExitTrackIDForPathType(PathController.PathType.CutCornerLane, path2.pathID), i);
				}
			}
			if (flag7)
			{
				PathData.Gate gate2 = gates[i];
				float sqrMagnitude = (gate2.position - inPosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					gate = gate2;
				}
			}
		}
		if (gate == null)
		{
			if (inVehicle != null)
			{
				global::Debug.LogErrorFormat("Could not find nearest gate, something went wrong. Current path type = {0}, Vehicle {1}", new object[]
				{
					inVehicle.pathController.currentPathType.ToString(),
					inVehicle.name
				});
			}
			else
			{
				global::Debug.LogError("Could not find nearest gate, something went wrong. ", null);
			}
			return;
		}
		Vector3 from = inPosition - gate.position;
		float num2 = Vector3.Angle(from, gate.normal);
		bool flag8 = num2 < 90f;
		if (flag8)
		{
			refPreviousGateID = gate.id;
			refNextGateID = PathController.FindNextGate(inPath, refPreviousGateID);
		}
		else
		{
			refNextGateID = gate.id;
			if (inPath.data.type == PathData.Type.Loop)
			{
				refPreviousGateID = PathUtility.WrapIndex(refNextGateID - 1, count);
			}
			else
			{
				refPreviousGateID = PathUtility.ClampIndex(refNextGateID - 1, count);
				if (refPreviousGateID == 0)
				{
					refNextGateID = 1;
				}
			}
		}
	}

	private static int FindNextGate(PathController.Path inPath, int inGateID)
	{
		List<PathData.Gate> gates = inPath.data.gates;
		int count = gates.Count;
		int result;
		if (inPath.data.type == PathData.Type.Loop)
		{
			result = PathUtility.WrapIndex(inGateID + 1, count);
		}
		else
		{
			result = PathUtility.ClampIndex(inGateID + 1, count);
		}
		return result;
	}

	public static float ConvertWorldSpaceToPathSpace(PathController.Path inPath, Vector3 inWorldPosition)
	{
		float width = inPath.data.width;
		return PathController.GetWorldSpaceToPathSpace(inPath.racingLinePosition.position, inPath.racingLinePosition.forward, inPath.racingLinePosition.right, inWorldPosition, width);
	}

	public static float ConvertWorldSpaceToPathSpace(PathController.Path inPath, Vector3 inWorldPosition, PathSpline.SplinePosition inSplinePosition)
	{
		float width = inPath.data.width;
		return PathController.GetWorldSpaceToPathSpace(inSplinePosition.position, inSplinePosition.forward, inSplinePosition.right, inWorldPosition, width);
	}

	private static float GetWorldSpaceToPathSpace(Vector3 position, Vector3 forward, Vector3 right, Vector3 inWorldPosition, float inTrackWidth)
	{
		PathController.mVehiclePathSpacePlane.Normal = forward;
		PathController.mVehiclePathSpacePlane.Constant = Vector3.Dot(PathController.mVehiclePathSpacePlane.Normal, position);
		inWorldPosition = PathController.mVehiclePathSpacePlane.Project(inWorldPosition);
		Vector3 from = inWorldPosition - position;
		float magnitude = from.magnitude;
		float num = magnitude / inTrackWidth;
		if (magnitude != 1f)
		{
			from.Normalize();
		}
		if (Vector3.Angle(from, right) > 90f)
		{
			num = -num;
		}
		return num;
	}

	public static Vector3 ConvertPathSpaceToWorldSpace(PathController.Path inPath, float inPathSpace)
	{
		return inPath.racingLinePosition.position + inPath.racingLinePosition.right * (inPathSpace * inPath.data.width);
	}

	public bool IsPotentialObstacleToMainTrackPath()
	{
		switch (this.currentPathType)
		{
		case PathController.PathType.PitlaneExit:
		case PathController.PathType.RunWideLane:
		case PathController.PathType.CutCornerLane:
			return true;
		case PathController.PathType.CrashLane:
			return !this.vehicle.behaviourManager.GetBehaviour<AICrashingBehaviour>().isOutOfTrack;
		}
		return false;
	}

	public float GetPathSpaceWidth(float inWidth)
	{
		PathController.Path currentPath = this.GetCurrentPath();
		return inWidth / (currentPath.data.width * 2f);
	}

	private void FindNearbyObstacles()
	{
		this.nearbyObstacles.Clear();
		int vehicleCount = Game.instance.vehicleManager.vehicleCount;
		bool flag = false;
		if (this.vehicle is RacingVehicle)
		{
			RacingVehicle racingVehicle = this.vehicle as RacingVehicle;
			if (racingVehicle.ERSController != null)
			{
				flag = (racingVehicle.ERSController.mode == ERSController.Mode.Power);
			}
		}
		int inGateDeltaToConsiderNearby = (!flag) ? 10 : 40;
		int id = this.GetPreviousGate().id;
		this.vehicleAheadOnPath = null;
		float num = float.MaxValue;
		this.vehicleBehindOnPath = null;
		float num2 = float.MinValue;
		VehicleManager vehicleManager = Game.instance.vehicleManager;
		PathData currentPathData = this.GetCurrentPathData();
		for (int i = 0; i < vehicleCount; i++)
		{
			Vehicle vehicle = vehicleManager.GetVehicle(i);
			if (this.AddObstacle(id, inGateDeltaToConsiderNearby, vehicle, currentPathData))
			{
				this.nearbyObstacles.Add(vehicle);
			}
			if (vehicle != this.mVehicle)
			{
				float pathDistanceToVehicle = this.GetPathDistanceToVehicle(vehicle);
				if (pathDistanceToVehicle >= 0f && pathDistanceToVehicle < num)
				{
					num = pathDistanceToVehicle;
					this.vehicleAheadOnPath = vehicle;
				}
				else if (pathDistanceToVehicle < 0f && pathDistanceToVehicle > num2)
				{
					num2 = pathDistanceToVehicle;
					this.vehicleBehindOnPath = vehicle;
				}
			}
		}
		if ((Game.instance.sessionManager.flag == SessionManager.Flag.SafetyCar || Game.instance.sessionManager.isRollingOut) && this.AddObstacle(id, inGateDeltaToConsiderNearby, vehicleManager.safetyVehicle, currentPathData))
		{
			this.nearbyObstacles.Add(vehicleManager.safetyVehicle);
			float pathDistanceToVehicle2 = this.GetPathDistanceToVehicle(vehicleManager.safetyVehicle);
			if (pathDistanceToVehicle2 >= 0f && pathDistanceToVehicle2 < num)
			{
				this.vehicleAheadOnPath = vehicleManager.safetyVehicle;
			}
			else if (pathDistanceToVehicle2 < 0f && pathDistanceToVehicle2 > num2)
			{
				this.vehicleBehindOnPath = vehicleManager.safetyVehicle;
			}
		}
	}

	private bool AddObstacle(int inGateID, int inGateDeltaToConsiderNearby, Vehicle inObstacle, PathData inData)
	{
		if (inObstacle != this.mVehicle)
		{
			bool flag = this.currentPathType == PathController.PathType.Track && inObstacle.pathController.IsPotentialObstacleToMainTrackPath();
			if (flag || inObstacle.pathController.currentPathType == this.currentPathType)
			{
				int count = inData.gates.Count;
				int num;
				int num2;
				if (inData.type == PathData.Type.Loop)
				{
					num = PathUtility.WrapIndex(inGateID - inGateDeltaToConsiderNearby, count);
					num2 = PathUtility.WrapIndex(inGateID + inGateDeltaToConsiderNearby, count);
				}
				else
				{
					num = Math.Max(inGateID - inGateDeltaToConsiderNearby, 0);
					num2 = Math.Min(inGateID + inGateDeltaToConsiderNearby, count - 1);
				}
				int num3;
				if (flag)
				{
					num3 = inObstacle.pathController.GetPath(PathController.PathType.Track).previousGateIdAccessor;
				}
				else
				{
					num3 = inObstacle.pathController.GetPreviousGate().id;
				}
				if (num > num2 && (num3 <= num2 || num3 >= num))
				{
					return true;
				}
				if (num3 >= num && num3 <= num2)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsAheadOfVehicle(Vehicle inVehicle)
	{
		bool result = false;
		if (this.IsOnComparablePath(inVehicle))
		{
			float pathDistanceToVehicle = this.GetPathDistanceToVehicle(inVehicle);
			result = (pathDistanceToVehicle < 0f);
		}
		return result;
	}

	public bool IsBehindVehicle(Vehicle inVehicle)
	{
		bool result = false;
		if (this.IsOnComparablePath(inVehicle))
		{
			float pathDistanceToVehicle = this.GetPathDistanceToVehicle(inVehicle);
			result = (pathDistanceToVehicle > 0f);
		}
		return result;
	}

	public bool IsBesideVehicle(Vehicle inVehicle)
	{
		bool result = false;
		if (this.IsOnComparablePath(inVehicle))
		{
			float pathDistanceToVehicle = this.GetPathDistanceToVehicle(inVehicle);
			result = (Math.Abs(pathDistanceToVehicle) < VehicleConstants.vehicleLength);
		}
		return result;
	}

	public bool IsOnComparablePath(Vehicle inVehicle)
	{
		return this.mIsInComparablePath[inVehicle.id];
	}

	private bool CalculateIsOnComparablePath(Vehicle inVehicle)
	{
		bool result = false;
		if ((this.mCurrentPathType == PathController.PathType.Track || this.mCurrentPathType == PathController.PathType.Pitlane || this.mCurrentPathType == PathController.PathType.PitlaneEntry || this.mCurrentPathType == PathController.PathType.PitlaneExit) && inVehicle.pathController.currentPathType == this.currentPathType)
		{
			result = true;
		}
		if ((this.mCurrentPathType == PathController.PathType.PitboxEntry || this.mCurrentPathType == PathController.PathType.PitboxExit) && inVehicle is RacingVehicle && this.vehicle is RacingVehicle && ((RacingVehicle)inVehicle).driver.contract.GetTeam() == ((RacingVehicle)this.vehicle).driver.contract.GetTeam())
		{
			result = true;
		}
		if (this.mCurrentPathType == PathController.PathType.Track && inVehicle.pathController.IsPotentialObstacleToMainTrackPath())
		{
			result = true;
		}
		return result;
	}

	private void CheckForPassedGate()
	{
		PathController.Path path = this.mPath[this.mCurrentPathTypeInt];
		Vector3 from = this.vehicle.transform.position - path.data.gates[path.nextGateIdAccessor].position;
		from.Normalize();
		float num = Vector3.Angle(from, path.data.gates[path.nextGateIdAccessor].normal);
		bool flag = num < 90f;
		if (flag)
		{
			this.OnGatePassed(path);
			int num2 = PathController.FindNextGate(path, path.nextGateIdAccessor);
			if (num2 != path.nextGateIdAccessor)
			{
				path.previousGateIdAccessor = path.nextGateIdAccessor;
				path.nextGateIdAccessor = num2;
			}
		}
		bool flag2 = this.mCachedSessionType == SessionDetails.SessionType.Race;
		bool flag3 = this.currentPathType != PathController.PathType.Track && (flag2 || this.IsPotentialObstacleToMainTrackPath());
		if (flag3 && this.mVehicle is RacingVehicle && !this.mVehicle.behaviourManager.isOutOfRace)
		{
			PathController.Path path2 = this.GetPath(PathController.PathType.Track);
			int nextGateIdAccessor = path2.nextGateIdAccessor;
			PathController.UpdatePathToNearestGate(path2, this.vehicle.transform.position, this.mVehicle);
			PathController.CalculatePathPositionData(path2, this.vehicle.transform.position);
			if (nextGateIdAccessor != path2.nextGateIdAccessor)
			{
				if (nextGateIdAccessor < path2.nextGateIdAccessor || path2.nextGateIdAccessor == 0)
				{
					this.CheckPassedTrackGates(nextGateIdAccessor, path2);
				}
				else
				{
					path2.nextGateIdAccessor = nextGateIdAccessor;
				}
			}
		}
	}

	private void CheckPassedTrackGates(int inOldNextGateID, PathController.Path inTrackPath)
	{
		int num = inOldNextGateID;
		int nextGateIdAccessor = inTrackPath.nextGateIdAccessor;
		while (num != nextGateIdAccessor)
		{
			inTrackPath.nextGateIdAccessor = num;
			if (this.HasentPassedTroughtTrackGate(num))
			{
				this.OnTrackGatePassed(inTrackPath);
				this.mGateIDsPassedWhileOffTrack.Add(num);
			}
			num = PathUtility.WrapIndex(num + 1, inTrackPath.data.gates.Count);
		}
		if (this.mGateIDsPassedWhileOffTrack.Count > 100)
		{
			global::Debug.LogWarningFormat("{0} vehicle has registered more than 100 gates passed while off track, suspisciously bugged stuff.", new object[]
			{
				this.mVehicle.name
			});
		}
		inTrackPath.nextGateIdAccessor = nextGateIdAccessor;
	}

	private bool HasentPassedTroughtTrackGate(int inNextGateID)
	{
		bool result = true;
		for (int i = 0; i < this.mGateIDsPassedWhileOffTrack.Count; i++)
		{
			if (this.mGateIDsPassedWhileOffTrack[i] == inNextGateID)
			{
				result = false;
			}
		}
		return result;
	}

	private void OnGatePassed(PathController.Path path)
	{
		int nextGateIdAccessor = path.nextGateIdAccessor;
		PathData.Gate gate = path.data.gates[nextGateIdAccessor];
		PathData.GateType gateType = gate.gateType;
		if (path.pathType == PathController.PathType.Track && this.HasentPassedTroughtTrackGate(nextGateIdAccessor))
		{
			this.OnTrackGatePassed(path);
			this.mGateIDsPassedWhileOffTrack.Clear();
		}
		if (path.pathType == this.mCurrentPathType)
		{
			this.vehicle.OnEnterGate(nextGateIdAccessor, gateType);
			if (gateType == PathData.GateType.BrakingZone)
			{
				this.FindNextBrakingGate();
			}
			bool flag = path.data.gates.Count == path.nextGateIdAccessor + 1;
			if (flag)
			{
				this.OnLastGateOnPathPassed();
			}
			if (gate.straight != null && gate.straight.startGateID == nextGateIdAccessor)
			{
				this.vehicle.OnEnterStraight(gate.straight);
			}
			if (gate.corner != null && gate.corner.startGateID == nextGateIdAccessor)
			{
				this.vehicle.OnEnterCorner(gate.corner);
			}
			this.mDistanceSinceLastGate = 0f;
			if (gate.isSparkGate && RandomUtility.GetRandom01() < 0.3f && this.mVehicle is RacingVehicle && ((RacingVehicle)this.mVehicle).setup.tyreSet.GetTread() != TyreSet.Tread.HeavyTread)
			{
				this.vehicle.unityVehicle.ProduceSparks();
			}
		}
	}

	private void FindNextBrakingGate()
	{
		PathData.Gate gate = this.GetCurrentPathData().FindNextGateOfType(this.GetNextGate(), PathData.GateType.BrakingZone);
		this.mNextBrakingGateId = ((gate == null) ? -1 : gate.id);
	}

	public void EnterCrashLane()
	{
		RacingVehicle racingVehicle = this.mVehicle as RacingVehicle;
		if (Game.instance.sessionManager.raceDirector.crashDirector.CalculateCrashChance(racingVehicle, true) || racingVehicle.behaviourManager.GetBehaviour<AIRacingBehaviour>().isSetToCrash)
		{
			racingVehicle.sessionEvents.EventActivated(SessionEvents.EventType.Crash);
			this.SetCurrentPath(PathController.PathType.CrashLane);
			this.vehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Crashing);
		}
		else
		{
			racingVehicle.strategy.SetStatus(SessionStrategy.Status.NoActionRequired);
		}
	}

	public void EnterPath(PathController.PathType inType)
	{
		this.SetCurrentPath(inType);
	}

	public void EnterPitlane()
	{
		if (this.mVehicle is RacingVehicle)
		{
			RacingVehicle racingVehicle = (RacingVehicle)this.mVehicle;
			if (this.mCachedSessionType == SessionDetails.SessionType.Race)
			{
				Game.instance.persistentEventData.PostStintLapInfo(racingVehicle, racingVehicle.timer.currentLap.time, Game.instance.sessionManager.currentSessionWeather.GetNormalizedTrackRubber(), racingVehicle.setup.tyreSet.GetCondition(), Game.instance.sessionManager.currentSessionWeather.GetNormalizedTrackWater());
			}
			else
			{
				racingVehicle.timer.MarkCurrentLapAsInLap();
			}
			if (racingVehicle != null && racingVehicle.timer.HasSetLapTime())
			{
				CommentaryManager.SendComment(racingVehicle, Comment.CommentType.DriverComesIn, new object[]
				{
					racingVehicle.driver
				});
			}
		}
		this.SetCurrentPath(PathController.PathType.PitlaneEntry);
	}

	private void OnTrackGatePassed(PathController.Path path)
	{
		int nextGateIdAccessor = path.nextGateIdAccessor;
		PathData.Gate gate = path.data.gates[nextGateIdAccessor];
		PathData.GateType gateType = gate.gateType;
		if (this.mVehicle is RacingVehicle)
		{
			RacingVehicle racingVehicle = (RacingVehicle)this.mVehicle;
			if (path.pathType == PathController.PathType.Track)
			{
				racingVehicle.timer.UpdateDistanceTraveled(path);
				if (gate.isSpeedTrap)
				{
					racingVehicle.LogSpeedTrapSpeed();
				}
			}
			racingVehicle.timer.OnPassingGate(nextGateIdAccessor);
			if (gateType == PathData.GateType.Sector)
			{
				if (nextGateIdAccessor == 0)
				{
					racingVehicle.timer.OnCrossingStartFinishLine();
				}
				else
				{
					racingVehicle.timer.OnSectorEnd();
				}
			}
		}
		if (gateType == PathData.GateType.Sector && nextGateIdAccessor == 0 && this.vehicle.OnLapEnd != null)
		{
			this.vehicle.OnLapEnd.Invoke();
		}
	}

	private void OnLastGateOnPathPassed()
	{
		RacingVehicle racingVehicle = null;
		if (this.mVehicle is RacingVehicle)
		{
			racingVehicle = (RacingVehicle)this.mVehicle;
		}
		switch (this.mCurrentPathType)
		{
		case PathController.PathType.Pitlane:
			this.SetCurrentPath(PathController.PathType.PitlaneExit);
			break;
		case PathController.PathType.PitlaneEntry:
			if (racingVehicle != null)
			{
				racingVehicle.timer.OnEnterPitlane();
			}
			this.mVehicle.pathState.ChangeState(PathStateManager.StateType.Pitlane);
			break;
		case PathController.PathType.PitlaneExit:
			if (racingVehicle != null)
			{
				racingVehicle.timer.OnExitPitlane();
				racingVehicle.strategy.OnExitPitlane();
				if (Game.IsActive() && this.mCachedSessionType != SessionDetails.SessionType.Race)
				{
					racingVehicle.timer.MarkCurrentLapAsOutLap();
				}
			}
			this.mVehicle.pathState.ChangeState(PathStateManager.StateType.Racing);
			break;
		case PathController.PathType.PitboxExit:
		case PathController.PathType.GarageExit:
			this.mVehicle.pathState.ChangeState(PathStateManager.StateType.Pitlane);
			break;
		case PathController.PathType.CrashLane:
		{
			AICrashingBehaviour behaviour = racingVehicle.behaviourManager.GetBehaviour<AICrashingBehaviour>();
			behaviour.OnVehicleStop(this.mVehicle.speed);
			racingVehicle.movementEnabled = false;
			break;
		}
		case PathController.PathType.RunWideLane:
		case PathController.PathType.CutCornerLane:
			this.EnterPath(PathController.PathType.Track);
			racingVehicle.behaviourManager.ChangeBehaviour(AIBehaviourStateManager.Behaviour.Racing);
			break;
		}
	}

	public void UpdatePathSelected(PathController.PathType inPath, int inLaneIndex = -1)
	{
		this.mPath[(int)inPath].GetRandomPath(inLaneIndex);
	}

	public PathController.Path GetPath(PathController.PathType inPath)
	{
		return this.mPath[(int)inPath];
	}

	public PathController.Path GetPath(int inPath)
	{
		return this.mPath[inPath];
	}

	public PathController.Path GetCurrentPath()
	{
		if (this.mCurrentPathTypeInt == -1)
		{
			this.mCurrentPathTypeInt = (int)this.mCurrentPathType;
		}
		return this.mPath[this.mCurrentPathTypeInt];
	}

	public PathData GetPathData(PathController.PathType inPath)
	{
		return this.GetPath(inPath).data;
	}

	public PathData GetCurrentPathData()
	{
		PathController.Path currentPath = this.GetCurrentPath();
		return currentPath.data;
	}

	public PathData.Gate GetNextGate()
	{
		PathController.Path currentPath = this.GetCurrentPath();
		return (currentPath.nextGateIdAccessor != -1) ? currentPath.data.gates[currentPath.nextGateIdAccessor] : null;
	}

	public PathData.Gate GetPreviousGate()
	{
		PathController.Path currentPath = this.GetCurrentPath();
		return (currentPath.previousGateIdAccessor != -1) ? currentPath.data.gates[currentPath.previousGateIdAccessor] : null;
	}

	public float GetPathDistanceToVehicle(Vehicle inVehicle)
	{
		return this.mDistanceToVehicle[inVehicle.id];
	}

	private float CalculatePathDistanceToVehicle(PathController.Path inPath, PathData inPathData, Vehicle inVehicle)
	{
		float result = float.MaxValue;
		PathController pathController = inVehicle.pathController;
		if (pathController.currentPathType == this.currentPathType || (this.currentPathType == PathController.PathType.Track && pathController.IsPotentialObstacleToMainTrackPath()))
		{
			float pathDistance = inPath.racingLinePosition.pathDistance;
			float pathDistance2 = pathController.GetCurrentPath().racingLinePosition.pathDistance;
			result = SimulationUtilityDLLParser.CalculatePathDistanceToVehicle(pathDistance, pathDistance2, inPathData.length);
		}
		return result;
	}

	public float GetDistanceAlongPath(PathController.PathType inPath)
	{
		PathController.Path path = this.GetPath(inPath);
		float num = path.racingLinePosition.pathDistance;
		float num2 = MathsUtility.Clamp01(num / path.data.racingLineSpline.length);
		if (path.previousGateIdAccessor == 0 && num2 > 0.5f)
		{
			num = 0f;
		}
		return num;
	}

	public float GetRaceDistanceUsingGates01()
	{
		PathController.Path path = this.GetPath(PathController.PathType.Track);
		float num = MathsUtility.Clamp01((float)path.nextGateIdAccessor / (float)path.data.gates.Count);
		RacingVehicle racingVehicle = (RacingVehicle)this.vehicle;
		float num2 = (float)Game.instance.sessionManager.lapCount;
		float num3 = (float)racingVehicle.timer.lap;
		num3 = MathsUtility.Clamp(num3, (float)racingVehicle.timer.lap, num2);
		float num4 = num3 / num2;
		float num5 = num4;
		if (racingVehicle.timer.hasCrossedStartLine)
		{
			num5 += num / num2;
		}
		return num5;
	}

	public float GetDistanceAlongPath01(PathController.PathType inPath)
	{
		if (inPath == PathController.PathType.Track)
		{
			return this.mDistanceAlongTrackPath01;
		}
		return this.CalculateDistanceAlongPath01(inPath);
	}

	private float CalculateDistanceAlongPath01(PathController.PathType inPath)
	{
		PathController.Path path = this.GetPath(inPath);
		return MathsUtility.Clamp01(this.GetDistanceAlongPath(inPath) / path.data.racingLineSpline.length);
	}

	public float GetRaceDistanceTraveled01()
	{
		RacingVehicle racingVehicle = (RacingVehicle)this.vehicle;
		if (racingVehicle.championship.series == Championship.Series.EnduranceSeries)
		{
			return Game.instance.sessionManager.GetNormalizedSessionTime();
		}
		float distanceAlongTrackPath = this.distanceAlongTrackPath01;
		float num = (float)Game.instance.sessionManager.lapCount;
		float num2 = (float)racingVehicle.timer.lap;
		num2 = MathsUtility.Clamp(num2, (float)racingVehicle.timer.lap, num);
		float num3 = num2 / num;
		float num4 = num3;
		if (racingVehicle.timer.hasCrossedStartLine)
		{
			num4 += distanceAlongTrackPath / num;
		}
		return MathsUtility.Clamp01(num4);
	}

	public bool IsInCorner()
	{
		return this.GetCurrentCorner() != null;
	}

	public PathData.Corner GetCurrentCorner()
	{
		return this.GetPreviousGate().corner;
	}

	public float GetCurrentCornerRadius()
	{
		if (this.IsInCorner())
		{
			return this.GetPreviousGate().corner.radius;
		}
		return 0f;
	}

	public bool IsOnStraight()
	{
		return this.GetPreviousGate().straight != null;
	}

	public PathData.Straight GetCurrentStraight()
	{
		return this.GetPreviousGate().straight;
	}

	public bool IsInDraftingZone()
	{
		return this.GetPreviousGate().distanceToCorner > float.Epsilon && this.GetPreviousGate().distanceToCorner > 100f;
	}

	public bool IsOvertakeZoneOfCurrentStraight()
	{
		PathData.Gate previousGate = this.GetPreviousGate();
		return previousGate.straight != null && previousGate.straight.startGateID != previousGate.id && previousGate.distanceToCorner > 100f;
	}

	public bool IsInOvertakeZoneOf(Vehicle inVehicleAhead)
	{
		if (this.IsOnComparablePath(inVehicleAhead))
		{
			float pathDistanceToVehicle = this.GetPathDistanceToVehicle(inVehicleAhead);
			SessionManager sessionManager = Game.instance.sessionManager;
			if (pathDistanceToVehicle > 1E-45f && !sessionManager.hasSessionEnded)
			{
				if (this.mCachedSessionType == SessionDetails.SessionType.Race && !(this.mVehicle is SafetyVehicle))
				{
					GateInfo gateTimer = sessionManager.GetGateTimer(this.mVehicle.pathController.GetPreviousGate().id);
					float timeGapBetweenVehicles = gateTimer.GetTimeGapBetweenVehicles(inVehicleAhead, this.mVehicle);
					if (timeGapBetweenVehicles < 0.5f)
					{
						return true;
					}
				}
				else
				{
					if (inVehicleAhead.speed < this.mVehicle.speed && inVehicleAhead.pathController.IsOnStraight())
					{
						float num = this.mVehicle.speed - inVehicleAhead.speed;
						float num2 = (pathDistanceToVehicle - VehicleConstants.vehicleLength) / num;
						if (num2 < 0.5f)
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

	public float distanceAlongTrackPath01
	{
		get
		{
			return this.mDistanceAlongTrackPath01;
		}
	}

	public Vehicle vehicle
	{
		get
		{
			return this.mVehicle;
		}
	}

	public PathController.PathType currentPathType
	{
		get
		{
			return this.mCurrentPathType;
		}
	}

	public PathData.Gate nextBrakingGate
	{
		get
		{
			return (this.mNextBrakingGateId != -1) ? this.GetCurrentPath().data.gates[this.mNextBrakingGateId] : null;
		}
	}

	public PathSpline.SplinePosition racingLinePosition
	{
		get
		{
			return this.GetCurrentPath().racingLinePosition;
		}
	}

	public PathSpline.SplinePosition centerLinePosition
	{
		get
		{
			return this.GetCurrentPath().centerLinePosition;
		}
	}

	public float distanceSinceLastGate
	{
		get
		{
			return this.mDistanceSinceLastGate;
		}
	}

	private static Plane3 mVehiclePathSpacePlane = default(Plane3);

	public List<Vehicle> nearbyObstacles = new List<Vehicle>(20);

	public Vehicle vehicleAheadOnPath;

	public Vehicle vehicleBehindOnPath;

	private Vehicle mVehicle;

	private PathController.Path[] mPath = new PathController.Path[11];

	private List<int> mGateIDsPassedWhileOffTrack = new List<int>(32);

	private float[] mDistanceToVehicle = new float[0];

	private PathController.PathType mCurrentPathType;

	private int mCurrentPathTypeInt = -1;

	private int mNextBrakingGateId = -1;

	private float mDistanceSinceLastGate;

	private bool[] mIsInComparablePath = new bool[25];

	private float mDistanceAlongTrackPath01;

	private SessionDetails.SessionType mCachedSessionType;

	public enum PathType
	{
		Track,
		Pitlane,
		PitlaneEntry,
		PitlaneExit,
		PitboxEntry,
		PitboxExit,
		GarageEntry,
		GarageExit,
		CrashLane,
		RunWideLane,
		CutCornerLane,
		Count
	}

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	public class Path
	{
		public Path()
		{
		}

		public void CopyPath(PathController.Path inPath)
		{
			this.pathType = inPath.pathType;
			this.vehicle = inPath.vehicle;
			this._data = inPath._data;
		}

		public PathData data
		{
			get
			{
				if (this._data != null)
				{
					return this._data;
				}
				CircuitScene circuit = Game.instance.sessionManager.circuit;
				TeamPitGarage teamPitGarage = null;
				if (this.pathType != PathController.PathType.Track)
				{
					if (this.vehicle != null && this.vehicle is RacingVehicle)
					{
						Team team = ((RacingVehicle)this.vehicle).driver.contract.GetTeam();
						teamPitGarage = circuit.GetTeamPitGarage(team);
					}
					else
					{
						teamPitGarage = circuit.GetGarageForSafetyCar();
					}
				}
				switch (this.pathType)
				{
				case PathController.PathType.Track:
					this._data = circuit.GetTrackPath().data;
					break;
				case PathController.PathType.Pitlane:
					this._data = circuit.pitlanePath.data;
					break;
				case PathController.PathType.PitlaneEntry:
					this._data = circuit.pitlaneEntryPath.data;
					break;
				case PathController.PathType.PitlaneExit:
					this._data = circuit.pitlaneExitPath.data;
					break;
				case PathController.PathType.PitboxEntry:
					this._data = teamPitGarage.pitboxEntryPath.data;
					break;
				case PathController.PathType.PitboxExit:
					this._data = teamPitGarage.pitboxExitPath.data;
					break;
				case PathController.PathType.GarageEntry:
					this._data = teamPitGarage.GetGarageEntryForVehicle(this.vehicle).data;
					break;
				case PathController.PathType.GarageExit:
					this._data = teamPitGarage.GetGarageExitForVehicle(this.vehicle).data;
					break;
				case PathController.PathType.CrashLane:
				case PathController.PathType.RunWideLane:
				case PathController.PathType.CutCornerLane:
					this._data = circuit.GetRandomPathOfType(out this.pathID, this.pathType, this.vehicle, -1).data;
					break;
				}
				return this._data;
			}
		}

		public void GetRandomPath(int inLaneIndex = -1)
		{
			switch (this.pathType)
			{
			case PathController.PathType.CrashLane:
			case PathController.PathType.RunWideLane:
			case PathController.PathType.CutCornerLane:
				if (inLaneIndex != -1)
				{
					this._data = Game.instance.sessionManager.circuit.GetRandomPathOfType(out this.pathID, this.pathType, this.vehicle, inLaneIndex).data;
				}
				else
				{
					this._data = Game.instance.sessionManager.circuit.GetRandomPathOfType(out this.pathID, this.pathType, this.vehicle, -1).data;
				}
				break;
			}
		}

		public void GetClosestPath()
		{
			switch (this.pathType)
			{
			case PathController.PathType.CrashLane:
			case PathController.PathType.RunWideLane:
			case PathController.PathType.CutCornerLane:
				this._data = Game.instance.sessionManager.circuit.GetClosestLaneOfType(out this.pathID, this.pathType, this.vehicle).data;
				break;
			}
		}

		public int previousGateIdAccessor
		{
			get
			{
				return this.previousGateId;
			}
			set
			{
				this.SetPreviousGateID(value);
			}
		}

		public int nextGateIdAccessor
		{
			get
			{
				return this.nextGateId;
			}
			set
			{
				this.SetNextGateID(value);
			}
		}

		private void SetPreviousGateID(int inValue)
		{
			this.previousGateId = inValue;
		}

		private void SetNextGateID(int inValue)
		{
			this.nextGateId = inValue;
		}

		public void OnLoad()
		{
			if (this.mPreviousGateId != -1 && this.previousGateId == -1)
			{
				this.previousGateId = this.mPreviousGateId;
			}
			if (this.mNextGateId != -1 && this.nextGateId == -1)
			{
				this.nextGateId = this.mNextGateId;
			}
		}

		public PathController.PathType pathType;

		public Vehicle vehicle;

		public int pathID;

		[NonSerialized]
		private PathData _data;

		private int previousGateId = -1;

		private int nextGateId = -1;

		private int mPreviousGateId = -1;

		private int mNextGateId = -1;

		public PathSpline.SplinePosition racingLinePosition = default(PathSpline.SplinePosition);

		public PathSpline.SplinePosition centerLinePosition = default(PathSpline.SplinePosition);

		public float pathSpace;
	}
}
