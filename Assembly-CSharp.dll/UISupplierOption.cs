using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISupplierOption : MonoBehaviour
{
	public UISupplierOption()
	{
	}

	public void Setup(UISupplierSelectionEntry inSupplierOptionEntry, Supplier inSupplier, ToggleGroup inToggleGroup)
	{
		GameUtility.SetActive(base.gameObject, true);
		this.mSupplierEntry = inSupplierOptionEntry;
		this.mSupplier = inSupplier;
		this.toggle.onValueChanged.RemoveAllListeners();
		this.toggle.group = null;
		this.toggle.isOn = false;
		this.toggle.group = inToggleGroup;
		this.toggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnToggle));
		UISupplierOption.SetupSupplierDetails(this.mSupplier, this.supplierLogoWidget, this.statParentObjects, new TextMeshProUGUI[]
		{
			this.statOne,
			this.statTwo,
			this.statThree,
			this.description
		});
		this.SetupSupplierData();
	}

	private void SetupSupplierData()
	{
		Team team = Game.instance.player.team;
		this.cost.text = GameUtility.GetCurrencyString((long)this.mSupplier.GetPrice(team), 0);
		GameUtility.SetActive(this.discountDetails, this.showDetails());

		this.canvasGroup.alpha = 1f;
		this.canvasGroup.interactable = true;

		// if option is not available for player, deactivate and grey out
		if (!this.mSupplier.CanTeamBuyThis(team) || !this.mSupplier.chassiIsAvailable) {
			this.canvasGroup.alpha = 0.2f;
			this.canvasGroup.interactable = false;
		}
	}

	public static void SetupSupplierDetails(Supplier inSupplier, UISupplierLogoWidget inSupplierLogoWidget, GameObject[] inParentObjects, params TextMeshProUGUI[] inStatLabels)
	{
		inSupplierLogoWidget.SetLogo(inSupplier);
		switch (inSupplier.supplierType)
		{
		case Supplier.SupplierType.Fuel:
			UISupplierOption.SetStatString(inStatLabels[0], inSupplier.supplierStats[CarChassisStats.Stats.FuelEfficiency]);
			UISupplierOption.SetFuelStatString(inStatLabels[1], inSupplier.maxReliablity * 100f);
			UISupplierOption.SetFuelStatString(inStatLabels[2], inSupplier.randomEngineLevelModifier);
			GameUtility.SetActiveAndCheckNull(inParentObjects[0], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[1], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[2], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[3], false);
			break;
		case Supplier.SupplierType.Engine:
			UISupplierOption.SetStatString(inStatLabels[0], inSupplier.supplierStats[CarChassisStats.Stats.FuelEfficiency]);
			inStatLabels[1].text = string.Format("{0}", (inSupplier.maxReliablity * 100f).ToString("#00"));
			inStatLabels[2].text = string.Format("{0}", inSupplier.randomEngineLevelModifier.ToString("##00"));
			GameUtility.SetActiveAndCheckNull(inParentObjects[0], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[1], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[2], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[3], false);
			break;
		case Supplier.SupplierType.Brakes:
		case Supplier.SupplierType.Materials:
			UISupplierOption.SetStatString(inStatLabels[0], inSupplier.supplierStats[CarChassisStats.Stats.TyreWear]);
			UISupplierOption.SetStatString(inStatLabels[1], inSupplier.supplierStats[CarChassisStats.Stats.TyreHeating]);
			GameUtility.SetActiveAndCheckNull(inParentObjects[0], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[1], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[2], false);
			GameUtility.SetActiveAndCheckNull(inParentObjects[3], false);
			break;
		case Supplier.SupplierType.Battery:
			inStatLabels[0].text = string.Format("{0}%", inSupplier.supplierStats[CarChassisStats.Stats.StartingCharge]);
			inStatLabels[1].text = string.Format("{0}%", Mathf.RoundToInt(inSupplier.randomHarvestEfficiencyModifier * 100f));
			GameUtility.SetActiveAndCheckNull(inParentObjects[0], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[1], true);
			GameUtility.SetActiveAndCheckNull(inParentObjects[2], false);
			GameUtility.SetActiveAndCheckNull(inParentObjects[3], false);
			break;
		case Supplier.SupplierType.ERSAdvanced:
			inStatLabels[3].text = inSupplier.GetDescription();
			GameUtility.SetActiveAndCheckNull(inParentObjects[0], false);
			GameUtility.SetActiveAndCheckNull(inParentObjects[1], false);
			GameUtility.SetActiveAndCheckNull(inParentObjects[2], false);
			GameUtility.SetActiveAndCheckNull(inParentObjects[3], true);
			break;
		}
	}

	private void OnToggle(bool inValue)
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (inValue)
		{
			CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
			screen.SetSupplier(this.mSupplier.supplierType, this.mSupplier);
			screen.ShowStatContibution(this.mSupplier);
			screen.preferencesWidget.UpdateBounds();
			this.mSupplierEntry.SupplierSelected(this.mSupplier);
		}
	}

	public static void SetStatString(TextMeshProUGUI inLabel, float inValue)
	{
		if (inValue > 8f)
		{
			inLabel.color = UIConstants.positiveColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010192", null);
		}
		else if (inValue > 6f)
		{
			inLabel.color = UIConstants.positiveColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010191", null);
		}
		else if (inValue > 4f)
		{
			inLabel.color = UIConstants.mailMedia;
			inLabel.text = Localisation.LocaliseID("PSG_10010190", null);
		}
		else if (inValue > 2f)
		{
			inLabel.color = UIConstants.negativeColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010189", null);
		}
		else
		{
			inLabel.color = UIConstants.negativeColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010188", null);
		}

		// add value to general quality description (only for testing purpose?)
		inLabel.text += " (" + inValue.ToString("0.0") + ")";
	}

	public static void SetFuelStatString(TextMeshProUGUI inLabel, float modifier) {
		if (modifier > 0f)
		{
			inLabel.color = UIConstants.positiveColor;
			inLabel.text = string.Format("+{0}", modifier.ToString("##0"));
		}
		else if (modifier == 0f)
		{
			inLabel.color = Color.grey;
			inLabel.text = "-";
		}
		else
		{
			inLabel.color = UIConstants.negativeColor;
			inLabel.text = string.Format("{0}", modifier.ToString("##0"));
		}
	}

	public void OnMouseEnter()
	{
		Team team = Game.instance.player.team;
		if (!this.mSupplier.CanTeamBuyThis(team))
		{
			GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
			dialog.Open(Localisation.LocaliseID("PSG_10010193", null), Localisation.LocaliseID("PSG_10010194", null));
		}
	}

	public void OnMouseExit()
	{
		Team team = Game.instance.player.team;
		if (!this.mSupplier.CanTeamBuyThis(team))
		{
			GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
			dialog.Hide();
		}
	}

	public void OnDiscountDetailsEnter()
	{
		Team team = Game.instance.player.team;

		GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
		StringVariableParser.supplierOriginalPrice = this.mSupplier.GetPriceNoDiscount(team.championship, team.championship.rules.batterySize);
		StringVariableParser.supplierDiscountPercent = this.mSupplier.GetTeamDiscount(team);

		string header = string.Empty;
		string description = string.Empty;

		if (this.mSupplier.supplierType == Supplier.SupplierType.Engine) {
			header = "Engine Details";
			description += "<color=yellow>Engine-Type: " + this.mSupplier.model + "</color>";
		} else if (this.mSupplier.supplierType == Supplier.SupplierType.Materials && this.mSupplier.chassiDevelopmentEngineerBonus >= 0.001f) {
			header = "Chassi Details";
			description += "Engineer Bonus: " + this.mSupplier.chassiDevelopmentEngineerBonus;
			description += "\nDriver Bonus: " + this.mSupplier.chassiDevelopmentTestDriverBonus;
			description += "\nInvested Bonus: " + this.mSupplier.chassiDevelopmentInvestedMoney;
			if (!this.mSupplier.chassiIsAvailable)
				description += "\n<color=red>Development not finished, you need to invest at least 2 Mio above minimum in next year car</color>";
		}

		// add discount
		if (this.mSupplier.HasDiscountWithTeam(Game.instance.player.team)) {
			if (description != string.Empty)
				header = Localisation.LocaliseID("PSG_10010195", null);
			if (description != string.Empty)
				description += "\n";
			description += Localisation.LocaliseID("PSG_10010196", null);
		}

		dialog.Open(header, description);
	}

	public void OnDiscountDetailsExit()
	{
		Team team = Game.instance.player.team;
		GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
		dialog.Hide();
	}

	private bool showDetails ()
	{
		return (this.mSupplier.HasDiscountWithTeam(Game.instance.player.team) || this.mSupplier.supplierType == Supplier.SupplierType.Engine);
	}

	public UISupplierLogoWidget supplierLogoWidget;

	public TextMeshProUGUI statOne;

	public TextMeshProUGUI statTwo;

	public TextMeshProUGUI statThree;

	public TextMeshProUGUI description;

	public TextMeshProUGUI cost;

	public Toggle toggle;

	public GameObject discountDetails;

	public CanvasGroup canvasGroup;

	public GameObject[] statParentObjects;

	private UISupplierSelectionEntry mSupplierEntry;

	private Supplier mSupplier;
}
