using System;
using System.Collections.Generic;
using UnityEngine;

public class ContractVariablesContainer
{
	public ContractVariablesContainer()
	{
		this.PopulateDriverContractVariables();
		this.PopulateEngineerContractVariables();
		this.PopulateMechanicContractVariables();
	}

	public ContractVariablesData GetVariablesData(Person inPerson)
	{
		if (inPerson is Driver && this.driverContractVariablesData != null)
		{
			int num = Mathf.RoundToInt(inPerson.GetStats().GetAbility()) - 1;
			num = Mathf.Clamp(num, 0, this.driverContractVariablesData.Length - 1);
			return this.driverContractVariablesData[num];
		}
		if (inPerson is Mechanic && this.mechanicContractVariablesData != null)
		{
			int num2 = Mathf.RoundToInt(inPerson.GetStats().GetAbility()) - 1;
			num2 = Mathf.Clamp(num2, 0, this.mechanicContractVariablesData.Length - 1);
			return this.mechanicContractVariablesData[num2];
		}
		if (inPerson is Engineer && this.engineerContractVariablesData != null)
		{
			int num3 = Mathf.RoundToInt(inPerson.GetStats().GetAbility()) - 1;
			num3 = Mathf.Clamp(num3, 0, this.engineerContractVariablesData.Length - 1);
			return this.engineerContractVariablesData[num3];
		}
		return null;
	}

	public float GetBaseDesiredWageForPerson(Person inPerson)
	{
		Dictionary<int, float> dictionary = null;
		if (inPerson is Driver)
		{
			dictionary = this.mDriverWageRangesCurve;
		}
		else if (inPerson is Mechanic)
		{
			dictionary = this.mMechanicWageRangesCurve;
		}
		else if (inPerson is Engineer)
		{
			dictionary = this.mEngineerWageRangesCurve;
		}
		GameUtility.Assert(dictionary != null, "ContractVariableContainer.GetDesiredWageForPerson() - Dictionary is null. This should not happening", null);
		float ability = inPerson.GetStats().GetAbility();
		int num = Mathf.FloorToInt(ability);
		int num2 = Mathf.CeilToInt(ability);
		float a = dictionary[num];
		float b = dictionary[num2];
		float t = ability - (float)num;
		return Mathf.Lerp(a, b, t);
	}

