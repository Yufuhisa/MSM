using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChooseChampionshipWidget : MonoBehaviour
{
	public UIChooseChampionshipWidget()
	{
	}

	public void OnEnter()
	{
		GameUtility.SetActive(this, false);
	}

	public void Activate(Championship.Series inSeries)
	{
		scSoundManager.BlockSoundEvents = true;
		this.toggleGroup.SetAllTogglesOff();
		GameUtility.SetActive(this, true);
		for (int i = 0; i < this.championshipData.Length; i++)
		{
			bool inIsActive = this.championshipData[i].series == inSeries;
			for (int j = 0; j < this.championshipData[i].objectsToEnable.Length; j++)
			{
				GameUtility.SetActive(this.championshipData[i].objectsToEnable[j], inIsActive);
			}
		}
		List<Championship> entityList = Game.instance.championshipManager.GetEntityList();
		for (int k = 0; k < this.series.Length; k++)
		{
			if (k < entityList.Count)
			{
				this.series[k].Setup(entityList[k]);
				if (this.series[k].championship.championshipID == 0) {
					GameUtility.SetActive(this.series[k].gameObject, true);
					this.series[k].toggle.enabled = true;
					GameUtility.SetActive(this.series[k].toggle.gameObject, true);
				} else {
					GameUtility.SetActive(this.series[k].gameObject, false);
					this.series[k].toggle.enabled = false;
					GameUtility.SetActive(this.series[k].toggle.gameObject, false);
				}
			}
			else
			{
				GameUtility.SetActive(this.series[k].gameObject, false);
			}
		}
		for (int l = 0; l < this.series.Length; l++)
		{
			if (this.series[l].championship.series == inSeries && this.series[l].isChoosable)
			{
				this.SetChampionshipData(this.series[l].championship);
				this.series[l].toggle.isOn = true;
				break;
			}
		}
		scSoundManager.BlockSoundEvents = false;
	}

	public void SetChampionshipData(Championship inChampionship)
	{
		this.championshipInfo.Setup(inChampionship);
	}

	public UIChooseChampionshipWidget.ChampionshipData[] championshipData;

	public UIChooseSeriesEntry[] series;

	public ToggleGroup toggleGroup;

	public UIChampionshipInfo championshipInfo;

	[Serializable]
	public struct ChampionshipData
	{
		public Championship.Series series;

		public GameObject[] objectsToEnable;
	}
}
