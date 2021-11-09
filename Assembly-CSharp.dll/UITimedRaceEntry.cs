using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITimedRaceEntry : MonoBehaviour
{
	public UITimedRaceEntry()
	{
	}

	private void Start()
	{
		this.greenArrow.color = UIConstants.positiveColor;
		this.redArrow.color = UIConstants.negativeColor;
		this.mObjectTargets[0] = this.riskDemotionIconContainer;
		this.mObjectTargets[1] = this.bonusPointsFastestLapContainer;
		this.mObjectTargets[2] = this.bonusPointsForPolePositionContainer;
		this.mObjectTargets[3] = this.timePenaltyContainer;
		this.mObjectTargets[4] = this.pitPenaltyContainer;
		this.mObjectTargets[5] = this.pointsPenaltyContainer;
	}

	private void Update()
	{
		UITimedRaceEntry.ToolTipType index;
		for (index = UITimedRaceEntry.ToolTipType.PartRuleBreak; index < UITimedRaceEntry.ToolTipType.Count; index++)
		{
			if (this.mObjectTargets[(int)index].activeSelf)
			{
				GameUtility.HandlePopup(ref this.mPopupOpen[(int)index], this.mObjectTargets[(int)index], delegate
				{
					this.ShowToolTip(index);
				}, new Action(this.HideTooltip));
			}
		}
	}

	public void SetInfo(RaceEventResults.ResultData inResultData, RaceEventResults.ResultData inFirstPlaceEntry, int inPosition, int inPreviousPosition)
	{
		this.mData = inResultData;
		Driver driver = this.mData.driver;
		Team team = driver.contract.GetTeam();
		UIQualifyPracticeEntry.SetClassLabel(team, this.classLabel, this.canvasGroup);
		GameUtility.SetActiveAndCheckNull(this.retiredContainer, this.mData.carState == RaceEventResults.ResultData.CarState.Retired);
		GameUtility.SetActiveAndCheckNull(this.crashedContainer, this.mData.carState == RaceEventResults.ResultData.CarState.Crashed);
		GameUtility.SetActiveAndCheckNull(this.bonusPointsFastestLapContainer, this.mData.gotExtraPointsForFastestLap);
		GameUtility.SetActiveAndCheckNull(this.bonusPointsForPolePositionContainer, this.mData.gotExtraPointsForPolePosition);
		if (Game.instance.challengeManager.ChallengeHasRaceObjective())
		{
			ChallengeObjectiveRaceBase raceObjective = Game.instance.challengeManager.currentChallenge.objectives.GetRaceObjective();
			int requiredPosition = raceObjective.GetRequiredPosition();
			bool inIsObjectiveBeingMet = raceObjective.IsAchievingTarget();
			this.ShowSessionObjective(inResultData.position == requiredPosition, inIsObjectiveBeingMet);
		}
		else
		{
			SessionObjective currentSessionObjective = Game.instance.player.team.sponsorController.GetCurrentSessionObjective();
			if (currentSessionObjective != null)
			{
				bool isPlayerChampionship = inResultData.team.championship.isPlayerChampionship;
				int num = (inResultData.concurrentPosition <= 0) ? (inPosition + 1) : inResultData.concurrentPosition;
				this.ShowSessionObjective(isPlayerChampionship && num == Mathf.RoundToInt((float)currentSessionObjective.targetResult), currentSessionObjective.lastObjectiveCompleted);
			}
			else
			{
				this.ShowSessionObjective(false, false);
			}
		}
		this.flag.SetNationality(driver.nationality);
		this.teamColorStrip.color = team.GetTeamColor().primaryUIColour.normal;
		if (team.championship.series == Championship.Series.EnduranceSeries)
		{
			this.driverNameLabel.text = team.GetCarName(null, team.GetDriversForCar(driver.carID));
			GameUtility.SetActive(this.flag, false);
		}
		else
		{
			this.driverNameLabel.text = driver.name;
			GameUtility.SetActive(this.flag, true);
		}
		this.positionLabel.text = inPosition.ToString();
		this.changeLabel.text = Mathf.Abs(inPreviousPosition - inPosition).ToString();
		this.teamNameLabel.text = team.GetTeamNameForUI();
		this.mData.lapsToLeader = inFirstPlaceEntry.laps - this.mData.laps;
		if (this.mData.position == 1)
		{
			this.timeLabel.text = GameUtility.GetLapTimeText(this.mData.time, false);
		}
		else
		{
			GameUtility.SetGapTimeText(this.timeLabel, this.mData, true);
		}
		this.pointsLabel.text = this.mData.points.ToString();
		this.stopsLabel.text = this.mData.stops.ToString();
		GameUtility.SetGapTimeText(this.gapLabel, this.mData, false);
		if (this.retiredContainer != null && this.crashedContainer != null && (this.retiredContainer.activeSelf || this.crashedContainer.activeSelf))
		{
			this.gapLabel.text = string.Empty;
		}
		if (team.IsPlayersTeam())
		{
			this.barType = UITimedRaceEntry.BarType.PlayerOwned;
		}
		this.SetArrows(inPreviousPosition, inPosition);
		this.SetBarType();
		bool flag = false;
		this.mTimePenaltyTotal = 0f;
		this.mPitlaneDrivesCount = 0;
		this.mPartsBanned = 0;
		this.mPlacesLost = 0;
		this.mPointsLost = 0;
		for (int i = 0; i < this.mData.penalties.Count; i++)
		{
			Penalty penalty = this.mData.penalties[i];
			if (penalty is PenaltyPoints)
			{
				this.mPointsLost++;
			}
			if (penalty is PenaltyTime)
			{
				this.mTimePenaltyTotal += ((PenaltyTime)penalty).seconds;
			}
			if (penalty is PenaltyPitlaneDriveThru)
			{
				this.mPitlaneDrivesCount++;
			}
			if (penalty is PenaltyPartRulesBroken)
			{
				PenaltyPartRulesBroken penaltyPartRulesBroken = penalty as PenaltyPartRulesBroken;
				this.mPartsBanned++;
				this.mPlacesLost += penaltyPartRulesBroken.placesLost;
				flag = true;
			}
		}
		if (flag)
		{
			this.timeLabel.text = "-";
			this.gapLabel.text = "-";
		}
		this.penaltyPointsLabel.text = "-" + this.mPointsLost.ToString();
		GameUtility.SetActiveAndCheckNull(this.timePenaltyContainer, this.mTimePenaltyTotal != 0f);
		GameUtility.SetActiveAndCheckNull(this.pitPenaltyContainer, this.mPitlaneDrivesCount != 0);
		GameUtility.SetActiveAndCheckNull(this.riskDemotionIconContainer, flag);
		GameUtility.SetActiveAndCheckNull(this.pointsPenaltyContainer, this.mPointsLost != 0);
	}

	public void ShowToolTip(UITimedRaceEntry.ToolTipType inType)
	{
		switch (inType)
		{
		case UITimedRaceEntry.ToolTipType.PartRuleBreak:
		{
			string inID = string.Empty;
			StringVariableParser.intValue1 = Mathf.RoundToInt((float)this.mPlacesLost);
			StringVariableParser.intValue2 = Mathf.RoundToInt((float)this.mPartsBanned);
			if (this.mPartsBanned == 1)
			{
				if (this.mPlacesLost == 1)
				{
					inID = "PSG_10011097";
				}
				else
				{
					inID = "PSG_10007155";
				}
			}
			else if (this.mPlacesLost == 1)
			{
				inID = "PSG_10011098";
			}
			else
			{
				inID = "PSG_10011099";
			}
			UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10007154", null), Localisation.LocaliseID(inID, null));
			break;
		}
		case UITimedRaceEntry.ToolTipType.FastestLapBonus:
			UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10007150", null), Localisation.LocaliseID("PSG_10007151", null));
			break;
		case UITimedRaceEntry.ToolTipType.QualifyingPolePositionBonus:
			UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10007152", null), Localisation.LocaliseID("PSG_10007153", null));
			break;
		case UITimedRaceEntry.ToolTipType.TimePenalty:
			StringVariableParser.intValue1 = Mathf.RoundToInt(this.mTimePenaltyTotal);
			UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10010042", null), Localisation.LocaliseID("PSG_10010043", null));
			break;
		case UITimedRaceEntry.ToolTipType.PitLaneDriveTrought:
			StringVariableParser.intValue1 = this.mPitlaneDrivesCount;
			if (this.mPitlaneDrivesCount == 1)
			{
				UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10010039", null), Localisation.LocaliseID("PSG_10010041", null));
			}
			else
			{
				UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10010039", null), Localisation.LocaliseID("PSG_10010040", null));
			}
			break;
		case UITimedRaceEntry.ToolTipType.PointsPenalty:
			StringVariableParser.intValue1 = this.mPointsLost;
			UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Open(Localisation.LocaliseID("PSG_10013879", null), Localisation.LocaliseID("PSG_10013880", null));
			break;
		}
	}

	public void HideTooltip()
	{
		UIManager.instance.dialogBoxManager.GetDialog<GenericInfoRollover>().Hide();
	}

	private void SetArrows(int inPreviousPosition, int inPosition)
	{
		if (inPreviousPosition > inPosition)
		{
			GameUtility.SetActive(this.greenArrow.gameObject, true);
			GameUtility.SetActive(this.redArrow.gameObject, false);
		}
		else if (inPreviousPosition < inPosition)
		{
			GameUtility.SetActive(this.redArrow.gameObject, true);
			GameUtility.SetActive(this.greenArrow.gameObject, false);
		}
		else
		{
			GameUtility.SetActive(this.redArrow.gameObject, false);
			GameUtility.SetActive(this.greenArrow.gameObject, false);
		}
	}

	private void SetBarType()
	{
		for (int i = 0; i < this.backing.Length; i++)
		{
			GameUtility.SetActive(this.backing[i].gameObject, i == (int)this.barType);
		}
	}

	public void ShowSessionObjective(bool inShow, bool inIsObjectiveBeingMet)
	{
		GameUtility.SetActive(this.sessionObjectiveLine.transform.parent.gameObject, inShow);
		if (inShow)
		{
			if (inIsObjectiveBeingMet)
			{
				this.sessionObjectiveLine.color = UIConstants.positiveColor;
			}
			else
			{
				this.sessionObjectiveLine.color = UIConstants.sponsorGreyColor;
			}
			if (this.sessionObjectiveLabel != null)
			{
				bool flag = Game.instance.challengeManager.ChallengeHasRaceObjective();
				this.sessionObjectiveLabel.text = Localisation.LocaliseID((!flag) ? "PSG_10008281" : "PSG_10012491", null);
			}
		}
	}

	public UITimedRaceEntry.BarType barType = UITimedRaceEntry.BarType.Darker;

	public TextMeshProUGUI positionLabel;

	public TextMeshProUGUI driverNameLabel;

	public TextMeshProUGUI teamNameLabel;

	public TextMeshProUGUI changeLabel;

	public TextMeshProUGUI stopsLabel;

	public TextMeshProUGUI timeLabel;

	public TextMeshProUGUI gapLabel;

	public TextMeshProUGUI pointsLabel;

	public GameObject bonusPointsFastestLapContainer;

	public GameObject bonusPointsForPolePositionContainer;

	public GameObject riskDemotionIconContainer;

	public Flag flag;

	public Image[] backing = new Image[0];

	public Image teamColorStrip;

	public Image greenArrow;

	public Image redArrow;

	public GameObject retiredContainer;

	public GameObject crashedContainer;

	public GameObject timePenaltyContainer;

	public GameObject pitPenaltyContainer;

	public GameObject pointsPenaltyContainer;

	public TextMeshProUGUI penaltyPointsLabel;

	public Image sessionObjectiveLine;

	public TextMeshProUGUI sessionObjectiveLabel;

	public TextMeshProUGUI classLabel;

	public CanvasGroup canvasGroup;

	private RaceEventResults.ResultData mData;

	private int mPlacesLost;

	private int mPartsBanned;

	private int mPointsLost;

	private float mTimePenaltyTotal;

	private int mPitlaneDrivesCount;

	private bool[] mPopupOpen = new bool[6];

	private GameObject[] mObjectTargets = new GameObject[6];

	public enum BarType
	{
		Lighter,
		Darker,
		PlayerOwned
	}

	public enum ToolTipType
	{
		PartRuleBreak,
		FastestLapBonus,
		QualifyingPolePositionBonus,
		TimePenalty,
		PitLaneDriveTrought,
		PointsPenalty,
		Count
	}
}
