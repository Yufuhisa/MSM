using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CarPartFittingScreen : UIScreen
{
	public CarPartFittingScreen()
	{
	}

	public override void OnStart()
	{
		base.OnStart();
		this.fitPartsButton.onClick.AddListener(new UnityAction(this.OnFitPartsButton));
		this.graphPerformanceToggle.isOn = true;
		this.graphPerformanceToggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnPerformanceToggle));
		this.graphReliabilityToggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnReliabilityToggle));
		this.PreloadScene();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Driver driverForCar = Game.instance.player.team.GetDriverForCar(0);
		Driver driverForCar2 = Game.instance.player.team.GetDriverForCar(1);
		this.driver1Panel.Setup(driverForCar);
		this.driver2Panel.Setup(driverForCar2);
		base.showNavigationBars = true;
		this.itemListWidget.Setup();
		Driver[] driversForCar = Game.instance.player.team.GetDriversForCar(0);
		for (int i = 0; i < this.driver1LegendLabel.Length; i++)
		{
			if (i < driversForCar.Length)
			{
				this.driver1LegendLabel[i].text = driversForCar[i].name;
				GameUtility.SetActive(this.driver1LegendLabel[i], true);
			}
			else
			{
				GameUtility.SetActive(this.driver1LegendLabel[i], false);
			}
		}
		driversForCar = Game.instance.player.team.GetDriversForCar(1);
		for (int j = 0; j < this.driver2LegendLabel.Length; j++)
		{
			if (j < driversForCar.Length)
			{
				this.driver2LegendLabel[j].text = driversForCar[j].name;
				GameUtility.SetActive(this.driver2LegendLabel[j], true);
			}
			else
			{
				GameUtility.SetActive(this.driver2LegendLabel[j], false);
			}
		}
		this.SetGraph();
		scSoundManager.Instance.PlaySound(SoundID.Sfx_TransitionFactory, 0f);
	}

	private void PreloadScene()
	{
		if (Game.IsActive() && !Game.instance.player.IsUnemployed())
		{
			Driver driverForCar = Game.instance.player.team.GetDriverForCar(0);
			Driver driverForCar2 = Game.instance.player.team.GetDriverForCar(1);
			this.driver1Panel.Setup(driverForCar);
			this.driver2Panel.Setup(driverForCar2);
			this.itemListWidget.Setup();
			this.SetGraph();
		}
	}

	public void RefreshCarInventoryWidgets()
	{
		this.driver1Panel.partsInventory.Refresh();
		this.driver2Panel.partsInventory.Refresh();
		this.driver1Panel.driverInfo.UpdateData();
		this.driver2Panel.driverInfo.UpdateData();
		this.itemListWidget.UpdateUnfitedParts();
		this.SetGraph();
	}

	public void Update()
	{
		if (!(App.instance.gameStateManager.currentState is TravelArrangementsState))
		{
			base.needsPlayerConfirmation = false;
		}
		else
		{
			base.needsPlayerConfirmation = !Game.instance.player.team.carManager.BothCarsReadyForEvent();
			if (App.instance.gameStateManager.currentState is TravelArrangementsState && base.needsPlayerConfirmation)
			{
				base.continueButtonLabel = Localisation.LocaliseID("PSG_10002122", null);
				base.continueButtonLowerLabel = Localisation.LocaliseID("PSG_10010987", null);
			}
			else if (App.instance.gameStateManager.currentState is TravelArrangementsState)
			{
				base.continueButtonLabel = Localisation.LocaliseID("PSG_10002122", null);
				base.continueButtonLowerLabel = string.Empty;
			}
		}
	}

	private void OnFitPartsButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		this.panelsAnimator.SetTrigger(AnimationHashes.ShowPartFittingPanel);
		this.itemListWidget.Open(CarPart.PartType.Brakes);
	}

	public override UIScreen.NavigationButtonEvent OnContinueButton()
	{
		return base.OnContinueButton();
	}

	public override void OpenConfirmDialogBox(Action inAction)
	{
		GenericConfirmation dialog = UIManager.instance.dialogBoxManager.GetDialog<GenericConfirmation>();
		Action inCancelAction = delegate()
		{
		};
		Action inConfirmAction = delegate()
		{
			CarManager carManager = Game.instance.player.team.carManager;
			if (!carManager.CarReadyForEvent(carManager.GetCar(0)) || !carManager.CarReadyForEvent(carManager.GetCar(1)))
			{
				carManager.AutofitBothCars();
			}
			this.needsPlayerConfirmation = !Game.instance.player.team.carManager.BothCarsReadyForEvent();
			if (inAction != null)
			{
				inAction.Invoke();
			}
			this.RefreshCarInventoryWidgets();
		};
		string inTitle = Localisation.LocaliseID("PSG_10003942", null);
		string inText = Localisation.LocaliseID("PSG_10003943", null);
		string inCancelString = Localisation.LocaliseID("PSG_10003944", null);
		string inConfirmString = Localisation.LocaliseID("PSG_10003945", null);
		dialog.Show(inCancelAction, inCancelString, inConfirmAction, inConfirmString, inText, inTitle);
	}

	private void SetGraph()
	{
		if (this.graphGT.gameObject.activeSelf)
		{
			this.graphGT.graphStat = ((!this.graphPerformanceToggle.isOn) ? UIRadarGraphWidget.Stat.Reliability : UIRadarGraphWidget.Stat.Performance);
			this.graphGT.UpdateGraphData();
		}
		if (this.graph.gameObject.activeSelf)
		{
			this.graph.graphStat = ((!this.graphPerformanceToggle.isOn) ? UIRadarGraphWidget.Stat.Reliability : UIRadarGraphWidget.Stat.Performance);
			this.graph.UpdateGraphData();
		}
		GameUtility.SetActive(this.headerPerformance, this.graphPerformanceToggle.isOn);
		GameUtility.SetActive(this.headerReliability, this.graphReliabilityToggle.isOn);
	}

	private void OnPerformanceToggle(bool inValue)
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (inValue)
		{
			this.SetGraph();
		}
	}

	private void OnReliabilityToggle(bool inValue)
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (inValue)
		{
			this.SetGraph();
		}
	}

	public UIPartFittingPanelWidget driver1Panel;

	public UIPartFittingPanelWidget driver2Panel;

	public UIPartFittingItemListWidget itemListWidget;

	public UIRadarGraphWidget graph;

	public UIRadarGraphWidget graphGT;

	public Toggle graphPerformanceToggle;

	public Toggle graphReliabilityToggle;

	public GameObject headerPerformance;

	public GameObject headerReliability;

	public Button fitPartsButton;

	public Animator panelsAnimator;

	public TextMeshProUGUI[] driver1LegendLabel;

	public TextMeshProUGUI[] driver2LegendLabel;
}
