using System;
using System.Collections.Generic;
using System.Text;
using FullSerializer;
using UnityEngine;

[fsObject("v2", new Type[]
{
	typeof(PersonalityTraitController)
}, MemberSerialization = fsMemberSerialization.OptOut)]
public class PersonalityTraitController_v2
{
	public PersonalityTraitController_v2(PersonalityTraitController inOldController)
	{
		this.permanentPersonalityTraits = inOldController.permanentPersonalityTraits;
		this.temporaryPersonalityTraits = inOldController.temporaryPersonalityTraits;
		this.mTraitHistory = inOldController.mTraitHistory;
		this.mMaxCooldownDaysRange = inOldController.mMaxCooldownDaysRange;
		this.cooldownPeriodEnd = inOldController.cooldownPeriodEnd;
		this.mLastRandomCooldownDayValue = inOldController.mLastRandomCooldownDayValue;
		this.mDriver = inOldController.mDriver;
		this.mDriverStats = inOldController.mDriverStats;
		this.allTraits = new List<PersonalityTrait>();
	}

	public PersonalityTraitController_v2(Driver inDriver)
	{
		this.mDriver = inDriver;
	}

	public PersonalityTraitController_v2()
	{
		this.allTraits = new List<PersonalityTrait>();
	}

	public PersonalityTrait AddPersonalityTrait(PersonalityTraitData inPersonalityTraitData, bool inActivatePersonalityTraitTrigger)
	{
		PersonalityTrait personalityTrait = new PersonalityTrait(inPersonalityTraitData, this.mDriver);
		if (personalityTrait.data.removesTraits != null)
		{
			for (int i = 0; i < personalityTrait.data.removesTraits.Length; i++)
			{
				int num = personalityTrait.data.removesTraits[i];
				for (int j = 0; j < this.permanentPersonalityTraits.Count; j++)
				{
					if (num == this.permanentPersonalityTraits[j].data.ID)
					{
						this.RemovePersonalityTrait(this.permanentPersonalityTraits[j]);
						j--;
					}
				}
				for (int k = 0; k < this.temporaryPersonalityTraits.Count; k++)
				{
					if (num == this.temporaryPersonalityTraits[k].data.ID)
					{
						this.RemovePersonalityTrait(this.temporaryPersonalityTraits[k]);
						k--;
					}
				}
			}
		}
		if (inPersonalityTraitData.type == PersonalityTraitData.TraitType.Permanent)
		{
			this.permanentPersonalityTraits.Add(personalityTrait);
		}
		else if (inPersonalityTraitData.type == PersonalityTraitData.TraitType.Temporary)
		{
			personalityTrait.SetupTraitEndTime();
			this.temporaryPersonalityTraits.Add(personalityTrait);
		}
		this.allTraits.Add(personalityTrait);
		if (!inPersonalityTraitData.isRepeatable)
		{
			this.mTraitHistory.Add(inPersonalityTraitData.ID);
		}
		this.CheckTraitAppliesModifierPotential(personalityTrait);
		personalityTrait.OnTraitStart();
		if (inActivatePersonalityTraitTrigger)
		{
			this.ActivatePersonalityTraitTrigger(personalityTrait);
		}
		if (Game.IsActive())
		{
			this.mDriver.driverStamina.Reset();
		}
		return personalityTrait;
	}

	public void OnLoad()
	{
		if (this.allTraits == null || this.allTraits.Count == 0 || this.permanentPersonalityTraits.Count + this.temporaryPersonalityTraits.Count != this.allTraits.Count)
		{
			if (this.allTraits == null)
			{
				this.allTraits = new List<PersonalityTrait>();
			}
			this.allTraits.Clear();
			this.allTraits.AddRange(this.permanentPersonalityTraits);
			this.allTraits.AddRange(this.temporaryPersonalityTraits);
		}
	}

	public bool HasInjuryTrait()
	{
		return this.HasSpecialCase(PersonalityTrait.SpecialCaseType.NeckInjury) || this.HasSpecialCase(PersonalityTrait.SpecialCaseType.FaceInjury) || this.HasSpecialCase(PersonalityTrait.SpecialCaseType.InjuryBurns);
	}

	public string GetInjurySprite(out PersonalityTrait.SpecialCaseType outInjuryType)
	{
		string result = string.Empty;
		if (this.HasSpecialCase(PersonalityTrait.SpecialCaseType.NeckInjury))
		{
			outInjuryType = PersonalityTrait.SpecialCaseType.NeckInjury;
			result = "FaceInjury";
		}
		else if (this.HasSpecialCase(PersonalityTrait.SpecialCaseType.FaceInjury))
		{
			outInjuryType = PersonalityTrait.SpecialCaseType.FaceInjury;
			result = "FaceInjury2";
		}
		else if (this.HasSpecialCase(PersonalityTrait.SpecialCaseType.InjuryBurns))
		{
			outInjuryType = PersonalityTrait.SpecialCaseType.InjuryBurns;
			result = "FaceInjury3";
		}
		else
		{
			outInjuryType = PersonalityTrait.SpecialCaseType.Count;
		}
		return result;
	}

