using System;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class SpinOutDirector
{
	public SpinOutDirector()
	{
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
		return Game.instance.sessionManager.lap != 1 && !flag2 && !isTutorialActiveInCurrentGameState && !inVehicle.behaviourManager.isOutOfRace && !flag && inVehicle.sessionEvents.IsReadyTo(SessionEvents.EventType.SpinOut);
	}

	private float mTimer;

	private float mFrequency = 30f;
}
