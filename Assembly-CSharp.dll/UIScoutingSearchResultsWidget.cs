using System;
using System.Collections.Generic;
using UnityEngine;

public class UIScoutingSearchResultsWidget : MonoBehaviour
{
	public UIScoutingSearchResultsWidget()
	{
	}

	public void OnStart()
	{
		this.driveableSeries.OnStart();
		this.jobRole.OnStart();
		this.view.OnStart();
		this.age.OnStart();
		this.ability.OnStart();
		for (int i = 0; i < this.tableFilters.Length; i++)
		{
			UIScoutingFilter uiscoutingFilter = this.tableFilters[i];
			uiscoutingFilter.toggle.isOn = (uiscoutingFilter.filterType == this.mFilter);
			uiscoutingFilter.OnStart();
		}
		UILoadList uiloadList = this.grid;
		uiloadList.OnScrollRect = (Action)Delegate.Remove(uiloadList.OnScrollRect, new Action(this.OnScrollRect));
		UILoadList uiloadList2 = this.grid;
		uiloadList2.OnScrollRect = (Action)Delegate.Combine(uiloadList2.OnScrollRect, new Action(this.OnScrollRect));
	}

	public void Refresh()
	{
		this.GetFilters();
		this.SetGrid();
	}

	public void RefreshScoutingStatus()
	{
		int count = this.mEntries.Count;
		for (int i = 0; i < count; i++)
		{
			this.mEntries[i].UpdateAbilityScoutingState();
		}
	}

	public void ApplyFilter(UIScoutingFilter.Type inFilter, bool inAsc)
	{
		this.mFilter = inFilter;
		this.mFilterAsc = inAsc;
		this.Refresh();
	}

	public void ClearGrid()
	{
		this.mEntries.Clear();
	}

	private void SetGrid()
	{
		this.mEntries.Clear();
		this.grid.HideListItems();
		GameUtility.SetActive(this.driveableSeries.gameObject, Game.instance.championshipManager.isGTSeriesActive && this.filterJobRole == UIScoutingFilterJobRole.Filter.Drivers);
		this.SetNotifications();
		this.GetSortedList();
		this.SortByFilter();
		this.UpdateGrid(true);
	}

	private void UpdateGrid(bool inForceUpdate = false)
	{
		int count = this.mList.Count;
		if (this.grid.SetSize(count, inForceUpdate))
		{
			this.mGridItems = this.grid.activatedItems;
			int num = this.mGridItems.Length;
			int num2 = this.grid.firstActivatedIndex;
			for (int i = 0; i < num; i++)
			{
				UIScoutingStaffEntry component = this.mGridItems[i].GetComponent<UIScoutingStaffEntry>();
				Person person = this.mList[num2];
				if (component.person != person)
				{
					component.SetupEntry(person);
				}
				else
				{
					component.UpdateEntry();
				}
				this.mEntries.Add(component);
				num2++;
			}
		}
	}

	private void GetFilters()
	{
		this.ability.UpdateState();
		this.view.UpdateState();
		this.filterDriveableSeries = this.driveableSeries.filter;
		this.filterJobRole = this.jobRole.filter;
		this.filterView = this.view.filter;
		this.filterAge = this.age.filter;
		this.filterAbility = this.ability.filter;
		this.filterAbilityStars = this.ability.abilityStars;
	}

	private void GetSortedList()
	{
		switch (this.filterJobRole)
		{
		case UIScoutingFilterJobRole.Filter.Drivers:
			this.SortList<Driver>(Game.instance.driverManager.GetEntityList());
			break;
		case UIScoutingFilterJobRole.Filter.Designers:
			this.SortList<Engineer>(Game.instance.engineerManager.GetEntityList());
			break;
		case UIScoutingFilterJobRole.Filter.Mechanics:
			this.SortList<Mechanic>(Game.instance.mechanicManager.GetEntityList());
			break;
		}
	}

