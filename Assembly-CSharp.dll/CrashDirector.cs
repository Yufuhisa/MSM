using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class CrashDirector
{
	public CrashDirector()
	{
	}

	public void OnSessionStarting()
	{
		this.mCrashVehicle.Clear();
		
		switch (DesignDataManager.GetGameLength(false))
		{
		case PrefGameRaceLength.Type.Short:
			this.mRealSafetyCarCount = 1;
			break;
		case PrefGameRaceLength.Type.Medium:
			this.mRealSafetyCarCount = 2;
			break;
		case PrefGameRaceLength.Type.Long:
			this.mRealSafetyCarCount = 2;
			break;
		}

		this.mSessionManager = Game.instance.sessionManager;
		this.mSafetyCar = Game.instance.vehicleManager.safetyVehicle;
	}

	public void OnLoad()
	{
	}

	public void OnSessionEnd()
	{
		this.mSafetyCar = null;
	}

	public void OnCrashIncident(RacingVehicle inVehicle)
	{
		if (Game.instance.sessionManager.flag != SessionManager.Flag.Green)
		{
			return;
		}
		Circuit circuit = this.mSessionManager.eventDetails.circuit;
		float safetyCarFlagProbability = circuit.safetyCarFlagProbability;
		float random = RandomUtility.GetRandom01();
		if (random < safetyCarFlagProbability)
		{
			float virtualSafetyCarProbability = circuit.virtualSafetyCarProbability;
			random = RandomUtility.GetRandom01();
			int num = this.mSessionManager.lapCount - this.mSessionManager.lap;
			bool flag = this.mSessionManager.championship.rules.safetyCarUsage != ChampionshipRules.SafetyCarUsage.RealSafetyCar;
			bool flag2 = Game.instance.vehicleManager.safetyVehicle.IsReadyToGoOut() && this.mSessionManager.championship.rules.safetyCarUsage != ChampionshipRules.SafetyCarUsage.VirtualSafetyCar && num > 3;
			if (!flag2 || (flag && random < virtualSafetyCarProbability))
			{
				this.SetVirtualSafetyCarFlag();
			}
			else if (flag2 && this.mRealSafetyCarCount > 0)
			{
				this.mRealSafetyCarCount--;
				this.SetSafetyCarFlag();
			}
			else
			{
				Game.instance.sessionManager.raceDirector.SetYellowFlag(inVehicle.timer.sectorVehicleIsIn);
			}
		}
		else
		{
			Game.instance.sessionManager.raceDirector.SetYellowFlag(inVehicle.timer.sectorVehicleIsIn);
		}
	}

	public void SetVirtualSafetyCarFlag()
	{
		this.mSessionManager.SetDesiredFlag(SessionManager.Flag.VirtualSafetyCar);
		this.mVirtualSafetyFlagDuration = (float)(Mathf.RoundToInt((float)RandomUtility.GetRandomInc(0, 2)) * 10 + 50);
	}

	public void SetSafetyCarFlag()
	{
		this.mSessionManager.SetDesiredFlag(SessionManager.Flag.SafetyCar);
		this.mSafetyCar.SendOut();
	}

	public void SimulationUpdate()
	{
		if (this.mSessionManager.flag == SessionManager.Flag.VirtualSafetyCar)
		{
			this.mVirtualSafetyFlagDuration -= GameTimer.simulationDeltaTime;
			if (this.mVirtualSafetyFlagDuration <= 0f)
			{
				this.mSessionManager.raceDirector.ResumeGreenFlag();
			}
		}
	}

	public bool CalculateCrashChance(RacingVehicle inVehicle, bool reduceCrashCount)
	{
		if (this.mCrashVehicle.Contains(inVehicle))
			return true;
		
		if (!reduceCrashCount && this.IsSessionCrashingViable(inVehicle))
		{
			this.mCrashVehicle.Add(inVehicle);
			return true;
		}
		return false;
	}

	public static bool HasTeamMateRetired(RacingVehicle inVehicle)
	{
		return Game.instance.vehicleManager.GetVehicleTeamMate(inVehicle).behaviourManager.isOutOfRace;
	}

	private bool IsSessionCrashingViable(RacingVehicle inVehicle)
	{
		SessionManager sessionManager = Game.instance.sessionManager;
		
		bool safetyCarReady = Game.instance.vehicleManager.safetyVehicle.IsReadyToGoOut() || sessionManager.championship.rules.safetyCarUsage != ChampionshipRules.SafetyCarUsage.RealSafetyCar;
		bool isTutorialActiveInCurrentGameState = Game.instance.tutorialSystem.isTutorialActiveInCurrentGameState;
		bool isRace = sessionManager.sessionType == SessionDetails.SessionType.Race;
		bool noRaceStartEnd = sessionManager.lap > 1 && sessionManager.lapCount - sessionManager.lap >= 3;
		bool crashesLeft = mCrashVehicle.Count < CrashDirector.MAX_CRASHES;
		bool isGreenFlag = Game.instance.sessionManager.flag == SessionManager.Flag.Green;
		return isGreenFlag && crashesLeft && !isTutorialActiveInCurrentGameState && safetyCarReady && isRace && noRaceStartEnd
		&& !inVehicle.behaviourManager.isOutOfRace && inVehicle.sessionEvents.IsReadyTo(SessionEvents.EventType.Crash);
	}

	public static RacingVehicle GetTeamMate(RacingVehicle inVehicle)
	{
		return Game.instance.vehicleManager.GetVehicleTeamMate(inVehicle);
	}

	public float virtualSafetyCarTimer
	{
		get
		{
			return this.mVirtualSafetyFlagDuration;
		}
	}

	public int lapsLenghtSafetyCar
	{
		get
		{
			return (this.mSafetyCar == null) ? 0 : this.mSafetyCar.behaviourManager.GetBehaviour<AISafetyCarBehaviour>().lapsLength;
		}
	}

	private int mRealSafetyCarCount = 1;

	private float mVirtualSafetyFlagDuration;

	private SafetyVehicle mSafetyCar;

	private SessionManager mSessionManager;
	
	private List<RacingVehicle> mCrashVehicle = new List<RacingVehicle>();
	
	private static int MAX_CRASHES = 10;

	}
