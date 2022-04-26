using System;
using System.Text;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class TyreSet
{
	public TyreSet()
	{
	}

	public void Start(RacingVehicle inVehicle)
	{
		this.mVehicle = inVehicle;
		this.mCondition = 1f;
		this.SetToTyreBlanketTemperature();
		this.SetTyreDesignData();
	}

	public void SetTyreDesignData()
	{
		CarManager carManager = this.mVehicle.driver.contract.GetTeam().carManager;
		float carChassisStatValueOnGrid = carManager.GetCarChassisStatValueOnGrid(CarChassisStats.Stats.TyreHeating, CarManager.MedianTypes.Highest);
		float carChassisStatValueOnGrid2 = carManager.GetCarChassisStatValueOnGrid(CarChassisStats.Stats.TyreHeating, CarManager.MedianTypes.Lowest);
		float stat = this.mVehicle.car.ChassisStats.GetStat(CarChassisStats.Stats.TyreHeating, true, this.mVehicle.car);
		float num = Mathf.Clamp01((stat - carChassisStatValueOnGrid2) / (carChassisStatValueOnGrid - carChassisStatValueOnGrid2));
		this.mTyreHeatingChassisStatImpact = 0f;
		if (num < 0.33f)
		{
			this.mTyreHeatingChassisStatImpact = (float)(-(float)DesignDataManager.instance.GetDesignData().carChassis.tyreHeatingTimeBonusInMinutes);
		}
		else if (num > 0.66f)
		{
			this.mTyreHeatingChassisStatImpact = (float)DesignDataManager.instance.GetDesignData().carChassis.tyreHeatingTimeBonusInMinutes;
		}
		this.mCompoundDesignData = DesignDataManager.instance.tyreData.GetCompoundData(this.GetCompound());
		this.mTyreDesignData = DesignDataManager.instance.tyreData;
	}

	public void SimulationUpdate()
	{
		float num = this.mTemperature;
		this.UpdateTyreDrivingStyleTemperature();
		this.UpdateWeatherImpactOnTyreTemperature();
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race && this.mVehicle.isPlayerDriver)
		{
			if (num < 1f && this.mTemperature >= 1f)
			{
				Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.TyresOverheating, this.mVehicle);
			}
			if (num > 0f && this.mTemperature <= 0f)
			{
				Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.TyresUnderheating, this.mVehicle);
			}
		}
		if (MathsUtility.ApproximatelyZero(this.GetCondition()))
		{
			this.mPunctureTimer += GameTimer.simulationDeltaTime;
			if (this.mPunctureTimer > this.mPunctureDuration)
			{
				if (!this.mIsPunctured)
				{
					this.mVehicle.timer.currentLap.AddEvent(LapDetailsData.LapEvents.Puncture);
				}
				this.mIsPunctured = true;
			}
		}
		if (this.mHasLooseWheel && !this.mWheelLost)
		{
			this.mCurrentLooseWheelDetachedTimer += GameTimer.simulationDeltaTime;
			if (this.mCurrentLooseWheelDetachedTimer >= this.mLooseWheelDetachedTimer && !this.mWheelLost)
			{
				this.mWheelLost = true;
				this.mVehicle.pathController.GetPath(PathController.PathType.RunWideLane).GetClosestPath();
				this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().ClearTarget();
				this.mVehicle.steeringManager.GetBehaviour<TargetPointSteeringBehaviour>().SetTargetPath(PathController.PathType.RunWideLane, false);
			}
		}
		if (this.mWheelLost && !this.mHasRanWide && this.mVehicle.behaviourManager.currentBehaviour.behaviourType == AIBehaviourStateManager.Behaviour.RuningWide)
		{
			this.mHasRanWide = true;
			this.mVehicle.unityVehicle.ActivateDamagedTyre(this.mTargetLooseTyreSlot);
			this.mLapWhenLostWheel = this.mVehicle.timer.lap;
			Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.CatastrophicLooseWheel, this.mVehicle);
		}
		if (this.mVehicle.isPlayerDriver && this.mLapWhenLostWheel != -1 && this.mVehicle.timer.lap == this.mLapWhenLostWheel + 1 && !this.mSendLooseWheelReminder)
		{
			this.mSendLooseWheelReminder = true;
			this.mVehicle.teamRadio.GetMessage<RadioMessageRunWide>().SendLooseWheelReminder();
		}
	}

	public void ApplyTyreWear(float inWearDistance)
	{
		inWearDistance = RandomUtility.GetRandom(0.99f, 1.01f) * inWearDistance;
		if (this.mHasWrongCompoundFitted)
		{
			if (this.mTyreDesignData == null)
			{
				this.mTyreDesignData = DesignDataManager.instance.tyreData;
			}
			inWearDistance += this.mTyreDesignData.wrongTyreCompoundTyreWearCost;
		}
		float num = this.mCondition - inWearDistance / this.GetMaxDistance();
		this.SetCondition(num);
		if (num <= 0f)
		{
			this.mVehicle.sessionData.tyresRunOut = true;
		}
	}

	public void ApplyTyreWearFromLockUp(float inWear)
	{
		float condition = this.mCondition - inWear;
		this.SetCondition(condition);
	}

	public void SetCondition(float inCondition)
	{
		float num = this.mCondition;
		this.mCondition = Mathf.Clamp01(inCondition);
		if (this.mVehicle != null && this.mVehicle.isPlayerDriver)
		{
			float num2 = App.instance.preferencesManager.superSpeedPreferences.TyreWearThreshold();
			if (num > num2 && this.mCondition <= num2)
			{
				Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.TyreWearLow, this.mVehicle);
			}
		}
	}

	public float GetTimeCost()
	{
		float num;
		if (this.mCondition >= 0.5f)
		{
			num = Mathf.Lerp(this.mMediumPerformanceRange.timeCost, this.mHighPerformanceRange.timeCost, this.mCondition / 2f);
		}
		else
		{
			num = Mathf.Lerp(this.mLowPerformanceRange.timeCost, this.mMediumPerformanceRange.timeCost, this.mCondition * 2f);
		}
		float num2 = 1f - this.mVehicle.championship.rules.speedBonusNormalized;
		num += DesignDataManager.instance.tyreData.tyreSupplierSpeedBonusMaxTimeCost * num2;
		num += Mathf.Lerp(0f, 30f, Mathf.Clamp01(this.mPunctureTimer / this.mPunctureDuration));
		if (this.hasWrongCompoundFitted)
		{
			if (this.mTyreDesignData == null)
			{
				this.mTyreDesignData = DesignDataManager.instance.tyreData;
			}
			num += this.mTyreDesignData.wrongTyreCompoundTimeCost;
		}
		if (this.mWheelLost)
		{
			if (this.mTyreDesignData == null)
			{
				this.mTyreDesignData = DesignDataManager.instance.tyreData;
			}
			num += this.mTyreDesignData.lostTyreTimeCost;
		}
		return num;
	}

	public float GetEstimatedTimeCostForDistance(float inDistance)
	{
		float num = 0f;
		float num2 = GameUtility.MilesToMeters(Game.instance.sessionManager.eventDetails.circuit.trackLengthMiles);
		int num3 = Mathf.CeilToInt(inDistance / num2);
		float num4 = this.mCondition;
		for (int i = 0; i < num3; i++)
		{
			num4 = Mathf.Clamp01(num4 - num2 / this.GetMaxDistance());
			float num5 = this.GetMaxDistance() * (1f - num4);
			if (num5 < this.mHighPerformanceRange.maxDistance)
			{
				num += this.mHighPerformanceRange.timeCost;
			}
			else if (num5 < this.mHighPerformanceRange.maxDistance + this.mMediumPerformanceRange.maxDistance)
			{
				num += this.mMediumPerformanceRange.timeCost;
			}
			else
			{
				num += this.mLowPerformanceRange.timeCost;
			}
			if (MathsUtility.ApproximatelyZero(num4) || num4 <= this.GetCliffCondition())
			{
				num += this.GetCliffConditionTimeCost();
			}
		}
		return num;
	}

	public float GetMaxDistance()
	{
		return this.mLowPerformanceRange.maxDistance + this.mMediumPerformanceRange.maxDistance + this.mHighPerformanceRange.maxDistance;
	}

	private void UpdateTyreDrivingStyleTemperature()
	{
		float num = 0f;
		switch (this.mVehicle.performance.drivingStyleMode)
		{
		case DrivingStyle.Mode.Attack:
			num = this.GetIncreaseTemperatureChangeRate();
			break;
		case DrivingStyle.Mode.Push:
			num = this.GetIncreaseTemperatureChangeRate() * 0.5f;
			break;
		case DrivingStyle.Mode.Neutral:
			num = 0f;
			break;
		case DrivingStyle.Mode.Conserve:
			num = this.GetDecreaseTemperatureChangeRate() * 0.5f;
			break;
		case DrivingStyle.Mode.BackUp:
			num = this.GetDecreaseTemperatureChangeRate();
			break;
		}
		bool flag = this.mTyreHeatingChassisStatImpact > 0f;
		float num2 = 0.4f;
		float num3 = 0.6f;
		if (num > 0f)
		{
			if (this.mTemperature < num2 && flag)
			{
				num *= 1.3f;
			}
		}
		else if (num < 0f && this.mTemperature > num3 && flag)
		{
			num *= 1.3f;
		}
		this.SetTemperature(this.mTemperature + num * GameTimer.simulationDeltaTime);
	}

	private void UpdateWeatherImpactOnTyreTemperature()
	{
		if (!this.mVehicle.HasStopped())
		{
			SessionWeatherDetails currentSessionWeather = Game.instance.sessionManager.currentSessionWeather;
			Weather currentWeather = currentSessionWeather.GetCurrentWeather();
			float num = 0f;
			int weatherTemperatureGainStart = this.mCompoundDesignData.weatherTemperatureGainStart;
			int weatherTemperatureLossStart = this.mCompoundDesignData.weatherTemperatureLossStart;
			float weatherMinTempDelta = this.mCompoundDesignData.weatherMinTempDelta;
			float weatherUnitChangePerDegree = this.mCompoundDesignData.weatherUnitChangePerDegree;
			float minMaxWeatherTemperatureRateChangeClamp = DesignDataManager.instance.tyreData.minMaxWeatherTemperatureRateChangeClamp;
			if (currentWeather.airTemperature >= weatherTemperatureGainStart)
			{
				num = weatherMinTempDelta;
				num += (float)(currentWeather.airTemperature - weatherTemperatureGainStart) * weatherUnitChangePerDegree;
			}
			else if (currentWeather.airTemperature <= weatherTemperatureLossStart)
			{
				num = -weatherMinTempDelta;
				num -= (float)(weatherTemperatureLossStart - currentWeather.airTemperature) * weatherUnitChangePerDegree;
			}
			num = Mathf.Clamp(num, -minMaxWeatherTemperatureRateChangeClamp, minMaxWeatherTemperatureRateChangeClamp);
			num = num / 60f * GameTimer.simulationDeltaTime;
			this.mAirTempRateChange = num;
			this.SetTemperature(this.mTemperature + num);
		}
	}

	public void SetTemperature(float inTemperature)
	{
		this.mTemperature = Mathf.Clamp(inTemperature, 0f, 1f);
	}

	public void SetToTyreBlanketTemperature()
	{
		if (Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race && this.mVehicle.pathState.stateType == PathStateManager.StateType.Grid)
		{
			this.SetTemperature(0.25f);
		}
		else
		{
			this.SetTemperature(0.5f);
		}
	}

	private float GetIncreaseTemperatureChangeRate()
	{
		return 1f / GameUtility.MinutesToSeconds(this.mCompoundDesignData.temperatureIncreaseTime + this.mTyreHeatingChassisStatImpact);
	}

	private float GetDecreaseTemperatureChangeRate()
	{
		return -(1f / GameUtility.MinutesToSeconds(this.mCompoundDesignData.temperatureDecreaseTime + this.mTyreHeatingChassisStatImpact));
	}

	public float GetCliffCondition()
	{
		return this.mCompoundDesignData.cliffCondition;
	}

	public float GetCliffConditionTimeCost()
	{
		return this.mCompoundDesignData.cliffConditionTimeCost;
	}

	public float GetTyreDistance()
	{
		return this.GetMaxDistance() * this.GetCondition();
	}

	public float GetOptimalTyreDistance()
	{
		float num = 0.20f; // tyre condition for pit stop
		return this.GetMaxDistance() * Mathf.Max(0f, this.GetCondition() - num);
	}

	public float GetWrongTreadForWaterLevelTimeCost()
	{
		return this.mCompoundDesignData.wrongTreadForWaterLevelTimeCost;
	}

	public virtual Color GetColor()
	{
		return Color.white;
	}

	public virtual TyreSet.Compound GetCompound()
	{
		return TyreSet.Compound.SuperSoft;
	}

	public virtual TyreSet.Tread GetTread()
	{
		return TyreSet.Tread.Slick;
	}

	public virtual string GetName()
	{
		return string.Empty;
	}

	public virtual string GetShortName()
	{
		return string.Empty;
	}

	public float GetCondition()
	{
		return this.mCondition;
	}

	public string GetConditionText()
	{
		int value = (int)(this.GetCondition() * 100f + 0.5f);
		string result;
		using (GameUtility.StringBuilderWrapper builderSafe = GameUtility.GlobalStringBuilderPool.GetBuilderSafe())
		{
			StringBuilder stringBuilder = builderSafe.stringBuilder;
			GameUtility.Append(stringBuilder, value, 1);
			stringBuilder.Append('%');
			result = stringBuilder.ToString();
		}
		return result;
	}

	public float GetTemperature()
	{
		return this.mTemperature;
	}

	public Color GetTemperatureColor(Gradient inGradient)
	{
		return inGradient.Evaluate(this.GetTemperature());
	}

	public float GetPerformanceForUI(TyreSet.Tread inDesiredTread)
	{
		switch (inDesiredTread)
		{
		case TyreSet.Tread.Slick:
			switch (this.GetCompound())
			{
			case TyreSet.Compound.SuperSoft:
				return 0.74f;
			case TyreSet.Compound.Soft:
				return 0.68f;
			case TyreSet.Compound.Medium:
				return 0.5f;
			case TyreSet.Compound.Hard:
				return 0.4f;
			case TyreSet.Compound.Intermediate:
				return 0.1f;
			case TyreSet.Compound.Wet:
				return 0.01f;
			case TyreSet.Compound.UltraSoft:
				return 0.9f;
			}
			break;
		case TyreSet.Tread.LightTread:
		{
			TyreSet.Compound compound = this.GetCompound();
			if (compound == TyreSet.Compound.Intermediate)
			{
				return 0.9f;
			}
			if (compound != TyreSet.Compound.Wet)
			{
				return 0.05f;
			}
			return 0.5f;
		}
		case TyreSet.Tread.HeavyTread:
		{
			TyreSet.Compound compound = this.GetCompound();
			if (compound == TyreSet.Compound.Intermediate)
			{
				return 0.4f;
			}
			if (compound != TyreSet.Compound.Wet)
			{
				return 0.05f;
			}
			return 0.9f;
		}
		}
		return 0f;
	}

	public virtual float GetDurabilityForUI()
	{
		return 0f;
	}

	public virtual string GetPreferedConditionsText()
	{
		return "Dry Track";
	}

	public static TyreSet CreateTyreSet(TyreSet.Compound inCompound)
	{
		TyreSet result = null;
		switch (inCompound)
		{
		case TyreSet.Compound.SuperSoft:
			result = new SuperSoftTyreSet();
			break;
		case TyreSet.Compound.Soft:
			result = new SoftTyreSet();
			break;
		case TyreSet.Compound.Medium:
			result = new MediumTyreSet();
			break;
		case TyreSet.Compound.Hard:
			result = new HardTyreSet();
			break;
		case TyreSet.Compound.Intermediate:
			result = new IntermediateTyreSet();
			break;
		case TyreSet.Compound.Wet:
			result = new WetTyreSet();
			break;
		case TyreSet.Compound.UltraSoft:
			result = new UltraSoftTyreSet();
			break;
		}
		return result;
	}

	public static float CalculateLapRangeOfTyre(TyreSet inTyreSet, float lapLength)
	{
		return inTyreSet.GetMaxDistance() / lapLength;
	}

	public bool IsInLowPerformanceRange()
	{
		float num = this.GetMaxDistance() * (1f - this.mCondition);
		return num > this.mHighPerformanceRange.maxDistance + this.mMediumPerformanceRange.maxDistance;
	}

	public void SetWrongCompoundFitted(TyreSet.Compound inCompound, SessionSetupChangeEntry.TyreSlot inTyreSlot)
	{
		this.mHasWrongCompoundFitted = true;
		this.mWrongCompoundFitted = inCompound;
		this.mWrongCompoundTyreSlot = inTyreSlot;
	}

	public void SetLooseWheel(SessionSetupChangeEntry.TyreSlot inLooseTyreSlot)
	{
		this.mHasLooseWheel = true;
		this.mWheelLost = false;
		this.mHasRanWide = false;
		this.mCurrentLooseWheelDetachedTimer = 0f;
		this.mTargetLooseTyreSlot = inLooseTyreSlot;
		if (this.mTyreDesignData == null)
		{
			this.mTyreDesignData = DesignDataManager.instance.tyreData;
		}
		this.mLooseWheelDetachedTimer = RandomUtility.GetRandom(this.mTyreDesignData.minDetachTyreTimer, this.mTyreDesignData.maxDetachTyreTimer);
	}

	public void ResetPitstopCompoundMistakes()
	{
		this.mHasWrongCompoundFitted = false;
		if (this.mHasRanWide)
		{
			this.mVehicle.unityVehicle.ResetAllDamagedWheelEffects();
		}
		if (!this.mVehicle.isPlayerDriver)
		{
			this.mHasRanWide = false;
			this.mHasLooseWheel = false;
			this.mWheelLost = false;
			this.mLapWhenLostWheel = -1;
			this.mSendLooseWheelReminder = false;
		}
		this.mCurrentLooseWheelDetachedTimer = 0f;
	}

	public TyreSetPerformanceRange lowPerformanceRange
	{
		get
		{
			return this.mLowPerformanceRange;
		}
	}

	public TyreSetPerformanceRange mediumPerformanceRange
	{
		get
		{
			return this.mMediumPerformanceRange;
		}
	}

	public TyreSetPerformanceRange highPerformanceRange
	{
		get
		{
			return this.mHighPerformanceRange;
		}
	}

	public bool isPunctured
	{
		get
		{
			return this.mIsPunctured;
		}
	}

	public float airTempRateChange
	{
		get
		{
			return this.mAirTempRateChange;
		}
	}

	public RacingVehicle vehicle
	{
		get
		{
			return this.mVehicle;
		}
	}

	public TyreSet.Compound wrongCompoundFitted
	{
		get
		{
			return this.mWrongCompoundFitted;
		}
	}

	public bool hasWrongCompoundFitted
	{
		get
		{
			return this.mHasWrongCompoundFitted;
		}
	}

	public bool hasLostWheel
	{
		get
		{
			return this.mWheelLost && this.mHasRanWide;
		}
	}

	public SessionSetupChangeEntry.TyreSlot targetLooseTyreSlot
	{
		get
		{
			return this.mTargetLooseTyreSlot;
		}
	}

	public SessionSetupChangeEntry.TyreSlot wrongCompoundTyreSlot
	{
		get
		{
			return this.mWrongCompoundTyreSlot;
		}
	}

	public static readonly int sTreadCount = 3;

	private TyreSetPerformanceRange mLowPerformanceRange = new TyreSetPerformanceRange();

	private TyreSetPerformanceRange mMediumPerformanceRange = new TyreSetPerformanceRange();

	private TyreSetPerformanceRange mHighPerformanceRange = new TyreSetPerformanceRange();

	private float mCondition;

	private float mTemperature;

	private float mAirTempRateChange;

	private float mPunctureTimer;

	private float mPunctureDuration = 60f;

	private bool mIsPunctured;

	private float mTyreHeatingChassisStatImpact;

	private RacingVehicle mVehicle;

	private TyreCompoundDesignData mCompoundDesignData;

	private TyreDesignData mTyreDesignData;

	private bool mHasWrongCompoundFitted;

	private TyreSet.Compound mWrongCompoundFitted = TyreSet.Compound.UltraSoft;

	private SessionSetupChangeEntry.TyreSlot mWrongCompoundTyreSlot = SessionSetupChangeEntry.TyreSlot.BackLeft;

	private bool mHasLooseWheel;

	private bool mWheelLost;

	private bool mHasRanWide;

	private float mLooseWheelDetachedTimer = 15f;

	private float mCurrentLooseWheelDetachedTimer;

	private SessionSetupChangeEntry.TyreSlot mTargetLooseTyreSlot = SessionSetupChangeEntry.TyreSlot.BackLeft;

	private int mLapWhenLostWheel = -1;

	private bool mSendLooseWheelReminder;

	public enum Compound
	{
		[LocalisationID("PSG_10000472")]
		SuperSoft,
		[LocalisationID("PSG_10000473")]
		Soft,
		[LocalisationID("PSG_10000467")]
		Medium,
		[LocalisationID("PSG_10000468")]
		Hard,
		[LocalisationID("PSG_10000471")]
		Intermediate,
		[LocalisationID("PSG_10000470")]
		Wet,
		[LocalisationID("PSG_10007137")]
		UltraSoft
	}

	public enum Tread
	{
		Slick,
		LightTread,
		HeavyTread,
		None
	}
}
