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
			GameUtility.SetActive(this.series[k].gameObject, k < entityList.Count);
			if (k < entityList.Count)
			{
				this.series[k].Setup(entityList[k]);
			}
		}
		for (int l = 0; l < this.series.Length; l++)
		{
			if (this.series[l].championship.series == inSeries && this.series[l].isChoosable)
			{
				this.series[l].toggle.isOn = true;
				this.SetChampionshipData(this.series[l].championship);
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
