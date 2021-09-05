using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Person : Entity, IEquatable<Person>, IComparable<Person>
{
	public Person()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static Person()
	{
	}

	public override void OnStart()
	{
		base.OnStart();
		this.dialogQuery.Start(this);
		this.careerHistory.Start(this);
		this.contractManager.Start(this);
	}

	public void GenerateCareerHistory()
	{
	}

	public override void OnDestory()
	{
		base.OnDestory();
	}

	public override void Update()
	{
		base.Update();
	}

	public override void OnLoad()
	{
		base.OnLoad();
		if (this.mImprovementRateDecay == 0f || this.mImprovementRateDecay < Person.improvementRateDecayMin || this.mImprovementRateDecay > Person.improvementRateDecayMax)
		{
			this.mImprovementRateDecay = RandomUtility.GetRandom(Person.improvementRateDecayMin, Person.improvementRateDecayMax);
		}
	}

	public void SetName(string inFirstName, string inLastName)
	{
		this.mFirstName = inFirstName;
		if (inLastName.Length > 0)
		{
			this.mLastName = GameUtility.ChangeFirstCharToUpperCase(inLastName);
		}
		this.mShortName = ((this.mFirstName.Length <= 0) ? "." : (this.firstName.Substring(1,1) + ". " + this.lastName));
		if (this.lastName.Length >= 3) {
			this.mThreeLetterName = this.lastName.Substring(1,3);
		} else {
			this.mThreeLetterName = this.lastName.Substring(1,this.lastName.Length);
		}
		this.name = inFirstName + " " + inLastName;
	}

	public int GetAge()
	{
		DateTime now = Game.instance.time.now;
		int num = now.Year - this.dateOfBirth.Year;
		if (now.Month < this.dateOfBirth.Month || (now.Month == this.dateOfBirth.Month && now.Day < this.dateOfBirth.Day))
		{
			num--;
		}
		return num;
	}

	public int GetAgeAtPeak()
	{
		int num = this.peakAge.Year - this.dateOfBirth.Year;
		if (this.peakAge.Month < this.dateOfBirth.Month || (this.peakAge.Month == this.dateOfBirth.Month && this.peakAge.Day < this.dateOfBirth.Day))
		{
			num--;
		}
		return num;
	}

	public bool IsAtPeak()
	{
		return this.GetAge() >= this.GetAgeAtPeak() && this.GetAge() <= this.GetAgeAtPeak() + this.peakDuration;
	}

	public virtual bool HasPassedPeakAge()
	{
		return this.GetAge() >= this.GetAgeAtPeak() + this.peakDuration;
	}

	public string GetPoachabilityString(int inResult)
	{
		switch (inResult)
		{
		case 0:
			return Localisation.LocaliseID("PSG_10009318", null);
		case 1:
			return Localisation.LocaliseID("PSG_10001437", null);
		case 2:
			return Localisation.LocaliseID("PSG_10001438", null);
		case 3:
			return Localisation.LocaliseID("PSG_10001439", null);
		case 4:
			return "Traitor";
		default:
			return "-";
		}
	}

	public string GetMarketabilityString(int inResult)
	{
		switch (inResult)
		{
		case 0:
			return Localisation.LocaliseID("PSG_10009318", null);
		case 1:
			return Localisation.LocaliseID("PSG_10001437", null);
		case 2:
			return Localisation.LocaliseID("PSG_10001438", null);
		case 3:
			return Localisation.LocaliseID("PSG_10001439", null);
		case 4:
			return Localisation.LocaliseID("PSG_10001440", null);
		default:
			return "-";
		}
	}

	public Color GetPoachabilityColor(int inResult)
	{
		switch (inResult)
		{
		case 0:
			return UIConstants.colorBandGreen;
		case 1:
			return UIConstants.colorBandGreen;
		case 2:
			return UIConstants.colorBandYellow;
		case 3:
			return UIConstants.colorBandRed;
		case 4:
			return UIConstants.colorBandRed;
		default:
			return UIConstants.colorBandRed;
		}
	}

	public string GetAgeClassificationString()
	{
		int num = Game.instance.time.now.Year - this.dateOfBirth.Year;
		string result;
		if (num > 70)
		{
			result = "VeryOld";
		}
		else if (num > 50)
		{
			result = "Old";
		}
		else if (num > 32)
		{
			result = "Veteran";
		}
		else if (num > 20)
		{
			result = "Adult";
		}
		else
		{
			result = "Young";
		}
		return result;
	}

	public TeamColor GetTeamColor()
	{
		TeamColor color = App.instance.teamColorManager.GetColor(0);
		if (this.contract.GetTeam() != null)
		{
			color = App.instance.teamColorManager.GetColor(this.contract.GetTeam().colorID);
		}
		return color;
	}

	public virtual float GetReputationValue()
	{
		return 0f;
	}

	public virtual bool IsReplacementPerson()
	{
		return false;
	}

	public virtual int GetPersonIndexInManager()
	{
		if (this.contract.job == Contract.Job.Journalist)
		{
			MediaManager mediaManager = Game.instance.mediaManager;
			return mediaManager.GetJournalistIndex(this);
		}
		return -1;
	}

	public Person.Reputation GetReputationType()
	{
		float reputationValue = this.GetReputationValue();
		if (reputationValue >= (float)Person.worldClassReputationValue)
		{
			return Person.Reputation.WorldClass;
		}
		if (reputationValue >= (float)Person.greatReputationValue)
		{
			return Person.Reputation.Great;
		}
		if (reputationValue >= (float)Person.goodReputationValue)
		{
			return Person.Reputation.Good;
		}
		if (reputationValue >= (float)Person.averageReputationValue)
		{
			return Person.Reputation.Average;
		}
		return Person.Reputation.Bad;
	}

	public Color GetReputationColor()
	{
		switch (this.GetReputationType())
		{
		case Person.Reputation.Bad:
			return UIConstants.colorBandRed;
		case Person.Reputation.Average:
			return UIConstants.colorBandYellow;
		case Person.Reputation.Good:
		case Person.Reputation.Great:
		case Person.Reputation.WorldClass:
			return UIConstants.colorBandGreen;
		default:
			return UIConstants.colorBandRed;
		}
	}

	public virtual bool IsFreeAgent()
	{
		return this.contract.job == Contract.Job.Unemployed;
	}

	public void ToggleShortlisted(bool shortListed)
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (this.mIsShortlisted != shortListed && shortListed)
		{
			FeedbackPopup.Open(Localisation.LocaliseID("PSG_10007168", null), string.Format("{0} has been added to your favourites", this.name));
		}
		this.mIsShortlisted = shortListed;
	}

	public void SetPeakDuration(int newPeakDuration)
	{
		this.peakDuration = newPeakDuration;
	}

	public virtual ContractNegotiationScreen.NegotatiationType GetNecessaryNegotiationType()
	{
		if (this.IsFreeAgent())
		{
			return ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed;
		}
		if (this.contract.GetTeam() == Game.instance.player.team)
		{
			return ContractNegotiationScreen.NegotatiationType.RenewStaff;
		}
		return ContractNegotiationScreen.NegotatiationType.NewStaffEmployed;
	}

	public virtual ContractNegotiationScreen.NegotatiationType GetNecessaryNegotiationType(Team team)
	{
		if (this.IsFreeAgent())
		{
			return ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed;
		}
		if (this.contract.GetTeam() == team)
		{
			return ContractNegotiationScreen.NegotatiationType.RenewStaff;
		}
		return ContractNegotiationScreen.NegotatiationType.NewStaffEmployed;
	}

	public string firstName
	{
		get
		{
			return this.mFirstName;
		}
	}

	public string lastName
	{
		get
		{
			return this.mLastName;
		}
	}

	public string twitterHandle
	{
		get
		{
			return this.shortName.Replace(" ", string.Empty);
		}
	}

	public string shortName
	{
		get
		{
			return this.mShortName;
		}
	}

	public string threeLetterLastName
	{
		get
		{
			return this.mThreeLetterName;
		}
	}

	public bool isShortlisted
	{
		get
		{
			return this.mIsShortlisted;
		}
	}

	public bool isTeamPrincipal
	{
		get
		{
			return this is TeamPrincipal;
		}
	}

	public bool canNegotiateContract
	{
		get
		{
			return !this.isTeamPrincipal && !this.IsReplacementPerson() && this.contractManager.noContractProposed;
		}
	}

	public bool isNegotiatingContract
	{
		get
		{
			return !this.isTeamPrincipal && !this.IsReplacementPerson() && this.contractManager.isNegotiating;
		}
	}

	public bool canBeFired
	{
		get
		{
			Team team = this.contract.GetTeam();
			return !this.isTeamPrincipal && !this.IsFreeAgent() && !this.IsReplacementPerson() && team.IsPlayersTeam();
		}
	}

	public virtual void SetMorale(float inMoraleValue)
	{
		this.mMorale = inMoraleValue;
	}

	public virtual float GetMorale()
	{
		return this.mMorale;
	}

	public virtual void ModifyMorale(float inModifierValue, string inModifierNameID, bool inOverwriteEntryWithSameName = false)
	{
		this.SetMorale(Mathf.Clamp01(this.mMorale + inModifierValue));
		if (!string.IsNullOrEmpty(inModifierNameID))
		{
			this.mMoraleStatModificationHistory.AddStatModificationEntry(inModifierNameID, inModifierValue * 100f, inOverwriteEntryWithSameName);
		}
	}

	public StatModificationHistory moraleStatModificationHistory
	{
		get
		{
			return this.mMoraleStatModificationHistory;
		}
	}

	public virtual bool WantsToRetire()
	{
		return false;
	}

	protected virtual bool WontRenewContract(Team inTeam)
	{
		return false;
	}

	protected virtual bool HasRivalInTeam(Team inTeam)
	{
		return false;
	}

	protected virtual bool IsRivalOfTeam(Team inTeam)
	{
		return false;
	}

	private bool WasContractARenew()
	{
		if (!this.IsFreeAgent())
		{
			CareerHistoryEntry latestFinishedEntry = this.careerHistory.GetLatestFinishedEntry();
			if (latestFinishedEntry != null)
			{
				return latestFinishedEntry.team == this.contract.GetTeam();
			}
		}
		return false;
	}

	public Person.InterestedToTalkResponseType GetInterestedToTalkReaction(Team inTeam)
	{
		ContractNegotiationScreen.NegotatiationType necessaryNegotiationType = this.GetNecessaryNegotiationType(inTeam);
		this.contractManager.contractEvaluation.desiredContractValues.SetNegotiationType(necessaryNegotiationType);
		bool flag = necessaryNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewDriver;
		flag |= (necessaryNegotiationType == ContractNegotiationScreen.NegotatiationType.RenewStaff);
		int num = this.contractManager.contractEvaluation.desiredContractValues.CalculateDesiredChampionship(inTeam);
		Driver driver = this as Driver;
		bool flag2 = inTeam.IsPlayersTeam() && Game.instance.player.playerBackStoryType == PlayerBackStory.PlayerBackStoryType.MotorsportLegend;
		if (this.HasRetired())
		{
			return Person.InterestedToTalkResponseType.WantsToRetire;
		}
		if (driver != null)
		{
			if (inTeam.IsPlayersTeam() && inTeam.investor != null && inTeam.investor.hasDriverMinAge && driver.GetAge() > inTeam.investor.driverMinAge)
			{
				return Person.InterestedToTalkResponseType.InvestorDriverAgeTooHigh;
			}
			if (!driver.HasPreferedSeries(inTeam.championship.series, true))
			{
				return Person.InterestedToTalkResponseType.WontDriveForThatSeries;
			}
			if (Game.IsActive() && Game.instance.challengeManager != null && Game.instance.challengeManager.IsAttemptingChallenge() && Game.instance.challengeManager.currentChallenge.id == 3 && driver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.ChairmansHappinessMirrorsDriver))
			{
				return Person.InterestedToTalkResponseType.NotInterestedToTalkGeneric;
			}
			if (flag2 || (inTeam.IsPlayersTeam() && driver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.ButteredUpByInterview)))
			{
				return Person.InterestedToTalkResponseType.InterestedToTalk;
			}
			if (inTeam.IsPlayersTeam() && driver.personalityTraitController.HasSpecialCase(PersonalityTrait.SpecialCaseType.OffendedByInterview))
			{
				return Person.InterestedToTalkResponseType.OffendedByInterview;
			}
		}
		if (flag2)
		{
			return Person.InterestedToTalkResponseType.InterestedToTalk;
		}
		if (inTeam.IsPlayersTeam() && this.contractManager.firedByPlayerDate > Game.instance.time.now)
		{
			return Person.InterestedToTalkResponseType.JustBeenFiredByPlayer;
		}
		if (inTeam.IsPlayersTeam() && this.contractManager.cooldownPeriod > Game.instance.time.now)
		{
			return Person.InterestedToTalkResponseType.InsultedByLastProposal;
		}
		if (this.WantsToRetire())
		{
			return Person.InterestedToTalkResponseType.WantsToRetire;
		}
		int championshipOrder = inTeam.championship.championshipOrder;
		if (!this.IsFreeAgent())
		{
			int championshipOrder2 = this.contract.GetTeam().championship.championshipOrder;
			if (championshipOrder > championshipOrder2)
			{
				return Person.InterestedToTalkResponseType.NotJoiningLowerChampionship;
			}
		}
		if (championshipOrder > num)
		{
			return Person.InterestedToTalkResponseType.WantToJoinHigherChampionship;
		}
		if (flag && this.WontRenewContract(inTeam))
		{
			return Person.InterestedToTalkResponseType.WontRenewContract;
		}
		if (this.HasRivalInTeam(inTeam) || this.IsRivalOfTeam(inTeam))
		{
			return Person.InterestedToTalkResponseType.WontJoinRival;
		}
		int num2 = 180;
		int days = Game.instance.time.now.Subtract(this.contract.startDate).Days;
		if (!this.IsFreeAgent() && !this.IsOpenToOffers() && !flag && days <= num2 && !this.WasContractARenew())
		{
			float num3 = 20f;
			float b = num3 * 0.6f;
			float num4 = Mathf.Lerp(num3, b, (float)days / (float)num2);
			if (inTeam.teamPrincipal.stats.loyalty < num4)
			{
				return Person.InterestedToTalkResponseType.JustStartedANewContract;
			}
		}
		if (inTeam.IsPlayersTeam() && this.contractManager.cancelledContractNegotiationCooldownPeriod > Game.instance.time.now)
		{
			return Person.InterestedToTalkResponseType.CanceledNegotiation;
		}
		if (inTeam.IsPlayersTeam() && this.contractManager.letContractNegotiationExpireCooldownPeriod > Game.instance.time.now)
		{
			return Person.InterestedToTalkResponseType.LetNegotiationExpire;
		}
		if (flag && this.contract.GetMonthsRemaining() >= 12)
		{
			return Person.InterestedToTalkResponseType.TooEarlyToRenew;
		}
		if (flag && this.GetMorale() <= 0.2f && this is Driver)
		{
			return Person.InterestedToTalkResponseType.MoraleTooLow;
		}
		this.contractManager.contractEvaluation.desiredContractValues.CalculateWantsToTalk(inTeam);
		if (this.contractManager.contractEvaluation.desiredContractValues.isInterestedToTalk)
		{
			return Person.InterestedToTalkResponseType.InterestedToTalk;
		}
		return Person.InterestedToTalkResponseType.NotInterestedToTalkGeneric;
	}

	public int CompareTo(Person inPerson)
	{
		if (inPerson == null)
		{
			return 1;
		}
		return this.name.CompareTo(inPerson.name);
	}

	public bool Equals(Person inPerson)
	{
		return inPerson != null && this.name.Equals(inPerson.name);
	}

	private static bool ContractCheck(Person personA, Person personB)
	{
		return !personA.IsFreeAgent() && !personB.IsFreeAgent();
	}

	private static int ContractCompare(Person personA, Person personB)
	{
		bool flag = personA.IsFreeAgent();
		bool flag2 = personB.IsFreeAgent();
		if ((flag && !flag2) || (!flag && flag2))
		{
			return flag.CompareTo(flag2);
		}
		return string.Compare(personA.name, personB.name);
	}

	private static bool TeamCheck(Person personA, Person personB)
	{
		Team team = personA.contract.GetTeam();
		Team team2 = personB.contract.GetTeam();
		return !(team is NullTeam) && !(team2 is NullTeam);
	}

	private static int TeamCompare(Person personA, Person personB)
	{
		Team team = personA.contract.GetTeam();
		Team team2 = personB.contract.GetTeam();
		bool flag = !(team is NullTeam);
		bool flag2 = !(team2 is NullTeam);
		if (flag && flag2 && team != team2)
		{
			return team.name.CompareTo(team2.name);
		}
		if ((flag && !flag2) || (flag2 && !flag))
		{
			return flag.CompareTo(flag2);
		}
		return string.Compare(personA.name, personB.name);
	}

	private static int ChampionshipCompare(Person personA, Person personB)
	{
		Championship championship = personA.contract.GetTeam().championship;
		Championship championship2 = personB.contract.GetTeam().championship;
		return string.Compare(championship.GetChampionshipName(false, string.Empty), championship2.GetChampionshipName(false, string.Empty));
	}

	public static void SortByAbility<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			if (!(person is Driver))
			{
				return person.GetStats().GetAbility().CompareTo(person2.GetStats().GetAbility());
			}
			Driver driver = personA as Driver;
			Driver driver2 = personB as Driver;
			if (driver.CanShowStats() && driver2.CanShowStats())
			{
				return person.GetStats().GetAbility().CompareTo(person2.GetStats().GetAbility());
			}
			if ((driver.CanShowStats() && !driver2.CanShowStats()) || (!driver.CanShowStats() && driver2.CanShowStats()))
			{
				return driver2.CanShowStats().CompareTo(driver.CanShowStats());
			}
			return string.Compare(driver.name, driver2.name);
		});
	}

	public static void SortByName<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort();
		if (!inASC)
		{
			inPersonList.Reverse();
		}
	}

	public static void SortByTeam<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person personA2 = (!inASC) ? personB : personA;
			Person personB2 = (!inASC) ? personA : personB;
			int result;
			if (Person.ContractCheck(personA, personB))
			{
				result = Person.TeamCompare(personA2, personB2);
			}
			else
			{
				result = Person.ContractCompare(personA, personB);
			}
			return result;
		});
	}

	public static void SortByNationality<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			int num = string.Compare(person.nationality.localisedCountry, person2.nationality.localisedCountry);
			if (num == 0)
			{
				num = string.Compare(personA.name, personB.name);
			}
			return num;
		});
	}

	public static void SortByRacingSeries<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person personA2 = (!inASC) ? personB : personA;
			Person personB2 = (!inASC) ? personA : personB;
			int num;
			if (Person.ContractCheck(personA, personB))
			{
				if (Person.TeamCheck(personA, personB))
				{
					num = Person.ChampionshipCompare(personA2, personB2);
					if (num == 0)
					{
						num = Person.TeamCompare(personA, personB);
					}
				}
				else
				{
					num = Person.TeamCompare(personA, personB);
				}
			}
			else
			{
				num = Person.ContractCompare(personA, personB);
			}
			return num;
		});
	}

	public static void SortByAge<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			int num = person.GetAge().CompareTo(person2.GetAge());
			if (num == 0)
			{
				num = string.Compare(personA.name, personB.name);
			}
			return num;
		});
	}

	public static void SortByContractEndDate<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			int result;
			if (Person.ContractCheck(personA, personB))
			{
				result = person.contract.endDate.CompareTo(person2.contract.endDate);
			}
			else
			{
				result = Person.ContractCompare(personA, personB);
			}
			return result;
		});
	}

	public static void SortByRaceCost<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			int result;
			if (Person.ContractCheck(personA, personB))
			{
				result = person.contract.perRaceCost.CompareTo(person2.contract.perRaceCost);
			}
			else
			{
				result = Person.ContractCompare(personA, personB);
			}
			return result;
		});
	}

	public static void SortByBreakClauseCost<T>(List<T> inPersonList, bool inASC = true) where T : Person
	{
		inPersonList.Sort(delegate(T personA, T personB)
		{
			Person person = (!inASC) ? personB : personA;
			Person person2 = (!inASC) ? personA : personB;
			int result;
			if (Person.ContractCheck(personA, personB))
			{
				result = person.contract.GetContractTerminationCost().CompareTo(person2.contract.GetContractTerminationCost());
			}
			else
			{
				result = Person.ContractCompare(personA, personB);
			}
			return result;
		});
	}

	public TimeSpan TimeSincePeakAge()
	{
		return Game.instance.time.now.Subtract(this.peakAge);
	}

	public bool HasRetired()
	{
		return this.retirementAge > 0;
	}

	public virtual bool HasAchievedCareerGoals()
	{
		return false;
	}

	public virtual bool IsReadyToRetire()
	{
		return !this.HasRetired() && this.TimeSincePeakAge().Days > 0 && this.HasAchievedCareerGoals();
	}

	public void Retire()
	{
		if (!this.HasRetired())
		{
			this.retirementAge = this.GetAge();
		}
	}

	public float GetBonusImprovementRateForAge(DateTime inDate)
	{
		DateTime dateTime = this.dateOfBirth.AddYears(Person.bonusImprovementAge);
		int num = 365;
		if (DateTime.IsLeapYear(inDate.Year))
		{
			num++;
		}
		TimeSpan timeSpan = dateTime - inDate;
		if (timeSpan.TotalDays <= 0.0)
		{
			return 1f;
		}
		if (timeSpan.TotalDays < (double)num)
		{
			return 1f + (Person.bonusImprovementAmount - 1f) * (float)(timeSpan.TotalDays / (double)((float)num));
		}
		return Person.bonusImprovementAmount;
	}

	public float GetImprovementRateForAge(DateTime inDate, DateTime inPeakAge, int inPeakDuration, float inImprovementRate)
	{
		int num = inDate.Year - inPeakAge.Year;
		int num2 = 365;
		if (DateTime.IsLeapYear(inDate.Year))
		{
			num2++;
		}
		TimeSpan timeSpan = inPeakAge - inDate;
		if (inDate >= inPeakAge)
		{
			if (num <= inPeakDuration)
			{
				return 0f;
			}
			return (float)((inDate - inPeakAge).TotalDays * (double)this.mImprovementRateDecay);
		}
		else
		{
			if (timeSpan.TotalDays < (double)num2)
			{
				return inImprovementRate * (float)(timeSpan.TotalDays / (double)((float)num2));
			}
			return inImprovementRate;
		}
	}

	public float GetImprovementRateForAgeWithBonus(DateTime inDate, DateTime inPeakAge, int inPeakDuration, float inImprovementRate)
	{
		float improvementRateForAge = this.GetImprovementRateForAge(inDate, inPeakAge, inPeakDuration, inImprovementRate);
		float bonusImprovementRateForAge = this.GetBonusImprovementRateForAge(inDate);
		return improvementRateForAge * bonusImprovementRateForAge;
	}

	public DateTime CalculatePeakAge(int inPeakAgeMin, int inPeakAgeMax)
	{
		int age = this.GetAge();
		int inMin = Mathf.Max(1, inPeakAgeMin - age);
		int inMax = Mathf.Max(1, inPeakAgeMax - age);
		return Game.instance.time.now.AddYears(RandomUtility.GetRandomInc(inMin, inMax));
	}

	public virtual PersonStats GetStats()
	{
		return null;
	}

	public virtual float GetStatsValue()
	{
		return 0f;
	}

	public virtual float GetExpectation(DatabaseEntry inWeightings)
	{
		float num = 0f;
		num += this.GetExperience() * ((float)inWeightings.GetIntValue("Experience") / 100f);
		num += this.GetStatsValue() * ((float)inWeightings.GetIntValue("Quality") / 100f);
		if (this.contract.GetTeam() != null)
		{
			num += (float)this.contract.GetTeam().reputation / 100f * ((float)inWeightings.GetIntValue("Team Reputation") / 100f);
		}
		return num;
	}

	public float GetChampionshipExpectation()
	{
		DatabaseEntry inWeightings = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Championship");
		return this.GetExpectation(inWeightings);
	}

	public float GetRaceExpectation()
	{
		DatabaseEntry inWeightings = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Race");
		return this.GetExpectation(inWeightings);
	}

	public virtual float GetRaceAchievements()
	{
		DatabaseEntry inWeightings = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Race");
		return this.GetAchievements(inWeightings);
	}

	public virtual float GetChampionshipAchievements()
	{
		DatabaseEntry inWeightings = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Championship");
		return this.GetAchievements(inWeightings);
	}

	public virtual float GetAchievements(DatabaseEntry inWeightings)
	{
		return 0f;
	}

	public virtual float GetValueForMoney(DatabaseEntry inWeightings)
	{
		if (this.contract.job != Contract.Job.Unemployed && this.contract.GetTeam().championship != null)
		{
			return this.GetAchievements(inWeightings) / ((float)this.contract.GetMonthlyWageCost() / 10000f);
		}
		return 0f;
	}

	public virtual void ModifyHappiness(float inHappiness, string inHappinessModifierName)
	{
	}

	public virtual int GetHappiness()
	{
		return 0;
	}

	public virtual float GetExperience()
	{
		int careerCount = this.careerHistory.careerCount;
		if (this.careerHistory.careerCount == 0)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < careerCount; i++)
		{
			CareerHistoryEntry careerHistoryEntry = this.careerHistory.career[i];
			num += (float)careerHistoryEntry.races;
			num2 += (float)careerHistoryEntry.careerPoints;
			num3 += (float)careerHistoryEntry.DNFs;
		}
		if (num == 0f)
		{
			num = 1f;
		}
		float num4 = num2 / num;
		float num5 = num3 / num;
		return num4 / 25f - num5 * 0.1f;
	}

	public bool WantsToRetire(DateTime aCurDate, float inImprovementRate)
	{
		return (this.HasAchievedCareerGoals() && this.GetImprovementRateForAge(aCurDate, this.peakAge, this.peakDuration, inImprovementRate) < 0f) || this.GetImprovementRateForAge(aCurDate, this.peakAge, this.peakDuration, inImprovementRate) < -2f;
	}

	public virtual bool WantsToLeave()
	{
		return false;
	}

	public virtual bool IsOpenToOffers()
	{
		return false;
	}

	public virtual bool CanShowStats()
	{
		return true;
	}

	public Transaction.Group GetTransactionType()
	{
		if (this is Driver)
		{
			return Transaction.Group.Drivers;
		}
		if (this is Mechanic)
		{
			return Transaction.Group.Mechanics;
		}
		if (this is Engineer)
		{
			return Transaction.Group.Designer;
		}
		if (this is PitCrewMember)
		{
			return Transaction.Group.PitCrew;
		}
		return Transaction.Group.HQUpkeepAndStaff;
	}

	public Team GetBestTeam()
	{
		Dictionary<Team, int> dictionary = new Dictionary<Team, int>();
		for (int i = 0; i < this.careerHistory.career.Count; i++)
		{
			CareerHistoryEntry careerHistoryEntry = this.careerHistory.career[i];
			if (careerHistoryEntry.team != null)
			{
				if (!dictionary.ContainsKey(careerHistoryEntry.team))
				{
					dictionary.Add(careerHistoryEntry.team, 1);
				}
				else
				{
					Dictionary<Team, int> dictionary3;
					Dictionary<Team, int> dictionary2 = dictionary3 = dictionary;
					Team team2;
					Team team = team2 = careerHistoryEntry.team;
					int num = dictionary3[team2];
					dictionary2[team] = num + 1;
				}
			}
		}
		KeyValuePair<Team, int> keyValuePair = default(KeyValuePair<Team, int>);
		foreach (KeyValuePair<Team, int> keyValuePair2 in dictionary)
		{
			if (keyValuePair.Key == null || keyValuePair.Value < keyValuePair2.Value)
			{
				keyValuePair = keyValuePair2;
			}
		}
		return keyValuePair.Key;
	}

	public bool hasWeigthSet
	{
		get
		{
			return (float)this.weight > 1f;
		}
	}

	private string mFirstName = string.Empty;

	private string mLastName = string.Empty;

	private string mShortName = string.Empty;

	private string mThreeLetterName = string.Empty;

	private bool mIsShortlisted;

	public Nationality nationality = new Nationality();

	public Portrait portrait = new Portrait();

	public Popularity popularity = new Popularity();

	public Relationships relationships = new Relationships();

	public DialogQueryCreator dialogQuery = new DialogQueryCreator();

	public ContractManagerPerson contractManager = new ContractManagerPerson();

	public ContractPerson contract = new ContractPerson();

	public ContractPerson nextYearContract = new ContractPerson();

	public Person.Gender gender;

	public DateTime dateOfBirth;

	public int weight;

	public int retirementAge;

	public float obedience;

	public int rewardID;

	public DateTime peakAge;

	public int peakDuration = RandomUtility.GetRandom(2, 6);

	public CareerHistory careerHistory = new CareerHistory();

	public static readonly int worldClassReputationValue = 18;

	public static readonly int greatReputationValue = 15;

	public static readonly int goodReputationValue = 12;

	public static readonly int averageReputationValue = 8;

	public static readonly int minRacesBeforeThinkingAboutJobChange = 3;

	private float mMorale;

	private StatModificationHistory mMoraleStatModificationHistory = new StatModificationHistory();

	public static readonly float bonusImprovementAmount = 2f;

	public static readonly int bonusImprovementAge = 23;

	public static readonly float improvementRateDecayMin = -0.003f;

	public static readonly float improvementRateDecayMax = -0.001f;

	private float mImprovementRateDecay = RandomUtility.GetRandom(Person.improvementRateDecayMin, Person.improvementRateDecayMax);

	public enum Gender
	{
		Male,
		Female
	}

	public enum Reputation
	{
		Bad,
		Average,
		Good,
		Great,
		WorldClass
	}

	public enum InterestedToTalkResponseType
	{
		InterestedToTalk,
		NotJoiningLowerChampionship,
		WantToJoinHigherChampionship,
		JustBeenFiredByPlayer,
		InsultedByLastProposal,
		WantsToRetire,
		WontRenewContract,
		WontJoinRival,
		JustStartedANewContract,
		TooEarlyToRenew,
		MoraleTooLow,
		LetNegotiationExpire,
		CanceledNegotiation,
		NotInterestedToTalkGeneric,
		WontDriveForThatSeries,
		OffendedByInterview,
		InvestorDriverAgeTooHigh
	}
}
