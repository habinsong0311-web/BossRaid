using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using static Enums_JJH;
using static FSM;
using static InputManager;
using static SOSkillState;

public sealed class PlayerSkillState : PlayerState
{
    private readonly SkillSpawner m_refSpawner;
    private readonly Dictionary<SkillAnimationType, int> m_hashTrigger = new();
    private readonly SkillPointer m_refPointer;
    private bool m_bTargeting;
    private int m_iCurrentSkill = -1;

    public PlayerSkillState(FSM _refOwner, SkillSpawner _refSpawner)
        : base(_refOwner, eStateType.Skill)
    {
        m_refSpawner = _refSpawner;
        m_refPointer = _refOwner.GetComponentInChildren<SkillPointer>();
    }

    public override void Init()
    {
        base.Init();
        m_hashTrigger[SkillAnimationType.Horizontal] = Animator.StringToHash("Test");
        m_hashTrigger[SkillAnimationType.Vertical] = Animator.StringToHash("Test2");
        m_hashTrigger[SkillAnimationType.GroundAttack] = Animator.StringToHash("GroundAttack");
        m_hashTrigger[SkillAnimationType.Hand] = Animator.StringToHash("Hand");
        m_refSpawner.OnSkillEnd += EndSkill;
        InputManager.m_Instance.Skill0Action.started += RequestSkill0;
        InputManager.m_Instance.Skill1Action.started += RequestSkill1;
        InputManager.m_Instance.Skill2Action.started += RequestSkill2;
        InputManager.m_Instance.Skill3Action.started += RequestSkill3;
        InputManager.m_Instance.Skill4Action.started += RequestSkill4;
    }

    public override void Dispose()
    {
        if (m_refSpawner != null)
            m_refSpawner.OnSkillEnd -= EndSkill;
        if (InputManager.m_Instance != null)
        {
            InputManager.m_Instance.Skill0Action.started -= RequestSkill0;
            InputManager.m_Instance.Skill1Action.started -= RequestSkill1;
            InputManager.m_Instance.Skill2Action.started -= RequestSkill2;
            InputManager.m_Instance.Skill3Action.started -= RequestSkill3;
            InputManager.m_Instance.Skill4Action.started -= RequestSkill4;
        }

        base.Dispose();
    }
    
    private void RequestSkill0(InputAction.CallbackContext _tContext)
        => RequestSkill(1001);

    private void RequestSkill1(InputAction.CallbackContext _tContext)
        => RequestSkill(1002);

    private void RequestSkill2(InputAction.CallbackContext _tContext)
        => RequestSkill(1000);

    private void RequestSkill3(InputAction.CallbackContext _tContext)
    {
        if (m_refOwner.IsMine && CanStartSkill() && !m_bTargeting && m_refPointer != null)
            TargetGroundSkill().Forget();
    }

    private void RequestSkill4(InputAction.CallbackContext _tContext)
        => RequestSkill(1004);

    private async UniTask TargetGroundSkill()
    {
        m_bTargeting = true;
        // Input callbacks run before Update; the opening E press must not confirm the pointer.
        await UniTask.NextFrame();
        if (m_refOwner == null || m_refPointer == null)
        {
            m_bTargeting = false;
            return;
        }
        Vector3? vTarget = await m_refPointer.ActiveSkillPointer();
        m_bTargeting = false;
        if (m_refOwner != null && vTarget.HasValue)
            RequestSkill(1003, vTarget);
    }

    private void RequestSkill(int _iSkillId, Vector3? _vTarget = null)
    {
        Debug.Log(_iSkillId);
        if (!m_refOwner.IsMine)
            return;

        SkillDataLoader refLoader = SkillDataLoader.Instance;
        if (!refLoader.SkillData.TryGetValue(_iSkillId, out SkillData tData) ||
            !m_hashTrigger.TryGetValue(tData.AnimationType, out int iTrigger))
            return;

        Debug.Log("데이터 있음");
        if (!m_refOwner.ChangeState(eStateType.Skill))
            return;

        m_iCurrentSkill = _iSkillId;
        bool bQueued = _vTarget.HasValue
            ? m_refSpawner.UseSkillAOE(m_iCurrentSkill, _vTarget.Value)
            : m_refSpawner.UseSkill(m_iCurrentSkill);
        if (!bQueued)
        {
            EndSkill();
            return;
        }
        m_refOwner.Animator.SetTrigger(iTrigger);
    }

    private bool CanStartSkill()
    {
        eStateType? eCurrent = m_refOwner.CurrentState?.Type;
        return m_refOwner.HasGameplayInput &&
            (eCurrent == eStateType.Idle || eCurrent == eStateType.Walk || eCurrent == eStateType.Run);
    }

    public override bool CanEnter() => CanStartSkill();

    public override void Enter()
    {
        m_refOwner.Rigidbody.rotation = m_refOwner.ViewRotation;
        Vector3 vVelocity = m_refOwner.Rigidbody.linearVelocity;
        m_refOwner.Rigidbody.linearVelocity = new Vector3(0f, vVelocity.y, 0f);
    }

    public override void Exit()
    {
        m_iCurrentSkill = -1;
    }

    private void EndSkill()
    {
        if (m_refOwner.IsMine && m_refOwner.CurrentState?.Type == eStateType.Skill)
            m_refOwner.ChangeState(PlayerMoveState.SelectState());
    }

}
