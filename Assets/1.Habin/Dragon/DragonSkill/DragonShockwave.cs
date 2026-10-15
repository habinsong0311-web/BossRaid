using System.Collections;
using UnityEngine;

public class DragonShockwave : MonoBehaviour
{
    private const float RadiusToDiameter = 2f;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private ShockwaveRing shockwavePrefab;

    [Header("예고 장판")]
    [SerializeField] private float warningHeightOffset = 0.03f;
    [Header("애니메이션")]
    [SerializeField] private string animationState = "Base Layer.Shockwave";

    [Min(0f)]
    [SerializeField] private float transitionTime = 0.1f;

    private GameObject currentWarning;
    private ShockwaveRing currentRing;
    private Coroutine attackRoutine;

    private Vector3 attackCenter;
    private bool isAttacking;
    private bool hasSpawned;
    private bool motionEnded;

    public bool IsAttacking => isAttacking;
    public Vector3 AttackCenter => attackCenter;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    public void Shockwave()
    {
        if (spawnPoint == null)
            return;

        Shockwave(spawnPoint.position);
    }
    public void Shockwave(Vector3 center)
    {
        if (isAttacking)
            return;

        if (animator == null || warningPrefab == null || shockwavePrefab == null)
        {
            return;
        }

        isAttacking = true;
        hasSpawned = false;
        motionEnded = false;

        attackCenter = center;

        CreateWarning();
        animator.CrossFadeInFixedTime(animationState, transitionTime);
    }

    private void CreateWarning()
    {
        Vector3 position = attackCenter + Vector3.up * warningHeightOffset;
        currentWarning = Instantiate(warningPrefab, position, Quaternion.identity);

        float diameter = shockwavePrefab.EndRadius * RadiusToDiameter;
        currentWarning.transform.localScale = new Vector3(diameter, 1f, diameter);
    }

    // Shockwave 클립의 타격 순간에 연결합니다.
    public void SpawnShockwave()
    {
        if (!isAttacking || hasSpawned)
            return;

        hasSpawned = true;
        RemoveWarning();

        currentRing = Instantiate(shockwavePrefab, attackCenter, shockwavePrefab.transform.rotation);
        currentRing.SetBoss(GetComponent<DragonBoss>());
        attackRoutine = StartCoroutine(WaitForEnd());
    }

    // Shockwave 클립의 종료 직전에 연결합니다.
    public void ShockwaveMotionEnd()
    {
        if (!isAttacking)
            return;

        motionEnded = true;

        if (!hasSpawned)
        {
            CleanupAttack();
        }
    }

    private IEnumerator WaitForEnd()
    {
        // 고리 확산과 공격 모션이 모두 끝나야 공격을 종료합니다.
        while (currentRing != null || !motionEnded)
            yield return null;

        attackRoutine = null;
        CleanupAttack();
    }

    private void RemoveWarning()
    {
        if (currentWarning == null)
            return;

        Destroy(currentWarning);
        currentWarning = null;
    }

    private void CleanupAttack()
    {
        RemoveWarning();

        if (currentRing != null)
            Destroy(currentRing.gameObject);

        currentRing = null;
        isAttacking = false;
        hasSpawned = false;
        motionEnded = false;
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = null;
        CleanupAttack();
    }
}