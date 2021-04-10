using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Supplier
{
	public Supplier()
	{
	}

	public int tier
	{
		get
		{
			return this.mTier;
		}
		set
		{
			this.mTier = value;
		}
	}

	public int price
	{
		set
		{
			this.mBasePrice = value;
		}
	}

	public int randomEngineLevelModifier
	{
		get
		{
			return this.mRandomEngineLevelModifier;
		}
	}

	public float randomHarvestEfficiencyModifier
	{
		get
		{
			return this.mRandomHarvestEfficiencyModifier;
		}
	}

	public int GetPrice(Team inTeam)
	{
		int num;
		if (this.supplierType == Supplier.SupplierType.Engine)
		{
			num = this.mBasePrice + Mathf.RoundToInt((float)this.mRandomEngineLevelModifier * this.mPriceMultiplier * this.mScalar);
		}
		else if (this.supplierType == Supplier.SupplierType.Battery)
		{
			num = this.mBasePrice + Mathf.RoundToInt(this.mRandomHarvestEfficiencyModifier * this.mPriceMultiplier * this.mScalar);
			if (inTeam.championship.rules.isHybridModeActive)
			{
				num += GameStatsConstants.hybridModeCost;
			}
			num = Mathf.RoundToInt((float)(num / 1000)) * 1000;
		}
		else
		{
			num = this.mBasePrice;
		}
		float num2;
		if (!this.teamDiscounts.TryGetValue(inTeam.teamID, ref num2))
		{
			return num;
		}
		return num - Mathf.RoundToInt((float)num * (num2 / 100f));
	}

	public int GetPriceNoDiscount(Championship inChampionship, ChampionshipRules.EnergySystemBattery inBatterySize = ChampionshipRules.EnergySystemBattery.Small)
	{
		if (this.supplierType == Supplier.SupplierType.Engine)
		{
			return this.mBasePrice + Mathf.RoundToInt((float)this.mRandomEngineLevelModifier * this.mPriceMultiplier * this.mScalar);
		}
		if (this.supplierType == Supplier.SupplierType.Battery)
		{
			int num = this.mBasePrice + Mathf.RoundToInt(this.mRandomHarvestEfficiencyModifier * this.mPriceMultiplier * this.mScalar);
			if (inChampionship.rules.isHybridModeActive)
			{
				num += GameStatsConstants.hybridModeCost;
			}
			if (inBatterySize == ChampionshipRules.EnergySystemBattery.Large)
			{
				num = (int)((float)num + (float)num * 0.1f);
			}
			return Mathf.RoundToInt((float)(num / 1000)) * 1000;
		}
		return this.mBasePrice;
	}

	public float GetTeamDiscount(Team inTeam)
	{
		float result = 0f;
		this.teamDiscounts.TryGetValue(inTeam.teamID, ref result);
		return result;
	}

	public float GetStat(CarChassisStats.Stats inStat)
	{
		if (this.supplierStats.ContainsKey(inStat))
		{
			return this.supplierStats[inStat];
		}
		return 0f;
	}

	public bool CanTeamBuyThis(Team inTeam)
	{
		return !this.mTeamsThatCannotBuy.Contains(inTeam.teamID);
	}

	public bool HasDiscountWithTeam(Team inTeam)
	{
		return this.teamDiscounts.ContainsKey(inTeam.teamID);
	}

	public void AddTeamDiscount(int inTeamID, float inDiscountAmount)
	{
		this.teamDiscounts.Add(inTeamID, inDiscountAmount);
	}

	public void AddTeamsThatCannotBuy(int inTeamID)
	{
		this.mTeamsThatCannotBuy.Add(inTeamID);
	}

	public void RollRandomBaseStatModifier()
	{
		if (this.supplierType == Supplier.SupplierType.Engine)
		{
			this.mRandomEngineLevelModifier = RandomUtility.GetRandomInc(this.minEngineLevelModifier, this.maxEngineLevelModifier);
		}
		if (this.supplierType == Supplier.SupplierType.Battery || this.supplierType == Supplier.SupplierType.ERSAdvanced)
		{
			this.mRandomHarvestEfficiencyModifier = RandomUtility.GetRandom(this.minHarvestEfficiencyModifier, this.maxHarvestEfficiencyModifier);
			this.supplierStats[CarChassisStats.Stats.HarvestEfficiency] = this.mRandomHarvestEfficiencyModifier;
		}
	}

	public string GetDescription()
	{
		if (!string.IsNullOrEmpty(this.descriptionID))
		{
			return Localisation.LocaliseID(this.descriptionID, null);
		}
		return string.Empty;
	}

	private readonly float mPriceMultiplier = 0.4f;

	private readonly float mScalar = 1000000f;

	public Supplier.SupplierType supplierType = Supplier.SupplierType.Brakes;

	public string name = string.Empty;

	public int id;

	public int logoIndex;

	public int minEngineLevelModifier;

	public int maxEngineLevelModifier;

	public int hybridGates;

	public int powerGates;

	public int chargeSize;

	public string descriptionID = string.Empty;

	public float minHarvestEfficiencyModifier;

	public float maxHarvestEfficiencyModifier;

	public Dictionary<CarChassisStats.Stats, float> supplierStats = new Dictionary<CarChassisStats.Stats, float>();

	public Dictionary<Supplier.CarAspect, float> carAspectMinBoundary = new Dictionary<Supplier.CarAspect, float>();

	public Dictionary<Supplier.CarAspect, float> carAspectMaxBoundary = new Dictionary<Supplier.CarAspect, float>();

	public Supplier.AdvancedERSBatteryType advancedBatteryType;

	private int mBasePrice;

	private int mTier;

	private Dictionary<int, float> teamDiscounts = new Dictionary<int, float>();

	private List<int> mTeamsThatCannotBuy = new List<int>();

	private int mRandomEngineLevelModifier;

	private float mRandomHarvestEfficiencyModifier;

	public enum SupplierType
	{
		[LocalisationID("PSG_10004263")]
		Engine,
		[LocalisationID("PSG_10010289")]
		Brakes,
		[LocalisationID("PSG_10004264")]
		Fuel,
		[LocalisationID("PSG_10004261")]
		Materials,
		[LocalisationID("PSG_10011517")]
		Battery,
		[LocalisationID("PSG_10011517")]
		ERSAdvanced
	}

	public enum CarAspect
	{
		[LocalisationID("PSG_10008693")]
		RearPackage,
		[LocalisationID("PSG_10008694")]
		NoseHeight
	}

	public enum AdvancedERSBatteryType
	{
		None,
		[LocalisationID("PSG_10013889")]
		Flywheel,
		[LocalisationID("PSG_10013891")]
		Battery,
		[LocalisationID("PSG_10013890")]
		SuperCapacitor
	}
}
