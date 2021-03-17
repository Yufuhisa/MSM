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
		float random = RandomUtility.GetRandom01();
		int i;
		if (random > 0.99f)
		{
			i = 8;
		}
		else if (random > 0.97f)
		{
			i = 7;
		}
		else if (random > 0.9f)
		{
			i = 6;
		}
		else if (random > 0.8f)
		{
			i = 5;
		}
		else if (random > 0.6f)
		{
			i = 4;
		}
		else if (random > 0.5f)
		{
			i = 3;
		}
		else if (random > 0.4f)
		{
			i = 2;
		}
		else if (random > 0.2f)
		{
			i = 1;
		}
		else
		{
			i = 0;
		}
		if (this.mSessionManager.currentSessionWeather.GetAverageWeather().rainType >= Weather.RainType.Heavy && i < 4)
		{
			i += RandomUtility.GetRandom(1, 2);
		}
		this.mCrashChunks = new CrashDirector.CrashRaceChunk[10];
		for (int j = 0; j < this.mCrashChunks.Length; j++)
		{
			this.mCrashChunks[j] = new CrashDirector.CrashRaceChunk();
			this.mCrashChunks[j].normalizedChunkStart = (float)j / ((float)this.mCrashChunks.Length - 1f);
			this.mCrashChunks[j].normalizedChunkSize = 1f / ((float)this.mCrashChunks.Length - 1f);
		}
		global::Debug.LogFormat("Crashes for this race: {0} ", new object[]
		{
			i
		});
		while (i > 0)
		{
			i--;
			this.mCrashChunks[RandomUtility.GetRandom(0, this.mCrashChunks.Length)].crashCount++;
		}
		this.mVehiclesCantCrash.Clear();
	}

	public void OnLoad()
	{
		if (this.mCrashChunks.Length == 0)
		{
			this.OnSessionStarting();
		}
		if (this.mVehiclesCantCrash == null)
		{
			this.mVehiclesCantCrash = new List<RacingVehicle>();
		}
	}

	public void OnSessionEnd()
	{
		this.mSafetyCar = null;
		this.mVehiclesCantCrash.Clear();
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
		for (int i = 0; i < this.mCrashChunks.Length; i++)
		{
			if (this.mCrashChunks[i].IsActiveChunk(Game.instance.sessionManager.GetNormalizedSessionTime()))
			{
				this.mActiveChunk = this.mCrashChunks[i];
				break;
			}
		}
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
		// bool flag4 = this.mActiveChunk.crashCount > 0;
		bool crashesLeft = mCrashVehicle.Count < 10;
		bool isGreenFlag = Game.instance.sessionManager.flag == SessionManager.Flag.Green;
		return isGreenFlag && !isTutorialActiveInCurrentGameState && safetyCarReady && isRace && noRaceStartEnd
		&& !inVehicle.behaviourManager.isOutOfRace && (this.mVehiclesCantCrash == null || !this.mVehiclesCantCrash.Contains(inVehicle))
		&& inVehicle.sessionEvents.IsReadyTo(SessionEvents.EventType.Crash);
	}

	public static RacingVehicle GetTeamMate(RacingVehicle inVehicle)
	{
		return Game.instance.vehicleManager.GetVehicleTeamMate(inVehicle);
	}

	public void AddCrash()
	{
		this.mActiveChunk.crashCount++;
	}

	public void AddVehicleCantCrash(RacingVehicle inVehicle)
	{
		if (!this.mVehiclesCantCrash.Contains(inVehicle))
		{
			this.mVehiclesCantCrash.Add(inVehicle);
		}
	}

	public void RemoveVehicleCantCrash(RacingVehicle inVehicle)
	{
		this.mVehiclesCantCrash.Remove(inVehicle);
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

	private List<RacingVehicle> mVehiclesCantCrash = new List<RacingVehicle>();

	private int mRealSafetyCarCount = 1;

	private float mVirtualSafetyFlagDuration;

	private CrashDirector.CrashRaceChunk[] mCrashChunks = new CrashDirector.CrashRaceChunk[0];

	private CrashDirector.CrashRaceChunk mActiveChunk;

	private SafetyVehicle mSafetyCar;

	private SessionManager mSessionManager;
	
	private List<RacingVehicle> mCrashVehicle = new List<RacingVehicle>();

	[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
	private class CrashRaceChunk
	{
		public CrashRaceChunk()
		{
		}

		public bool IsActiveChunk(float inNormalizedRaceTime)
		{
			return inNormalizedRaceTime >= this.normalizedChunkStart && inNormalizedRaceTime < this.normalizedChunkStart + this.normalizedChunkSize;
		}

		public float normalizedChunkSize;

		public float normalizedChunkStart;

		public int crashCount;
	}
}