	private void CheckTraitAppliesModifierPotential(PersonalityTrait inPersonalityTrait)
	{
		if (inPersonalityTrait.DoesModifyStat(PersonalityTrait.StatModified.Potential))
		{
			this.mDriver.UpdateModifiedPotentialValue(inPersonalityTrait.GetSingleModifierForStat(PersonalityTrait.StatModified.Potential));
		}
	}

	public void RemoveAllPersonalityTraits()
	{
		this.RemovePersonalityTraits(new List<PersonalityTrait>(this.temporaryPersonalityTraits), false);
		this.RemovePersonalityTraits(new List<PersonalityTrait>(this.permanentPersonalityTraits), false);
	}

	private void RemovePersonalityTrait(PersonalityTrait inPersonalityTrait)
	{
		this.EndPersonalityTraitTrigger(inPersonalityTrait);
		if (inPersonalityTrait.data.type == PersonalityTraitData.TraitType.Permanent)
		{
			this.permanentPersonalityTraits.Remove(inPersonalityTrait);
		}
		else if (inPersonalityTrait.data.type == PersonalityTraitData.TraitType.Temporary)
		{
			this.temporaryPersonalityTraits.Remove(inPersonalityTrait);
			if (!this.mDriver.IsFreeAgent() && this.mDriver.contract.GetTeam().IsPlayersTeam() && !App.instance.gameStateManager.currentState.IsFrontend())
			{
				if (this.raceTraitsHistory == null)
				{
					this.raceTraitsHistory = new List<PersonalityTrait>();
				}
				this.raceTraitsHistory.Add(inPersonalityTrait);
			}
		}
		if (this.allTraits.Contains(inPersonalityTrait))
		{
			this.allTraits.Remove(inPersonalityTrait);
		}
	}

	private void RemovePersonalityTraits(List<PersonalityTrait> inPersonalityTraits, bool inEndTrait = true)
	{
		for (int i = 0; i < inPersonalityTraits.Count; i++)
		{
			if (inEndTrait)
			{
				inPersonalityTraits[i].OnTraitEnd();
			}
			this.RemovePersonalityTrait(inPersonalityTraits[i]);
		}
	}