	private void SortList<T>(List<T> inList) where T : Person
	{
		this.mList.Clear();
		int count = inList.Count;
		for (int i = 0; i < count; i++)
		{
			Person person = inList[i];
			if (this.ApplyFilterPerson(person))
			{
				if (this.ApplyFilterSeries(person))
				{
					if (this.ApplyFilterPlayerTeam(person))
					{
						if (this.ApplyFilterScoutingLevel(person))
						{
							if (this.ApplyFilterChallengeReward(person))
							{
								if (this.ApplyFilterAge(person))
								{
									if (this.ApplyFilterAbility(person))
									{
										if (this.ApplyChampionshipFilter(person))
										{
											this.AddPersonNotifications(person);
											if (this.ApplyFilterView(person))
											{
												this.mList.Add(person);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void AddPersonNotifications(Person inPerson)
	{
		this.mNotificationAll.IncrementCount();
		if (inPerson.isShortlisted)
		{
			this.mNotificationFavourites.IncrementCount();
		}
		if (inPerson is Driver)
		{
			Driver driver = inPerson as Driver;
			if (driver.hasBeenScouted)
			{
				this.mNotificationScouted.IncrementCount();
			}
		}
	}

	private bool ApplyFilterPerson(Person inPerson)
	{
		return !inPerson.IsReplacementPerson() && !inPerson.HasRetired();
	}

	private bool ApplyFilterSeries(Person inPerson)
	{
		Driver driver = inPerson as Driver;
		if (driver != null && !driver.joinsAnySeries)
		{
			for (int i = 0; i < this.filterDriveableSeries.Length; i++)
			{
				if (!driver.HasPreferedSeries(this.filterDriveableSeries[i], true))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool ApplyFilterPlayerTeam(Person inPerson)
	{
		return inPerson.IsFreeAgent() || !inPerson.contract.GetTeam().IsPlayersTeam();
	}

	private bool ApplyFilterScoutingLevel(Person inPerson)
	{
		Driver driver = inPerson as Driver;
		if (driver == null)
		{
			return true;
		}
		int scoutingLevelRequired = driver.GetDriverStats().scoutingLevelRequired;
		if (scoutingLevelRequired == 0)
		{
			return true;
		}
		HQsBuilding_v1 building = Game.instance.player.team.headquarters.GetBuilding(HQsBuildingInfo.Type.ScoutingFacility);
		return building != null && building.isBuilt && building.currentLevel >= scoutingLevelRequired;
	}

	private bool ApplyFilterChallengeReward(Person inPerson)
	{
		return inPerson.rewardID == 0 || App.instance.challengeRewardsManager.IsRewardUnlocked(inPerson.rewardID);
	}

	private bool ApplyFilterView(Person inPerson)
	{
		UIScoutingFilterView.Filter filter = this.filterView;
		if (filter == UIScoutingFilterView.Filter.Favourites)
		{
			return inPerson.isShortlisted;
		}
		if (filter != UIScoutingFilterView.Filter.Scouted)
		{
			return true;
		}
		if (inPerson is Driver)
		{
			Driver driver = (Driver)inPerson;
			return driver.hasBeenScouted;
		}
		return true;
	}

	private bool ApplyFilterAge(Person inPerson)
	{
		int num = inPerson.GetAge();
		switch (this.filterAge)
		{
		case UIScoutingFilterAge.Filter.Young:
			return num >= 16 && num <= 22;
		case UIScoutingFilterAge.Filter.Medium:
			return num >= 22 && num <= 28;
		case UIScoutingFilterAge.Filter.Old:
			return num >= 28 && num <= 34;
		case UIScoutingFilterAge.Filter.Older:
			return num >= 34;
		default:
			return true;
		}
	}

	private bool ApplyFilterAbility(Person inPerson)
	{
		float num = inPerson.GetStats().GetAbility();
		UIScoutingFilterAbility.Filter filter = this.filterAbility;
		if (filter != UIScoutingFilterAbility.Filter.Specific)
		{
			return true;
		}
		if (inPerson is Driver)
		{
			Driver driver = (Driver)inPerson;
			return driver.CanShowStats() && num >= this.filterAbilityStars;
		}
		return num >= this.filterAbilityStars;
	}

	private bool ApplyChampionshipFilter(Person inPerson)
	{
		return inPerson.IsFreeAgent() || inPerson.contract.GetTeam().championship.isChoosable;
	}

	private void SortByFilter()
	{
		switch (this.mFilter)
		{
		case UIScoutingFilter.Type.Name:
			Person.SortByName<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.Nationality:
			Person.SortByNationality<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.Age:
			Person.SortByAge<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.Ability:
			Person.SortByAbility<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.Team:
			Person.SortByTeam<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.RacingSeries:
			Person.SortByRacingSeries<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.RaceCost:
			Person.SortByRaceCost<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.BreakClause:
			Person.SortByBreakClauseCost<Person>(this.mList, this.mFilterAsc);
			break;
		case UIScoutingFilter.Type.ContractEnds:
			Person.SortByContractEndDate<Person>(this.mList, this.mFilterAsc);
			break;
		}
	}

	private void SetNotifications()
	{
		if (this.mNotificationAll == null)
		{
			this.mNotificationAll = Game.instance.notificationManager.GetNotification("ScoutingScreenAll");
		}
		if (this.mNotificationFavourites == null)
		{
			this.mNotificationFavourites = Game.instance.notificationManager.GetNotification("ScoutingScreenFavourites");
		}
		if (this.mNotificationScouted == null)
		{
			this.mNotificationScouted = Game.instance.notificationManager.GetNotification("ScoutingScreenScouted");
		}
		this.mNotificationAll.ResetCount();
		this.mNotificationFavourites.ResetCount();
		this.mNotificationScouted.ResetCount();
	}

	public void UpdateFavouriteNotification(bool inIncrease)
	{
		if (this.mNotificationFavourites == null)
		{
			this.mNotificationFavourites = Game.instance.notificationManager.GetNotification("ScoutingScreenFavourites");
		}
		if (inIncrease)
		{
			this.mNotificationFavourites.IncrementCount();
		}
		else
		{
			this.mNotificationFavourites.DecrementCount();
		}
	}

	private void OnScrollRect()
	{
		this.UpdateGrid(false);
	}

	public void HideTooltips()
	{
		DriverInfoRollover.HideTooltip();
		StaffInfoRollover.HideTooltip();
	}

	public UILoadList grid;

	public UIScoutingFilter[] tableFilters;

	public UIScoutingFilterDriveableSeries driveableSeries;

	public UIScoutingFilterJobRole jobRole;

	public UIScoutingFilterView view;

	public UIScoutingFilterAge age;

	public UIScoutingFilterAbility ability;

	public Championship.Series[] filterDriveableSeries = new Championship.Series[1];

	public UIScoutingFilterJobRole.Filter filterJobRole;

	public UIScoutingFilterView.Filter filterView;

	public UIScoutingFilterAge.Filter filterAge;

	public UIScoutingFilterAbility.Filter filterAbility;

	public float filterAbilityStars = 5f;

	private GameObject[] mGridItems;

	private List<Person> mList = new List<Person>();

	private List<UIScoutingStaffEntry> mEntries = new List<UIScoutingStaffEntry>();

	private UIScoutingFilter.Type mFilter = UIScoutingFilter.Type.Ability;

	private bool mFilterAsc;

	private Notification mNotificationAll;

	private Notification mNotificationFavourites;

	private Notification mNotificationScouted;
}
