using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class SessionEvents : InstanceCounter
{
	public SessionEvents()
	{
	}

	public void Start(RacingVehicle inVehicle)
	{
		this.mVehicle = inVehicle;
		this.mDriverStats = this.mVehicle.driver.GetDriverStats();
	}

	public void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		if (inGateID == this.mPreviousGate)
		{
			return;
		}
		this.mPreviousGate = inGateID;
		this.UpdatePoints();
	}

	private void UpdatePoints()
	{
		if (this.mDriverStats == null)
		{
			this.mDriverStats = this.mVehicle.driver.GetDriverStats();
		}
		for (SessionEvents.EventType eventType = SessionEvents.EventType.Crash; eventType < SessionEvents.EventType.Count; eventType++)
		{
			float num = 0f;
			float num2 = SessionEvents.GetPointsForWeatherType(eventType, Game.instance.sessionManager.currentSessionWeather);
			num += num2;
			this.mPointsPerType[(int)eventType, 0] += num2;
			num2 = SessionEvents.GetPointsForTyreStats(eventType, this.mVehicle);
			num += num2;
			this.mPointsPerType[(int)eventType, 1] += num2;
			num2 = SessionEvents.GetPointsForDrivingSytle(eventType, this.mVehicle);
			num += num2;
			this.mPointsPerType[(int)eventType, 2] += num2;
			num2 = SessionEvents.GetPointsForDriverStats(eventType, this.mDriverStats, Game.instance.sessionManager.GetNormalizedSessionTime() > 0.7f);
			num += num2;
			this.mPointsPerType[(int)eventType, 3] += num2;
			num2 = SessionEvents.GetPointsForEngineMode(eventType, this.mVehicle);
			num += num2;
			this.mPointsPerType[(int)eventType, 4] += num2;
			num2 = SessionEvents.GetPointsForBehaviourType(eventType, this.mVehicle);
			num += num2;
			this.mPointsPerType[(int)eventType, 5] += num2;
			num2 = SessionEvents.GetPointsForVehicleData(eventType, this.mVehicle);
			num += num2;
			this.mPointsPerType[(int)eventType, 6] += num2;
			switch (eventType)
			{
			case SessionEvents.EventType.Crash:
				this.mCrashPoints += num;
				this.mCrashPoints = Mathf.Max(this.mCrashPoints, 0f);
				break;
			case SessionEvents.EventType.SpinOut:
				this.mSpinOutPoints += num;
				this.mSpinOutPoints = Mathf.Max(this.mSpinOutPoints, 0f);
				break;
			case SessionEvents.EventType.LockUp:
				this.mLockUpPoints += num;
				this.mLockUpPoints = Mathf.Max(this.mLockUpPoints, 0f);
				break;
			}
		}
	}

	private void LogReport(SessionEvents.EventType inType)
	{
		string text = inType.ToString() + " {" + this.mVehicle.driver.lastName + "} : ";
		for (SessionEvents.PointsType pointsType = SessionEvents.PointsType.Weather; pointsType < SessionEvents.PointsType.Count; pointsType++)
		{
			string text2 = text;
			text = string.Concat(new object[]
			{
				text2,
				pointsType.ToString(),
				": ",
				this.mPointsPerType[(int)inType, (int)pointsType],
				" | "
			});
			this.mPointsPerType[(int)inType, (int)pointsType] = 0f;
		}
		global::Debug.LogFormat("{0}", new object[]
		{
			text
		});
	}

	public void SetPoints(SessionEvents.EventType inEventType, int inPoints)
	{
		switch (inEventType)
		{
		default:
			this.mCrashPoints = (float)inPoints;
			break;
		case SessionEvents.EventType.SpinOut:
			this.mSpinOutPoints = (float)inPoints;
			break;
		case SessionEvents.EventType.LockUp:
			this.mLockUpPoints = (float)inPoints;
			break;
		}
	}

	public void ResetAllPoints()
	{
		this.mCrashPoints = 0f;
		this.mLockUpPoints = 0f;
		this.mSpinOutPoints = 0f;
	}

	public void EventActivated(SessionEvents.EventType inEventType)
	{
		switch (inEventType)
		{
		default:
			this.mCrashPoints = 0f;
			break;
		case SessionEvents.EventType.SpinOut:
			this.mSpinOutPoints = 0f;
			break;
		case SessionEvents.EventType.LockUp:
			this.mLockUpPoints = 0f;
			this.mLockUpPointsLimit += this.mLockUpPointsLimit;
			break;
		}
	}

	public bool IsReadyTo(SessionEvents.EventType inEventType)
	{
		switch (inEventType)
		{
		default:
			return this.mCrashPoints > ((!CrashDirector.HasTeamMateRetired(this.mVehicle)) ? this.mCrashPointsLimit : (this.mCrashPointsLimit * 2f));
		case SessionEvents.EventType.SpinOut:
			return this.mSpinOutPoints > this.mSpinOutPointsLimit;
		case SessionEvents.EventType.LockUp:
			return this.mLockUpPoints > this.mLockUpPointsLimit;
		}
	}

	private static float GetPointsForVehicleData(SessionEvents.EventType inEventType, RacingVehicle inVehicle)
	{
		bool flag = inVehicle.championship.series == Championship.Series.GTSeries;
		float num = 0f;
		if (inVehicle.pathController.currentPathType != PathController.PathType.Track)
		{
			return -0.5f;
		}
		if (inVehicle.timer.gapToLeader > 0f)
		{
			if (inVehicle.timer.gapToAhead < 2f)
			{
				num += 0.25f;
			}
		}
		else
		{
			num += 0.25f;
		}
		if (inVehicle.timer.gapToBehind < 2f)
		{
			num += 0.25f;
		}
		if (inEventType != SessionEvents.EventType.Crash && inVehicle.speed < 50f)
		{
			num -= 1f;
		}
		if (flag)
		{
			num *= 0.2f;
		}
		return num;
	}

	private static float GetPointsForWeatherType(SessionEvents.EventType inEventType, SessionWeatherDetails inWeatherDetails)
	{
		float num = 0f;
		Weather cachedCurrentWeather = inWeatherDetails.GetCachedCurrentWeather();
		switch (cachedCurrentWeather.rainType)
		{
		case Weather.RainType.None:
			num = -2f;
			break;
		case Weather.RainType.Light:
			num = 0.25f;
			break;
		case Weather.RainType.Medium:
			num = 0.5f;
			break;
		case Weather.RainType.Heavy:
		case Weather.RainType.Monsooon:
			num = 1f;
			break;
		}
		num += inWeatherDetails.GetNormalizedTrackWater();
		if (inEventType == SessionEvents.EventType.LockUp && num > 0f)
		{
			num *= 0.5f;
		}
		return num;
	}

	private static float GetPointsForTyreStats(SessionEvents.EventType inEventType, RacingVehicle inVehicle)
	{
		float num = 0f;
		TyreSet.Tread recommendedTreadRightNow = SessionStrategy.GetRecommendedTreadRightNow();
		TyreSet.Tread tread = inVehicle.setup.currentSetup.tyreSet.GetTread();
		if (tread < recommendedTreadRightNow)
		{
			int num2 = recommendedTreadRightNow - tread;
			if (num2 != 0)
			{
				num += (float)num2;
			}
			else
			{
				num -= 1f;
			}
		}
		float condition = inVehicle.setup.currentSetup.tyreSet.GetCondition();
		num += 2f - condition;
		if (inEventType == SessionEvents.EventType.LockUp && tread > TyreSet.Tread.Slick && num > 0f)
		{
			num *= 0.25f;
		}
		return num;
	}

	private static float GetPointsForDrivingSytle(SessionEvents.EventType inEventType, RacingVehicle inVehicle)
	{
		float num = 0f;
		if (inVehicle.pathController.currentPathType != PathController.PathType.Track)
		{
			return -0.1f;
		}
		switch (inVehicle.performance.drivingStyleMode)
		{
		case DrivingStyle.Mode.Attack:
			num = 0.5f;
			break;
		case DrivingStyle.Mode.Push:
			num = 0.25f;
			break;
		case DrivingStyle.Mode.Neutral:
			num -= 0.05f;
			break;
		case DrivingStyle.Mode.Conserve:
			num -= 0.25f;
			break;
		case DrivingStyle.Mode.BackUp:
			num -= 0.5f;
			break;
		}
		return num;
	}

	private static float GetPointsForEngineMode(SessionEvents.EventType inEventType, RacingVehicle inVehicle)
	{
		float num = 0f;
		if (inVehicle.pathController.currentPathType != PathController.PathType.Track)
		{
			return -0.1f;
		}
		switch (inVehicle.performance.fuel.engineMode)
		{
		case Fuel.EngineMode.SuperOvertake:
			num = 1f;
			break;
		case Fuel.EngineMode.Overtake:
			num = 0.5f;
			break;
		case Fuel.EngineMode.High:
			num = 0.25f;
			break;
		case Fuel.EngineMode.Medium:
			num = 0f;
			break;
		case Fuel.EngineMode.Low:
			num -= 1f;
			break;
		}
		return num;
	}

	private static float GetPointsForDriverStats(SessionEvents.EventType inEventType, DriverStats inStats, bool addFitness)
	{
		float num = 0f;
		num += 0.5f - inStats.focus / 20f;
		if (addFitness)
		{
			num += 0.5f - inStats.fitness / 20f;
		}
		return num * 0.5f;
	}

	private static float GetPointsForBehaviourType(SessionEvents.EventType inEventType, RacingVehicle inVehicle)
	{
		bool flag = inVehicle.championship.series == Championship.Series.GTSeries;
		float num = 0f;
		switch (inEventType)
		{
		case SessionEvents.EventType.Crash:
		case SessionEvents.EventType.SpinOut:
		case SessionEvents.EventType.LockUp:
		{
			AIBehaviourStateManager.Behaviour behaviourType = inVehicle.behaviourManager.currentBehaviour.behaviourType;
			switch (behaviourType)
			{
			case AIBehaviourStateManager.Behaviour.Racing:
				num -= 0.2f;
				break;
			case AIBehaviourStateManager.Behaviour.Overtaking:
				num = 5f;
				break;
			case AIBehaviourStateManager.Behaviour.Defending:
				num = 4f;
				break;
			default:
				if (behaviourType == AIBehaviourStateManager.Behaviour.SafetyFlag)
				{
					num -= 10f;
				}
				break;
			}
			break;
		}
		}
		if (Game.instance.sessionManager.flag != SessionManager.Flag.Green)
		{
			num -= 10f;
		}
		if (flag)
		{
			num *= 0.75f;
		}
		return num;
	}

	public float crashPoints
	{
		get
		{
			return this.mCrashPoints;
		}
	}

	public float spinOutPoints
	{
		get
		{
			return this.mSpinOutPoints;
		}
	}

	public float lockUpPoints
	{
		get
		{
			return this.mLockUpPoints;
		}
	}

	private float mCrashPoints;

	private float mCrashPointsLimit = 1000f;

	private float mSpinOutPoints;

	private float mSpinOutPointsLimit = 1000f;

	private float mLockUpPoints;

	private float mLockUpPointsLimit = 1000f;

	private RacingVehicle mVehicle;

	private DriverStats mDriverStats;

	private int mPreviousGate;

	private float[,] mPointsPerType = new float[3, 7];

	public enum EventType
	{
		Crash,
		SpinOut,
		LockUp,
		Count
	}

	public enum PointsType
	{
		Weather,
		Tyre,
		DrivingStyle,
		DriverStats,
		EngineMode,
		AIBehaviourType,
		ClosestVehiclesPressure,
		Count
	}
}
