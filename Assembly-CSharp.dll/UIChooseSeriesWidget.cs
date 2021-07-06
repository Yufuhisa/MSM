using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIChooseSeriesWidget : MonoBehaviour
{
	public UIChooseSeriesWidget()
	{
	}

	private void Awake()
	{
		for (int i = 0; i < this.seriesButtons.Length; i++)
		{
			UIChooseSeriesWidget.SeriesButton seriesButton = this.seriesButtons[i];
			seriesButton.button.onClick.AddListener(delegate()
			{
				this.SelectSeries(seriesButton.series);
			});
			seriesButton.getContentButton.onClick.AddListener(delegate()
			{
				this.GetContentButton(seriesButton.dlcName);
			});
		}
	}

	public void OnEnter()
	{
		GameUtility.SetActive(this, true);
		for (int i = 0; i < this.seriesButtons.Length; i++)
		{
			UIChooseSeriesWidget.SeriesButton seriesButton = this.seriesButtons[i];
			GameUtility.SetActive(seriesButton.newContentContainer, !DLCManager.GetDlcByName(seriesButton.dlcName).isOwned);
			List<Championship> championshipsForSeries = Game.instance.championshipManager.GetChampionshipsForSeries(seriesButton.series);
			bool flag = false;
			for (int j = 0; j < championshipsForSeries.Count; j++)
			{
				Championship championship = championshipsForSeries[j];
				bool flag2 = (!CreateTeamManager.isCreatingTeam || Game.instance.challengeManager.IsAttemptingChallenge()) ? (championship.isChoosable && !championship.isBlockedByChallenge) : championship.isChoosableCreateTeam;
				flag2 = (flag2 && App.instance.dlcManager.IsDlcWithIdInstalled(championship.DlcID));
				if (flag2)
				{
					flag = true;
					break;
				}
			}
			if (seriesButton.series == Championship.Series.SingleSeaterSeries) {
				GameUtility.SetActive(seriesButton.lockedContainer, false);
				seriesButton.button.interactable = true;
			}
			else {
				GameUtility.SetActive(seriesButton.lockedContainer, true);
				seriesButton.button.interactable = false;
			}
		}
	}

	private void GetContentButton(string inName)
	{
		DLCManager.HandleGetDlcButton(DLCManager.GetDlcByName(inName).appId);
	}

	private void SelectSeries(Championship.Series inSeries)
	{
		UIManager.instance.GetScreen<ChooseSeriesScreen>().SelectSeries(inSeries);
		GameUtility.SetActive(this, false);
		scSoundManager.Instance.PlaySound(SoundID.Sfx_TransitionStandings, 0f);
	}

	public UIChooseSeriesWidget.SeriesButton[] seriesButtons;

	[Serializable]
	public struct SeriesButton
	{
		public Button button;

		public Championship.Series series;

		public GameObject newContentContainer;

		public GameObject lockedContainer;

		public Button getContentButton;

		public string dlcName;
	}
}
