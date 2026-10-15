using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonMeteor : MonoBehaviourPun
{
    private const float RadiusToDiameter = 2f;

    [Header("참조")]
    [SerializeField] private Transform target;
    [SerializeField] private CircleTelegraph telegraphPrefab;
    [SerializeField] private MeteorProjectile meteorPrefab;
    [SerializeField] private DragonBoss boss;

    [Header("운석 수")]
    [Min(1)]
    [SerializeField] private int meteorCount = 10;

    [Header("범위")]
    [Min(0f)]
    [SerializeField] private float scatterRadius = 10f;

    [Header("운석 장판 넓이")]
    [Min(0.01f)]
    [SerializeField] private float impactRadius = 2f;

    [Header("높이")]
    [Min(0.01f)]
    [SerializeField] private float spawnHeight = 15f;

    [Header("예고 시간")]
    [Min(0.01f)]
    [SerializeField] private float warningTime = 1.5f;

    [Header("간격")]
    [Min(0f)]
    [SerializeField] private float meteorInterval = 0.2f;

    [Header("장판 높이")]
    [SerializeField] private float telegraphHeightOffset = 0.03f;

    [Header("바닥 탐색")]
    [SerializeField] private LayerMask groundLayers;

    [Min(0.01f)]
    [SerializeField] private float groundSearchHeight = 30f;

    [Min(0.01f)]
    [SerializeField] private float groundSearchDepth = 60f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string summonState = "Base Layer.Fire Head 2";
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField] private float transitionTime = 0.1f;

    private readonly List<CircleTelegraph> telegraphs = new List<CircleTelegraph>();
    private readonly List<MeteorProjectile> meteors = new List<MeteorProjectile>();
    private Coroutine attackRoutine;
    private bool isAttacking;
    public bool IsAttacking => isAttacking;
    private bool HasAuthority => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (boss == null)
            boss = GetComponent<DragonBoss>();
    }


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    public void Meteor()
    {
        if (!HasAuthority || isAttacking)
            return;

        if (!HasRequiredReferences() || target == null)
            return;
        Vector3[] impactPoints = CalculateImpactPoints(target.position);

        if (impactPoints.Length == 0)
        {
            return;
        }

        BeginMeteor(impactPoints);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(ReceiveMeteor), RpcTarget.Others, transform.position, transform.rotation, impactPoints);
    }
    private bool HasRequiredReferences()
    {
        if (animator != null && boss != null && telegraphPrefab != null && meteorPrefab != null)
            return true;

        return false;
    }
    private Vector3[] CalculateImpactPoints(Vector3 center)
    {
        List<Vector3> impactPoints = new List<Vector3>();

        for (int i = 0; i < meteorCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 candidate = center + new Vector3(randomOffset.x, 0f, randomOffset.y);
            Vector3 rayOrigin = candidate + Vector3.up * groundSearchHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSearchDepth, groundLayers, QueryTriggerInteraction.Ignore))
                impactPoints.Add(hit.point);
        }

        return impactPoints.ToArray();
    }
    [PunRPC]
    private void ReceiveMeteor(Vector3 bossPosition, Quaternion bossRotation, Vector3[] impactPoints, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient || !isActiveAndEnabled)
            return;

        if (!HasRequiredReferences())
            return;

        transform.SetPositionAndRotation(bossPosition, bossRotation);

        BeginMeteor(impactPoints);
    }
    private void BeginMeteor(Vector3[] impactPoints)
    {
        CleanupAttack();

        isAttacking = true;
        animator.CrossFadeInFixedTime(summonState, transitionTime);

        attackRoutine = StartCoroutine(MeteorRoutine(impactPoints));
    }

    private IEnumerator MeteorRoutine(Vector3[] impactPoints)
    {
        for (int i = 0; i < impactPoints.Length; i++)
        {
            CreateWarning(impactPoints[i]);

            if (i < impactPoints.Length - 1 && meteorInterval > 0f)
                yield return new WaitForSeconds(meteorInterval);
        }

        while (true)
        {
            telegraphs.RemoveAll(item => item == null);
            meteors.RemoveAll(item => item == null);

            if (telegraphs.Count == 0 && meteors.Count == 0)
                break;

            yield return null;
        }

        isAttacking = false;
        attackRoutine = null;
    }

    private void CreateWarning(Vector3 impactPoint)
    {
        CircleTelegraph telegraph = Instantiate(telegraphPrefab, impactPoint + Vector3.up * telegraphHeightOffset, Quaternion.identity);

        float diameter = impactRadius * RadiusToDiameter;
        telegraph.transform.localScale = new Vector3(diameter, 1f, diameter);

        telegraphs.Add(telegraph);
        telegraph.Play(warningTime, () => SpawnMeteor(telegraph, impactPoint));
    }

    private void SpawnMeteor(CircleTelegraph telegraph, Vector3 impactPoint)
    {
        if (!isAttacking || telegraph == null)
            return;

        Destroy(telegraph.gameObject);

        Vector3 spawnPosition = impactPoint + Vector3.up * spawnHeight;
        MeteorProjectile meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);

        AttackDamage attackDamage = meteor.GetComponent<AttackDamage>();

        if (attackDamage != null)
            attackDamage.SetBoss(boss);

        meteors.Add(meteor);
    }

    public void MeteorSummonEnd()
    {
        if (!isAttacking)
            return;

        animator.CrossFadeInFixedTime(idleState, transitionTime);
    }

    private void CleanupAttack()
    {
        isAttacking = false;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        foreach (CircleTelegraph telegraph in telegraphs)
        {
            if (telegraph != null)
                Destroy(telegraph.gameObject);
        }

        foreach (MeteorProjectile meteor in meteors)
        {
            if (meteor != null)
                Destroy(meteor.gameObject);
        }

        telegraphs.Clear();
        meteors.Clear();
        attackRoutine = null;
    }

    private void OnDisable()
    {
        CleanupAttack();
    }
}