using System;
using System.Collections.Generic;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarPartComponent : fsISerializationCallbacks
{
	public CarPartComponent()
	{
	}

	public bool IsAvailableForType(CarPart.PartType inType)
	{
		return this.partsAvailableTo.Contains(inType);
	}

	public bool IsUnlocked(Team inTeam)
	{
		for (int i = 0; i < this.unlockRequirements.Count; i++)
		{
			if (this.unlockRequirements[i].IsLocked(inTeam))
			{
				return false;
			}
		}
		return true;
	}

	public string GetIconPath()
	{
		return this.iconPath;
	}

	public string GetLockedDescription()
	{
		for (int i = 0; i < this.unlockRequirements.Count; i++)
		{
			if (this.unlockRequirements[i].IsLocked(Game.instance.player.team))
			{
				return this.unlockRequirements[i].GetDescription(Game.instance.player.team) + " \n";
			}
		}
		return string.Empty;
	}

	public void Refresh(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.Refresh(inDesign, inPart);
			});
		}
	}

	public void ApplyBonus(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.ApplyBonus(inDesign, inPart);
			});
		}
	}

	public void OnPartBuildStart(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.OnPartBuildStart(inDesign, inPart);
			});
		}
	}

	public void OnPartBuilt(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.OnPartBuilt(inDesign, inPart);
			});
		}
	}

	public void OnSelect(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.OnSelect(inDesign, inPart);
			});
		}
	}

	public void OnDeSelect(CarPartDesign inDesign, CarPart inPart)
	{
		if (this.IsUnlocked(inDesign.team))
		{
			this.mBonuses.ForEach(delegate(CarPartComponentBonus bonus)
			{
				bonus.OnDeselect(inDesign, inPart);
			});
		}
	}

	public void ApplyStats(CarPart inPart)
	{
		if (this.HasActivationRequirement())
		{
			return;
		}
		inPart.stats.rulesRisk += this.riskLevel;
		inPart.stats.maxPerformance += this.maxStatBoost;
		inPart.stats.partCondition.redZone += this.redZone;
		inPart.stats.AddMaxReliability(this.maxReliabilityBoost);
		inPart.stats.SetStat(CarPartStats.CarPartStat.MainStat, inPart.stats.stat + this.statBoost);
		inPart.stats.SetStat(CarPartStats.CarPartStat.Reliability, inPart.stats.reliability + this.reliabilityBoost);
	}

	public bool IgnoreBonusForUI()
	{
		return this.HasSpecificBonus<BonusUnlockExtraSlot>();
	}

	public bool HasSpecificBonus<T>() where T : CarPartComponentBonus
	{
		for (int i = 0; i < this.mBonuses.Count; i++)
		{
			if (this.mBonuses[i] is T)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasActivationRequirement()
	{
		return this.activationRequirements.Count != 0;
	}

	public bool AreAllRequirementsMeet(RacingVehicle inVehicle)
	{
		bool result = true;
		for (int i = 0; i < this.activationRequirements.Count; i++)
		{
			CarPartComponentRequirement carPartComponentRequirement = this.activationRequirements[i];
			if (carPartComponentRequirement.IsLocked(inVehicle))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private void AddStatData(List<KeyValuePair<string, string>> inList, string inKey, string inValue)
	{
		KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(inKey, inValue);
		inList.Add(keyValuePair);
	}

	public string GetName(CarPart inPart)
	{
		if (!string.IsNullOrEmpty(this.mCustomComponentName) && this.mCustomComponentName != "0")
		{
			return this.mCustomComponentName;
		}
		StringVariableParser.partWithComponent = inPart;
		StringVariableParser.component = this;
		string result = Localisation.LocaliseID(this.mNameID, null);
		StringVariableParser.partWithComponent = null;
		StringVariableParser.component = null;
		return result;
	}

	public void AddBonuses(CarPartComponentBonus inBonus)
	{
		inBonus.component = this;
		this.mBonuses.Add(inBonus);
	}

	public void OnBeforeSerialize(Type storageType)
	{
	}

	public void OnAfterSerialize(Type storageType, ref fsData data)
	{
	}

	public void OnBeforeDeserialize(Type storageType, ref fsData data)
	{
	}

	public void OnAfterDeserialize(Type storageType)
	{
		App.instance.componentsManager.ValidateData(this);
	}

	public List<CarPartComponentBonus> bonuses
	{
		get
		{
			return this.mBonuses;
		}
	}

	public string nameID
	{
		get
		{
			return this.mNameID;
		}
		set
		{
			this.mNameID = value;
		}
	}

	public string customComponentName
	{
		set
		{
			this.mCustomComponentName = value;
		}
	}

	public float nonAgressiveReliabilityTeamWeightings
	{
		get
		{
			return this.maxReliabilityBoost * 100 + this.nonAgressiveTeamWeightings;
		}
	}

	public float agressiveReliabilityTeamWeightings
	{
		get
		{
			return this.maxReliabilityBoost * 100 + this.agressiveTeamWeightings;
		}
	}

	public List<CarPart.PartType> partsAvailableTo = new List<CarPart.PartType>();

	public List<CarPartUnlockRequirement> unlockRequirements = new List<CarPartUnlockRequirement>();

	public List<CarPartComponentRequirement> activationRequirements = new List<CarPartComponentRequirement>();

	private List<CarPartComponentBonus> mBonuses = new List<CarPartComponentBonus>();

	public CarPartComponent.ComponentType componentType;

	public float riskLevel;

	public float statBoost;

	public float maxStatBoost;

	public float reliabilityBoost;

	public float maxReliabilityBoost;

	public float productionTime;

	public float cost;

	public float redZone;

	public bool isRandomComponent;

	public string iconPath = string.Empty;

	public int iconID;

	public int level;

	public int id = -1;

	public float agressiveTeamWeightings = 1f;

	public float nonAgressiveTeamWeightings = 1f;

	private string mNameID = string.Empty;

	private string mCustomComponentName = string.Empty;

	public enum ComponentType
	{
		Stock,
		Engineer,
		Risky
	}
}
