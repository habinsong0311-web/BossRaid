using UnityEngine;

public class DragonSlam : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("장판 프리펩")]
    [SerializeField] private CircleTelegraph attackPrefab;
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
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스 입력됨");
            Slam();
        }
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

        currentTelegraph.transform.localScale =
            new Vector3(slamRange, 1f, slamRange);

        // 공격을 시작하면 원이차기시작함
        currentTelegraph.Play(warningTime, null);
        PlaySlamAnimation();
    }

    private void PlaySlamAnimation()
    {
        animator.CrossFadeInFixedTime(slamAnimationState, 0.3f);
    }

    // Animation Event: 바닥을 치는 순간
    public void SlamHit()
    {
        if (!isAttacking || hasHit)
            return;

        hasHit = true;

        Debug.Log("내려찍기 타격!");
        // 데미지 계산 추가
        RemoveTelegraph();
    }

    // Animation Event: 공격 모션 끝
    public void SlamEnd()
    {
        Debug.Log("내려찍기 종료");
        RemoveTelegraph();
        isAttacking = false;
    }

    private void RemoveTelegraph()
    {//장판 삭제
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph.gameObject);
            currentTelegraph = null;
        }
    }

    private void OnDisable()
    {
        RemoveTelegraph();
        isAttacking = false;
    }
}