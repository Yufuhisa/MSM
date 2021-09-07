using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIScoutingScoutWidget : MonoBehaviour
{
	public UIScoutingScoutWidget()
	{
	}

	public void OnStart()
	{
		this.cancelJobsButton.onClick.AddListener(new UnityAction(this.OnCancelJobs));
		GameUtility.SetActive(this.cancelJobsButton.gameObject, false);
	}

	public void Setup()
	{
		this.mScoutingManager = Game.instance.scoutingManager;
		this.SetDetails();
		this.Refresh();
	}

	public void Refresh()
	{
		this.SetGrid();
	}

	private void SetDetails()
	{
		Person personOnJob = Game.instance.player.team.contractManager.GetPersonOnJob(Contract.Job.Scout);
		this.scoutPortrait.SetPortrait(personOnJob);
		this.scoutName.text = personOnJob.name;
	}

	private void SetGrid()
	{
		bool inIsActive = this.mScoutingManager.IsScouting();
		GameUtility.SetActive(this.cancelJobsButton.gameObject, inIsActive);
		this.grid.DestroyListItems();
		bool flag = this.mScoutingManager.scoutingAssigmentsCompleteCount >= 1;
		this.CreateHeader(this.headerComplete, flag);
		if (flag)
		{
			this.CreateEntries(UIScoutingScoutWidget.Type.Completed);
		}
		this.CreateHeader(this.headerScouting, true);
		this.CreateEntries(UIScoutingScoutWidget.Type.OnGoing);
		bool flag2 = this.mScoutingManager.scoutingAssignmentsCount >= 1;
		this.CreateHeader(this.headerInQueue, flag2);
		if (flag2)
		{
			this.CreateEntries(UIScoutingScoutWidget.Type.InQueue);
		}
		GameUtility.SetActive(this.entryPrefab, false);
	}

	private void CreateHeader(GameObject inHeader, bool inValue)
	{
		GameUtility.SetActive(inHeader, inValue);
		if (inValue)
		{
			inHeader.transform.SetAsLastSibling();
		}
	}

	private void CreateEntries(UIScoutingScoutWidget.Type inType)
	{
		this.grid.itemPrefab = this.entryPrefab;
		GameUtility.SetActive(this.entryPrefab, true);
		switch (inType)
		{
		case UIScoutingScoutWidget.Type.OnGoing:
		{
			int totalScoutingSlots = this.mScoutingManager.totalScoutingSlots;
			for (int i = 0; i < totalScoutingSlots; i++)
			{
				UIScoutingEntry uiscoutingEntry = this.grid.CreateListItem<UIScoutingEntry>();
				if (this.mScoutingManager.IsSlotLocked(i))
				{
					int inLevelRequired = i - this.mScoutingManager.baseScoutingSlotsCount;
					uiscoutingEntry.SetupLocked(inLevelRequired);
				}
				else if (this.mScoutingManager.IsSlotEmpty(i))
				{
					uiscoutingEntry.SetupEmpty();
				}
				else
				{
					uiscoutingEntry.Setup(this.mScoutingManager.GetCurrentScoutingEntry(i).driver, inType);
				}
			}
			break;
		}
		case UIScoutingScoutWidget.Type.Completed:
		{
			int scoutingAssigmentsCompleteCount = this.mScoutingManager.scoutingAssigmentsCompleteCount;
			for (int j = 0; j < scoutingAssigmentsCompleteCount; j++)
			{
				UIScoutingEntry uiscoutingEntry2 = this.grid.CreateListItem<UIScoutingEntry>();
				uiscoutingEntry2.Setup(this.mScoutingManager.GetCompletedDriver(j).driver, inType);
			}
			break;
		}
		case UIScoutingScoutWidget.Type.InQueue:
		{
			int scoutingAssignmentsCount = this.mScoutingManager.scoutingAssignmentsCount;
			for (int k = 0; k < scoutingAssignmentsCount; k++)
			{
				UIScoutingEntry uiscoutingEntry3 = this.grid.CreateListItem<UIScoutingEntry>();
				uiscoutingEntry3.Setup(this.mScoutingManager.GetDriverInQueue(k).driver, inType);
			}
			break;
		}
		}
	}

	private void OnCancelJobs()
	{
		GenericConfirmation dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericConfirmation>();
		string inCancelString = Localisation.LocaliseID("PSG_10009077", null);
		string inConfirmString = Localisation.LocaliseID("PSG_10009078", null);
		string inText = Localisation.LocaliseID("PSG_10010497", null);
		string inTitle = Localisation.LocaliseID("PSG_10010498", null);
		Action inConfirmAction = delegate()
		{
			this.mScoutingManager.StopAllScoutingJobs();
			this.screen.Refresh();
		};
		dialog.Show(null, inCancelString, inConfirmAction, inConfirmString, inText, inTitle);
	}

	public ScoutingManager scoutingManager
	{
		get
		{
			return this.mScoutingManager;
		}
	}

	public UIGridList grid;

	public Button cancelJobsButton;

	public UICharacterPortrait scoutPortrait;

	public TextMeshProUGUI scoutName;

	public GameObject headerScouting;

	public GameObject headerInQueue;

	public GameObject headerComplete;

	public GameObject entryPrefab;

	public ScoutingScreen screen;

	private ScoutingManager mScoutingManager;

	public enum Type
	{
		OnGoing,
		Completed,
		InQueue
	}
}