	public bool IsTraitOppositeToCurrentOnes(PersonalityTraitData inPersonalityTraitData)
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].data.IsTraitOpposite(inPersonalityTraitData))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsTraitEvolvingFromCurrentOnes(PersonalityTraitData inPersonalityTraitData)
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			int[] evolvesInto = this.allTraits[i].data.evolvesInto;
			for (int j = 0; j < evolvesInto.Length; j++)
			{
				if (inPersonalityTraitData.ID == evolvesInto[j])
				{
					return true;
				}
			}
		}
		return false;
	}

	public void SetFirstCooldownDate()
	{
		bool flag = !this.mDriver.IsFreeAgent() && this.mDriver.contract.GetTeam().championship.series == Championship.Series.EnduranceSeries;
		this.mLastRandomCooldownDayValue = RandomUtility.GetRandomInc(0, (!flag) ? this.mMaxCooldownDaysRange : this.mMaxCooldownDaysRangeEndurance);
		this.cooldownPeriodEnd = Game.instance.time.now.AddDays((double)this.mLastRandomCooldownDayValue);
	}

	public void SetupCooldownPeriod()
	{
		bool flag = !this.mDriver.IsFreeAgent() && this.mDriver.contract.GetTeam().championship.series == Championship.Series.EnduranceSeries;
		int num = (!flag) ? this.mMaxCooldownDaysRange : this.mMaxCooldownDaysRangeEndurance;
		int num2 = 0;
		if (this.cooldownPeriodEnd > Game.instance.time.now)
		{
			num2 = Mathf.RoundToInt((float)Game.instance.time.now.Subtract(this.cooldownPeriodEnd).TotalDays);
		}
		int randomInc = RandomUtility.GetRandomInc(0, num);
		int num3 = num - this.mLastRandomCooldownDayValue + num2 + randomInc;
		if (num3 > 0)
		{
			this.cooldownPeriodEnd = Game.instance.time.now.AddDays((double)num3);
			this.mLastRandomCooldownDayValue = randomInc;
		}
	}

	public void UpdatePersonalityTraits()
	{
		if (!this.mDriver.IsReplacementPerson())
		{
			this.UpdateTemporaryTraits();
			bool flag = this.cooldownPeriodEnd <= Game.instance.time.now;
			if (flag)
			{
				bool flag2 = RandomUtility.GetRandom(0f, 1f) >= ((!this.mDriver.IsFreeAgent() && !this.mDriver.IsMainDriver()) ? 0.5f : -1f);
				if (flag2 && !this.TryPermanentTraitsEvolution())
				{
					this.TryGetNewPersonalityTrait();
				}
				this.SetupCooldownPeriod();
			}
		}
	}

	public void CheckPersonalityTraitEventTrigger(PersonalityTraitData.EventTriggerType inEventType)
	{
		if (!this.mDriver.IsReplacementPerson())
		{
			List<PersonalityTraitData> list = this.FilterPossibleTraits(PersonalityTraitData.TraitType.Temporary, inEventType, new PersonalityTraitController_v2.DialogCriteriaFilter(this.GetTraitCriteriaForPostRace), false);
			list.Sort((PersonalityTraitData X, PersonalityTraitData Y) => X.probability.CompareTo(Y.probability));
			this.PickRandomTraitFromList(list);
		}
	}

	private void UpdateTemporaryTraits()
	{
		bool flag = false;
		List<PersonalityTrait> list = new List<PersonalityTrait>();
		for (int i = 0; i < this.temporaryPersonalityTraits.Count; i++)
		{
			PersonalityTrait personalityTrait = this.temporaryPersonalityTraits[i];
			if (personalityTrait.isTraitTimeFinished)
			{
				if (this.TryEvolveTrait(personalityTrait))
				{
					flag = true;
				}
				list.Add(personalityTrait);
			}
		}
		this.RemovePersonalityTraits(list, true);
		if (flag)
		{
			this.SetupCooldownPeriod();
		}
	}

	private bool TryPermanentTraitsEvolution()
	{
		bool result = false;
		for (int i = 0; i < this.permanentPersonalityTraits.Count; i++)
		{
			PersonalityTrait inPersonalityTrait = this.permanentPersonalityTraits[i];
			if (this.TryEvolveTrait(inPersonalityTrait))
			{
				result = true;
			}
		}
		return result;
	}

	private bool TryEvolveTrait(PersonalityTrait inPersonalityTrait)
	{
		bool result = false;
		if (inPersonalityTrait.data.canEvolve)
		{
			List<PersonalityTraitData> possibleEvolutions = this.GetPossibleEvolutions(inPersonalityTrait, new PersonalityTraitController_v2.DialogCriteriaFilter(this.GetPersonalTraitCriteria));
			if (possibleEvolutions.Count > 0)
			{
				int random = RandomUtility.GetRandom(0, possibleEvolutions.Count);
				float random2 = RandomUtility.GetRandom(0f, 1f);
				if (random2 > possibleEvolutions[random].probability)
				{
					this.AddPersonalityTrait(possibleEvolutions[random], true);
					result = true;
				}
			}
		}
		return result;
	}

	private List<PersonalityTraitData> GetPossibleEvolutions(PersonalityTrait inPersonalityTrait, PersonalityTraitController_v2.DialogCriteriaFilter traitCriteriaFilter)
	{
		List<DialogCriteria> inDriverCriteria = traitCriteriaFilter();
		PersonalityTraitDataManager personalityTraitManager = Game.instance.personalityTraitManager;
		List<PersonalityTraitData> list = new List<PersonalityTraitData>();
		int[] evolvesInto = inPersonalityTrait.data.evolvesInto;
		for (int i = 0; i < evolvesInto.Length; i++)
		{
			PersonalityTraitData personalityTraitData;
			if (!personalityTraitManager.personalityTraits.TryGetValue(evolvesInto[i], out personalityTraitData))
			{
				global::Debug.LogWarningFormat("Trying evolving trait {0} into trait {1} but no trait with this ID has been found. Check the database!", new object[]
				{
					inPersonalityTrait.data.ID,
					inPersonalityTrait.data.evolvesInto[i]
				});
			}
			else if (this.CanAddNewPersonalityTrait(personalityTraitData) && personalityTraitData.MeetsCriteria(inDriverCriteria))
			{
				list.Add(personalityTraitData);
			}
		}
		return list;
	}

	private void TryGetNewPersonalityTrait()
	{
		List<PersonalityTraitData> inMatchingPersonalityTraits = this.FilterPossibleTraits(PersonalityTraitData.TraitType.Temporary, PersonalityTraitData.EventTriggerType.None, new PersonalityTraitController_v2.DialogCriteriaFilter(this.GetPersonalTraitCriteria), true);
		this.ShuffleList<PersonalityTraitData>(ref inMatchingPersonalityTraits);
		this.PickRandomTraitFromList(inMatchingPersonalityTraits);
	}

	private void PickRandomTraitFromList(List<PersonalityTraitData> inMatchingPersonalityTraits)
	{
		for (int i = 0; i < inMatchingPersonalityTraits.Count; i++)
		{
			float random = RandomUtility.GetRandom(0f, 1f);
			if (random >= inMatchingPersonalityTraits[i].probability)
			{
				this.AddPersonalityTrait(inMatchingPersonalityTraits[i], true);
				break;
			}
		}
	}

	private void ShuffleList<T>(ref List<T> inListToShuffle)
	{
		int i = inListToShuffle.Count;
		while (i > 1)
		{
			i--;
			int random = RandomUtility.GetRandom(0, i + 1);
			T t = inListToShuffle[random];
			inListToShuffle[random] = inListToShuffle[i];
			inListToShuffle[i] = t;
		}
	}

	private List<PersonalityTraitData> FilterPossibleTraits(PersonalityTraitData.TraitType inPersonalityTraitType, PersonalityTraitData.EventTriggerType inEventTriggerType, PersonalityTraitController_v2.DialogCriteriaFilter traitCriteriaFilter, bool inAddPermanentTraitsWithProbability = false)
	{
		List<DialogCriteria> inDriverCriteria = traitCriteriaFilter();
		PersonalityTraitDataManager personalityTraitManager = Game.instance.personalityTraitManager;
		List<PersonalityTraitData> list = new List<PersonalityTraitData>();
		foreach (KeyValuePair<int, PersonalityTraitData> keyValuePair in personalityTraitManager.personalityTraits)
		{
			bool flag = keyValuePair.Value.type == inPersonalityTraitType;
			if (inAddPermanentTraitsWithProbability)
			{
				if (keyValuePair.Value.type == PersonalityTraitData.TraitType.Permanent)
				{
					if (keyValuePair.Value.probability > 0f)
					{
						flag = true;
					}
				}
			}
			bool flag2 = keyValuePair.Value.eventTriggerType == inEventTriggerType;
			bool flag3 = this.CanAddNewPersonalityTrait(keyValuePair.Value);
			bool flag4 = !this.IsTraitEvolvingFromCurrentOnes(keyValuePair.Value);
			if (flag && flag2 && flag3 && flag4)
			{
				if (keyValuePair.Value.MeetsCriteria(inDriverCriteria))
				{
					List<PersonalityTraitData> list2 = list;
					list2.Add(keyValuePair.Value);
				}
			}
		}
		return list;
	}

	private bool CanAddNewPersonalityTrait(PersonalityTraitData inNewPersonalityTraitData)
	{
		bool flag = false;
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].data.ID == inNewPersonalityTraitData.ID)
			{
				if (!this.IsID(inNewPersonalityTraitData.ID, new int[]
				{
					275,
					276,
					277,
					300,
					301
				}))
				{
					flag = true;
				}
			}
		}
		return !this.mTraitHistory.Contains(inNewPersonalityTraitData.ID) && !flag && !this.IsTraitOppositeToCurrentOnes(inNewPersonalityTraitData);
	}

	private bool IsID(int inIDToCompare, params int[] inIDs)
	{
		for (int i = 0; i < inIDs.Length; i++)
		{
			if (inIDToCompare == inIDs[i])
			{
				return true;
			}
		}
		return false;
	}

	public void ActivatePersonalityTraitTrigger(PersonalityTrait inPersonalityTrait)
	{
		if (!Game.instance.player.IsUnemployed())
		{
			bool flag = inPersonalityTrait.data.shownType == PersonalityTraitData.TriggerShownType.AllDrivers;
			flag |= (this.mDriver.IsPlayersDriver() && inPersonalityTrait.data.shownType == PersonalityTraitData.TriggerShownType.PlayerDriverOnly);
			if (flag)
			{
				Game.instance.dialogSystem.OnNewPersonalityTrait(this.mDriver, inPersonalityTrait);
			}
		}
	}

	public void EndPersonalityTraitTrigger(PersonalityTrait inPersonalityTrait)
	{
		if (!Game.instance.player.IsUnemployed())
		{
			bool flag = inPersonalityTrait.data.shownType == PersonalityTraitData.TriggerShownType.AllDrivers;
			flag |= (this.mDriver.IsPlayersDriver() && inPersonalityTrait.data.shownType == PersonalityTraitData.TriggerShownType.PlayerDriverOnly);
			if (flag)
			{
				Game.instance.dialogSystem.OnEndPersonalityTrait(this.mDriver, inPersonalityTrait);
			}
		}
	}

	private List<DialogCriteria> GetTraitCriteriaForPostRace()
	{
		List<DialogCriteria> list = new List<DialogCriteria>();
		if (!this.mDriver.IsFreeAgent())
		{
			Championship championship = this.mDriver.contract.GetTeam().championship;
			int eventNumber = championship.eventNumber;
			int num = 0;
			bool flag = true;
			int num2 = 0;
			bool flag2 = true;
			for (int i = eventNumber; i >= 0; i--)
			{
				RaceEventResults.ResultData resultForDriver = championship.calendar[i].results.GetResultsForSession(SessionDetails.SessionType.Race).GetResultForDriver(this.mDriver);
				if (resultForDriver != null && resultForDriver.position == 1 && flag2)
				{
					num++;
				}
				else
				{
					if (!flag)
					{
						break;
					}
					flag2 = false;
					RaceEventResults.ResultData resultForDriver2 = championship.calendar[i].results.GetResultsForSession(SessionDetails.SessionType.Qualifying).GetResultForDriver(this.mDriver);
					RaceEventResults.ResultData resultForDriver3 = championship.calendar[i].results.GetResultsForSession(SessionDetails.SessionType.Practice).GetResultForDriver(this.mDriver);
					if (resultForDriver2 == null && resultForDriver3 == null && resultForDriver == null)
					{
						num2++;
					}
					else
					{
						flag = false;
					}
				}
			}
			list.Add(new DialogCriteria("RacesSinceLastDrive", num2.ToString()));
			list.Add(new DialogCriteria("ConsecutiveWins", num.ToString()));
			if (this.mDriver.IsMainDriver())
			{
				list.Add(new DialogCriteria("ReserveDriver", "False"));
			}
			else
			{
				list.Add(new DialogCriteria("ReserveDriver", "True"));
			}
			RaceEventResults.SessonResultData resultsForSession = championship.GetCurrentEventDetails().results.GetResultsForSession(SessionDetails.SessionType.Race);
			RaceEventResults.ResultData resultForDriver4 = resultsForSession.GetResultForDriver(this.mDriver);
			if (resultForDriver4 != null)
			{
				RaceEventResults.ResultData.CarState carState = resultForDriver4.carState;
				if (carState != RaceEventResults.ResultData.CarState.None && resultForDriver4.accidentDriver == this.mDriver)
				{
					list.Add(new DialogCriteria("CarStatus", carState.ToString()));
				}
				else
				{
					list.Add(new DialogCriteria("RacePosition", (RaceEventResults.GetPositionForChampionshipClass(resultForDriver4.driver, resultsForSession.resultData, resultForDriver4.team.championship) + 1).ToString()));
				}
				for (int j = 0; j < resultForDriver4.penalties.Count; j++)
				{
					if (resultForDriver4.penalties[j].penaltyType == Penalty.PenaltyType.PartPenalty)
					{
						list.Add(new DialogCriteria("Busted", "True"));
					}
				}
			}
			Driver rivalDriver = this.mDriver.GetRivalDriver();
			if (rivalDriver != null)
			{
				if (!rivalDriver.IsFreeAgent())
				{
					if (!rivalDriver.IsPlayersDriver())
					{
						list.Add(new DialogCriteria("HasRival", "OtherTeam"));
					}
					else
					{
						list.Add(new DialogCriteria("HasRival", "Teammate"));
					}
				}
				else
				{
					list.Add(new DialogCriteria("HasRival", "OtherTeam"));
				}
			}
			string inInfo = "Any";
			if (championship.GetCurrentEventDetails().circuit.nationality == this.mDriver.nationality)
			{
				inInfo = "Home";
			}
			list.Add(new DialogCriteria("Location", inInfo));
			if (championship.GetFinalEventDetails().isEventEnding || championship.GetFinalEventDetails().hasEventEnded)
			{
				ChampionshipEntry_v1 entry = championship.standings.GetEntry(this.mDriver);
				if (entry != null && entry.GetCurrentChampionshipPosition() == 1)
				{
					list.Add(new DialogCriteria("WonChampionship", "True"));
					switch (championship.series)
					{
					case Championship.Series.SingleSeaterSeries:
						list.Add(new DialogCriteria("Tier", (championship.championshipOrder + 1).ToString()));
						break;
					case Championship.Series.GTSeries:
						list.Add(new DialogCriteria("TierGT", (championship.championshipOrder + 1).ToString()));
						break;
					case Championship.Series.EnduranceSeries:
						list.Add(new DialogCriteria("TierEndurance", (championship.championshipOrder + 1).ToString()));
						break;
					}
				}
			}
			list.Add(new DialogCriteria("Employed", "True"));
		}
		else
		{
			list.Add(new DialogCriteria("Employed", "False"));
		}
		return list;
	}

	private List<DialogCriteria> GetPersonalTraitCriteria()
	{
		List<DialogCriteria> list = new List<DialogCriteria>();
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			list.Add(new DialogCriteria("Trait", this.allTraits[i].data.ID.ToString()));
		}
		if (this.mDriver.gender == Person.Gender.Male)
		{
			list.Add(new DialogCriteria("Gender", "Male"));
		}
		if (this.mDriver.IsAtPeak())
		{
			list.Add(new DialogCriteria("AtPeak", "True"));
		}
		list.Add(new DialogCriteria("Age", this.mDriver.GetAge().ToString()));
		list.Add(new DialogCriteria("Potential", this.mDriver.GetStats().GetPotential().ToString()));
		list.Add(new DialogCriteria("Morale", Mathf.RoundToInt(this.mDriver.GetMorale() * 100f).ToString()));
		if (this.mDriver.nationality != null)
		{
			list.Add(new DialogCriteria("Nationality", this.mDriver.nationality.flagSpritePathName));
		}
		float random = RandomUtility.GetRandom01();
		if (random > 0.5f)
		{
			list.Add(new DialogCriteria("TeammateLovePossible", "True"));
		}
		random = RandomUtility.GetRandom01();
		if (random > 0.5f)
		{
			list.Add(new DialogCriteria("RumoursAboutDriver", "True"));
		}
		if (!this.mDriver.IsFreeAgent())
		{
			if (this.mDriver.contract.GetTeam().chairman.hasMadeUltimatum)
			{
				list.Add(new DialogCriteria("ChairmanUltimatum", "True"));
			}
			if (this.mDriver.IsMainDriver())
			{
				list.Add(new DialogCriteria("ReserveDriver", "False"));
			}
			else
			{
				list.Add(new DialogCriteria("ReserveDriver", "True"));
			}
			list.Add(new DialogCriteria("Employed", "True"));
		}
		else
		{
			list.Add(new DialogCriteria("Employed", "False"));
		}
		for (int j = 0; j < this.mDriver.preferedSeries.Count; j++)
		{
			list.Add(new DialogCriteria("CategoryPreference", this.mDriver.preferedSeries[j].ToString().Replace("Series", string.Empty)));
		}
		bool[] array = new bool[3];
		for (int k = 0; k < this.mDriver.careerHistory.careerCount; k++)
		{
			CareerHistoryEntry careerHistoryEntry = this.mDriver.careerHistory.career[k];
			if (careerHistoryEntry != null && careerHistoryEntry.championship != null)
			{
				int series = (int)careerHistoryEntry.championship.series;
				if (!array[series] && careerHistoryEntry.wins > 0)
				{
					array[series] = true;
					list.Add(new DialogCriteria("WonTopSeriesRace", careerHistoryEntry.championship.series.ToString().Replace("Series", string.Empty)));
				}
			}
		}
		return list;
	}

	public List<PersonalityTrait> GetAllPersonalityTraits()
	{
		return this.allTraits;
	}

	public void AssignRandomPersonalityTraits()
	{
		int randomInc = RandomUtility.GetRandomInc(0, 2);
		this.AssignRandomPersonalityTraits(randomInc, false);
	}

	public void AssignRandomPersonalityTraits(int inNumber, bool inActivatePersonalityTraitTrigger)
	{
		for (int i = 0; i < inNumber; i++)
		{
			List<PersonalityTraitData> list = this.FilterPossibleTraits(PersonalityTraitData.TraitType.Temporary, PersonalityTraitData.EventTriggerType.None, new PersonalityTraitController_v2.DialogCriteriaFilter(this.GetPersonalTraitCriteria), false);
			list.AddRange(this.FilterPossibleTraits(PersonalityTraitData.TraitType.Permanent, PersonalityTraitData.EventTriggerType.None, new PersonalityTraitController_v2.DialogCriteriaFilter(this.GetPersonalTraitCriteria), false));
			if (list.Count > 0)
			{
				int random = RandomUtility.GetRandom(0, list.Count);
				this.AddPersonalityTrait(list[random], inActivatePersonalityTraitTrigger);
			}
		}
	}

	public void CheckIfCarPartPromiseIsFulfilled()
	{
		for (int i = this.temporaryPersonalityTraits.Count - 1; i >= 0; i--)
		{
			if (this.temporaryPersonalityTraits[i].HasSpecialCase(PersonalityTrait.SpecialCaseType.CarPartPromise))
			{
				this.temporaryPersonalityTraits[i].CheckFulfilledCarPartPromise();
				this.RemovePersonalityTrait(this.temporaryPersonalityTraits[i]);
			}
		}
	}

	public void CheckIfFulfilledAnyPromise()
	{
		for (int i = this.temporaryPersonalityTraits.Count - 1; i >= 0; i--)
		{
			if (this.temporaryPersonalityTraits[i].HasFulfilledPromise())
			{
				this.RemovePersonalityTrait(this.temporaryPersonalityTraits[i]);
			}
		}
	}

	public DriverStats GetDriverStatsModifier()
	{
		this.mDriverStats.Clear();
		int count = this.allTraits.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.allTraits[i].CanApplyTrait())
			{
				DriverStats driverStatsModifier = this.allTraits[i].GetDriverStatsModifier();
				this.mDriverStats.Add(driverStatsModifier);
				this.mDriverStats.marketability += driverStatsModifier.marketability;
			}
		}
		return this.mDriverStats;
	}

	public void GetDriverPermanentStatsModifier(ref DriverStats driver_stats)
	{
		int count = this.permanentPersonalityTraits.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.permanentPersonalityTraits[i].CanApplyTrait())
			{
				DriverStats driverStatsModifier = this.permanentPersonalityTraits[i].GetDriverStatsModifier();
				driver_stats.Add(driverStatsModifier);
			}
		}
	}

	public List<PersonalityTrait> GetModifierTraitsForStat(PersonalityTrait.StatModified inStatModified)
	{
		this.mModifingStatTraitsCache.Clear();
		int count = this.allTraits.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.allTraits[i].CanApplyTrait() && this.allTraits[i].DoesModifyStat(inStatModified))
			{
				this.mModifingStatTraitsCache.Add(this.allTraits[i]);
			}
		}
		if (inStatModified == PersonalityTrait.StatModified.Morale)
		{
			this.mModifingStatTraitsCache.AddRange(this.GetModifierTraitsFromOtherTeammates(PersonalityTrait.StatModified.TeammateMorale));
		}
		return this.mModifingStatTraitsCache;
	}

	public float GetSingleModifierForStat(PersonalityTrait.StatModified inStatModified)
	{
		List<PersonalityTrait> modifierTraitsForStat = this.GetModifierTraitsForStat(inStatModified);
		float num = 0f;
		for (int i = 0; i < modifierTraitsForStat.Count; i++)
		{
			num += modifierTraitsForStat[i].GetSingleModifierForStat(inStatModified);
		}
		if (inStatModified == PersonalityTrait.StatModified.Morale)
		{
			num += this.GetSingleModifierFromOtherTeammates(PersonalityTrait.StatModified.TeammateMorale);
		}
		return num;
	}

	private float GetSingleModifierFromOtherTeammates(PersonalityTrait.StatModified inStatModified)
	{
		float num = 0f;
		if (!this.mDriver.IsFreeAgent() && !(this.mDriver.contract.GetTeam() is NullTeam))
		{
			List<Driver> allPeopleOnJob = this.mDriver.contract.GetTeam().contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
			for (int i = 0; i < allPeopleOnJob.Count; i++)
			{
				if (allPeopleOnJob[i] != this.mDriver)
				{
					num += allPeopleOnJob[i].personalityTraitController.GetSingleModifierForOtherPerson(inStatModified, this.mDriver);
				}
			}
		}
		return num;
	}

	private List<PersonalityTrait> GetModifierTraitsFromOtherTeammates(PersonalityTrait.StatModified inStatModified)
	{
		List<PersonalityTrait> list = new List<PersonalityTrait>();
		if (!this.mDriver.IsFreeAgent() && !(this.mDriver.contract.GetTeam() is NullTeam))
		{
			List<Driver> allPeopleOnJob = this.mDriver.contract.GetTeam().contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
			for (int i = 0; i < allPeopleOnJob.Count; i++)
			{
				Driver driver = allPeopleOnJob[i];
				if (driver != null && driver != this.mDriver)
				{
					list.AddRange(driver.personalityTraitController.GetModifierTraitsForOtherPerson(inStatModified, this.mDriver));
				}
			}
		}
		return list;
	}

	private List<PersonalityTrait> GetModifierTraitsForOtherPerson(PersonalityTrait.StatModified inStatModified, Person inPerson)
	{
		List<PersonalityTrait> list = new List<PersonalityTrait>();
		int count = this.allTraits.Count;
		for (int i = 0; i < count; i++)
		{
			if (this.allTraits[i].CanApplyToOtherPerson(inPerson) && this.allTraits[i].DoesModifyStat(inStatModified))
			{
				list.Add(this.allTraits[i]);
			}
		}
		return list;
	}

	private float GetSingleModifierForOtherPerson(PersonalityTrait.StatModified inStatModified, Person inPerson)
	{
		List<PersonalityTrait> modifierTraitsForOtherPerson = this.GetModifierTraitsForOtherPerson(inStatModified, inPerson);
		float num = 0f;
		for (int i = 0; i < modifierTraitsForOtherPerson.Count; i++)
		{
			num += modifierTraitsForOtherPerson[i].GetSingleModifierForStat(inStatModified);
		}
		return num;
	}

	public string GetSingleModifierForStatText(PersonalityTrait.StatModified inStatModified)
	{
		float singleModifierForStat = this.GetSingleModifierForStat(inStatModified);
		return PersonalityTraitController_v2.GetSingleModifierStatText(singleModifierForStat, inStatModified);
	}

	public static string GetSingleModifierStatText(float inModifier, PersonalityTrait.StatModified inStatModified)
	{
		StringBuilder builder = GameUtility.GlobalStringBuilderPool.GetBuilder();
		if (inModifier > 0f)
		{
			builder.Append("+");
		}
		switch (inStatModified)
		{
		case PersonalityTrait.StatModified.Marketability:
		case PersonalityTrait.StatModified.Morale:
		case PersonalityTrait.StatModified.TeammateMorale:
		case PersonalityTrait.StatModified.Improveability:
			inModifier *= 100f;
			builder.Append(Mathf.Round(inModifier).ToString(Localisation.numberFormatter));
			goto IL_B2;
		case PersonalityTrait.StatModified.DesiredEarnings:
			builder.Append(GameUtility.GetCurrencyString((long)inModifier, 0));
			goto IL_B2;
		}
		builder.Append(Mathf.Round(inModifier).ToString(Localisation.numberFormatter));
		IL_B2:
		string result = builder.ToString();
		GameUtility.GlobalStringBuilderPool.ReturnBuilder(builder);
		return result;
	}

	public bool IsModifingStat(PersonalityTrait.StatModified inStatModified)
	{
		return this.GetSingleModifierForStat(inStatModified) != 0f;
	}

	public bool hasSpecialCases
	{
		get
		{
			for (int i = 0; i < this.allTraits.Count; i++)
			{
				if (this.allTraits[i].hasSpecialCase)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool HasSpecialCase(PersonalityTrait.SpecialCaseType inSpecialCaseType)
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasSpecialCase(inSpecialCaseType))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasFightWithTeammate(Driver inTeammate)
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasFightWithTeammate(inTeammate))
			{
				return true;
			}
		}
		return false;
	}

	public void RemovePersonalityTraitsWithSpecialCase(PersonalityTrait.SpecialCaseType inSpecialCaseType, bool triggerOnEndTrait)
	{
		for (int i = this.temporaryPersonalityTraits.Count - 1; i >= 0; i--)
		{
			if (this.temporaryPersonalityTraits[i].HasSpecialCase(inSpecialCaseType))
			{
				if (triggerOnEndTrait)
				{
					this.temporaryPersonalityTraits[i].OnTraitEnd();
				}
				this.allTraits.Remove(this.temporaryPersonalityTraits[i]);
				this.temporaryPersonalityTraits.RemoveAt(i);
			}
		}
		for (int j = this.permanentPersonalityTraits.Count - 1; j >= 0; j--)
		{
			if (this.permanentPersonalityTraits[j].HasSpecialCase(inSpecialCaseType))
			{
				this.allTraits.Remove(this.permanentPersonalityTraits[j]);
				this.permanentPersonalityTraits.RemoveAt(j);
			}
		}
	}

	public void RemovePersonalityTraitsRelatedToTeam()
	{
		if (this.HasSpecialCase(PersonalityTrait.SpecialCaseType.WIllNotRenewContract))
		{
			DateTime traitEndTime = default(DateTime);
			for (int i = 0; i < this.allTraits.Count; i++)
			{
				if (this.allTraits[i].HasSpecialCase(PersonalityTrait.SpecialCaseType.WIllNotRenewContract))
				{
					traitEndTime = this.allTraits[i].traitEndTime;
				}
			}
			this.RemovePersonalityTraitsWithSpecialCase(PersonalityTrait.SpecialCaseType.WIllNotRenewContract, false);
			PersonalityTraitData inPersonalityTraitData;
			if (!Game.instance.personalityTraitManager.personalityTraits.TryGetValue(168, out inPersonalityTraitData))
			{
				return;
			}
			PersonalityTrait personalityTrait = this.AddPersonalityTrait(inPersonalityTraitData, false);
			personalityTrait.traitEndTime = traitEndTime;
		}
		this.RemovePersonalityTraitsWithSpecialCase(PersonalityTrait.SpecialCaseType.FightWithTeammate, true);
	}

	public void RemovePersonalityTraitFightWithDriver(Driver inFightWithDriver)
	{
		if (this.HasFightWithTeammate(inFightWithDriver))
		{
			for (int i = this.temporaryPersonalityTraits.Count - 1; i >= 0; i--)
			{
				if (this.temporaryPersonalityTraits[i].HasFightWithTeammate(inFightWithDriver))
				{
					this.allTraits.Remove(this.temporaryPersonalityTraits[i]);
					this.temporaryPersonalityTraits.RemoveAt(i);
				}
			}
		}
	}

	public List<Transaction> GetPersonalityTraitTransactions(PersonalityTrait.SpecialCaseType inSpecialCaseType, bool isDataForUI)
	{
		List<Transaction> list = new List<Transaction>();
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasSpecialCase(inSpecialCaseType))
			{
				list.AddRange(this.allTraits[i].GetPersonalityTraitTransactions(inSpecialCaseType, isDataForUI));
			}
		}
		return list;
	}

	public bool HasRivalInTeam(Team inTeam)
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasRivalInTeam(inTeam))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDriverRival(Driver inDriver)
	{
		return this.mDriver.GetRivalDriver() == inDriver;
	}

	public int GetTieredPayDriverAmount()
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasSpecialCase(PersonalityTrait.SpecialCaseType.PayDriver))
			{
				return this.allTraits[i].tieredPayDriverAmount;
			}
		}
		return 0;
	}

	public int GetTieredPayDriverAmountForPlayer()
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasSpecialCase(PersonalityTrait.SpecialCaseType.PayDriver))
			{
				return this.allTraits[i].tieredPayDriverAmountForPlayer;
			}
		}
		return 0;
	}

	public bool HasTrait(bool mustHaveAllTraits, params int[] inID)
	{
		for (int i = 0; i < inID.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < this.allTraits.Count; j++)
			{
				if (inID[i] == this.allTraits[j].data.ID)
				{
					if (!mustHaveAllTraits)
					{
						return true;
					}
					flag = true;
				}
			}
			if (mustHaveAllTraits && !flag)
			{
				return false;
			}
		}
		return false;
	}

	public bool IsPayDriver()
	{
		for (int i = 0; i < this.allTraits.Count; i++)
		{
			if (this.allTraits[i].HasSpecialCase(PersonalityTrait.SpecialCaseType.PayDriver))
			{
				return true;
			}
		}
		return false;
	}

	public bool UsesAIForStrategy(RacingVehicle inVehicle)
	{
		return this.HasSpecialCase(PersonalityTrait.SpecialCaseType.TurnOffStrategy) || (inVehicle != null && Game.instance.sessionManager.sessionType == SessionDetails.SessionType.Race && inVehicle.championship.series == Championship.Series.EnduranceSeries && inVehicle.driver.driverStamina.IsInOptimalZone() && this.HasSpecialCase(PersonalityTrait.SpecialCaseType.RogueInZone));
	}

	public List<PersonalityTrait> permanentPersonalityTraits = new List<PersonalityTrait>();

	public List<PersonalityTrait> temporaryPersonalityTraits = new List<PersonalityTrait>();

	public List<PersonalityTrait> allTraits = new List<PersonalityTrait>();

	public List<PersonalityTrait> raceTraitsHistory;

	private List<int> mTraitHistory = new List<int>();

	private readonly int mMaxCooldownDaysRange = 180;

	private readonly int mMaxCooldownDaysRangeEndurance = 240;

	private DateTime cooldownPeriodEnd = default(DateTime);

	private int mLastRandomCooldownDayValue;

	private Driver mDriver;

	private DriverStats mDriverStats = new DriverStats();

	private List<PersonalityTrait> mModifingStatTraitsCache = new List<PersonalityTrait>();

	private delegate List<DialogCriteria> DialogCriteriaFilter();
}
