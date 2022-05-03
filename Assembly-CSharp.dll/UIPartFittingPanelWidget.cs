using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIPartFittingPanelWidget : MonoBehaviour
{
	public UIPartFittingPanelWidget()
	{
	}

	private void Start()
	{
		this.autoPickButton.onClick.AddListener(new UnityAction(this.OnDropdownValueChanged));
	}

	private void OnDropdownValueChanged()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		this.mCar.carManager.AutoFit(this.mCar, CarManager.AutofitOptions.Performance, CarManager.AutofitAvailabilityOption.UnfitedParts);
		this.RefreshScreen();
	}

	private void RefreshScreen()
	{
		CarPartFittingScreen screen = UIManager.instance.GetScreen<CarPartFittingScreen>();
		screen.RefreshCarInventoryWidgets();
		this.driverInfo.SetHappinessData(this.mCar, this.mDriver, false);
	}

	public void Setup(Driver inDriver)
	{
		this.mDriver = inDriver;
		this.mCar = Game.instance.player.team.carManager.GetCarForDriver(this.mDriver);
		this.driverInfo.Setup(this.mDriver);
		this.partsInventory.Setup(this.mCar);
	}

	public UIPanelDriverInfo driverInfo;

	public UIPanelPartsInventory partsInventory;

	public Button autoPickButton;

	private Driver mDriver;

	private Car mCar;
}
