using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static FSM;
using static InputManager;
using static SOSkillState;

public sealed class PlayerDeadState : PlayerState
{
    private readonly int DeadHash = Animator.StringToHash("Dead");

    public PlayerDeadState(FSM _refOwner, AnimationClip _refClip, AnimationEvent[] _arrClipEvents)
        : base(_refOwner, eStateType.Dead)
    {
        m_refClip = _refClip;
        m_arrClipEvents = _arrClipEvents;
    }

    public override void Init()
    {
        base.Init();

        m_refAnimEvent.RegisterFrame(m_refClip, m_arrClipEvents[0], FinishDeadEvent);
    }

    public override void Dispose()
    {
        m_refAnimEvent.UnregisterFrame(m_refClip, m_arrClipEvents[0], FinishDeadEvent);

        base.Dispose();
    }

    public override void Enter()
    {
        m_refOwner.Rigidbody.linearVelocity = Vector3.zero;
        m_refOwner.Animator.speed = 1f;
        m_refOwner.Animator.SetTrigger(DeadHash);
    }

    public override void UpdateState()
    {
      
    }

    public override void Exit()
    {
        
    }
    private void FinishDeadEvent(AnimationEvent _refCallback)
    {
         m_refOwner.Dead();
    }

}