	private void PopulateDriverContractVariables()
	{
		this.driverContractVariablesData = new ContractVariablesData[5];
		for (int i = 0; i < this.driverContractVariablesData.Length; i++)
		{
			this.driverContractVariablesData[i] = new ContractVariablesData();
		}
		this.mDriverWageRangesCurve.Add(0, 3.6f);
		this.mDriverWageRangesCurve.Add(1, 4.0f);
		this.mDriverWageRangesCurve.Add(2, 4.5f);
		this.mDriverWageRangesCurve.Add(3, 8.5f);
		this.mDriverWageRangesCurve.Add(4, 13.5f);
		this.mDriverWageRangesCurve.Add(5, 23.5f);
		this.mDriverWageRangesCurve.Add(6, 40f);
		float[] inRanges = new float[]
		{
			0.06f,
			0.05f,
			0.04f,
			0.03f
		};
		float[] inRanges2 = new float[]
		{
			0.18f,
			0.16f,
			0.14f,
			0.12f
		};
		float[] inRanges3 = new float[]
		{
			0.8f,
			0.6f,
			0.4f,
			0.2f
		};
		float[] inRanges4 = new float[]
		{
			12f,
			8f,
			4f,
			1f
		};
		float[] inRanges5 = new float[]
		{
			30f,
			25f,
			20f,
			15f
		};
		float[] inRanges6 = new float[]
		{
			0.09f,
			0.07f,
			0.05f,
			0.03f
		};
		float[] inRanges7 = new float[]
		{
			0.3f,
			0.25f,
			0.2f,
			0.15f
		};
		float[] inRanges8 = new float[]
		{
			0.5f,
			0.45f,
			0.4f,
			0.35f
		};
		float[] inRanges9 = new float[]
		{
			2f,
			1.75f,
			1.5f,
			1.25f
		};
		float[] inRanges10 = new float[]
		{
			4f,
			3.5f,
			3f,
			2.5f
		};
		float[] inRaceBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inRaceBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inRaceBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inRaceBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inRaceBonuses5 = new float[]
		{
			0.1f,
			0.25f
		};
		float[] inQualifyingBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inQualifyingBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inQualifyingBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inQualifyingBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inQualifyingBonuses5 = new float[]
		{
			0.1f,
			0.25f
		};
		this.driverContractVariablesData[0].PopulateWageRanges(inRanges);
		this.driverContractVariablesData[0].PopulateSignOnFeeRanges(inRanges6);
		this.driverContractVariablesData[0].PopulateRaceBonuses(inRaceBonuses);
		this.driverContractVariablesData[0].PopulateQualifyingBonuses(inQualifyingBonuses);
		this.driverContractVariablesData[1].PopulateWageRanges(inRanges2);
		this.driverContractVariablesData[1].PopulateSignOnFeeRanges(inRanges7);
		this.driverContractVariablesData[1].PopulateRaceBonuses(inRaceBonuses2);
		this.driverContractVariablesData[1].PopulateQualifyingBonuses(inQualifyingBonuses2);
		this.driverContractVariablesData[2].PopulateWageRanges(inRanges3);
		this.driverContractVariablesData[2].PopulateSignOnFeeRanges(inRanges8);
		this.driverContractVariablesData[2].PopulateRaceBonuses(inRaceBonuses3);
		this.driverContractVariablesData[2].PopulateQualifyingBonuses(inQualifyingBonuses3);
		this.driverContractVariablesData[3].PopulateWageRanges(inRanges4);
		this.driverContractVariablesData[3].PopulateSignOnFeeRanges(inRanges9);
		this.driverContractVariablesData[3].PopulateRaceBonuses(inRaceBonuses4);
		this.driverContractVariablesData[3].PopulateQualifyingBonuses(inQualifyingBonuses4);
		this.driverContractVariablesData[4].PopulateWageRanges(inRanges5);
		this.driverContractVariablesData[4].PopulateSignOnFeeRanges(inRanges10);
		this.driverContractVariablesData[4].PopulateRaceBonuses(inRaceBonuses5);
		this.driverContractVariablesData[4].PopulateQualifyingBonuses(inQualifyingBonuses5);
	}

