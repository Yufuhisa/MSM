using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPartInfoPopupWidget : UIDialogBox
{
	public UIPartInfoPopupWidget()
	{
	}

	protected override void Awake()
	{
		base.Awake();
		this.mInitialDateColor = this.partBuildDate.color;
		UIManager.OnScreenChange += new Action(this.HideTooltip);
	}

	public void OnDestroy()
	{
		UIManager.OnScreenChange -= new Action(this.HideTooltip);
	}

	public void ShowTooltip(CarPart inPart)
	{
		this.ShowTooltip(inPart, null, UIPartInfoPopupWidget.Mode.Normal, null);
	}

	public void ShowTooltip(CarPart inPart, RectTransform inTransform, UIPartInfoPopupWidget.Mode inMode = UIPartInfoPopupWidget.Mode.Normal, List<CarPartComponentBonus> inBonuses = null)
	{
		this.mBonuses = inBonuses;
		this.mMode = inMode;
		for (int i = 0; i < this.modeData.Length; i++)
		{
			for (int j = 0; j < this.modeData[i].containers.Length; j++)
			{
				GameUtility.SetActive(this.modeData[i].containers[j], this.modeData[i].mode == this.mMode);
			}
		}
		scSoundManager.BlockSoundEvents = true;
		this.mRefTransform = inTransform;
		this.mTransform = base.GetComponent<RectTransform>();
		GameUtility.SetTooltipTransformInsideScreen(this.mTransform, this.mRefTransform, default(Vector3), false, null);
		this.Setup(inPart);
		base.gameObject.SetActive(true);
		scSoundManager.BlockSoundEvents = false;
	}

	private void Update()
	{
		GameUtility.SetTooltipTransformInsideScreen(this.mTransform, this.mRefTransform, default(Vector3), false, null);
	}

	public void HideTooltip()
	{
		base.gameObject.SetActive(false);
	}

	private void Setup(CarPart inPart)
	{
		this.SetIcon(inPart.GetPartType());
		this.statName.text = Localisation.LocaliseEnum(inPart.stats.statType);
		this.partType.text = Localisation.LocaliseEnum(inPart.GetPartType());
		Color partLevelColor = UIConstants.GetPartLevelColor(inPart.stats.level);
		this.performanceFill.fillAmount = CarPartStats.GetNormalizedStatValue(inPart.stats.statWithPerformance, inPart.GetPartType(), null);
		this.performanceFill.color = partLevelColor;
		partLevelColor.a = 0.8f;
		this.maxPerformanceFill.fillAmount = CarPartStats.GetNormalizedStatValue(inPart.stats.stat + inPart.stats.maxPerformance, inPart.GetPartType(), null);
		this.maxPerformanceFill.color = partLevelColor;
		this.knowledgeBar.SetupKnowledge(inPart.stats.level);
		this.conditionBar.Setup(inPart);
		GameUtility.SetActive(this.riskContainer, inPart.stats.rulesRisk != 0f && this.mMode == UIPartInfoPopupWidget.Mode.Normal);
		GameUtility.SetActive(this.partBannedContainer, inPart.isBanned && this.mMode == UIPartInfoPopupWidget.Mode.Normal);
		StringVariableParser.stringValue1 = inPart.stats.rulesRiskString;
		this.riskLabel.text = Localisation.LocaliseID("PSG_10010428", null);
		this.performanceHeaderLabel.text = Localisation.LocaliseEnum(inPart.stats.statType);
		this.performanceLabel.text = inPart.stats.statWithPerformance.ToString("N0", Localisation.numberFormatter);
		this.maxPerformanceLabel.text = "/ " + (inPart.stats.stat + Mathf.Max(0f, inPart.stats.maxPerformance)).ToString("N0", Localisation.numberFormatter);
		this.partReliability.text = inPart.stats.partCondition.condition.ToString("P0", Localisation.numberFormatter);
		this.partMaxReliability.text = "/ " + inPart.stats.GetMaxReliability().ToString("P0", Localisation.numberFormatter);
		int days = (Game.instance.time.now - inPart.buildDate).Days;
		if (days < 7)
		{
			this.partBuildDate.text = Localisation.LocaliseID("PSG_10010379", null);
			this.partBuildDate.color = UIConstants.positiveColor;
		}
		else
		{
			StringVariableParser.intValue1 = days;
			this.partBuildDate.text = Localisation.LocaliseID("PSG_10010380", null);
			this.partBuildDate.color = this.mInitialDateColor;
		}
		this.ShowAditionalData(inPart);
	}

	private void ShowAditionalData(CarPart inPart)
	{
		bool inIsActive = false;
		int num = 0;
		for (int i = 0; i < inPart.components.Count; i++)
		{
			CarPartComponent carPartComponent = inPart.components[i];
			if (carPartComponent != null && !carPartComponent.IgnoreBonusForUI() && (this.mBonuses != null || carPartComponent.HasActivationRequirement() || carPartComponent.bonuses.Count != 0))
			{
				if (this.mBonuses != null)
				{
					bool flag = false;
					for (int j = 0; j < carPartComponent.bonuses.Count; j++)
					{
						if (this.mBonuses.Contains(carPartComponent.bonuses[j]))
						{
							flag = true;
						}
					}
					if (!flag)
					{
						goto IL_C7;
					}
				}
				string name = carPartComponent.GetName(inPart);
				inIsActive = true;
				this.AddBonusText(name, num, carPartComponent);
				num++;
			}
			IL_C7:;
		}
		GameUtility.SetActive(this.bonusesContainerData, inIsActive);
		for (int k = num; k < this.bonusesEntries.Count; k++)
		{
			this.bonusesEntries[k].gameObject.SetActive(false);
		}
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

	private void SetIcon(CarPart.PartType inType)
	{
		for (int i = 0; i < this.partIconParent.childCount; i++)
		{
			if (i == (int)inType)
			{
				this.partIconParent.GetChild(i).gameObject.SetActive(true);
			}
			else
			{
				this.partIconParent.GetChild(i).gameObject.SetActive(false);
			}
		}
	}

	public UIKnowledgeBar knowledgeBar;

	public Transform partIconParent;

	public TextMeshProUGUI statName;

	public TextMeshProUGUI partType;

	public TextMeshProUGUI riskLabel;

	public GameObject riskContainer;

	public Image performanceFill;

	public Image maxPerformanceFill;

	public TextMeshProUGUI performanceHeaderLabel;

	public TextMeshProUGUI performanceLabel;

	public TextMeshProUGUI maxPerformanceLabel;

	public UIPartConditionBar conditionBar;

	public TextMeshProUGUI partReliability;

	public TextMeshProUGUI partMaxReliability;

	public TextMeshProUGUI partBuildDate;

	public GameObject partBannedContainer;

	public GameObject bonusesContainerData;

	public List<UIAdditionalBonusEntry> bonusesEntries = new List<UIAdditionalBonusEntry>();

	public UIPartInfoPopupWidget.ModeData[] modeData;

	private List<CarPartComponentBonus> mBonuses;

	private UIPartInfoPopupWidget.Mode mMode;

	private Color mInitialDateColor = default(Color);

	private RectTransform mTransform;

	private RectTransform mRefTransform;

	public enum Mode
	{
		Normal,
		BonusesOnly
	}

	[Serializable]
	public struct ModeData
	{
		public GameObject[] containers;

		public UIPartInfoPopupWidget.Mode mode;
	}
}
