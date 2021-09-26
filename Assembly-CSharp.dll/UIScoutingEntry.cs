using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIScoutingEntry : MonoBehaviour
{
	public UIScoutingEntry()
	{
	}

	public void SetupEmpty()
	{
		GameUtility.SetActive(this.emptyState, true);
		GameUtility.SetActive(this.lockedState, false);
		GameUtility.SetActive(this.onGoingState, false);
	}

	public void SetupLocked(int inLevelRequired)
	{
		GameUtility.SetActive(this.emptyState, false);
		GameUtility.SetActive(this.lockedState, true);
		GameUtility.SetActive(this.onGoingState, false);
		if (inLevelRequired == 1)
		{
			this.mScoutingLevelRequired = Localisation.LocaliseID("PSG_10010499", null);
		}
		else
		{
			StringVariableParser.ordinalNumberString = inLevelRequired.ToString();
			this.mScoutingLevelRequired = Localisation.LocaliseID("PSG_10010500", null);
		}
	}

	public void Setup(Driver inDriver, UIScoutingScoutWidget.Type inType)
	{
		GameUtility.SetActive(this.emptyState, false);
		GameUtility.SetActive(this.lockedState, false);
		GameUtility.SetActive(this.onGoingState, true);
		if (inDriver != null)
		{
			this.mDriver = inDriver;
			this.mType = inType;
			this.button.onClick.RemoveAllListeners();
			this.button.onClick.AddListener(new UnityAction(this.OnDetailsButton));
			this.cancelButton.onClick.RemoveAllListeners();
			this.cancelButton.onClick.AddListener(new UnityAction(this.OnCancelButton));
			this.detailsButton.onClick.RemoveAllListeners();
			this.detailsButton.onClick.AddListener(new UnityAction(this.OnDetailsButton));
			this.SetDetails();
		}
	}

	private void Update()
	{
		if (this.mDriver != null && this.mType == UIScoutingScoutWidget.Type.OnGoing)
		{
			this.UpdateProgress();
		}
	}

	private void SetDetails()
	{
		this.personFlag.SetNationality(this.mDriver.nationality);
		this.personName.text = this.mDriver.name;
		bool flag = this.widget.scoutingManager.IsDriverInScoutQueue(this.mDriver);
		flag |= this.widget.scoutingManager.IsDriverCurrentlyScouted(this.mDriver);
		GameUtility.SetActive(this.daysLeft.gameObject, flag);
		GameUtility.SetActive(this.timeToCompleteObject, flag);
		GameUtility.SetActive(this.cancelButton.gameObject, flag);
		GameUtility.SetActive(this.detailsButton.gameObject, !flag);
		if (this.daysLeft.gameObject.activeSelf)
		{
			this.daysLeft.text = GameUtility.FormatTimeSpanDays(this.widget.scoutingManager.GetTimeLeftForScoutingDriver(this.mDriver));
		}
		this.UpdateProgress();
	}

	private void UpdateProgress()
	{
		bool flag = this.widget.scoutingManager.IsDriverCurrentlyScouted(this.mDriver);
		if (this.daysLeft.gameObject.activeSelf && flag)
		{
			this.daysLeft.text = GameUtility.FormatTimeSpanDays(this.widget.scoutingManager.GetTimeLeftForScoutingDriver(this.mDriver));
		}
		switch (this.mType)
		{
		case UIScoutingScoutWidget.Type.OnGoing:
			GameUtility.SetActive(this.progressBar.gameObject, true);
			this.progressBar.value = ((!flag) ? 0f : this.widget.scoutingManager.GetTimeLeftForScoutingDriverNormalized(this.mDriver));
			break;
		case UIScoutingScoutWidget.Type.Completed:
			GameUtility.SetActive(this.progressBar.gameObject, false);
			break;
		case UIScoutingScoutWidget.Type.InQueue:
			GameUtility.SetActive(this.progressBar.gameObject, false);
			break;
		}
	}

	private void OnCancelButton()
	{
		if ((this.mDriver != null && this.widget.scoutingManager.IsDriverInScoutQueue(this.mDriver)) || this.widget.scoutingManager.IsDriverCurrentlyScouted(this.mDriver))
		{
			scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
			this.widget.scoutingManager.RemoveDriverFromScoutingJobs(this.mDriver);
			this.mDriver = null;
			this.widget.screen.Refresh();
		}
	}

	private void OnDetailsButton()
	{
		if (this.mDriver != null)
		{
			UIManager.instance.ChangeScreen("DriverScreen", this.mDriver, UIManager.ScreenTransition.None, 0f, UIManager.NavigationType.Normal);
		}
	}

	public void ShowLockedTooltip()
	{
		scSoundManager.BlockSoundEvents = true;
		UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10010501", null), this.mScoutingLevelRequired);
		scSoundManager.BlockSoundEvents = false;
	}

	public void HideLockedTooltip()
	{
		UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Hide();
	}

	public Button button;

	public Button cancelButton;

	public Button detailsButton;

	public Flag personFlag;

	public TextMeshProUGUI personName;

	public TextMeshProUGUI daysLeft;

	public Slider progressBar;

	public GameObject onGoingState;

	public GameObject emptyState;

	public GameObject lockedState;

	public GameObject timeToCompleteObject;

	public UIScoutingScoutWidget widget;

	private UIScoutingScoutWidget.Type mType;

	private Driver mDriver;

	private string mScoutingLevelRequired = string.Empty;
}
