using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ExecuteBossSkill", story: "Execute [SelectedSkill] for [Self] against [Target] and record [LastSkill]", category: "Action", id: "283523355066cd36e5240023c173edc9")]
public partial class ExecuteBossSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<BossSkillType> SelectedSkill;
    [SerializeReference] public BlackboardVariable<BossSkillType> LastSkill;

    private DragonSlam slam;
    private DragonTailWhip tailWhip;
    private DragonRush rush;
    private DragonMeteor meteor;
    private BossSkillType executingSkill;

    protected override Status OnStart()
    {
        if (Self == null || Self.Value == null || Target == null || Target.Value == null || SelectedSkill == null || LastSkill == null)
            return Status.Failure;

        if (!Target.Value.TryGetComponent<Player>(out var targetPlayer) || !targetPlayer.IsAlive)
            return Status.Failure;

        slam = Self.Value.GetComponent<DragonSlam>();
        tailWhip = Self.Value.GetComponent<DragonTailWhip>();
        rush = Self.Value.GetComponent<DragonRush>();
        meteor = Self.Value.GetComponent<DragonMeteor>();

        // 다른 공격이 실행 중이면 시작하지 않습니다.
        if ((slam != null && slam.IsAttacking) || (tailWhip != null && tailWhip.IsAttacking) || (rush != null && rush.IsAttacking))
            return Status.Failure;

        executingSkill = SelectedSkill.Value;

        switch (executingSkill)
        {
            case BossSkillType.Slam:
                if (slam == null || !slam.isActiveAndEnabled)
                    return Status.Failure;

                slam.Slam();
                break;

            case BossSkillType.TailWhip:
                if (tailWhip == null || !tailWhip.isActiveAndEnabled)
                    return Status.Failure;

                tailWhip.TailWhip();
                break;

            case BossSkillType.Rush:
                if (rush == null || !rush.isActiveAndEnabled)
                    return Status.Failure;

                rush.SetTarget(Target.Value.transform);
                rush.Rush();
                break;

            case BossSkillType.Meteor:
                if (meteor == null || !meteor.isActiveAndEnabled)
                    return Status.Failure;

                meteor.SetTarget(Target.Value.transform);
                meteor.Meteor();
                break;

            default:
                // 아직 실행을 연결하지 않은 스킬입니다.
                return Status.Failure;
        }

        // 참조 누락 등으로 스킬 시작에 실패하면 기록하지 않습니다.
        if (!IsSkillRunning())
            return Status.Failure;

        LastSkill.Value = executingSkill;

        Debug.Log($"공격 시작: {executingSkill}");

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || !HasActiveSkill())
            return Status.Failure;

        if (IsSkillRunning())
            return Status.Running;

        Debug.Log($"공격 종료: {executingSkill}");

        return Status.Success;
    }

    private bool IsSkillRunning()
    {
        switch (executingSkill)
        {
            case BossSkillType.Slam:
                return slam != null && slam.IsAttacking;

            case BossSkillType.TailWhip:
                return tailWhip != null && tailWhip.IsAttacking;

            case BossSkillType.Rush:
                return rush != null && rush.IsAttacking;

            case BossSkillType.Meteor:
                return meteor != null && meteor.IsAttacking;

            default:
                return false;
        }
    }

    private bool HasActiveSkill()
    {
        switch (executingSkill)
        {
            case BossSkillType.Slam:
                return slam != null && slam.isActiveAndEnabled;

            case BossSkillType.TailWhip:
                return tailWhip != null && tailWhip.isActiveAndEnabled;

            case BossSkillType.Rush:
                return rush != null && rush.isActiveAndEnabled;

            case BossSkillType.Meteor:
                return meteor != null && meteor.isActiveAndEnabled;

            default:
                return false;
        }
    }
}

