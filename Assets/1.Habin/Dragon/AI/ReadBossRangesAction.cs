using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReadBossRanges",story: "Read ranges from [Self]",category: "Action", id: "06827aed27045c27afb803f501d7293b")]
public partial class ReadBossRangesAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    protected override Status OnStart()
    {
        //가드절
        if (Self == null || Self.Value == null)
            return Status.Failure;
        //범위 BossRangeSettings에서 가져오기
        BossRangeSettings ranges = Self.Value.GetComponent<BossRangeSettings>();
        if (ranges == null)
        {
            Debug.LogError("보스에 BossRangeSettings가 없습니다.");
            return Status.Failure;
        }
        Debug.Log(
            $"탐색: {ranges.DetectionRange}, " +
            $"근거리: {ranges.MeleeRange}, " +
            $"원거리: {ranges.RangedRange}"
        );

        return Status.Success;
    }
}