using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelectBossSkill", story: "Select [SelectedSkill] for [Self] against [Target] excluding [LastSkill]", category: "Action", id: "343e17a0c561efd7efba6afb3187e88d")]
public partial class SelectBossSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<BossSkillType> SelectedSkill;
    [SerializeReference] public BlackboardVariable<BossSkillType> LastSkill;

    private BossSkillType[] meleeSkills = { BossSkillType.Slam, BossSkillType.TailWhip, BossSkillType.Meteor, BossSkillType.Shockwave };
    private BossSkillType[] rangedSkills = { BossSkillType.Rush, BossSkillType.Meteor, BossSkillType.Shockwave };


    protected override Status OnStart()
    {
        if (Self == null || Self.Value == null || Target == null || Target.Value == null || SelectedSkill == null || LastSkill == null)
            return Status.Failure;

        if (!Target.Value.TryGetComponent<Player>(out var targetPlayer) || !targetPlayer.IsAlive)
            return Status.Failure;

        //초기화
        SelectedSkill.Value = BossSkillType.None;

        BossRangeSettings ranges = Self.Value.GetComponent<BossRangeSettings>();

        if (ranges == null)
            return Status.Failure;

        //타겟과의 거리계산
        Vector3 offset = Target.Value.transform.position - Self.Value.transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        BossSkillType[] skills;
        //스킬 유형 선택
        if (distance <= ranges.MeleeRange)
            skills = meleeSkills;
        else if (distance <= ranges.RangedRange)
            skills = rangedSkills;
        else
            return Status.Failure;

        var candidates = new System.Collections.Generic.List<BossSkillType>();
        //직전 쓴 스킬 제외
        foreach (BossSkillType skill in skills)
        {
            if (skill == BossSkillType.None || skill == BossSkillType.Wipe || skill == LastSkill.Value)
                continue;

            if (!candidates.Contains(skill))
                candidates.Add(skill);
        }

        if (candidates.Count == 0)
            return Status.Failure;
        //렌덤으로 스킬 선택 
        int index = UnityEngine.Random.Range(0, candidates.Count);
        SelectedSkill.Value = candidates[index];

        Debug.Log($"선택한 스킬: {SelectedSkill.Value}");

        return Status.Success;
    }
}

