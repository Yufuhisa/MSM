using System;
using TMPro;
using UnityEngine;

public class UIComponentsChoiceWidget : MonoBehaviour
{
	public UIComponentsChoiceWidget()
	{
	}

	public void Setup()
	{
		PartDesignScreen screen = UIManager.instance.GetScreen<PartDesignScreen>();
		this.mDesign = Game.instance.player.team.carManager.carPartDesign;
		this.engineerComponentsWidget.Setup(screen.partType);
		for (int i = 0; i < this.componentsWidget.Length; i++)
		{
			this.componentsWidget[i].Setup();
		}
		StringVariableParser.partFrontendUI = screen.partType;
		this.partTitle.text = Localisation.LocaliseID("PSG_10010946", null);
		this.componentsTotal.text = "/" + (this.mDesign.componentSlots.Count + this.mDesign.componentBonusSlots.Count).ToString();
		this.SetIcon(screen.partType);
		CarPartDesign carPartDesign = this.mDesign;
		carPartDesign.OnDesignModified = (Action)Delegate.Combine(carPartDesign.OnDesignModified, new Action(this.UpdateComponentFitted));
		this.UpdateComponentFitted();
	}

	private void OnDisable()
	{
		if (Game.IsActive() && this.mDesign != null)
		{
			CarPartDesign carPartDesign = this.mDesign;
			carPartDesign.OnDesignModified = (Action)Delegate.Remove(carPartDesign.OnDesignModified, new Action(this.UpdateComponentFitted));
		}
	}

	private void UpdateComponentFitted()
	{
		if (this.mDesign.part != null)
		{
			this.componentsSelected.text = this.mDesign.part.GetComponentsFittedCount().ToString();
			this.emptySlotsContainer.SetActive(this.mDesign.part.hasComponentSlotsAvailable);
			int numberOfSlots = this.mDesign.GetNumberOfSlots(this.mDesign.part.GetPartType());
			for (int i = 0; i < this.componentsWidget.Length; i++)
			{
				UIComponentLevelWidget uicomponentLevelWidget = this.componentsWidget[i];
				if (numberOfSlots > uicomponentLevelWidget.level || (!this.mDesign.part.hasComponentSlotsAvailable && numberOfSlots == uicomponentLevelWidget.level))
				{
					this.componentsWidget[i].Show();
				}
				else
				{
					this.componentsWidget[i].Hide();
				}
			}
		}
	}

	private void SetIcon(CarPart.PartType inType)
	{
		for (int i = 0; i < this.iconTransform.childCount; i++)
		{
			if (i == (int)inType)
			{
				this.iconTransform.GetChild(i).gameObject.SetActive(true);
			}
			else
			{
				this.iconTransform.GetChild(i).gameObject.SetActive(false);
			}
		}
	}

	public void ResetChoices()
	{
		for (int i = 0; i < this.componentsWidget.Length; i++)
		{
			this.componentsWidget[i].ResetChoices();
		}
	}

	public UIComponentLevelWidget[] componentsWidget = new UIComponentLevelWidget[0];

	public UIEngineerComponentsWidget engineerComponentsWidget;

	public TextMeshProUGUI partTitle;

	public TextMeshProUGUI componentsSelected;

	public TextMeshProUGUI componentsTotal;

	public GameObject emptySlotsContainer;

	public Transform iconTransform;

	private CarPartDesign mDesign;
}
