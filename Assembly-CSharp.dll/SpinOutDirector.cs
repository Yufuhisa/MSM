using System;
using System.Collections.Generic;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class SpinOutDirector
{
	public SpinOutDirector()
	{
	}

	public void OnSessionStarting()
	{
		this.mVehiclesSpinningOut.Clear();
	}

	public void addSpinningOut(RacingVehicle inVehicle) {
		if (!mVehiclesSpinningOut.Contains(inVehicle))
			mVehiclesSpinningOut.Add(inVehicle);
	}

	public bool isSpinningOut(RacingVehicle inVehicle) {
		return mVehiclesSpinningOut.Contains(inVehicle);
	}

	public void SimulationUpdate()
	{
		this.mTimer += GameTimer.simulationDeltaTime;
	}

	public void OnSpinOutIncident(RacingVehicle inVehicle)
	{
		this.mTimer = RandomUtility.GetRandom(0f, this.mFrequency / 5f);
	}

	public bool IsSpinOutViable(RacingVehicle inVehicle)
	{
		bool isTutorialActiveInCurrentGameState = Game.instance.tutorialSystem.isTutorialActiveInCurrentGameState;
		bool flag = Game.instance.sessionManager.flag == SessionManager.Flag.Chequered;
		bool flag2 = inVehicle.speed < 45f;
		if (Game.instance.sessionManager.lap != 1
			&& !flag2
			&& !isTutorialActiveInCurrentGameState
			&& !inVehicle.behaviourManager.isOutOfRace
			&& !flag)
		{
			// if vehicle is alread spinning out this does not change
			if (mVehiclesSpinningOut.Contains(inVehicle))
				return true;

			if (inVehicle.sessionEvents.IsReadyTo(SessionEvents.EventType.SpinOut))
				return true;
			else
				return false;
		}
		else
			return false;
	}

	private float mTimer;

	private float mFrequency = 30f;
	
	private List<RacingVehicle> mVehiclesSpinningOut = new List<RacingVehicle>();
}
