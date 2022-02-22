using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class ChampionshipManager : GenericManager<Championship>
{
	public ChampionshipManager()
	{
	}

	public override void OnStart(Database database)
	{
		base.OnStart(database);
		EntityManager entityManager = Game.instance.entityManager;
		List<DatabaseEntry> championshipData = database.championshipData;
		List<DatabaseEntry> politicsPresidentsData = database.politicsPresidentsData;
		Person person = null;
		for (int i = 0; i < championshipData.Count; i++)
		{
			Championship championship = entityManager.CreateEntity<Championship>();
			DatabaseEntry databaseEntry = championshipData[i];
			championship.series = (Championship.Series)((int)Enum.Parse(typeof(Championship.Series), databaseEntry.GetStringValue("Series")));
			championship.championshipID = databaseEntry.GetIntValue("ID");
			championship.DlcID = databaseEntry.GetIntValue("DLC ID");
			championship.championshipOrderRelative = databaseEntry.GetFloatValue("Championship Order");
			championship.allowPromotions = bool.Parse(databaseEntry.GetStringValue("Allow Promotions"));
			championship.logoID = databaseEntry.GetIntValue("Logo");
			championship.isChoosable = (databaseEntry.GetIntValue("Is Choosable") != 0);
			championship.isChoosableCreateTeam = (databaseEntry.GetIntValue("Is Choosable in Create Team Mode") != 0);
			championship.SetTheChampionshipID(databaseEntry.GetStringValue("TheChampionship"));
			championship.SetTheChampionshipUpperCaseID(databaseEntry.GetStringValue("TheChampionshipUppercase"));
			championship.seasonStart = databaseEntry.GetIntValue("Season Start");
			championship.seasonEnd = GameStatsConstants.lastChampionshipEventWeek;
			championship.prizeFund = databaseEntry.GetIntValue("Prize fund") * 1000000;
			championship.tvAudience = databaseEntry.GetIntValue("TV audience") * 1000000;
			championship.uiColor = GameUtility.HexStringToColour(databaseEntry.GetStringValue("Color"));
			championship.SetChampionshipDescriptionID(databaseEntry.GetStringValue("Description"));
			championship.modelID = databaseEntry.GetStringValue("Model");
			championship.weightKG = databaseEntry.GetIntValue("Weight");
			championship.topSpeedMPH = databaseEntry.GetIntValue("Top Speed");
			championship.accelerationID = databaseEntry.GetStringValue("0-60");
			championship.partManufacturingID = databaseEntry.GetStringValue("Part Manufacturing");
			championship.qualityTeamAverage = databaseEntry.GetIntValue("Quality Team Average");
			championship.qualityCars = databaseEntry.GetIntValue("Quality Car");
			championship.qualityDrivers = databaseEntry.GetIntValue("Quality Drivers");
			championship.qualityHQ = databaseEntry.GetIntValue("Quality HQ");
			championship.qualityStaff = databaseEntry.GetIntValue("Quality Staff");
			championship.qualityFinances = databaseEntry.GetIntValue("Quality Finances");
			championship.customChampionshipName = databaseEntry.GetStringValue("Custom Championship Name");
			championship.customAcronym = databaseEntry.GetStringValue("Custom Acronym");
			championship.customTheChampionship = databaseEntry.GetStringValue("Custom TheChampionship");
			championship.customTheChampionshipUppercase = databaseEntry.GetStringValue("Custom TheChampionshipUppercase");
			championship.customDescription = databaseEntry.GetStringValue("Custom Description");
			foreach (CarPart.PartType key in CarPart.GetPartType(championship.series, false))
			{
				PartTypeSlotSettings partTypeSlotSettings;
				if (Game.instance.partSettingsManager.championshipPartSettings.ContainsKey(championship.championshipID))
				{
					partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[championship.championshipID][key];
				}
				else
				{
					Dictionary<CarPart.PartType, PartTypeSlotSettings> value = Game.instance.partSettingsManager.championshipPartSettings[championship.championshipID - 1];
					Game.instance.partSettingsManager.championshipPartSettings[championship.championshipID] = value;
					partTypeSlotSettings = Game.instance.partSettingsManager.championshipPartSettings[championship.championshipID][key];
					global::Debug.LogError(string.Format("Part database does not contain data for championship id:{0}", championship.championshipID), null);
				}
				championship.rules.partStatSeasonMinValue.Add(key, partTypeSlotSettings.baseMinStat);
				championship.rules.partStatSeasonMaxValue.Add(key, partTypeSlotSettings.baseMaxStat);
			}
			this.AddRules(databaseEntry, championship, "Rules");
			string[] array = databaseEntry.GetStringValue("Restricted Rules").Split(new char[]
			{
				';'
			});
			championship.PoliticsOnStart();
			if (array.Length != 0 && array[0] != string.Empty)
			{
				championship.rules.SetRestrictedRules(array);
			}
			championship.rules.weightStrippingRatio = databaseEntry.GetFloatValue("WeightStripping Ratio");
			if (person == null)
			{
				DatabaseEntry databaseEntry2 = politicsPresidentsData[i];
				person = Game.instance.entityManager.CreateEntity<Person>();
				person.SetName(databaseEntry2.GetStringValue("First Name"), databaseEntry2.GetStringValue("Last Name"));
				person.contract.job = Contract.Job.IMAPresident;
				if (databaseEntry2.GetStringValue("Gender") == "M")
				{
					person.gender = Person.Gender.Male;
				}
				else
				{
					person.gender = Person.Gender.Female;
				}
			}
			championship.politicalSystem.president = person;
			championship.rules.ActivateRulesThatAffectCalendar();
			this.PopulateCalendar(championship, databaseEntry);
			this.AddTeamsToChampionship(championship, databaseEntry);
			this.SetupHistoryGenerationData(championship, databaseEntry);
			championship.rules.ActivateRules();
			championship.rules.ValidateChampionshipRules();
			championship.nextYearsRules = championship.rules.Clone();
			championship.politicalSystem.GenerateCalendarEvents();
			championship.SetCurrentSeasonDates();
			championship.SetupCalendarWeather();
			championship.SetupNextYearCalendarWeather();
			championship.seasonDirector.OnSeasonStart(championship);
			base.AddEntity(championship);
		}
		this.SetChampionshipOrder(ref this.championshipsOrdered, Championship.Series.SingleSeaterSeries);
		this.SetChampionshipOrder(ref this.championshipsOrderedDLC, Championship.Series.GTSeries);
		this.SetChampionshipOrder(ref this.championshipsOrderedEndurance, Championship.Series.EnduranceSeries);
		Game.instance.teamManager.nullTeam.championship = this.nullChampionship;
	}

	private void AddRules(DatabaseEntry inData, Championship inChampionship, string inRuleColumnName)
	{
		string[] array = inData.GetStringValue(inRuleColumnName).Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			int num = int.Parse(text);
			if (num != 0)
			{
				if (!App.instance.votesManager.votes.ContainsKey(num))
				{
					global::Debug.LogWarningFormat("Championship {0} references invalid rule {1}; ignoring rule", new object[]
					{
						inChampionship.championshipID,
						text
					});
				}
				else
				{
					PoliticalVote politicalVote = App.instance.votesManager.votes[num].Clone();
					politicalVote.Initialize(inChampionship);
					inChampionship.rules.AddRule(politicalVote);
				}
			}
		}
	}

	private void SetChampionshipOrder(ref List<Championship> inList, Championship.Series inSeries)
	{
		inList = new List<Championship>(base.GetEntityList());
		inList.RemoveAll((Championship x) => !x.isChoosable || x.series != inSeries || !x.allowPromotions);
		inList.Sort((Championship x, Championship y) => x.championshipOrderRelative.CompareTo(y.championshipOrderRelative));
		for (int i = 0; i < inList.Count; i++)
		{
			bool flag = i > 0;
			bool flag2 = i < inList.Count - 1;
			inList[i].championshipAboveID = ((!flag) ? -1 : inList[i - 1].championshipID);
			inList[i].championshipBelowID = ((!flag2) ? -1 : inList[i + 1].championshipID);
			inList[i].championshipOrder = i;
		}
	}

	public override void OnStartingGame()
	{
		this.AddActions();
	}

	public void OnLoad()
	{
		this.AddActions();
		List<DatabaseEntry> championshipData = App.instance.database.championshipData;
		for (int i = 0; i < championshipData.Count; i++)
		{
			DatabaseEntry databaseEntry = championshipData[i];
			Championship championshipByID = Game.instance.championshipManager.GetChampionshipByID(databaseEntry.GetIntValue("ID"));
			if (championshipByID != null)
			{
				if (championshipByID.rules.restrictedRuleIDS.Count == 0)
				{
					string[] array = databaseEntry.GetStringValue("Restricted Rules").Split(new char[]
					{
						';'
					});
					if (array.Length != 0 && array[0] != string.Empty)
					{
						championshipByID.rules.SetRestrictedRules(array);
						championshipByID.nextYearsRules.SetRestrictedRules(array);
					}
				}
				if (championshipByID.rules.weightStrippingRatio == 0f)
				{
					championshipByID.rules.weightStrippingRatio = databaseEntry.GetFloatValue("WeightStripping Ratio");
					championshipByID.nextYearsRules.weightStrippingRatio = championshipByID.rules.weightStrippingRatio;
					championshipByID.ResetWeightStrippingForTeams();
				}
			}
		}
	}

	private void PopulateCalendar(Championship inChampionship, DatabaseEntry inData)
	{
		string[] array = inData.GetStringValue("Locations").Split(new char[]
		{
			';'
		});
		if (array.Length == 0)
		{
			global::Debug.LogWarningFormat("Championship {0} has no circuits listed in its Locations; not adding any races", new object[]
			{
				inChampionship.championshipID
			});
			return;
		}
		List<string> list = new List<string>(array.Length);
		float num = (float)inChampionship.seasonStart;
		float num2 = 0f;
		if (array.Length > 1)
		{
			num2 = (float)(inChampionship.seasonEnd - inChampionship.seasonStart) / (float)(array.Length - 1);
		}
		for (int i = 0; i < array.Length; i++)
		{
			RaceEventCalendarData raceEventCalendarData = new RaceEventCalendarData();
			int num3 = int.Parse(array[i]);
			Circuit circuitByID = Game.instance.circuitManager.GetCircuitByID(num3);
			if (circuitByID == null)
			{
				global::Debug.LogWarningFormat("Championship {0} references invalid circuit {1}; not adding", new object[]
				{
					inChampionship.championshipID,
					num3
				});
			}
			else
			{
				if (!list.Contains(circuitByID.locationNameID))
				{
					list.Add(circuitByID.locationNameID);
				}
				raceEventCalendarData.circuit = circuitByID;
				if (i == array.Length - 1)
				{
					raceEventCalendarData.week = inChampionship.seasonEnd;
				}
				else
				{
					raceEventCalendarData.week = Mathf.RoundToInt(num);
				}
				inChampionship.calendarData.Add(raceEventCalendarData);
				num += num2;
			}
		}
		inChampionship.GenerateCalendar();
		inChampionship.GenerateNextYearCalendar(true);
		inChampionship.eventLocations = list.Count;
	}

	private void AddTeamsToChampionship(Championship inChampionship, DatabaseEntry inData)
	{
		TeamManager teamManager = Game.instance.teamManager;
		string[] array = inData.GetStringValue("Teams").Split(new char[]
		{
			';'
		});
		for (int i = 0; i < array.Length; i++)
		{
			int inIndex = int.Parse(array[i]) - 2;
			Team entity = teamManager.GetEntity(inIndex);
			entity.SetChampionship(inChampionship);
			inChampionship.AddTeam(entity);
		}
	}

	private void SetupHistoryGenerationData(Championship inChampionship, DatabaseEntry inData)
	{
		inChampionship.historySeed = inData.GetIntValue("History Seed");
		inChampionship.historyMinStartAge = inData.GetIntValue("History Min Start Age");
		inChampionship.historyMaxStartAge = inData.GetIntValue("History Max Start Age");
		inChampionship.historyVariance = (float)inData.GetIntValue("History Variance") / 100f;
		inChampionship.historyDNFChance = (float)inData.GetIntValue("History DNF Chance") / 100f;
		inChampionship.historyYears = inData.GetIntValue("History Years");
	}

	public void GenerateInitialCareerHistory()
	{
		for (int i = 0; i < base.count; i++)
		{
			base.GetEntity(i).GenerateInitialChampionshipHistory();
		}
	}

	public void FinishPromotions()
	{
		for (int i = 0; i < base.count; i++)
		{
			if (!base.GetEntity(i).readyForPromotions)
			{
				return;
			}
		}
		for (int j = 0; j < base.count; j++)
		{
			Championship entity = base.GetEntity(j);
			if (!entity.completedPromotions)
			{
				entity.ProcessChampionshipPromotions(true);
			}
			entity.readyForPromotions = false;
			entity.completedPromotions = false;
		}
		for (int k = 0; k < base.count; k++)
		{
			base.GetEntity(k).OnChampionshipPromotionsEnd();
		}
		for (int l = 0; l < base.count; l++)
		{
			Championship entity2 = base.GetEntity(l);
			entity2.rules.ClearTrackRules();
			entity2.nextYearsRules.ClearTrackRules();
		}
		if (!Game.instance.player.IsUnemployed())
		{
			Game.instance.dialogSystem.OnTeamsPromoted();
		}
	}

	public List<Championship> GetChampionshipsForSeries(Championship.Series inSeries)
	{
		List<Championship> list = new List<Championship>();
		for (int i = 0; i < base.count; i++)
		{
			Championship entity = base.GetEntity(i);
			if (entity.series == inSeries)
			{
				list.Add(entity);
			}
		}
		return list;
	}

	public Championship GetChampionshipByID(int championshipID)
	{
		for (int i = 0; i < base.count; i++)
		{
			Championship entity = base.GetEntity(i);
			if (entity.championshipID == championshipID)
			{
				return entity;
			}
		}
		return null;
	}

	public Championship GetMainChampionship(Championship.Series inSeries = Championship.Series.SingleSeaterSeries)
	{
		List<Championship> championshipsByOrder = this.GetChampionshipsByOrder(inSeries);
		if (championshipsByOrder != null)
		{
			return championshipsByOrder[0];
		}
		return null;
	}

	public Championship[] GetChampionshipsRacingToday(bool inCheckEvent = false, Championship.Series inSeries = Championship.Series.Count)
	{
		this.mReturnChampionships.Clear();
		int count = base.count;
		DateTime date = Game.instance.time.now.Date.AddDays(-5.0);
		for (int i = 0; i < count; i++)
		{
			Championship entity = base.GetEntity(i);
			if (!inCheckEvent || !entity.HasSeasonEnded())
			{
				RaceEventDetails eventDetailsForNextEventAfterDate = entity.GetEventDetailsForNextEventAfterDate(date);
				if (eventDetailsForNextEventAfterDate != null)
				{
					SessionDetails sessionDetails = eventDetailsForNextEventAfterDate.GetNextRaceActiveSession();
					if (sessionDetails == null)
					{
						sessionDetails = eventDetailsForNextEventAfterDate.raceSessions[0];
					}
					DateTime date2 = sessionDetails.sessionDateTime.Date;
					bool flag = inSeries == Championship.Series.Count || inSeries == entity.series;
					if ((!inCheckEvent || (inCheckEvent && !eventDetailsForNextEventAfterDate.hasEventEnded && !sessionDetails.hasEnded)) && date2 == Game.instance.time.now.Date && flag)
					{
						this.mReturnChampionships.Add(entity);
					}
				}
			}
		}
		return this.mReturnChampionships.ToArray();
	}

	public Championship GetRandomChampionship(Championship.Series inSeries = Championship.Series.SingleSeaterSeries)
	{
		if (inSeries == Championship.Series.GTSeries)
		{
			return this.championshipsOrderedDLC[RandomUtility.GetRandom(0, this.championshipsOrdered.Count)];
		}
		if (inSeries != Championship.Series.EnduranceSeries)
		{
			return this.championshipsOrdered[RandomUtility.GetRandom(0, this.championshipsOrdered.Count)];
		}
		return this.championshipsOrderedEndurance[RandomUtility.GetRandom(0, this.championshipsOrdered.Count)];
	}

	public void OnSeasonStart()
	{
		for (int i = 0; i < base.count; i++)
		{
			Championship entity = base.GetEntity(i);
			entity.OnSeasonStart();
			Game.instance.teamManager.CalculateDriverExpectedPositionsInChampionship(entity);
		}
	}

	public void UpdateAllStandings()
	{
		int count = base.count;
		for (int i = 0; i < count; i++)
		{
			base.GetEntity(i).standings.UpdateStandings();
		}
	}

	private void GenerateNewYearCareerEntries()
	{
		for (int i = 0; i < base.count; i++)
		{
			base.GetEntity(i).GenerateNewChampionshipHistory();
		}
	}

	private void CloseLastYearsCareerEntries()
	{
		for (int i = 0; i < base.count; i++)
		{
			base.GetEntity(i).FinishCareerHistorySeasonEntry();
		}
	}

	public void AddActions()
	{
		GameTimer time = Game.instance.time;
		time.OnYearEnd = (Action)Delegate.Combine(time.OnYearEnd, new Action(this.CloseLastYearsCareerEntries));
		GameTimer time2 = Game.instance.time;
		time2.OnYearEnd = (Action)Delegate.Combine(time2.OnYearEnd, new Action(this.GenerateNewYearCareerEntries));
	}

	public void RemoveActions()
	{
		GameTimer time = Game.instance.time;
		time.OnYearEnd = (Action)Delegate.Remove(time.OnYearEnd, new Action(this.CloseLastYearsCareerEntries));
		GameTimer time2 = Game.instance.time;
		time2.OnYearEnd = (Action)Delegate.Remove(time2.OnYearEnd, new Action(this.GenerateNewYearCareerEntries));
	}

	public List<Championship> GetChampionshipsByOrder(Championship.Series inSeries = Championship.Series.SingleSeaterSeries)
	{
		if (inSeries == Championship.Series.GTSeries)
		{
			return this.championshipsOrderedDLC;
		}
		if (inSeries != Championship.Series.EnduranceSeries)
		{
			return this.championshipsOrdered;
		}
		return this.championshipsOrderedEndurance;
	}

	public bool isGTSeriesActive
	{
		get
		{
			return this.championshipsOrderedDLC != null && this.championshipsOrderedDLC.Count > 0;
		}
	}

	public bool isEnduranceSeriesActive
	{
		get
		{
			return this.championshipsOrderedEndurance != null && this.championshipsOrderedEndurance.Count > 0;
		}
	}

	public NullChampionship nullChampionship = new NullChampionship();

	private List<Championship> championshipsOrdered;

	private List<Championship> championshipsOrderedDLC;

	private List<Championship> championshipsOrderedEndurance;

	private List<Championship> mReturnChampionships = new List<Championship>();
}
