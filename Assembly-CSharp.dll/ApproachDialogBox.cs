using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ApproachDialogBox : UIDialogBox
{
	public ApproachDialogBox()
	{
	}

	private void Start()
	{
		this.startNegotiationsButton.onClick.AddListener(new UnityAction(this.OnStartNegotiationButton));
		this.retractOfferButton.onClick.AddListener(new UnityAction(this.OnCancelButtonClicked));
	}

	public void Show(Person inPerson, ApproachDialogBox.ApproachType inType)
	{
		UIManager.instance.dialogBoxManager.Show(this);
		this.mApproachType = inType;
		this.mPerson = inPerson;
		this.SetApproachType();
		this.SetupDialogBox();
	}

	private void SetApproachType()
	{
		if (this.mPerson is Driver)
		{
			this.mApproachEntity = ApproachDialogBox.ApproachEntity.Driver;
		}
		else if (this.mPerson is Chairman)
		{
			this.mApproachEntity = ApproachDialogBox.ApproachEntity.Team;
		}
		else
		{
			this.mApproachEntity = ApproachDialogBox.ApproachEntity.Staff;
		}
	}

	private void SetupDialogBox()
	{
		this.SetupHeader();
		this.SetupButtonLabels();
		this.SetupPortrait();
		this.SetupAbilityStars();
		this.SetupTeamDetails();
		StringVariableParser.subject = this.mPerson;
		ApproachDialogBox.ApproachType approachType = this.mApproachType;
		if (approachType != ApproachDialogBox.ApproachType.SignNewContract)
		{
			if (approachType == ApproachDialogBox.ApproachType.RenewContract)
			{
				this.SetupRenewStatus();
			}
		}
		else
		{
			this.SetupApproachStatus();
		}
		StringVariableParser.subject = this.mPerson;
		this.personPosition.text = Localisation.LocaliseEnum(this.mPerson.contract.job);
		this.personNationality.SetNationality(this.mPerson.nationality);
		this.personName.text = this.mPerson.name;
		StringVariableParser.subject = null;
	}

	private void SetupHeader()
	{
		ApproachDialogBox.ApproachType approachType = this.mApproachType;
		if (approachType != ApproachDialogBox.ApproachType.SignNewContract)
		{
			if (approachType == ApproachDialogBox.ApproachType.RenewContract)
			{
				this.popupHeader.text = Localisation.LocaliseID("PSG_10006855", null);
			}
		}
		else
		{
			switch (this.mApproachEntity)
			{
			case ApproachDialogBox.ApproachEntity.Driver:
				this.popupHeader.text = Localisation.LocaliseID("PSG_10007753", null);
				break;
			case ApproachDialogBox.ApproachEntity.Staff:
				if (this.mPerson is Engineer)
				{
					this.popupHeader.text = Localisation.LocaliseID("PSG_10007755", null);
				}
				else
				{
					this.popupHeader.text = Localisation.LocaliseID("PSG_10007754", null);
				}
				break;
			case ApproachDialogBox.ApproachEntity.Team:
				this.popupHeader.text = Localisation.LocaliseID("PSG_10009239", null);
				break;
			}
		}
	}

	private void SetupButtonLabels()
	{
		ApproachDialogBox.ApproachType approachType = this.mApproachType;
		if (approachType != ApproachDialogBox.ApproachType.SignNewContract)
		{
			if (approachType == ApproachDialogBox.ApproachType.RenewContract)
			{
				this.negotiationButtonLabel.text = Localisation.LocaliseID("PSG_10009241", null);
				this.retractButtonLabel.text = Localisation.LocaliseID("PSG_10009242", null);
			}
		}
		else
		{
			switch (this.mApproachEntity)
			{
			case ApproachDialogBox.ApproachEntity.Driver:
			case ApproachDialogBox.ApproachEntity.Staff:
				this.negotiationButtonLabel.text = Localisation.LocaliseID("PSG_10009241", null);
				this.retractButtonLabel.text = Localisation.LocaliseID("PSG_10009242", null);
				break;
			case ApproachDialogBox.ApproachEntity.Team:
				this.negotiationButtonLabel.text = Localisation.LocaliseID("PSG_10009243", null);
				this.retractButtonLabel.text = Localisation.LocaliseID("PSG_10009244", null);
				break;
			}
		}
	}

	private void SetupPortrait()
	{
		switch (this.mApproachEntity)
		{
		case ApproachDialogBox.ApproachEntity.Driver:
			GameUtility.SetActive(this.personPortrait.gameObject, false);
			GameUtility.SetActive(this.driverPortrait.gameObject, true);
			this.driverPortrait.SetPortrait(this.mPerson);
			break;
		case ApproachDialogBox.ApproachEntity.Staff:
		case ApproachDialogBox.ApproachEntity.Team:
			GameUtility.SetActive(this.personPortrait.gameObject, true);
			GameUtility.SetActive(this.driverPortrait.gameObject, false);
			this.personPortrait.SetPortrait(this.mPerson);
			break;
		}
	}

	private void SetupAbilityStars()
	{
		switch (this.mApproachEntity)
		{
		case ApproachDialogBox.ApproachEntity.Driver:
			this.personAbilityStars.SetAbilityStarsData(this.mPerson as Driver);
			break;
		case ApproachDialogBox.ApproachEntity.Staff:
			this.personAbilityStars.SetAbilityStarsData(this.mPerson);
			break;
		case ApproachDialogBox.ApproachEntity.Team:
			GameUtility.SetActive(this.personAbilityStars.gameObject, false);
			break;
		}
	}

	private void SetupTeamDetails()
	{
		switch (this.mApproachEntity)
		{
		case ApproachDialogBox.ApproachEntity.Driver:
		case ApproachDialogBox.ApproachEntity.Staff:
			if (this.mPerson.IsFreeAgent())
			{
				GameUtility.SetActive(this.teamObject, false);
				this.personTeamLogo.SetTeam(this.mPerson.contract.GetTeam());
			}
			else
			{
				GameUtility.SetActive(this.teamObject, true);
				this.personTeamName.text = this.mPerson.contract.GetTeam().name;
				this.personTeamLogo.SetTeam(this.mPerson.contract.GetTeam());
			}
			break;
		case ApproachDialogBox.ApproachEntity.Team:
			GameUtility.SetActive(this.teamObject, true);
			this.personTeamName.text = this.mPerson.contract.GetTeam().name;
			this.personTeamLogo.SetTeam(this.mPerson.contract.GetTeam());
			break;
		}
	}

	private void SetupRenewStatus()
	{
		ApproachDialogBox.ApproachEntity approachEntity = this.mApproachEntity;
		if (approachEntity == ApproachDialogBox.ApproachEntity.Driver || approachEntity == ApproachDialogBox.ApproachEntity.Staff)
		{
			Team team = Game.instance.player.team;
			Person.InterestedToTalkResponseType interestedToTalkReaction = this.mPerson.GetInterestedToTalkReaction(team);
			bool flag = interestedToTalkReaction == Person.InterestedToTalkResponseType.InterestedToTalk;
			GameUtility.SetActive(this.startNegotiationsButton.gameObject, flag);
			this.approachChat.text = this.GetInterestedToTalkText(interestedToTalkReaction);
			if (flag)
			{
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009245", null);
				this.approachStatus.color = UIConstants.approachDialogBoxGreen;
				this.approachHighlight.color = UIConstants.approachDialogBoxGreen;
			}
			else
			{
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009246", null);
				this.approachStatus.color = UIConstants.approachDialogBoxRed;
				this.approachHighlight.color = UIConstants.approachDialogBoxRed;
				this.retractButtonLabel.text = Localisation.LocaliseID("PSG_10009081", null);
			}
		}
	}

	private void SetupApproachStatus()
	{
		switch (this.mApproachEntity)
		{
		case ApproachDialogBox.ApproachEntity.Driver:
		case ApproachDialogBox.ApproachEntity.Staff:
		{
			Team team = Game.instance.player.team;
			Person.InterestedToTalkResponseType interestedToTalkReaction = this.mPerson.GetInterestedToTalkReaction(team);
			bool flag = interestedToTalkReaction == Person.InterestedToTalkResponseType.InterestedToTalk;
			GameUtility.SetActive(this.startNegotiationsButton.gameObject, flag);
			this.approachChat.text = this.GetInterestedToTalkText(interestedToTalkReaction);
			if (flag)
			{
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009245", null);
				this.approachStatus.color = UIConstants.approachDialogBoxGreen;
				this.approachHighlight.color = UIConstants.approachDialogBoxGreen;
			}
			else
			{
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009246", null);
				this.approachStatus.color = UIConstants.approachDialogBoxRed;
				this.approachHighlight.color = UIConstants.approachDialogBoxRed;
			}
			break;
		}
		case ApproachDialogBox.ApproachEntity.Team:
		{
			Team team2 = this.mPerson.contract.GetTeam();
			bool flag2 = Game.instance.player.CanApproachTeam(team2);
			if (flag2)
			{
				GameUtility.SetActive(this.startNegotiationsButton.gameObject, true);
				this.approachChat.text = Localisation.LocaliseID("PSG_10009247", null);
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009248", null);
				this.approachStatus.color = UIConstants.approachDialogBoxGreen;
				this.approachHighlight.color = UIConstants.approachDialogBoxGreen;
			}
			else
			{
				GameUtility.SetActive(this.startNegotiationsButton.gameObject, false);
				bool flag3 = Game.instance.player.isTeamHistoryCooldownReady(team2);
				bool hasJoinedTeamRecently = Game.instance.player.hasJoinedTeamRecently;
				bool flag4 = Game.instance.time.now.Subtract(team2.teamPrincipal.contract.startDate).TotalDays < 180.0;
				if (!flag3)
				{
					this.approachChat.text = Localisation.LocaliseID("PSG_10009249", null);
				}
				else if (hasJoinedTeamRecently)
				{
					this.approachChat.text = Localisation.LocaliseID("PSG_10009250", null);
				}
				else if (flag4)
				{
					this.approachChat.text = Localisation.LocaliseID("PSG_10009251", null);
				}
				else
				{
					this.approachChat.text = Localisation.LocaliseID("PSG_10009252", null);
				}
				this.approachStatus.text = Localisation.LocaliseID("PSG_10009253", null);
				this.approachStatus.color = UIConstants.approachDialogBoxRed;
				this.approachHighlight.color = UIConstants.approachDialogBoxRed;
			}
			break;
		}
		}
	}

	private string GetInterestedToTalkText(Person.InterestedToTalkResponseType inInterestedToTalk)
	{
		switch (inInterestedToTalk)
		{
		case Person.InterestedToTalkResponseType.InterestedToTalk:
			return Localisation.LocaliseID("PSG_10009258", null);
		case Person.InterestedToTalkResponseType.NotJoiningLowerChampionship:
			return Localisation.LocaliseID("PSG_10011117", null);
		case Person.InterestedToTalkResponseType.WantToJoinHigherChampionship:
			return Localisation.LocaliseID("PSG_10011119", null);
		case Person.InterestedToTalkResponseType.JustBeenFiredByPlayer:
			return Localisation.LocaliseID("PSG_10009254", null);
		case Person.InterestedToTalkResponseType.InsultedByLastProposal:
			return Localisation.LocaliseID("PSG_10009257", null);
		case Person.InterestedToTalkResponseType.WantsToRetire:
			return Localisation.LocaliseID("PSG_10009261", null);
		case Person.InterestedToTalkResponseType.WontRenewContract:
			return Localisation.LocaliseID("PSG_10009262", null);
		case Person.InterestedToTalkResponseType.WontJoinRival:
			return Localisation.LocaliseID("PSG_10009256", null);
		case Person.InterestedToTalkResponseType.JustStartedANewContract:
			return Localisation.LocaliseID("PSG_10009255", null);
		case Person.InterestedToTalkResponseType.TooEarlyToRenew:
			return Localisation.LocaliseID("PSG_10009260", null);
		case Person.InterestedToTalkResponseType.MoraleTooLow:
			return Localisation.LocaliseID("PSG_10009259", null);
		case Person.InterestedToTalkResponseType.LetNegotiationExpire:
			return Localisation.LocaliseID("PSG_10011091", null);
		case Person.InterestedToTalkResponseType.CanceledNegotiation:
			return Localisation.LocaliseID("PSG_10011022", null);
		case Person.InterestedToTalkResponseType.WontDriveForThatSeries:
		{
			Driver driver = this.mPerson as Driver;
			if (driver != null)
			{
				if (driver.preferedSeries.Count == 1)
				{
					switch (driver.preferedSeries[0])
					{
					case Championship.Series.SingleSeaterSeries:
						return Localisation.LocaliseID("PSG_10011985", null);
					case Championship.Series.GTSeries:
						return Localisation.LocaliseID("PSG_10011986", null);
					case Championship.Series.EnduranceSeries:
						return Localisation.LocaliseID("PSG_10013540", null);
					}
				}
				else if (driver.preferedSeries.Count == 2)
				{
					if (!driver.HasPreferedSeries(Championship.Series.SingleSeaterSeries, false))
					{
						return Localisation.LocaliseID("PSG_10013584", null);
					}
					if (!driver.HasPreferedSeries(Championship.Series.GTSeries, false))
					{
						return Localisation.LocaliseID("PSG_10013585", null);
					}
					if (!driver.HasPreferedSeries(Championship.Series.EnduranceSeries, false))
					{
						return Localisation.LocaliseID("PSG_10013541", null);
					}
				}
			}
			return "'Won't driver for series' being used for a non driver entity";
		}
		case Person.InterestedToTalkResponseType.OffendedByInterview:
			return Localisation.LocaliseID("PSG_10012169", null);
		case Person.InterestedToTalkResponseType.InvestorDriverAgeTooHigh:
			return Localisation.LocaliseID("PSG_10012273", null);
		case Person.InterestedToTalkResponseType.TeamRangToLow:
			return Localisation.LocaliseID("PSG_11000000", null);
		case Person.InterestedToTalkResponseType.F1MidSeason:
			return Localisation.LocaliseID("PSG_11000001", null);
		}
		return Localisation.LocaliseID("PSG_10009263", null);
	}

	private void OnStartNegotiationButton()
	{
		if (this.mApproachEntity != ApproachDialogBox.ApproachEntity.Team)
		{
			UIManager.instance.ChangeScreen("ContractNegotiationScreen", this.mPerson, UIManager.ScreenTransition.None, 0f, UIManager.NavigationType.Normal);
			this.Hide();
		}
		else
		{
			Game.instance.player.SendJobApplication(this.mPerson.contract.GetTeam());
			this.Hide();
		}
	}

	[SerializeField]
	private TextMeshProUGUI popupHeader;

	[SerializeField]
	private UICharacterPortrait personPortrait;

	[SerializeField]
	private UICharacterPortrait driverPortrait;

	[SerializeField]
	private UITeamLogo personTeamLogo;

	[SerializeField]
	private TextMeshProUGUI personName;

	[SerializeField]
	private TextMeshProUGUI personTeamName;

	[SerializeField]
	private TextMeshProUGUI personPosition;

	[SerializeField]
	private UIAbilityStars personAbilityStars;

	[SerializeField]
	private Flag personNationality;

	[SerializeField]
	private GameObject teamObject;

	[SerializeField]
	private TextMeshProUGUI approachChat;

	[SerializeField]
	private TextMeshProUGUI approachStatus;

	[SerializeField]
	private Image approachHighlight;

	[SerializeField]
	private Button retractOfferButton;

	[SerializeField]
	private Button startNegotiationsButton;

	[SerializeField]
	private TextMeshProUGUI negotiationButtonLabel;

	[SerializeField]
	private TextMeshProUGUI retractButtonLabel;

	private ApproachDialogBox.ApproachEntity mApproachEntity;

	private ApproachDialogBox.ApproachType mApproachType;

	private Person mPerson;

	public enum ApproachType
	{
		SignNewContract,
		RenewContract
	}

	private enum ApproachEntity
	{
		Driver,
		Staff,
		Team
	}
}
