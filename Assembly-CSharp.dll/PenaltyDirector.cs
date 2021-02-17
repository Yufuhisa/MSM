using System;
using System.Collections.Generic;
using FullSerializer;
using MM2;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class PenaltyDirector
{
	public void OnSessionStarting()
	{
		this.mHasScrutinizedRules = false;
	}

	public bool EligbleForPenalty(RacingVehicle inVehicle)
	{
		return !inVehicle.sessionPenalty.hasActivePenalty;
	}

	public void ApplyPenaltyIfViable(RacingVehicle inVehicle, Penalty.PenaltyCause inCause)
	{
		if (!this.EligbleForPenalty(inVehicle))
		{
			return;
		}
		if (Game.instance.sessionManager.GetNormalizedSessionTime() < 0.7f)
		{
			this.SetDriveThruPenalty(0.5f, inVehicle, inCause);
		}
		else
		{
			this.SetTimePenalty(0.5f, inVehicle, (PenaltyTime.PenaltySize)RandomUtility.GetRandom01(), inCause);
		}
	}

	public void SetDriveThruPenalty(float chance01, RacingVehicle inVehicle, Penalty.PenaltyCause inCause = Penalty.PenaltyCause.Count)
	{
		PenaltyPitlaneDriveThru penaltyPitlaneDriveThru = new PenaltyPitlaneDriveThru(inCause);
		penaltyPitlaneDriveThru.SetState(this.ChoosePenaltyState(chance01));
		inVehicle.sessionPenalty.AddPenalty(penaltyPitlaneDriveThru);
		if (inVehicle.isPlayerDriver)
		{
			Game.instance.sessionManager.raceDirector.sessionSimSpeedDirector.SlowDownForEvent(SessionSimSpeedDirector.SlowdownEvents.DriveThroughPenalty, inVehicle);
		}
	}

	public void SetTimePenalty(float chance01, RacingVehicle inVehicle, PenaltyTime.PenaltySize inSize, Penalty.PenaltyCause inCause = Penalty.PenaltyCause.Count)
	{
		PenaltyTime penaltyTime = new PenaltyTime(inSize, inCause);
		penaltyTime.SetState(this.ChoosePenaltyState(chance01));
		inVehicle.sessionPenalty.AddPenalty(penaltyTime);
	}

	public void ApplyTimePenalties()
	{
		int vehicleCount = Game.instance.vehicleManager.vehicleCount;
		for (int i = 0; i < vehicleCount; i++)
		{
			RacingVehicle vehicle = Game.instance.vehicleManager.GetVehicle(i);
			vehicle.timer.sessionTime += vehicle.timer.sessionTimePenalty;
		}
		RaceSessionState.UpdateStandingsToTrackPosition();
	}

	private Penalty.PenaltyState ChoosePenaltyState(float chance01)
	{
		if (RandomUtility.GetRandom01() > 0.5f)
		{
			return Penalty.PenaltyState.PenaltyGiven;
		}
		return Penalty.PenaltyState.PenaltyCleared;
	}

	public void ScrutinizePartRules(ref List<RacingVehicle> inVehicles)
	{
		if (this.mHasScrutinizedRules)
		{
			return;
		}
		this.mHasScrutinizedRules = true;
		Dictionary<RacingVehicle, int> dictionary = new Dictionary<RacingVehicle, int>();
		for (int i = 0; i < inVehicles.Count; i++)
		{
			RacingVehicle racingVehicle = inVehicles[i];
			Team team = racingVehicle.driver.contract.GetTeam();
			int num = 0;
			int num2 = 0;
			bool flag = team.investor != null && team.investor.hasPartRiskBonus;
			int num3 = (!flag) ? 0 : team.investor.partRiskBonus;
			foreach (CarPart.PartType inType in CarPart.GetPartType(team.championship.series, false))
			{
				CarPart part = racingVehicle.car.GetPart(inType);
				if (part.stats.rulesRisk > 0f)
				{
					float num4 = Mathf.Max(part.stats.rulesRisk + (float)num3, 0f);
					if ((float)RandomUtility.GetRandom(0, 100) < num4 * GameStatsConstants.scrutineeringChance)
					{
						team.rulesBrokenThisSeason++;
						part.isBanned = true;
						team.carManager.partImprovement.RemovePartImprove(CarPartStats.CarPartStat.Performance, part);
						team.carManager.partImprovement.RemovePartImprove(CarPartStats.CarPartStat.Reliability, part);
						racingVehicle.car.UnfitPart(part);
						long inAmount = (long)(100000 * team.rulesBrokenThisSeason);
						StringVariableParser.partForUI = part;
						Transaction transaction = new Transaction(Transaction.Group.GlobalMotorsport, Transaction.Type.Debit, inAmount, Localisation.LocaliseID("PSG_10010576", null));
						team.financeController.unnallocatedTransactions.Add(transaction);
						num++;
						int num5 = 2 * team.rulesBrokenThisSeason;
						num2 += num5;
						PenaltyPartRulesBroken inPenalty = new PenaltyPartRulesBroken(part, inAmount, num5);
						racingVehicle.sessionPenalty.AddPenalty(inPenalty);
						if (team.IsPlayersTeam())
						{
							App.instance.steamAchievementsManager.UnlockAchievement(Achievements.AchievementEnum.Caught_Breaking_Rules);
							Game.instance.dialogSystem.OnPartRuleBroken(null, part);
						}
					}
				}
			}
			if (num2 != 0)
			{
				dictionary.Add(racingVehicle, num2);
			}
		}
		for (int k = 0; k < inVehicles.Count; k++)
		{
			RacingVehicle racingVehicle2 = inVehicles[k];
			if (dictionary.ContainsKey(racingVehicle2))
			{
				int num6 = Mathf.Min(k + dictionary[racingVehicle2], inVehicles.Count - 1);
				inVehicles.Remove(racingVehicle2);
				inVehicles.Insert(num6, racingVehicle2);
				dictionary.Remove(racingVehicle2);
				k--;
			}
		}
		for (int l = 0; l < Game.instance.sessionManager.championships.Count; l++)
		{
			Championship championship = Game.instance.sessionManager.championships[l];
			RacingVehicle vehicleWithFastestLap = Game.instance.sessionManager.GetVehicleWithFastestLap(championship);
			if (vehicleWithFastestLap != null && vehicleWithFastestLap.sessionPenalty.GetPartPenaltyCount() > 0)
			{
				RacingVehicle racingVehicle3 = null;
				int count = inVehicles.Count;
				for (int m = 0; m < count; m++)
				{
					RacingVehicle racingVehicle4 = inVehicles[m];
					if (racingVehicle4.championship == championship)
					{
						if (racingVehicle4.sessionPenalty.GetPartPenaltyCount() > 0 && (racingVehicle3 == null || racingVehicle3.timer.fastestLap.time > racingVehicle4.timer.fastestLap.time))
						{
							racingVehicle3 = racingVehicle4;
						}
					}
				}
				if (racingVehicle3 != null)
				{
					Game.instance.sessionManager.sessionFastestLapData.SetVehicleWithFastestLap(racingVehicle3);
				}
			}
		}
		for (int n = 0; n < inVehicles.Count; n++)
		{
			inVehicles[n].SetStandingsPosition(n + 1);
		}
	}

	public bool hasScrutinizedRules
	{
		get
		{
			return this.mHasScrutinizedRules;
		}
	}

	private bool mHasScrutinizedRules;
}
