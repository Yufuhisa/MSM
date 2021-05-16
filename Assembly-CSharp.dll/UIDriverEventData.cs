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
			bool dsqFlag = false;
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
					
					foreach (Penalty penalaty in resultForDriver.penalties) {
						if (penalaty is PenaltyPartRulesBroken) {
							dsqFlag = true;
						}
					}
					
					
				}
			}
			
			if (dsqFlag) {
				this.pointsForEvent.text = "DSQ";
				this.pointsForEvent.color = UIDriverEventData.COLOR_DSQ;
				this.backing.enabled = false;
			}
			else if (dnfFlag && points <= 0) {
				// if retired or crashed and didnt get any points -> show DNF for this race
				this.pointsForEvent.text = "DNF";
				this.pointsForEvent.color = this.labelColorFaded;
				this.backing.enabled = false;
			}
			else if (points == -1) {
				this.pointsForEvent.text = "-";
				this.pointsForEvent.color = this.labelColorFaded;
				this.backing.enabled = false;
			}
			else if (points == 0) {
				this.pointsForEvent.text = string.Empty;
				this.pointsForEvent.color = this.labelColorFaded;
				this.backing.enabled = false;
			}
			else {
				this.pointsForEvent.text = points.ToString();
				this.pointsForEvent.color = this.labelColor;
				this.backing.enabled = false;
				
				if (position >= 1 && position <= 3) {
					this.backing.enabled = true;
					position = Mathf.Clamp(position - 1, 0, this.podiumColors.Length);
					this.backing.color = this.podiumColors[position];
				}
			}
		}
	}

	public Image backing;

	public TextMeshProUGUI pointsForEvent;

	public Color[] podiumColors = new Color[0];

	public Color labelColor = default(Color);

	public Color labelColorFaded = default(Color);
	
	private static Color COLOR_DSQ = new Color(255f, 0f, 0f);
}
