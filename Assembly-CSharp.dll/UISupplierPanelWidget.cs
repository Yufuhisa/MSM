using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISupplierPanelWidget : MonoBehaviour
{
	public UISupplierPanelWidget()
	{
	}

	private void Start()
	{
		this.selectSupplierButton.onClick.AddListener(new UnityAction(this.Close));
		this.leftSupplierButton.onClick.AddListener(new UnityAction(this.OnLeftButton));
		this.rightSupplierButton.onClick.AddListener(new UnityAction(this.OnRightButton));
	}

	private void OnLeftButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (!this.mRightAnim)
		{
			this.mSupplierIndex--;
		}
		this.mRightAnim = false;
		this.mSupplierIndex = this.LoopIndex(this.mSupplierIndex);
		this.supplierAnimator.SetTrigger(AnimationHashes.ChangeSupplierRight);
		this.SetEntryToIndex(this.mSupplierIndex);
	}

	private void OnRightButton()
	{
		scSoundManager.Instance.PlaySound(SoundID.Button_Select, 0f);
		if (this.mRightAnim)
		{
			this.mSupplierIndex++;
		}
		this.mRightAnim = true;
		this.mSupplierIndex = this.LoopIndex(this.mSupplierIndex);
		this.supplierAnimator.SetTrigger(AnimationHashes.ChangeSupplierLeft);
		this.SetEntryToIndex(this.mSupplierIndex);
	}

	private void SetEntryToIndex(int inIndex)
	{
		this.supplierOptions[0].Setup(this.mSuppliers[this.LoopIndex(inIndex - 2)], this.mSuppliers[inIndex].supplierType);
		this.supplierOptions[1].Setup(this.mSuppliers[this.LoopIndex(inIndex - 1)], this.mSuppliers[inIndex].supplierType);
		this.supplierOptions[2].Setup(this.mSuppliers[inIndex], this.mSuppliers[inIndex].supplierType);
		this.supplierOptions[3].Setup(this.mSuppliers[this.LoopIndex(inIndex + 1)], this.mSuppliers[inIndex].supplierType);
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		int num = inIndex;
		if (!this.mRightAnim)
		{
			num = this.LoopIndex(num - 1);
		}
		screen.SetSupplier(this.mSuppliers[num].supplierType, this.mSuppliers[num]);
		this.UpdateEnabledGFX(this.mSuppliers[num].supplierType);
		for (int i = 0; i < this.mSuppliers.Count; i++)
		{
			Transform orCreateItem = this.dotList.GetOrCreateItem<Transform>(i);
			if (num == i)
			{
				orCreateItem.GetComponent<Image>().color = Game.instance.player.GetTeamColor().primaryUIColour.normal;
			}
			else
			{
				orCreateItem.GetComponent<Image>().color = this.emptyDotColor;
			}
		}
	}

	private int LoopIndex(int inIndex)
	{
		if (inIndex > this.mSuppliers.Count - 1)
		{
			return 0;
		}
		if (inIndex < 0)
		{
			return this.mSuppliers.Count - 1;
		}
		return inIndex;
	}

	public void OnEnter()
	{
		this.SetupEngineerData();
		this.Close();
	}

	private void SetupEngineerData()
	{
		Engineer engineer = (Engineer)Game.instance.player.team.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		this.engineerName.text = engineer.name;
		this.flag.SetNationality(engineer.nationality);
		this.portrait.SetPortrait(engineer);
		this.stars.SetAbilityStarsData(engineer);
	}

	public void Setup(Supplier.SupplierType inType)
	{
		this.SetupSupplierOptions(inType);
		string inID = string.Empty;
		switch (inType)
		{
		case Supplier.SupplierType.Engine:
			inID = "PSG_10004267";
			break;
		case Supplier.SupplierType.Brakes:
			inID = "PSG_10004266";
			break;
		case Supplier.SupplierType.Fuel:
			inID = "PSG_10004268";
			break;
		case Supplier.SupplierType.Materials:
			inID = "PSG_10004265";
			break;
		}
		this.engineerComment.text = Localisation.LocaliseID(inID, null);
	}

	public void SetupSupplierOptions(Supplier.SupplierType inType)
	{
		this.mRightAnim = true;
		this.mSuppliers = new List<Supplier>();
		switch (inType)
		{
		case Supplier.SupplierType.Engine:
			this.mSuppliers = Game.instance.supplierManager.engineSuppliers;
			break;
		case Supplier.SupplierType.Brakes:
			this.mSuppliers = Game.instance.supplierManager.brakesSuppliers;
			break;
		case Supplier.SupplierType.Fuel:
			this.mSuppliers = Game.instance.supplierManager.fuelSuppliers;
			break;
		case Supplier.SupplierType.Materials:
			this.mSuppliers = Game.instance.supplierManager.materialsSuppliers;
			break;
		}
		this.chooseSupplierLabel.text = "Choose a " + Localisation.LocaliseEnum(inType) + " Supplier";
		this.chooseButtonSupplierLabel.text = "Select " + Localisation.LocaliseEnum(inType) + " Supplier";
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		Supplier supplier = screen.supplierList[(int)inType];
		this.mSupplierIndex = ((supplier == null) ? 0 : this.LoopIndex(this.mSuppliers.IndexOf(supplier)));
		this.mHighlightStats.Clear();
		int num = 0;
		foreach (CarChassisStats.Stats stats in this.mSuppliers[0].supplierStats.Keys)
		{
			this.mHighlightStats.Add(stats);
			num++;
		}
		this.SetEntryToIndex(this.mSupplierIndex);
		this.UpdateEnabledGFX(inType);
		for (int i = 0; i < this.dotList.itemCount; i++)
		{
			Transform item = this.dotList.GetItem<Transform>(i);
			if (i < this.mSuppliers.Count)
			{
				item.gameObject.SetActive(true);
			}
			else
			{
				item.gameObject.SetActive(false);
			}
		}
	}

	public void HighlightStats(bool inValue)
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

	private void Update()
	{
		this.HighlightStats(true);
	}

	public void UpdateEnabledGFX(Supplier.SupplierType inType)
	{
		CarDesignScreen screen = UIManager.instance.GetScreen<CarDesignScreen>();
		for (int i = 0; i < this.supplierOptions.Length; i++)
		{
			this.supplierOptions[i].selectedGFX.SetActive(this.supplierOptions[i].supplier == screen.supplierList[(int)inType]);
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(false);
		this.HighlightStats(false);
	}

	public void Open()
	{
		base.gameObject.SetActive(true);
	}

	public TextMeshProUGUI chooseSupplierLabel;

	public TextMeshProUGUI chooseButtonSupplierLabel;

	public TextMeshProUGUI engineerName;

	public Flag flag;

	public UICharacterPortrait portrait;

	public UIAbilityStars stars;

	public TextMeshProUGUI engineerComment;

	public UISupplierOptionEntry[] supplierOptions = new UISupplierOptionEntry[0];

	public Button selectSupplierButton;

	public Button leftSupplierButton;

	public Button rightSupplierButton;

	public UIGridList dotList;

	public Color emptyDotColor = default(Color);

	public Animator supplierAnimator;

	private List<CarChassisStats.Stats> mHighlightStats = new List<CarChassisStats.Stats>();

	private List<Supplier> mSuppliers = new List<Supplier>();

	private int mSupplierIndex;

	private bool mRightAnim;
}
