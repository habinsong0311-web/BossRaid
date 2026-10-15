using Photon.Pun;
using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonBoss : MonoBehaviourPun, ITakeDamgeable, IPunObservable
{
    [Header("설정")]
    [SerializeField] private MonsterData monsterData;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private BehaviorGraphAgent behaviorAgent;
    [SerializeField] private Collider bodyCollider;

    [Header("사망 시 중단할 스킬")]
    [SerializeField] private MonoBehaviour[] skillScripts;

    [Header("사망 애니메이션")]
    [SerializeField] private string deathState = "Base Layer.Death";
    [SerializeField] private float deathAnimationSpeed = 1f;

    [Header("현재 상태")]
    [SerializeField] private float currentHp;
    [SerializeField] private float attack;

    private bool deathHandled;

    public float Attack => attack;
    public float CurrentHp => currentHp;
    public bool IsDead => currentHp <= 0f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (behaviorAgent == null)
            behaviorAgent = GetComponent<BehaviorGraphAgent>();

        if (monsterData == null)
        {
            Debug.LogError("Monster Data를 연결하세요.", this);
            return;
        }

        currentHp = monsterData.MaxHp;
        attack = monsterData.Attack;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            TakeDamage(new tAttackInfo(transform.position, 100f));
    }
    public void TakeDamage(tAttackInfo attackInfo)
    {
        if (monsterData == null || IsDead)
            return;
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(ReceiveDamageRequest), RpcTarget.MasterClient, attackInfo.Damage);
            return;
        }

        ApplyDamage(attackInfo.Damage);
    }

    [PunRPC]
    private void ReceiveDamageRequest(float requestedDamage)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        ApplyDamage(requestedDamage);
    }

    private void ApplyDamage(float requestedDamage)
    {
        if (monsterData == null || IsDead)
            return;

        float damage = Mathf.Max(0f, requestedDamage);
        currentHp = Mathf.Max(0f, currentHp - damage);

        Debug.LogWarning($"보스 피격: {damage}, 남은 HP: {currentHp}", this);

        if (IsDead)
            Die();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHp);
        }
        else
        {
            currentHp = (float)stream.ReceiveNext();

            if (IsDead)
                Die();
        }
    }

    private void Die()
    {
        if (deathHandled)
            return;

        deathHandled = true;

        if (behaviorAgent != null)
            behaviorAgent.enabled = false;

        if (skillScripts != null)
        {
            foreach (MonoBehaviour skill in skillScripts)
            {
                if (skill != null)
                    skill.enabled = false;
            }
        }

        if (animator == null)
        {
            Debug.LogError("사망 모션 실패: Animator 연결 없음", this);
        }
        else
        {
            int deathHash = Animator.StringToHash(deathState);

            if (animator.HasState(0, deathHash))
            {
                animator.speed = deathAnimationSpeed;
                animator.Play(deathHash, 0, 0f);
            }
            else
            {
                Debug.LogError($"사망 애니메이션 상태를 찾지 못했습니다: {deathState}", this);
            }
        }

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        Debug.Log("보스 사망", this);
        Debug.Log($"Die 호출 — 방장: {PhotonNetwork.IsMasterClient}, HP: {currentHp}", this);
    }
}