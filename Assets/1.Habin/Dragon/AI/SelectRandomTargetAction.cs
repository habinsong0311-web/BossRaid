using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SelectRandomTarget", story: "Select [Target] for [Self]", category: "Action", id: "547b2b95d06ae0d95e242467c9c2d851")]
public partial class SelectRandomTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    [SerializeField] private string playerTag = "Player";

    [Header("재탐색 시간")]
    [Min(0.01f)]
    [SerializeField] private float searchInterval = 0.5f;

    private BossRangeSettings ranges;
    private float searchTimer;
    private readonly List<GameObject> candidates = new List<GameObject>();

    protected override Status OnStart()
    {
        if (Self == null || Self.Value == null || Target == null)
            return Status.Failure;

        Target.Value = null;
        ranges = Self.Value.GetComponent<BossRangeSettings>();

        if (ranges == null)
            return Status.Failure;

        searchTimer = searchInterval;

        if (TrySelectTarget())
            return Status.Success;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || ranges == null)
            return Status.Failure;

        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
            return Status.Running;

        searchTimer = searchInterval;

        if (TrySelectTarget())
            return Status.Success;

        return Status.Running;
    }

    private bool TrySelectTarget()
    {
        Debug.Log($"타겟 탐색 시각: {Time.time:F3}");
        candidates.Clear();

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        float rangeSquared = ranges.DetectionRange * ranges.DetectionRange;

        foreach (GameObject player in players)
        {
            //Hp가 0인 캐릭터는 타겟안됨 
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent == null || !playerComponent.IsAlive)
                continue;
            Vector3 offset = player.transform.position - Self.Value.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= rangeSquared)
                candidates.Add(player);
        }

        if (candidates.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        Target.Value = candidates[index];

        Debug.Log($"어글자 선택: {Target.Value.name}");

        return true;
    }
}
