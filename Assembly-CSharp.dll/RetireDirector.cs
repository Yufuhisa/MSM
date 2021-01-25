using System;
using FullSerializer;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class RetireDirector
{
	public RetireDirector()
	{
	}

	public void OnSessionStarting()
	{
	}

	public CarPartCondition.PartState GetConditionLossOutcome(CarPart inPart)
	{
		CarPart.PartType partType = inPart.GetPartType();
		if (RandomUtility.GetRandom01() < RetireDirector.GetChanceForPartType(partType))
		{
			return CarPartCondition.PartState.CatastrophicFailure;
		}
		return CarPartCondition.PartState.Failure;
	}

	private static float GetChanceForPartType(CarPart.PartType inType)
	{
		switch (inType)
		{
		case CarPart.PartType.Brakes:
		case CarPart.PartType.BrakesGT:
		case CarPart.PartType.BrakesGET:
			return GameStatsConstants.brakesRate;
		case CarPart.PartType.Engine:
		case CarPart.PartType.EngineGT:
		case CarPart.PartType.EngineGET:
			return GameStatsConstants.engineRate;
		case CarPart.PartType.FrontWing:
		case CarPart.PartType.FrontWingGET:
			return GameStatsConstants.frontWingRate;
		case CarPart.PartType.Gearbox:
		case CarPart.PartType.GearboxGT:
		case CarPart.PartType.GearboxGET:
			return GameStatsConstants.gearBoxRate;
		case CarPart.PartType.RearWing:
		case CarPart.PartType.RearWingGT:
		case CarPart.PartType.RearWingGET:
			return GameStatsConstants.rearWingRate;
		case CarPart.PartType.Suspension:
		case CarPart.PartType.SuspensionGT:
		case CarPart.PartType.SuspensionGET:
			return GameStatsConstants.suspensionRate;
		default:
			return 0f;
		}
	}
}
