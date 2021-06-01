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
		
		List<Supplier> supplierList = Game.instance.supplierManager.GetSupplierList(this.supplierType);
		if (supplierList.Count > 0)
			supplier = supplierList[0];

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
