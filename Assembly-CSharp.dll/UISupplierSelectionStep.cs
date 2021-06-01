using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISupplierSelectionStep : MonoBehaviour
{
	public UISupplierSelectionStep()
	{
	}

	public bool isComplete
	{
		get
		{
			return this.mIsComplete;
		}
	}

	public void OnStart(UISupplierSelectionEntry inOptionWidget)
	{
		this.mStepOptionsWidget = inOptionWidget.gameObject;
		inOptionWidget.OnStart(this.supplierType, this);
		GameUtility.SetActive(this.tick, false);
		this.toggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnToggle));
		GameUtility.SetActive(this.mStepOptionsWidget, false);
		this.SetHighlightStats();
	}

	public void OnEnter()
	{
		this.SetComplete(false);
		GameUtility.SetActive(this.mStepOptionsWidget, false);
	}

	private void OnToggle(bool inValue)
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		GameUtility.SetActiveAndCheckNull(this.mStepOptionsWidget, inValue);
		this.UpdateHighlight(inValue);
	}

	public void SetComplete(bool inValue)
	{
		this.mIsComplete = inValue;
		GameUtility.SetActive(this.tick, this.mIsComplete);
		this.suppliersSelectionWidget.NotifyStepComplete();
	}

	public void OnDisable()
	{
		this.toggle.isOn = false;
		this.OnToggle(false);
	}

	private void UpdateHighlight(bool inValue)
	{
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		if (inValue)
		{
			screen.estimatedOutputWidget.HighlightBarsForStats(this.mHighlightStats);
		}
		else
		{
			screen.estimatedOutputWidget.ResetHighlightState();
		}
	}

	private void SetHighlightStats()
	{
		Supplier supplier = null;
		switch (this.supplierType)
		{
		case Supplier.SupplierType.Engine:
			supplier = Game.instance.supplierManager.engineSuppliers[0];
			break;
		case Supplier.SupplierType.Brakes:
			supplier = Game.instance.supplierManager.brakesSuppliers[0];
			break;
		case Supplier.SupplierType.Fuel:
			supplier = Game.instance.supplierManager.fuelSuppliers[0];
			break;
		case Supplier.SupplierType.Materials:
			supplier = Game.instance.supplierManager.materialsSuppliers[0];
			break;
		case Supplier.SupplierType.Battery:
			supplier = Game.instance.supplierManager.batterySuppliers[0];
			break;
		case Supplier.SupplierType.ERSAdvanced:
			if (Game.instance.supplierManager.ersAdvancedSuppliers.Count > 0)
			{
				supplier = Game.instance.supplierManager.ersAdvancedSuppliers[0];
			}
			break;
		}
		if (supplier != null)
		{
			this.mHighlightStats.Clear();
			foreach (CarChassisStats.Stats stats in supplier.supplierStats.Keys)
			{
				this.mHighlightStats.Add(stats);
			}
		}
	}

	public Supplier.SupplierType supplierType = Supplier.SupplierType.Brakes;

	public Toggle toggle;

	public GameObject tick;

	public UISupplierSelectionWidget suppliersSelectionWidget;

	private GameObject mStepOptionsWidget;

	private bool mIsComplete;

	private List<CarChassisStats.Stats> mHighlightStats = new List<CarChassisStats.Stats>();
}
