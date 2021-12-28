using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ContractManagerPerson
{
	public ContractManagerPerson()
	{
	}

	public void Start(Person inPerson)
	{
		this.contractEvaluation.Start(inPerson);
		this.mPerson = inPerson;
	}

	public void StartNegotiation(ContractNegotiationScreen.NegotatiationType inNegotiationType, Team inTeam)
	{
		this.mNegotiationType = inNegotiationType;
		this.contractEvaluation.desiredContractValues.CalculateDesiredContractValues(this.mNegotiationType, inTeam);
		this.CalculateAvailablePatienceForThisNegotiation(inTeam);
	}

	private void CalculateAvailablePatienceForThisNegotiation(Team inTeam)
	{
		float num = 0f;
		float starsStat = inTeam.GetStarsStat();
		float ability = this.mPerson.GetStats().GetAbility();
		if (ability < starsStat)
		{
			num = (ability + starsStat) * 0.35f;
		}
		else if (ability > starsStat)
		{
			num = -((ability - starsStat) * 0.35f);
		}
		num = Mathf.Clamp(num, -4f, 4f);
		this.contractPatienceAvailable = Mathf.Clamp(Mathf.FloorToInt((float)this.contractPatience + num), 1, 5);
	}

	public void IsPromotingDriver()
	{
		this.mNegotiationType = ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver;
	}

	public void ReceiveDraftProposal(ContractPerson targetContract, ContractPerson inDraftContract, ContractNegotiationScreen.NegotatiationType inNegotiationType, ContractNegotiationScreen.ContractYear inContractYear)
	{
		if (this.mDraftProposalContract != null && this.mContractElapsedEvent != null && this.mContractElapsedEvent.triggerDate >= Game.instance.time.now)
		{
			this.ContractElapsedAction();
		}
		this.mTargetContract = targetContract;
		this.mNegotiationType = inNegotiationType;
		this.mDraftProposalContract = inDraftContract;
		this.mContractProposalState = ContractManagerPerson.ContractProposalState.ConsideringProposal;
		this.mContractYear = inContractYear;
		this.contractEvaluation.SetupForEvaluation(this.mNegotiationType, this.mDraftProposalContract);
		Team team = inDraftContract.GetTeam();
		int random = RandomUtility.GetRandom(1, 3);
		StringVariableParser.sender = inDraftContract.person;
		this.mCalendarConsideredEvent = new CalendarEvent_v1
		{
			category = CalendarEventCategory.Contract,
			showOnCalendar = team.IsPlayersTeam(),
			triggerState = GameState.Type.FrontendState,
			triggerDate = Game.instance.time.now.AddDays((double)random),
			interruptGameTime = team.IsPlayersTeam(),
			OnEventTrigger = MMAction.CreateFromAction(new Action(this.CalendarConsideredEventAction)),
			displayEffect = new TeamDisplayEffect
			{
				changeDisplay = true,
				changeInterrupt = true,
				team = team
			}
		};
		this.mCalendarConsideredEvent.SetDynamicDescription("PSG_10009154");
		Game.instance.calendar.AddEvent(this.mCalendarConsideredEvent);
	}

	private void CalendarConsideredEventAction()
	{
		if (this.contractEvaluation.IsContractNull())
		{
			global::Debug.LogError("Tried to evaluate contract, but the draft contract is null", null);
			return;
		}
		this.CheckIfNegotiationTypeHasChanged();
		this.EvaluateContract();
		this.SendMailResponseToProposalContract();
		if (this.mDraftProposalContract.GetTeam().IsPlayersTeam())
		{
			this.RefreshNeededScreens();
			if (this.mDraftProposalContract.job == Contract.Job.Driver)
			{
				Game.instance.notificationManager.GetNotification("DriverContractConsidered").IncrementCount();
			}
			else
			{
				Game.instance.notificationManager.GetNotification("StaffContractConsidered").IncrementCount();
			}
		}
		else
		{
			this.SendContractResponseToAI();
		}
	}

	public void CheckIfNegotiationTypeHasChanged()
	{
		if (this.mPerson.GetNecessaryNegotiationType() != this.mNegotiationType)
		{
			this.mNegotiationType = this.mPerson.GetNecessaryNegotiationType();
			this.contractEvaluation.desiredContractValues.CalculateDesiredContractValues(this.mNegotiationType, this.mDraftProposalContract.GetTeam());
			if (this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed || this.mNegotiationType == ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed)
			{
				this.mDraftProposalContract.amountForTargetToPay = 0;
				this.mDraftProposalContract.amountForContractorToPay = 0;
			}
			this.contractEvaluation.SetupForEvaluation(this.mNegotiationType, this.mDraftProposalContract);
		}
	}

	private void ContractNegotiationExpiredAction()
	{
		bool flag = this.mDraftProposalContract.GetTeam().IsPlayersTeam();
		this.RefreshNeededScreens();
		this.ContractElapsedAction();
		if (flag)
		{
			Game.instance.dialogSystem.OnContractNegotiationExpired(this.mPerson);
		}
		this.mLetContractNegotiationExpireCooldownPeriod = Game.instance.time.now.AddDays(90.0);
	}

	private void RefreshNeededScreens()
	{
		if (this.mDraftProposalContract.GetTeam().IsPlayersTeam() && (UIManager.instance.IsScreenOpen("AllDriversScreen") || UIManager.instance.IsScreenOpen("MailScreen")))
		{
			UIManager.instance.RefreshCurrentPage();
		}
	}

	private void ContractNegotiationExpiringAction()
	{
		if (this.mDraftProposalContract.GetTeam().IsPlayersTeam())
		{
			Game.instance.dialogSystem.OnContractNegotiationExpiring(this.mPerson);
		}
	}

	private void ContractElapsedAction()
	{
		if (this.mDraftProposalContract != null)
		{
			this.mDraftProposalContract.GetTeam().contractManager.RemoveDraftProposal(this.mPerson);
		}
	}

	public void CancelProposal()
	{
		Game.instance.calendar.RemoveEvent(this.mCalendarConsideredEvent);
		this.RemoveDraftProposal();
	}

	public void RemoveDraftProposal()
	{
		this.mTargetContract = null;
		this.mDraftProposalContract = null;
		this.mNegotiationType = ContractNegotiationScreen.NegotatiationType.NewDriver;
		this.mContractProposalState = ContractManagerPerson.ContractProposalState.NoContractProposed;
		this.mContractYear = ContractNegotiationScreen.ContractYear.Current;
		this.mCalendarConsideredEvent = null;
		if (this.mContractElapsedEvent != null && this.mContractElapsedEvent.triggerDate >= Game.instance.time.now)
		{
			Game.instance.calendar.RemoveEvent(this.mContractElapsedEvent);
		}
		if (this.mContractNegotiationExpiringEvent != null && this.mContractNegotiationExpiringEvent.triggerDate >= Game.instance.time.now)
		{
			Game.instance.calendar.RemoveEvent(this.mContractNegotiationExpiringEvent);
		}
		this.mContractElapsedEvent = null;
		this.mContractNegotiationExpiringEvent = null;
		this.contractEvaluation.Reset();
	}

	public void AddCancelledNegotiationCooldown()
	{
		if (this.mContractProposalState != ContractManagerPerson.ContractProposalState.ProposalRejected || this.mIsLastChance != ContractManagerPerson.LastChanceProposalState.LastChanceUsed)
		{
			this.mCancelledContractNegotiationCooldownPeriod = Game.instance.time.now.AddDays(90.0);
			this.ResetContractPatienceUsed();
		}
	}

	private void ResetContractPatienceUsed()
	{
		this.contractPatienceUsed = 0;
		this.mIsLastChance = ContractManagerPerson.LastChanceProposalState.NotLastChanceYet;
	}

	private void SendMailResponseToProposalContract()
	{
		DialogCriteria dialogCriteria = new DialogCriteria();
		if (this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalAccepted)
		{
			dialogCriteria.mType = "Source";
			if (this.mDraftProposalContract.person is Driver)
			{
				dialogCriteria.mCriteriaInfo = "ContractAccepted";
			}
			else if (this.mDraftProposalContract.person is Mechanic)
			{
				dialogCriteria.mCriteriaInfo = "StaffContractAgreed";
			}
			else if (this.mDraftProposalContract.person is Engineer)
			{
				dialogCriteria.mCriteriaInfo = "StaffContractAgreed";
			}
		}
		else if (this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalRejected && this.mIsLastChance == ContractManagerPerson.LastChanceProposalState.IsLastChance)
		{
			dialogCriteria.mType = "Source";
			dialogCriteria.mCriteriaInfo = "ContractRenegotiation";
		}
		else if (this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalRejected && this.mIsLastChance == ContractManagerPerson.LastChanceProposalState.LastChanceUsed)
		{
			dialogCriteria.mType = "Source";
			dialogCriteria.mCriteriaInfo = "ContractRejected";
		}
		else if (this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalRejected && this.contractPatienceUsed < this.contractPatienceAvailable)
		{
			dialogCriteria.mType = "Source";
			dialogCriteria.mCriteriaInfo = "ContractRenegotiation";
		}
		if (this.mDraftProposalContract.GetTeam().IsPlayersTeam())
		{
			Game.instance.dialogSystem.SendMail(this.mDraftProposalContract.person, new DialogCriteria[]
			{
				dialogCriteria,
				new DialogCriteria("Type", "Header")
			});
		}
	}

	public bool IsAcceptingDraftContractAI(ContractPerson inDraftContract, ContractNegotiationScreen.NegotatiationType inNegotiationType)
	{
		if (this.mDraftProposalContract != null && this.mContractElapsedEvent != null && this.mContractElapsedEvent.triggerDate >= Game.instance.time.now)
		{
			this.ContractElapsedAction();
		}
		this.mNegotiationType = inNegotiationType;
		this.contractEvaluation.SetupForEvaluation(inNegotiationType, inDraftContract);
		float num = this.contractEvaluation.EvaluateContractValue();
		float minimumContractValueAcceptance = this.contractEvaluation.desiredContractValues.minimumContractValueAcceptance;
		return num >= minimumContractValueAcceptance;
	}

	private void SendContractResponseToAI()
	{
		Team team = this.mDraftProposalContract.GetTeam();
		if (!team.IsPlayersTeam())
		{
			team.teamAIController.UpdateProposedContractState(this.mDraftProposalContract.person, this.mContractProposalState);
		}
	}

	private void EvaluateContract()
	{
		float num = this.contractEvaluation.EvaluateContractValue();
		float minimumContractValueAcceptance = this.contractEvaluation.desiredContractValues.minimumContractValueAcceptance;
		if (num >= minimumContractValueAcceptance)
		{
			this.ResetContractPatienceUsed();
			this.mContractProposalState = ContractManagerPerson.ContractProposalState.ProposalAccepted;
			DateTime nonWeekEndDay = this.GetNonWeekEndDay(6);
			this.mContractElapsingTime = nonWeekEndDay.AddDays(1.0);
			this.mContractElapsedEvent = new CalendarEvent_v1
			{
				category = CalendarEventCategory.Contract,
				showOnCalendar = false,
				triggerState = GameState.Type.FrontendState,
				triggerDate = this.mContractElapsingTime,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.ContractNegotiationExpiredAction))
			};
			this.mContractElapsedEvent.SetDynamicDescription("Contract elapsed - " + this.mDraftProposalContract.person.name);
			Game.instance.calendar.AddEvent(this.mContractElapsedEvent);
			this.mContractNegotiationExpiringEvent = new CalendarEvent_v1
			{
				category = CalendarEventCategory.Contract,
				showOnCalendar = false,
				triggerState = GameState.Type.FrontendState,
				triggerDate = nonWeekEndDay,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.ContractNegotiationExpiringAction))
			};
			this.mContractNegotiationExpiringEvent.SetDynamicDescription("Contract negotiation expiring - " + this.mDraftProposalContract.person.name);
			Game.instance.calendar.AddEvent(this.mContractNegotiationExpiringEvent);
		}
		else
		{
			this.mContractProposalState = ContractManagerPerson.ContractProposalState.ProposalRejected;
			this.contractPatienceUsed += this.contractEvaluation.GetPatienceUsedForContract();
			this.contractPatienceUsed++;
			if (this.contractPatienceUsed >= this.contractPatienceAvailable)
			{
				if (this.mIsLastChance == ContractManagerPerson.LastChanceProposalState.NotLastChanceYet)
				{
					this.mIsLastChance = ContractManagerPerson.LastChanceProposalState.IsLastChance;
				}
				else
				{
					this.cooldownPeriod = Game.instance.time.now.AddDays(90.0);
					CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
					{
						category = CalendarEventCategory.Contract,
						showOnCalendar = false,
						triggerState = GameState.Type.FrontendState,
						triggerDate = this.cooldownPeriod,
						OnEventTrigger = MMAction.CreateFromAction(new Action(this.ResetContractPatienceUsed))
					};
					calendarEvent_v.SetDynamicDescription("Cooldown period ends - " + this.mDraftProposalContract.person.name);
					Game.instance.calendar.AddEvent(calendarEvent_v);
					this.mContractElapsedEvent = new CalendarEvent_v1
					{
						category = CalendarEventCategory.Contract,
						showOnCalendar = false,
						triggerState = GameState.Type.FrontendState,
						triggerDate = Game.instance.time.now.AddDays(2.0),
						OnEventTrigger = MMAction.CreateFromAction(new Action(this.ContractElapsedAction))
					};
					this.mContractElapsedEvent.SetDynamicDescription("Remove insulted contract - " + this.mDraftProposalContract.person.name);
					Game.instance.calendar.AddEvent(this.mContractElapsedEvent);
					this.mIsLastChance = ContractManagerPerson.LastChanceProposalState.LastChanceUsed;
				}
			}
		}
	}

	private DateTime GetNonWeekEndDay(int inDaysOffset)
	{
		DateTime result = Game.instance.time.now.AddDays((double)inDaysOffset);
		if (result.DayOfWeek == DayOfWeek.Friday)
		{
			return result.AddDays(3.0);
		}
		if (result.DayOfWeek == DayOfWeek.Saturday)
		{
			return result.AddDays(2.0);
		}
		if (result.DayOfWeek == null)
		{
			return result.AddDays(1.0);
		}
		return result;
	}

	public List<UIContractOptionEntry.OptionType> GetOptionNegotiationTypes()
	{
		List<UIContractOptionEntry.OptionType> list = new List<UIContractOptionEntry.OptionType>();
		switch (this.mNegotiationType)
		{
		case ContractNegotiationScreen.NegotatiationType.NewDriver:
			list.Add(UIContractOptionEntry.OptionType.Status);
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.BuyOutClause);
			list.Add(UIContractOptionEntry.OptionType.SignOnFee);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		case ContractNegotiationScreen.NegotatiationType.NewDriverUnemployed:
			list.Add(UIContractOptionEntry.OptionType.Status);
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.SignOnFee);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		case ContractNegotiationScreen.NegotatiationType.RenewDriver:
			list.Add(UIContractOptionEntry.OptionType.Status);
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		case ContractNegotiationScreen.NegotatiationType.NewStaffEmployed:
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.BuyOutClause);
			list.Add(UIContractOptionEntry.OptionType.SignOnFee);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		case ContractNegotiationScreen.NegotatiationType.NewStaffUnemployed:
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.SignOnFee);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		case ContractNegotiationScreen.NegotatiationType.RenewStaff:
			list.Add(UIContractOptionEntry.OptionType.WagesPerRace);
			list.Add(UIContractOptionEntry.OptionType.ContractLength);
			list.Add(UIContractOptionEntry.OptionType.QualifyingBonus);
			list.Add(UIContractOptionEntry.OptionType.RaceBonus);
			break;
		}
		return list;
	}

	public void SetCooldownPeriodForBeingFiredByPlayer()
	{
		this.mFiredByPlayerDate = Game.instance.time.now.AddDays(180.0);
	}

	public string contractAgreedTimeRemaining
	{
		get
		{
			if (this.isProposalAccepted)
			{
				return GameUtility.FormatTimeSpan(this.mContractElapsingTime.Subtract(Game.instance.time.now));
			}
			return string.Empty;
		}
	}

	public bool hasPatience
	{
		get
		{
			return this.contractPatienceUsed < this.contractPatienceAvailable || this.mIsLastChance == ContractManagerPerson.LastChanceProposalState.IsLastChance;
		}
	}

	public bool isNegotiating
	{
		get
		{
			if (this.mDraftProposalContract == null)
			{
				return false;
			}
			if (this.mDraftProposalContract.GetTeam().IsPlayersTeam())
			{
				return (!this.isProposalRejected && !this.noContractProposed) || (this.isProposalRejected && this.hasPatience);
			}
			return !this.isProposalRejected && !this.noContractProposed;
		}
	}

	public bool isConsideringProposal
	{
		get
		{
			return this.mContractProposalState == ContractManagerPerson.ContractProposalState.ConsideringProposal;
		}
	}

	public bool isProposalAccepted
	{
		get
		{
			return this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalAccepted;
		}
	}

	public bool isProposalRejected
	{
		get
		{
			return this.mContractProposalState == ContractManagerPerson.ContractProposalState.ProposalRejected;
		}
	}

	public bool noContractProposed
	{
		get
		{
			return this.mContractProposalState == ContractManagerPerson.ContractProposalState.NoContractProposed;
		}
	}

	public bool isLastChance
	{
		get
		{
			return this.mIsLastChance == ContractManagerPerson.LastChanceProposalState.IsLastChance;
		}
	}

	public ContractPerson draftProposalContract
	{
		get
		{
			return this.mDraftProposalContract;
		}
	}

	public ContractPerson targetContract
	{
		get
		{
			return this.mTargetContract;
		}
	}

	public ContractManagerPerson.ContractProposalState contractProposalState
	{
		get
		{
			return this.mContractProposalState;
		}
	}

	public ContractNegotiationScreen.NegotatiationType negotiationType
	{
		get
		{
			return this.mNegotiationType;
		}
	}

	public ContractNegotiationScreen.ContractYear contractYear
	{
		get
		{
			return this.mContractYear;
		}
	}

	public bool isRenewProposal
	{
		get
		{
			return this.negotiationType == ContractNegotiationScreen.NegotatiationType.RenewDriver || this.negotiationType == ContractNegotiationScreen.NegotatiationType.RenewStaff;
		}
	}

	public DateTime firedByPlayerDate
	{
		get
		{
			return this.mFiredByPlayerDate;
		}
	}

	public DateTime letContractNegotiationExpireCooldownPeriod
	{
		get
		{
			return this.mLetContractNegotiationExpireCooldownPeriod;
		}
	}

	public DateTime cancelledContractNegotiationCooldownPeriod
	{
		get
		{
			return this.mCancelledContractNegotiationCooldownPeriod;
		}
	}

	public int contractPatience;

	public int contractPatienceUsed;

	public int contractPatienceAvailable;

	public DateTime cooldownPeriod = default(DateTime);

	public ContractEvaluationPerson contractEvaluation = new ContractEvaluationPerson();

	private ContractManagerPerson.ContractProposalState mContractProposalState;

	private ContractPerson mTargetContract;

	private ContractPerson mDraftProposalContract;

	private ContractNegotiationScreen.NegotatiationType mNegotiationType;

	private ContractNegotiationScreen.ContractYear mContractYear;

	private ContractManagerPerson.LastChanceProposalState mIsLastChance;

	private DateTime mFiredByPlayerDate = default(DateTime);

	private DateTime mLetContractNegotiationExpireCooldownPeriod = default(DateTime);

	private DateTime mCancelledContractNegotiationCooldownPeriod = default(DateTime);

	private CalendarEvent_v1 mCalendarConsideredEvent;

	private DateTime mContractElapsingTime = default(DateTime);

	private CalendarEvent_v1 mContractNegotiationExpiringEvent;

	private CalendarEvent_v1 mContractElapsedEvent;

	private Person mPerson;

	public enum ContractProposalState
	{
		NoContractProposed,
		ConsideringProposal,
		ProposalRejected,
		ProposalAccepted
	}

	public enum LastChanceProposalState
	{
		NotLastChanceYet,
		IsLastChance,
		LastChanceUsed
	}
}
