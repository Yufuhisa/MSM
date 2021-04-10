using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Vehicle : InstanceCounter
{
	public Vehicle()
	{
	}

	public Vehicle(int inID, UnityVehicleManager unityVehicleManager)
	{
		this.id = inID;
		this.name = "Vehicle";
		this.enabled = true;
		this.mUnityVehicleManager = unityVehicleManager;
		this.mChampionship = Game.instance.sessionManager.championship;
		global::Debug.Assert(this.mChampionship != null, "Championship is null");
		this.mPathStateManager.Start(this);
		this.mPerformance.Start(this);
		this.mSteeringManager.Start(this);
		this.mSpeedManager.Start(this);
		this.mPathController.Start(this);
		this.mBehaviourManager.Start(this);
		this.mThrottle.SetValue(0f, AnimatedFloat.Animation.DontAnimate);
		if (this.mUnityVehicleManager != null)
		{
			this.mUnityVehicle = unityVehicleManager.CreateCar(this.name, this);
		}
		this.CreateCollisionBounds();
	}

	public virtual void OnLoad(UnityVehicleManager unityVehicleManager)
	{
		if (this.mChampionship == null)
		{
			this.mChampionship = Game.instance.sessionManager.championship;
		}
		this.mUnityVehicleManager = unityVehicleManager;
		this.mUnityVehicle = unityVehicleManager.CreateCar(this.name, this);
		if (this.mPerformance == null)
		{
			this.mPerformance = new SessionPerformance();
		}
		this.mPerformance.OnLoad(this);
		this.mBehaviourManager.OnLoad();
		this.mBehaviourManager.currentBehaviour.OnLoad();
		this.mPathController.OnLoad();
		this.mSteeringManager.OnLoad();
	}

	public virtual void Destroy()
	{
		SafeAction.NullAnAction(ref this.OnLapEnd);
		if (this is RacingVehicle)
		{
			Game.instance.persistentEventData.SaveTyreData((RacingVehicle)this);
		}
		if (this.mUnityVehicleManager != null)
		{
			this.mUnityVehicleManager.DestroyCar(this.mUnityVehicle);
			this.mUnityVehicle = null;
			this.mUnityVehicleManager = null;
		}
		if (this.mBehaviourManager != null)
		{
			this.mBehaviourManager.Destroy();
		}
		if (this.mPathStateManager != null)
		{
			this.mPathStateManager.Destroy();
		}
	}

	protected void CreateCollisionBounds()
	{
		float num = 0f;
		float num2 = 0f;
		switch (this.championship.series)
		{
		case Championship.Series.SingleSeaterSeries:
			num = VehicleConstants.vehicleWidth;
			num2 = VehicleConstants.vehicleLength;
			break;
		case Championship.Series.GTSeries:
			num = VehicleConstants.vehicleWidthGT;
			num2 = VehicleConstants.vehicleLengthGT;
			break;
		case Championship.Series.EnduranceSeries:
			num = VehicleConstants.vehicleWidthEndurance;
			num2 = VehicleConstants.vehicleLengthEndurance;
			break;
		}
		num *= 0.5f;
		num2 *= 0.5f;
		this.mCollisionBounds.AddPoint(num, num2);
		this.mCollisionBounds.AddPoint(num, -num2);
		this.mCollisionBounds.AddPoint(-num, -num2);
		this.mCollisionBounds.AddPoint(-num, num2);
		this.mCollisionBounds.Offset(0f, 0f);
		this.mCollisionBounds.Build();
		this.mTransform.SetCollisionBounds(this.mCollisionBounds);
	}

	public virtual void Hide()
	{
		if (this.mUnityVehicle != null)
		{
			this.mUnityVehicle.gameObject.SetActive(false);
		}
	}

	public virtual void UnHide()
	{
		if (this.mUnityVehicle != null)
		{
			this.mUnityVehicle.gameObject.SetActive(true);
		}
	}

	public virtual void Update()
	{
	}

	public virtual void SimulationUpdate()
	{
		this.mPreviousPosition = this.transform.position;
		this.mBehaviourManager.SimulationUpdate();
		if (Game.instance.sessionManager.isSessionActive)
		{
			this.mPerformance.SimulationUpdate();
		}
	}

	public void UpdateSteering()
	{
		this.mSteeringManager.SimulationUpdate();
	}

	public void UpdateSpeed()
	{
		this.mSpeedManager.Update();
		float desiredSpeed = this.mSpeedManager.desiredSpeed;
		this.mPedalStateTimer += GameTimer.simulationDeltaTime;
		if (this.speed < desiredSpeed)
		{
			float num = this.GetAcceleration();
			num = MathsUtility.Lerp(num * 0.5f, num, MathsUtility.Clamp01(this.mPedalStateTimer / 0.75f));
			this.speed += num * GameTimer.simulationDeltaTime;
			this.speed = Math.Min(this.speed, desiredSpeed);
			this.SetPedalState(Vehicle.PedalState.Accelerating);
		}
		else if (this.speed > desiredSpeed)
		{
			this.speed -= this.GetBraking() * GameTimer.simulationDeltaTime;
			this.speed = Math.Max(this.speed, desiredSpeed);
			this.SetPedalState(Vehicle.PedalState.Braking);
		}
		else
		{
			this.speed = desiredSpeed;
		}
		this.speed = Math.Max(this.speed, 0f);
		this.speed = Math.Min(this.speed, this.mMaxSpeed);
		this.mVelocity = this.mSteeringManager.steeringDirection * this.speed;
		this.mTransform.SetPosition(this.mTransform.position + this.mVelocity * GameTimer.simulationDeltaTime);
		this.currentAcceleration = this.GetAcceleration();
		this.currentBraking = this.GetBraking();
		this.mBraking = 0f;
		this.mAcceleration = float.MaxValue;
		this.mMaxSpeed = float.MaxValue;
	}

	public void PostSimulationUpdate()
	{
		if (!Game.instance.sessionManager.isSkippingSession)
		{
			if (this.speed > 0f)
			{
				this.UpdateCollisionWithPathBounds();
			}
			this.UpdateCollisionWithVehicles();
		}
		Vector3 position = this.transform.position;
		position.y = this.pathController.racingLinePosition.position.y;
		this.transform.SetPosition(position);
	}

	private void UpdateCollisionWithPathBounds()
	{
		PathController.Path currentPath = this.pathController.GetCurrentPath();
		float pathCenterPathSpace = this.mSteeringManager.pathCenterPathSpace;
		float num = currentPath.pathSpace + -pathCenterPathSpace;
		float num2 = 0.8f;
		if (Math.Abs(num) > num2)
		{
			float num3 = (num >= 0f) ? (num - num2) : (num + num2);
			num3 = -num3;
			float num4 = num + num3;
			num4 -= num;
			num4 = MathsUtility.Lerp(0f, num4, 2f * GameTimer.simulationDeltaTime);
			this.transform.SetPosition(this.transform.position + currentPath.racingLinePosition.right * num4 * currentPath.data.width);
			float angle = 40f * num4 * GameTimer.simulationDeltaTime;
			Vector3 vector = this.transform.forward;
			vector = Quaternion.AngleAxis(angle, Vector3.up) * vector;
			this.transform.SetForward(vector);
			this.pathController.UpdatePathPositionData();
		}
	}

	private void UpdateCollisionWithVehicles()
	{
		bool flag = Math.Abs(this.pathController.GetCurrentPath().pathSpace) < 0.9f;
		if (flag)
		{
			Vector3 zero = Vector3.zero;
			bool flag2 = false;
			bool flag3 = Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race;
			int count = this.pathController.nearbyObstacles.Count;
			for (int i = 0; i < count; i++)
			{
				Vehicle vehicle = this.pathController.nearbyObstacles[i];
				if (this != vehicle)
				{
					float num = Math.Abs(this.pathController.GetPathDistanceToVehicle(vehicle));
					flag = (num < VehicleConstants.vehicleLength * 2f);
					if (flag)
					{
						bool flag4 = this.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.InOutLap;
						bool flag5 = vehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.InOutLap;
						bool IsSpinning = (vehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.Spin || this.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.Spin);
						if (flag3)
						{
							flag4 = (this.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.BlueFlag);
							flag5 = (vehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.BlueFlag);
						}
						if (IsSpinning || (flag4 && !flag5) || (!flag4 && flag5))
						{
							flag = false;
						}
					}
					if (flag)
					{
						CollisionResult collisionResult = CollisionDetection.Intersects(this.mCollisionBounds, vehicle.mCollisionBounds, Vector2.zero);
						if (collisionResult.intersecting)
						{
							flag2 = true;
							zero.x += collisionResult.collisionResponse.x;
							zero.z += collisionResult.collisionResponse.y;
							float num2 = Math.Abs(collisionResult.collisionResponse.x) + Math.Abs(collisionResult.collisionResponse.y);
							if (this.mCollisionCooldown <= 0f && (num2 > 0.9f || (num2 > 0.8f && RandomUtility.GetRandom01() > 0.8f)))
							{
								Game.instance.sessionManager.raceDirector.OnVehicleCollision(this, vehicle);
								this.mCollisionCooldown = 5f;
							}
						}
					}
				}
			}
			if (flag2)
			{
				this.transform.SetPosition(this.transform.position + zero);
				this.pathController.UpdatePathPositionData();
			}
		}
		this.mCollisionCooldown -= GameTimer.simulationDeltaTime;
	}

	public virtual void OnFlagChange(SessionManager.Flag inFlag)
	{
		this.behaviourManager.OnFlagChange(inFlag);
	}

	public virtual float GetBraking()
	{
		return this.mBraking;
	}

	public virtual float GetAcceleration()
	{
		return this.mAcceleration;
	}

	public virtual float GetTopSpeed()
	{
		return 0f;
	}

	public virtual bool HandleMessage(Vehicle inSender, AIMessage.Type inType, object inData)
	{
		return true;
	}

	public virtual void SetBraking(float inValue)
	{
		inValue = Math.Max(inValue, 0f);
		this.mBraking = Math.Max(inValue, this.mBraking);
	}

	public virtual void SetAcceleration(float inValue)
	{
		inValue = Math.Max(inValue, 0f);
		this.mAcceleration = Math.Min(inValue, this.mAcceleration);
	}

	public virtual void SetMaxSpeed(float inValue)
	{
		inValue = Math.Max(inValue, 0f);
		this.mMaxSpeed = Math.Min(inValue, this.mMaxSpeed);
	}

	public void SetPedalState(Vehicle.PedalState inState)
	{
		if (this.mPedalState != inState)
		{
			this.mPedalState = inState;
			this.mPedalStateTimer = 0f;
			if (this.mPedalState == Vehicle.PedalState.Accelerating)
			{
				this.mThrottle.SetValue(1f, AnimatedFloat.Animation.Animate, RandomUtility.GetRandom(0.1f, 0.2f), RandomUtility.GetRandom(4f, 6f), EasingUtility.Easing.OutCubic);
			}
			else if (this.mPedalState == Vehicle.PedalState.Braking)
			{
				this.mThrottle.SetValue(0f, AnimatedFloat.Animation.Animate, 0f, RandomUtility.GetRandom(1f, 2f), EasingUtility.Easing.InSin);
			}
			else if (this.mPedalState == Vehicle.PedalState.Cruising)
			{
			}
		}
	}

	public void MovePosition(Vector3 inPosition)
	{
		this.mPreviousPosition = inPosition;
		this.mTransform.SetPosition(inPosition);
		this.pathController.SetCurrentPath(this.pathController.currentPathType);
		this.mTransform.SetForward(this.pathController.racingLinePosition.forward);
	}

	public virtual void OnEnterPath(PathController.PathType inPath)
	{
		this.mSteeringManager.OnEnterPath();
		this.mSpeedManager.OnEnterPath();
	}

	public virtual bool HasRunWide()
	{
		return Math.Abs(this.pathController.GetCurrentPath().pathSpace) > 1f;
	}

	public virtual bool HasStopped()
	{
		return this.speed < 3f;
	}

	public virtual void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		this.pathState.OnEnterGate(inGateID, inGateType);
		this.steeringManager.OnEnterGate(inGateID, inGateType);
		this.speedManager.OnEnterGate(inGateID, inGateType);
	}

	public virtual void OnEnterStraight(PathData.Straight inStraight)
	{
		this.pathState.OnEnterStraight(inStraight);
		this.steeringManager.OnEnterStraight(inStraight);
		this.speedManager.OnEnterStraight(inStraight);
	}

	public virtual void OnEnterCorner(PathData.Corner inCorner)
	{
		this.pathState.OnEnterCorner(inCorner);
		this.steeringManager.OnEnterCorner(inCorner);
		this.speedManager.OnEnterCorner(inCorner);
	}

	public float GetPathSpaceWidth()
	{
		return this.pathController.GetPathSpaceWidth(this.GetVehicleWidth());
	}

	private float GetVehicleWidth()
	{
		float result = 0f;
		switch (this.championship.series)
		{
		case Championship.Series.SingleSeaterSeries:
			result = VehicleConstants.vehicleWidth;
			break;
		case Championship.Series.GTSeries:
			result = VehicleConstants.vehicleWidthGT;
			break;
		case Championship.Series.EnduranceSeries:
			result = VehicleConstants.vehicleWidthEndurance;
			break;
		}
		return result;
	}

	public float GetVehicleLength()
	{
		float result = 0f;
		switch (this.championship.series)
		{
		case Championship.Series.SingleSeaterSeries:
			result = VehicleConstants.vehicleLength;
			break;
		case Championship.Series.GTSeries:
			result = VehicleConstants.vehicleLengthGT;
			break;
		case Championship.Series.EnduranceSeries:
			result = VehicleConstants.vehicleLengthEndurance;
			break;
		}
		return result;
	}

	public virtual bool CanPassVehicle(Vehicle inVehicle)
	{
		return true;
	}

	public virtual bool IsLightOn()
	{
		return false;
	}

	public SessionPerformance performance
	{
		get
		{
			return this.mPerformance;
		}
	}

	public PathStateManager pathState
	{
		get
		{
			return this.mPathStateManager;
		}
	}

	public SpeedManager speedManager
	{
		get
		{
			return this.mSpeedManager;
		}
	}

	public SteeringManager steeringManager
	{
		get
		{
			return this.mSteeringManager;
		}
	}

	public PathController pathController
	{
		get
		{
			return this.mPathController;
		}
	}

	public PathTransform transform
	{
		get
		{
			return this.mTransform;
		}
	}

	public Vector3 velocity
	{
		get
		{
			return this.mVelocity;
		}
		set
		{
			this.mVelocity = value;
		}
	}

	public Vehicle.PedalState pedalState
	{
		get
		{
			return this.mPedalState;
		}
	}

	public AnimatedFloat throttle
	{
		get
		{
			return this.mThrottle;
		}
	}

	public UnityVehicle unityVehicle
	{
		get
		{
			return this.mUnityVehicle;
		}
		set
		{
			this.mUnityVehicle = value;
		}
	}

	public CollisionBounds collisionBounds
	{
		get
		{
			return this.mCollisionBounds;
		}
	}

	public Transform unityTransform
	{
		get
		{
			return this.mUnityVehicle.transform;
		}
	}

	public UnityVehicleManager unityVehicleManager
	{
		get
		{
			return this.mUnityVehicleManager;
		}
		set
		{
			this.mUnityVehicleManager = value;
		}
	}

	public float braking
	{
		get
		{
			return this.mBraking;
		}
	}

	public float acceleration
	{
		get
		{
			return this.mAcceleration;
		}
	}

	public AIBehaviourStateManager behaviourManager
	{
		get
		{
			return this.mBehaviourManager;
		}
	}

	public Championship championship
	{
		get
		{
			return this.mChampionship;
		}
		set
		{
			this.mChampionship = value;
		}
	}

	public Action OnLapEnd;

	public int id;

	public string name = string.Empty;

	public float speed;

	public bool enabled;

	public bool movementEnabled = true;

	public float currentAcceleration;

	public float currentBraking;

	protected PathStateManager mPathStateManager = new PathStateManager();

	protected AIBehaviourStateManager mBehaviourManager = new AIBehaviourStateManager();

	protected SpeedManager mSpeedManager = new SpeedManager();

	protected SteeringManager mSteeringManager = new SteeringManager();

	protected PathController mPathController = new PathController();

	protected SessionPerformance mPerformance = new SessionPerformance();

	protected PathTransform mTransform = new PathTransform();

	protected CollisionBounds mCollisionBounds = new CollisionBounds();

	protected Vector3 mVelocity = Vector3.zero;

	protected Vehicle.PedalState mPedalState;

	protected AnimatedFloat mThrottle = new AnimatedFloat();

	protected float mPedalStateTimer;

	protected float mBraking;

	protected float mAcceleration = float.MaxValue;

	protected float mMaxSpeed = float.MaxValue;

	protected float mCollisionCooldown;

	protected Vector3 mPreviousPosition = Vector3.zero;

	private Championship mChampionship;

	[NonSerialized]
	protected UnityVehicle mUnityVehicle;

	[NonSerialized]
	protected UnityVehicleManager mUnityVehicleManager;

	public enum PedalState
	{
		Braking,
		Accelerating,
		Cruising
	}
}
