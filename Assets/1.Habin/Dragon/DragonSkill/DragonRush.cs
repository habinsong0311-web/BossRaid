using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DragonRush : MonoBehaviour
{
    private const float Half = 0.5f;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bossRoot;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject attackPrefab;

    [Header("돌진")]
    [Min(0.01f)]
    [SerializeField] private float rushDistance = 12f;

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
    [SerializeField]
    private string preparationState =
        "Base Layer.Idle";

    [SerializeField]
    private string rushState =
        "Base Layer.Run";

    [SerializeField]
    private string endState =
        "Base Layer.Idle";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    [Header("테스트")]
    [SerializeField] private KeyCode testKey = KeyCode.R;

    private GameObject currentTelegraph;
    private Coroutine rushRoutine;
    private Tween moveTween;

    private bool isAttacking;
    private float attackHeight;
    private Vector3 rushDirection;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testKey))
            Rush();
    }

    public void Rush()
    {
        if (isAttacking)
            return;

        if (!HasRequiredReferences())
            return;

        isAttacking = true;

        // 공격 시작 시 보스의 높이를 저장합니다.
        attackHeight = bossRoot.position.y;

        // 타겟과 같은 위치여도 사용할 초기 수평 방향입니다.
        float directionY = bossRoot.eulerAngles.y;

        rushDirection =
            Quaternion.Euler(0f, directionY, 0f) * Vector3.forward;

        rushRoutine = StartCoroutine(RushRoutine());
    }

    private bool HasRequiredReferences()
    {
        if (animator != null &&
            bossRoot != null &&
            target != null &&
            attackPrefab != null)
        {
            return true;
        }

        Debug.LogError(
            "Animator, Boss Root, Target, Attack Prefab을 연결하세요.",
            this
        );

        return false;
    }

    private IEnumerator RushRoutine()
    {
        PlayAnimation(preparationState);

        currentTelegraph = Instantiate(attackPrefab);

        currentTelegraph.transform.localScale =
            new Vector3(rushWidth, 1f, rushDistance);

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

        // 마지막으로 방향을 계산한 뒤 고정합니다.
        UpdateAim();

        Vector3 startPosition = bossRoot.position;
        startPosition.y = attackHeight;

        Vector3 endPosition =
            startPosition + rushDirection * rushDistance;

        // 타겟 높이와 관계없이 같은 높이로 이동합니다.
        endPosition.y = attackHeight;

        PlayAnimation(rushState);

        moveTween = bossRoot.DOMove(endPosition, rushDuration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);

        yield return moveTween.WaitForCompletion();

        FinishAttack();
    }

    private void UpdateAim()
    {
        Vector3 bossPosition = bossRoot.position;
        bossPosition.y = attackHeight;
        bossRoot.position = bossPosition;

        Vector3 direction = target.position - bossPosition;

        // 위아래 방향은 무시하고 바닥 평면만 사용합니다.
        direction.y = 0f;

        float minimumDistanceSquared =
            minimumTargetDistance * minimumTargetDistance;

        if (direction.sqrMagnitude > minimumDistanceSquared)
            rushDirection = direction.normalized;

        Quaternion rotation =
            Quaternion.LookRotation(rushDirection, Vector3.up);

        // X·Z 기울기 없이 Y축으로만 회전합니다.
        bossRoot.rotation = rotation;

        Vector3 telegraphCenter =
            bossPosition + rushDirection * rushDistance * Half;

        telegraphCenter.y =
            attackHeight + telegraphHeightOffset;

        currentTelegraph.transform.SetPositionAndRotation(
            telegraphCenter,
            rotation
        );
    }

    private void PlayAnimation(string stateName)
    {
        animator.CrossFadeInFixedTime(
            stateName,
            transitionTime
        );
    }

    private void FinishAttack()
    {
        RemoveTelegraph();
        PlayAnimation(endState);

        moveTween = null;
        rushRoutine = null;
        isAttacking = false;
    }

    private void RemoveTelegraph()
    {
        if (currentTelegraph == null)
            return;

        Destroy(currentTelegraph);
        currentTelegraph = null;
    }

    private void OnDisable()
    {
        if (rushRoutine != null)
            StopCoroutine(rushRoutine);

        moveTween?.Kill();
        RemoveTelegraph();

        rushRoutine = null;
        moveTween = null;
        isAttacking = false;
    }
}