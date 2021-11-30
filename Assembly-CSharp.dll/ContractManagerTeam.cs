using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ContractManagerTeam
{
	public ContractManagerTeam()
	{
	}

	public void Start(Team inTeam)
	{
		this.mTeam = inTeam;
	}

	public void OnLoad()
	{
		if (this.mSignedContracts != null)
		{
			this.mSignedContracts.Clear();
		}
	}

	public int GetStaffCount()
	{
		int num = 0;
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (!this.mEmployeeSlots[i].IsAvailable())
			{
				num++;
			}
		}
		return num;
	}

	public Driver GetDriverSittingOut()
	{
		return this.mHealingDriver;
	}

	public bool IsAnyDriverSittingOutEvent()
	{
		return this.mHealingDriver != null;
	}

	public bool IsSittingOutEvent(Driver inDriver)
	{
		return inDriver == this.mHealingDriver;
	}

	public void SetSittingOutEventDriver(Driver inDriver)
	{
		this.mHealingDriver = inDriver;
	}

	public void ResetSittingOutEventDriver(bool reduceInjuryTraitTime = true)
	{
		if (this.mHealingDriver != null && reduceInjuryTraitTime)
		{
			this.mHealingDriver.ReduceInjuriesTraitTime();
		}
		this.mHealingDriver = null;
	}

	public void ProcessSessionResult()
	{
		DriverStatsProgression driverStatsProgression = Game.instance.sessionManager.eventDetails.circuit.driverStats;
		if (driverStatsProgression == null)
		{
			driverStatsProgression = Game.instance.driverManager.raceDriverStatProgression;
		}
		bool flag = Game.instance.sessionManager.eventDetails.currentSession.sessionType != SessionDetails.SessionType.Qualifying || Game.instance.sessionManager.eventDetails.IsLastSessionOfType();
		int num = Game.instance.sessionManager.standings.Count;
		SessionDetails.SessionType sessionType;
		for (int i = 0; i < Game.instance.sessionManager.standings.Count; i++)
		{
			RacingVehicle racingVehicle = Game.instance.sessionManager.standings[i];
			if (racingVehicle.driver.contract.GetTeam() == this.mTeam)
			{
				Driver[] driversForCar = racingVehicle.driversForCar;
				Mechanic mechanicOfDriver = this.mTeam.GetMechanicOfDriver(this.mTeam.GetDriverForCar(racingVehicle.car.identifier));
				int num2 = i + 1;
				if (num2 < num)
				{
					num = num2;
				}
				foreach (Driver driver in driversForCar)
				{
					switch (Game.instance.sessionManager.eventDetails.currentSession.sessionType)
					{
					case SessionDetails.SessionType.Practice:
						if (flag)
						{
							if (driver.IsReserveDriver())
							{
								driver.AccumulateDailyStats(driverStatsProgression, "Practice");
							}
							else
							{
								driver.AccumulateDailyStats(Game.instance.driverManager.practiceDriverStatProgression, "Practice");
							}
						}
						break;
					case SessionDetails.SessionType.Qualifying:
						if (driver.contract.qualifyingBonusTargetPosition != 0 && flag && driver.contract.qualifyingBonus != 0 && num2 <= driver.contract.qualifyingBonusTargetPosition)
						{
							this.AddContractBonusTransaction(driver, Transaction.Group.Drivers, false);
						}
						driver.AccumulateDailyStats(Game.instance.driverManager.qualifyingDriverStatProgression, "Qualifying");
						break;
					case SessionDetails.SessionType.Race:
						if (driver.contract.raceBonusTargetPosition != 0 && flag)
						{
							bool flag2 = this.mTeam.championship.series == Championship.Series.EnduranceSeries;
							bool flag3 = !flag2 || racingVehicle.timer.IsDriverEligbleForPoints(driver);
							if (driver.contract.raceBonus != 0 && num2 <= driver.contract.raceBonusTargetPosition && flag3)
							{
								this.AddContractBonusTransaction(driver, Transaction.Group.Drivers, true);
							}
						}
						driver.AccumulateDailyStats(driverStatsProgression, "Race");
						break;
					}
				}
				sessionType = Game.instance.sessionManager.eventDetails.currentSession.sessionType;
				if (sessionType != SessionDetails.SessionType.Qualifying)
				{
					if (sessionType == SessionDetails.SessionType.Race)
					{
						if (mechanicOfDriver.contract.raceBonusTargetPosition != 0 && flag && mechanicOfDriver.contract.raceBonus != 0 && num2 <= mechanicOfDriver.contract.raceBonusTargetPosition)
						{
							this.AddContractBonusTransaction(mechanicOfDriver, Transaction.Group.Mechanics, true);
						}
					}
				}
				else if (mechanicOfDriver.contract.qualifyingBonusTargetPosition != 0 && flag && mechanicOfDriver.contract.qualifyingBonus != 0 && num2 <= mechanicOfDriver.contract.qualifyingBonusTargetPosition)
				{
					this.AddContractBonusTransaction(mechanicOfDriver, Transaction.Group.Mechanics, false);
				}
			}
		}
		Engineer personOnJob = this.GetPersonOnJob<Engineer>(Contract.Job.EngineerLead);
		sessionType = Game.instance.sessionManager.eventDetails.currentSession.sessionType;
		if (sessionType != SessionDetails.SessionType.Qualifying)
		{
			if (sessionType == SessionDetails.SessionType.Race)
			{
				if (personOnJob.contract.raceBonusTargetPosition != 0 && flag && personOnJob.contract.raceBonus != 0 && num <= personOnJob.contract.raceBonusTargetPosition)
				{
					this.AddContractBonusTransaction(personOnJob, Transaction.Group.Designer, true);
				}
			}
		}
		else if (personOnJob.contract.qualifyingBonusTargetPosition != 0 && flag && personOnJob.contract.qualifyingBonus != 0 && num <= personOnJob.contract.qualifyingBonusTargetPosition)
		{
			this.AddContractBonusTransaction(personOnJob, Transaction.Group.Designer, false);
		}
	}

	private void AddContractBonusTransaction(Person inPerson, Transaction.Group inTransactionGroup, bool isRace)
	{
		int inPosition = (!isRace) ? inPerson.contract.qualifyingBonusTargetPosition : inPerson.contract.raceBonusTargetPosition;
		int inAmount = (!isRace) ? inPerson.contract.qualifyingBonus : inPerson.contract.raceBonus;
		StringVariableParser.stringValue1 = inPerson.shortName;
		StringVariableParser.ordinalNumberString = GameUtility.FormatForPosition(inPosition, null);
		string inName = Localisation.LocaliseID((!isRace) ? "PSG_10010557" : "PSG_10010558", null);
		Transaction transaction = new Transaction(inTransactionGroup, Transaction.Type.Debit, inAmount, inName);
		this.mTeam.financeController.unnallocatedTransactions.Add(transaction);
	}

	public int EndOfMonthCost()
	{
		int num = 0;
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (!this.mEmployeeSlots[i].IsAvailable())
			{
				Person personHired = this.mEmployeeSlots[i].personHired;
				int num2 = Mathf.RoundToInt((float)(personHired.contract.yearlyWages / 12));
				num += num2;
			}
		}
		return num;
	}

	public EmployeeSlot GetFreeSlot(Contract.Job inJob)
	{
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob && this.mEmployeeSlots[i].IsAvailable())
			{
				return this.mEmployeeSlots[i];
			}
		}
		return null;
	}

	public EmployeeSlot GetNextYearFreeSlot(Contract.Job inJob)
	{
		for (int i = 0; i < this.mNextYearEmployeeSlots.Count; i++)
		{
			if (this.mNextYearEmployeeSlots[i].jobType == inJob && this.mNextYearEmployeeSlots[i].IsAvailable())
			{
				return this.mNextYearEmployeeSlots[i];
			}
		}
		return null;
	}

	public EmployeeSlot GetSlot(Contract.Job inJob)
	{
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob)
			{
				return this.mEmployeeSlots[i];
			}
		}
		return null;
	}

	public int GetSlotIndexForPerson(Person inPerson)
	{
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].personHired != null && this.mEmployeeSlots[i].personHired == inPerson)
			{
				return i;
			}
		}
		return -1;
	}

	public T GetPersonOnJob<T>(Contract.Job inJob) where T : Person
	{
		Person personOnJob = this.GetPersonOnJob(inJob);
		if (personOnJob != null)
		{
			return personOnJob as T;
		}
		return (T)((object)null);
	}

	public Person GetPersonOnJob(Contract.Job inJob)
	{
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob && !this.mEmployeeSlots[i].IsAvailable())
			{
				return this.mEmployeeSlots[i].personHired;
			}
		}
		return null;
	}

	public List<T> GetAllPeopleOnJob<T>(Contract.Job inJob) where T : Person
	{
		List<Person> allPeopleOnJob = this.GetAllPeopleOnJob(inJob);
		return allPeopleOnJob.ConvertAll<T>((Person x) => (T)((object)x));
	}

	public List<Person> GetAllPeopleOnJob(Contract.Job inJob)
	{
		this.mCachedPeople.Clear();
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob && !this.mEmployeeSlots[i].IsAvailable())
			{
				this.mCachedPeople.Add(this.mEmployeeSlots[i].personHired);
			}
		}
		return this.mCachedPeople;
	}

	public void GetAllDrivers(ref List<Driver> drivers)
	{
		int count = this.mEmployeeSlots.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == Contract.Job.Driver && !this.mEmployeeSlots[i].IsAvailable())
			{
				Driver driver = this.mEmployeeSlots[i].personHired as Driver;
				drivers.Add(driver);
			}
		}
	}

	public void GetAllDriversForCar(ref List<Driver> drivers, int inCarIndex)
	{
		int count = this.mEmployeeSlots.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == Contract.Job.Driver && !this.mEmployeeSlots[i].IsAvailable())
			{
				Driver driver = this.mEmployeeSlots[i].personHired as Driver;
				if (driver.carID == inCarIndex)
				{
					drivers.Add(driver);
				}
			}
		}
	}

	public void GetAllMechanics(ref List<Mechanic> mechanics)
	{
		int count = this.mEmployeeSlots.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == Contract.Job.Mechanic && !this.mEmployeeSlots[i].IsAvailable())
			{
				Mechanic mechanic = this.mEmployeeSlots[i].personHired as Mechanic;
				mechanics.Add(mechanic);
			}
		}
	}

	public List<EmployeeSlot> GetAllEmployeeSlotsForJob(Contract.Job inJob)
	{
		this.mCachedEmployedSlots.Clear();
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob)
			{
				this.mCachedEmployedSlots.Add(this.mEmployeeSlots[i]);
			}
		}
		return this.mCachedEmployedSlots;
	}

	public void GetAllEmployeeSlotsForJob(Contract.Job inJob, ref List<EmployeeSlot> employee_slots)
	{
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].jobType == inJob)
			{
				employee_slots.Add(this.mEmployeeSlots[i]);
			}
		}
	}

	public List<EmployeeSlot> GetAllNextYearEmployeeSlotsForJob(Contract.Job inJob)
	{
		this.mCachedEmployedSlots.Clear();
		for (int i = 0; i < this.mNextYearEmployeeSlots.Count; i++)
		{
			if (this.mNextYearEmployeeSlots[i].jobType == inJob)
			{
				this.mCachedEmployedSlots.Add(this.mNextYearEmployeeSlots[i]);
			}
		}
		return this.mCachedEmployedSlots;
	}

	public List<Person> GetAllEmployees()
	{
		List<Person> list = new List<Person>();
		int count = this.mEmployeeSlots.Count;
		for (int i = 0; i < count; i++)
		{
			EmployeeSlot employeeSlot = this.mEmployeeSlots[i];
			if (!employeeSlot.IsAvailable())
			{
				list.Add(employeeSlot.personHired);
			}
		}
		return list;
	}

	public EmployeeSlot GetNextYearDriverSlot(int inIndex)
	{
		List<EmployeeSlot> allNextYearEmployeeSlotsForJob = this.GetAllNextYearEmployeeSlotsForJob(Contract.Job.Driver);
		return allNextYearEmployeeSlotsForJob[inIndex];
	}

	public void AddSignedContract(ContractPerson inContract)
	{
		inContract.SignContractAndSetContractEndEvents();
		ContractPerson inContract2 = inContract;
		inContract2.OnOptionClauseEnd = (Action)Delegate.Combine(inContract2.OnOptionClauseEnd, (Action)delegate()
		{
			this.OnOptionsClauseEnding(inContract);
		});
		if (this.mTeam == Game.instance.player.team)
		{
			if (inContract.person.contractManager.negotiationType == ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver)
			{
				Game.instance.dialogSystem.OnContractPromotionMessages(inContract.person);
			}
			else if (inContract.person.contractManager.isRenewProposal)
			{
				Game.instance.dialogSystem.OnContractRenewedMessages(inContract.person);
			}
			else
			{
				Game.instance.dialogSystem.OnContractSignedMessages(inContract.person);
			}
		}
		else if (Game.IsActive())
		{
			if (inContract.person.contractManager.negotiationType != ContractNegotiationScreen.NegotatiationType.PromoteReserveDriver)
			{
				if (!inContract.person.contractManager.isRenewProposal)
				{
					Game.instance.dialogSystem.OnAIContractSignedMessages(inContract.person);
				}
			}
		}
		this.RemoveDraftProposal(inContract.person);
	}

	public void SwapMechanicForDriver(Mechanic inMechanic, int inNewDriverIndex)
	{
		List<Person> allPeopleOnJob = this.GetAllPeopleOnJob(Contract.Job.Mechanic);
		Mechanic mechanic = null;
		for (int i = 0; i < allPeopleOnJob.Count; i++)
		{
			if (allPeopleOnJob[i] != inMechanic)
			{
				mechanic = (allPeopleOnJob[i] as Mechanic);
			}
		}
		if (mechanic != null)
		{
			mechanic.driver = inMechanic.driver;
			inMechanic.driver = inNewDriverIndex;
			mechanic.SetDefaultDriverRelationship();
			inMechanic.SetDefaultDriverRelationship();
		}
		if (this.mTeam.IsPlayersTeam())
		{
			StringVariableParser.subject = inMechanic;
			FeedbackPopup.Open(Localisation.LocaliseID("PSG_10007166", null), string.Empty);
		}
	}

	public void AddEmployeeSlot(Contract.Job inJobType, Person inPerson)
	{
		EmployeeSlot employeeSlot = new EmployeeSlot(inJobType, inPerson, this.mTeam);
		employeeSlot.slotID = this.mEmployeeSlots.Count;
		this.mEmployeeSlots.Add(employeeSlot);
		EmployeeSlot employeeSlot2 = new EmployeeSlot(inJobType, null, this.mTeam);
		employeeSlot2.slotID = this.mNextYearEmployeeSlots.Count;
		this.mNextYearEmployeeSlots.Add(employeeSlot2);
	}

	public EmployeeSlot GetEmployeeSlot(int index)
	{
		return this.mEmployeeSlots[index];
	}

	public EmployeeSlot GetSlotForPerson(Person inPerson)
	{
		EmployeeSlot result = null;
		for (int i = 0; i < this.mEmployeeSlots.Count; i++)
		{
			if (this.mEmployeeSlots[i].personHired == inPerson)
			{
				result = this.mEmployeeSlots[i];
				break;
			}
		}
		return result;
	}

	public EmployeeSlot GetNextYearSlotForPerson(Person inPerson)
	{
		EmployeeSlot result = null;
		for (int i = 0; i < this.mNextYearEmployeeSlots.Count; i++)
		{
			if (this.mNextYearEmployeeSlots[i].personHired == inPerson)
			{
				result = this.mNextYearEmployeeSlots[i];
				break;
			}
		}
		return result;
	}

	public ContractPerson CreateDefaultReplacementContract()
	{
		ContractPerson contractPerson = new ContractPerson();
		contractPerson.employeer = this.mTeam;
		contractPerson.startDate = Game.instance.time.now;
		contractPerson.endDate = contractPerson.startDate.AddYears(1);
		contractPerson.yearlyWages = 13000;
		contractPerson.qualifyingBonus = 0;
		contractPerson.raceBonus = 0;
		contractPerson.championBonus = 0;
		contractPerson.payDriver = 0;
		return contractPerson;
	}

	public TeamPrincipal HireReplacementTeamPrincipal()
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.TeamPrincipal);
		if (freeSlot != null)
		{
			TeamPrincipalManager teamPrincipalManager = Game.instance.teamPrincipalManager;
			TeamPrincipal replacementTeamPrincipal = teamPrincipalManager.GetReplacementTeamPrincipal(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.TeamPrincipal;
			this.HireNewPerson(contractPerson, replacementTeamPrincipal);
			return replacementTeamPrincipal;
		}
		return null;
	}

	public Chairman HireReplacementChairman()
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.Chairman);
		if (freeSlot != null)
		{
			ChairmanManager chairmanManager = Game.instance.chairmanManager;
			Chairman replacementChairman = chairmanManager.GetReplacementChairman(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.Chairman;
			this.HireNewPerson(contractPerson, replacementChairman);
			return replacementChairman;
		}
		return null;
	}

	public Assistant HireReplacementTeamAssistant(Nationality inNationality = null)
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.TeamAssistant);
		if (freeSlot != null)
		{
			AssistantManager assistantManager = Game.instance.assistantManager;
			Assistant assistant = (!(inNationality == null)) ? assistantManager.GetReplacementAssistantOfNationality(inNationality) : assistantManager.GetReplacementAssistant(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.TeamAssistant;
			this.HireNewPerson(contractPerson, assistant);
			return assistant;
		}
		return null;
	}

	public Scout HireReplacementScout(Nationality inNationality = null)
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.Scout);
		if (freeSlot != null)
		{
			ScoutManager scoutManager = Game.instance.scoutManager;
			Scout scout = (!(inNationality == null)) ? scoutManager.GetReplacementScoutOfNationality(inNationality) : scoutManager.GetReplacementScout(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.Scout;
			this.HireNewPerson(contractPerson, scout);
			return scout;
		}
		return null;
	}

	public Driver HireReplacementDriver()
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.Driver);
		if (freeSlot != null)
		{
			DriverManager driverManager = Game.instance.driverManager;
			Driver replacementDriver = driverManager.GetReplacementDriver(true);
			replacementDriver.SetMorale(0.5f);
			replacementDriver.moraleStatModificationHistory.ClearHistory();
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			List<EmployeeSlot> allEmployeeSlotsForJob = this.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
			bool flag = this.mTeam.championship.series == Championship.Series.EnduranceSeries || allEmployeeSlotsForJob[0] == freeSlot || allEmployeeSlotsForJob[1] == freeSlot;
			contractPerson.job = Contract.Job.Driver;
			contractPerson.SetCurrentStatus((!flag) ? ContractPerson.Status.Reserve : ContractPerson.Status.Equal);
			contractPerson.SetProposedStatus((!flag) ? ContractPerson.Status.Reserve : ContractPerson.Status.Equal);
			if (!this.mNextYearEmployeeSlots[freeSlot.slotID].IsAvailable())
			{
				contractPerson.endDate = this.mNextYearEmployeeSlots[freeSlot.slotID].personHired.nextYearContract.startDate;
			}
			this.HireNewDriver(contractPerson, replacementDriver);
			return replacementDriver;
		}
		return null;
	}

	public Engineer HireReplacementEngineer()
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.EngineerLead);
		if (freeSlot != null)
		{
			EngineerManager engineerManager = Game.instance.engineerManager;
			Engineer replacementEngineer = engineerManager.GetReplacementEngineer(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.EngineerLead;
			this.HireNewPerson(contractPerson, replacementEngineer);
			return replacementEngineer;
		}
		return null;
	}

	public Mechanic HireReplacementMechanic()
	{
		EmployeeSlot freeSlot = this.GetFreeSlot(Contract.Job.Mechanic);
		if (freeSlot != null)
		{
			MechanicManager mechanicManager = Game.instance.mechanicManager;
			Mechanic replacementMechanic = mechanicManager.GetReplacementMechanic(true);
			ContractPerson contractPerson = this.CreateDefaultReplacementContract();
			contractPerson.job = Contract.Job.Mechanic;
			Person personOnJob = this.GetPersonOnJob(Contract.Job.Mechanic);
			Mechanic mechanic = personOnJob as Mechanic;
			if (mechanic != null)
			{
				replacementMechanic.driver = 1 - mechanic.driver;
			}
			this.HireNewPerson(contractPerson, replacementMechanic);
			return replacementMechanic;
		}
		return null;
	}

	public void FirePerson(Person inPersonToFire, Contract.ContractTerminationType inContractTerminationType = Contract.ContractTerminationType.Generic)
	{
		if (this.hasDraftProposal(inPersonToFire) && inPersonToFire.contractManager.isRenewProposal)
		{
			if (this.mTeam.IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnContractElapsedOrFiredWhileRenewing(inPersonToFire);
			}
			if (inPersonToFire.contractManager.isConsideringProposal)
			{
				this.CancelDraftProposal(inPersonToFire);
			}
			else
			{
				this.RemoveDraftProposal(inPersonToFire);
			}
		}
		inPersonToFire.contract.job = Contract.Job.Unemployed;
		inPersonToFire.contract.SetContractTerminated(inContractTerminationType);
		EmployeeSlot slotForPerson = this.GetSlotForPerson(inPersonToFire);
		slotForPerson.personHired = null;
		inPersonToFire.careerHistory.MarkLastEntryTeamAsFinished(this.mTeam);
		if (Game.IsActive() && this.mTeam.IsPlayersTeam() && inPersonToFire != Game.instance.player && !Game.instance.dilemmaSystem.isFiringBecauseOfDilemma)
		{
			Game.instance.player.ApplyLoyaltyChange(Player.LoyaltyChange.FiringTeamMember);
		}
		if (Game.IsActive())
		{
			Game.instance.teamManager.teamRumourManager.RemoveRumoursForPerson(inPersonToFire);
		}
	}

	public void FireDriver(Person inPersonToFire, Contract.ContractTerminationType inContractTerminationType = Contract.ContractTerminationType.Generic)
	{
		Driver driver = inPersonToFire as Driver;
		Game.instance.driverManager.RemoveDriverEntryFromChampionship(inPersonToFire as Driver);
		this.mTeam.ClearSelectedDriversForSession();
		if (this.mTeam.IsPlayersTeam())
		{
			RaceEventDetails previousEventDetails = this.mTeam.championship.GetPreviousEventDetails();
			if (previousEventDetails != null)
			{
				RaceEventResults.SessonResultData resultsForSession = previousEventDetails.results.GetResultsForSession(SessionDetails.SessionType.Race);
				RaceEventResults.ResultData resultForDriver = resultsForSession.GetResultForDriver(inPersonToFire as Driver);
				if (resultForDriver != null)
				{
					this.mLatestFiredActiveDriver = driver;
				}
			}
			Game.instance.player.promisesController.RemovePromisesForDriver(inPersonToFire as Driver);
		}
		driver.personalityTraitController.RemovePersonalityTraitsRelatedToTeam();
		this.FirePerson(inPersonToFire, inContractTerminationType);
		if (inContractTerminationType == Contract.ContractTerminationType.FiredByPlayer)
		{
			driver.UpdateMoraleOnFired();
			this.UpdateAchievementsDriverFired();
		}
		if (Game.IsActive())
		{
			Game.instance.challengeManager.NotifyChallengeManagerOfGameEventAndCheckCompletion(ChallengeManager.ChallengeManagerGameEvents.DriverLeftTeam);
		}
	}

	public void PromoteDriver(Person inPersonToDemote, Person inPersonToPromote)
	{
		EmployeeSlot slotForPerson = this.GetSlotForPerson(inPersonToDemote);
		EmployeeSlot slotForPerson2 = this.GetSlotForPerson(inPersonToPromote);
		slotForPerson.personHired = inPersonToPromote;
		slotForPerson2.personHired = inPersonToDemote;
		inPersonToPromote.contract.SetCurrentStatus(inPersonToDemote.contract.currentStatus);
		inPersonToDemote.contract.SetCurrentStatus(ContractPerson.Status.Reserve);
		Driver driver = inPersonToDemote as Driver;
		Driver driver2 = inPersonToPromote as Driver;
		driver.UpdateMoraleWithPromotionDemotion();
		driver2.UpdateMoraleWithPromotionDemotion();
		DriverManager driverManager = Game.instance.driverManager;
		driverManager.RemoveDriverEntryFromChampionship(inPersonToDemote as Driver);
		this.mTeam.ClearSelectedDriversForSession();
		driverManager.AddDriverToChampionship(inPersonToPromote as Driver, false);
		this.mTeam.SelectMainDriversForSession();
		this.mTeam.championship.standings.UpdateStandings();
		Driver driver3 = (Driver)inPersonToPromote;
		Mechanic mechanicOfDriver = this.mTeam.GetMechanicOfDriver(driver3);
		mechanicOfDriver.SetDefaultDriverRelationship();
		driver3.carOpinion.CalculateDriverOpinions(driver3);
		if (this.mTeam.IsPlayersTeam())
		{
			Game.instance.dialogSystem.OnContractDemotionMessages(inPersonToDemote);
			Game.instance.dialogSystem.OnContractPromotionMessages(inPersonToPromote);
		}
	}

	public void HireNewPerson(ContractPerson inDraftContract, Person inNewPersonToHire)
	{
		bool flag = false;
		if (!inNewPersonToHire.IsFreeAgent())
		{
			Team team = inNewPersonToHire.contract.GetTeam();
			if (team != inDraftContract.GetTeam())
			{
				flag = team.IsPlayersTeam();
				if (!inNewPersonToHire.IsReplacementPerson())
				{
					this.PayOtherTeamTerminationCosts(team, inNewPersonToHire.contract);
				}
				team.contractManager.FirePerson(inNewPersonToHire, Contract.ContractTerminationType.HiredBySomeoneElse);
				if (inNewPersonToHire is Mechanic)
				{
					team.contractManager.HireReplacementMechanic();
				}
				else if (inNewPersonToHire is Engineer)
				{
					team.contractManager.HireReplacementEngineer();
				}
				else if (inNewPersonToHire is Driver)
				{
					team.contractManager.HireReplacementDriver();
				}
			}
		}
		EmployeeSlot freeSlot = this.GetFreeSlot(inDraftContract.job);
		if (freeSlot != null)
		{
			freeSlot.personHired = inNewPersonToHire;
			inNewPersonToHire.contract = inDraftContract;
			inNewPersonToHire.contract.SetPerson(inNewPersonToHire);
			inNewPersonToHire.contract.SetContractState(Contract.ContractStatus.OnGoing);
			this.AddSignedContract(inNewPersonToHire.contract);
			if (inNewPersonToHire is Mechanic)
			{
				Mechanic mechanic = inNewPersonToHire as Mechanic;
				mechanic.SetDefaultDriverRelationship();
				this.mTeam.carManager.partImprovement.AssignChiefMechanics();
				if (this.mTeam.HasAIPitcrew)
				{
					this.mTeam.pitCrewController.AIPitCrew.RegenerateTaskStats();
				}
			}
			if (inNewPersonToHire is TeamPrincipal)
			{
				Chairman chairman = inNewPersonToHire.contract.GetTeam().chairman;
				chairman.ResetHappiness();
			}
			if (inNewPersonToHire is Chairman)
			{
				Chairman chairman2 = inNewPersonToHire as Chairman;
				chairman2.ResetHappiness();
			}
		}
		if (flag)
		{
			Game.instance.dialogSystem.OnDriverPoached(inNewPersonToHire);
		}
		Game.instance.teamManager.teamRumourManager.RemoveRumoursForPerson(inNewPersonToHire);
	}

	private void PayOtherTeamTerminationCosts(Team inOldTeam, ContractPerson inOldContract)
	{
		Transaction.Group inType = Transaction.Group.Count;
		Contract.Job job = inOldContract.job;
		switch (job)
		{
		case Contract.Job.Driver:
			inType = Transaction.Group.Drivers;
			break;
		default:
			if (job == Contract.Job.Mechanic)
			{
				inType = Transaction.Group.Mechanics;
			}
			break;
		case Contract.Job.EngineerLead:
			inType = Transaction.Group.Designer;
			break;
		}
		StringVariableParser.stringValue1 = inOldContract.person.shortName;
		Transaction transaction = new Transaction(inType, Transaction.Type.Credit, inOldContract.GetContractTerminationCost(), Localisation.LocaliseID("PSG_10010574", null));
		if (inOldTeam.IsPlayersTeam() && inOldContract.person is Driver)
		{
			inOldTeam.financeController.unnallocatedTransactions.Add(transaction);
		}
		else
		{
			inOldTeam.financeController.finance.ProcessTransactions(null, null, false, new Transaction[]
			{
				transaction
			});
		}
	}

	public void HireNewDriver(ContractPerson inDraftContract, Person inPersonToHire)
	{
		Driver driver = inPersonToHire as Driver;
		this.mTeam.AssignDriverToCar(driver);
		this.HireNewPerson(inDraftContract, inPersonToHire);
		if (!driver.IsReplacementPerson())
		{
			driver.moraleStatModificationHistory.ClearHistory();
			driver.UpdateMoraleOnContractSigned();
		}
		DriverManager driverManager = Game.instance.driverManager;
		if (inDraftContract.currentStatus != ContractPerson.Status.Reserve)
		{
			driverManager.AddDriverToChampionship(driver, false);
			this.mTeam.SelectMainDriversForSession();
			this.mTeam.championship.standings.UpdateStandings();
			Mechanic mechanicOfDriver = this.mTeam.GetMechanicOfDriver(driver);
			if (mechanicOfDriver != null)
			{
				mechanicOfDriver.SetDefaultDriverRelationship();
			}
		}
		if (this.mTeam.IsPlayersTeam())
		{
			driver.SetBeenScouted();
			ScoutingManager scoutingManager = Game.instance.scoutingManager;
			if (scoutingManager.IsScouting() && (scoutingManager.IsDriverCurrentlyScouted(driver) || scoutingManager.IsDriverInScoutQueue(driver)))
			{
				Game.instance.scoutingManager.RemoveDriverFromScoutingJobs(driver);
			}
		}
		else
		{
			this.mTeam.teamAIController.RemoveDriverFromScoutingJobs(driver);
			this.mTeam.teamAIController.EvaluateDriverLineUp();
		}
	}

	public void FireNextYearDriver(Person inDriverToFire)
	{
		EmployeeSlot slotForPerson = this.GetSlotForPerson(inDriverToFire);
		if (slotForPerson != null && !slotForPerson.IsAvailable() && inDriverToFire.contract.IsContractedForNextSeason())
		{
			int year = Game.instance.time.now.Year;
			inDriverToFire.contract.endDate = new DateTime(year, 12, 31);
		}
		else
		{
			EmployeeSlot nextYearSlotForPerson = this.GetNextYearSlotForPerson(inDriverToFire);
			if (nextYearSlotForPerson != null)
			{
				inDriverToFire.nextYearContract.job = Contract.Job.Unemployed;
				inDriverToFire.nextYearContract.SetContractTerminated(Contract.ContractTerminationType.Generic);
				nextYearSlotForPerson.personHired = null;
			}
		}
	}

	public void HireNewNextYearDriver(ContractPerson inDraftContract, Person inNewPersonToHire, int inDriverIndex)
	{
		if (!inNewPersonToHire.IsFreeAgent())
		{
			Team team = inNewPersonToHire.contract.GetTeam();
			team.contractManager.FireNextYearDriver(inNewPersonToHire);
		}
		EmployeeSlot nextYearDriverSlot = this.GetNextYearDriverSlot(inDriverIndex);
		if (nextYearDriverSlot != null)
		{
			nextYearDriverSlot.personHired = inNewPersonToHire;
			inNewPersonToHire.nextYearContract = inDraftContract;
			inNewPersonToHire.nextYearContract.SetPerson(inNewPersonToHire);
			inNewPersonToHire.nextYearContract.SetContractState(Contract.ContractStatus.OnGoing);
			this.AddSignedContract(inNewPersonToHire.nextYearContract);
		}
	}

	public void ReplaceCurrentDriverWithNewOne(ContractPerson inDraftContract, Person inNewPersonToHire, Person inPersonToReplace)
	{
		int oldID = ((Driver)inPersonToReplace).carID;
		StringVariableParser.personReplaced = inPersonToReplace;
		inDraftContract.SetCurrentStatus(inPersonToReplace.contract.currentStatus);
		this.FireDriver(inPersonToReplace, Contract.ContractTerminationType.FiredByPlayer);
		this.HireNewDriver(inDraftContract, inNewPersonToHire);
		Driver driver = (Driver)inNewPersonToHire;
		driver.UpdateMoraleStatusAgainstWhatWasPromised();
		if (driver.contract.currentStatus != ContractPerson.Status.Reserve)
		{
			driver.carOpinion.CalculateDriverOpinions(driver);
		}
		driver.SetCarID(oldID);
		if (this.mTeam.IsPlayersTeam())
		{
			global::Debug.LogErrorFormat("ReplaceCurrentDriverWithNewOne - Player Team Driver 1 is {0} and has CarID {1}", new object[]
			{
				this.mTeam.GetDriver(0).shortName,
				this.mTeam.GetDriver(0).carID
			});
			global::Debug.LogErrorFormat("ReplaceCurrentDriverWithNewOne - Player Team Driver 2 is {0} and has CarID {1}", new object[]
			{
				this.mTeam.GetDriver(1).shortName,
				this.mTeam.GetDriver(1).carID
			});
		}
	}

	public void ReplacePersonWithNewOne(ContractPerson inDraftContract, Person inNewPersonToHire, Person inPersonToReplace)
	{
		if (inNewPersonToHire is Mechanic)
		{
			Mechanic mechanic = inPersonToReplace as Mechanic;
			Mechanic mechanic2 = inNewPersonToHire as Mechanic;
			mechanic2.driver = mechanic.driver;
		}
		StringVariableParser.personReplaced = inPersonToReplace;
		this.FirePerson(inPersonToReplace, Contract.ContractTerminationType.Generic);
		this.HireNewPerson(inDraftContract, inNewPersonToHire);
	}

	public void ReplaceNextYearDriverWithNewOne(ContractPerson inDraftContract, Person inNewPersonToHire, Person inPersonToReplace)
	{
		EmployeeSlot slotForPerson = this.GetSlotForPerson(inPersonToReplace);
		int slotID;
		if (slotForPerson != null)
		{
			slotID = slotForPerson.slotID;
		}
		else
		{
			EmployeeSlot nextYearSlotForPerson = this.GetNextYearSlotForPerson(inPersonToReplace);
			slotID = nextYearSlotForPerson.slotID;
		}
		this.FireNextYearDriver(inPersonToReplace);
		this.HireNewNextYearDriver(inDraftContract, inNewPersonToHire, slotID);
	}

	private void OnOptionsClauseEnding(ContractPerson inContract)
	{
		int random = RandomUtility.GetRandom(0, 100);
		if (random < 10)
		{
			inContract.SetContractState(Contract.ContractStatus.OnGoing);
			inContract.endDate = inContract.endDate.AddDays(2.0);
			inContract.optionClauseEndDate = inContract.optionClauseEndDate.AddDays(1.0);
		}
		else
		{
			inContract.SetContractState(Contract.ContractStatus.Terminated);
		}
	}

	public void SwapCurrentDriverSlotWithNextSeasonDriver(int inCurrentDriverIndex)
	{
		List<EmployeeSlot> allEmployeeSlotsForJob = this.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
		EmployeeSlot employeeSlot = allEmployeeSlotsForJob[inCurrentDriverIndex];
		EmployeeSlot nextYearDriverSlot = this.GetNextYearDriverSlot(inCurrentDriverIndex);
		EmployeeSlot slotForPerson = this.GetSlotForPerson(employeeSlot.personHired);
		if (slotForPerson.slotID == employeeSlot.slotID)
		{
			employeeSlot.personHired.contract.job = Contract.Job.Unemployed;
			employeeSlot.personHired.contract.SetContractTerminated(Contract.ContractTerminationType.Generic);
		}
		employeeSlot.personHired = null;
		employeeSlot.personHired = nextYearDriverSlot.personHired;
		employeeSlot.personHired.contract = nextYearDriverSlot.personHired.nextYearContract;
		employeeSlot.personHired.contract.SetPerson(employeeSlot.personHired);
		employeeSlot.personHired.contract.SetContractState(Contract.ContractStatus.OnGoing);
		this.AddSignedContract(employeeSlot.personHired.contract);
		DriverManager driverManager = Game.instance.driverManager;
		if (employeeSlot.personHired.contract.proposedStatus != ContractPerson.Status.Reserve)
		{
			driverManager.AddDriverToChampionship(employeeSlot.personHired as Driver, false);
			employeeSlot.personHired.contract.GetTeam().championship.standings.UpdateStandings();
		}
		nextYearDriverSlot.personHired.nextYearContract = null;
		nextYearDriverSlot.personHired = null;
	}

	public void OnContractEnd(ContractPerson inContract)
	{
		if (this.hasDraftProposal(inContract.person) && inContract.person.contractManager.isRenewProposal)
		{
			if (this.mTeam.IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnContractElapsedOrFiredWhileRenewing(inContract.person);
			}
			if (inContract.person.contractManager.isConsideringProposal)
			{
				this.CancelDraftProposal(inContract.person);
			}
			else
			{
				this.RemoveDraftProposal(inContract.person);
			}
		}
		switch (inContract.job)
		{
		case Contract.Job.Driver:
			this.OnDriverContractEnd(inContract);
			break;
		case Contract.Job.EngineerLead:
			this.OnEngineerContractEnd(inContract);
			break;
		case Contract.Job.TeamAssistant:
		case Contract.Job.TeamPrincipal:
		case Contract.Job.Scout:
		case Contract.Job.Chairman:
			this.RenewContractPersonUntilNextSeason(inContract);
			break;
		case Contract.Job.Mechanic:
			this.OnMechanicContractEnd(inContract);
			break;
		}
	}

	private void RenewContractPersonUntilNextSeason(ContractPerson inContract)
	{
		ContractPerson contractPerson = new ContractPerson(inContract);
		contractPerson.ResetCalendarEvent();
		contractPerson.startDate = Game.instance.time.now;
		contractPerson.endDate = new DateTime(Game.instance.time.now.Year + 1, 12, 31);
		contractPerson.optionClauseEndDate = Game.instance.time.now.AddHours(1.0);
		this.RenewContractForPerson(inContract.person, contractPerson);
	}

	public void RenewContractForPerson(Person personToRenew, ContractPerson inNewContract)
	{
		inNewContract.SetCurrentStatus(personToRenew.contract.currentStatus);
		personToRenew.contract.SetContractTerminated(Contract.ContractTerminationType.Generic);
		personToRenew.contract = inNewContract;
		personToRenew.contract.SetPerson(personToRenew);
		personToRenew.contract.SetContractState(Contract.ContractStatus.OnGoing);
		this.AddSignedContract(personToRenew.contract);
		if (personToRenew is Driver && !personToRenew.IsReplacementPerson())
		{
			Driver driver = personToRenew as Driver;
			driver.UpdateMoraleOnContractSigned();
		}
		if (this.mTeam.IsPlayersTeam())
		{
			this.mTeam.CheckIfDriversPromisedAreFulfilled();
		}
	}

	private void OnDriverContractEnd(ContractPerson inContract)
	{
		Driver inPerson = inContract.person as Driver;
		List<EmployeeSlot> allEmployeeSlotsForJob = this.GetAllEmployeeSlotsForJob(Contract.Job.Driver);
		int num = -1;
		if (inContract.proposedStatus != ContractPerson.Status.Reserve)
		{
			for (int i = 0; i < Team.maxDriverCount; i++)
			{
				if (allEmployeeSlotsForJob[i].personHired != null && allEmployeeSlotsForJob[i].personHired == inContract.person)
				{
					num = i;
					break;
				}
			}
		}
		else
		{
			num = 2;
		}
		EmployeeSlot nextYearDriverSlot = this.GetNextYearDriverSlot(num);
		if (nextYearDriverSlot.IsAvailableNextYear())
		{
			DriverManager driverManager = Game.instance.driverManager;
			if (driverManager.IsReplacementPerson(inPerson))
			{
				this.RenewContractPersonUntilNextSeason(inContract);
			}
			else
			{
				this.FireDriver(inContract.person, Contract.ContractTerminationType.ContractElapsed);
				this.HireReplacementDriver();
			}
		}
		else if (nextYearDriverSlot.personHired.nextYearContract.startDate > Game.instance.time.now)
		{
			this.FireDriver(inContract.person, Contract.ContractTerminationType.ContractElapsed);
			this.HireReplacementDriver();
		}
		else
		{
			this.SwapCurrentDriverSlotWithNextSeasonDriver(num);
		}
	}

	private void OnEngineerContractEnd(ContractPerson inContract)
	{
		int slotIndexForPerson = this.GetSlotIndexForPerson(inContract.person);
		EmployeeSlot employeeSlot = this.mNextYearEmployeeSlots[slotIndexForPerson];
		if (employeeSlot.IsAvailable())
		{
			EngineerManager engineerManager = Game.instance.engineerManager;
			Engineer inPerson = inContract.person as Engineer;
			if (engineerManager.IsReplacementPerson(inPerson))
			{
				this.RenewContractPersonUntilNextSeason(inContract);
			}
			else
			{
				this.FirePerson(inContract.person, Contract.ContractTerminationType.ContractElapsed);
				this.HireReplacementEngineer();
			}
		}
		else
		{
			this.ReplacePersonWithNewOne(employeeSlot.personHired.contract, employeeSlot.personHired, inContract.person);
			employeeSlot.personHired = null;
		}
	}

	private void OnMechanicContractEnd(ContractPerson inContract)
	{
		int slotIndexForPerson = this.GetSlotIndexForPerson(inContract.person);
		EmployeeSlot employeeSlot = this.mNextYearEmployeeSlots[slotIndexForPerson];
		if (employeeSlot.IsAvailable())
		{
			MechanicManager mechanicManager = Game.instance.mechanicManager;
			Mechanic inPerson = inContract.person as Mechanic;
			if (mechanicManager.IsReplacementPerson(inPerson))
			{
				this.RenewContractPersonUntilNextSeason(inContract);
			}
			else
			{
				this.FirePerson(inContract.person, Contract.ContractTerminationType.ContractElapsed);
				this.HireReplacementMechanic();
			}
		}
		else
		{
			this.ReplacePersonWithNewOne(employeeSlot.personHired.contract, employeeSlot.personHired, inContract.person);
			employeeSlot.personHired = null;
		}
	}

	public void ProposeNewDraftContract(ContractPerson inDraftContract, Person inPerson, ContractNegotiationScreen.NegotatiationType inType, ContractNegotiationScreen.ContractYear inYear)
	{
		inPerson.contractManager.ReceiveDraftProposal(inPerson.contract, inDraftContract, inType, inYear);
		if (!this.mProposedDrafts.Contains(inPerson))
		{
			this.mProposedDrafts.Add(inPerson);
		}
	}

	public bool hasDraftProposal(Person inPerson)
	{
		return this.mProposedDrafts.Contains(inPerson);
	}

	public void RemoveDraftProposal(Person inPerson)
	{
		inPerson.contractManager.RemoveDraftProposal();
		this.mProposedDrafts.Remove(inPerson);
	}

	public void CancelDraftProposal(Person inPerson)
	{
		inPerson.contractManager.CancelProposal();
		this.mProposedDrafts.Remove(inPerson);
	}

	public void UpdateAchievementsDriverFired()
	{
		if (this.mTeam == Game.instance.player.team)
		{
			App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Fire_A_Driver);
		}
	}

	public void SetupContractManagerEvents()
	{
		DateTime defaultContractEndDate = ContractPerson.DefaultContractEndDate;
		TimeSpan timeSpan = new TimeSpan(30, 0, 0, 0);
		CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
		{
			showOnCalendar = false,
			category = CalendarEventCategory.Contract,
			triggerDate = defaultContractEndDate.Subtract(timeSpan),
			triggerState = GameState.Type.FrontendState,
			interruptGameTime = false,
			OnEventTrigger = MMAction.CreateFromAction(new Action(this.OnContractEndingsEvent))
		};
		calendarEvent_v.SetDynamicDescription("Contract Endings");
		Game.instance.calendar.AddEvent(calendarEvent_v);
	}

	private void OnContractEndingsEvent()
	{
		if (this.mTeam.IsPlayersTeam())
		{
			Game.instance.dialogSystem.OnContractsEndingMessages();
		}
	}

	public int employeeSlotsCount
	{
		get
		{
			return this.mEmployeeSlots.Count;
		}
	}

	public List<Person> proposedDrafts
	{
		get
		{
			return this.mProposedDrafts;
		}
	}

	public Driver latestFiredActiveDriver
	{
		get
		{
			return this.mLatestFiredActiveDriver;
		}
	}

	private List<EmployeeSlot> mEmployeeSlots = new List<EmployeeSlot>();

	private List<EmployeeSlot> mNextYearEmployeeSlots = new List<EmployeeSlot>();

	private List<Contract> mSignedContracts;

	private List<Person> mProposedDrafts = new List<Person>();

	private List<Person> mCachedPeople = new List<Person>();

	private List<EmployeeSlot> mCachedEmployedSlots = new List<EmployeeSlot>();

	private Team mTeam;

	private Driver mLatestFiredActiveDriver;

	private Driver mHealingDriver;
}
