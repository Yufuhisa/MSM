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
			int num = -1;
			int num2 = 20;
			int count = inDetails.results.GetAllResultsForSession(SessionDetails.SessionType.Race).Count;
			for (int i = 0; i < count; i++)
			{
				RaceEventResults.ResultData resultForDriver = inDetails.results.GetResultsForSession(SessionDetails.SessionType.Race).GetResultForDriver(inDriver);
				if (resultForDriver != null)
				{
					if (num == -1)
					{
						num = 0;
					}
					num += resultForDriver.points;
					num2 = resultForDriver.position;
				}
			}
			this.pointsForEvent.text = ((num != -1) ? ((num != 0) ? num.ToString() : string.Empty) : "-");
			this.pointsForEvent.color = ((num > 0) ? this.labelColor : this.labelColorFaded);
			if (num2 >= 1 && num2 <= 3)
			{
				num2 = Mathf.Clamp(num2 - 1, 0, this.podiumColors.Length);
				this.backing.enabled = true;
				this.backing.color = this.podiumColors[num2];
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
