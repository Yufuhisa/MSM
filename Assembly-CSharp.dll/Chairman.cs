using System;
using System.Collections.Generic;
using System.Text;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Chairman : Person
{
	public Chairman()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static Chairman()
	{
	}

	private void AddHappiness(string inHappinessHistoryEntryName, float inHappiness)
	{
		this.mHappiness = Mathf.Clamp(this.mHappiness + inHappiness, 0f, (float)this.GetMaxHappiness());
		this.mHappinessModificationHistory.AddStatModificationEntry(inHappinessHistoryEntryName, inHappiness, false);
	}

	public void AddRaceHappiness(int inTeamPositionEvent)
	{
		Team team = this.contract.GetTeam();
		float num = this.mHappiness;
		if (team != null && Game.instance.isCareer)
		{
			float num2 = Chairman.happinessPositiveMultiplier[team.pressure - 1];
			float num3 = Chairman.happinessNegativeMultiplier[team.pressure - 1];
			int num4 = this.expectedTeamChampionshipResult - inTeamPositionEvent;
			float num5 = Chairman.happinessChampionshipPositionNormalMultiplier;
			bool flag = team.financeController.GetTotalAvailableFunds() < 0L;
			num = Mathf.Clamp(num + ((!flag) ? Chairman.happinessFinancesFixedChange : (-Chairman.happinessFinancesFixedChange)), 0f, (float)this.GetMaxHappiness());
			if (team.championship.eventNumber > 1)
			{
				int num6 = this.expectedTeamChampionshipResult - team.GetChampionshipEntry().GetCurrentChampionshipPosition();
				num5 = ((num4 >= 0 || num6 < 0) ? Chairman.happinessChampionshipPositionNormalMultiplier : Chairman.happinessKeptExpectedChampionshipPositionMultiplier);
			}
			if (team.IsPlayersTeam())
			{
				Game.instance.player.AddRaceManagement(num4);
			}
			if (num4 >= 0)
			{
				num += Mathf.Min(Chairman.happinessFixedChange + Chairman.happinessMultiplier * (float)Mathf.Abs(num4) * num2 * num5, Chairman.maxRaceHappinessChangePerEvent);
			}
			else
			{
				num -= Mathf.Min(Chairman.happinessFixedChange + Chairman.happinessMultiplier * (float)Mathf.Abs(num4) * num3 * num5, Chairman.maxRaceHappinessChangePerEvent);
			}
			if (inTeamPositionEvent <= 3)
			{
				num = Mathf.Clamp(num + Chairman.happinessPodiumBonus, 0f, (float)this.GetMaxHappiness());
			}
			bool flag2 = team.championship.eventNumber >= team.championship.eventCount - 1;
			bool flag3 = !team.teamPrincipal.IsReplacementPerson() && team.teamPrincipal.hasJoinedTeamRecently;
			num = Mathf.Clamp(num, (!flag2 && !flag3) ? 0f : 1f, (float)this.GetMaxHappiness());
		}
		this.AddHappiness(Game.instance.sessionManager.eventDetails.circuit.locationNameID, num - this.mHappiness);
	}

	public void ResetHappiness()
	{
		this.mHappiness = Chairman.happinessResetValue;
	}

	public int GetRelationshipWithPlayer()
	{
		return UnityEngine.Random.Range(0, 101);
	}

	public int GetEstimatedPosition(Chairman.EstimatedPosition inPosition, Team inTeam)
	{
		int startOfSeasonExpectedChampionshipResult = inTeam.startOfSeasonExpectedChampionshipResult;
		int teamEntryCount = inTeam.championship.standings.teamEntryCount;
		switch (inPosition)
		{
		case Chairman.EstimatedPosition.Low:
			return Mathf.Clamp(startOfSeasonExpectedChampionshipResult + 2, 1, teamEntryCount);
		case Chairman.EstimatedPosition.Medium:
			return Mathf.Clamp(startOfSeasonExpectedChampionshipResult, 1, teamEntryCount);
		case Chairman.EstimatedPosition.High:
			return Mathf.Clamp(startOfSeasonExpectedChampionshipResult - 2, 1, teamEntryCount);
		default:
			return 0;
		}
	}

	public Chairman.RequestFundsAnswer CanRequestFunds()
	{
		Team team = this.contract.GetTeam();
		if (!(team is NullTeam))
		{
			bool flag = team.championship.InPreseason();
			bool flag2 = team.championship.eventNumber == 0;
			bool flag3 = this.happinessNormalized < Chairman.happinessMinimumRequestFundsValueNormalized;
			if (flag)
			{
				return Chairman.RequestFundsAnswer.DeclinedPreSeason;
			}
			if (flag2)
			{
				return Chairman.RequestFundsAnswer.DeclinedSeasonStart;
			}
			if (flag3)
			{
				return Chairman.RequestFundsAnswer.DeclinedLowHappiness;
			}
			if (team.canRequestFunds)
			{
				return Chairman.RequestFundsAnswer.Accepted;
			}
		}
		return Chairman.RequestFundsAnswer.Declined;
	}

	public void RequestFunds()
	{
		Team team = this.contract.GetTeam();
		if (team is NullTeam || this.CanRequestFunds() != Chairman.RequestFundsAnswer.Accepted)
		{
			return;
		}
		Action inOnTransactionSucess = delegate()
		{
			this.AddHappiness("PSG_10011850", Chairman.happinessRequestFundsValue);
			team.canRequestFunds = false;
			if (UIManager.instance.IsScreenOpen("FinanceScreen"))
			{
				UIManager.instance.RefreshCurrentPage();
			}
		};
		Transaction requestFundsTransaction = this.GetRequestFundsTransaction();
		team.financeController.finance.ProcessTransactions(inOnTransactionSucess, null, true, new Transaction[]
		{
			requestFundsTransaction
		});
	}

	public Transaction GetRequestFundsTransaction()
	{
		long requestFundsAmmount = this.GetRequestFundsAmmount();
		return new Transaction(Transaction.Group.ChairmanPayments, Transaction.Type.Credit, requestFundsAmmount, Localisation.LocaliseID("PSG_10011851", null));
	}

	public long GetRequestFundsAmmount()
	{
		Team team = this.contract.GetTeam();
		if (team is NullTeam || this.CanRequestFunds() != Chairman.RequestFundsAnswer.Accepted)
		{
			return 0L;
		}
		return team.financeController.racePayment;
	}

	public bool CanMakeUltimatum()
	{
		Team team = this.contract.GetTeam();
		return Mathf.Approximately(this.happinessNormalized, 0f) && team != null && !this.hasMadeUltimatum;
	}

	public void CheckUltimatum()
	{
		if (Game.instance.isCareer && this.CanMakeUltimatum())
		{
			Team team = this.contract.GetTeam();
			this.GenerateUltimatum(team);
		}
	}

	public void GenerateUltimatum(Team inTeam)
	{
		if (Game.instance.isCareer && inTeam != null)
		{
			this.ultimatum.onGoing = true;
			this.ultimatum.complete = false;
			this.ultimatum.positionAccomplished = 0;
			this.ultimatum.positionExpected = this.GenerateUltimatumExpectedPosition();
			if (inTeam.IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnUltimatum(this);
				DilemmaDialogBox.ShowUltimatum(DilemmaDialogBox.UltimatumType.Warning);
			}
			else
			{
				Game.instance.dialogSystem.OnAIUltimatum(this);
			}
			this.ultimatumsGeneratedThisSeason++;
		}
	}

	public void CompleteUltimatum()
	{
		if (this.hasMadeUltimatum)
		{
			if (this.ultimatum.positionAccomplished <= this.ultimatum.positionExpected)
			{
				this.AddHappiness("PSG_10004820", Chairman.happinessUltimatumBoostSameManager);
				if (this.contract.GetTeam().IsPlayersTeam())
				{
					Game.instance.dialogSystem.OnUltimatumPlayerSafe(this);
					App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Avoid_Chairman_Ultimatum);
					DilemmaDialogBox.ShowUltimatum(DilemmaDialogBox.UltimatumType.Safe);
				}
			}
			else if (this.contract.GetTeam().IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnUltimatumPlayerFired(this);
				App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Get_Fired);
				DilemmaDialogBox.ShowUltimatum(DilemmaDialogBox.UltimatumType.Fired);
				Game.instance.MakePlayerUnemployed();
				this.ResetHappiness();
			}
			else
			{
				Game.instance.dialogSystem.OnUltimatumAIFired(this);
				Team team = this.contract.GetTeam();
				TeamPrincipal teamPrincipal = team.teamPrincipal;
				team.contractManager.FirePerson(teamPrincipal, Contract.ContractTerminationType.Generic);
				team.contractManager.HireReplacementTeamPrincipal();
				this.AddHappiness("PSG_10004820", Chairman.happinessUltimatumBoostNewManager);
			}
			this.ultimatum.onGoing = false;
			this.ultimatum.complete = true;
		}
	}

	public void FirePlayer()
	{
		Game.instance.MakePlayerUnemployed();
		this.ResetHappiness();
		if (this.hasMadeUltimatum)
		{
			this.ultimatum.onGoing = false;
			this.ultimatum.complete = true;
		}
		if (!Game.instance.challengeManager.IsAttemptingChallengeAndFailed())
		{
			UIManager.instance.ChangeScreen("PlayerScreen", UIManager.ScreenTransition.Fade, 1f, null, UIManager.NavigationType.Normal);
			UIManager.instance.ClearNavigationStacks();
		}
	}

	public long GetFinanceLowerBound()
	{
		return GameStatsConstants.fundsLowerBound;
	}

	public void OnSeasonEnd()
	{
		this.playerChosenExpectedTeamChampionshipPosition = 0;
		this.ultimatumsGeneratedThisSeason = 0;
	}

	public void SetHappiness(int inHappiness)
	{
		this.mHappiness = (float)inHappiness;
	}

	public override void ModifyHappiness(float inHappiness, string inHappinessModifierName)
	{
		this.mHappiness = (float)Mathf.RoundToInt(this.mHappiness + inHappiness);
		if (!string.IsNullOrEmpty(inHappinessModifierName))
		{
			this.mHappinessModificationHistory.AddStatModificationEntry(inHappinessModifierName, inHappiness, true);
		}
	}

	public void ClearHappinessHistory()
	{
		this.mHappinessModificationHistory.ClearHistory();
	}

	public float GetHappinessModifier()
	{
		float num = 0f;
		if (!this.IsFreeAgent())
		{
			List<Driver> allPeopleOnJob = this.contract.GetTeam().contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
			for (int i = 0; i < allPeopleOnJob.Count; i++)
			{
				num += allPeopleOnJob[i].personalityTraitController.GetSingleModifierForStat(PersonalityTrait.StatModified.ChairmanHappiness);
			}
		}
		return num;
	}

	public List<PersonalityTrait> GetPersonalityTraitHappinessModifiers()
	{
		List<PersonalityTrait> list = new List<PersonalityTrait>();
		if (!this.IsFreeAgent())
		{
			List<Driver> allPeopleOnJob = this.contract.GetTeam().contractManager.GetAllPeopleOnJob<Driver>(Contract.Job.Driver);
			for (int i = 0; i < allPeopleOnJob.Count; i++)
			{
				if (allPeopleOnJob[i].personalityTraitController.IsModifingStat(PersonalityTrait.StatModified.ChairmanHappiness))
				{
					list.AddRange(allPeopleOnJob[i].personalityTraitController.GetModifierTraitsForStat(PersonalityTrait.StatModified.ChairmanHappiness));
				}
			}
		}
		return list;
	}

	public int GetMaxHappiness()
	{
		return 100;
	}

	public override int GetHappiness()
	{
		return Mathf.RoundToInt(Mathf.Clamp(this.mHappiness + this.GetHappinessModifier(), 0f, 100f));
	}

	public string GetHappinessModifierText()
	{
		float happinessModifier = this.GetHappinessModifier();
		StringBuilder builder = GameUtility.GlobalStringBuilderPool.GetBuilder();
		if (happinessModifier > 0f)
		{
			builder.Append("+");
		}
		builder.Append(Mathf.RoundToInt(happinessModifier).ToString());
		string result = builder.ToString();
		GameUtility.GlobalStringBuilderPool.ReturnBuilder(builder);
		return result;
	}

	private int GenerateUltimatumExpectedPosition()
	{
		Team team = this.contract.GetTeam();
		int teamEntryCount = team.championship.standings.teamEntryCount;
		int num = teamEntryCount * CarManager.carCount;
		float t = Mathf.Clamp01((float)this.playerChosenExpectedTeamChampionshipPosition / (float)teamEntryCount);
		int num2 = Mathf.RoundToInt(Mathf.Lerp(2f, (float)num * Chairman.ultimatumMaxPositionPercentage, t)) + (3 - team.pressure);
		return Mathf.Clamp(num2 - this.ultimatumsGeneratedThisSeason, 1, num - 2);
	}

	public override int GetPersonIndexInManager()
	{
		ChairmanManager chairmanManager = Game.instance.chairmanManager;
		return chairmanManager.GetPersonIndex(this);
	}

	public int expectedTeamChampionshipResult
	{
		get
		{
			Team team = this.contract.GetTeam();
			return (!(team is NullTeam)) ? ((!team.IsPlayersTeam() || !this.hasSelectedExpectedPosition) ? team.startOfSeasonExpectedChampionshipResult : this.playerChosenExpectedTeamChampionshipPosition) : 0;
		}
	}

	public float happinessNormalized
	{
		get
		{
			return Mathf.Clamp01((float)this.GetHappiness() / 100f);
		}
	}

	public bool hasMadeUltimatum
	{
		get
		{
			return this.ultimatum.onGoing;
		}
	}

	public bool hasSelectedExpectedPosition
	{
		get
		{
			return this.playerChosenExpectedTeamChampionshipPosition != 0;
		}
	}

	public StatModificationHistory happinessModificationHistory
	{
		get
		{
			return this.mHappinessModificationHistory;
		}
	}

	private static readonly float happinessPodiumBonus = 5f;

	private static readonly float happinessFixedChange = 5f;

	private static readonly float happinessMultiplier = 10f;

	private static readonly float happinessChampionshipPositionNormalMultiplier = 1f;

	private static readonly float happinessKeptExpectedChampionshipPositionMultiplier = 0.1f;

	private static readonly float maxRaceHappinessChangePerEvent = 100f;

	private static readonly float happinessUltimatumBoostSameManager = 35f;

	private static readonly float happinessUltimatumBoostNewManager = 50f;

	private static readonly float happinessFinancesFixedChange = 1f;

	private static readonly float happinessResetValue = 60f;

	private static readonly float happinessMinimumRequestFundsValueNormalized = 0.55f;

	private static readonly float happinessRequestFundsValue = -50f;

	private static readonly float[] happinessNegativeMultiplier = new float[]
	{
		0.4f,
		0.5f,
		0.6f
	};

	private static readonly float[] happinessPositiveMultiplier = new float[]
	{
		0.6f,
		0.5f,
		0.4f
	};

	private static readonly float ultimatumMaxPositionPercentage = 0.7f;

	public ChairmanUltimatum ultimatum = new ChairmanUltimatum();

	public int ultimatumsGeneratedThisSeason;

	public int costFocus;

	public int patience;

	public int patienceStrikesCount;

	public int playerChosenExpectedTeamChampionshipPosition;

	private float mHappiness;

	public int happinessBeforeEvent;

	private StatModificationHistory mHappinessModificationHistory = new StatModificationHistory();

	public enum EstimatedPosition
	{
		Low,
		Medium,
		High
	}

	public enum RequestFundsAnswer
	{
		Accepted,
		DeclinedLowHappiness,
		DeclinedPreSeason,
		DeclinedSeasonStart,
		Declined
	}
}
