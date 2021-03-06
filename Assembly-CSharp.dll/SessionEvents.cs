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
		// removed points calculation, now chance based
	}

	public bool IsReadyTo(SessionEvents.EventType inEventType)
	{
		switch (inEventType)
		{
			case SessionEvents.EventType.Crash:
				return this.IsReadyToCrash();
			case SessionEvents.EventType.SpinOut:
				return this.IsReadyToSpin();
			case SessionEvents.EventType.LockUp:
				return false;//this.IsReadyToLockUp();
		}
		return false;
	}
	
	private bool IsReadyToSpin() {

		float spinChance = 0.0005f;
		
		Weather.RainType rainType = Game.instance.sessionManager.currentSessionWeather.GetCurrentWeather().rainType;
		Weather.WaterLevel trackWaterType = Game.instance.sessionManager.currentSessionWeather.waterLevel;
		Weather.RubberLevel trackRubberType = Game.instance.sessionManager.currentSessionWeather.rubberLevel;

		// ===============================
		// driver perks
		// ===============================

		// hot Head drivers have +20% spin chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {287}))
			spinChance *= 1.2f;

		// rockSolid drivers have -10% spin chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {44}))
			spinChance *= 0.9f;

		// slow Reactions drivers have +20% spin chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {83}))
			spinChance *= 1.2f;
		// lightning Reactions drivers have -20% spin chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {82}))
			spinChance *= 0.8f;

		// ===============================
		// rubber type
		// ===============================
		
		float rubberModifier = 1f;
		
		switch (trackRubberType) {
			case Weather.RubberLevel.Low:
				rubberModifier = 0.9f;
				break;
			case Weather.RubberLevel.Medium:
				rubberModifier = 0.8f;
				break;
			case Weather.RubberLevel.High:
				rubberModifier = 0.7f;
				break;
			default:
				rubberModifier = 1f;
				break;
		}

		spinChance *= rubberModifier;

		// ===============================
		// weather type
		// ===============================
		
		float weatherModifier = 0f;
		
		switch (rainType) {
			case Weather.RainType.Light:
				weatherModifier = 0.1f;
				break;
			case Weather.RainType.Medium:
				weatherModifier = 0.25f;
				break;
			case Weather.RainType.Heavy:
				weatherModifier = 1f;
				break;
			case Weather.RainType.Monsooon:
				weatherModifier = 5f;
				break;
			default:
				weatherModifier = -0.25f;
				break;
		}
		// weatherPro driver have +50% rain modifier (only for bad/positiv modifiers)
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {73}) && weatherModifier > 0f)
			weatherModifier *= 1.5f;
		// weatherStruggler driver have half rain modifier (only for bad/positiv modifiers)
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {74}) && weatherModifier > 0f)
			weatherModifier *= 0.5f;
		
		spinChance *= (1f + weatherModifier);

		// ===============================
		// tyre stats
		// ===============================
		
		float tyreStatsModifier = 1f;
		
		// +100% for every tyre type level below recommended tyre type
		float diffCurTyresToRecTyres = SessionStrategy.GetRecommendedTreadRightNow() - this.mVehicle.setup.currentSetup.tyreSet.GetTread();
		if (diffCurTyresToRecTyres > 0) {
			tyreStatsModifier *= (1f + diffCurTyresToRecTyres);
		}
		
		// change for tyre condition
		float tyreCondition = this.mVehicle.setup.currentSetup.tyreSet.GetCondition();
		if (tyreCondition < 0.0f) {
			// +100% for 0 condition
			tyreStatsModifier *= 2f;
		}
		else if (tyreCondition < 0.25f) {
			// +25% between 0% and 25% condition
			tyreStatsModifier *= 1.25f;
		}
		else if (tyreCondition < 0.5f) {
			// +10% between 25% and 50% condition
			tyreStatsModifier *= 1.1f;
		}
		
		spinChance *= tyreStatsModifier;
		
		// ===============================
		// modifier driving style
		// ===============================
		
		float drivingStyleModifier = 1f;
		
		switch (this.mVehicle.performance.drivingStyleMode) {
			case DrivingStyle.Mode.Attack:
				drivingStyleModifier = 1.5f;
				break;
			case DrivingStyle.Mode.Push:
				drivingStyleModifier = 1.25f;
				break;
			default:
				drivingStyleModifier = 1f;
				break;
		}
		
		spinChance *= drivingStyleModifier;

		// ===============================
		// modifier driver stats
		// ===============================

		float driverStatsModifier = 1f;

		// modifier between -/+ 50% depending on smoothness statt (+0% with 10 smoothness)
		driverStatsModifier *= 1.5f - this.mDriverStats.smoothness / 20f;
		// modifier between -/+ 50% depending on cornering statt (+0% with 10 cornering)
		driverStatsModifier *= 1.5f - this.mDriverStats.cornering / 20f;

		// up to +50% for last 30% of race, depending on fitness
		if (Game.instance.sessionManager.GetNormalizedSessionTime() > 0.7f)
		{
			driverStatsModifier *= 1.5f - (0.5f * this.mDriverStats.fitness / 20f);
		}

		spinChance *= driverStatsModifier;

		// ===============================
		// modifier behaviour type
		// ===============================
		
		float behaviourTypeModifier = 0f;
		
		switch (this.mVehicle.behaviourManager.currentBehaviour.behaviourType) {
			case AIBehaviourStateManager.Behaviour.Overtaking:
				if (rainType == Weather.RainType.Heavy || rainType == Weather.RainType.Monsooon)
					behaviourTypeModifier = 0.5f;
				else
					behaviourTypeModifier = 0.25f;
				// reduce modifier if ahead is letting this car through (or should be)
				if (this.mVehicle.ahead.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.BlueFlag)
					behaviourTypeModifier *= 0.5f;
				break;
			case AIBehaviourStateManager.Behaviour.Defending:
				behaviourTypeModifier = 0.1f;
				break;
			case AIBehaviourStateManager.Behaviour.SafetyFlag:
				behaviourTypeModifier = -0.5f;
				break;
			case AIBehaviourStateManager.Behaviour.SafetyCar:
				behaviourTypeModifier = -0.9f;
				break;
			default:
				behaviourTypeModifier = 0f;
				break;
		}
		
		spinChance *= (1f + behaviourTypeModifier);
		
		// ===============================
		// modifier vehicle data
		// ===============================
		
		float vehicleDataModifier = 1f;
		
		if (this.mVehicle.pathController.currentPathType != PathController.PathType.Track)
			vehicleDataModifier = 0f;
		else {
			// check if duel (at least one other car below 1 second and without BlueFlag)
			bool duelCheck = (this.mVehicle.ahead != null && this.mVehicle.timer.gapToAhead < 1f && this.mVehicle.ahead.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.BlueFlag)
				|| (this.mVehicle.timer.gapToBehind < 1f && this.mVehicle.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.BlueFlag);
			if (duelCheck) {
				if (this.mVehicle.timer.gapToAhead < 1f) {
					// if duel with car ahead track water matters
					switch (trackWaterType) {
						case Weather.WaterLevel.Wet:
							weatherModifier = 1.15f;
							break;
						case Weather.WaterLevel.Soaked:
							weatherModifier = 1.175f;
							break;
						default:
							weatherModifier = 1.1f;
							break;
					}
				}
				else {
					vehicleDataModifier = 1.1f;
				}
			}
		}
		
		spinChance *= vehicleDataModifier;

		// ===============================
		// final crash chance
		// ===============================

		return (RandomUtility.GetRandom01() < spinChance);

	}
		
	private bool IsReadyToLockUp() {
		float lockUpChance = this.baseChanceLockUp;
		return RandomUtility.GetRandom01() < lockUpChance;
	}
	
	private bool IsReadyToCrash() {
		
		float crashChance = 0.0004f;

		Weather.RainType rainType = Game.instance.sessionManager.currentSessionWeather.GetCurrentWeather().rainType;
		Weather.WaterLevel trackWaterType = Game.instance.sessionManager.currentSessionWeather.waterLevel;
		Weather.RubberLevel trackRubberType = Game.instance.sessionManager.currentSessionWeather.rubberLevel;

		// ===============================
		// driver perks
		// ===============================

		// crashHappy drivers have double crash chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {43}))
			crashChance *= 1.75f;
		// rockSolid drivers have half crash chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {44}))
			crashChance *= 0.5f;

		// slow Reactions drivers have +20% crash chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {83}))
			crashChance *= 1.2f;
		// lightning Reactions drivers have -20% crash chance
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {82}))
			crashChance *= 0.8f;

		// ===============================
		// rubber type
		// ===============================
		
		float rubberModifier = 1f;
		
		switch (trackRubberType) {
			case Weather.RubberLevel.Low:
				rubberModifier = 0.9f;
				break;
			case Weather.RubberLevel.Medium:
				rubberModifier = 0.8f;
				break;
			case Weather.RubberLevel.High:
				rubberModifier = 0.7f;
				break;
			default:
				rubberModifier = 1f;
				break;
		}

		crashChance *= rubberModifier;

		// ===============================
		// weather type
		// ===============================
		
		float weatherModifier = 0f;
		
		switch (rainType) {
			case Weather.RainType.Light:
				weatherModifier = 0.1f;
				break;
			case Weather.RainType.Medium:
				weatherModifier = 0.25f;
				break;
			case Weather.RainType.Heavy:
				weatherModifier = 1f;
				break;
			case Weather.RainType.Monsooon:
				weatherModifier = 5f;
				break;
			default:
				weatherModifier = 0f;
				break;
		}
		// weatherPro driver have +50% rain modifier
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {73}))
			weatherModifier *= 1.5f;
		// weatherStruggler driver have half rain modifier
		if (this.mVehicle.driver.personalityTraitController.HasTrait(false, new int[] {74}))
			weatherModifier *= 0.5f;
		
		crashChance *= (1f + weatherModifier);

		// ===============================
		// tyre stats
		// ===============================
		
		float tyreStatsModifier = 1f;
		
		// +100% for every tyre type level below recommended tyre type
		float diffCurTyresToRecTyres = SessionStrategy.GetRecommendedTreadRightNow() - this.mVehicle.setup.currentSetup.tyreSet.GetTread();
		if (diffCurTyresToRecTyres > 0) {
			tyreStatsModifier *= (1f + diffCurTyresToRecTyres);
		}
		
		// change for tyre condition
		float tyreCondition = this.mVehicle.setup.currentSetup.tyreSet.GetCondition();
		if (tyreCondition < 0.0f) {
			// +100% for 0 condition
			tyreStatsModifier *= 2f;
		}
		else if (tyreCondition < 0.25f) {
			// +25% between 0% and 25% condition
			tyreStatsModifier *= 1.25f;
		}
		else if (tyreCondition < 0.5f) {
			// +10% between 25% and 50% condition
			tyreStatsModifier *= 1.1f;
		}
		
		crashChance *= tyreStatsModifier;
	
		// ===============================
		// modifier driving style
		// ===============================
		
		float drivingStyleModifier = 1f;
		
		switch (this.mVehicle.performance.drivingStyleMode) {
			case DrivingStyle.Mode.Attack:
				drivingStyleModifier = 1.5f;
				break;
			case DrivingStyle.Mode.Push:
				drivingStyleModifier = 1.25f;
				break;
			case DrivingStyle.Mode.Neutral:
				drivingStyleModifier = 0.95f;
				break;
			case DrivingStyle.Mode.Conserve:
				drivingStyleModifier = 0.75f;
				break;
			case DrivingStyle.Mode.BackUp:
				drivingStyleModifier = 0.5f;
				break;
		}
		
		crashChance *= drivingStyleModifier;

		// ===============================
		// modifier driver stats
		// ===============================
		
		float driverStatsModifier = 1f;
		
		// modifier between -/+ 50% depending on focus statt (+0% with 10 focus)
		driverStatsModifier *= 1.5f - this.mDriverStats.focus / 20f;
		
		// up to +50% for last 30% of race, depending on fitness
		if (Game.instance.sessionManager.GetNormalizedSessionTime() > 0.7f)
		{
			driverStatsModifier *= 1.5f - (0.5f * this.mDriverStats.fitness / 20f);
		}
		
		// up to +10% depending on braking
		driverStatsModifier *= 1.1f - (0.1f * this.mDriverStats.braking / 20f);
		
		crashChance *= driverStatsModifier;

		// ===============================
		// modifier behaviour type
		// ===============================
		
		float behaviourTypeModifier = 0f;
		
		switch (this.mVehicle.behaviourManager.currentBehaviour.behaviourType) {
			case AIBehaviourStateManager.Behaviour.Overtaking:
				if (rainType == Weather.RainType.Heavy || rainType == Weather.RainType.Monsooon)
					behaviourTypeModifier = 0.5f;
				else
					behaviourTypeModifier = 0.25f;
				// reduce modifier if ahead is letting this car through (or should be)
				if (this.mVehicle.ahead.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.BlueFlag)
					behaviourTypeModifier *= 0.5f;
				break;
			case AIBehaviourStateManager.Behaviour.Defending:
				behaviourTypeModifier = 0.1f;
				break;
			case AIBehaviourStateManager.Behaviour.SafetyFlag:
				behaviourTypeModifier = -0.5f;
				break;
			case AIBehaviourStateManager.Behaviour.SafetyCar:
				behaviourTypeModifier = -0.9f;
				break;
			default:
				behaviourTypeModifier = 0f;
				break;
		}
		
		crashChance *= (1f + behaviourTypeModifier);
		
		// ===============================
		// modifier vehicle data
		// ===============================
		
		float vehicleDataModifier = 1f;
		
		if (this.mVehicle.pathController.currentPathType != PathController.PathType.Track)
			vehicleDataModifier = 0f;
		else {
			// check if duel (at least one other car below 1 second and without BlueFlag)
			bool duelCheck = (this.mVehicle.ahead != null && this.mVehicle.timer.gapToAhead < 1f && this.mVehicle.ahead.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.BlueFlag)
				|| (this.mVehicle.timer.gapToBehind < 1f && this.mVehicle.behaviourManager.currentBehaviour.behaviourType != AIBehaviourStateManager.Behaviour.BlueFlag);
			if (duelCheck) {
				if (this.mVehicle.timer.gapToAhead < 1f) {
					// if duel with car ahead track water matters
					switch (trackWaterType) {
						case Weather.WaterLevel.Wet:
							weatherModifier = 1.15f;
							break;
						case Weather.WaterLevel.Soaked:
							weatherModifier = 1.175f;
							break;
						default:
							weatherModifier = 1.1f;
							break;
					}
				}
				else {
					vehicleDataModifier = 1.1f;
				}
			}
		}
		
		crashChance *= vehicleDataModifier;

		// ===============================
		// final crash chance
		// ===============================

		return (RandomUtility.GetRandom01() < crashChance);
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
	
	private float baseChanceCrash = 0.05f;
	private float baseChanceSpin = 0.5f;
	private float baseChanceLockUp = 0.5f;

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
