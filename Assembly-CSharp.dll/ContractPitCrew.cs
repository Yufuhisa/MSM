using System;
using UnityEngine;

public class ContractPitCrew : ContractPerson
{
	public ContractPitCrew()
	{
	}

	public override void SetContractTerminated(Contract.ContractTerminationType inTerminationType = Contract.ContractTerminationType.Generic)
	{
		this.job = Contract.Job.Unemployed;
		this.employeer = null;
		this.employeerName = string.Empty;
		this.mEmployeerTeam = null;
		this.SetContractState(Contract.ContractStatus.Terminated);
	}

	public override int GetContractTerminationCost()
	{
		return 0;
	}

	public void OnRaceWeekendEnd()
	{
		this.mRacesLeft--;
	}

	public void SignContract(Entity inTeam)
	{
		this.mRacesLeft = 12;
		this.job = Contract.Job.PitCrewMember;
		this.employeer = inTeam;
		this.SetContractState(Contract.ContractStatus.OnGoing);
	}

	public void RenewContract()
	{
		this.SignContract(this.employeer);
	}

	public void CalculatePitCrewWage(float inStarsStatValue)
	{
		if (base.person.IsReplacementPerson())
		{
			this.yearlyWages = 12000;
			this.signOnFee = 0;
		}
		else
		{
			int num = Mathf.FloorToInt(inStarsStatValue);
			float num2 = inStarsStatValue - (float)num;
			long[] starsValuePerRaceCosts = DesignDataManager.instance.GetDesignData().pitCrew.pitCrewContractPerRaceCosts.starsValuePerRaceCosts;
			if (starsValuePerRaceCosts != null)
			{
				float num3 = (float)(starsValuePerRaceCosts[num + 1] - starsValuePerRaceCosts[num]);
				this.yearlyWages = (int)GameUtility.RoundCurrency(num2 * num3 + (float)starsValuePerRaceCosts[num]) * 12;
			}
			else
			{
				this.yearlyWages = 100000;
			}
			this.signOnFee = (int)DesignDataManager.instance.GetDesignData().pitCrew.contractSignOnFee;
		}
	}

	public bool canRenewContract
	{
		get
		{
			return this.mRacesLeft < 12;
		}
	}

	public override long perRaceCost
	{
		get
		{
			return GameUtility.RoundCurrency((long)(this.yearlyWages / 12));
		}
	}

	public int racesLeft
	{
		get
		{
			return this.mRacesLeft;
		}
	}

	public const int CONTRACT_RACES_LENGTH = 12;

	private int mRacesLeft = 12;
}
