using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITopBarDriverEntry : MonoBehaviour
{
	public void OnUnload()
	{
		this.mTeam = null;
		this.mDriver = null;
	}

	public void SetChampionshipEntry(ChampionshipEntry_v1 inEntry)
	{
		if (inEntry != null)
		{
			this.mDriver = inEntry.GetEntity<Driver>();
			this.mTeam = this.mDriver.contract.GetTeam();
			int currentChampionshipPosition = inEntry.GetCurrentChampionshipPosition();
			this.colorStripe.color = this.mTeam.GetTeamColor().primaryUIColour.normal;
			this.positionLabel.text = currentChampionshipPosition.ToString();
			this.nameLabel.text = this.mDriver.lastName;
			this.pointsLabel.text = inEntry.GetCurrentPoints().ToString();
			int count = inEntry.championship.calendar.Count;
			int itemCount = this.eventPointsList.itemCount;
			int num = count - itemCount;
			for (int i = 0; i < num; i++)
			{
				this.eventPointsList.CreateListItem<UIDriverEventData>();
			}
			itemCount = this.eventPointsList.itemCount;
			for (int j = 0; j < itemCount; j++)
			{
				UIDriverEventData item = this.eventPointsList.GetItem<UIDriverEventData>(j);
				if (j < count)
				{
					item.SetPointsForEvent(this.mDriver, inEntry.championship.calendar[j]);
				}
				GameUtility.SetActive(item.gameObject, j < count);
			}
		}
	}

	public Image colorStripe;

	public TextMeshProUGUI positionLabel;

	public TextMeshProUGUI nameLabel;

	public TextMeshProUGUI pointsLabel;

	public UIGridList eventPointsList;

	private Team mTeam;

	private Driver mDriver;
}
