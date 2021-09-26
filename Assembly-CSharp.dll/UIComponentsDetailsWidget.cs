using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIComponentsDetailsWidget : MonoBehaviour
{
	public UIComponentsDetailsWidget()
	{
	}

	private void Awake()
	{
	}

	private void OnCancelDesign()
	{
		this.mScreen.CancelDesign();
	}

	public void HighLightSlots(int inLevel)
	{
		for (int i = 0; i < this.slotEntries.Count; i++)
		{
			this.slotEntries[i].highLight.SetActive(i >= inLevel);
		}
		if (this.mDesign.componentBonusSlotsLevel.Count > 0)
		{
			for (int j = 0; j < this.bonusSlotEntries.Count; j++)
			{
				if (j >= this.mDesign.componentBonusSlotsLevel.Count)
				{
					break;
				}
				this.bonusSlotEntries[j].highLight.SetActive(this.mDesign.componentBonusSlotsLevel[j] >= inLevel);
			}
			if (this.mDesign.componentBonusSlotsLevel.Count > this.bonusSlotEntries.Count)
			{
				global::Debug.LogError("Not enought UI slots for all bonus components allocated to part", null);
			}
		}
	}

	public void Setup()
	{
		this.mScreen = UIManager.instance.GetScreen<PartDesignScreen>();
		for (int i = 0; i < this.animatedFloats.Length; i++)
		{
			this.animatedFloats[i] = new AnimatedFloat();
		}
		this.RefreshUI();
	}

	private void OnEnable()
	{
		if (Game.IsActive())
		{
			CarPartDesign carPartDesign = Game.instance.player.team.carManager.carPartDesign;
			carPartDesign.OnDesignModified = (Action)Delegate.Combine(carPartDesign.OnDesignModified, new Action(this.RefreshUI));
		}
	}

	private void OnDisable()
	{
		if (Game.IsActive())
		{
			CarPartDesign carPartDesign = Game.instance.player.team.carManager.carPartDesign;
			carPartDesign.OnDesignModified = (Action)Delegate.Remove(carPartDesign.OnDesignModified, new Action(this.RefreshUI));
		}
	}

	private void RefreshUI()
	{
		this.mDesign = Game.instance.player.team.carManager.carPartDesign;
		for (int i = 0; i < this.mDesign.componentSlots.Count; i++)
		{
			CarPartComponent carPartComponent = this.mDesign.componentSlots[i];
			if (carPartComponent != null)
			{
				this.slotEntries[i].Setup(UIComponentSlotEntry.State.Used, carPartComponent, i);
			}
			else
			{
				this.slotEntries[i].Setup(UIComponentSlotEntry.State.Empty, null, i);
			}
		}
		for (int j = this.mDesign.componentSlots.Count; j < this.slotEntries.Count; j++)
		{
			this.slotEntries[j].Setup(UIComponentSlotEntry.State.Locked, null, j);
		}
		for (int k = 0; k < this.mDesign.componentBonusSlots.Count; k++)
		{
			if (k >= this.bonusSlotEntries.Count)
			{
				global::Debug.LogError("Not enought UI slots for all bonus components allocated to part", null);
				break;
			}
			CarPartComponent carPartComponent2 = this.mDesign.componentBonusSlots[k];
			if (carPartComponent2 != null)
			{
				this.bonusSlotEntries[k].Setup(UIComponentSlotEntry.State.Used, carPartComponent2, this.mDesign.componentBonusSlotsLevel[k] - 1);
			}
			else
			{
				this.bonusSlotEntries[k].Setup(UIComponentSlotEntry.State.Empty, null, this.mDesign.componentBonusSlotsLevel[k] - 1);
			}
			GameUtility.SetActive(this.bonusSlotEntries[k].gameObject, true);
		}
		for (int l = this.mDesign.componentBonusSlots.Count; l < this.bonusSlotEntries.Count; l++)
		{
			GameUtility.SetActive(this.bonusSlotEntries[l].gameObject, false);
		}
		this.SetPartData(this.mDesign.part);
	}

	private void SetPartData(CarPart inPart)
	{
		Color partLevelColor = UIConstants.GetPartLevelColor(inPart.stats.level);
		this.mainStatFill.color = partLevelColor;
		this.mainStatLegend.color = partLevelColor;
		this.mainStatLabel.color = partLevelColor;
		partLevelColor.a = 0.8f;
		this.mainMaxStatFill.color = partLevelColor;
		this.perfomanceStatLegend.color = partLevelColor;
		this.mainStatMaxLabel.color = partLevelColor;
		StringVariableParser.stringValue1 = inPart.stats.rulesRiskString;
		this.rulesRiskAmount.text = Localisation.LocaliseID("PSG_10010428", null);
		this.SetAnimatedFloats(inPart);
		this.mainStatNameLabel.text = Localisation.LocaliseEnum(inPart.stats.statType);
		this.reliabilityLabel.text = Mathf.Round(inPart.stats.reliability * 100f) + "%";
		this.reliabilityMaxLabel.text = "/" + Mathf.Round(inPart.stats.GetMaxReliability() * 100f) + "%";
		this.mainStatLabel.text = inPart.stats.statWithPerformance.ToString("0", Localisation.numberFormatter);
		this.mainStatMaxLabel.text = "/" + (inPart.stats.stat + Mathf.Max(0f, inPart.stats.maxPerformance)).ToString("0", Localisation.numberFormatter);
		TimeSpan componentsDesignDurationBonus = this.mDesign.GetComponentsDesignDurationBonus();
		this.timeComponentAdjustmentContainer.SetActive(componentsDesignDurationBonus.TotalDays != 0.0);
		StringVariableParser.ordinalNumberString = componentsDesignDurationBonus.TotalDays.ToString("N0");
		this.timeComponentAdjustmentText.text = Localisation.LocaliseID("PSG_10010377", null);
		TimeSpan designDuration = this.mDesign.GetDesignDuration();
		RaceEventDetails currentEventDetails = Game.instance.player.team.championship.GetCurrentEventDetails();
		if (!currentEventDetails.hasEventEnded)
		{
			this.buildTimeForEvent.text = UIPartDevStatImprovementWidget.GetDateText(Game.instance.time.now + designDuration) + " " + UIPartDevStatImprovementWidget.GetEventText(Game.instance.time.now + designDuration);
		}
		else
		{
			this.buildTimeForEvent.text = string.Empty;
		}
		StringVariableParser.ordinalNumberString = designDuration.TotalDays.ToString("0.#");
		string text = Localisation.LocaliseID("PSG_10010377", null);
		if (this.buildTime.text != text)
		{
			this.timeAnimator.SetTrigger(AnimationHashes.Highlight);
		}
		this.buildTime.text = text;
		this.costComponentAdjustmentContainer.SetActive(this.mDesign.GetComponentDesignCostBonus() != 0);
		this.costComponentAdjustmentText.text = GameUtility.GetCurrencyString((long)this.mDesign.GetComponentDesignCostBonus(), 0);
		if (this.buildCost.text != GameUtility.GetCurrencyString((long)this.mDesign.GetDesignCost(), 0))
		{
			this.costAnimator.SetTrigger(AnimationHashes.Highlight);
		}
		this.buildCost.text = GameUtility.GetCurrencyString((long)this.mDesign.GetDesignCost(), 0);
		bool active = false;
		int num = 0;
		for (int i = 0; i < inPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = inPart.components[i];
			if (carPartComponent != null && !carPartComponent.IgnoreBonusForUI() && (carPartComponent.HasActivationRequirement() || carPartComponent.bonuses.Count != 0))
			{
				string name = carPartComponent.GetName(inPart);
				active = true;
				this.AddBonusText(name, num, carPartComponent);
				num++;
			}
		}
		this.bonusesContainerData.SetActive(active);
		for (int j = num; j < this.bonusesEntries.Count; j++)
		{
			this.bonusesEntries[j].gameObject.SetActive(false);
		}
		this.partEntry.Setup(inPart);
	}

	private void SetAnimatedFloats(CarPart inPart)
	{
		CarPartInventory partInventory = Game.instance.player.team.carManager.partInventory;
		float inDuration = 0.5f;
		float inDelay = 0f;
		float inValue = CarPartStats.GetNormalizedStatValue(inPart.stats.stat + inPart.stats.maxPerformance, inPart.GetPartType(), null);
		this.animatedFloats[0].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
		inValue = CarPartStats.GetNormalizedStatValue(inPart.stats.statWithPerformance, inPart.GetPartType(), null);
		this.animatedFloats[1].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
		inValue = CarPartStats.GetNormalizedStatValue(partInventory.GetHighestStatOfType(inPart.GetPartType(), CarPartStats.CarPartStat.MainStat), inPart.GetPartType(), null);
		this.animatedFloats[2].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
		inValue = inPart.stats.reliability;
		this.animatedFloats[3].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
		inValue = inPart.stats.partCondition.redZone;
		this.animatedFloats[4].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
		inValue = inPart.stats.GetMaxReliability();
		this.animatedFloats[5].SetValue(inValue, AnimatedFloat.Animation.Animate, inDelay, inDuration, EasingUtility.Easing.InOutSin);
	}

	private void Update()
	{
		for (int i = 0; i < this.animatedFloats.Length; i++)
		{
			if (this.animatedFloats[i] == null)
			{
				return;
			}
			this.animatedFloats[i].Update();
		}
		GameUtility.SetImageFillAmountIfDifferent(this.mainMaxStatFill, this.animatedFloats[0].value, 0.001953125f);
		GameUtility.SetImageFillAmountIfDifferent(this.mainStatFill, this.animatedFloats[1].value, 0.001953125f);
		this.currentPartStatSlider.value = this.animatedFloats[2].value;
		this.reliabilitySlider.normalizedValue = this.animatedFloats[3].value;
		this.redZoneSlider.normalizedValue = this.animatedFloats[4].value;
		this.maxReliabilityFill.fillAmount = this.animatedFloats[5].value;
	}

	public void ShowTooltip(string inType)
	{
		string inHeader = string.Empty;
		string inDescription = string.Empty;
		if (inType != null)
		{
			switch (inType)
			{
			case "Performance":
				inHeader = Localisation.LocaliseID("PSG_10010510", null);
				inDescription = this.mDesign.GetPerformanceBreakdown();
				break;
			case "Reliability":
				inHeader = Localisation.LocaliseID("PSG_10010511", null);
				inDescription = this.mDesign.GetReliabilityBreakdown();
				break;
			case "Cost":
				inHeader = Localisation.LocaliseID("PSG_10010512", null);
				inDescription = this.mDesign.GetCostBreakdown();
				break;
			case "Time":
				inHeader = Localisation.LocaliseID("PSG_10010513", null);
				inDescription = this.mDesign.GetDesignTimeBreakdown();
				break;
			case "SlotLocked1":
				this.SetSlotLockedData(out inHeader, out inDescription, 1);
				break;
			case "SlotLocked2":
				this.SetSlotLockedData(out inHeader, out inDescription, 2);
				break;
			case "SlotLocked3":
				this.SetSlotLockedData(out inHeader, out inDescription, 3);
				break;
			case "SlotLocked4":
				this.SetSlotLockedData(out inHeader, out inDescription, 4);
				break;
			case "SlotLocked5":
				this.SetSlotLockedData(out inHeader, out inDescription, 5);
				break;
			}
		}
		UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(inHeader, inDescription);
	}

	private void SetSlotLockedData(out string outHeader, out string outDescription, int inIndex)
	{
		outHeader = Localisation.LocaliseID("PSG_10010504", null);
		switch (inIndex)
		{
		case 1:
			outDescription = Localisation.LocaliseID("PSG_10010505", null);
			break;
		case 2:
			outDescription = Localisation.LocaliseID("PSG_10010506", null);
			break;
		case 3:
			outDescription = Localisation.LocaliseID("PSG_10010507", null);
			break;
		case 4:
			outDescription = Localisation.LocaliseID("PSG_10010508", null);
			break;
		case 5:
			outDescription = Localisation.LocaliseID("PSG_10010509", null);
			break;
		default:
			outDescription = string.Empty;
			break;
		}
	}

	public void HideTooltip()
	{
		UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Close();
	}

	private void AddBonusText(string inDescription, int inIndex, CarPartComponent inComponent)
	{
		UIAdditionalBonusEntry uiadditionalBonusEntry;
		if (this.bonusesEntries.Count <= inIndex)
		{
			uiadditionalBonusEntry = UnityEngine.Object.Instantiate<UIAdditionalBonusEntry>(this.bonusesEntries[0]);
			uiadditionalBonusEntry.transform.SetParent(this.bonusesEntries[0].transform.parent, false);
			this.bonusesEntries.Add(uiadditionalBonusEntry);
		}
		else
		{
			uiadditionalBonusEntry = this.bonusesEntries[inIndex];
		}
		uiadditionalBonusEntry.Setup(inComponent);
		uiadditionalBonusEntry.gameObject.SetActive(true);
		uiadditionalBonusEntry.text.text = inDescription;
	}

	public List<UIComponentSlotEntry> slotEntries = new List<UIComponentSlotEntry>();

	public List<UIComponentSlotEntry> bonusSlotEntries = new List<UIComponentSlotEntry>();

	public TextMeshProUGUI rulesRiskAmount;

	public TextMeshProUGUI mainStatNameLabel;

	public Image mainStatFill;

	public Image mainMaxStatFill;

	public Slider currentPartStatSlider;

	public Image mainStatLegend;

	public Image perfomanceStatLegend;

	public TextMeshProUGUI mainStatLabel;

	public TextMeshProUGUI mainStatMaxLabel;

	public Slider reliabilitySlider;

	public Slider redZoneSlider;

	public Image maxReliabilityFill;

	public TextMeshProUGUI reliabilityLabel;

	public TextMeshProUGUI reliabilityMaxLabel;

	public TextMeshProUGUI buildTime;

	public TextMeshProUGUI buildTimeForEvent;

	public TextMeshProUGUI buildCost;

	public GameObject timeComponentAdjustmentContainer;

	public GameObject costComponentAdjustmentContainer;

	public TextMeshProUGUI timeComponentAdjustmentText;

	public TextMeshProUGUI costComponentAdjustmentText;

	public GameObject bonusesContainerData;

	public List<UIAdditionalBonusEntry> bonusesEntries = new List<UIAdditionalBonusEntry>();

	public UICarPartEntry partEntry;

	public Animator costAnimator;

	public Animator timeAnimator;

	private AnimatedFloat[] animatedFloats = new AnimatedFloat[6];

	private PartDesignScreen mScreen;

	private CarPartDesign mDesign;
}
