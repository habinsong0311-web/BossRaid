using Photon.Pun;
using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DragonBoss : MonoBehaviourPun, ITakeDamgeable, IPunObservable
{
    [Header("설정")]
    [SerializeField] private MonsterData monsterData;
    [Header("클리어 포탈")]
    [SerializeField] private GameObject clearPortal;

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
    [SerializeField] private float maxHp;
    [SerializeField] private float currentHp;
    [SerializeField] private float attack;


    [SerializeField] private BattleEndController battleEndController;
    private bool deathHandled;
    public event System.Action OnHpChanged;

    public float Attack => attack;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => currentHp <= 0f;
    public bool IsInvincible { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (behaviorAgent == null)
            behaviorAgent = GetComponent<BehaviorGraphAgent>();

        if (clearPortal != null)
            clearPortal.SetActive(false);

        if (monsterData == null)
        {
            Debug.LogError("Monster Data를 연결하세요.", this);
            return;
        }

        maxHp = monsterData.MaxHp;
        currentHp = maxHp;
        attack = monsterData.Attack;
    }
    private void Start()
    {
        if (monsterData == null)
            return;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        int playerCount = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 1;

        maxHp = monsterData.MaxHp * Mathf.Max(1, playerCount);
        currentHp = maxHp;

        OnHpChanged?.Invoke();

        Debug.Log($"보스 체력 설정: {playerCount}명, {currentHp}/{maxHp}", this);
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            TakeDamage(new tAttackInfo(transform.position, 10f));
        Debug.Log($"보스 {CurrentHp}/{MaxHp}");
    }
#endif
    public void TakeDamage(tAttackInfo attackInfo)
    {
        if (monsterData == null || IsDead || IsInvincible)
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
        if (monsterData == null || IsDead || IsInvincible)
            return;
        float damage = Mathf.Max(0f, requestedDamage);
        float previousHp = currentHp;

        currentHp = Mathf.Max(0f, currentHp - damage);

        if (currentHp != previousHp)
            OnHpChanged?.Invoke();
        Debug.LogWarning($"보스 피격: {damage}, 남은 HP: {currentHp}", this);
        if (IsDead)
            Die();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHp);
            stream.SendNext(maxHp);
        }
        else
        {
            float receivedHp = (float)stream.ReceiveNext();
            float receivedMaxHp = (float)stream.ReceiveNext();

            bool changed = currentHp != receivedHp || maxHp != receivedMaxHp;

            currentHp = receivedHp;
            maxHp = receivedMaxHp;

            if (changed)
                OnHpChanged?.Invoke();

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
        battleEndController.PanelOpenEvent();

        if (clearPortal != null)
            clearPortal.SetActive(true);

        GameManager.m_Instance.RefreshPlayerAll();
    }
    public void SetInvincible(bool value)
    {
        IsInvincible = value;
    }
    public void MultiplyAttack(float multiplier)
    {
        attack *= Mathf.Max(1f, multiplier);

        Debug.Log($"보스 공격력 증가: {attack}", this);
    }
}