using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CarOpinion
{
	public CarOpinion()
	{
	}

	public float overallMoraleHit
	{
		get
		{
			return this.mOverallMoraleHit;
		}
	}

	public float againstOtherCarMoraleHit
	{
		get
		{
			return this.mAgainstOtherCarMoraleHit;
		}
	}

	public void CalculateDriverOpinions(Driver inDriver)
	{
		if (Game.IsActive() && !Game.instance.player.IsUnemployed() && inDriver.IsPlayersDriver())
		{
			Game.instance.teamManager.CalculateDriverExpectedPositionsInChampionship(Game.instance.player.team.championship);
		}
		this.mDriver = inDriver;
		Team team = this.mDriver.contract.GetTeam();
		Car carForDriver = team.carManager.GetCarForDriver(this.mDriver);
		if (!carForDriver.HasAllPartSlotsFitted() || !Game.IsActive())
		{
			this.mOverallCarComment = null;
			this.mAgainstOtherCarComment = null;
			return;
		}
		this.mDriverOverallHappiness = this.CalculateDriverOverallHappiness(this.mDriver);
		this.mDriverAgainstOtherCarHappiness = this.CalculateDriverAgainstOtherCarHappiness(this.mDriver);
		this.mDriverAverageHappiness = this.GetAverageDriverHappiness(this.mDriver);
		if (this.mDriver.IsPlayersDriver())
		{
			this.mOverallCarComment = this.GenerateDriverComment(this.mDriver, this.mDriverOverallHappiness, CarOpinion.CommentType.OverallCarComment);
			this.mAgainstOtherCarComment = this.GenerateDriverComment(this.mDriver, this.mDriverAgainstOtherCarHappiness, CarOpinion.CommentType.AgainstOtherCarComment);
			StringVariableParser.ResetAllStaticReferences();
		}
		this.CalculateMoraleHits(this.mDriver);
	}

	public bool HasOpinion()
	{
		return this.mOverallCarComment != null && this.mAgainstOtherCarComment != null;
	}

	public CarOpinion.Happiness GetDriverOverallHappiness()
	{
		return this.mDriverOverallHappiness;
	}

	public CarOpinion.Happiness GetDriverAgainstOtherCarHappiness()
	{
		return this.mDriverAgainstOtherCarHappiness;
	}

	public CarOpinion.Happiness GetDriverAverageHappiness()
	{
		return this.mDriverAverageHappiness;
	}

	public DialogRule GetDriverComment(CarOpinion.CommentType inSource)
	{
		if (inSource == CarOpinion.CommentType.OverallCarComment)
		{
			return this.mOverallCarComment;
		}
		if (inSource != CarOpinion.CommentType.AgainstOtherCarComment)
		{
			return null;
		}
		return this.mAgainstOtherCarComment;
	}

	private DialogRule GenerateDriverComment(Driver inDriver, CarOpinion.Happiness inHappiness, CarOpinion.CommentType inSource)
	{
		Team team = inDriver.contract.GetTeam();
		int inCarIndex = (team.GetDriverIndex(inDriver) != 0) ? 0 : 1;
		Driver driverForCar = team.GetDriverForCar(inCarIndex);
		Car carForDriver = team.carManager.GetCarForDriver(inDriver);
		Car carForDriver2 = team.carManager.GetCarForDriver(driverForCar);
		float num = (float)carForDriver.GetStats().statsTotal;
		float num2 = (float)carForDriver2.GetStats().statsTotal;
		StringVariableParser.anyDriver = inDriver;
		StringVariableParser.otherDriver = driverForCar;
		DialogQuery dialogQuery = new DialogQuery();
		dialogQuery.AddCriteria("Source", inSource.ToString());
		dialogQuery.who = new DialogCriteria("Who", "AnyDriver");
		switch (inHappiness)
		{
		case CarOpinion.Happiness.Angry:
		case CarOpinion.Happiness.Unhappy:
			dialogQuery.AddCriteria("DriverOutlook", "Negative");
			break;
		case CarOpinion.Happiness.Content:
			dialogQuery.AddCriteria("DriverOutlook", "Neutral");
			break;
		case CarOpinion.Happiness.Happy:
		case CarOpinion.Happiness.Delighted:
			dialogQuery.AddCriteria("DriverOutlook", "Positive");
			break;
		}
		dialogQuery.AddCriteria("DriverHappiness", inHappiness.ToString());
		List<Car> overralBestCarsOfChampionship = CarManager.GetOverralBestCarsOfChampionship(team.championship);
		int num3 = overralBestCarsOfChampionship.IndexOf(carForDriver);
		string inInfo = (num3 >= 8) ? ((num3 >= 16) ? "Low" : "Medium") : "High";
		dialogQuery.AddCriteria("OverallCarQuality", inInfo);
		float reliability = carForDriver.GetReliability();
		string inInfo2 = (reliability <= 0.85f) ? ((reliability <= 0.6f) ? "Low" : "Medium") : "High";
		dialogQuery.AddCriteria("OverallCarReliability", inInfo2);
		float inDelta = num - num2;
		string teamCarComparisonCriteria = this.GetTeamCarComparisonCriteria(inDelta, inDriver.contract.proposedStatus);
		dialogQuery.AddCriteria("TeamCarQualityComparison", teamCarComparisonCriteria);
		CarPart.PartType partType = CarPart.PartType.None;
		for (int i = 0; i < carForDriver.seriesCurrentParts.Length; i++)
		{
			CarPart carPart = carForDriver.seriesCurrentParts[i];
			if (carPart != null && carPart.reliability < 0.4f)
			{
				partType = carPart.GetPartType();
				if (RandomUtility.GetRandom(0, 100) > 50)
				{
					break;
				}
			}
		}
		if (partType != CarPart.PartType.None)
		{
			StringVariableParser.reliabilityPartType = partType;
			dialogQuery.AddCriteria("PartReliabilityLevel", "ReliabilityWarning");
		}
		CarPart.PartType partType2 = CarPart.PartType.None;
		string inInfo3 = string.Empty;
		for (int j = 0; j < carForDriver.seriesCurrentParts.Length; j++)
		{
			CarPart carPart2 = carForDriver.seriesCurrentParts[j];
			CarPart carPart3 = carForDriver2.seriesCurrentParts[j];
			if (carPart2 != null && carPart3 != null)
			{
				float num4 = (float)(carPart2.stats.level - carPart3.stats.level);
				if (num4 > 40f)
				{
					partType2 = carPart2.GetPartType();
					inInfo3 = "Better";
					if (RandomUtility.GetRandom(0, 100) > 50)
					{
						break;
					}
				}
				else if (num4 < -40f)
				{
					partType2 = carPart2.GetPartType();
					inInfo3 = "Worse";
					if (RandomUtility.GetRandom(0, 100) > 50)
					{
						break;
					}
				}
			}
		}
		if (partType2 != CarPart.PartType.None)
		{
			StringVariableParser.levelGapPartType = partType2;
			dialogQuery.AddCriteria("PartLevelGapLarge", inInfo3);
		}
		string inInfo4 = string.Empty;
		float num5 = 0.5f;
		if (num5 > 0.66f)
		{
			inInfo4 = "Oversteer";
			dialogQuery.AddCriteria("BalanceType", inInfo4);
		}
		if (num5 < 0.33f)
		{
			inInfo4 = "Understeer";
			dialogQuery.AddCriteria("BalanceType", inInfo4);
		}
		float num6 = Mathf.Abs(num5 - inDriver.GetDriverStats().balance);
		float num7 = Mathf.Abs(num5 - driverForCar.GetDriverStats().balance);
		string text = string.Empty;
		if (num6 < 0.15f)
		{
			text = "Favourable";
		}
		else if (num6 < 0.4f)
		{
			text = "Neutral";
		}
		else
		{
			text = "Unfavourable";
		}
		dialogQuery.AddCriteria("CarBalance", text);
		if (num7 < 0.25f && text != "Favourable")
		{
			dialogQuery.AddCriteria("BalancedForOtherDriver", "True");
		}
		StringVariableParser.subject = inDriver;
		return inDriver.dialogQuery.ProcessQueryWithOwnCriteria(dialogQuery, false);
	}

	private CarOpinion.Happiness GetAverageDriverHappiness(Driver inDriver)
	{
		int num = (int)this.mDriverOverallHappiness;
		int num2 = (int)this.mDriverAgainstOtherCarHappiness;
		return (CarOpinion.Happiness)Mathf.RoundToInt(((float)num + (float)num2) * 0.5f);
	}

	private CarOpinion.Happiness CalculateDriverOverallHappiness(Driver inDriver)
	{
		Team team = inDriver.contract.GetTeam();
		Car carForDriver = team.carManager.GetCarForDriver(inDriver);
		List<Car> overralBestCarsOfChampionship = CarManager.GetOverralBestCarsOfChampionship(team.championship);
		int num = overralBestCarsOfChampionship.IndexOf(carForDriver);
		if (num < 4)
		{
			return CarOpinion.Happiness.Delighted;
		}
		if (num < 8)
		{
			return CarOpinion.Happiness.Happy;
		}
		if (num < 12)
		{
			return CarOpinion.Happiness.Content;
		}
		if (num < 16)
		{
			return CarOpinion.Happiness.Unhappy;
		}
		return CarOpinion.Happiness.Angry;
	}

	private CarOpinion.Happiness CalculateDriverAgainstOtherCarHappiness(Driver inDriver)
	{
		return (CarOpinion.Happiness)this.GetOtherCarComparisonValue(inDriver);
	}

	private float GetOtherCarComparisonValue(Driver inDriver)
	{
		Team team = inDriver.contract.GetTeam();
		int inCarIndex = (inDriver.carID != 0) ? 0 : 1;
		Driver driverForCar = team.GetDriverForCar(inCarIndex);
		Car carForDriver = team.carManager.GetCarForDriver(inDriver);
		Car carForDriver2 = team.carManager.GetCarForDriver(driverForCar);
		float inDelta = (float)(carForDriver.GetStats().statsTotal - carForDriver2.GetStats().statsTotal);
		return this.GetCarComparisionWeight(inDelta, inDriver.contract.proposedStatus);
	}

	private float GetTraitsCarMoraleHit(Driver inDriver)
	{
		Team team = inDriver.contract.GetTeam();
		Car carForDriver = team.carManager.GetCarForDriver(inDriver);
		float result = 0f;
		if (inDriver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.CarHappinessBonus))
		{
			List<CarPart> mostRecentParts = team.carManager.partInventory.GetMostRecentParts(6, CarPart.PartType.None);
			for (int i = mostRecentParts.Count - 1; i >= 0; i--)
			{
				if (!mostRecentParts[i].isFitted)
				{
					mostRecentParts.Remove(mostRecentParts[i]);
				}
			}
			int num = 0;
			for (int j = 0; j < carForDriver.seriesCurrentParts.Length; j++)
			{
				if (mostRecentParts.Contains(carForDriver.seriesCurrentParts[j]))
				{
					num++;
				}
			}
			float num2 = (float)num / (float)mostRecentParts.Count;
			if (num2 < 1f)
			{
				result = (1f - num2) * -5f;
			}
		}
		if (inDriver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.CarHappinessNegative))
		{
			result = 5f;
		}
		return result;
	}

	private float GetCarComparisionWeight(float inDelta, ContractPerson.Status inStatus)
	{
		float result = 0f;
		switch (inStatus)
		{
		case ContractPerson.Status.Equal:
			if (inDelta > 0f)
			{
				result = 4f;
			}
			else if (inDelta > -50f)
			{
				result = 3f;
			}
			else
			{
				result = 1f;
			}
			break;
		case ContractPerson.Status.One:
			if (inDelta > 50f)
			{
				result = 3f;
			}
			else if (inDelta > 0f)
			{
				result = 1f;
			}
			else
			{
				result = 0f;
			}
			break;
		case ContractPerson.Status.Two:
		case ContractPerson.Status.Reserve:
			if (inDelta > 0f)
			{
				result = 4f;
			}
			else if (inDelta > -50f)
			{
				result = 3f;
			}
			else
			{
				result = 2f;
			}
			break;
		}
		return result;
	}

	public string GetTeamCarComparisonCriteria(float inDelta, ContractPerson.Status inStatus)
	{
		switch (inStatus)
		{
		case ContractPerson.Status.Equal:
			if (inDelta > 0f)
			{
				return "Better";
			}
			if (inDelta > -50f)
			{
				return "Equal";
			}
			return "Worse";
		case ContractPerson.Status.One:
			if (inDelta > 50f)
			{
				return "Better";
			}
			if (inDelta > 0f)
			{
				return "Equal";
			}
			return "Worse";
		case ContractPerson.Status.Two:
		case ContractPerson.Status.Reserve:
			if (inDelta > 0f)
			{
				return "Better";
			}
			if (inDelta > -50f)
			{
				return "Equal";
			}
			return "Worse";
		default:
			return "Equal";
		}
	}

	public static Color GetColor(CarOpinion.Happiness inHappiness)
	{
		switch (inHappiness)
		{
		case CarOpinion.Happiness.Angry:
			return UIConstants.negativeColor;
		case CarOpinion.Happiness.Unhappy:
			return UIConstants.negativeColor;
		case CarOpinion.Happiness.Content:
			return UIConstants.colorBandYellow;
		case CarOpinion.Happiness.Happy:
			return UIConstants.positiveColor;
		case CarOpinion.Happiness.Delighted:
			return UIConstants.positiveColor;
		default:
			return Color.white;
		}
	}

	private void CalculateMoraleHits(Driver inDriver)
	{
		this.mOverallMoraleHit = this.GetMoraleHit(this.mDriverOverallHappiness);
		this.mOverallMoraleHit += this.mRandomOverallMoraleHitModifier / 100f;
		this.mAgainstOtherCarMoraleHit = this.GetMoraleHit(this.mDriverAgainstOtherCarHappiness);
		this.mAgainstOtherCarMoraleHit += this.GetTraitsCarMoraleHit(inDriver) / 100f;
		this.mAgainstOtherCarMoraleHit += this.mRandomAgainstOtherCarMoraleHitModifier / 100f;
	}

	private float GetMoraleHit(CarOpinion.Happiness inHappiness)
	{
		switch (inHappiness)
		{
		case CarOpinion.Happiness.Angry:
			return -0.05f;
		case CarOpinion.Happiness.Unhappy:
			return -0.025f;
		case CarOpinion.Happiness.Content:
			return 0f;
		case CarOpinion.Happiness.Happy:
			return 0.025f;
		case CarOpinion.Happiness.Delighted:
			return 0.05f;
		default:
			return 0f;
		}
	}

	public void ApplyMoraleHit()
	{
		this.mDriver.ModifyMorale(this.mOverallMoraleHit + this.mAgainstOtherCarMoraleHit, "PSG_10011021", false);
	}

	private CarOpinion.Happiness mDriverOverallHappiness;

	private CarOpinion.Happiness mDriverAgainstOtherCarHappiness;

	private CarOpinion.Happiness mDriverAverageHappiness;

	private DialogRule mOverallCarComment;

	private DialogRule mAgainstOtherCarComment;

	private Driver mDriver;

	private float mOverallMoraleHit;

	private float mAgainstOtherCarMoraleHit;

	private readonly float mRandomOverallMoraleHitModifier = (float)RandomUtility.GetRandomInc(-1, 1);

	private readonly float mRandomAgainstOtherCarMoraleHitModifier = (float)RandomUtility.GetRandomInc(-1, 1);

	public enum Happiness
	{
		[LocalisationID("PSG_10002061")]
		Angry,
		[LocalisationID("PSG_10001551")]
		Unhappy,
		[LocalisationID("PSG_10002044")]
		Content,
		[LocalisationID("PSG_10001365")]
		Happy,
		[LocalisationID("PSG_10010104")]
		Delighted
	}

	public enum CommentType
	{
		OverallCarComment,
		AgainstOtherCarComment
	}
}
