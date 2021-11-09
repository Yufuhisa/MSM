using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGridDriverWidget : MonoBehaviour
{
	public UIGridDriverWidget()
	{
	}

	public void Setup(RaceEventResults.ResultData inQualifyingResult, RaceEventResults.ResultData inReferencePositionData, int inPosition)
	{
		this.driver = inQualifyingResult.driver;
		RacingVehicle racingVehicle = Game.instance.vehicleManager.GetVehicle(this.driver);
		Championship.Series series = Game.instance.sessionManager.GetPlayerChampionship().series;
		GameUtility.SetActive(this.gtCarIcon, series == Championship.Series.GTSeries);
		GameUtility.SetActive(this.singleSeaterIcon, series == Championship.Series.SingleSeaterSeries);
		GameUtility.SetActive(this.enduranceCarIcon, series == Championship.Series.EnduranceSeries);
		if (racingVehicle == null)
		{
			if (series != Championship.Series.EnduranceSeries)
			{
				racingVehicle = RaceEventResults.GetVehicle(this.driver, null);
			}
			else
			{
				racingVehicle = RaceEventResults.GetVehicleForCarID(this.driver);
			}
			this.driver = racingVehicle.driver;
		}
		this.SetDriverDetails();
		this.gridPosition.text = (inPosition + 1).ToString();
		bool flag = inPosition == 0 || inPosition == GameStatsConstants.qualifyingThresholdForQ2 || inPosition == GameStatsConstants.qualifyingThresholdForQ3;
		flag |= (Game.instance.sessionManager.GetPlayerChampionship().rules.gridSetup != ChampionshipRules.GridSetup.QualifyingBased3Sessions);
		this.lapTime.text = GameUtility.GetLapTimeText(inQualifyingResult.bestLapTime, false);
		this.lapTime.gameObject.SetActive(flag);
		float num = inQualifyingResult.bestLapTime - inReferencePositionData.bestLapTime;
		if (inQualifyingResult.laps > 0)
		{
			GameUtility.SetActive(this.lapTimeGap.gameObject, true);
			if (num == 0f)
			{
				this.lapTimeGap.text = "-";
			}
			else
			{
				this.lapTimeGap.text = "+" + GameUtility.GetLapTimeText(num, false);
			}
		}
		else
		{
			GameUtility.SetActive(this.lapTimeGap.gameObject, false);
		}
		this.tyre.SetTyreSet(racingVehicle.setup.tyreSet, null);
		this.tyre.UpdateTyreLocking(racingVehicle, true);
	}

	private void SetDriverDetails()
	{
		this.driverName.text = this.driver.name;
		this.driverPortrait.SetPortrait(this.driver);
		this.driverPortrait.SetDriverFormType(true);
		this.driverFlag.SetNationality(this.driver.nationality);
		this.teamName.text = this.driver.contract.GetTeam().name;
		this.uiCar.SetTeamColor(this.driver.contract.GetTeam().GetTeamColor().carColor);
		if (this.driver.IsPlayersDriver())
		{
			this.highlight.gameObject.SetActive(true);
		}
		else
		{
			this.highlight.gameObject.SetActive(false);
		}
		StringVariableParser.subject = this.driver;
		this.championshipCurrentPosition.text = Localisation.LocaliseID("PSG_10010583", null);
		StringVariableParser.subject = null;
	}

	public UICharacterPortrait driverPortrait;

	public Flag driverFlag;

	public UICar uiCar;

	public Image highlight;

	public TextMeshProUGUI gridPosition;

	public TextMeshProUGUI driverName;

	public TextMeshProUGUI lapTime;

	public TextMeshProUGUI lapTimeGap;

	public TextMeshProUGUI teamName;

	public TextMeshProUGUI championshipCurrentPosition;

	public UITyreWearIcon tyre;

	public Driver driver;

	public GameObject singleSeaterIcon;

	public GameObject gtCarIcon;

	public GameObject enduranceCarIcon;
}
