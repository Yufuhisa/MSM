using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class Championship : Entity
{
	public override void OnStart()
	{
		this.standings.OnStart(this);
		this.standingsHistory.OnStart(this);
		this.rules.championship = this;
		this.nextYearsRules.championship = this;
		this.preSeasonTesting = Game.instance.entityManager.CreateEntity<PreSeasonTesting>();
		this.preSeasonTesting.championship = this;
		this.preSeasonTesting.Reset();
	}

	public void PoliticsOnStart()
	{
		this.politicalSystem.OnStart(this);
	}

	public void AddTeam(Team inTeam)
	{
		this.standings.AddEntry(inTeam, this);
	}

	public override void OnSave()
	{
		this.records.OnSave();
	}

	public override void OnLoad()
	{
		this.rules.OnLoad();
		this.nextYearsRules.OnLoad();
		this.records.OnLoad();
		if (!this.rules.qualifyingBasedActive && this.calendar[0].qualifyingSessions.Count > 0 && this.calendar[0].qualifyingSessions.Count == 3)
		{
			for (int i = 0; i < this.calendar.Count; i++)
			{
				if (this.calendar[i].qualifyingSessions.Count == 3)
				{
					this.calendar[i].qualifyingSessions.RemoveAt(2);
					this.calendar[i].qualifyingSessions.RemoveAt(1);
					this.calendar[i].sessions.RemoveAt(3);
					this.calendar[i].sessions.RemoveAt(2);
				}
				if (this.nextYearsCalendar[i].qualifyingSessions.Count == 3)
				{
					this.nextYearsCalendar[i].qualifyingSessions.RemoveAt(2);
					this.nextYearsCalendar[i].qualifyingSessions.RemoveAt(1);
					this.nextYearsCalendar[i].sessions.RemoveAt(3);
					this.nextYearsCalendar[i].sessions.RemoveAt(2);
				}
			}
		}
		if (this.standingsHistory == null)
		{
			this.standingsHistory = new ChampionshipStandingsHistory();
		}
		this.standings.OnStart(this);
		this.standingsHistory.OnStart(this);
		if (this.mChampionshipPromotions == null)
		{
			this.mChampionshipPromotions = new ChampionshipPromotions();
		}
		if (this.seasonDirector == null)
		{
			this.seasonDirector = new SeasonDirector();
		}
		if (!this.seasonDirector.IsSetupForSeason())
		{
			this.seasonDirector.OnSeasonStart(this);
		}
		this.seasonDirector.OnLoad(this);
		this.GetChampionshipName(false, string.Empty);
		this.GetAcronym(false, string.Empty);
	}

	private void OnPreSeasonStart()
	{
		this.OnPartAdaptation(false);
		if (this.isPlayerChampionship)
		{
			App.instance.gameStateManager.SetState(GameState.Type.PreSeasonState, GameStateManager.StateChangeType.CheckForFadedScreenChange, false);
			Game.instance.dialogSystem.OnPreSeasonStartMessages();
		}
		else if (this.championshipID == 0 && Game.instance.player.IsUnemployed())
		{
			App.instance.gameStateManager.SetState(GameState.Type.PreSeasonState, GameStateManager.StateChangeType.CheckForFadedScreenChange, false);
		}

		// on season start Reroll supplier random modifiers for this championship suppliers
		Game.instance.supplierManager.rollRandomBaseStatModifiers(this.championshipID + 1);
		// reset number of contracts for supplier
		Game.instance.supplierManager.ResetContractsForTier(this.championshipID + 1);

		// create new shuffled team list (teams should select suppliers in random order, because some suppliers only accept limited number of contracts)
		Team[] teamList = new Team[this.standings.teamEntryCount];
		for (int i = 0; i < this.standings.teamEntryCount; i++) {
			teamList[i] = this.standings.GetTeamEntry(i).GetEntity<Team>();
		}
		// Fisher-Yates shuffle for team list
		for (int i = 0; i < teamList.Length; i++) {
			int k = RandomUtility.GetRandom(0, teamList.Length - 1);
			Team team = teamList[k];
			teamList[k] = teamList[i];
			teamList[i] = team;
		}

		for (int i = 0; i < teamList.Length; i++)
		{
			global::Debug.LogErrorFormat("PreSeason Preperation for Team {0}", new object[] { teamList[i].GetShortName() });
			teamList[i].OnPreSeasonStart();
		}
		this.GenerateCalendarEvents();
		this.politicalSystem.GenerateCalendarEvents();
		Game.instance.calendar.UpdateEventsShownOnCalendar();
	}

	public void OnPartAdaptation(bool inResetPartDevelopment = false)
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.carManager.carPartDesign.FinishPartImmediatly();
		}
		PartTypeSlotSettings partTypeSlotSettings;
		Team team = (this.inPromotedTeamFromLowerTier == null) ? null : this.inPromotedTeamFromLowerTier.team;
		Team team2 = (this.inRelegatedTeamFromHigherTier == null) ? null : this.inRelegatedTeamFromHigherTier.team;
		List<CarPart.PartType> list = new List<CarPart.PartType>();
		for (CarStats.StatType statType = CarStats.StatType.TopSpeed; statType < CarStats.StatType.Count; statType++)
		{
			int num = int.MaxValue;
			int num2 = int.MinValue;
			List<Car> carStandingsOnStat = CarManager.GetCarStandingsOnStat(statType, this, new Team[]
			{
				team,
				team2
			});
			CarPart.PartType partForStatType = CarPart.GetPartForStatType(statType, this.series);
			if (partForStatType != CarPart.PartType.None)
			{
				partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.championshipID][partForStatType];
				for (int j = 0; j < carStandingsOnStat.Count; j++)
				{
					CarPart part = carStandingsOnStat[j].GetPart(partForStatType);
					if (part != null)
					{
						float statWithPerformance = part.stats.statWithPerformance;
						num2 = (int)Mathf.Max((float)num2, statWithPerformance);
						num = (int)Mathf.Min((float)num, statWithPerformance);
					}
				}
				this.rules.partStatSeasonMinValue[partForStatType] = num;
				this.rules.partStatSeasonMaxValue[partForStatType] = num2 + GameStatsConstants.newSeasonMaxPartCap;
				if (num2 > partTypeSlotSettings.championshipMaxStat)
				{
					list.Add(partForStatType);
				}
			}
		}
		if (this.inPromotedTeamFromLowerTier != null && !this.inPromotedTeamFromLowerTier.carStatsDataUsed)
		{
			this.inPromotedTeamFromLowerTier.carStatsDataUsed = true;
			this.AdaptPartsForNewTeams(this.inPromotedTeamFromLowerTier.team, this.inPromotedTeamFromLowerTier.teamPreviousChampPartStatRankings);
		}
		if (this.inRelegatedTeamFromHigherTier != null && !this.inRelegatedTeamFromHigherTier.carStatsDataUsed)
		{
			this.inRelegatedTeamFromHigherTier.carStatsDataUsed = true;
			this.AdaptPartsForNewTeams(this.inRelegatedTeamFromHigherTier.team, this.inRelegatedTeamFromHigherTier.teamPreviousChampPartStatRankings);
		}
		bool flag = list.Count > 0 || inResetPartDevelopment;
		if (flag)
		{
			this.ResetPartTypeStatsProgression(CarPart.GetPartType(this.series, false));
		}
		for (int k = 0; k < this.standings.teamEntryCount; k++)
		{
			Team entity2 = this.standings.GetTeamEntry(k).GetEntity<Team>();
			if (team != entity2 && team2 != entity2)
			{
				entity2.carManager.AdaptPartsForNewSeason(null);
			}
		}

		// recalculate stats for alle part types
		CarPart.PartType[] partType = CarPart.GetPartType(this.series, false);
		List<CarPart> parts;
		float mainStat;
		float newMainStat;

		float minStat;
		float maxStat;

		foreach (CarPart.PartType inType in partType) {

			// MinMax Stat for parts from championship
			partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.championshipID][inType];
			minStat = partTypeSlotSettings.championshipMinStat;
			maxStat = partTypeSlotSettings.championshipMaxStat;

			// skip step for Engines, there performance is decided by suppliers
			if (inType == CarPart.PartType.Engine)
				continue;

			// find best and worst performance stat over all teams
			float bestStat  = float.MinValue;
			float worstStat = float.MaxValue;

			for (int n_team = 0; n_team < this.standings.teamEntryCount; n_team++) {
				team = this.standings.GetTeamEntry(n_team).GetEntity<Team>();
				if (team == null)
					continue;
				parts = team.carManager.partInventory.GetPartInventory(inType);
				for (int n_part = 0; n_part < parts.Count; n_part++) {
					mainStat = parts[n_part].stats.statWithPerformance;
					if (mainStat < worstStat)
						worstStat = mainStat;
					if (mainStat > bestStat)
						bestStat = mainStat;
				}
			}

			// calculate factor to keep stat inside boundries
			float factor = (maxStat - minStat) / (bestStat - worstStat);
			if (factor > 1f)
				factor = 1f;

			if (this.championshipID == 0) {
				global::Debug.LogErrorFormat("Stats for PartType {0} are: worst = {1}; best = {2}; factor is {3}", new object[] {
					inType,
					worstStat,
					bestStat,
					factor.ToString("0.00")
				});
			}

			// set main stat within boundry
			for (int n_team = 0; n_team < this.standings.teamEntryCount; n_team++) {
				team = this.standings.GetTeamEntry(n_team).GetEntity<Team>();
				if (team == null)
					continue;
				parts = team.carManager.partInventory.GetPartInventory(inType);
				for (int n_part = 0; n_part < parts.Count; n_part++) {
					mainStat = parts[n_part].stats.statWithPerformance;
					// calculate new main stat
					newMainStat = maxStat - ((bestStat - mainStat) * factor);
					// set new main stat
					parts[n_part].stats.SetStat(CarPartStats.CarPartStat.MainStat, newMainStat);

					if (this.championshipID == 0) {
						global::Debug.LogErrorFormat("New Part Stats for Team {0} PartNum {1}; before: {2}; after: {3}", new object[] {
							team.GetShortName(false),
							n_part,
							mainStat,
							newMainStat
						});
					}
				}
			}
		}

		this.rules.ApplySpecParts();
	}

	private void AdaptPartsForNewTeams(Team inTeam, CarStats inPreviousChampRankings)
	{
		if (inTeam != null)
		{
			for (CarStats.StatType statType = CarStats.StatType.TopSpeed; statType < CarStats.StatType.Count; statType++)
			{
				Team team = (this.inRelegatedTeamFromHigherTier == null) ? null : this.inRelegatedTeamFromHigherTier.team;
				Team team2 = (this.inPromotedTeamFromLowerTier == null) ? null : this.inPromotedTeamFromLowerTier.team;
				CarPart.PartType partForStatType = CarPart.GetPartForStatType(statType, this.series);
				if (partForStatType != CarPart.PartType.None)
				{
					List<Team> teamStandingsOnStat = TeamStatistics.GetTeamStandingsOnStat(statType, this, new Team[]
					{
						team,
						team2
					});
					float stat = inPreviousChampRankings.GetStat(statType);
					global::Debug.Assert(stat >= 0f && stat <= 1f, "Stat used for part adaptation is not normalized ");
					if (this.inRelegatedTeamFromHigherTier != null && inTeam == this.inRelegatedTeamFromHigherTier.team)
					{
						float num = GameStatsConstants.relegatedTeamSpreadScaler;
						float statWithPerformance = teamStandingsOnStat[2].carManager.partInventory.GetHighestStatPartOfType(partForStatType).stats.statWithPerformance;
						float statWithPerformance2 = teamStandingsOnStat[0].carManager.partInventory.GetHighestStatPartOfType(partForStatType).stats.statWithPerformance;
						float num2 = statWithPerformance2 - statWithPerformance;
						inPreviousChampRankings.SetStat(statType, statWithPerformance + num * num2 * stat);
					}
					else
					{
						float num = GameStatsConstants.promotedTeamSpreadScaler;
						float statWithPerformance3 = teamStandingsOnStat[teamStandingsOnStat.Count - 2].carManager.partInventory.GetHighestStatPartOfType(partForStatType).stats.statWithPerformance;
						float num3 = statWithPerformance3 - (float)this.rules.partStatSeasonMinValue[partForStatType];
						inPreviousChampRankings.SetStat(statType, (float)this.rules.partStatSeasonMinValue[partForStatType] + num * num3 * stat);
					}
				}
			}
			inTeam.carManager.AdaptPartsForNewSeason(inPreviousChampRankings);
		}
	}

	public void ResetPartTypeStatsProgression(params CarPart.PartType[] inType)
	{
		foreach (CarPart.PartType partType in inType)
		{
			PartTypeSlotSettings partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[this.championshipID][partType];
			this.rules.partStatSeasonMinValue[partType] = partTypeSlotSettings.baseMinStat;
			this.rules.partStatSeasonMaxValue[partType] = partTypeSlotSettings.baseMaxStat;
			this.nextYearsRules.partStatSeasonMinValue[partType] = partTypeSlotSettings.baseMinStat;
			this.nextYearsRules.partStatSeasonMaxValue[partType] = partTypeSlotSettings.baseMaxStat;
		}
	}

	private void OnLiveryEditPrompt()
	{
		if (this.isPlayerChampionship)
		{
			PreSeasonState preSeasonState = (PreSeasonState)App.instance.gameStateManager.currentState;
			preSeasonState.SetStage(PreSeasonState.PreSeasonStage.ChooseLivery);
			Game.instance.dialogSystem.OnLiveryEditPrompt();
		}
		else if (this.championshipID == 0 && Game.instance.player.IsUnemployed())
		{
			PreSeasonState preSeasonState2 = (PreSeasonState)App.instance.gameStateManager.currentState;
			preSeasonState2.SetStage(PreSeasonState.PreSeasonStage.ChooseLivery);
		}
	}

	private void OnPreSeasonTestingEnding()
	{
		if (this.isPlayerChampionship)
		{
			Game.instance.dialogSystem.OnPreSeasonTestingEndingMessages();
		}
	}

	private void OnPreSeasonEnd()
	{
		if (this.isPlayerChampionship)
		{
			Game.instance.dialogSystem.OnPreSeasonEndMessages();
		}
		else if (this.championshipID == 0 && Game.instance.player.IsUnemployed())
		{
			((PreSeasonState)App.instance.gameStateManager.currentState).SetStage(PreSeasonState.PreSeasonStage.InPreSeasonTest);
		}
		int teamEntryCount = this.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.OnPreSeasonEnd();
		}
		this.SetCurrentSeasonDates();
		this.OnSeasonStart();
		this.inPromotedTeamFromLowerTier = null;
		this.inRelegatedTeamFromHigherTier = null;
	}

	public void OnSeasonStart()
	{
		this.rules.ValidateChampionshipRules();
		this.nextYearsRules.ValidateChampionshipRules();
		this.GenerateNewChampionshipHistory();
		int teamEntryCount = this.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.carManager.carPartDesign.OnNewSeasonStart();
			// initialize chassi development for F1 Teams
			if (this.series == Championship.Series.SingleSeaterSeries && this.championshipID == 0)
				Game.instance.supplierManager.InitializeChasiDevelopment(entity);
		}
		for (int j = 0; j < teamEntryCount; j++)
		{
			Team entity2 = this.standings.GetTeamEntry(j).GetEntity<Team>();
			bool inWasPromoted = false;
			bool inWasRelegated = false;
			if (this.inPromotedTeamFromLowerTier != null && entity2 == this.inPromotedTeamFromLowerTier.team)
			{
				inWasPromoted = true;
			}
			else if (this.inRelegatedTeamFromHigherTier != null && entity2 == this.inRelegatedTeamFromHigherTier.team)
			{
				inWasRelegated = true;
			}
			entity2.OnNewSeasonStart(inWasPromoted, inWasRelegated);
		}
		this.UpdateTeamExpectations();
		this.seasonDirector.OnSeasonStart(this);
		if (this.championshipID == 0)
		{
			Game.instance.driverManager.OnSeasonStart();
		}
		this.ApplyWeightStrippingRatios();
		this.ValidateConcurrentChampionshipCalendar();
		if (Game.IsActive() && this.isPlayerChampionship)
		{
			Game.instance.dialogSystem.SendPlayerRuleDependentMails();
		}
	}

	private void RecordChampionshipResult()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			int previousSeasonTeamResult = i + 1;
			entity.history.previousSeasonTeamResult = previousSeasonTeamResult;
		}
	}

	public void OnSeasonEnd()
	{
		// Update Chassi for F1 Teams
		if (this.championshipID == 0) {
			Game.instance.supplierManager.UpdateTeamChassi(this.standings);
		}

		this.standings.UpdateStandings();
		this.standingsHistory.AddEntry();
		this.HandoutPrizeMoney();
		this.RecordChampionshipResult();
		if (this.standings.driverEntryCount > 0)
		{
			Driver entity = this.standings.GetDriverEntry(0).GetEntity<Driver>();
			Team team = entity.contract.GetTeam();
			if (team is NullTeam)
			{
				entity.careerHistory.currentEntry.IncreaseStat(History.HistoryStat.Championships, 1);
			}
			else if (this.series == Championship.Series.EnduranceSeries)
			{
				List<Driver> championshipWinners = this.standings.GetChampionshipWinners(entity);
				for (int i = 0; i < championshipWinners.Count; i++)
				{
					team.IncreaseDriverHistoryStat(History.HistoryStat.Championships, championshipWinners[i], true, 1);
				}
			}
			else
			{
				team.IncreaseDriverHistoryStat(History.HistoryStat.Championships, entity, true, 1);
			}
			this.records.PostChampionshipWin(entity);
		}
		if (this.standings.teamEntryCount > 0)
		{
			Team entity2 = this.standings.GetTeamEntry(0).GetEntity<Team>();
			entity2.IncreaseHistoryStat(History.HistoryStat.Championships, 1);
			entity2.IncreaseStaffHistoryStat(History.HistoryStat.Championships, 1);
			entity2.AddToMarketebility(0.25f);
			this.mChampionshipPromotions.champion = entity2;
			this.mChampionshipPromotions.lastPlace = this.standings.GetTeamEntry(this.standings.teamEntryCount - 1).GetEntity<Team>();
			this.mChampionshipPromotions.championPartRankings = TeamStatistics.GetPartRankingsForChampionship(this.mChampionshipPromotions.champion);
			this.mChampionshipPromotions.lastPlacePartRankings = TeamStatistics.GetPartRankingsForChampionship(this.mChampionshipPromotions.lastPlace);
		}
		this.PostChampionsWinData();
		if (this.isPlayerChampionship)
		{
			Game.instance.dialogSystem.OnSeasonEndMessages();
			Game.instance.achievementData.OnNewSeasonSetup();
		}
		this.politicalSystem.endOfSeasonMessage = false;
		for (int j = 0; j < this.standings.teamEntryCount; j++)
		{
			Team entity3 = this.standings.GetTeamEntry(j).GetEntity<Team>();
			entity3.HandleEndOfSeason();
			entity3.rulesBrokenThisSeason = 0;
		}
		this.seasonDirector.OnSeasonEnd(this);
	}

	private void PostChampionsWinData()
	{
		ChampionshipEntry_v1 driverEntry = this.standings.GetDriverEntry(0);
		Driver entity = driverEntry.GetEntity<Driver>();
		ChampionshipEntry_v1 teamEntry = this.standings.GetTeamEntry(0);
		Team entity2 = teamEntry.GetEntity<Team>();
		Driver[] driverChampionsEndurance = null;
		if (this.series == Championship.Series.EnduranceSeries)
		{
			driverChampionsEndurance = this.standings.GetChampionshipWinners(entity).ToArray();
		}
		ChampionshipWinnersEntry inEntry = new ChampionshipWinnersEntry
		{
			driverChampion = entity,
			driverChampionsEndurance = driverChampionsEndurance,
			driverPodiums = driverEntry.podiums,
			driverRaces = driverEntry.races,
			driverPoints = driverEntry.GetCurrentPoints(),
			driverWins = driverEntry.wins,
			driverDNFs = driverEntry.DNFs,
			driversTeam = entity.contract.GetTeam(),
			driversTeamPrincipal = entity.contract.GetTeam().teamPrincipal,
			teamChampion = entity2,
			teamPoints = teamEntry.GetCurrentPoints()
		};
		this.records.PostChampionshipWinnersData(inEntry, Game.instance.time.now.Year);
	}

	public void OnChampionshipPromotionsEnd()
	{
		this.preSeasonTesting.Reset();
		this.mEventNumber = 0;
		if (this.IsConcurrentChampionship() && !this.IsMainConcurrentChampionship())
		{
			this.nextYearsRules.CopyConcurrentRules(this.GetMainConcurrentChampionship().rules);
		}
		this.rules = this.nextYearsRules;
		this.rules.ActivateRules();
		this.rules.ActivateRulesThatAffectCalendar();
		this.nextYearsRules = this.rules.Clone();
		if (Game.IsActive() && this.isPlayerChampionship)
		{
			Game.instance.dialogSystem.OnPoliticsNewYearRulesApplied(null);
			Game.instance.calendar.UpdateEventsShownOnCalendar();
		}
		this.calendar = new List<RaceEventDetails>(this.nextYearsCalendar);
		this.RecreateCalendarSessions(this.calendar);
		this.RecreateStandings();
		this.GenerateNextYearCalendar(true);
		this.SetupNextYearCalendarWeather();
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.pitCrewController.OnChampionshipRulesApplied();
		}
		if (this.IsConcurrentChampionship() && !this.IsMainConcurrentChampionship())
		{
			this.rules.InitializeConcurrentRules();
		}
	}

	private void RecreateCalendarSessions(List<RaceEventDetails> inCalendar)
	{
		for (int i = 0; i < inCalendar.Count; i++)
		{
			inCalendar[i].RecreateSessions(this.rules);
		}
		this.SetupCalendarWeather();
	}

	public void ValidateConcurrentChampionshipCalendar()
	{
		if (this.IsConcurrentChampionship() && !this.IsMainConcurrentChampionship())
		{
			Championship mainConcurrentChampionship = this.GetMainConcurrentChampionship();
			for (int i = 0; i < this.calendar.Count; i++)
			{
				this.calendar[i].CopyWeather(mainConcurrentChampionship.calendar[i]);
			}
		}
	}

	private void HandlePromotions()
	{
		this.HandleChampionshipPromotions();
	}

	private void HandleChampionshipPromotions()
	{
		if (this.championshipAboveID != -1)
		{
			if (this.mChampionshipPromotions.champion.IsPlayersTeam())
			{
				Game.instance.dialogSystem.OnPlayerTeamPromotable();
			}
			else
			{
				this.SetChampionshipReadyForPromotions();
			}
		}
		else
		{
			this.SetChampionshipReadyForPromotions();
		}
	}

	public void CompletePlayerPromotionOffer(bool inPlayerAcceptPromotion)
	{
		this.ProcessChampionshipPromotions(inPlayerAcceptPromotion);
		this.SetChampionshipReadyForPromotions();
		this.CheckPlayerPromotionAchievements(inPlayerAcceptPromotion);
	}

	private void SetChampionshipReadyForPromotions()
	{
		this.readyForPromotions = true;
		Game.instance.championshipManager.FinishPromotions();
	}

	public void ProcessChampionshipPromotions(bool inPlayerAcceptPromotion = true)
	{
		Championship nextTierChampionship = this.GetNextTierChampionship();
		bool flag = (!this.isPlayerChampionship) ? (RandomUtility.GetRandom01() <= 0.85f) : inPlayerAcceptPromotion;
		int num = 0;
		if (nextTierChampionship != null && nextTierChampionship.championshipPromotions.lastPlace.IsPlayersTeam())
		{
			Game.instance.dialogSystem.OnPlayerTeamRelegatable(flag, this.championshipID);
		}
		if (nextTierChampionship != null && flag)
		{
			num++;
			Team lastPlace = nextTierChampionship.championshipPromotions.lastPlace;
			nextTierChampionship.championshipPromotions.lastPlaceStatus = ChampionshipPromotions.Status.Relegated;
			nextTierChampionship.RemoveTeamEntry(lastPlace);
			this.RemoveTeamEntry(this.mChampionshipPromotions.champion);
			this.mChampionshipPromotions.championStatus = ChampionshipPromotions.Status.Promoted;
			nextTierChampionship.SetPromotedTeam(this.mChampionshipPromotions.champion, this, this.mChampionshipPromotions.championPartRankings);
			nextTierChampionship.AddTeamEntry(this.mChampionshipPromotions.champion);
			this.SetRelegatedTeam(lastPlace, nextTierChampionship, nextTierChampionship.championshipPromotions.lastPlacePartRankings);
			this.AddTeamEntry(lastPlace);
			nextTierChampionship.standings.UpdateStandings();
			this.standings.UpdateStandings();
		}
		else
		{
			this.mChampionshipPromotions.championStatus = ChampionshipPromotions.Status.RefusedPromotion;
			if (nextTierChampionship != null)
			{
				nextTierChampionship.mChampionshipPromotions.championStatus = ChampionshipPromotions.Status.SavedFromRelegation;
			}
		}
		this.completedPromotions = true;
	}

	private void CheckPlayerPromotionAchievements(bool inPlayerAcceptPromotion)
	{
		if (inPlayerAcceptPromotion)
		{
			Championship nextTierChampionship = this.GetNextTierChampionship();
			if (nextTierChampionship != null && nextTierChampionship.championshipID == 5 && Game.instance.player.team.teamID == 59)
			{
				App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Achievement_3_4);
			}
		}
	}

	public void GenerateInitialChampionshipHistory()
	{
		// no randomly generated history
	}

	public void GenerateNewChampionshipHistory()
	{
		int teamEntryCount = this.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			if (entity != null && !(entity is NullTeam))
			{
				List<Person> allEmployees = entity.contractManager.GetAllEmployees();
				int count = allEmployees.Count;
				for (int j = 0; j < count; j++)
				{
					allEmployees[j].careerHistory.AddHistory();
				}
			}
		}
	}

	public void FinishCareerHistorySeasonEntry()
	{
		int teamEntryCount = this.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			if (entity != null && !(entity is NullTeam))
			{
				List<Person> allEmployees = entity.contractManager.GetAllEmployees();
				int count = allEmployees.Count;
				for (int j = 0; j < count; j++)
				{
					allEmployees[j].careerHistory.MarkLastEntryTeamAsFinished(entity);
				}
			}
		}
	}

	public void SetPromotedTeam(Team inTeam, Championship inChampionship, CarStats inStats)
	{
		this.inPromotedTeamFromLowerTier = new ChampionshipPromotionData(inTeam, inChampionship, ChampionshipPromotionData.TeamStatus.Promoted, inStats);
	}

	public void SetRelegatedTeam(Team inTeam, Championship inChampionship, CarStats inStats)
	{
		this.inRelegatedTeamFromHigherTier = new ChampionshipPromotionData(inTeam, inChampionship, ChampionshipPromotionData.TeamStatus.Relegated, inStats);
	}

	public void RemoveTeamEntry(Team inTeam)
	{
		for (int i = 0; i < Team.maxDriverCount; i++)
		{
			Driver driver = inTeam.GetDriver(i);
			if (driver != null)
			{
				this.standings.RemoveEntry(driver);
				driver.ResetChampionshipEntry();
			}
		}
		this.standings.RemoveEntry(inTeam);
		inTeam.ResetChampionshipEntry();
	}

	public void AddTeamEntry(Team inTeam)
	{
		List<Driver> drivers = inTeam.GetDrivers();
		for (int i = 0; i < drivers.Count; i++)
		{
			Driver driver = drivers[i];
			if (!driver.IsReserveDriver())
			{
				this.standings.AddEntry(driver, this);
				driver.GetChampionshipEntry();
			}
		}
		this.standings.AddEntry(inTeam, this);
		inTeam.GetChampionshipEntry();
		if (inTeam.championship != this)
		{
			inTeam.championship = this;
			inTeam.NotifyChampionshipChanged();
		}
	}

	public RaceEventDetails GetPreviousEventDetails()
	{
		if (this.HasSeasonEnded())
		{
			return this.GetFinalEventDetails();
		}
		int num = this.mEventNumber - 1;
		if (num >= 0)
		{
			return this.calendar[num];
		}
		return null;
	}

	public RaceEventDetails GetCurrentEventDetails()
	{
		return this.calendar[this.mEventNumber];
	}

	public RaceEventDetails GetNextEventDetails()
	{
		int num = this.mEventNumber + 1;
		if (num < this.calendar.Count)
		{
			return this.calendar[num];
		}
		return null;
	}

	public RaceEventDetails GetFirstEventDetails()
	{
		return this.calendar[0];
	}

	public RaceEventDetails GetFinalEventDetails()
	{
		int num = this.calendar.Count - 1;
		if (num >= 0 && num < this.calendar.Count)
		{
			return this.calendar[num];
		}
		return null;
	}

	public RaceEventDetails GetEventDetailsForNextEventAfterDate(DateTime date)
	{
		for (int i = 0; i < this.calendar.Count; i++)
		{
			if (this.calendar[i].eventDate > date)
			{
				return this.calendar[i];
			}
		}
		return null;
	}

	public bool HasSeasonEnded()
	{
		return this.GetFinalEventDetails().hasEventEnded;
	}

	public void OnSessionStart()
	{
		RaceEventDetails currentEventDetails = this.GetCurrentEventDetails();
		currentEventDetails.OnSessionStart();
		Game.instance.persistentEventData.OnSessionStart();
	}

	public void OnSessionEnd()
	{
		RaceEventDetails currentEventDetails = this.GetCurrentEventDetails();
		bool flag = App.instance.gameStateManager.currentState.group == GameState.Group.Frontend;
		bool flag2 = currentEventDetails.currentSession.sessionType == SessionDetails.SessionType.Race;
		if (flag2 && flag)
		{
			Game.instance.sessionManager.ScrutinizePartRules();
		}
		currentEventDetails.OnSessionEnd(flag || currentEventDetails.currentSession.sessionType == SessionDetails.SessionType.Qualifying, this);
		if (flag || currentEventDetails.currentSession.sessionType == SessionDetails.SessionType.Qualifying)
		{
			this.ProcessTeamObjectives();
		}
		if (flag2)
		{
			this.UpdateTeamsPostEventData();
		}
		if (Game.instance.gameType != Game.GameType.SingleEvent && this.isPlayerChampionship && !flag2)
		{
			Game.instance.mediaManager.GetStoryForSession(Game.instance.sessionManager.eventDetails);
		}
	}

	public void UpdateTeamsPostEventData()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.financeController.AddPostEventTransactions();
			entity.carManager.partImprovement.FixCondition();
			entity.carManager.ResetSessionSetupCarStatsContribution();
			entity.contractManager.ResetSittingOutEventDriver(true);
		}
	}

	public void ApplyWeightStripping(RaceEventDetails inDetails)
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			if (!entity.IsPlayersTeam())
			{
				entity.teamAIController.SetWeightStrippingForEvent(inDetails);
			}
		}
	}

	public void ApplyWeightStrippingRatios()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			List<CarPart> allParts = entity.carManager.partInventory.GetAllParts();
			for (int j = 0; j < allParts.Count; j++)
			{
				allParts[j].stats.SetWeightStrippingReliabilityRatio(this.rules.weightStrippingRatio);
			}
		}
	}

	public void ResetWeightStrippingForTeams()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.carManager.ResetWeightStripping();
		}
	}

	public void ProcessTeamObjectives()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.ProcessSessionResults();
		}
	}

	private void HandoutPrizeMoney()
	{
		int num = Mathf.Min(this.standings.teamEntryCount, this.rules.prizePoolPercentage.Count);
		for (int i = 0; i < num; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			StringVariableParser.ordinalNumberString = GameUtility.FormatForPosition(i + 1, null);
			Transaction transaction = new Transaction(Transaction.Group.PrizeMoney, Transaction.Type.Credit, this.rules.GetPrizeMoney(i, this.prizeFund), Localisation.LocaliseID("PSG_10010556", null));
			entity.financeController.unnallocatedTransactions.Add(transaction);
		}
	}

	public void GoToNextSession()
	{
		RaceEventDetails currentEventDetails = this.GetCurrentEventDetails();
		currentEventDetails.GoToNextSession();
	}

	public void EndEvent()
	{
		this.UpdateMechanicDriverRelationships();
		Game.instance.persistentEventData.OnEventEnd();
		int num = this.mEventNumber;
		this.mEventNumber = Mathf.Min(this.mEventNumber + 1, this.calendar.Count - 1);
		if (this.mEventNumber != num)
		{
			this.standings.OnEventEnded();
		}
		MessageData messageData = new MessageData();
		messageData.Add("Championship", this);
		Game.instance.messageManager.DoEvent<MessageEvent_OnEventEnd>(messageData);
		this.UpdateTeamExpectations();
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			entity.sponsorController.SetWeekendSponsor(null);
		}
	}

	private void UpdateTeamExpectations()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			Circuit circuit = this.GetCurrentEventDetails().circuit;
			entity.expectedRacePosition = Game.instance.teamManager.CalculateExpectedPositionForRace(entity, circuit);
		}
	}

	private void UpdateMechanicDriverRelationships()
	{
		RaceEventResults.SessonResultData resultsForSession = this.GetCurrentEventDetails().results.GetResultsForSession(SessionDetails.SessionType.Race);
		int count = resultsForSession.resultData.Count;
		for (int i = 0; i < count; i++)
		{
			RaceEventResults.ResultData resultData = resultsForSession.resultData[i];
			Team team = resultData.driver.contract.GetTeam();
			if (team.championship == this)
			{
				team.EndRaceUpdateMechanicsRelationshipWithDrivers(resultData.driver, resultsForSession.resultData, this);
			}
		}
	}

	public void GenerateCalendar()
	{
		this.calendar.Clear();
		if (Game.instance.isCareer)
		{
			for (int i = 0; i < this.calendarData.Count; i++)
			{
				RaceEventDetails raceEventDetails = new RaceEventDetails();
				raceEventDetails.circuit = this.calendarData[i].circuit;
				raceEventDetails.CalculateEventDate(this.calendarData[i].week, Game.instance.time.now.Year, this.rules);
				this.calendar.Add(raceEventDetails);
			}
			this.GenerateCalendarEvents();
		}
		else
		{
			RaceEventDetails raceEventDetails2 = new RaceEventDetails();
			raceEventDetails2.circuit = Game.instance.circuitManager.GetCircuitByIndex(0);
			raceEventDetails2.CalculateEventDate(1, Game.instance.time.now.Year, this.rules);
			this.calendar.Add(raceEventDetails2);
		}
	}

	public void SetupCalendarWeather()
	{
		this.SetupCalendarWeatherImpl(ref this.calendar);
	}

	public void SetupNextYearCalendarWeather()
	{
		this.SetupCalendarWeatherImpl(ref this.nextYearsCalendar);
	}

	private void SetupCalendarWeatherImpl(ref List<RaceEventDetails> inCalendar)
	{
		if (inCalendar != null)
		{
			int count = inCalendar.Count;
			List<int> list = new List<int>();
			for (int i = 0; i < count; i++)
			{
				inCalendar[i].SetupWeather(SessionDetails.WeatherSettings.Dynamic, this);
				if (!inCalendar[i].raceSessions[0].sessionWeather.WillRaceBeDry())
				{
					list.Add(i);
				}
			}
			float num = (float)count;
			float num2 = (float)(count - list.Count) / num;
			if (num2 < 0.6f)
			{
				int num3 = Mathf.RoundToInt(num * (0.6f - num2));
				while (num3 > 0 && list.Count > 0)
				{
					int num4 = UnityEngine.Random.Range(0, list.Count);
					inCalendar[list[num4]].SetupWeather(SessionDetails.WeatherSettings.Dry, this);
					list.RemoveAt(num4);
					num3--;
				}
			}
		}
	}

	public void RecalculateNextYearCalendarWeeks()
	{
		float num = (float)this.seasonStart;
		float num2 = 0f;
		int count = this.calendarData.Count;
		if (this.calendarData.Count > 1)
		{
			num2 = (float)(this.seasonEnd - this.seasonStart) / (float)(count - 1);
		}
		for (int i = 0; i < count; i++)
		{
			RaceEventCalendarData raceEventCalendarData = this.calendarData[i];
			if (i == count - 1)
			{
				raceEventCalendarData.week = this.seasonEnd;
			}
			else
			{
				raceEventCalendarData.week = Mathf.RoundToInt(num);
			}
			num += num2;
		}
	}

	public void GenerateNextYearCalendar(bool inGenerateDates)
	{
		this.nextYearsCalendar.Clear();
		for (int i = 0; i < this.calendarData.Count; i++)
		{
			RaceEventDetails raceEventDetails = new RaceEventDetails();
			raceEventDetails.circuit = this.calendarData[i].circuit;
			raceEventDetails.CalculateEventDate(this.calendarData[i].week, this.calendar[0].eventDate.Year + 1, this.nextYearsRules);
			this.nextYearsCalendar.Add(raceEventDetails);
		}
		if (inGenerateDates)
		{
			this.GenerateNextYearCalendarDates();
		}
	}

	private void GenerateCalendarEvents()
	{
		int count = this.calendar.Count;
		for (int i = this.eventNumber; i < count; i++)
		{
			RaceEventDetails raceEventDetails = this.calendar[i];
			int eventNum = i;
			StringVariableParser.newTrack = raceEventDetails.circuit;
			StringVariableParser.randomChampionship = this;
			CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
			{
				showOnCalendar = true,
				category = CalendarEventCategory.TravelToEvent,
				triggerDate = raceEventDetails.eventDate,
				triggerState = GameState.Type.FrontendState,
				interruptGameTime = true,
				uiState = CalendarEvent.UIState.TimeBarFillTarget,
				effect = new GoToEventPreStateEffect
				{
					championship = this
				},
				OnButtonClick = MMAction.CreateFromAction(new Action(new ChangeEventCalendarScreenCommand(this, eventNum).Execute)),
				displayEffect = new ChampionshipDisplayEffect
				{
					changeUIState = true,
					changeInterrupt = true,
					championship = this
				}
			};
			calendarEvent_v.SetDynamicDescription("PSG_10009141");
			Game.instance.calendar.AddEvent(calendarEvent_v);
			CalendarEvent_v1 calendarEvent = new CalendarEvent_v1
			{
				triggerDate = raceEventDetails.eventDate.AddHours(1.0),
				triggerState = GameState.Type.TravelArrangements,
				effect = new GoToEventStateEffect
				{
					championship = this
				}
			};
			Game.instance.calendar.AddEvent(calendarEvent);
			for (int j = 0; j < raceEventDetails.practiceSessions.Count; j++)
			{
				SessionDetails sessionDetails = raceEventDetails.practiceSessions[j];
				CalendarEvent_v1 calendarEvent_v2 = new CalendarEvent_v1
				{
					showOnCalendar = (this.isChoosable && j == 0),
					category = CalendarEventCategory.Race,
					triggerDate = sessionDetails.sessionDateTime,
					OnButtonClick = MMAction.CreateFromAction(new Action(new ChangeEventCalendarScreenCommand(this, eventNum).Execute))
				};
				if (calendarEvent_v2.showOnCalendar)
				{
					calendarEvent_v2.displayEffect = new ChampionshipDisplayEffect
					{
						sessionType = SessionDetails.SessionType.Practice,
						changeDisplaySessionActive = true,
						changeInterrupt = true,
						changeDisplay = true,
						championship = this
					};
				}
				calendarEvent_v2.SetDynamicDescription("PSG_10009142");
				Game.instance.calendar.AddEvent(calendarEvent_v2);
			}
			for (int k = 0; k < raceEventDetails.qualifyingSessions.Count; k++)
			{
				SessionDetails sessionDetails2 = raceEventDetails.qualifyingSessions[k];
				CalendarEvent_v1 calendarEvent_v3 = new CalendarEvent_v1
				{
					showOnCalendar = (this.isChoosable && k == 0),
					category = CalendarEventCategory.Race,
					triggerDate = sessionDetails2.sessionDateTime,
					OnButtonClick = MMAction.CreateFromAction(new Action(new ChangeEventCalendarScreenCommand(this, eventNum).Execute))
				};
				if (calendarEvent_v3.showOnCalendar)
				{
					calendarEvent_v3.displayEffect = new ChampionshipDisplayEffect
					{
						sessionType = SessionDetails.SessionType.Qualifying,
						changeDisplaySessionActive = true,
						changeInterrupt = true,
						changeDisplay = true,
						championship = this
					};
				}
				calendarEvent_v3.SetDynamicDescription("PSG_10009143");
				Game.instance.calendar.AddEvent(calendarEvent_v3);
			}
			for (int l = 0; l < raceEventDetails.raceSessions.Count; l++)
			{
				SessionDetails sessionDetails3 = raceEventDetails.raceSessions[l];
				CalendarEvent_v1 calendarEvent_v4 = new CalendarEvent_v1
				{
					showOnCalendar = (this.isChoosable && l == 0),
					category = CalendarEventCategory.Race,
					triggerDate = sessionDetails3.sessionDateTime,
					triggerState = GameState.Type.FrontendState,
					effect = new GoToSimulateEventEffect
					{
						championship = this
					},
					OnButtonClick = MMAction.CreateFromAction(new Action(new ChangeEventCalendarScreenCommand(this, eventNum).Execute))
				};
				if (calendarEvent_v4.showOnCalendar)
				{
					calendarEvent_v4.displayEffect = new ChampionshipDisplayEffect
					{
						sessionType = SessionDetails.SessionType.Race,
						changeDisplaySessionActive = true,
						changeInterrupt = true,
						changeDisplay = true,
						championship = this
					};
				}
				calendarEvent_v4.SetDynamicDescription("PSG_10009144");
				Game.instance.calendar.AddEvent(calendarEvent_v4);
			}
		}
	}

	private void GenerateNextYearCalendarDates()
	{
		if (Game.instance.isCareer)
		{
			List<DateTime> list = new List<DateTime>();
			DateTime eventDate = this.calendar[this.calendar.Count - 1].eventDate;
			this.seasonEndDate = eventDate.AddDays(5.0);
			this.preSeasonStartDate = this.seasonEndDate.AddDays(7.0);
			this.preSeasonEndDate = this.nextYearsCalendar[0].eventDate.AddDays(-11.0);
			DateTime dateTime = this.preSeasonEndDate.AddDays(-7.0);
			DateTime dateTime2 = this.preSeasonEndDate.AddDays(-14.0);
			List<DateTime> list2 = list;
			List<DateTime> list3 = new List<DateTime>();
			list3.Add(eventDate);
			list3.Add(this.seasonEndDate);
			list3.Add(this.preSeasonStartDate);
			list3.Add(this.preSeasonEndDate);
			list3.Add(dateTime);
			list3.Add(dateTime2);
			list2.AddRange(list3);
			StringVariableParser.randomChampionship = this;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] < Game.instance.time.now)
				{
					global::Debug.LogError("Championship calendar events cannot be set in the past", null);
				}
			}
			CalendarEvent_v1 calendarEvent_v = new CalendarEvent_v1
			{
				showOnCalendar = false,
				category = CalendarEventCategory.Championship,
				triggerDate = this.preSeasonStartDate,
				triggerState = GameState.Type.FrontendState,
				interruptGameTime = false,
				uiState = CalendarEvent.UIState.None,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.OnPreSeasonStart)),
				displayEffect = new ChampionshipDisplayEffect
				{
					changeInterrupt = true,
					championship = this,
					showWMCIfUnemployed = true
				}
			};
			calendarEvent_v.SetDynamicDescription("PSG_10009145");
			CalendarEvent_v1 calendarEvent_v2 = new CalendarEvent_v1
			{
				showOnCalendar = false,
				category = CalendarEventCategory.Championship,
				triggerDate = dateTime,
				triggerState = GameState.Type.PreSeasonState,
				interruptGameTime = false,
				uiState = CalendarEvent.UIState.None,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.OnPreSeasonTestingEnding))
			};
			calendarEvent_v2.SetDynamicDescription("PSG_10011067");
			CalendarEvent_v1 calendarEvent_v3 = new CalendarEvent_v1
			{
				showOnCalendar = false,
				category = CalendarEventCategory.Championship,
				triggerDate = this.preSeasonEndDate,
				triggerState = GameState.Type.PreSeasonState,
				interruptGameTime = false,
				uiState = CalendarEvent.UIState.None,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.OnPreSeasonEnd)),
				displayEffect = new ChampionshipDisplayEffect
				{
					changeInterrupt = true,
					championship = this,
					showWMCIfUnemployed = true
				}
			};
			calendarEvent_v3.SetDynamicDescription("PSG_10009146");
			CalendarEvent_v1 calendarEvent_v4 = new CalendarEvent_v1
			{
				showOnCalendar = false,
				category = CalendarEventCategory.Design,
				triggerDate = dateTime2,
				triggerState = GameState.Type.PreSeasonState,
				interruptGameTime = false,
				uiState = CalendarEvent.UIState.None,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.OnLiveryEditPrompt)),
				displayEffect = new ChampionshipDisplayEffect
				{
					changeInterrupt = true,
					championship = this,
					showWMCIfUnemployed = true
				}
			};
			calendarEvent_v4.SetDynamicDescription("PSG_10009148");
			CalendarEvent_v1 calendarEvent_v5 = new CalendarEvent_v1
			{
				showOnCalendar = true,
				category = CalendarEventCategory.Championship,
				triggerDate = this.seasonEndDate,
				triggerState = GameState.Type.FrontendState,
				interruptGameTime = false,
				uiState = CalendarEvent.UIState.None,
				effect = new ChampionshipSeasonEndEffect
				{
					championship = this
				}
			};
			calendarEvent_v5.SetDynamicDescription("PSG_10009149");
			CalendarEvent_v1 calendarEvent_v6 = new CalendarEvent_v1
			{
				showOnCalendar = true,
				category = CalendarEventCategory.Championship,
				triggerDate = this.seasonEndDate.AddHours(3.0),
				triggerState = GameState.Type.FrontendState,
				interruptGameTime = true,
				uiState = CalendarEvent.UIState.None,
				OnEventTrigger = MMAction.CreateFromAction(new Action(this.HandleChampionshipPromotions)),
				displayEffect = new ChampionshipDisplayEffect
				{
					changeInterrupt = true,
					championship = this
				}
			};
			calendarEvent_v6.SetDynamicDescription("PSG_10009150");
			Game.instance.calendar.AddEvent(calendarEvent_v5);
			Game.instance.calendar.AddEvent(calendarEvent_v6);
			Game.instance.calendar.AddEvent(calendarEvent_v);
			Game.instance.calendar.AddEvent(calendarEvent_v4);
			Game.instance.calendar.AddEvent(calendarEvent_v2);
			Game.instance.calendar.AddEvent(calendarEvent_v3);
		}
	}

	public void SetCurrentSeasonDates()
	{
		this.currentSeasonEndDate = this.seasonEndDate;
		this.currentPreSeasonStartDate = this.preSeasonStartDate;
		this.currentPreSeasonEndDate = this.preSeasonEndDate;
	}

	public void RecreateStandings()
	{
		List<Team> teamList = this.standings.GetTeamList();
		this.standings.ResetChampionshipEntries();
		this.standings.ClearStandings();
		int count = teamList.Count;
		for (int i = 0; i < count; i++)
		{
			Team inTeam = teamList[i];
			this.AddTeamEntry(inTeam);
		}
		this.standings.UpdateStandings();
	}

	public float GetNormalisedTimeToNextEvent()
	{
		RaceEventDetails previousEventDetails = this.GetPreviousEventDetails();
		RaceEventDetails currentEventDetails = this.GetCurrentEventDetails();
		DateTime eventDate = currentEventDetails.eventDate;
		DateTime eventDate2 = new DateTime(Game.instance.time.now.Year, 3, 10);
		if (previousEventDetails != null)
		{
			eventDate2 = previousEventDetails.eventDate;
		}
		double totalSeconds = (eventDate - eventDate2).TotalSeconds;
		double totalSeconds2 = (eventDate - Game.instance.time.now).TotalSeconds;
		return Mathf.Clamp01((float)((totalSeconds - totalSeconds2) / totalSeconds));
	}

	public Team GetTeamWithMostVotingPower()
	{
		Team team = null;
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			if (team == null || team.votingPower < entity.votingPower)
			{
				team = entity;
			}
		}
		return team;
	}

	public bool IsDriverChampionshipWon()
	{
		if (this.GetFinalEventDetails().hasEventEnded)
		{
			return true;
		}
		int num = 0;
		for (int i = this.eventNumber; i < this.eventCount; i++)
		{
			num += this.rules.GetPointsForPosition(1);
		}
		return this.standings.GetDriverEntry(1).GetCurrentPoints() + num < this.standings.GetDriverEntry(0).GetCurrentPoints();
	}

	public bool IsTeamChampionshipWon()
	{
		if (this.GetFinalEventDetails().hasEventEnded)
		{
			return true;
		}
		int num = 0;
		for (int i = this.eventNumber; i < this.eventCount; i++)
		{
			num += this.rules.GetPointsForPosition(1);
			num += this.rules.GetPointsForPosition(2);
		}
		return this.standings.GetTeamEntry(1).GetCurrentPoints() + num < this.standings.GetTeamEntry(0).GetCurrentPoints();
	}

	public void SetupAITeamsForEvent()
	{
		for (int i = 0; i < this.standings.teamEntryCount; i++)
		{
			Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
			if (!entity.IsPlayersTeam())
			{
				entity.teamAIController.SetupTeamForEvent();
			}
		}
	}

	public string GetIdentMovieString()
	{
		return "Champ" + this.logoID;
	}

	public void OnDriverChampionshipAwardedAchievements()
	{
		Driver entity = this.standings.GetDriverEntry(0).GetEntity<Driver>();
		bool flag = entity.IsPlayersDriver();
		SteamAchievementsManager steamAchievementsManager = App.instance.steamAchievementsManager;
		steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Finish_A_Season);
		if (flag)
		{
			Game.instance.achievementData.championships_won++;
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Driver_Champ);
			if (Game.instance.player.team.name == "Predator Racing Group")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Driver_Champ_Predator);
			}
			this.OnPlayerWinsAnyChampionshipAchievements();
		}
	}

	public void OnTeamChampionshipAwardedAchievements()
	{
		Team entity = this.standings.GetTeamEntry(0).GetEntity<Team>();
		bool flag = entity.IsPlayersTeam();
		SteamAchievementsManager steamAchievementsManager = App.instance.steamAchievementsManager;
		steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Finish_A_Season);
		if (flag)
		{
			long inPrizeMoney = (long)(this.rules.prizePoolPercentage[0] / 100f * (float)this.prizeFund);
			Game.instance.player.trophyHistory.AddTrophy(entity, this, this.seasonEndDate.Year, this.standings.GetTeamEntry(0).GetCurrentPoints(), inPrizeMoney);
			Game.instance.achievementData.championships_won++;
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Team_Champ);
			if (Game.instance.player.team.name == "Thornton Motorsport")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Team_Champ_Thornton);
			}
			if (this.championshipID == 2 && Game.instance.time.now.Year <= 2017 && Game.instance.player.team.name == "Predator Racing Group")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_By_2017_ERS_Predator);
			}
			if (this.championshipID == 0 && Game.instance.time.now.Year <= 2018 && Game.instance.player.team.name == "Kitano Sport")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_By_2018_WMC_Kitano);
			}
			if (this.championshipID == 5)
			{
				if (this.rules.isERSAdvancedModeActive)
				{
					Supplier supplierERSAdvanced = Game.instance.player.team.carManager.GetCar(0).ChassisStats.supplierERSAdvanced;
					if (supplierERSAdvanced != null)
					{
						switch (supplierERSAdvanced.id)
						{
						case 130:
							Game.instance.achievementData.wonIECAdvancedFlyWheelSupplier = true;
							break;
						case 131:
							Game.instance.achievementData.wonIECAdvancedBatterySupplier = true;
							break;
						case 132:
							Game.instance.achievementData.wonIECAdvancedSuperCapacitatorSupplier = true;
							break;
						}
					}
					if (Game.instance.achievementData.wonIECAdvancedFlyWheelSupplier && Game.instance.achievementData.wonIECAdvancedBatterySupplier && Game.instance.achievementData.wonIECAdvancedSuperCapacitatorSupplier)
					{
						steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Achievement_3_6);
					}
				}
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Achievement_3_1);
			}
			this.OnPlayerWinsAnyChampionshipAchievements();
		}
	}

	public bool IsChallengeFinishedOnRaceEnd()
	{
		return Game.instance.challengeManager.NotifyChallengeManagerOfGameEventAndCheckCompletion(ChallengeManager.ChallengeManagerGameEvents.RaceEnd);
	}

	public bool IsChallengeFinishedOnSeasonEnd()
	{
		return this.HasSeasonEnded() && Game.instance.challengeManager.NotifyChallengeManagerOfGameEventAndCheckCompletion(ChallengeManager.ChallengeManagerGameEvents.SeasonEnd);
	}

	public bool IsChallengeFinishedOnWonTopTier()
	{
		bool flag = this.standings.GetDriverEntry(0).GetEntity<Driver>().IsPlayersDriver();
		bool flag2 = this.standings.GetTeamEntry(0).GetEntity<Team>().IsPlayersTeam();
		return this.championshipID == 0 && this.HasSeasonEnded() && (flag || flag2) && Game.instance.challengeManager.NotifyChallengeManagerOfGameEventAndCheckCompletion(ChallengeManager.ChallengeManagerGameEvents.WonTopTier);
	}

	private void OnPlayerWinsAnyChampionshipAchievements()
	{
		bool flag = this.standings.GetDriverEntry(0).GetEntity<Driver>().IsPlayersDriver();
		bool flag2 = this.standings.GetTeamEntry(0).GetEntity<Team>().IsPlayersTeam();
		SteamAchievementsManager steamAchievementsManager = App.instance.steamAchievementsManager;
		if (flag && flag2)
		{
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Both_Champ);
			if (Game.instance.player.team.name == "Chariot Motor Group")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Both_Champs_Together_Chariot);
			}
			if (Game.instance.player.team.isCreatedByPlayer)
			{
				switch (this.championshipID)
				{
				case 0:
					App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Both_WMC_Custom_Team);
					break;
				case 3:
					App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Both_IGTC_Custom_Team);
					break;
				}
			}
		}
		if (this.championshipID == 0)
		{
			if (Game.instance.player.team.name == "Krüger Motorsport")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_WMC_With_Kruger);
			}
			else if (Game.instance.player.team.name == "Silva Racing")
			{
				steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_WMC_With_Silva);
			}
		}
		if (Game.instance.achievementData.championships_won >= 7)
		{
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_7_Champs);
		}
		if (!Game.instance.achievementData.hasGotSponsorBonusesThisSeason)
		{
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Any_Champ_No_Sponsors);
		}
		if (!Game.instance.achievementData.hasUpgradedCarPartThisSeason)
		{
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Any_Champ_No_Part_Upgrades);
		}
		if (!Game.instance.achievementData.hasUpgradedHQThisSeason)
		{
			steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Win_Any_Champ_With_Min_HQ);
		}
	}

	public List<Car> GetOverralBestCarsOfChampionship()
	{
		if (this.mPreviousUpdateTime != Game.instance.time.now.Ticks || this.mBestCarsOfChampionship.Count == 0)
		{
			this.mPreviousUpdateTime = Game.instance.time.now.Ticks;
			this.mBestCarsOfChampionship.Clear();
			this.mAllCarsCache.Clear();
			for (int i = 0; i < this.standings.teamEntryCount; i++)
			{
				Team entity = this.standings.GetTeamEntry(i).GetEntity<Team>();
				this.mAllCarsCache.Add(entity.carManager.GetCar(0));
				this.mAllCarsCache.Add(entity.carManager.GetCar(1));
			}
			while (this.mAllCarsCache.Count > 0)
			{
				Car car = null;
				for (int j = 0; j < this.mAllCarsCache.Count; j++)
				{
					Car car2 = this.mAllCarsCache[j];
					if (car == null)
					{
						car = car2;
					}
					else
					{
						CarStats stats = car2.GetStats();
						if (stats.statsTotal > car.GetStats().statsTotal)
						{
							car = car2;
						}
					}
				}
				this.mBestCarsOfChampionship.Add(car);
				this.mAllCarsCache.Remove(car);
			}
		}
		return this.mBestCarsOfChampionship;
	}

	public Championship GetPreviousTierChampionship()
	{
		return Game.instance.championshipManager.GetChampionshipByID(this.championshipBelowID);
	}

	public Championship GetNextTierChampionship()
	{
		return Game.instance.championshipManager.GetChampionshipByID(this.championshipAboveID);
	}

	public bool IsDateInPreseason(DateTime inDateTime)
	{
		return inDateTime >= this.currentPreSeasonStartDate && inDateTime <= this.currentPreSeasonEndDate;
	}

	public string GetAcronym(bool getCachedVersion = false, string inLanguague = "")
	{
		if (!string.IsNullOrEmpty(this.mCustomAcronym) && this.mCustomAcronym != "0")
		{
			return this.mCustomAcronym;
		}
		if (GameUtility.IsInMainThread)
		{
			switch (this.championshipID)
			{
			case 0:
				this.mAcronymn = "PSG_10002255";
				break;
			case 1:
				this.mAcronymn = "PSG_10002260";
				break;
			case 2:
				this.mAcronymn = "PSG_10002259";
				break;
			case 3:
				this.mAcronymn = "PSG_10011489";
				break;
			case 4:
				this.mAcronymn = "PSG_10011491";
				break;
			case 5:
				this.mAcronymn = "PSG_10012664";
				break;
			case 6:
				this.mAcronymn = "PSG_10012666";
				break;
			}
			if (this.mAcronymn == string.Empty)
			{
				return "{Championship Acronym not set}";
			}
			if (inLanguague != string.Empty)
			{
				return Localisation.LocaliseID(this.mAcronymn, inLanguague, null, string.Empty);
			}
			this.mAcronymn = Localisation.LocaliseID(this.mAcronymn, null);
		}
		return this.mAcronymn;
	}

	public string GetChampionshipNamePSGID()
	{
		string result = string.Empty;
		switch (this.championshipID)
		{
		case 0:
			result = "PSG_10002261";
			break;
		case 1:
			result = "PSG_10002257";
			break;
		case 2:
			result = "PSG_10002258";
			break;
		case 3:
			result = "PSG_10011488";
			break;
		case 4:
			result = "PSG_10011490";
			break;
		case 5:
			result = "PSG_10012663";
			break;
		case 6:
			result = "PSG_10012665";
			break;
		}
		return result;
	}

	public string GetChampionshipName(bool getCachedVersion = false, string inLanguague = "")
	{
		if (!string.IsNullOrEmpty(this.mCustomChampionshipName) && this.mCustomChampionshipName != "0")
		{
			return this.mCustomChampionshipName;
		}
		if (GameUtility.IsInMainThread)
		{
			this.mName = this.GetChampionshipNamePSGID();
			if (this.mName == string.Empty)
			{
				return "{Championship Name not set}";
			}
			if (inLanguague != string.Empty)
			{
				return Localisation.LocaliseID(this.mName, inLanguague, null, string.Empty);
			}
			this.mName = Localisation.LocaliseID(this.mName, null);
		}
		return this.mName;
	}

	public string GetChampionshipDescription(string inLanguage = "")
	{
		if (!string.IsNullOrEmpty(this.mCustomDescription) && this.mCustomDescription != "0")
		{
			return this.mCustomDescription;
		}
		if (inLanguage != string.Empty)
		{
			return Localisation.LocaliseID(this.descriptionID, inLanguage, null, string.Empty);
		}
		return Localisation.LocaliseID(this.descriptionID, null);
	}

	public string GetTheChampionshipString(string inLanguage = "")
	{
		if (!string.IsNullOrEmpty(this.mCustomTheChampionship) && this.mCustomTheChampionship != "0")
		{
			return this.mCustomTheChampionship;
		}
		if (inLanguage != string.Empty)
		{
			return Localisation.LocaliseID(this.theChampionshipID, inLanguage, null, string.Empty);
		}
		return Localisation.LocaliseID(this.theChampionshipID, null);
	}

	public string GetTheChampionshipUppercaseString(string inLanguage = "")
	{
		if (!string.IsNullOrEmpty(this.mCustomTheChampionshipUppercase) && this.mCustomTheChampionshipUppercase != "0")
		{
			return this.mCustomTheChampionshipUppercase;
		}
		if (inLanguage != string.Empty)
		{
			return Localisation.LocaliseID(this.theChampionshipIDUpperCase, inLanguage, null, string.Empty);
		}
		return Localisation.LocaliseID(this.theChampionshipIDUpperCase, null);
	}

	public bool InPreseason()
	{
		return Game.instance.time.now >= this.currentPreSeasonStartDate && Game.instance.time.now <= this.currentPreSeasonEndDate;
	}

	public void SetTheChampionshipID(string inID)
	{
		this.theChampionshipID = inID;
	}

	public void SetTheChampionshipUpperCaseID(string inID)
	{
		this.theChampionshipIDUpperCase = inID;
	}

	public void SetChampionshipDescriptionID(string inID)
	{
		this.descriptionID = inID;
	}

	public bool IsConcurrentChampionship()
	{
		return this.series == Championship.Series.EnduranceSeries;
	}

	public bool IsMainConcurrentChampionship()
	{
		return this.series == Championship.Series.EnduranceSeries && this.championshipID == 5;
	}

	public Championship GetMainConcurrentChampionship()
	{
		Championship championship = this;
		while (championship.GetNextTierChampionship() != null)
		{
			championship = championship.GetNextTierChampionship();
		}
		return championship;
	}

	public List<Championship> GetConcurrentChampionships()
	{
		List<Championship> list = new List<Championship>();
		List<Championship> entityList = Game.instance.championshipManager.GetEntityList();
		for (int i = 0; i < entityList.Count; i++)
		{
			Championship championship = entityList[i];
			if (championship.series == this.series)
			{
				list.Add(championship);
			}
		}
		return list;
	}

	public string GetClassText()
	{
		if (this.IsMainConcurrentChampionship())
		{
			return "A";
		}
		return "B";
	}

	public bool isPlayerChampionship
	{
		get
		{
			return this == Game.instance.player.team.championship;
		}
	}

	public int eventNumber
	{
		get
		{
			return this.mEventNumber;
		}
	}

	public int eventNumberForUI
	{
		get
		{
			return this.mEventNumber + 1;
		}
	}

	public int eventsLeft
	{
		get
		{
			return this.calendar.Count - this.mEventNumber - 1;
		}
	}

	public int eventCount
	{
		get
		{
			return this.calendar.Count;
		}
	}

	public DateTime carDevelopmenEndDate
	{
		get
		{
			return this.currentPreSeasonEndDate;
		}
	}

	public ChampionshipPromotions championshipPromotions
	{
		get
		{
			return this.mChampionshipPromotions;
		}
	}

	public ChampionshipPromotionData InPromotedTeamFromLowerTier
	{
		get
		{
			return this.inPromotedTeamFromLowerTier;
		}
	}

	public ChampionshipPromotionData InRelegatedTeamFromHigherTier
	{
		get
		{
			return this.inRelegatedTeamFromHigherTier;
		}
	}

	public string customChampionshipName
	{
		set
		{
			this.mCustomChampionshipName = value;
		}
	}

	public string customAcronym
	{
		set
		{
			this.mCustomAcronym = value;
		}
	}

	public string customTheChampionship
	{
		set
		{
			this.mCustomTheChampionship = value;
		}
	}

	public string customTheChampionshipUppercase
	{
		set
		{
			this.mCustomTheChampionshipUppercase = value;
		}
	}

	public string customDescription
	{
		set
		{
			this.mCustomDescription = value;
		}
	}

	public bool IsBaseGameChampionship
	{
		get
		{
			return this.DlcID == 0;
		}
	}

	public const float teamPromotionAcceptChance = 0.85f;

	public const float teamChampionMarketabilityReward = 25f;

	public const int invalidChampionshipID = -1;

	public int championshipID = -1;

	public float championshipOrderRelative;

	public bool allowPromotions = true;

	public int championshipOrder;

	public int championshipAboveID = -1;

	public int championshipBelowID = -1;

	public int DlcID;

	public int logoID;

	public Color uiColor = default(Color);

	public bool isChoosable;

	public bool isChoosableCreateTeam;

	public bool isBlockedByChallenge;

	public DateTime currentSeasonEndDate = DateTime.Today;

	public DateTime currentPreSeasonEndDate = DateTime.Today;

	public DateTime currentPreSeasonStartDate = DateTime.Today;

	public DateTime seasonEndDate = DateTime.Today;

	public DateTime preSeasonEndDate = DateTime.Today;

	public DateTime preSeasonStartDate = DateTime.Today;

	public List<RaceEventCalendarData> calendarData = new List<RaceEventCalendarData>();

	public List<RaceEventDetails> calendar = new List<RaceEventDetails>();

	public List<RaceEventDetails> nextYearsCalendar = new List<RaceEventDetails>();

	public int seasonStart;

	public int seasonEnd;

	public ChampionshipRecords records = new ChampionshipRecords();

	public ChampionshipStandings standings = new ChampionshipStandings();

	public ChampionshipStandingsHistory standingsHistory = new ChampionshipStandingsHistory();

	public PreSeasonTesting preSeasonTesting;

	public PoliticalSystem politicalSystem = new PoliticalSystem();

	public ChampionshipRules rules = new ChampionshipRules();

	public ChampionshipRules nextYearsRules = new ChampionshipRules();

	public int prizeFund;

	public int tvAudience;

	public int historySeed;

	public int historyMinStartAge = 19;

	public int historyMaxStartAge = 25;

	public float historyVariance = 0.35f;

	public float historyDNFChance = 0.03f;

	public int historyYears = 30;

	public string modelID = string.Empty;

	public int weightKG = 100;

	public int topSpeedMPH = 200;

	public string accelerationID = string.Empty;

	public string partManufacturingID = string.Empty;

	public int qualityTeamAverage;

	public int qualityCars;

	public int qualityDrivers;

	public int qualityHQ;

	public int qualityStaff;

	public int qualityFinances;

	public int eventLocations;

	public bool readyForPromotions;

	public bool completedPromotions;

	public Championship.Series series;

	public SeasonDirector seasonDirector = new SeasonDirector();

	private string mName = string.Empty;

	private string mAcronymn = string.Empty;

	private int mEventNumber;

	private string descriptionID = string.Empty;

	private string theChampionshipID = string.Empty;

	private string theChampionshipIDUpperCase = string.Empty;

	private string mCustomChampionshipName = string.Empty;

	private string mCustomAcronym = string.Empty;

	private string mCustomTheChampionship = string.Empty;

	private string mCustomTheChampionshipUppercase = string.Empty;

	private string mCustomDescription = string.Empty;

	private ChampionshipPromotions mChampionshipPromotions = new ChampionshipPromotions();

	private ChampionshipPromotionData inPromotedTeamFromLowerTier;

	private ChampionshipPromotionData inRelegatedTeamFromHigherTier;

	private List<Car> mBestCarsOfChampionship = new List<Car>();

	private List<Car> mAllCarsCache = new List<Car>();

	private long mPreviousUpdateTime;

	public enum Series
	{
		[LocalisationID("PSG_10011514")]
		SingleSeaterSeries,
		[LocalisationID("PSG_10011515")]
		GTSeries,
		[LocalisationID("PSG_10012728")]
		EnduranceSeries,
		Count
	}
}
