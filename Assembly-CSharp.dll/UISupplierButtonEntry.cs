using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISupplierButtonEntry : MonoBehaviour
{
	public UISupplierButtonEntry()
	{
	}

	private void Start()
	{
		EventTrigger eventTrigger = base.gameObject.AddComponent<EventTrigger>();
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerEnter;
		entry.callback.AddListener(delegate(BaseEventData eventData)
		{
			this.OnMouseEnter();
		});
		eventTrigger.triggers.Add(entry);
		EventTrigger.Entry entry2 = new EventTrigger.Entry();
		entry2.eventID = EventTriggerType.PointerExit;
		entry2.callback.AddListener(delegate(BaseEventData eventData)
		{
			this.OnMouseExit();
		});
		eventTrigger.triggers.Add(entry2);
	}

	private void OnMouseEnter()
	{
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		screen.estimatedOutputWidget.HighlightBarsForStats(this.mHighlightStats);
		screen.ShowStatContibution(this.mSupplier);
	}

	private void OnMouseExit()
	{
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		screen.estimatedOutputWidget.ResetHighlightState();
		screen.HideStatContribution();
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
		}
		this.mHighlightStats.Clear();
		int num = 0;
		foreach (CarChassisStats.Stats stats in supplier.supplierStats.Keys)
		{
			this.mHighlightStats.Add(stats);
			num++;
		}
	}

	public void OnEnter()
	{
		this.nameLabel.text = Localisation.LocaliseEnum(this.supplierType);
		for (int i = 0; i < this.button.Length; i++)
		{
			this.button[i].onClick.AddListener(new UnityAction(this.OnButtonPressed));
		}
		this.mSupplier = null;
		this.UpdateSupplierState();
		this.SetHighlightStats();
	}

	private void OnButtonPressed()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		screen.HideStatContribution();
	}

	public void SetSupplier(Supplier inSupplier)
	{
		if (inSupplier != null)
		{
			this.mSupplier = inSupplier;
			this.logo.SetLogo(this.mSupplier);
		}
		this.UpdateSupplierState();
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		screen.preferencesWidget.UpdateBounds();
	}

	private void UpdateSupplierState()
	{
		if (this.mSupplier == null)
		{
			this.emptyContainer.SetActive(true);
			this.filledContainer.SetActive(false);
		}
		else
		{
			this.emptyContainer.SetActive(false);
			this.filledContainer.SetActive(true);
		}
	}

	public Supplier supplier
	{
		get
		{
			return this.mSupplier;
		}
	}

	public Supplier.SupplierType supplierType = Supplier.SupplierType.Brakes;

	public Button[] button;

	public TextMeshProUGUI nameLabel;

	public GameObject emptyContainer;

	public GameObject filledContainer;

	public UISupplierLogoWidget logo;

	private Supplier mSupplier;

	private List<CarChassisStats.Stats> mHighlightStats = new List<CarChassisStats.Stats>();
}
