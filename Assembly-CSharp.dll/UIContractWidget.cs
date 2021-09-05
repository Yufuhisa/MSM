using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIContractWidget : MonoBehaviour
{
	public UIContractWidget()
	{
	}

	private void Start()
	{
		this.negotiateButton.onClick.AddListener(new UnityAction(this.OnNegotiateButton));
		this.compareButton.onClick.AddListener(new UnityAction(this.OnCompareButton));
		this.fireButton.onClick.AddListener(new UnityAction(this.OnFireButton));
	}

	public void Setup(Driver inDriver)
	{
		this.mDriver = inDriver;
		this.details.Setup(this.mDriver);
		this.negotiateLabel.text = ((!this.mDriver.isNegotiatingContract) ? ((!this.mDriver.IsPlayersDriver()) ? Localisation.LocaliseID("PSG_10007753", null) : Localisation.LocaliseID("PSG_10006855", null)) : Localisation.LocaliseID("PSG_10010603", null));
		bool flag = App.instance.gameStateManager.currentState.group != GameState.Group.Frontend;
		bool flag2 = !Game.instance.player.IsUnemployed();
		this.negotiateButton.interactable = (!flag && !this.mDriver.isNegotiatingContract);
		this.compareButton.interactable = !flag;
		this.fireButton.interactable = !flag;
		GameUtility.SetActive(this.negotiateButton.gameObject, flag2 && (this.mDriver.canNegotiateContract || this.mDriver.isNegotiatingContract));
		GameUtility.SetActive(this.compareButton.gameObject, flag2 && this.mDriver.CanShowStats());
		GameUtility.SetActive(this.fireButton.gameObject, this.mDriver.canBeFired);
	}

	private void OnNegotiateButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (this.mDriver.canNegotiateContract)
		{
			bool staffTransferWindowPreseason = Game.instance.player.team.championship.rules.staffTransferWindowPreseason;
			bool flag = !staffTransferWindowPreseason || (staffTransferWindowPreseason && App.instance.gameStateManager.currentState is PreSeasonState);
			bool flag2 = this.mDriver.IsFreeAgent() || !this.mDriver.contract.GetTeam().IsPlayersTeam();
			if (flag && flag2)
			{
				UIManager.instance.dialogBoxManager.GetDialog<ApproachDialogBox>().Show(this.mDriver, ApproachDialogBox.ApproachType.SignNewContract);
			}
			else if (!flag2)
			{
				UIManager.instance.dialogBoxManager.GetDialog<ApproachDialogBox>().Show(this.mDriver, ApproachDialogBox.ApproachType.RenewContract);
			}
			else
			{
				GenericConfirmation dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericConfirmation>();
				string inTitle = Localisation.LocaliseID("PSG_10010846", null);
				string inText = Localisation.LocaliseID("PSG_10010847", null);
				string inCancelString = Localisation.LocaliseID("PSG_10009081", null);
				string empty = string.Empty;
				dialog.Show(null, inCancelString, null, empty, inText, inTitle);
			}
		}
	}

	private void OnCompareButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		UIManager.instance.ChangeScreen("CompareStaffScreen", this.mDriver, UIManager.ScreenTransition.None, 0f, UIManager.NavigationType.Normal);
	}

	private void OnFireButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (!Game.instance.challengeManager.IsFiringDriverForChallenge(this.mDriver))
		{
			FirePopup dialog = UIManager.instance.dialogBoxManager.GetDialog<FirePopup>();
			dialog.Setup(this.mDriver);
			UIManager.instance.dialogBoxManager.Show(dialog);
		}
	}

	public TextMeshProUGUI negotiateLabel;

	public UIPersonContractDetailsWidget details;

	public Button fireButton;

	public Button compareButton;

	public Button negotiateButton;

	private Driver mDriver;
}
