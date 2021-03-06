using System;

public static class GameStatsConstants
{

	public static int daysToKeepData = 90;

	public static float averageCharactersReadPerSecond = 16.6f;

	public static float chassisSupplierStatMax = 5f;

	public static float chassisStatMax = 20f;

	public static float chassisSliderAmmount = 10f;

	public static float[] chassisBaseStat = new float[]
	{
		6f,
		4f,
		2f,
		6f,
		4f,
		6f,
		6f
	};

	public static int newSeasonMaxPartCap = 450;

	public static float promotedTeamSpreadScaler = 1f;

	public static float relegatedTeamSpreadScaler = 1f;

	public static float partResetMaxBonus = 50f;

	public static int slotCount = 5;

	public static float initialReliabilityValue = 0.55f; // initial part reliability

	public static float initialMaxReliabilityValue = 0.74f; // initial part maxReliability

	public static float absolutMaxReliability = 0.98f; // highest possible maxReliability

	public static float targetMaxReliabilityMin = 0.58f; // part MaxReliability that Teams with max aggressivity (1.0) strife for (in development)

	public static float targetMaxReliabilityMax = 0.98f; // part  MaxReliability that Teams with min aggressivity (0.0) strife for (in development)

	public static float targetReliabilityMin = 0.55f; // part reliability that Teams with max aggressivity (1.0) strife for (must be below targetMaxReliabilityMin)

	public static float targetReliabilityMax = 0.85f; // part reliability that Teams with min aggressivity (0.0) strife for (must be below targetMaxReliabilityMax)

	public static float baseCarPartPerformance = 20f;

	public static float initialRedZone = 0.2f;

	public static float level1PerformanceBoost = 10f;

	public static float level2PerformanceBoost = 10f;

	public static float level3PerformanceBoost = 10f;

	public static float level4PerformanceBoost = 10f;

	public static float level5PerformanceBoost = 10f;

	public static int[] specPartValues = new int[]
	{
		600,
		300,
		150,
		300,
		150,
		600,
		100,
		100,
		100
	};

	public static int[] randomComponentsIDsPool = new int[]
	{
		2,
		3,
		4,
		7,
		11,
		13,
		59,
		15,
		17,
		18,
		20,
		21,
		75,
		69,
		28,
		57,
		79,
		83
	};

	public static float[] normalizedPerformanceScaleForPartLevel = new float[]
	{
		1f,
		0.75f,
		0.5f,
		0.25f,
		0f
	};

	public static int[] injuryTraits = new int[]
	{
		174,
		175,
		176,
		177,
		178,
		179,
		180,
		239,
		240,
		241,
		242,
		243,
		308,
		400,
		401,
		402,
		403,
		404,
		406,
		407,
		408,
		409,
		411,
		412,
		413,
		414,
		415,
		420,
		421,
		422
	};

	public static int[] criticalInjuryTraits = new int[]
	{
		178, // cracked rib
		241, // brocken meter tarsal (something with the foot)
		242, // dislocated shoulder
		243, // wiplash
		308, // burn
		400, // brocken legs - 19 weeks
		401, // use testdriver in race (workarround for AI to use testdriver) - 10 weeks
		402, // middle ear inflammation - 8 weeks
		403, // use testdriver in race (workarround for AI to use testdriver) - 16 weeks
		404, // use testdriver in race (workarround for AI to use testdriver) - 23 weeks
		406, // brocken legs - 16 weeks
		407, // brocken hand - 3 weeks
		408, // appendixitis - 3 weeks
		409, // back pain - 3 weeks
		411, // use testdriver in race (workarround for AI to use testdriver) - 6 weeks
		412, // sharing cockpit (workarround for AI to use testdriver) - 15 weeks
		413, // sharing cockpit (workarround for AI to use testdriver) - 15 weeks
		414, // sharing cockpit (workarround for AI to use testdriver) - 17 weeks
		415, // sharing cockpit (workarround for AI to use testdriver) - 22 weeks
		420, // dizziness Heinz Harald Frentzen - 3 weeks
		421, // temporary promoted reserve driver (Alex Yoong) - 38 weeks
		422  // last chance for Matzakane (Burti 2001) - 10 weeks
	};

	public static int daysRecoveredFromSittingOut = 2;

	public static float safetyCarSpeedLimit = 120f;

	public static float otherVehiclesSafetyCarSpeedLimit = 85f;

