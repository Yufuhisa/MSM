using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDriverEventData : MonoBehaviour
{
	public void SetPointsForEvent(Driver inDriver, RaceEventDetails inDetails)
	{
		if (!inDetails.hasEventEnded)
		{
			this.backing.enabled = false;
			this.pointsForEvent.text = string.Empty;
		}
		else
		{
			int points = -1;
			int position = 20;
			bool dnfFlag = false;
			int count = inDetails.results.GetAllResultsForSession(SessionDetails.SessionType.Race).Count;
			for (int i = 0; i < count; i++)
			{
				RaceEventResults.ResultData resultForDriver = inDetails.results.GetResultsForSession(SessionDetails.SessionType.Race).GetResultForDriver(inDriver);
				if (resultForDriver != null)
				{
					if (points == -1)
					{
						points = 0;
					}
					points += resultForDriver.points;
					position = resultForDriver.position;
					if (resultForDriver.carState != RaceEventResults.ResultData.CarState.None)
						dnfFlag = true;
				}
			}
			
			if (dnfFlag && points <= 0)
				// if retired or crashed and didnt get any points -> show DNF for this race
				this.pointsForEvent.text = "DNF";
			else
				this.pointsForEvent.text = ((points != -1) ? ((points != 0) ? points.ToString() : string.Empty) : "-");
			
			this.pointsForEvent.color = ((points > 0) ? this.labelColor : this.labelColorFaded);
			if (position >= 1 && position <= 3)
			{
				position = Mathf.Clamp(position - 1, 0, this.podiumColors.Length);
				this.backing.enabled = true;
				this.backing.color = this.podiumColors[position];
			}
			else
			{
				this.backing.enabled = false;
			}
		}
	}

	public Image backing;

	public TextMeshProUGUI pointsForEvent;

	public Color[] podiumColors = new Color[0];

	public Color labelColor = default(Color);

	public Color labelColorFaded = default(Color);
}
