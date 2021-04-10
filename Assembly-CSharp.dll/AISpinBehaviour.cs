using System;
using FullSerializer;
using UnityEngine;

[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public class AISpinBehaviour : AIRacingBehaviour
{
	public AISpinBehaviour()
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static AISpinBehaviour()
	{
	}

	public override void Start(Vehicle inVehicle)
	{
		base.Start(inVehicle);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		this.SetState(AISpinBehaviour.SpinState.SpiningOut);
		this.mRacingVehicle.behaviourManager.SetCanDefendVehicle(false);
		this.mInitialBraking = 20f;
		this.mRecoveryTime = (float)RandomUtility.GetRandom(3, 4);
		this.mCurrentAngle = 0f;
		this.mTargetAngle = RandomUtility.GetRandom(0.5f, 2f) * 180f;
		Vector3 normalized = (this.mRacingVehicle.pathController.GetPreviousGate().position - this.mRacingVehicle.pathController.GetNextGate().position).normalized;
		float f = Vector3.Dot(normalized, this.mRacingVehicle.transform.forward);
		float num = Mathf.Acos(f) * 57.29578f;
		if (num < 0f)
		{
			this.mTargetAngle = -this.mTargetAngle;
		}
		this.SetTarget();
		CommentaryManager.SendComment(this.mRacingVehicle, Comment.CommentType.Spins, new object[]
		{
			this.mRacingVehicle.driver
		});
	}

	public override void Destroy()
	{
		SafeAction.NullAnAction(ref this.OnSpinMessage);
	}

	private void SetState(AISpinBehaviour.SpinState inState)
	{
		this.mState = inState;
		this.mTimer = 0f;
		AISpinBehaviour.SpinState spinState = this.mState;
		if (spinState != AISpinBehaviour.SpinState.Refocus)
		{
			if (spinState == AISpinBehaviour.SpinState.GoBackToTrack)
			{
				this.mRacingVehicle.SetBraking(0f);
				this.mRacingVehicle.steeringManager.DeactivateAllBehaviours();
				SteeringManager.Behaviour[] steeringBehaviours = this.mRacingVehicle.behaviourManager.GetBehaviour(AIBehaviourStateManager.Behaviour.Racing).steeringBehaviours;
				for (int i = 0; i < steeringBehaviours.Length; i++)
				{
					this.mRacingVehicle.steeringManager.GetBehaviour(steeringBehaviours[i]).SetActive(true);
				}
				this.mRacingVehicle.speedManager.DeactivateAllControllers();
				SpeedManager.Controller[] speedControllers = this.mRacingVehicle.behaviourManager.GetBehaviour(AIBehaviourStateManager.Behaviour.Racing).speedControllers;
				for (int j = 0; j < speedControllers.Length; j++)
				{
					this.mRacingVehicle.speedManager.GetController(speedControllers[j]).SetActive(true);
				}
			}
		}
		else
		{
			float random = RandomUtility.GetRandom(0f, 0.05f);
			if (Mathf.Approximately(random, 0f))
			{
				this.mRacingVehicle.setup.tyreSet.ApplyTyreWearFromLockUp(random);
			}
			if (this.OnSpinMessage != null)
			{
				this.OnSpinMessage.Invoke();
			}
		}
	}

	private void SetTarget()
	{
		AISpinBehaviour.SpinState spinState = this.mState;
		if (spinState != AISpinBehaviour.SpinState.SpiningOut)
		{
			if (spinState == AISpinBehaviour.SpinState.Refocus)
			{
				this.mCurrentAngle = Mathf.MoveTowards(this.mCurrentAngle, 0f, (Mathf.Clamp01(this.mTimer) * (this.mCurrentAngle / 2f) + 20f) * GameTimer.simulationDeltaTime);
				this.mRotation = Quaternion.AngleAxis(this.mCurrentAngle, Vector3.up) * this.mVehicle.transform.forward;
				this.SetTarget(this.mRotation);
			}
		}
		else
		{
			this.mCurrentAngle = Mathf.MoveTowards(this.mCurrentAngle, this.mTargetAngle, Mathf.Clamp01(this.mTimer) * this.mTargetAngle / 2f * GameTimer.simulationDeltaTime);
			this.mRotation = Quaternion.AngleAxis(this.mCurrentAngle, Vector3.up) * this.mVehicle.transform.forward;
			this.SetBraking(this.mCurrentAngle);
			this.SetTarget(this.mRotation);
		}
	}

	private void SetTarget(Vector3 inVector)
	{
		this.mRacingVehicle.unityVehicle.SetRotationTargetType(UnityVehicle.RotationTarget.Target);
		this.mRacingVehicle.unityVehicle.SetRotationTarget(inVector);
	}

	private void SetBraking(float inRotation)
	{
		inRotation -= inRotation % 90f;
		float num = 1f - Mathf.Clamp01(Mathf.Abs(inRotation) / 90f);
		this.mRacingVehicle.SetBraking(this.mInitialBraking * 0.5f + this.mInitialBraking * num * 0.5f);
	}

	public override void OnExit()
	{
		base.OnExit();
		this.mRacingVehicle.unityVehicle.SetRotationTargetType(UnityVehicle.RotationTarget.ForwardVector);
		this.mRacingVehicle.behaviourManager.SetCanDefendVehicle(true);
	}

	public override void SimulationUpdate()
	{
		this.SetTarget();
		switch (this.mState)
		{
		case AISpinBehaviour.SpinState.SpiningOut:
			this.mTimer += GameTimer.simulationDeltaTime;
			if (this.mRacingVehicle.HasStopped())
			{
				this.SetState(AISpinBehaviour.SpinState.Refocus);
			}
			break;
		case AISpinBehaviour.SpinState.Refocus:
			this.mTimer += GameTimer.simulationDeltaTime;
			if (this.mTimer > this.mRecoveryTime)
			{
				this.SetState(AISpinBehaviour.SpinState.GoBackToTrack);
			}
			break;
		case AISpinBehaviour.SpinState.GoBackToTrack:
			if (this.mQueuedBehaviour == null)
			{
				this.mRacingVehicle.behaviourManager.ReturnToPreviousBehaviour();
			}
			else
			{
				this.mRacingVehicle.behaviourManager.ChangeBehaviour(this.mQueuedBehaviour.behaviourType);
			}
			break;
		}
	}

	public override bool HandleMessage(Vehicle inSender, AIMessage.Type inType, object inData)
	{
		return false;
	}

	public override void OnEnterGate(int inGateID, PathData.GateType inGateType)
	{
		base.OnEnterGate(inGateID, inGateType);
	}

	public override void OnFlagChange(SessionManager.Flag inFlag)
	{
	}

	public override AIBehaviourStateManager.Behaviour behaviourType
	{
		get
		{
			return AIBehaviourStateManager.Behaviour.Spin;
		}
	}

	public override SteeringManager.Behaviour[] steeringBehaviours
	{
		get
		{
			return AISpinBehaviour.mSteeringBehaviours;
		}
	}

	public override SpeedManager.Controller[] speedControllers
	{
		get
		{
			return AISpinBehaviour.mSpeedControllers;
		}
	}

	public Action OnSpinMessage;

	private static readonly SpeedManager.Controller[] mSpeedControllers = new SpeedManager.Controller[]
	{
		SpeedManager.Controller.SpiningOut
	};

	private static readonly SteeringManager.Behaviour[] mSteeringBehaviours = new SteeringManager.Behaviour[]
	{
		SteeringManager.Behaviour.SpinOut
	};

	private float mTimer;

	private float mRecoveryTime;

	private Vector3 mRotation;

	private float mTargetAngle;

	private float mCurrentAngle;

	private float mInitialBraking;

	private AISpinBehaviour.SpinState mState;

	public enum SpinState
	{
		SpiningOut,
		Refocus,
		GoBackToTrack
	}
}
