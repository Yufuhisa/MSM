using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarPartStats
{
	public CarPartStats(CarPart inPart)
	{
		this.partCondition.Setup(inPart);
		this.statType = CarPart.GetStatForPartType(inPart.GetPartType());
	}

	public CarPartStats()
	{
	}

	public float performancePerReliabilityPointWithModifiers
	{
		get
		{
			return this.performanceGainedPerReliabilityPointWeightStripped * this.mWeightStrippingModifier;
		}
	}

	public void AddToStat(CarPartStats.CarPartStat inStat, float inValue)
	{
		switch (inStat)
		{
		case CarPartStats.CarPartStat.MainStat:
			this.mStat += inValue;
			break;
		case CarPartStats.CarPartStat.Reliability:
			this.mReliability += inValue;
			this.mReliability = Mathf.Clamp(this.mReliability, 0f, this.maxReliability);
			break;
		case CarPartStats.CarPartStat.Condition:
			this.partCondition.SetCondition(this.partCondition.condition + inValue);
			if (this.partCondition.condition > this.mReliability)
			{
				this.partCondition.SetCondition(this.mReliability);
			}
			break;
		case CarPartStats.CarPartStat.Performance:
			this.mPerformance += inValue;
			this.mPerformance = Mathf.Min(this.mPerformance, this.maxPerformance);
			break;
		}
	}

	public void SetWeightStrippingReliabilityRatio(float inValue)
	{
		this.performanceGainedPerReliabilityPointWeightStripped = inValue;
	}

	public void SetStat(CarPartStats.CarPartStat inStat, float inValue)
	{
		switch (inStat)
		{
		case CarPartStats.CarPartStat.MainStat:
			this.mStat = inValue;
			break;
		case CarPartStats.CarPartStat.Reliability:
			this.mReliability = inValue;
			this.mReliability = Mathf.Clamp(this.mReliability, 0f, this.maxReliability);
			this.SetStat(CarPartStats.CarPartStat.Condition, this.mReliability);
			break;
		case CarPartStats.CarPartStat.Condition:
			this.partCondition.SetCondition(inValue);
			break;
		case CarPartStats.CarPartStat.Performance:
			this.mPerformance = inValue;
			this.mPerformance = Mathf.Min(this.mPerformance, this.maxPerformance);
			break;
		}
	}

	public float GetStat(CarPartStats.CarPartStat inStat)
	{
		float result = 0f;
		switch (inStat)
		{
		case CarPartStats.CarPartStat.MainStat:
			result = this.mStat;
			break;
		case CarPartStats.CarPartStat.Reliability:
			result = this.mReliability;
			break;
		case CarPartStats.CarPartStat.Condition:
			result = this.partCondition.condition;
			break;
		case CarPartStats.CarPartStat.Performance:
			result = this.mPerformance;
			break;
		}
		return result;
	}

	public CarPartStats Add(CarPartStats inStats)
	{
		this.mStat += inStats.mStat;
		this.mReliability += inStats.mReliability;
		this.partCondition.SetCondition(this.partCondition.condition + inStats.partCondition.condition);
		return this;
	}

	public CarPartStats Subtract(CarPartStats inStats)
	{
		this.mStat -= inStats.mStat;
		this.mReliability -= inStats.mReliability;
		this.partCondition.SetCondition(this.partCondition.condition - inStats.partCondition.condition);
		return this;
	}

	public void ApplyWeightStrippingModifier(CarPart inPart)
	{
		this.mWeightStrippingModifier = 1f;
		float num = 0f;
		for (int i = 0; i < inPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = inPart.components[i];
			if (carPartComponent != null)
			{
				for (int j = 0; j < carPartComponent.bonuses.Count; j++)
				{
					if (carPartComponent.bonuses[j] is BonusWeightStripping)
					{
						num += carPartComponent.bonuses[j].bonusValue;
					}
				}
			}
		}
		if (num != 0f)
		{
			this.mWeightStrippingModifier *= num;
		}
	}

	public void SetWeightStripping(float inValue, SessionDetails.SessionType inSessionType)
	{
		this.ResetWeightStripping(inSessionType);
		this.mWeightStrippingReliabilityLost[(int)inSessionType] = inValue;
		this.mStat += inValue * this.performancePerReliabilityPointWithModifiers;
		this.mReliability -= inValue / 100f;
		this.SetStat(CarPartStats.CarPartStat.Condition, this.mReliability);
	}

	public void ResetAllWeightStripping()
	{
		for (int i = 0; i < this.mWeightStrippingReliabilityLost.Length; i++)
		{
			this.ResetWeightStripping((SessionDetails.SessionType)i);
		}
	}

	public void ResetWeightStripping(SessionDetails.SessionType inSessionType)
	{
		float num = this.mWeightStrippingReliabilityLost[(int)inSessionType];
		this.mStat -= num * this.performancePerReliabilityPointWithModifiers;
		this.mReliability += num / 100f;
		this.SetStat(CarPartStats.CarPartStat.Condition, this.mReliability);
		this.mWeightStrippingReliabilityLost[(int)inSessionType] = 0f;
	}

	public float GetWeightStripping(SessionDetails.SessionType inSessionType)
	{
		return this.mWeightStrippingReliabilityLost[(int)inSessionType];
	}

	public float GetReliabilityWithoutWeightStripping(SessionDetails.SessionType inSessionType)
	{
		return this.mReliability + this.GetWeightStripping(inSessionType) / 100f;
	}

	public float GetTotalReliabilityWithoutWeightStripping()
	{
		float num = this.mReliability;
		for (int i = 0; i < this.mWeightStrippingReliabilityLost.Length; i++)
		{
			num += this.mWeightStrippingReliabilityLost[i] / 100f;
		}
		return num;
	}

	public float GetTotalWeightStripping()
	{
		float num = 0f;
		for (int i = 0; i < this.mWeightStrippingReliabilityLost.Length; i++)
		{
			num += this.mWeightStrippingReliabilityLost[i];
		}
		return num;
	}

	public CarPartStats Clone()
	{
		return new CarPartStats
		{
			statType = this.statType,
			mPerformance = this.mPerformance,
			mReliability = this.mReliability,
			mStat = this.mStat,
			maxPerformance = this.maxPerformance,
			maxReliability = this.maxReliability,
			level = this.level,
			rulesRisk = this.rulesRisk
		};
	}

	public static CarPartStats.RulesRisk GetRisk(float inValue)
	{
		if (inValue <= 0f)
		{
			return CarPartStats.RulesRisk.None;
		}
		if (inValue <= 1f)
		{
			return CarPartStats.RulesRisk.Low;
		}
		if (inValue <= 2f)
		{
			return CarPartStats.RulesRisk.Medium;
		}
		return CarPartStats.RulesRisk.High;
	}

	public static Color GetRiskColor(float inValue)
	{
		switch (CarPartStats.GetRisk(inValue))
		{
		case CarPartStats.RulesRisk.None:
			return UIConstants.riskNone;
		case CarPartStats.RulesRisk.Low:
			return UIConstants.riskLow;
		case CarPartStats.RulesRisk.Medium:
			return UIConstants.riskMedium;
		case CarPartStats.RulesRisk.High:
			return UIConstants.riskHigh;
		default:
			return Color.white;
		}
	}

	public static float GetNormalizedStatValue(float inStatValue, CarPart.PartType inType, Team inTeam = null)
	{
		Team team = (inTeam != null) ? inTeam : Game.instance.player.team;
		float num = (float)team.championship.rules.partStatSeasonMinValue[inType];
		float num2 = (float)team.championship.rules.partStatSeasonMaxValue[inType];
		return (inStatValue - num) / (num2 - num);
	}

	public float statWithPerformance
	{
		get
		{
			return this.mStat + this.mPerformance;
		}
	}

	public float reliability
	{
		get
		{
			return this.mReliability;
		}
	}

	public float performance
	{
		get
		{
			return this.mPerformance;
		}
	}

	public float stat
	{
		get
		{
			return this.mStat;
		}
	}

	public string rulesRiskString
	{
		get
		{
			return string.Format("{0}{1}</color>", GameUtility.ColorToRichTextHex(CarPartStats.GetRiskColor(this.rulesRisk)), Localisation.LocaliseEnum(CarPartStats.GetRisk(this.rulesRisk)));
		}
	}

	public const float weightStrippedReliabilityMin = 0.5f;

	public CarStats.StatType statType = CarStats.StatType.Acceleration;

	public int level;

	public float GetMaxReliability() {
		return Mathf.Clamp(maxReliability, 0f, GameStatsConstants.absolutMaxReliability);
	}

	public void SetMaxReliability(float inValue) {
		maxReliability = Mathf.Clamp(inValue, 0f, GameStatsConstants.absolutMaxReliability);
	}

	private float maxReliability = GameStatsConstants.initialMaxReliabilityValue;

	public float maxPerformance = GameStatsConstants.baseCarPartPerformance;

	public float rulesRisk;

	private float[] mWeightStrippingReliabilityLost = new float[SessionDetails.sessionTypeCount];

	private float mPerformance;

	private float mReliability = GameStatsConstants.initialReliabilityValue;

	private float mStat;

	private float mWeightStrippingModifier = 1f;

	public CarPartCondition partCondition = new CarPartCondition();

	private float performanceGainedPerReliabilityPointWeightStripped = 3f;

	public enum CarPartStat
	{
		MainStat,
		[LocalisationID("PSG_10002078")]
		Reliability,
		[LocalisationID("PSG_10004387")]
		Condition,
		[LocalisationID("PSG_10004388")]
		Performance,
		Count
	}

	public enum RulesRisk
	{
		[LocalisationID("PSG_10005815")]
		None,
		[LocalisationID("PSG_10001437")]
		Low,
		[LocalisationID("PSG_10001438")]
		Medium,
		[LocalisationID("PSG_10001439")]
		High
	}
}
