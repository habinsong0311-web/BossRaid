using UnityEngine;
using static FSM;
using static InputManager;
using UnityEngine.Serialization;
using Photon.Pun;

[RequireComponent(typeof(FSM), typeof(Animator), typeof(Rigidbody))]
public sealed class Player : MonoBehaviourPun , ITakeDamgeable , IPhotonViewCallback
{
    //스킬같은 우선권이 밀릴 때도 마우스 회전을 받기 위해서 Player쪽에서 관리
    [SerializeField, Min(0f)] private float m_fRotateSensitivity = 3f;
    private float m_fRotateY;
    public Quaternion ViewRotation => Quaternion.Euler(0f, m_fRotateY, 0f);

    private FSM m_refFSM;
    private PhotonView m_refPhotonView;
    [SerializeField] private GameObject m_refLocalCamera;

    private ObjectInfo m_refObjectInfo = new();
    public ObjectInfo ObjectInfo => m_refObjectInfo;

    [FormerlySerializedAs("m_refTest")]
    [SerializeField] private SkillSpawner m_refSkillSpawner;
    public SkillSpawner SkillSpawner => m_refSkillSpawner;

    public bool IsMine => m_refPhotonView.IsMine;
    public bool IsAlive => isActiveAndEnabled && m_refObjectInfo.CurrentHP > 0f &&
        m_refFSM?.CurrentState?.Type != eStateType.Dead;
    private void Awake()
    {
        m_refPhotonView = GetComponent<PhotonView>();
        //임시 코드 -> SO로 수정
        m_refObjectInfo.CurrentHP = 100.0f;
        m_refObjectInfo.CurrentSpeed = 2.0f;
        m_refObjectInfo.AtkPoint = 4;

        if (m_refSkillSpawner == null)
            m_refSkillSpawner = GetComponentInChildren<SkillSpawner>();
        Init();
    }

    private void Init()
    {
        m_refFSM = GetComponent<FSM>();
        m_fRotateY = GetComponent<Rigidbody>().rotation.eulerAngles.y;
        if (!m_refFSM.Init(this))
            enabled = false;
    }

    private void OnEnable()
    {
        if (m_refPhotonView != null && PhotonNetwork.InRoom && !m_refPhotonView.IsMine)
            return;

        if (m_refObjectInfo.CurrentHP > 0f)
            m_refFSM.StartState();
    }

    private void Start()
    {
        if (m_refPhotonView == null || !PhotonNetwork.InRoom || m_refPhotonView.IsMine)
        {
            if (m_refLocalCamera != null)
                m_refLocalCamera.SetActive(true);
            return;
        }

        //enabled = false;
        //m_refFSM.enabled = false;
        //// Disabling Update alone leaves the remote character's input callbacks subscribed.
        //m_refFSM.Dispose();
        //Rigidbody refBody = GetComponent<Rigidbody>();
        //refBody.linearVelocity = Vector3.zero;
        //refBody.angularVelocity = Vector3.zero;
        //refBody.isKinematic = true;
        //refBody.useGravity = false;
    }

    private void Update()
    {
        if (m_refPhotonView != null && PhotonNetwork.InRoom && !m_refPhotonView.IsMine)
            return;

        if (IsAlive && m_Instance.HasGameplayInput)
        {
            float fRotate = m_Instance.InputInfo.Delta.x * m_fRotateSensitivity;
            m_fRotateY = Mathf.Repeat(m_fRotateY + fRotate, 360f);
        }

        if (m_refFSM.isActiveAndEnabled)
            m_refFSM.UpdateState();
    }

    private void FixedUpdate()
    {
        if (m_refPhotonView != null && PhotonNetwork.InRoom && !m_refPhotonView.IsMine)
            return;

        if (m_refFSM.isActiveAndEnabled)
            m_refFSM.FixedUpdateState();
    }

    private void OnDisable()
    {
        m_refFSM.Stop();
    }

    private void OnDestroy()
    {
        m_refFSM.Dispose();
    }

    public void ReceiveAnimEvent(AnimationEvent _refEvent)
    {
        if (isActiveAndEnabled && m_refFSM != null && m_refFSM.isActiveAndEnabled)
            m_refFSM.ExecuteEvent(_refEvent);
    }

    public void TakeDamage(tAttackInfo _tInfo)
    {
        if (!IsAlive || _tInfo.Damage <= 0f || float.IsNaN(_tInfo.Damage) || float.IsInfinity(_tInfo.Damage))
            return;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            if (IsMine)
                photonView.RPC(nameof(RequestDamage), RpcTarget.MasterClient, _tInfo.Pos, _tInfo.Damage);
            return;
        }

        float remainingHp = Mathf.Max(0f, m_refObjectInfo.CurrentHP - _tInfo.Damage);
        ApplyDamage(remainingHp, _tInfo.Pos);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(ReceiveDamage), RpcTarget.Others, remainingHp, _tInfo.Pos);
    }

    [PunRPC]
    private void RequestDamage(Vector3 position, float damage, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender != photonView.Owner)
            return;

        TakeDamage(new tAttackInfo(position, damage));
    }

    [PunRPC]
    private void ReceiveDamage(float remainingHp, Vector3 position, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        ApplyDamage(remainingHp, position);
    }

    private void ApplyDamage(float remainingHp, Vector3 position)
    {
        float damage = m_refObjectInfo.CurrentHP - remainingHp;
        m_refObjectInfo.CurrentHP = remainingHp;

        if (m_refObjectInfo.CurrentHP <= 0f)
        {
            m_refFSM.ChangeState(eStateType.Dead);
        }
        else if (!PhotonNetwork.InRoom || IsMine)
        {
            // The owner runs knockback; its Animator and Transform views synchronize the reaction.
            m_refFSM.TakeDamage(new tAttackInfo(position, damage));
        }
    }
}
