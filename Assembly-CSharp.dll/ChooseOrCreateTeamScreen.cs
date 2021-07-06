using System;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChooseOrCreateTeamScreen : UIScreen
{
	public ChooseOrCreateTeamScreen()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static ChooseOrCreateTeamScreen()
	{
	}

	public override void OnStart()
	{
		base.OnStart();
		this.createTeamGetContentButton.onClick.AddListener(new UnityAction(this.OnCreateTeamGetContent));
		this.chooseTeamButton.onClick.AddListener(new UnityAction(this.OnChooseTeamButton));
		this.createTeamButton.onClick.AddListener(new UnityAction(this.OnCreateTeamButton));
	}

	private void OnChooseTeamButton()
	{
		CreateTeamManager.Reset();
		UIManager.instance.ChangeScreen("ChooseSeriesScreen", UIManager.ScreenTransition.None, 0f, null, UIManager.NavigationType.Normal);
	}

	private void OnCreateTeamButton()
	{
		CreateTeamManager.StartCreateNewTeam();
		UIManager.instance.ChangeScreen("ChooseSeriesScreen", UIManager.ScreenTransition.None, 0f, null, UIManager.NavigationType.Normal);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		base.continueButtonInteractable = false;
		base.SetTopBarMode(UITopBar.Mode.Core);
		base.SetBottomBarMode(UIBottomBar.Mode.Core);
		this.UpdateData();
		DLCManager dlcManager = App.instance.dlcManager;
		dlcManager.OnOwnedDlcChanged = (Action)Delegate.Combine(dlcManager.OnOwnedDlcChanged, new Action(this.UpdateData));
	}

	public override void OnExit()
	{
		DLCManager dlcManager = App.instance.dlcManager;
		dlcManager.OnOwnedDlcChanged = (Action)Delegate.Remove(dlcManager.OnOwnedDlcChanged, new Action(this.UpdateData));
		base.OnExit();
	}

	private void UpdateData()
	{
		GameUtility.SetActive(this.createTeamLock, !App.instance.dlcManager.IsDlcKnown(ChooseOrCreateTeamScreen.createTeamDlcAppId) || !App.instance.dlcManager.IsDlcInstalled(ChooseOrCreateTeamScreen.createTeamDlcAppId));
		this.createTeamButton.interactable = !this.createTeamLock.activeSelf;
	}

	public override UIScreen.NavigationButtonEvent OnContinueButton()
	{
		return UIScreen.NavigationButtonEvent.HandledByScreen;
	}

	private void OnCreateTeamGetContent()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		SteamFriends.ActivateGameOverlayToStore(new AppId_t(DLCManager.GetDlcByName("Create Team").appId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
	}

	private static uint createTeamDlcAppId = DLCManager.GetDlcByName("Create Team").appId;

	public Button chooseTeamButton;

	public Button createTeamButton;

	public GameObject createTeamLock;

	public Button createTeamGetContentButton;
}
