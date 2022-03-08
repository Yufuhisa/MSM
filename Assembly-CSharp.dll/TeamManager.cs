using System;
using System.Collections.Generic;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class TeamManager : GenericManager<Team>
{
	public TeamManager()
	{
	}

	public override void OnStart(Database database)
	{
		base.OnStart(database);
		this.AddTeamsFromDatabase(database);
	}

	public override void OnStartingGame()
	{
		this.AddActions();
	}

	public void OnLoad()
	{
		this.AddActions();
		this.ValidateTeamData();
	}

	public void UpdateSponsorObjectives(Championship inChampionship)
	{
		for (int i = 0; i < base.count; i++)
		{
			Team entity = base.GetEntity(i);
			if (entity.championship == inChampionship)
			{
				entity.sponsorController.CreateNewSessionObjectives();
			}
		}
	}

	private void AddTeamsFromDatabase(Database database)
	{
		EntityManager entityManager = Game.instance.entityManager;
		List<DatabaseEntry> teamData = database.teamData;
		int index;
		for (index = 0; index < teamData.Count; index++)
		{
			DatabaseEntry databaseEntry = teamData[index];
			Team team = entityManager.CreateEntity<Team>();
			base.AddEntity(team);
			team.teamID = index;
			team.name = databaseEntry.GetStringValue("Name");
			team.SetShortName(databaseEntry.GetStringValue("Short Name"));
			team.SetTeamStartDescriptionID(databaseEntry.GetStringValue("Team Start Description"));
			team.locationID = databaseEntry.GetStringValue("Location ID");
			team.pressure = databaseEntry.GetIntValue("Pressure");
			team.aggression = databaseEntry.GetFloatValue("Aggression");
			team.marketability = (float)databaseEntry.GetIntValue("Marketability") / 5f;
			team.fanBase = databaseEntry.GetFloatValue("Fan Base");
			team.colorID = databaseEntry.GetIntValue("Color");
			team.liveryID = databaseEntry.GetIntValue("Livery");
			team.driversHatStyle = databaseEntry.GetIntValue("Driver HatStyle");
			team.driversBodyStyle = databaseEntry.GetIntValue("Driver BodyStyle");
			team.nationality = Nationality.GetNationalityByName(databaseEntry.GetStringValue("Location"));
			team.customStartDescription = databaseEntry.GetStringValue("Custom Start Description");
			// custom aggressiveness cause there seems to be an error in reading the right teamAIWeightings 
			team.customAggressiveness = databaseEntry.GetFloatValue("Custom Aggressiveness");
			int num = 1000000;
			team.financeController.racePayment = (long)(databaseEntry.GetFloatValue("Race Payment Offset") * (float)num);
			team.financeController.racePaymentOffset = team.financeController.racePayment;
			team.financeController.availableFunds = (long)(databaseEntry.GetFloatValue("Budget") * (float)num);

			DatabaseEntry databaseEntry2 = database.teamAIWeightings.Find((DatabaseEntry x) => x.GetIntValue("Team Index") == index);

			if (databaseEntry2 == null)
			{
				Debug.LogErrorFormat("Missing AIWeightingsEntry for team ID: {0}", new object[]
				{
					index
				});
				databaseEntry2 = database.teamAIWeightings[0];
			}
			team.aiWeightings = new TeamAIWeightings(databaseEntry2);
			
			if (index <= 19) {
				global::Debug.LogErrorFormat("LoadDatabaseCeck: Team {0} with index {1} and mAggressiveness {2} and customAggressiveness {3}", new object[] {
					team.name,
					index,
					team.aiWeightings.mAggressiveness,
					team.customAggressiveness
				});
			}

			team.PostInitialise();
			team.votingCharacteristics = this.GetCharacteristics(databaseEntry.GetStringValue("Political Attributes"));
		}
		for (int i = 0; i < teamData.Count; i++)
		{
			Team entity = base.GetEntity(i);
			DatabaseEntry databaseEntry3 = teamData[i];
			int inIndex = databaseEntry3.GetIntValue("Rival") - 2;
			entity.rivalTeam = base.GetEntity(inIndex);
			entity.startOfSeasonExpectedChampionshipResult = this.CalculateExpectedPositionForChampionship(entity);
		}
	}

	public List<PoliticalVote.TeamCharacteristics> GetCharacteristics(string inString)
	{
		List<PoliticalVote.TeamCharacteristics> list = new List<PoliticalVote.TeamCharacteristics>();
		string[] array = inString.Split(new char[]
		{
			';'
		});
		foreach (string text in array)
		{
			string text2 = text.Trim();
			PoliticalVote.TeamCharacteristics teamCharacteristics = (PoliticalVote.TeamCharacteristics)((int)Enum.Parse(typeof(PoliticalVote.TeamCharacteristics), text2));
			list.Add(teamCharacteristics);
		}
		return list;
	}

	public int CalculateExpectedPositionForChampionship(Team inTeam)
	{
		if (!inTeam.IsInAChampionship())
		{
			return base.GetEntityList().Count;
		}
		float teamExpectation = inTeam.GetChampionshipExpectation();
		List<float> list = new List<float>();
		list.Add(teamExpectation);
		foreach (Team team in base.GetEntityList())
		{
			if (team.championship == inTeam.championship && team != inTeam)
			{
				list.Add(team.GetChampionshipExpectation());
			}
		}
		list.Sort((float x, float y) => y.CompareTo(x));
		return list.FindIndex((float x) => x.Equals(teamExpectation)) + 1;
	}

	public void CalculateDriverExpectedPositionsInChampionship(Championship inChampionship)
	{
		if (inChampionship.GetCurrentEventDetails() != null)
		{
			CarStats trackStatsCharacteristics = inChampionship.GetCurrentEventDetails().circuit.trackStatsCharacteristics;
			List<Driver> list = new List<Driver>();
			foreach (Team team in base.GetEntityList())
			{
				if (team.championship == inChampionship)
				{
					switch (inChampionship.series)
					{
					case Championship.Series.SingleSeaterSeries:
					case Championship.Series.GTSeries:
						// use real drivers for car (including check for reserve driver)
						list.Add(team.GetDriverForCar(0));
						list.Add(team.GetDriverForCar(1));
						break;
					case Championship.Series.EnduranceSeries:
						list.AddRange(team.GetDrivers());
						break;
					}
				}
			}
			DatabaseEntry inWeightings = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Championship");
			DatabaseEntry inWeightings2 = App.instance.database.personExpectationWeightings.Find((DatabaseEntry curEntry) => curEntry.GetStringValue("Type") == "Race");
			foreach (Driver driver in list)
			{
				driver.championshipExpectation = driver.GetExpectation(inWeightings);
				driver.raceExpectation = driver.GetRaceExpectation(inWeightings2, trackStatsCharacteristics);
			}
			list.Sort((Driver x, Driver y) => y.raceExpectation.CompareTo(x.raceExpectation));
			int num = 0;
			List<Driver> list2 = new List<Driver>();
			for (int i = 0; i < list.Count; i++)
			{
				if (inChampionship.series == Championship.Series.EnduranceSeries)
				{
					Team team2 = list[i].contract.GetTeam();
					if (!list2.Contains(list[i]))
					{
						Driver[] driversForCar = team2.GetDriversForCar(list[i].carID);
						for (int j = 0; j < driversForCar.Length; j++)
						{
							driversForCar[j].expectedRacePosition = num + 1;
							list2.Add(driversForCar[j]);
						}
						num++;
					}
				}
				else
				{
					list[i].expectedRacePosition = i + 1;
				}
			}
			num = 0;
			list2.Clear();
			list.Sort((Driver x, Driver y) => y.championshipExpectation.CompareTo(x.championshipExpectation));
			for (int k = 0; k < list.Count; k++)
			{
				if (inChampionship.series == Championship.Series.EnduranceSeries)
				{
					Team team3 = list[k].contract.GetTeam();
					if (!list2.Contains(list[k]))
					{
						Driver[] driversForCar2 = team3.GetDriversForCar(list[k].carID);
						for (int l = 0; l < driversForCar2.Length; l++)
						{
							driversForCar2[l].expectedChampionshipPosition = num + 1;
							if (!inChampionship.GetFirstEventDetails().hasEventEnded)
							{
								driversForCar2[l].startOfSeasonExpectedChampionshipPosition = num + 1;
							}
							list2.Add(driversForCar2[l]);
						}
						num++;
					}
				}
				else
				{
					list[k].expectedChampionshipPosition = k + 1;
					if (!inChampionship.GetFirstEventDetails().hasEventEnded)
					{
						list[k].startOfSeasonExpectedChampionshipPosition = k + 1;
					}
				}
			}
		}
	}

	public int CalculateExpectedPositionForRace(Team inTeam, Circuit inCircuit)
	{
		if (!inTeam.IsInAChampionship())
		{
			return base.GetEntityList().Count;
		}
		float teamExpectation = inTeam.GetRaceExpectation(inCircuit);
		List<float> list = new List<float>();
		list.Add(teamExpectation);
		foreach (Team team in base.GetEntityList())
		{
			if (team.championship == inTeam.championship && team != inTeam)
			{
				list.Add(team.GetRaceExpectation(inCircuit));
			}
		}
		list.Sort((float x, float y) => y.CompareTo(x));
		return list.FindIndex((float x) => x.Equals(teamExpectation)) + 1;
	}

	public static Team GetTeamWithUnderPressureManager(Championship inChampionship)
	{
		List<Team> list = new List<Team>();
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			if (entity.contractManager.GetPersonOnJob<TeamPrincipal>(Contract.Job.TeamPrincipal).GetJobSecurity() == TeamPrincipal.JobSecurity.Risk)
			{
				list.Add(entity);
			}
		}
		if (list.Count > 0)
		{
			return list[RandomUtility.GetRandom(0, list.Count)];
		}
		return null;
	}

	public static Team GetTeamOfTheSeason(Championship inChampionship)
	{
		int num = 0;
		Team team = null;
		int teamEntryCount = inChampionship.standings.teamEntryCount;
		for (int i = 0; i < teamEntryCount; i++)
		{
			ChampionshipEntry_v1 teamEntry = inChampionship.standings.GetTeamEntry(i);
			Team entity = teamEntry.GetEntity<Team>();
			int num2 = entity.startOfSeasonExpectedChampionshipResult - teamEntry.GetCurrentChampionshipPosition();
			if (team == null || (teamEntry.GetCurrentPoints() > 0 && num2 > num))
			{
				team = entity;
				num = num2;
			}
		}
		return team;
	}

	public static Team GetTeamWithBestExpectedRaceResult(Championship inChampionship, bool includeTrackNotSuitedTeam)
	{
		Team team = null;
		Team team2 = (!includeTrackNotSuitedTeam) ? TeamManager.GetTeamThatDoesntSuitTrack(Game.instance.player.team.championship, Game.instance.player.team.championship.GetCurrentEventDetails().circuit, true) : null;
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			if (entity != team2)
			{
				if (team == null || team.expectedRacePosition > entity.expectedRacePosition)
				{
					team = entity;
				}
			}
		}
		return team;
	}

	public static Team GetTeamWithExpectedRaceResult(int inExpectation, Championship inChampionship)
	{
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			if (entity.expectedRacePosition == inExpectation)
			{
				return entity;
			}
		}
		Debug.LogErrorFormat("No Team with expected race result of {0}", new object[]
		{
			inExpectation
		});
		return null;
	}

	public static Team GetTeamWithWorstPartOfType(CarPart.PartType inType, Championship inChampionship)
	{
		return TeamManager.GetTeamOfPartQuality(inType, inChampionship, false);
	}

	public static Team GetTeamWithBestPartOfType(CarPart.PartType inType, Championship inChampionship)
	{
		return TeamManager.GetTeamOfPartQuality(inType, inChampionship, true);
	}

	private static Team GetTeamOfPartQuality(CarPart.PartType inType, Championship inChampionship, bool inChooseBestPart)
	{
		Team result = null;
		CarPart carPart = null;
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			List<CarPart> partsInCarsOfType = entity.carManager.GetPartsInCarsOfType(inType);
			for (int j = 0; j < partsInCarsOfType.Count; j++)
			{
				CarPart carPart2 = partsInCarsOfType[j];
				if (carPart == null || (inChooseBestPart && carPart.stats.statWithPerformance < carPart2.stats.statWithPerformance) || (!inChooseBestPart && carPart.stats.statWithPerformance > carPart2.stats.statWithPerformance))
				{
					carPart = carPart2;
					result = entity;
				}
			}
		}
		return result;
	}

	private static List<Team> GetTeamsForChampionship(Championship inChampionship)
	{
		List<Team> list = new List<Team>();
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			list.Add(entity);
		}
		return list;
	}

	public static int GetTeamDriversRank(Team inTeam)
	{
		List<Team> teamsForChampionship = TeamManager.GetTeamsForChampionship(inTeam.championship);
		teamsForChampionship.Sort((Team teamOne, Team teamTwo) => teamTwo.GetDriverQuality().CompareTo(teamOne.GetDriverQuality()));
		return teamsForChampionship.IndexOf(inTeam);
	}

	public static int GetTeamRankOnTyreWear(Team inTeam)
	{
		List<Team> teamsForChampionship = TeamManager.GetTeamsForChampionship(inTeam.championship);
		teamsForChampionship.Sort((Team teamOne, Team teamTwo) => teamTwo.carManager.GetCar(0).ChassisStats.tyreWear.CompareTo(teamOne.carManager.GetCar(0).ChassisStats.tyreWear));
		return teamsForChampionship.IndexOf(inTeam);
	}

	public static int GetTeamRankOnFuelEfficiency(Team inTeam)
	{
		List<Team> teamsForChampionship = TeamManager.GetTeamsForChampionship(inTeam.championship);
		teamsForChampionship.Sort((Team teamOne, Team teamTwo) => teamTwo.carManager.GetCar(0).ChassisStats.fuelEfficiency.CompareTo(teamOne.carManager.GetCar(0).ChassisStats.fuelEfficiency));
		return teamsForChampionship.IndexOf(inTeam);
	}

	public static int GetTeamsFinancialWorthRanking(Team inTeam)
	{
		List<Team> teamsForChampionship = TeamManager.GetTeamsForChampionship(inTeam.championship);
		teamsForChampionship.Sort((Team teamOne, Team teamTwo) => teamTwo.financeController.worth.CompareTo(teamOne.financeController.worth));
		return teamsForChampionship.IndexOf(inTeam);
	}

	public static Team GetTeamThatSuitsTrack(Championship inChampionship, Circuit inCircuit)
	{
		return TeamManager.GetTeamThatSuitsTrack(inChampionship, inCircuit, true, false);
	}

	public static Team GetTeamThatDoesntSuitTrack(Championship inChampionship, Circuit inCircuit, bool ignoreFavourite = false)
	{
		return TeamManager.GetTeamThatSuitsTrack(inChampionship, inCircuit, false, ignoreFavourite);
	}

	public static Team GetTeamThatSuitsTrack(Championship inChampionship, Circuit inCircuit, bool inChooseBest, bool ignoreFavourite = false)
	{
		Team team = (!ignoreFavourite) ? null : TeamManager.GetTeamWithBestExpectedRaceResult(inChampionship, true);
		Team result = null;
		int num = (!inChooseBest) ? 6 : 0;
		for (int i = 0; i < inChampionship.standings.teamEntryCount; i++)
		{
			Team entity = inChampionship.standings.GetTeamEntry(i).GetEntity<Team>();
			if (team != entity)
			{
				int num2 = 0;
				foreach (CarPart.PartType partType2 in CarPart.GetPartType(inChampionship.series, false))
				{
					if (inCircuit.GetRelevancy(CarPart.GetStatForPartType(partType2)) >= CarStats.RelevantToCircuit.VeryUseful)
					{
						if (inChooseBest && entity == TeamManager.GetTeamWithBestPartOfType(partType2, inChampionship))
						{
							num2++;
						}
						else if (!inChooseBest && entity == TeamManager.GetTeamWithWorstPartOfType(partType2, inChampionship))
						{
							num2--;
						}
					}
				}
				if ((inChooseBest && num <= num2) || (!inChooseBest && num >= num2))
				{
					result = entity;
					num = num2;
				}
			}
		}
		return result;
	}

	public static int GetTeamRankForTrackStats(Team inTeam, Circuit inCircuit)
	{
		Championship championship = inTeam.championship;
		List<Team> list = new List<Team>();
		for (int i = 0; i < championship.standings.teamEntryCount; i++)
		{
			Team entity = championship.standings.GetTeamEntry(i).GetEntity<Team>();
			list.Add(entity);
		}
		list.Sort((Team teamOne, Team teamTwo) => teamTwo.GetRelevantStatsSumForCircuit(inCircuit).CompareTo(teamOne.GetRelevantStatsSumForCircuit(inCircuit)));
		return list.IndexOf(inTeam);
	}

	public void GenerateInitialAIPitCrewStats()
	{
		List<Team> entityList = base.GetEntityList();
		for (int i = 0; i < entityList.Count; i++)
		{
			if (entityList[i].HasAIPitcrew)
			{
				entityList[i].pitCrewController.AIPitCrew.RegenerateTaskStats();
			}
		}
	}

	public void OnDayEnd()
	{
		bool flag = !Game.instance.player.IsUnemployed();
		if (flag)
		{
			this.CalculateDriverExpectedPositionsInChampionship(Game.instance.player.team.championship);
		}
		List<Championship> entityList = Game.instance.championshipManager.GetEntityList();
		for (int i = 0; i < entityList.Count; i++)
		{
			ChampionshipStandings standings;
			if (entityList[i].InPreseason())
			{
				standings = entityList[i].standingsHistory.GetEntryYear((Game.instance.time.now.Month >= 6) ? Game.instance.time.now.Year : (Game.instance.time.now.Year - 1)).standings;
			}
			else
			{
				standings = entityList[i].standings;
			}
			int teamEntryCount = standings.teamEntryCount;
			for (int j = 0; j < teamEntryCount; j++)
			{
				Team entity = standings.GetTeamEntry(j).GetEntity<Team>();
				entity.OnDayEnd(this.teamRumourManager);
			}
		}
		if (flag)
		{
			this.teamRumourManager.HandleRumours();
		}
	}

	public void AddActions()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Combine(time.OnDayEnd, new Action(this.OnDayEnd));
	}

	public void RemoveActions()
	{
		GameTimer time = Game.instance.time;
		time.OnDayEnd = (Action)Delegate.Remove(time.OnDayEnd, new Action(this.OnDayEnd));
	}

	private void ValidateTeamData()
	{
		List<DatabaseEntry> teamData = App.instance.database.teamData;
		for (int i = 0; i < teamData.Count; i++)
		{
			if (i >= base.count)
			{
				break;
			}
			DatabaseEntry databaseEntry = teamData[i];
			Team entity = base.GetEntity(i);
			int intValue = databaseEntry.GetIntValue("Driver BodyStyle");
			if (!Game.instance.challengeManager.IsAttemptingChallenge() && entity.GetShortName(true) == string.Empty && !entity.isCreatedByPlayer)
			{
				entity.SetShortName(databaseEntry.GetStringValue("Short Name"));
			}
			if (!entity.isCreatedByPlayer && entity.driversBodyStyle != intValue)
			{
				entity.driversBodyStyle = intValue;
			}
		}
	}

	public NullTeam nullTeam = new NullTeam();

	public TeamRumourManager teamRumourManager = new TeamRumourManager();
}
