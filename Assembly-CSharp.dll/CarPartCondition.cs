using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarPartCondition
{
	public CarPartCondition()
	{
	}

	public CarPartCondition Clone(CarPart inCarPart)
	{
		CarPartCondition carPartCondition = new CarPartCondition();
		carPartCondition.mState = this.mState;
		carPartCondition.mCondition = this.mCondition;
		carPartCondition.mRedBand = this.mRedBand;
		carPartCondition.mActiveSessionRedZone = this.mActiveSessionRedZone;
		carPartCondition.mRepairInPit = this.mRepairInPit;
		carPartCondition.mRefreshPitRepairAmount = this.mRefreshPitRepairAmount;
		carPartCondition.mPitRepairAmount = this.mPitRepairAmount;
		carPartCondition.mPart = inCarPart;
		for (int i = 0; i < this.mBonus.Count; i++)
		{
			carPartCondition.mBonus.Add(this.mBonus[i]);
		}
		return carPartCondition;
	}

	public void Setup(CarPart inPart)
	{
		this.mPart = inPart;
	}

	public void AddBonus(CarPartComponentBonus inBonus)
	{
		this.mBonus.Add(inBonus);
	}

	public void RemoveBonus(CarPartComponentBonus inBonus)
	{
		this.mBonus.Remove(inBonus);
	}

	private void RemoveOutOfDateBonuses()
	{
		for (int i = 0; i < this.mBonus.Count; i++)
		{
			BonusNoConditionLossFirstSession bonusNoConditionLossFirstSession = (BonusNoConditionLossFirstSession)this.mBonus[i];
			if (bonusNoConditionLossFirstSession != null && bonusNoConditionLossFirstSession.assignedToSessionEnd)
			{
				this.RemoveBonus(bonusNoConditionLossFirstSession);
				SessionManager sessionManager = Game.instance.sessionManager;
				sessionManager.OnSessionEnd = (Action)Delegate.Remove(sessionManager.OnSessionEnd, new Action(this.RemoveOutOfDateBonuses));
				i--;
			}
		}
	}

	public float GetPitTimeToRepair(RacingVehicle inVehicle = null)
	{
		float t = Mathf.Clamp01(this.mPart.stats.reliability - this.mCondition);
		float num = EasingUtility.EaseByType(EasingUtility.Easing.InQuad, CarPartCondition.pitRepairPartTimeMin[this.mPart.GetPartType()], CarPartCondition.pitRepairPartTimeMax[this.mPart.GetPartType()], t);
		if (inVehicle != null && inVehicle.bonuses.IsBonusActive(MechanicBonus.Trait.QuickFixes))
		{
			num *= 0.5f;
		}
		return num;
	}

	public bool IsOnRed()
	{
		return this.mCondition <= this.redZone;
	}

	public bool IsFixed()
	{
		return Mathf.Approximately(this.mCondition, this.mPart.stats.reliability);
	}

	public void FixConditionAfterRace(float inNormalizedTime)
	{
		inNormalizedTime = Mathf.Max(inNormalizedTime, this.normalizedCondition);
		this.SetCondition(Mathf.Lerp(0f, this.mPart.stats.reliability, inNormalizedTime));
	}

	public void SetCondition(float inCondition)
	{
		this.mCondition = this.ClampConditionToReliability(inCondition);
		if (this.mCondition > 0f || !Game.instance.sessionManager.isSessionActive)
		{
			this.mState = CarPartCondition.PartState.Optimal;
		}
		else if (this.mState == CarPartCondition.PartState.Optimal)
		{
			this.mState = Game.instance.sessionManager.raceDirector.retireDirector.GetConditionLossOutcome(this.mPart);
		}
	}

	private float ClampConditionToReliability(float inCondition)
	{
		return Mathf.Clamp(inCondition, 0f, this.mPart.stats.reliability);
	}

	public bool CanRepairInPit()
	{
		switch (this.mPart.GetPartType())
        {
			case CarPart.PartType.Engine:
			case CarPart.PartType.Brakes:
			case CarPart.PartType.Gearbox:
			case CarPart.PartType.Suspension:
				return false;
			default:
				this.CheckRefreshPitAmount();
				return Mathf.RoundToInt(100f * this.mCondition) < Mathf.RoundToInt(this.ClampConditionToReliability(this.mPitRepairAmount + this.mCondition) * 100f);
		}
	}

	public void SetRepairInPit(bool inRepairInPit)
	{
		this.CheckRefreshPitAmount();
		this.mRepairInPit = inRepairInPit;
	}

	private void CheckRefreshPitAmount()
	{
		if (this.mRefreshPitRepairAmount)
		{
			this.mPitRepairAmount = this.GetPitRepairCondition();
			this.mRefreshPitRepairAmount = false;
		}
	}

	public void ApplyPitStopConditionFix(RacingVehicle inVehicle)
	{
		if (this.repairInPit)
		{
			this.SetCondition(this.condition + this.mPitRepairAmount);
			this.SetRepairInPit(false);
			this.mRefreshPitRepairAmount = true;
			if (this.mPart.reliability < 0.6f)
			{
				inVehicle.sessionData.lowReliabilityPartFixed = true;
			}
			inVehicle.partFailure.OnPartStateChanged(this.mPart, true);
		}
	}

	public float GetConditionAfterPit()
	{
		if (!this.repairInPit)
		{
			global::Debug.LogWarningFormat("Cant repait part {0} as it is not set to repair", new object[]
			{
				this.mPart.name
			});
			return 0f;
		}
		return this.ClampConditionToReliability(this.mPart.partCondition.condition + this.mPitRepairAmount);
	}

	private bool BonusConditionLossPrevented()
	{
		for (int i = 0; i < this.mBonus.Count; i++)
		{
			if (this.mBonus[i] is BonusNoConditionLossFirstSession)
			{
				BonusNoConditionLossFirstSession bonusNoConditionLossFirstSession = (BonusNoConditionLossFirstSession)this.mBonus[i];
				if (bonusNoConditionLossFirstSession.session == Game.instance.sessionManager.sessionType)
				{
					if (!bonusNoConditionLossFirstSession.assignedToSessionEnd)
					{
						SessionManager sessionManager = Game.instance.sessionManager;
						sessionManager.OnSessionEnd = (Action)Delegate.Combine(sessionManager.OnSessionEnd, new Action(this.RemoveOutOfDateBonuses));
						bonusNoConditionLossFirstSession.assignedToSessionEnd = true;
					}
					return true;
				}
			}
			else if (this.mBonus[i] is BonusNoConditionLoss)
			{
				return true;
			}
		}
		return false;
	}

	// calculate possibility for spontanous breakdown
	public float GetNextRealConditionLoss(float inCondition, CarPart.PartType inType, RacingVehicle inVehicle)
	{

		float ausfallWahrscheinlichkeit;

		if (this.mPart.stats.reliability >= 0.95f)
			ausfallWahrscheinlichkeit = 0.0007f;
		if (this.mPart.stats.reliability >= 0.9f)
			ausfallWahrscheinlichkeit = 0.0007f + ((0.95f - this.mPart.stats.reliability) / 0.05f * 0.0006f);
		else if (this.mPart.stats.reliability >= 0.75f)
			ausfallWahrscheinlichkeit = 0.0013f + ((0.9f - this.mPart.stats.reliability) / 0.15f * 0.0012f);
		else if (this.mPart.stats.reliability >= 0.6f)
			ausfallWahrscheinlichkeit = 0.0025f + ((0.75f - this.mPart.stats.reliability) / 0.15f * 0.0020f);
		else
			ausfallWahrscheinlichkeit = 0.0045f;

		if (RandomUtility.GetRandom01() <= ausfallWahrscheinlichkeit)
			return 1f;

		return this.GetNextConditionLoss(inCondition, inType, inVehicle);
	}

	public float GetNextConditionLoss(float inCondition, CarPart.PartType inType, RacingVehicle inVehicle)
	{
		if (this.BonusConditionLossPrevented() || inVehicle.timer.hasSeenChequeredFlag || inVehicle.behaviourManager.isOutOfRace)
		{
			return 0f;
		}
		if (inType == CarPart.PartType.Engine)
		{
			float num = RandomUtility.GetRandom(0.015f, 0.02f);
			switch (inVehicle.performance.fuel.engineMode)
			{
				case Fuel.EngineMode.SuperOvertake:
				case Fuel.EngineMode.Overtake:
					num *= 1.6f;
					break;
				case Fuel.EngineMode.High:
					num *= 1.2f;
					break;
				case Fuel.EngineMode.Medium:
					num *= 1f;
					break;
				case Fuel.EngineMode.Low:
					num *= 0.5f;
					break;
			}
			return num;
		}
		if (inType == CarPart.PartType.Brakes)
		{
			return RandomUtility.GetRandom(0.015f, 0.02f);
		}
		if (inType == CarPart.PartType.Gearbox)
		{
			return RandomUtility.GetRandom(0.015f, 0.02f);
		}
		if (inType == CarPart.PartType.Suspension)
		{
			return RandomUtility.GetRandom(0.015f, 0.02f);
		}
		if (inType == CarPart.PartType.FrontWing)
		{
			return RandomUtility.GetRandom(0.015f, 0.02f);
		}
		if (inType == CarPart.PartType.RearWing)
		{
			return RandomUtility.GetRandom(0.015f, 0.02f);
		}
		return 0f;
	}

	public void DecrementCondition(CarPart.PartType inType, RacingVehicle inVehicle)
	{
		float nextConditionLoss = this.GetNextRealConditionLoss(this.mCondition, inType, inVehicle);
		CarPartCondition.PartState partState = this.mState;
		float num = this.mCondition;
		this.SetCondition(this.mCondition - nextConditionLoss);
		if (inVehicle.isPlayerDriver && num >= App.instance.preferencesManager.superSpeedPreferences.PartCondition() && this.mCondition < App.instance.preferencesManager.superSpeedPreferences.PartCondition())
		{
			StringVariableParser.partFrontendUI = inType;
			Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.PartConditionLow, inVehicle);
		}
		if (partState != this.mState)
		{
			inVehicle.partFailure.OnPartStateChanged(this.mPart, true);
			CarPartCondition.PartState partState2 = this.mState;
			if (partState2 == CarPartCondition.PartState.Failure || partState2 == CarPartCondition.PartState.CatastrophicFailure)
			{
				if (this.mPart.reliability < 0.6f)
				{
					inVehicle.sessionData.lowReliabilityPartBroke = true;
				}
				if (this.mState == CarPartCondition.PartState.CatastrophicFailure)
				{
					CommentaryManager.SendComment(inVehicle, Comment.CommentType.Retirement, new object[]
					{
						inVehicle.driver
					});
				}
				else
				{
					CommentaryManager.SendComment(inVehicle, Comment.CommentType.MechanicalIssue, new object[]
					{
						inVehicle.driver
					});
				}
			}
		}
		else if (this.mCondition < 0.5f && (inType == CarPart.PartType.Engine || inType == CarPart.PartType.EngineGT) && inVehicle.isPlayerDriver && nextConditionLoss > 0.08f && (inVehicle.performance.fuel.engineMode == Fuel.EngineMode.Overtake || inVehicle.performance.fuel.engineMode == Fuel.EngineMode.SuperOvertake))
		{
			inVehicle.teamRadio.GetRadioMessage<RadioMessageCarPart>().SendOvertakeModeHurtingEngineMessage();
		}
	}

	private float GetLossBonusForCircuitStatRelevancy(CarPart.PartType inType, RacingVehicle inVehicle)
	{
		Circuit circuit = Game.instance.sessionManager.eventDetails.circuit;
		CarStats.StatType statForPartType = CarPart.GetStatForPartType(inType);
		CarStats.RelevantToCircuit relevancy = circuit.GetRelevancy(statForPartType);
		CarStats.RelevantToCircuit relevantToCircuit = relevancy;
		if (relevantToCircuit != CarStats.RelevantToCircuit.VeryImportant)
		{
			return 0.8f;
		}
		if (inVehicle.championship.series == Championship.Series.EnduranceSeries)
		{
			return 1.25f;
		}
		return 1.15f;
	}

	public Color GetConditionColor()
	{
		return CarPartCondition.GetColorGradient(this.mCondition);
	}

	public Color GetConditionColor(float inCondition)
	{
		return CarPartCondition.GetColorGradient(inCondition);
	}

	private static Color GetColorGradient(float inValue)
	{
		if (inValue > 0.8f)
		{
			return Color.Lerp(UIConstants.conditionColor2, UIConstants.conditionColor1, inValue * 5f - 4f);
		}
		if (inValue > 0.6f)
		{
			return Color.Lerp(UIConstants.conditionColor3, UIConstants.conditionColor2, inValue * 5f - 3f);
		}
		if (inValue > 0.4f)
		{
			return Color.Lerp(UIConstants.conditionColor4, UIConstants.conditionColor3, inValue * 5f - 2f);
		}
		if (inValue > 0.2f)
		{
			return Color.Lerp(UIConstants.conditionColor5, UIConstants.conditionColor4, inValue * 5f - 1f);
		}
		return Color.Lerp(UIConstants.conditionColor6, UIConstants.conditionColor5, inValue * 5f);
	}

	public void SetActiveSessionRedZone(float inRedBand)
	{
		this.mActiveSessionRedZone = inRedBand;
	}

	private float GetPitRepairCondition()
	{
		return this.GetPitRepairMechanicStat() + UnityEngine.Random.Range(CarPartCondition.pitRepairPartConditionMin[this.mPart.GetPartType()], CarPartCondition.pitRepairPartConditionMax[this.mPart.GetPartType()]);
	}

	private float GetPitRepairMechanicStat()
	{
		Driver driver = Game.instance.vehicleManager.GetVehicle(this.mPart.fittedCar).driver;
		Mechanic mechanicOfDriver = driver.contract.GetTeam().GetMechanicOfDriver(driver);
		float t = Mathf.Clamp01(mechanicOfDriver.stats.leadership / 20f);
		return Mathf.Lerp(0.05f, 0.1f, t);
	}

	public float normalizedCondition
	{
		get
		{
			return (this.mPart.stats.reliability == 0f) ? 1f : (this.mCondition / this.mPart.stats.reliability);
		}
	}

	public float condition
	{
		get
		{
			return this.mCondition;
		}
	}

	public float redZoneBaseValue
	{
		get
		{
			return this.mRedBand;
		}
	}

	public float redZone
	{
		get
		{
			if (!App.instance.gameStateManager.currentState.IsFrontend())
			{
				return this.mActiveSessionRedZone;
			}
			return this.mRedBand;
		}
		set
		{
			this.mRedBand = value;
			this.mActiveSessionRedZone = value;
		}
	}

	public CarPartCondition.PartState partState
	{
		get
		{
			return this.mState;
		}
	}

	public bool repairInPit
	{
		get
		{
			return this.mRepairInPit;
		}
	}

	private const float mechanicRepairOffsetMin = 0.05f;

	private const float mechanicRepairOffsetMax = 0.1f;

	private static readonly Dictionary<CarPart.PartType, float> pitRepairPartConditionMin = new Dictionary<CarPart.PartType, float>
	{
		{
			CarPart.PartType.Brakes,
			0.3f
		},
		{
			CarPart.PartType.Engine,
			0.2f
		},
		{
			CarPart.PartType.FrontWing,
			1f
		},
		{
			CarPart.PartType.Gearbox,
			0.2f
		},
		{
			CarPart.PartType.RearWing,
			0.7f
		},
		{
			CarPart.PartType.Suspension,
			0.4f
		},
		{
			CarPart.PartType.RearWingGT,
			0.7f
		},
		{
			CarPart.PartType.BrakesGT,
			0.3f
		},
		{
			CarPart.PartType.EngineGT,
			0.2f
		},
		{
			CarPart.PartType.GearboxGT,
			0.2f
		},
		{
			CarPart.PartType.SuspensionGT,
			0.4f
		},
		{
			CarPart.PartType.BrakesGET,
			0.3f
		},
		{
			CarPart.PartType.EngineGET,
			0.2f
		},
		{
			CarPart.PartType.FrontWingGET,
			1f
		},
		{
			CarPart.PartType.GearboxGET,
			0.2f
		},
		{
			CarPart.PartType.RearWingGET,
			0.7f
		},
		{
			CarPart.PartType.SuspensionGET,
			0.4f
		}
	};

	private static readonly Dictionary<CarPart.PartType, float> pitRepairPartConditionMax = new Dictionary<CarPart.PartType, float>
	{
		{
			CarPart.PartType.Brakes,
			0.5f
		},
		{
			CarPart.PartType.Engine,
			0.4f
		},
		{
			CarPart.PartType.FrontWing,
			1f
		},
		{
			CarPart.PartType.Gearbox,
			0.4f
		},
		{
			CarPart.PartType.RearWing,
			1f
		},
		{
			CarPart.PartType.Suspension,
			0.6f
		},
		{
			CarPart.PartType.RearWingGT,
			1f
		},
		{
			CarPart.PartType.BrakesGT,
			0.5f
		},
		{
			CarPart.PartType.EngineGT,
			0.4f
		},
		{
			CarPart.PartType.GearboxGT,
			0.4f
		},
		{
			CarPart.PartType.SuspensionGT,
			0.6f
		},
		{
			CarPart.PartType.BrakesGET,
			0.5f
		},
		{
			CarPart.PartType.EngineGET,
			0.4f
		},
		{
			CarPart.PartType.FrontWingGET,
			1f
		},
		{
			CarPart.PartType.GearboxGET,
			0.4f
		},
		{
			CarPart.PartType.RearWingGET,
			1f
		},
		{
			CarPart.PartType.SuspensionGET,
			0.6f
		}
	};

	private static readonly Dictionary<CarPart.PartType, float> pitRepairPartTimeMin = new Dictionary<CarPart.PartType, float>
	{
		{
			CarPart.PartType.Brakes,
			6f
		},
		{
			CarPart.PartType.Engine,
			15f
		},
		{
			CarPart.PartType.FrontWing,
			4f
		},
		{
			CarPart.PartType.Gearbox,
			15f
		},
		{
			CarPart.PartType.RearWing,
			6f
		},
		{
			CarPart.PartType.Suspension,
			8f
		},
		{
			CarPart.PartType.RearWingGT,
			6f
		},
		{
			CarPart.PartType.BrakesGT,
			6f
		},
		{
			CarPart.PartType.EngineGT,
			15f
		},
		{
			CarPart.PartType.GearboxGT,
			15f
		},
		{
			CarPart.PartType.SuspensionGT,
			8f
		},
		{
			CarPart.PartType.BrakesGET,
			20f
		},
		{
			CarPart.PartType.EngineGET,
			60f
		},
		{
			CarPart.PartType.FrontWingGET,
			10f
		},
		{
			CarPart.PartType.GearboxGET,
			45f
		},
		{
			CarPart.PartType.RearWingGET,
			20f
		},
		{
			CarPart.PartType.SuspensionGET,
			30f
		}
	};

	private static readonly Dictionary<CarPart.PartType, float> pitRepairPartTimeMax = new Dictionary<CarPart.PartType, float>
	{
		{
			CarPart.PartType.Brakes,
			10f
		},
		{
			CarPart.PartType.Engine,
			30f
		},
		{
			CarPart.PartType.FrontWing,
			8f
		},
		{
			CarPart.PartType.Gearbox,
			30f
		},
		{
			CarPart.PartType.RearWing,
			10f
		},
		{
			CarPart.PartType.Suspension,
			12f
		},
		{
			CarPart.PartType.RearWingGT,
			10f
		},
		{
			CarPart.PartType.BrakesGT,
			10f
		},
		{
			CarPart.PartType.EngineGT,
			30f
		},
		{
			CarPart.PartType.GearboxGT,
			30f
		},
		{
			CarPart.PartType.SuspensionGT,
			12f
		},
		{
			CarPart.PartType.BrakesGET,
			30f
		},
		{
			CarPart.PartType.EngineGET,
			90f
		},
		{
			CarPart.PartType.FrontWingGET,
			20f
		},
		{
			CarPart.PartType.GearboxGET,
			60f
		},
		{
			CarPart.PartType.RearWingGET,
			30f
		},
		{
			CarPart.PartType.SuspensionGET,
			45f
		}
	};

	private CarPartCondition.PartState mState;

	private float mCondition = 1f;

	private float mRedBand;

	private float mActiveSessionRedZone;

	private bool mRepairInPit;

	private bool mRefreshPitRepairAmount = true;

	private float mPitRepairAmount;

	private CarPart mPart;

	private List<CarPartComponentBonus> mBonus = new List<CarPartComponentBonus>();

	public enum PartState
	{
		Optimal,
		Failure,
		CatastrophicFailure
	}
}
