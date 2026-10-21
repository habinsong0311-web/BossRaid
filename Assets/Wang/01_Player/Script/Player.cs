using UnityEngine;
using static FSM;
using static InputManager;
using UnityEngine.Serialization;
using Photon.Pun;
using ExitGames.Client.Photon;

[RequireComponent(typeof(FSM), typeof(Animator), typeof(Rigidbody))]
public sealed class Player : MonoBehaviourPunCallbacks , ITakeDamgeable , IPhotonViewCallback
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

    public event System.Action OnDeadEvent;
    public event System.Action<float, float> OnTakeDamage;

    public bool IsMine => m_refPhotonView.IsMine;
    public bool IsAlive => isActiveAndEnabled && m_refObjectInfo.CurrentHP > 0f &&
        m_refFSM?.CurrentState?.Type != eStateType.Dead;
    private void Awake()
    {
        m_refPhotonView = GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && !m_refPhotonView.IsMine)
        {
            Rigidbody refBody = GetComponent<Rigidbody>();
            refBody.useGravity = false;
            refBody.interpolation = RigidbodyInterpolation.None;
            refBody.isKinematic = true;
        }

        DontDestroyOnLoad(gameObject);
        //임시 코드 -> SO로 수정
        m_refObjectInfo.CurrentHP = m_refObjectInfo.MaxHP;
        m_refObjectInfo.CurrentSpeed = 2.0f;
        m_refObjectInfo.AtkPoint = 4;

        if (m_refSkillSpawner == null)
            m_refSkillSpawner = GetComponentInChildren<SkillSpawner>();
        Init();
        GameManager.m_Instance.OnEnterPlayer(this);
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
        if (GameManager.m_Instance != null)
            GameManager.m_Instance.OnExitPlayer(this);
        m_refFSM.Dispose();
    }

    public void RefreshPlayer(Vector3 _vSpawnPos)
    {
        m_refObjectInfo.CurrentHP = m_refObjectInfo.MaxHP;
        OnTakeDamage?.Invoke(m_refObjectInfo.CurrentHP, m_refObjectInfo.MaxHP);
        m_refSkillSpawner.SkillUseUnLock();
        m_refFSM.RefreshPlayer(_vSpawnPos);

        if (IsMine && m_refLocalCamera != null)
        {
            m_refLocalCamera.SetActive(true);
            m_refLocalCamera.GetComponent<CameraController>().RestorePlayerTarget(this);
        }
    }

    public void RefreshPlayer() => RefreshPlayer(transform.position);


    public void ReceiveAnimEvent(AnimationEvent _refEvent)
    {
        if (isActiveAndEnabled && m_refFSM != null && m_refFSM.isActiveAndEnabled)
            m_refFSM.ExecuteEvent(_refEvent);
    }

    public void TakeDamage(tAttackInfo _tInfo)
    {
        if (!IsAlive || _tInfo.Damage <= 0f || m_refFSM.CurrentState?.Type == eStateType.Roll)
            return;

        //마클이 아니면 마클한테 허락을 구하기
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            if (IsMine)
                photonView.RPC(nameof(RequestDamage), RpcTarget.MasterClient, _tInfo.Pos, _tInfo.Damage);
            return;
        }

        float remainingHp = Mathf.Max(0f, m_refObjectInfo.CurrentHP - _tInfo.Damage);
        // Send _fDamage before a death event can start loading the next scene.
        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(ReceiveDamage), RpcTarget.Others, remainingHp, _tInfo.Pos);

        ApplyDamage(remainingHp, _tInfo.Pos);
    }

    [PunRPC]
    private void RequestDamage(Vector3 vPos, float _fDamage, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || info.Sender != photonView.Owner)
            return;

        TakeDamage(new tAttackInfo(vPos, _fDamage));
    }

    [PunRPC]
    private void ReceiveDamage(float remainingHp, Vector3 _vPos, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
            return;

        ApplyDamage(remainingHp, _vPos);
    }

    private void ApplyDamage(float _fRemainingHp, Vector3 _vPos)
    {
        
        bool bWasAlive = m_refObjectInfo.CurrentHP > 0f;
        float _fDamage = m_refObjectInfo.CurrentHP - _fRemainingHp;
        m_refObjectInfo.CurrentHP = _fRemainingHp;
        OnTakeDamage?.Invoke(m_refObjectInfo.CurrentHP, m_refObjectInfo.MaxHP);

        m_refSkillSpawner.SkillUseUnLock();
        if (m_refObjectInfo.CurrentHP <= 0f)
        {
            m_refFSM.ChangeState(eStateType.Dead);
            if (bWasAlive)
                OnDeadEvent?.Invoke();
        }
        else if (!PhotonNetwork.InRoom || IsMine)
            m_refFSM.TakeDamage(new tAttackInfo(_vPos, _fDamage));

    }
}
