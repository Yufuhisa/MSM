using System;
using System.Collections.Generic;
using System.Text;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ContractDesiredValuesHelper
{
	public ContractDesiredValuesHelper()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static ContractDesiredValuesHelper()
	{
	}

	public void Start(Person inPerson)
	{
		this.mTargetPerson = inPerson;
	}

	public void SetNegotiationType(ContractNegotiationScreen.NegotatiationType inNegotiationType)
	{
		this.mNegotiationType = inNegotiationType;
	}

	public bool isInterestedToTalk
	{
		get
		{
			return this.mIsInterestedToTalk;
		}
	}

	public float negotiationWeight
	{
		get
		{
			return this.mNegotiationWeight;
		}
	}

	public float desiredWages
	{
		get
		{
			return this.mDesiredWages;
		}
	}

	public float baseDesiredWages
	{
		get
		{
			return this.mBaseDesiredWages;
		}
	}

	public float maximumOfferableWage
	{
		get
		{
			float num = this.mBaseDesiredWages * ContractDesiredValuesHelper.evaluationWeightings.maximumWageMultiplier;
			if (!this.mTargetPerson.IsFreeAgent() && num < (float)this.mTargetPerson.contract.yearlyWages)
			{
				return (float)this.mTargetPerson.contract.yearlyWages * ContractDesiredValuesHelper.evaluationWeightings.maximumCurrentWageMultiplier;
			}
			return num;
		}
	}

	public float minimumOfferableWage
	{
		get
		{
			return this.mBaseDesiredWages * ContractDesiredValuesHelper.evaluationWeightings.minimumWageMultiplier;
		}
	}

	public float desiredSignOnFee
	{
		get
		{
			return this.mDesiredSignOnFee;
		}
	}

	public float maximumOfferableSignOnFee
	{
		get
		{
			return this.mDesiredSignOnFee * ContractDesiredValuesHelper.evaluationWeightings.maximumQualifyingBonusMultiplier;
		}
	}

	public float[] desiredRaceBonus
	{
		get
		{
			return this.mDesiredRaceBonus;
		}
	}

	public float maximumOfferableRaceBonus
	{
		get
		{
			return this.mDesiredRaceBonus[1] * ContractDesiredValuesHelper.evaluationWeightings.maximumRaceBonusMultiplier;
		}
	}

	public float maximumRaceBonusMultiplier
	{
		get
		{
			return ContractDesiredValuesHelper.evaluationWeightings.maximumRaceBonusMultiplier;
		}
	}

	public float[] desiredQualifyingBonus
	{
		get
		{
			return this.mDesiredQualifyingBonus;
		}
	}

	public float maximumOfferableQualifyingBonus
	{
		get
		{
			return this.mDesiredQualifyingBonus[1] * ContractDesiredValuesHelper.evaluationWeightings.maximumQualifyingBonusMultiplier;
		}
	}

	public float maximumQualifyingBonusMultiplier
	{
		get
		{
			return ContractDesiredValuesHelper.evaluationWeightings.maximumQualifyingBonusMultiplier;
		}
	}

	public ContractPerson.ContractLength desiredContractLength
	{
		get
		{
			return this.mDesiredContractLength;
		}
	}

	public ContractPerson.Status desiredStatus
	{
		get
		{
			return this.mDesiredStatus;
		}
	}

	public ContractPerson.BuyoutClauseSplit desiredBuyoutSplit
	{
		get
		{
			return this.mDesiredBuyoutSplit;
		}
	}

	public bool wantSignOnFee
	{
		get
		{
			return this.mWantSignOnFee;
		}
	}

	public bool wantsQualifyingBonus
	{
		get
		{
			return this.mWantsQualifyingBonus;
		}
	}

	public bool wantsRaceBonus
	{
		get
		{
			return this.mWantsRaceBonus;
		}
	}

	public float minimumContractValueAcceptance
	{
		get
		{
			return this.mMinimumContractValueAcceptance;
		}
	}

	public ContractVariablesData.RangeType wageRangeType
	{
		get
		{
			return this.mWageRangeType;
		}
	}

	public ContractVariablesData.RangeType signOnFeeRangeType
	{
		get
		{
			return this.mSignOnFeeRangeType;
		}
	}

	public void CalculateDesiredContractValues(ContractNegotiationScreen.NegotatiationType inNegotiationType, Team inTeam)
	{
		switch (inTeam.championship.series)
		{
		case Championship.Series.EnduranceSeries:
			this.mSeriesModifier = 0.5f;
			break;
		}
		this.mNegotiationType = inNegotiationType;
		ContractVariablesData variablesData = ContractDesiredValuesHelper.variablesContainer.GetVariablesData(this.mTargetPerson);
		this.mNegotiationWeight = this.GetNegotiationWeightForTargetPerson(inTeam);
		this.mWageRangeType = this.CalculateDesiredWagesRange(this.mTargetPerson, this.negotiationWeight);
		this.mDesiredWages = this.CalculateDesiredWage(this.mTargetPerson) * this.mScalarForValues * this.mSeriesModifier;
		this.mBaseDesiredWages *= this.mScalarForValues * this.mSeriesModifier;
		this.mSignOnFeeRangeType = this.CalculateDesiredSignOnFee(this.negotiationWeight);
		this.mDesiredSignOnFee = variablesData.GetSignOnFee(this.mSignOnFeeRangeType) * this.mScalarForValues * this.mSeriesModifier;
		if (this.mTargetPerson is Driver)
		{
			this.mDesiredStatus = this.CalculateDesiredStatus(this.mTargetPerson as Driver);
		}
		this.mDesiredContractLength = this.CalculateDesiredContractLength(this.mTargetPerson, this.negotiationWeight, inTeam);
		this.mDesiredBuyoutSplit = this.CalculateDesiredBuyoutClause(this.mTargetPerson, this.negotiationWeight);
		this.CalculateDesiredQualifyingBonus(this.negotiationWeight);
		Array.Copy(variablesData.GetQualifyingBonus(), this.mDesiredQualifyingBonus, 2);
		this.mDesiredQualifyingBonus[0] *= this.mScalarForValues * this.mSeriesModifier;
		this.mDesiredQualifyingBonus[1] *= this.mScalarForValues * this.mSeriesModifier;
		this.CalculateDesiredRaceBonus(this.negotiationWeight);
		Array.Copy(variablesData.GetRaceBonus(), this.mDesiredRaceBonus, 2);
		this.mDesiredRaceBonus[0] *= this.mScalarForValues * this.mSeriesModifier;
		this.mDesiredRaceBonus[1] *= this.mScalarForValues * this.mSeriesModifier;
		this.mMinimumContractValueAcceptance = Mathf.Lerp(ContractDesiredValuesHelper.evaluationWeightings.minimumContractValueRange, ContractDesiredValuesHelper.evaluationWeightings.maximumContractValueRange, this.normalizedNegotiationWeight);
		this.CalculateWantsToTalk(inTeam);
		this.CalculateDesiredChampionship(null);
	}

	public string DebugLogDesiredEvaluationValues()
	{
		StringBuilder builder = GameUtility.GlobalStringBuilderPool.GetBuilder();
		builder.Append(this.mTargetPerson.name);
		builder.AppendFormat(" - Interested To Talk: " + this.mIsInterestedToTalk.ToString(), new object[0]);
		builder.AppendLine();
		builder.AppendFormat("Negotiation weight: {0}", this.mNegotiationWeight.ToString());
		builder.AppendLine();
		builder.AppendFormat("Desired Wage: {0}", this.mDesiredWages);
		builder.AppendLine();
		builder.AppendFormat("Desired Base Wage: {0}", this.mBaseDesiredWages);
		builder.AppendLine();
		builder.AppendFormat("Desired Sign On Fee: {0}", this.mWantSignOnFee.ToString());
		builder.AppendFormat(", amount: {0}", this.mDesiredSignOnFee.ToString());
		builder.AppendLine();
		builder.AppendFormat("Desired Status: {0}", this.mDesiredStatus.ToString());
		builder.AppendLine();
		builder.AppendFormat("Desired Length: {0}", this.mDesiredContractLength.ToString());
		builder.AppendLine();
		builder.AppendFormat("Desired Buyout Clause Split: {0}", this.mDesiredBuyoutSplit.ToString());
		builder.AppendLine();
		builder.AppendFormat("Desired Race Bonus: {0}", this.mWantsRaceBonus);
		builder.AppendFormat(", amount: {0}, {1}", this.mDesiredRaceBonus[0], this.mDesiredRaceBonus[1]);
		builder.AppendLine();
		builder.AppendFormat("Desired Qualifying Bonus: {0}", this.mWantsQualifyingBonus);
		builder.AppendFormat(", amount: {0}, {1}", this.mDesiredQualifyingBonus[0], this.mDesiredQualifyingBonus[1]);
		builder.AppendLine();
		builder.AppendFormat("Minimum Acceptable Contract value: {0}", this.mMinimumContractValueAcceptance);
		builder.AppendLine();
		builder.AppendFormat("Desired Championship: Tier {0}", this.mDesiredChampionship + 1);
		builder.AppendLine();
		string text = builder.ToString();
		GameUtility.GlobalStringBuilderPool.ReturnBuilder(builder);
		global::Debug.Log(text, null);
		return text;
	}

	public void CalculateWantsToTalk(Team inTeam)
	{
		switch (this.mNegotiationType)
		{
		case ContractNegotiationScreen.NegotatiationType.NewDriver:
		{
			float num = 0f;
			float targetPersonStars = this.targetPersonStars;
			float num2 = this.playerTeamStars(inTeam);
			if (num2 > targetPersonStars)
			{
				num += -(num2 - targetPersonStars + ContractDesiredValuesHelper.evaluationWeightings.newDriverFirstConditionModifier);
			}
			else if (num2 < targetPersonStars)
			{
				num += targetPersonStars - num2 + ContractDesiredValuesHelper.evaluationWeightings.newDriverSecondConditionModifier;
			}
			float targetPersonChampionshipTier = this.targetPersonChampionshipTier;
			float num3 = this.playerChampionshipTier(inTeam);
			if (num3 > targetPersonChampionshipTier)
			{
				this.mIsInterestedToTalk = false;
			}
			else
			{
				if (num3 < targetPersonChampionshipTier)
				{
					num += -(targetPersonChampionshipTier - num3);
				}
				num += this.GetDriverStatusNegotiationWeight(this.mTargetPerson as Driver);
				num += this.GetWinsNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.driverWinsWeighting);
				num += this.GetUnemploymentWeight(this.mTargetPerson);
				this.mIsInterestedToTalk = (num < ContractDesiredValuesHelper.evaluationWeightings.newDriverInterestedToTalkThreshold);
			}
			break;
		}
		case ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed:
		case ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed:
			this.mIsInterestedToTalk = true;
			break;
		case ContractNegotiationScreen.NegotatiationType.RenewDriver:
		{
			float num4 = 0f;
			if ((float)this.mTargetPerson.GetAge() > ContractDesiredValuesHelper.evaluationWeightings.existingDriverInterestedToTalkAgeThreshold)
			{
				num4 = 10f;
			}
			else
			{
				num4 += this.GetMoraleNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingDriverMoraleResults);
				num4 += this.GetAgeNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingDriverAgeResults);
				num4 += this.GetDriverStatusNegotiationWeight(this.mTargetPerson as Driver);
				num4 += this.GetMarketabilityNegotiationWeight(this.mTargetPerson as Driver);
				num4 += this.GetManagerLoyaltyNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingDriverManagerLoyaltyWeighting);
				num4 += this.GetManagerFinancesNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.driverManagerFinanceWeighting);
			}
			this.mIsInterestedToTalk = (num4 < ContractDesiredValuesHelper.evaluationWeightings.existingDriverInterestedToTalkThreshold);
			break;
		}
		case ContractNegotiationScreen.NegotatiationType.NewStaffEmployed:
		{
			float num5 = 0f;
			float targetPersonStars2 = this.targetPersonStars;
			float num6 = this.playerTeamStars(inTeam);
			if (num6 > targetPersonStars2)
			{
				num5 += -(num6 - targetPersonStars2 + ContractDesiredValuesHelper.evaluationWeightings.newStaffFirstConditionModifier);
			}
			else if (num6 < targetPersonStars2)
			{
				num5 += targetPersonStars2 - num6 + ContractDesiredValuesHelper.evaluationWeightings.newStaffSecondConditionModifier;
			}
			float targetPersonChampionshipTier2 = this.targetPersonChampionshipTier;
			float num7 = this.playerChampionshipTier(inTeam);
			if (num7 > targetPersonChampionshipTier2)
			{
				this.mIsInterestedToTalk = false;
			}
			else
			{
				if (num7 < targetPersonChampionshipTier2)
				{
					num5 += -(targetPersonChampionshipTier2 - num7);
				}
				num5 += this.GetManagerLoyaltyNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.newStaffManagerLoyaltyWeighting);
				num5 += this.GetWinsNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.newStaffWinsWeighting);
				num5 += this.GetUnemploymentWeight(this.mTargetPerson);
				this.mIsInterestedToTalk = (num5 < ContractDesiredValuesHelper.evaluationWeightings.newStaffInterestedToTalkThreshold);
			}
			break;
		}
		case ContractNegotiationScreen.NegotatiationType.RenewStaff:
		{
			float num8 = 0f;
			if ((float)this.mTargetPerson.GetAge() > ContractDesiredValuesHelper.evaluationWeightings.existingStaffInterestedToTalkAgeThreshold)
			{
				num8 = 10f;
			}
			else
			{
				num8 += this.GetAgeNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingDriverAgeResults);
				num8 += this.GetPersonStarsNegotiationWeight(this.mTargetPerson, inTeam);
				num8 += this.GetManagerLoyaltyNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingStaffManagerLoyaltyWeighting);
				num8 += this.GetManagerFinancesNegotiationWeight(this.mTargetPerson, ContractDesiredValuesHelper.evaluationWeightings.existingStaffManagerFinancesWeighting);
			}
			this.mIsInterestedToTalk = (num8 < ContractDesiredValuesHelper.evaluationWeightings.existingStaffInterestedToTalkThreshold);
			break;
		}
		}
	}

	public int CalculateDesiredChampionship(Team inTeam = null)
	{
		float num = this.mTargetPerson.GetStats().GetAbility();
		bool flag = this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewDriver;
		flag |= (this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewStaff);
		if (inTeam != null)
		{
			flag &= inTeam.IsPlayersTeam();
		}
		if (this.mTargetPerson.IsFreeAgent() || flag)
		{
			num += ContractDesiredValuesHelper.evaluationWeightings.desiredChampionshipUnemployedStarModifier;
		}
		this.mDesiredChampionship = ContractDesiredValuesHelper.evaluationWeightings.desiredChampionshipAgainstStarsValue[Mathf.Max(0, Mathf.FloorToInt(num))];
		return this.mDesiredChampionship;
	}

	private float GetNegotiationWeightForTargetPerson(Team inTeam)
	{
		List<float> list = new List<float>();
		Person person = this.mTargetPerson;
		switch (this.mNegotiationType)
		{
		case ContractNegotiationScreen.NegotatiationType.NewDriver:
		case ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed:
			list.Add(this.GetPersonStarsNegotiationWeight(person, inTeam));
			list.Add(this.GetAgeNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newDriverAgeResults));
			list.Add(this.GetMoraleNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newDriverMoraleResults));
			list.Add(this.GetCurrentChampionshipNegotiationWeight(person, inTeam));
			list.Add(this.GetUnemploymentWeight(person));
			list.Add(this.GetDriverStatusNegotiationWeight(person as Driver));
			list.Add(this.GetWinsNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.driverWinsWeighting));
			list.Add(this.GetRacesNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.driverRacesWeighting));
			list.Add(this.GetAgeToStarsNegotiationWeight(person as Driver, inTeam));
			list.Add(this.GetTeamStarsNegotiationWeight(person, inTeam));
			list.Add(this.RandomChangeNegotiationWeight());
			list.Add(this.GetManagerLoyaltyNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newDriverManagerLoyaltyWeighting));
			list.Add(this.GetPotentialWeightDriver(person as Driver));
			break;
		case ContractNegotiationScreen.NegotatiationType.RenewDriver:
			list.Add(this.GetPersonStarsNegotiationWeight(person, inTeam));
			list.Add(this.GetAgeNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingDriverAgeResults));
			list.Add(this.GetMoraleNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingDriverMoraleResults));
			list.Add(this.GetDriverStatusNegotiationWeight(person as Driver));
			list.Add(this.GetWinsNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.driverWinsWeighting));
			list.Add(this.GetMarketabilityNegotiationWeight(person as Driver));
			list.Add(this.GetAgeToStarsNegotiationWeight(person as Driver, inTeam));
			list.Add(this.RandomChangeNegotiationWeight());
			list.Add(this.GetManagerLoyaltyNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingDriverManagerLoyaltyWeighting));
			list.Add(this.GetManagerFinancesNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.driverManagerFinanceWeighting));
			list.Add(this.GetMechanicRelationshipWeight(person as Driver));
			list.Add(this.GetPotentialWeightDriver(person as Driver));
			break;
		case ContractNegotiationScreen.NegotatiationType.NewStaffEmployed:
		case ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed:
			list.Add(this.GetPersonStarsNegotiationWeight(person, inTeam));
			list.Add(this.GetAgeNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newStaffAgeResults));
			list.Add(this.GetCurrentChampionshipNegotiationWeight(person, inTeam));
			list.Add(this.GetUnemploymentWeight(person));
			list.Add(this.GetWinsNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newStaffWinsWeighting));
			list.Add(this.GetRacesNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.staffRacesWeighting));
			list.Add(this.GetTeamStarsNegotiationWeight(person, inTeam));
			list.Add(this.GetManagerLoyaltyNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newStaffManagerLoyaltyWeighting));
			list.Add(this.GetManagerFinancesNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.newStaffManagerFinancesWeighting));
			list.Add(this.RandomChangeNegotiationWeight());
			list.Add(this.GetPotentialWeightStaff(person));
			break;
		case ContractNegotiationScreen.NegotatiationType.RenewStaff:
			list.Add(this.GetPersonStarsNegotiationWeight(person, inTeam));
			list.Add(this.GetAgeNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingStaffAgeResults));
			list.Add(this.GetWinsNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingStaffWinsWeighting));
			list.Add(this.RandomChangeNegotiationWeight());
			list.Add(this.GetManagerLoyaltyNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingStaffManagerLoyaltyWeighting));
			list.Add(this.GetManagerFinancesNegotiationWeight(person, ContractDesiredValuesHelper.evaluationWeightings.existingStaffManagerFinancesWeighting));
			list.Add(this.GetPotentialWeightStaff(person));
			break;
		}
		return this.GetAverageNegotiationWeight(list);
	}

	private float GetAverageNegotiationWeight(List<float> inAllAspectNegotiationWeights)
	{
		float num = 0f;
		for (int i = 0; i < inAllAspectNegotiationWeights.Count; i++)
		{
			num += inAllAspectNegotiationWeights[i];
		}
		return Mathf.Clamp(num, -5f, 5f);
	}

	private float GetPersonStarsNegotiationWeight(Person inPerson, Team inTeam)
	{
		float starsStat = inTeam.GetStarsStat();
		float ability = inPerson.GetStats().GetAbility();
		return ability - starsStat;
	}

	private float GetAgeNegotiationWeight(Person inPerson, Dictionary<int, int> inLookupTableForAgeResults)
	{
		int age = inPerson.GetAge();
		return this.EvaluateLookupTableAgainstValue(age, inLookupTableForAgeResults);
	}

	private float GetMoraleNegotiationWeight(Person inPerson, Dictionary<int, int> inLookupTableForMoraleResults)
	{
		int inValue = Mathf.RoundToInt(inPerson.GetMorale() * 100f);
		return this.EvaluateLookupTableAgainstValue(inValue, inLookupTableForMoraleResults);
	}

	private float EvaluateLookupTableAgainstValue(int inValue, Dictionary<int, int> inLookupTable)
	{
		int num = 0;
		int count = inLookupTable.Keys.Count;
		int num2 = 0;
		foreach (KeyValuePair<int, int> keyValuePair in inLookupTable)
		{
			if (num2 == count - 1)
			{
				num = keyValuePair.Value;
				break;
			}
			if (inValue < keyValuePair.Key)
			{
				num = keyValuePair.Value;
				break;
			}
			num2++;
		}
		return (float)num;
	}

	private float GetCurrentChampionshipNegotiationWeight(Person inPerson, Team inTeam)
	{
		float currentChampionshipDifference = this.GetCurrentChampionshipDifference(inPerson, inTeam);
		if (inPerson is Driver)
		{
			return currentChampionshipDifference;
		}
		return currentChampionshipDifference + (float)ContractDesiredValuesHelper.evaluationWeightings.staffCurrentChampionshipModifier;
	}

	private float GetUnemploymentWeight(Person inPerson)
	{
		float result = 0f;
		if (inPerson.IsFreeAgent())
		{
			if (inPerson is Driver)
			{
				result = (float)ContractDesiredValuesHelper.evaluationWeightings.driverUnemployedWeightResult;
			}
			else
			{
				result = (float)ContractDesiredValuesHelper.evaluationWeightings.staffUnemployedWeightResult;
			}
		}
		return result;
	}

	private float GetCurrentChampionshipDifference(Person inPerson, Team inTeam)
	{
		float num = 0f;
		if (!inPerson.IsFreeAgent())
		{
			num = (float)(inPerson.contract.GetTeam().championship.championshipOrder + 1);
		}
		float num2 = (float)(inTeam.championship.championshipOrder + 1);
		return num2 - num;
	}

	private float GetDriverStatusNegotiationWeight(Driver inDriver)
	{
		if (!inDriver.IsMainDriver())
		{
			return (float)ContractDesiredValuesHelper.evaluationWeightings.driverReserveWeightResult;
		}
		return 0f;
	}

	private float GetWinsNegotiationWeight(Person inPerson, float inWinsWeighting)
	{
		int totalCareerWins = inPerson.careerHistory.GetTotalCareerWins();
		float f = (float)totalCareerWins * inWinsWeighting;
		return (float)Mathf.FloorToInt(f);
	}

	private float GetRacesNegotiationWeight(Person inPerson, float inRacesWeighting)
	{
		int totalCareerRaces = inPerson.careerHistory.GetTotalCareerRaces();
		float f = (float)totalCareerRaces * inRacesWeighting;
		return (float)Mathf.FloorToInt(f);
	}

	private float GetAgeToStarsNegotiationWeight(Driver inDriver, Team inTeam)
	{
		if ((float)inDriver.GetAge() < ContractDesiredValuesHelper.evaluationWeightings.driverAgeToStarsMaxAgeRequired)
		{
			return this.GetPersonStarsNegotiationWeight(inDriver, inTeam) * ContractDesiredValuesHelper.evaluationWeightings.driverAgeToStarslWeighting;
		}
		return 0f;
	}

	private float GetPotentialWeightDriver(Driver inDriver)
	{
		return inDriver.GetStats().GetPotential() * ContractDesiredValuesHelper.evaluationWeightings.driverPotentialWeighting;
	}

	private float GetPotentialWeightStaff(Person inPerson)
	{
		return inPerson.GetStats().GetPotential() * ContractDesiredValuesHelper.evaluationWeightings.staffPotentialWeighting;
	}

	private float GetTeamStarsNegotiationWeight(Person inPerson, Team inTeam)
	{
		float num = 0f;
		if (!inPerson.IsFreeAgent())
		{
			Team team = inPerson.contract.GetTeam();
			num = team.GetStarsStat();
		}
		float starsStat = inTeam.GetStarsStat();
		return num - starsStat + num;
	}

	private float RandomChangeNegotiationWeight()
	{
		return (float)this.mRandomChangeNegotiationWeight;
	}

	private float GetManagerLoyaltyNegotiationWeight(Person inPerson, float inLoyaltyWeighting)
	{
		bool flag = this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewDriver;
		flag |= (this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewStaff);
		float num = 0f;
		if (flag)
		{
			num = Game.instance.player.stats.loyalty;
		}
		else if (!inPerson.IsFreeAgent())
		{
			num = inPerson.contract.GetTeam().teamPrincipal.stats.loyalty;
		}
		return -Mathf.Floor(num / inLoyaltyWeighting);
	}

	private float GetManagerFinancesNegotiationWeight(Person inPerson, float inFinanceWeighting)
	{
		bool flag = this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewDriver;
		flag |= (this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewStaff);
		float num = 0f;
		if (flag)
		{
			num = Game.instance.player.stats.financial;
		}
		else if (!inPerson.IsFreeAgent())
		{
			num = inPerson.contract.GetTeam().teamPrincipal.stats.financial;
		}
		return -Mathf.Floor(num / inFinanceWeighting);
	}

	private float GetMarketabilityNegotiationWeight(Driver inDriver)
	{
		float num = inDriver.GetDriverStats().marketability * 100f;
		return Mathf.Floor(num * ContractDesiredValuesHelper.evaluationWeightings.driverMarketabilityWeighting);
	}

	private float GetMechanicRelationshipWeight(Driver inDriver)
	{
		if (!inDriver.IsFreeAgent() && inDriver.IsMainDriver())
		{
			Mechanic mechanicOfDriver = inDriver.contract.GetTeam().GetMechanicOfDriver(inDriver);
			return -(mechanicOfDriver.GetRelationshipAmmount() * ContractDesiredValuesHelper.evaluationWeightings.driverMechanicRelationshipWeighting);
		}
		return 0f;
	}

	private float CalculateDesiredWage(Person inPerson)
	{
		this.mBaseDesiredWages = ContractDesiredValuesHelper.variablesContainer.GetBaseDesiredWageForPerson(inPerson);
		Driver driver = inPerson as Driver;
		if (driver != null)
		{
			this.mBaseDesiredWages += (float)driver.GetDesiredEarnings() / (this.mScalarForValues * this.mSeriesModifier);
		}
		float num = Mathf.Lerp(-ContractDesiredValuesHelper.evaluationWeightings.baseDesiredWageMultiplier, ContractDesiredValuesHelper.evaluationWeightings.baseDesiredWageMultiplier, this.normalizedNegotiationWeight);
		float num2 = this.mBaseDesiredWages * num;
		float num3 = inPerson.GetStats().GetAbilityPotential() / 5f;
		float num4 = this.mBaseDesiredWages * num3 * ContractDesiredValuesHelper.evaluationWeightings.baseDesiredWagePotentialMultiplier;
		return this.mBaseDesiredWages + num2 + num4;
	}

	private ContractVariablesData.RangeType CalculateDesiredWagesRange(Person inPerson, float inNegotiationWeight)
	{
		if (inPerson is Driver)
		{
			inNegotiationWeight += ContractDesiredValuesHelper.evaluationWeightings.driverDesiredWageModifier;
		}
		return this.GetRangeAgainstThreshold(inNegotiationWeight);
	}

	private ContractVariablesData.RangeType GetRangeAgainstThreshold(float inNegotiationWeight)
	{
		if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.lowRangeWageThreshold)
		{
			return ContractVariablesData.RangeType.RangeD;
		}
		if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.mediumRangeWageThreshold)
		{
			return ContractVariablesData.RangeType.RangeC;
		}
		if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.highRangeWageThreshold)
		{
			return ContractVariablesData.RangeType.RangeB;
		}
		return ContractVariablesData.RangeType.RangeA;
	}

	private ContractPerson.Status CalculateDesiredStatus(Driver inDriver)
	{
		if (inDriver.IsFreeAgent())
		{
			return ContractPerson.Status.Reserve;
		}
		if (!inDriver.IsPlayersDriver())
		{
			return inDriver.contract.currentStatus;
		}
		ContractPerson.Status currentStatus = inDriver.contract.currentStatus;
		ContractPerson.Status proposedStatus = inDriver.contract.proposedStatus;
		if (currentStatus == ContractPerson.Status.One || proposedStatus == ContractPerson.Status.One)
		{
			return ContractPerson.Status.One;
		}
		if (currentStatus == ContractPerson.Status.Equal || proposedStatus == ContractPerson.Status.Equal)
		{
			return ContractPerson.Status.Equal;
		}
		if (currentStatus == ContractPerson.Status.Two || proposedStatus == ContractPerson.Status.Two)
		{
			return ContractPerson.Status.Two;
		}
		return ContractPerson.Status.Reserve;
	}

	private ContractPerson.ContractLength CalculateDesiredContractLength(Person inPerson, float inNegotiationWeight, Team inTeam)
	{
		if (inPerson is Driver)
		{
			float ageToStarsNegotiationWeight = this.GetAgeToStarsNegotiationWeight(inPerson as Driver, inTeam);
			if (ageToStarsNegotiationWeight > ContractDesiredValuesHelper.evaluationWeightings.driverDesiredContractLengthPotentialThreshold)
			{
				return ContractPerson.ContractLength.Short;
			}
			inNegotiationWeight += ContractDesiredValuesHelper.evaluationWeightings.driverDesiredContractLengthModifier;
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.driverDesiredContractLengthLowRangeThreshold)
			{
				return ContractPerson.ContractLength.Long;
			}
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.driverDesiredContractLengthMediumRangeThreshold)
			{
				return ContractPerson.ContractLength.Medium;
			}
			return ContractPerson.ContractLength.Short;
		}
		else
		{
			inNegotiationWeight += ContractDesiredValuesHelper.evaluationWeightings.staffDesiredContractLengthModifier;
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.staffDesiredContractLengthLowRangeThreshold)
			{
				return ContractPerson.ContractLength.Long;
			}
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.staffDesiredContractLengthMediumRangeThreshold)
			{
				return ContractPerson.ContractLength.Medium;
			}
			return ContractPerson.ContractLength.Short;
		}
	}

	private ContractPerson.BuyoutClauseSplit CalculateDesiredBuyoutClause(Person inPerson, float inNegotiationWeight)
	{
		if (inPerson is Driver)
		{
			if (!inPerson.IsFreeAgent())
			{
				float num = (float)inPerson.contract.GetContractTerminationCost();
				if (num < this.mDesiredWages)
				{
					return ContractPerson.BuyoutClauseSplit.PersonPaysAll;
				}
				if (num < this.mDesiredSignOnFee)
				{
					return ContractPerson.BuyoutClauseSplit.PersonPaysAll;
				}
			}
			if (inPerson.GetStats().GetAbility() > ContractDesiredValuesHelper.evaluationWeightings.driverDesiredBuyoutHighRangeStarsThreshold)
			{
				return ContractPerson.BuyoutClauseSplit.TeamPaysAll;
			}
			if (inPerson.GetStats().GetAbility() > ContractDesiredValuesHelper.evaluationWeightings.driverDesiredBuyoutMediumRangeStarsThreshold)
			{
				return ContractPerson.BuyoutClauseSplit.EvenSplit;
			}
			return ContractPerson.BuyoutClauseSplit.PersonPaysAll;
		}
		else
		{
			if (!inPerson.IsFreeAgent())
			{
				float ability = inPerson.GetStats().GetAbility();
				float financial = inPerson.contract.GetTeam().teamPrincipal.stats.financial;
				if (ability + financial > ContractDesiredValuesHelper.evaluationWeightings.staffDesiredBuyoutStarsManagerFinancesThreshold)
				{
					return ContractPerson.BuyoutClauseSplit.TeamPaysAll;
				}
			}
			inNegotiationWeight += ContractDesiredValuesHelper.evaluationWeightings.staffDesiredBuyoutModifier;
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.staffDesiredBuyoutLowRangeThreshold)
			{
				return ContractPerson.BuyoutClauseSplit.PersonPaysAll;
			}
			if (inNegotiationWeight < ContractDesiredValuesHelper.evaluationWeightings.staffDesiredBuyoutMediumRangeThreshold)
			{
				return ContractPerson.BuyoutClauseSplit.EvenSplit;
			}
			return ContractPerson.BuyoutClauseSplit.TeamPaysAll;
		}
	}

	private ContractVariablesData.RangeType CalculateDesiredSignOnFee(float inNegotiationWeight)
	{
		this.mWantSignOnFee = (inNegotiationWeight > ContractDesiredValuesHelper.evaluationWeightings.wantSignOnFeeThreshold);
		inNegotiationWeight += ContractDesiredValuesHelper.evaluationWeightings.desiredSignOnFeeModifier;
		return this.GetRangeAgainstThreshold(inNegotiationWeight);
	}

	private void CalculateDesiredQualifyingBonus(float inNegotiationWeight)
	{
		this.mWantsQualifyingBonus = (inNegotiationWeight > ContractDesiredValuesHelper.evaluationWeightings.wantQualifyingBonusThreshold);
	}

	private void CalculateDesiredRaceBonus(float inNegotiationWeight)
	{
		this.mWantsRaceBonus = (inNegotiationWeight > ContractDesiredValuesHelper.evaluationWeightings.wantRaceBonusThreshold);
	}

	private float targetPersonStars
	{
		get
		{
			return this.mTargetPerson.GetStats().GetAbility();
		}
	}

	private float playerTeamStars(Team inTeam)
	{
		return inTeam.GetStarsStat();
	}

	private float targetPersonChampionshipTier
	{
		get
		{
			if (!this.mTargetPerson.IsFreeAgent())
			{
				return (float)(this.mTargetPerson.contract.GetTeam().championship.championshipOrder + 1);
			}
			return 0f;
		}
	}

	private float playerChampionshipTier(Team inTeam)
	{
		return (float)(inTeam.championship.championshipOrder + 1);
	}

	private float normalizedNegotiationWeight
	{
		get
		{
			float num = this.mNegotiationWeight + 5f;
			return num / 10f;
		}
	}

	private readonly float mScalarForValues = 1000000f;

	private float mBaseDesiredWages;

	private float mDesiredWages;

	private float mDesiredSignOnFee;

	private float[] mDesiredRaceBonus = new float[2];

	private float[] mDesiredQualifyingBonus = new float[2];

	private ContractPerson.ContractLength mDesiredContractLength;

	private ContractPerson.Status mDesiredStatus = ContractPerson.Status.Reserve;

	private ContractPerson.BuyoutClauseSplit mDesiredBuyoutSplit = ContractPerson.BuyoutClauseSplit.PersonPaysAll;

	private bool mWantSignOnFee;

	private bool mWantsQualifyingBonus;

	private bool mWantsRaceBonus;

	private bool mIsInterestedToTalk;

	private ContractVariablesData.RangeType mWageRangeType = ContractVariablesData.RangeType.RangeD;

	private ContractVariablesData.RangeType mSignOnFeeRangeType = ContractVariablesData.RangeType.RangeD;

	private float mNegotiationWeight;

	private float mMinimumContractValueAcceptance;

	private float mSeriesModifier = 1f;

	private static readonly ContractEvaluationWeightings evaluationWeightings = new ContractEvaluationWeightings();

	private static readonly ContractVariablesContainer variablesContainer = new ContractVariablesContainer();

	private Person mTargetPerson;

	private ContractNegotiationScreen.NegotatiationType mNegotiationType;

	private int mDesiredChampionship;

	private readonly int mRandomChangeNegotiationWeight = RandomUtility.GetRandomInc(-1, 1);
}
