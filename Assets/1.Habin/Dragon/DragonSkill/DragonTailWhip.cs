using UnityEngine;

public class DragonTailWhip : MonoBehaviour
{
    private const float RadiusToDiameter = 2f;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private HalfCircleTelegraph attackPrefab;
    [SerializeField] private CapsuleCollider tailHitBox;
    
    [Header("공격 범위")]
    [Min(0.01f)]
    [SerializeField] private float attackRadius = 7f;

    [Header("장판")]
    [Min(0.01f)]
    [SerializeField] private float preparationTime = 2f;

    [SerializeField] private float telegraphHeightOffset = 0.03f;
    [SerializeField] private float telegraphDirectionOffset;

    [Header("애니메이션")]
    [SerializeField]
    private string animationState = "Base Layer.Tail Whip R";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    private HalfCircleTelegraph currentTelegraph;

    private bool isAttacking;
    private bool isReady;
    private bool isFilled;
    private bool isSwinging;
    private bool hasHit;

    private float originalAnimatorSpeed;

    // 이후 데미지 판정에도 같은 위치와 방향을 사용합니다.
    private Vector3 attackCenter;
    private Quaternion attackRotation;

    public bool IsAttacking => isAttacking;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void TailWhip()
    {
        if (isAttacking)
            return;

        if (!HasRequiredReferences())
            return;

        isAttacking = true;
        isReady = false;
        isFilled = false;
        isSwinging = false;
        hasHit = false;

        originalAnimatorSpeed = animator.speed;

        CreateTelegraph();

        // 공격 모션과 장판 채우기를 동시에 시작합니다.
        animator.CrossFadeInFixedTime(animationState,transitionTime);

        currentTelegraph.Play(preparationTime,OnTelegraphFilled);
    }

    private bool HasRequiredReferences()
    {
        if (animator != null && attackPoint != null && attackPrefab != null)
        {
            return true;
        }
        return false;
    }

    private void CreateTelegraph()
    {
        attackCenter = attackPoint.position;

        float directionY = attackPoint.eulerAngles.y + telegraphDirectionOffset;

        attackRotation = Quaternion.Euler(0f, directionY, 0f);

        currentTelegraph = Instantiate(attackPrefab,attackCenter + Vector3.up * telegraphHeightOffset,attackRotation);
        float diameter = attackRadius * RadiusToDiameter;
        currentTelegraph.transform.localScale =
            new Vector3(diameter, 1f, diameter);
    }
    public void TailWhipReady()
    {
        if (!isAttacking || isReady)
            return;

        isReady = true;
        animator.speed = 0f;

        TryStartSwing();
    }
    private void OnTelegraphFilled()
    {
        if (!isAttacking)
            return;

        isFilled = true;
        TryStartSwing();
    }
    private void TryStartSwing()
    {
        if (!isAttacking || !isReady || !isFilled || isSwinging)
        {
            return;
        }

        isSwinging = true;

        animator.speed = originalAnimatorSpeed;
    }
    public void TailWhipHit()
    {
        if (!isAttacking || !isSwinging || hasHit)
            return;

        hasHit = true;
        tailHitBox.GetComponent<AttackDamage>().ResetHits();
        tailHitBox.enabled = true;

        RemoveTelegraph();
    }
    public void TailWhipEnd()
    {
        if (!isAttacking)
            return;
        tailHitBox.enabled = false;
        CleanupAttack();
    }

    private void RemoveTelegraph()
    {
        if (currentTelegraph == null)
            return;

        Destroy(currentTelegraph.gameObject);
        currentTelegraph = null;
    }

    private void CleanupAttack()
    {
        if (tailHitBox != null)
            tailHitBox.enabled = false;

        RemoveTelegraph();

        if (isAttacking && animator != null)
            animator.speed = originalAnimatorSpeed;

        isAttacking = false;
        isReady = false;
        isFilled = false;
        isSwinging = false;
        hasHit = false;
    }

    private void OnDisable()
    {
        CleanupAttack();
    }
}