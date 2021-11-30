using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISupplierSelectionEntry : MonoBehaviour
{
	public UISupplierSelectionEntry()
	{
	}

	public Supplier selectedSupplier
	{
		get
		{
			return this.mSelectedSupplier;
		}
	}

	public void OnStart(Supplier.SupplierType inSupplierType, UISupplierSelectionStep inStep)
	{
		this.mSupplierType = inSupplierType;
		this.mSelectionStep = inStep;
	}

	public void OnEnter()
	{
		List<Supplier> suppliersForTeam = Game.instance.supplierManager.GetSuppliersForTeam(this.mSupplierType, Game.instance.player.team, true);
		this.supplierOptionsEntries.HideListItems();
		ToggleGroup component = this.supplierOptionsEntries.grid.GetComponent<ToggleGroup>();
		for (int i = 0; i < suppliersForTeam.Count; i++)
		{
			UISupplierOption orCreateItem = this.supplierOptionsEntries.GetOrCreateItem<UISupplierOption>(i);
			orCreateItem.Setup(this, suppliersForTeam[i], component);
		}
		this.SetupHeaderLabels();
		this.mSelectedSupplier = null;
	}

	private void SetupHeaderLabels()
	{
		switch (this.mSupplierType)
		{
		case Supplier.SupplierType.Engine:
		case Supplier.SupplierType.Fuel:
			this.statOne.text = Localisation.LocaliseID("PSG_10004256", null);
			this.statTwo.text = Localisation.LocaliseID("PSG_10004258", null);
			GameUtility.SetActive(this.thirdStatOption, true);
			this.statThree.text = Localisation.LocaliseID("PSG_10011045", null);
			break;
		case Supplier.SupplierType.Brakes:
		case Supplier.SupplierType.Materials:
			this.statOne.text = Localisation.LocaliseID("PSG_10004259", null);
			this.statTwo.text = Localisation.LocaliseID("PSG_10004260", null);
			GameUtility.SetActive(this.thirdStatOption, false);
			break;
		case Supplier.SupplierType.Battery:
			GameUtility.SetActive(this.thirdStatOption, false);
			this.statOne.text = Localisation.LocaliseID("PSG_10011518", null);
			this.statTwo.text = Localisation.LocaliseID("PSG_10011520", null);
			break;
		}
	}

	public void SupplierSelected(Supplier inSelectedSupplier)
	{
		this.mSelectedSupplier = inSelectedSupplier;
		this.mSelectionStep.SetComplete(true);
	}

	public UIGridList supplierOptionsEntries;

	public TextMeshProUGUI statOne;

	public TextMeshProUGUI statTwo;

	public TextMeshProUGUI statThree;

	public GameObject thirdStatOption;

	private Supplier.SupplierType mSupplierType = Supplier.SupplierType.Brakes;

	private UISupplierSelectionStep mSelectionStep;

	private Supplier mSelectedSupplier;
}