	private void PopulateEngineerContractVariables()
	{
		this.engineerContractVariablesData = new ContractVariablesData[5];
		for (int i = 0; i < this.engineerContractVariablesData.Length; i++)
		{
			this.engineerContractVariablesData[i] = new ContractVariablesData();
		}
		this.mEngineerWageRangesCurve.Add(0, 1.05f);
		this.mEngineerWageRangesCurve.Add(1, 1.1f);
		this.mEngineerWageRangesCurve.Add(2, 1.5f);
		this.mEngineerWageRangesCurve.Add(3, 2f);
		this.mEngineerWageRangesCurve.Add(4, 6f);
		this.mEngineerWageRangesCurve.Add(5, 8.5f);
		this.mEngineerWageRangesCurve.Add(6, 10f);
		float[] inRanges = new float[]
		{
			0.06f,
			0.05f,
			0.04f,
			0.03f
		};
		float[] inRanges2 = new float[]
		{
			0.18f,
			0.16f,
			0.14f,
			0.12f
		};
		float[] inRanges3 = new float[]
		{
			0.8f,
			0.6f,
			0.4f,
			0.2f
		};
		float[] inRanges4 = new float[]
		{
			2.5f,
			2f,
			1.5f,
			1f
		};
		float[] inRanges5 = new float[]
		{
			4.5f,
			4f,
			3.5f,
			3f
		};
		float[] inRanges6 = new float[]
		{
			0.06f,
			0.05f,
			0.04f,
			0.03f
		};
		float[] inRanges7 = new float[]
		{
			0.3f,
			0.25f,
			0.2f,
			0.15f
		};
		float[] inRanges8 = new float[]
		{
			0.5f,
			0.45f,
			0.4f,
			0.35f
		};
		float[] inRanges9 = new float[]
		{
			0.9f,
			0.8f,
			0.7f,
			0.6f
		};
		float[] inRanges10 = new float[]
		{
			2.5f,
			2f,
			1.5f,
			1f
		};
		float[] inRaceBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inRaceBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inRaceBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inRaceBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inRaceBonuses5 = new float[]
		{
			0.1f,
			0.15f
		};
		float[] inQualifyingBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inQualifyingBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inQualifyingBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inQualifyingBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inQualifyingBonuses5 = new float[]
		{
			0.1f,
			0.15f
		};
		this.engineerContractVariablesData[0].PopulateWageRanges(inRanges);
		this.engineerContractVariablesData[0].PopulateSignOnFeeRanges(inRanges6);
		this.engineerContractVariablesData[0].PopulateRaceBonuses(inRaceBonuses);
		this.engineerContractVariablesData[0].PopulateQualifyingBonuses(inQualifyingBonuses);
		this.engineerContractVariablesData[1].PopulateWageRanges(inRanges2);
		this.engineerContractVariablesData[1].PopulateSignOnFeeRanges(inRanges7);
		this.engineerContractVariablesData[1].PopulateRaceBonuses(inRaceBonuses2);
		this.engineerContractVariablesData[1].PopulateQualifyingBonuses(inQualifyingBonuses2);
		this.engineerContractVariablesData[2].PopulateWageRanges(inRanges3);
		this.engineerContractVariablesData[2].PopulateSignOnFeeRanges(inRanges8);
		this.engineerContractVariablesData[2].PopulateRaceBonuses(inRaceBonuses3);
		this.engineerContractVariablesData[2].PopulateQualifyingBonuses(inQualifyingBonuses3);
		this.engineerContractVariablesData[3].PopulateWageRanges(inRanges4);
		this.engineerContractVariablesData[3].PopulateSignOnFeeRanges(inRanges9);
		this.engineerContractVariablesData[3].PopulateRaceBonuses(inRaceBonuses4);
		this.engineerContractVariablesData[3].PopulateQualifyingBonuses(inQualifyingBonuses4);
		this.engineerContractVariablesData[4].PopulateWageRanges(inRanges5);
		this.engineerContractVariablesData[4].PopulateSignOnFeeRanges(inRanges10);
		this.engineerContractVariablesData[4].PopulateRaceBonuses(inRaceBonuses5);
		this.engineerContractVariablesData[4].PopulateQualifyingBonuses(inQualifyingBonuses5);
	}

