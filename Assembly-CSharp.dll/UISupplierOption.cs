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
		GameUtility.SetActive(this.discountDetails, this.mSupplier.HasDiscountWithTeam(team));
		if (this.mSupplier.CanTeamBuyThis(team))
		{
			this.canvasGroup.alpha = 1f;
			this.canvasGroup.interactable = true;
		}
		else
		{
			this.canvasGroup.alpha = 0.2f;
			this.canvasGroup.interactable = false;
		}
		if (this.mSupplier.supplierType == Supplier.SupplierType.Fuel)
		{
			GameUtility.SetActive(this.statParentObjects[2], false);
		}
	}

	public static void SetupSupplierDetails(Supplier inSupplier, UISupplierLogoWidget inSupplierLogoWidget, GameObject[] inParentObjects, params TextMeshProUGUI[] inStatLabels)
	{
		inSupplierLogoWidget.SetLogo(inSupplier);
		switch (inSupplier.supplierType)
		{
		case Supplier.SupplierType.Engine:
		case Supplier.SupplierType.Fuel:
			UISupplierOption.SetStatString(inStatLabels[0], inSupplier.supplierStats[CarChassisStats.Stats.FuelEfficiency]);
			UISupplierOption.SetStatString(inStatLabels[1], inSupplier.supplierStats[CarChassisStats.Stats.Improvability]);
			inStatLabels[2].text = ((inSupplier.supplierType != Supplier.SupplierType.Engine) ? "-" : string.Format("+{0}", inSupplier.randomEngineLevelModifier.ToString()));
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
		if (inValue > 6f)
		{
			inLabel.color = UIConstants.positiveColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010192", null);
		}
		else if (inValue > 5f)
		{
			inLabel.color = UIConstants.positiveColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010191", null);
		}
		else if (inValue > 4f)
		{
			inLabel.color = UIConstants.mailMedia;
			inLabel.text = Localisation.LocaliseID("PSG_10010190", null);
		}
		else if (inValue > 3f)
		{
			inLabel.color = UIConstants.negativeColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010189", null);
		}
		else
		{
			inLabel.color = UIConstants.negativeColor;
			inLabel.text = Localisation.LocaliseID("PSG_10010188", null);
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
		if (this.mSupplier.HasDiscountWithTeam(team))
		{
			GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
			StringVariableParser.supplierOriginalPrice = this.mSupplier.GetPriceNoDiscount(team.championship, team.championship.rules.batterySize);
			StringVariableParser.supplierDiscountPercent = this.mSupplier.GetTeamDiscount(team);
			dialog.Open(Localisation.LocaliseID("PSG_10010195", null), Localisation.LocaliseID("PSG_10010196", null));
		}
	}

	public void OnDiscountDetailsExit()
	{
		Team team = Game.instance.player.team;
		if (this.mSupplier.HasDiscountWithTeam(team))
		{
			GenericInfoRollover dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>();
			dialog.Hide();
		}
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