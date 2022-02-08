using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIEngineerComponentsWidget : MonoBehaviour
{
	public UIEngineerComponentsWidget()
	{
	}

	public void Setup()
	{
		Engineer engineer = (Engineer)Game.instance.player.team.contractManager.GetPersonOnJob(Contract.Job.EngineerLead);
		this.portrait.SetPortrait(engineer);
		this.stars.SetAbilityStarsData(engineer);
		this.engineerName.text = engineer.name;
		for (int i = 0; i < this.entries.Count; i++)
		{
			this.entries[i].Setup(null);
		}
		for (int j = 0; j < engineer.availableComponents.Count; j++)
		{
			CarPartComponent carPartComponent = engineer.availableComponents[j];
			if (carPartComponent.level - 1 >= 0 && carPartComponent.level - 1 < this.entries.Count)
			{
				this.entries[carPartComponent.level - 1].Setup(carPartComponent);
			}
		}
		this.noComponentsAvailableContainer.SetActive(engineer.availableComponents.Count == 0);
	}

	public UICharacterPortrait portrait;

	public UIAbilityStars stars;

	public TextMeshProUGUI engineerName;

	public List<UIComponentEntry> entries = new List<UIComponentEntry>();

	public GameObject noComponentsAvailableContainer;
}
