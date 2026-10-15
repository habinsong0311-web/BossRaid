using UnityEngine;

public class DragonSlam : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("장판 프리펩")]
    [SerializeField] private CircleTelegraph attackPrefab;
    [Header("히트박스 프리펩")]
    [SerializeField] private MeshCollider slamHitBox;
    [SerializeField] private float hitBoxHeight = 2f;

    private const float Half = 0.5f;
    [Header("장판 생성 위치")]
    [SerializeField] private Transform slamPoint;
    [Header("공격 범위")]
    [SerializeField] private float slamRange = 14f;
    [Header("장판 시간")]
    [SerializeField] private float warningTime = 1.7f;
    [Header("장판 높이 설정")]
    [SerializeField] private float telegraphHeightOffset = 0.03f;
    [Header("애니메이션 설정")]
    [SerializeField] private string slamAnimationState ="Base Layer.Attack 2";

    private CircleTelegraph currentTelegraph;
    private bool isAttacking;
    private bool hasHit;
    public bool IsAttacking => isAttacking;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Slam()
    {
        if (isAttacking)
            return;

        isAttacking = true;
        hasHit = false;
        //장판 생성
        currentTelegraph = Instantiate(attackPrefab,
            slamPoint.position + Vector3.up * telegraphHeightOffset,
            Quaternion.identity
        );

        currentTelegraph.transform.localScale = new Vector3(slamRange, 1f, slamRange);
        
        //히트박스 생성
        Transform hitBoxTransform = slamHitBox.transform;
        Vector3 parentScale = hitBoxTransform.parent.lossyScale;

        hitBoxTransform.localScale = new Vector3(slamRange / Mathf.Abs(parentScale.x), hitBoxHeight * Half / Mathf.Abs(parentScale.y), slamRange / Mathf.Abs(parentScale.z));
        hitBoxTransform.localPosition = new Vector3(0f, hitBoxHeight * Half / Mathf.Abs(parentScale.y), 0f);

        // 공격을 시작하면 원이차기시작함
        currentTelegraph.Play(warningTime, null);
        PlaySlamAnimation();
    }

    private void PlaySlamAnimation()
    {
        animator.CrossFadeInFixedTime(slamAnimationState, 0.3f);
    }
    public void SlamHit()
    {
        if (!isAttacking || hasHit)
            return;

        hasHit = true;

        slamHitBox.GetComponent<AttackDamage>().ResetHits();
        slamHitBox.enabled = true;
        RemoveTelegraph();
    }

    public void SlamEnd()
    {
        slamHitBox.enabled = false;
        hasHit = false;
        isAttacking = false;
        RemoveTelegraph();
    }

    private void RemoveTelegraph()
    {//장판 삭제
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph.gameObject);
            currentTelegraph = null;
        }
    }

    private void CleanupAttack()
    {
        if (slamHitBox != null)
            slamHitBox.enabled = false;

        RemoveTelegraph();
        hasHit = false;
        isAttacking = false;
    }

    private void OnDisable()
    {
        CleanupAttack();
    }
}