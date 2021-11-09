using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarChassisStats
{
	public CarChassisStats()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static CarChassisStats()
	{
	}

	public void OnLoad(Team inTeam)
	{
		if (inTeam != null && this.mTyreWear == 0f && this.mTyreHeating == 0f && this.mImprovability == 0f && this.mFuelEfficiency == 0f && this.mStartingCharge == 0f)
		{
			List<Supplier> suppliers = this.GetSuppliers();
			if (suppliers.Count > 0 && suppliers[0] != null)
			{
				this.ApplyChampionshipBaseStat(inTeam.championship);
				this.ApplySupplierStats();
			}
		}
	}

	public void ResetSessionSetupChanges()
	{
		this.mSetupTyreWear = 0f;
		this.mSetupTyreHeating = 0f;
	}

	public void SetStat(CarChassisStats.Stats inStat, float inValue)
	{
		switch (inStat)
		{
		case CarChassisStats.Stats.TyreWear:
			this.mTyreWear = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		case CarChassisStats.Stats.TyreHeating:
			this.mTyreHeating = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		case CarChassisStats.Stats.FuelEfficiency:
			this.mFuelEfficiency = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		case CarChassisStats.Stats.Improvability:
			this.mImprovability = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		case CarChassisStats.Stats.StartingCharge:
			this.mStartingCharge = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		case CarChassisStats.Stats.HarvestEfficiency:
			this.mHarvestEfficiency = Mathf.Clamp(inValue, 0f, GameStatsConstants.chassisStatMax);
			break;
		}
	}

	public float GetStat(CarChassisStats.Stats inStat, bool withCarBonus = false, Car inCar = null)
	{
		float num = 0f;
		if (withCarBonus)
		{
			num = this.GetExtraValue(inStat, inCar);
		}
		switch (inStat)
		{
		case CarChassisStats.Stats.TyreWear:
			return this.mTyreWear + num;
		case CarChassisStats.Stats.TyreHeating:
			return this.mTyreHeating + num;
		case CarChassisStats.Stats.FuelEfficiency:
			return this.mFuelEfficiency + num;
		case CarChassisStats.Stats.Improvability:
			float reliability = (supplierEngine.maxReliablity + supplierFuel.maxReliablity) * 100f;
			reliability -= 55f;
			reliability /= 45f;
			reliability *= 20f;
			return reliability;
		case CarChassisStats.Stats.StartingCharge:
			return this.mStartingCharge + num;
		case CarChassisStats.Stats.HarvestEfficiency:
			return this.mHarvestEfficiency + num;
		default:
			return 0f;
		}
	}

	private float GetExtraValue(CarChassisStats.Stats inStat, Car inCar)
	{
		float num = 0f;
		List<BonusChassisStats> activePartBonus = inCar.GetActivePartBonus<BonusChassisStats>(null, CarPart.PartType.None);
		for (int i = 0; i < activePartBonus.Count; i++)
		{
			if (activePartBonus[i].stat == inStat)
			{
				num += activePartBonus[i].bonusValue;
			}
		}
		return num * GameStatsConstants.chassisStatMax;
	}

	public List<Supplier> GetSuppliers()
	{
		List<Supplier> list = new List<Supplier>(5);
		list.Add(this.supplierEngine);
		list.Add(this.supplierBrakes);
		list.Add(this.supplierFuel);
		list.Add(this.supplierMaterials);
		list.Add(this.supplierBattery);
		list.Add(this.supplierERSAdvanced);
		return list;
	}

	public Transaction GetEngineTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierEngine;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierEngine.GetPrice(inTeam), Localisation.LocaliseID("PSG_10010930", null));
	}

	public Transaction GetBrakesTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierBrakes;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierBrakes.GetPrice(inTeam), Localisation.LocaliseID("PSG_10010932", null));
	}

	public Transaction GetFuelTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierFuel;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierFuel.GetPrice(inTeam), Localisation.LocaliseID("PSG_10010555", null));
	}

	public Transaction GetMaterialTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierMaterials;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierMaterials.GetPrice(inTeam), Localisation.LocaliseID("PSG_10010931", null));
	}

	public Transaction GetBatteryTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierBattery;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierBattery.GetPrice(inTeam), this.supplierBattery.name + " - " + Localisation.LocaliseID("PSG_10011521", null));
	}

	public Transaction GetERSTransaction(Team inTeam)
	{
		StringVariableParser.supplier = this.supplierERSAdvanced;
		return new Transaction(Transaction.Group.NextYearCar, Transaction.Type.Debit, this.supplierERSAdvanced.GetPrice(inTeam), Localisation.LocaliseEnum(this.supplierERSAdvanced.advancedBatteryType) + " - " + Localisation.LocaliseID("PSG_10013673", null));
	}

	public void ApplyChampionshipBaseStat(Championship inChampionship)
	{
		this.ApplyChampionshipBaseStat(inChampionship.championshipID);
	}

	public void ApplyChampionshipBaseStat(int inChampionshipID)
	{
		float num;
		if (inChampionshipID < GameStatsConstants.chassisBaseStat.Length)
		{
			num = GameStatsConstants.chassisBaseStat[inChampionshipID];
		}
		else
		{
			num = GameStatsConstants.chassisBaseStat[GameStatsConstants.chassisBaseStat.Length - 1];
		}
		this.mFuelEfficiency += num;
		this.mImprovability += num;
		this.mTyreHeating += num;
		this.mTyreWear += num;
	}

	public void ApplySupplierStats()
	{
		this.ApplySupplierStats(this.GetSuppliers().ToArray());
	}

	public void ApplySupplierStats(params Supplier[] inSupplier)
	{
		foreach (Supplier supplier in inSupplier)
		{
			if (supplier.supplierType == Supplier.SupplierType.Battery || supplier.supplierType == Supplier.SupplierType.ERSAdvanced)
			{
				this.mStartingCharge = supplier.GetStat(CarChassisStats.Stats.StartingCharge);
				this.mHarvestEfficiency = supplier.GetStat(CarChassisStats.Stats.HarvestEfficiency);
			}
			else
			{
				this.mFuelEfficiency += supplier.GetStat(CarChassisStats.Stats.FuelEfficiency);
				this.mImprovability += supplier.GetStat(CarChassisStats.Stats.Improvability);
				this.mTyreHeating += supplier.GetStat(CarChassisStats.Stats.TyreHeating);
				this.mTyreWear += supplier.GetStat(CarChassisStats.Stats.TyreWear);
			}
		}
	}

	public CarChassisStats Clone()
	{
		return new CarChassisStats
		{
			mTyreHeating = this.tyreHeating,
			mTyreWear = this.tyreWear,
			mImprovability = this.improvability,
			mFuelEfficiency = this.fuelEfficiency,
			mStartingCharge = this.startingCharge,
			mHarvestEfficiency = this.harvestEfficiency,
			supplierEngine = this.supplierEngine,
			supplierBrakes = this.supplierBrakes,
			supplierFuel = this.supplierFuel,
			supplierMaterials = this.supplierMaterials,
			supplierBattery = this.supplierBattery,
			supplierERSAdvanced = this.supplierERSAdvanced,
			mSetupTyreWear = this.mSetupTyreWear,
			mSetupTyreHeating = this.mSetupTyreHeating
		};
	}

	public float startingCharge01
	{
		get
		{
			return this.mStartingCharge / 100f;
		}
	}

	public float tyreWear
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.TyreWear, false, null);
		}
		set
		{
			this.mTyreWear = value;
		}
	}

	public float tyreHeating
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.TyreHeating, false, null);
		}
		set
		{
			this.mTyreHeating = value;
		}
	}

	public float improvability
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.Improvability, false, null);
		}
		set
		{
			this.mImprovability = value;
		}
	}

	public float fuelEfficiency
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.FuelEfficiency, false, null);
		}
		set
		{
			this.mFuelEfficiency = value;
		}
	}

	public float startingCharge
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.StartingCharge, false, null);
		}
		set
		{
			this.mStartingCharge = value;
		}
	}

	public float harvestEfficiency
	{
		get
		{
			return this.GetStat(CarChassisStats.Stats.HarvestEfficiency, false, null);
		}
		set
		{
			this.mHarvestEfficiency = value;
		}
	}

	public const float maxSetupStatContribution = 0.5f;

	public Supplier supplierEngine = new Supplier();

	public Supplier supplierBrakes = new Supplier();

	public Supplier supplierFuel = new Supplier();

	public Supplier supplierMaterials = new Supplier();

	public Supplier supplierBattery = new Supplier();

	public Supplier supplierERSAdvanced = new Supplier();

	private float mTyreWear;

	private float mTyreHeating;

	private float mImprovability;

	private float mFuelEfficiency;

	private float mStartingCharge;

	private float mHarvestEfficiency = 1f;

	private float mSetupTyreWear;

	private float mSetupTyreHeating;

	public static CarChassisStats.Stats[] carDesignScreenStats = new CarChassisStats.Stats[]
	{
		CarChassisStats.Stats.TyreWear,
		CarChassisStats.Stats.TyreHeating,
		CarChassisStats.Stats.FuelEfficiency,
		CarChassisStats.Stats.Improvability
	};

	public enum Stats
	{
		[LocalisationID("PSG_10004259")]
		TyreWear,
		[LocalisationID("PSG_10004260")]
		TyreHeating,
		[LocalisationID("PSG_10004256")]
		FuelEfficiency,
		[LocalisationID("PSG_10004258")]
		Improvability,
		[LocalisationID("PSG_10011518")]
		StartingCharge,
		[LocalisationID("PSG_10011520")]
		HarvestEfficiency,
		Count
	}
}
