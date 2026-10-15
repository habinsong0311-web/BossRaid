using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MoveToTarget", story: "Move [Self] toward [Target] considering [LastSkill]", category: "Action", id: "8cfa2e326c45b9e861287cd888ebdf27")]
public partial class MoveToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<BossSkillType> LastSkill;

    [Header("이동")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 3f;

    [Min(0f)]
    [SerializeField] private float turnSpeed = 180f;

    [Header("애니메이션")]
    [SerializeField] private string walkState = "Base Layer.Walk";
    [SerializeField] private string idleState = "Base Layer.Idle";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    private BossRangeSettings ranges;
    private Animator animator;
    private bool startedWalking;

    protected override Status OnStart()
    {
        startedWalking = false;
        animator = null;

        if (Self == null || Self.Value == null || Target == null || Target.Value == null || LastSkill == null)
            return Status.Failure;

        if (!Target.Value.TryGetComponent<Player>(out var targetPlayer) || !targetPlayer.IsAlive)
            return Status.Failure;

        ranges = Self.Value.GetComponent<BossRangeSettings>();
        animator = Self.Value.GetComponent<Animator>();

        if (ranges == null || animator == null)
            return Status.Failure;

        if (GetHorizontalDistance() <= GetStopRange())
            return Status.Success;

        animator.CrossFadeInFixedTime(walkState, transitionTime);
        startedWalking = true;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || Target.Value == null || ranges == null)
            return Status.Failure;

        if (!Target.Value.TryGetComponent<Player>(out var targetPlayer) || !targetPlayer.IsAlive)
            return Status.Failure;

        Transform boss = Self.Value.transform;

        Vector3 destination = Target.Value.transform.position;
        destination.y = boss.position.y;

        Vector3 direction = destination - boss.position;
        float distance = direction.magnitude;
        float stopRange = GetStopRange();

        if (distance <= stopRange)
            return Status.Success;

        float step = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopRange);
        boss.position += direction.normalized * step;

        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        Vector3 angles = boss.eulerAngles;
        angles.y = Mathf.MoveTowardsAngle(angles.y, targetYaw, turnSpeed * Time.deltaTime);
        boss.eulerAngles = angles;

        if (GetHorizontalDistance() <= stopRange)
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (startedWalking && animator != null)
            animator.CrossFadeInFixedTime(idleState, transitionTime);

        startedWalking = false;
    }

    private float GetStopRange()
    {
        return ranges.RangedRange;
    }

    private float GetHorizontalDistance()
    {
        Vector3 offset = Target.Value.transform.position - Self.Value.transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
}