	public static float yellowFlagSpeedLimit = 40f;

	public static float scrutineeringChance = 15f;

	public static float minConditionTimeToFix = 12f;

	public static float maxConditionTimeToFix = 72f;

	public static float[] costPerRaceDataT1 = new float[]
	{
		2.8f,
		2.8f,
		2.6f,
		2.6f,
		2.4f,
		2.4f,
		2.2f,
		2.2f,
		2f,
		2f,
		2f,
		2f
	};

	public static float[] costPerRaceDataT2 = new float[]
	{
		2f,
		2f,
		1.75f,
		1.75f,
		1.5f,
		1.5f,
		1.25f,
		1.25f,
		1f,
		1f,
		1f,
		1f
	};

	public static float[] costPerRaceDataT3 = new float[]
	{
		1.8f,
		1.8f,
		1.7f,
		1.7f,
		1.6f,
		1.6f,
		1.5f,
		1.5f,
		1.4f,
		1.4f,
		1.4f,
		1.4f
	};

	public static float[] costPerRaceDataGT1 = new float[]
	{
		1.4f,
		1.4f,
		1.2f,
		1.2f,
		1.2f,
		1f,
		1f,
		1f,
		0.75f,
		0.75f,
		0.75f,
		0.75f
	};

	public static float[] costPerRaceDataGT2 = new float[]
	{
		1f,
		1f,
		0.75f,
		0.75f,
		0.75f,
		0.5f,
		0.5f,
		0.5f,
		0.4f,
		0.4f,
		0.4f,
		0.4f
	};

	public static float[] costPerRaceDataGET = new float[]
	{
		2.6f,
		2.6f,
		2.4f,
		2.4f,
		2.2f,
		2.2f,
		2f,
		2f,
		1.8f,
		1.8f,
		1.8f,
		1.8f
	};

	public static float[] costPerRaceDataGET2 = new float[]
	{
		1.7f,
		1.7f,
		1.5f,
		1.5f,
		1.3f,
		1.3f,
		1.2f,
		1.2f,
		1.2f,
		1.2f,
		1.2f,
		1.2f
	};

	public static long fundsLowerBound = -5000000L;

	public static long liveryEditCost = 500000L;

	public static long playerVotePrice = 1000000L;

	public static long millionScalar = 1000000L;

	public static int hybridModeCost = 1000000;

	public static readonly long[] nextYearCarDesignMinExpenses = new long[]
	{
		10000000L,
		7000000L,
		5000000L,
		7000000L,
		5000000L,
		9000000L,
		7500000L
	};

	public static long lastPlaceBonus = 250000L;

	public static long promotionBonus = 10000000L;

	public static long refuellingCost = 20000L;

	public static long largePitCrewCost = 15000L;

	public static long[] tyreSupplierCost = new long[]
	{
		8500L,
		10000L
	};

	public static long[] tyreTypeCost = new long[]
	{
		25000L,
		30000L,
		20000L,
		15000L,
		10000L
	};

	public static int lastChampionshipEventWeek = 48;

	public static float averageMilesPerLap = 2.75f;

	public static float smallPitCrewTimeModifier = 2f;

	public static float largePitCrewTimeModifier = 1f;

	public static float pitStopAverageTimeLoss = 25f;

	public static int ballastWeightKg = 100;

	public static float rubberRemovalRate = 0.01f;

	public static int aiOrderRefreshGateRate = 100;

	public static readonly int qualifyingThresholdForQ2 = 15;

	public static readonly int qualifyingThresholdForQ3 = 10;

	// Chance for parts for critical error (car dropping out) if total condition loss
	public static float frontWingRate = 0.2f;
	public static float rearWingRate = 0.5f;
	public static float engineRate = 1.0f;
	public static float brakesRate = 1.0f;
	public static float suspensionRate = 1.0f;
	public static float gearBoxRate = 1.0f;

	public static int replacementPeopleCount = 20;

	public static int minTweetValue = 2;

	public static int maxTweetValue = 4;

	public static int[] ambitiousTraitIDS = new int[]
	{
		5,
		16,
		24,
		26,
		80
	};

	public static int[] slackerTraitIDS = new int[]
	{
		7,
		17,
		25,
		292
	};

	public static int[] unpredictableTraitIDS = new int[]
	{
		27,
		84,
		85,
		86,
		286
	};
}