	private void PopulateMechanicContractVariables()
	{
		this.mechanicContractVariablesData = new ContractVariablesData[5];
		for (int i = 0; i < this.mechanicContractVariablesData.Length; i++)
		{
			this.mechanicContractVariablesData[i] = new ContractVariablesData();
		}
		this.mMechanicWageRangesCurve.Add(0, 0.05f);
		this.mMechanicWageRangesCurve.Add(1, 0.1f);
		this.mMechanicWageRangesCurve.Add(2, 0.25f);
		this.mMechanicWageRangesCurve.Add(3, 0.5f);
		this.mMechanicWageRangesCurve.Add(4, 1f);
		this.mMechanicWageRangesCurve.Add(5, 5f);
		this.mMechanicWageRangesCurve.Add(6, 10f);
		float[] inRanges = new float[]
		{
			0.06f,
			0.05f,
			0.04f,
			0.03f
		};
		float[] inRanges2 = new float[]
		{
			0.18f,
			0.16f,
			0.14f,
			0.12f
		};
		float[] inRanges3 = new float[]
		{
			0.8f,
			0.6f,
			0.4f,
			0.2f
		};
		float[] inRanges4 = new float[]
		{
			1.75f,
			1.5f,
			1.25f,
			1f
		};
		float[] inRanges5 = new float[]
		{
			2.75f,
			2.5f,
			2.25f,
			2f
		};
		float[] inRanges6 = new float[]
		{
			0.01f,
			0.0075f,
			0.005f,
			0.0025f
		};
		float[] inRanges7 = new float[]
		{
			0.04f,
			0.03f,
			0.02f,
			0.01f
		};
		float[] inRanges8 = new float[]
		{
			0.125f,
			0.1f,
			0.075f,
			0.05f
		};
		float[] inRanges9 = new float[]
		{
			1f,
			0.75f,
			0.5f,
			0.25f
		};
		float[] inRanges10 = new float[]
		{
			2f,
			1.75f,
			1.5f,
			1.25f
		};
		float[] inRaceBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inRaceBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inRaceBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inRaceBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inRaceBonuses5 = new float[]
		{
			0.1f,
			0.15f
		};
		float[] inQualifyingBonuses = new float[]
		{
			0.01f,
			0.02f
		};
		float[] inQualifyingBonuses2 = new float[]
		{
			0.02f,
			0.04f
		};
		float[] inQualifyingBonuses3 = new float[]
		{
			0.04f,
			0.06f
		};
		float[] inQualifyingBonuses4 = new float[]
		{
			0.06f,
			0.08f
		};
		float[] inQualifyingBonuses5 = new float[]
		{
			0.1f,
			0.15f
		};
		this.mechanicContractVariablesData[0].PopulateWageRanges(inRanges);
		this.mechanicContractVariablesData[0].PopulateSignOnFeeRanges(inRanges6);
		this.mechanicContractVariablesData[0].PopulateRaceBonuses(inRaceBonuses);
		this.mechanicContractVariablesData[0].PopulateQualifyingBonuses(inQualifyingBonuses);
		this.mechanicContractVariablesData[1].PopulateWageRanges(inRanges2);
		this.mechanicContractVariablesData[1].PopulateSignOnFeeRanges(inRanges7);
		this.mechanicContractVariablesData[1].PopulateRaceBonuses(inRaceBonuses2);
		this.mechanicContractVariablesData[1].PopulateQualifyingBonuses(inQualifyingBonuses2);
		this.mechanicContractVariablesData[2].PopulateWageRanges(inRanges3);
		this.mechanicContractVariablesData[2].PopulateSignOnFeeRanges(inRanges8);
		this.mechanicContractVariablesData[2].PopulateRaceBonuses(inRaceBonuses3);
		this.mechanicContractVariablesData[2].PopulateQualifyingBonuses(inQualifyingBonuses3);
		this.mechanicContractVariablesData[3].PopulateWageRanges(inRanges4);
		this.mechanicContractVariablesData[3].PopulateSignOnFeeRanges(inRanges9);
		this.mechanicContractVariablesData[3].PopulateRaceBonuses(inRaceBonuses4);
		this.mechanicContractVariablesData[3].PopulateQualifyingBonuses(inQualifyingBonuses4);
		this.mechanicContractVariablesData[4].PopulateWageRanges(inRanges5);
		this.mechanicContractVariablesData[4].PopulateSignOnFeeRanges(inRanges10);
		this.mechanicContractVariablesData[4].PopulateRaceBonuses(inRaceBonuses5);
		this.mechanicContractVariablesData[4].PopulateQualifyingBonuses(inQualifyingBonuses5);
	}

	private Dictionary<int, float> mDriverWageRangesCurve = new Dictionary<int, float>();

	private Dictionary<int, float> mMechanicWageRangesCurve = new Dictionary<int, float>();

	private Dictionary<int, float> mEngineerWageRangesCurve = new Dictionary<int, float>();

	private ContractVariablesData[] driverContractVariablesData;

	private ContractVariablesData[] mechanicContractVariablesData;

	private ContractVariablesData[] engineerContractVariablesData;
}
