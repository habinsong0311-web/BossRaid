using System.Collections;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonRush : MonoBehaviourPun
{
    private const float Half = 0.5f;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bossRoot;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private CapsuleCollider rushHitBox;

    [Header("돌진")]
    [Min(0.01f)]
    [SerializeField] private float rushDuration = 0.8f;

    [Header("준비")]
    [Min(0.01f)]
    [SerializeField] private float preparationTime = 2f;

    [Min(0.01f)]
    [SerializeField] private float minimumTargetDistance = 0.01f;

    [Header("장판")]
    [Min(0.01f)]
    [SerializeField] private float rushWidth = 4f;
    [SerializeField] private float telegraphHeightOffset = 0.03f;

    [Header("애니메이션")]
    [SerializeField] private string preparationState = "Base Layer.Idle";
    [SerializeField] private string rushState = "Base Layer.Run";
    [SerializeField] private string endState = "Base Layer.Idle";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    [Header("테스트")]
    [SerializeField] private KeyCode testKey = KeyCode.R;

    private GameObject currentTelegraph;
    private Coroutine rushRoutine;
    private Tween moveTween;

    private bool isAttacking;
    private bool isPreparing;
    private float attackHeight;
    private Vector3 rushDestination;

    public bool IsAttacking => isAttacking;
    private bool HasAuthority => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (HasAuthority && Input.GetKeyDown(testKey))
            Rush();

        if (!HasAuthority && isPreparing && target != null)
        {
            rushDestination = target.position;
            rushDestination.y = attackHeight;

            UpdateTelegraph(bossRoot.position, rushDestination);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Rush()
    {
        if (!HasAuthority || isAttacking)
            return;

        if (!HasRequiredReferences() || target == null)
            return;

        int targetViewId = 0;

        if (PhotonNetwork.InRoom)
        {
            PhotonView targetView = target.GetComponentInParent<PhotonView>();

            if (targetView == null)
            {
                return;
            }

            targetViewId = targetView.ViewID;
        }

        BeginPreparation(target.position);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(ReceiveRushPreparation), RpcTarget.Others, targetViewId, bossRoot.position, bossRoot.rotation, target.position);

        rushRoutine = StartCoroutine(RushRoutine());
    }

    private bool HasRequiredReferences()
    {
        if (animator != null && bossRoot != null && attackPrefab != null && rushHitBox != null)
            return true;

        return false;
    }

    private void BeginPreparation(Vector3 targetPosition)
    {
        isAttacking = true;
        isPreparing = true;
        attackHeight = bossRoot.position.y;

        rushHitBox.enabled = false;
        rushDestination = targetPosition;
        rushDestination.y = attackHeight;

        currentTelegraph = Instantiate(attackPrefab);

        UpdateTelegraph(bossRoot.position, rushDestination);
        PlayAnimation(preparationState);
    }

    private IEnumerator RushRoutine()
    {
        float elapsed = 0f;

        while (elapsed < preparationTime)
        {
            if (target == null)
            {
                FinishAttack();
                yield break;
            }

            UpdateAim();

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target == null)
        {
            FinishAttack();
            yield break;
        }

        // 준비가 끝난 순간의 목적지를 확정합니다.
        UpdateAim();
        isPreparing = false;

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(ReceiveRushStart), RpcTarget.Others, bossRoot.position, rushDestination);

        PlayAnimation(rushState);

        rushHitBox.GetComponent<AttackDamage>().ResetHits();
        rushHitBox.enabled = true;

        // 이 코루틴은 방장 또는 오프라인 테스트에서만 실행됩니다.
        moveTween = bossRoot.DOMove(rushDestination, rushDuration).SetEase(Ease.Linear).SetLink(gameObject);

        yield return moveTween.WaitForCompletion();

        FinishAttack();
    }

    private void UpdateAim()
    {
        Vector3 startPosition = bossRoot.position;
        startPosition.y = attackHeight;
        bossRoot.position = startPosition;

        rushDestination = target.position;
        rushDestination.y = attackHeight;

        Vector3 direction = rushDestination - startPosition;

        if (direction.sqrMagnitude > minimumTargetDistance * minimumTargetDistance)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            Vector3 angles = bossRoot.eulerAngles;
            angles.y = yaw;
            bossRoot.eulerAngles = angles;
        }

        UpdateTelegraph(startPosition, rushDestination);
    }

    private void UpdateTelegraph(Vector3 startPosition, Vector3 destination)
    {
        if (currentTelegraph == null)
            return;

        startPosition.y = attackHeight;
        destination.y = attackHeight;

        Vector3 direction = destination - startPosition;
        float distance = direction.magnitude;
        float yaw = bossRoot.eulerAngles.y;

        if (distance > minimumTargetDistance)
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        Vector3 center = (startPosition + destination) * Half;
        center.y = attackHeight + telegraphHeightOffset;

        currentTelegraph.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
        currentTelegraph.transform.localScale = new Vector3(rushWidth, 1f, distance);
    }
    [PunRPC]
    private void ReceiveRushPreparation(int targetViewId, Vector3 bossPosition, Quaternion bossRotation, Vector3 targetPosition, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient || !isActiveAndEnabled)
            return;

        if (!HasRequiredReferences())
            return;

        CleanupAttack();
        bossRoot.SetPositionAndRotation(bossPosition, bossRotation);

        PhotonView targetView = PhotonView.Find(targetViewId);
        target = targetView != null ? targetView.transform : null;

        BeginPreparation(targetPosition);
    }
    [PunRPC]
    private void ReceiveRushStart(Vector3 startPosition, Vector3 destination, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient || !isAttacking)
            return;

        isPreparing = false;
        rushDestination = destination;

        UpdateTelegraph(startPosition, destination);
        PlayAnimation(rushState);
    }

    [PunRPC]
    private void ReceiveRushEnd(PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient || !isAttacking)
            return;

        CleanupAttack();
        PlayAnimation(endState);
    }

    private void FinishAttack()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(ReceiveRushEnd), RpcTarget.Others);

        CleanupAttack();
        PlayAnimation(endState);
    }

    private void PlayAnimation(string stateName)
    {
        animator.CrossFadeInFixedTime(stateName, transitionTime);
    }

    private void CleanupAttack()
    {
        if (rushRoutine != null)
            StopCoroutine(rushRoutine);

        moveTween?.Kill();

        if (rushHitBox != null)
            rushHitBox.enabled = false;

        if (currentTelegraph != null)
            Destroy(currentTelegraph);
        currentTelegraph = null;
        rushRoutine = null;
        moveTween = null;
        isPreparing = false;
        isAttacking = false;
    }
    private void OnDisable()
    {
        if (isAttacking && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(ReceiveRushEnd), RpcTarget.Others);

        CleanupAttack();
    }
}