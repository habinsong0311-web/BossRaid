using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static InputManager;

public sealed class FSM : MonoBehaviour
{
    public enum eStateType
    {
        Idle,
        Walk,
        Run,
        Jump,
        Roll,
        Hit,
        Dead,
        Skill,
    }

    [SerializeField] private List<SOState> m_listState = new List<SOState>();
    [SerializeField] private eStateType m_eInitialState = eStateType.Idle;
    [SerializeField] private LayerMask m_iGroundLayers = ~0;

    [SerializeField] private GameObject m_refPlayerView;

    private readonly Dictionary<eStateType, AnimatorEvent> m_hashAnimEvent = new Dictionary<eStateType, AnimatorEvent>();
    private readonly Dictionary<eStateType, PlayerState> m_hashState = new Dictionary<eStateType, PlayerState>();
    private PlayerState m_refCurState;
    private Player m_refPlayer;
    public Player Player => m_refPlayer;

    private Collider m_refGroundCollider;

    public PlayerState CurrentState => m_refCurState;
    public PlayerState PreviousState { get; private set; }
    public Animator Animator { get; private set; }
    public Rigidbody Rigidbody { get; private set; }
    public Quaternion ViewRotation => m_refPlayer.ViewRotation;
    public SkillSpawner SkillSpawner => m_refPlayer.SkillSpawner;
    public bool HasGameplayInput => isActiveAndEnabled && m_refPlayer.isActiveAndEnabled &&
        m_refPlayer.ObjectInfo.CurrentHP > 0f && m_Instance != null && m_Instance.HasGameplayInput;
    public bool IsGrounded => m_refGroundCollider != null && Rigidbody.linearVelocity.y <= 0.1f;

    public bool IsMine => m_refPlayer.IsMine;
    public bool Init(Player _refController)
    {
        m_refPlayer = _refController;
        Animator = GetComponent<Animator>();
        Rigidbody = GetComponent<Rigidbody>();

        foreach (SOState refData in m_listState)
        {
            if (m_hashState.ContainsKey(refData.State))
            {
                Debug.LogError("FSM에 같은 상태가 중복 등록되었습니다.", this);
                return false;
            }

            PlayerState refState = refData.CreateState(this, refData.Clip);
            m_hashState.Add(refState.Type, refState);
            m_hashAnimEvent.Add(refState.Type, new AnimatorEvent());
        }

        if (!m_hashState.ContainsKey(m_eInitialState))
        {
            Debug.LogError("FSM 초기 상태의 SOState를 등록해야 합니다.", this);
            return false;
        }

        foreach (var tKeyValue in m_hashState)
            tKeyValue.Value.Init();

        m_Instance.MoveAction.canceled += CancelInput;
        return true;
    }

    public void Dispose()
    {
        Stop();
        if (m_Instance != null)
            m_Instance.MoveAction.canceled -= CancelInput;
        foreach (var tKeyValue in m_hashState)
            tKeyValue.Value.Dispose();

        m_hashState.Clear();
        m_hashAnimEvent.Clear();
        PreviousState = null;
    }

    private void OnDisable()
    {
        Stop();
        ClearGroundContact();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void CancelInput(InputAction.CallbackContext _tContext)
    {
        if (m_Instance.HasGameplayInput)
            return;

        if (m_refCurState?.Type != eStateType.Hit && m_refCurState?.Type != eStateType.Dead)
            Stop();
    }

    public bool StartState()
    {
        return ChangeState(m_eInitialState);
    }

    public void TakeDamage(tAttackInfo _tInfo)
    {
        if (m_refCurState == null)
            return;

        if (m_refCurState.Type == eStateType.Hit || m_refCurState.Type == eStateType.Dead)
            return;

        PlayerHitState refHitState = FindState<PlayerHitState>(eStateType.Hit);
        refHitState.SetHitInfo(_tInfo, m_refCurState.Type);
        ChangeState(eStateType.Hit);
    }

    public void UpdateState()
    {
        if (m_refCurState?.Type == eStateType.Dead)
            return;

        if (m_Instance.HasGameplayInput == false &&
            m_refCurState?.Type != eStateType.Hit)
        {
            Stop();
            return;
        }

        m_refCurState?.UpdateState();

        if(Input.GetKeyDown(KeyCode.P))
        {
            Vector3 vFoward = transform.position + (transform.forward * 2.0f);
            m_refPlayer.TakeDamage(new tAttackInfo(vFoward, 10));
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            m_refPlayer.TakeDamage(new tAttackInfo(transform.position, m_refPlayer.ObjectInfo.CurrentHP));
        }

    }

    public void FixedUpdateState()
    {
        m_refCurState?.FixedUpdateState();
    }

    // Animator와 같은 오브젝트에서 모든 클립 이벤트를 받습니다.
    public void ExecuteEvent(AnimationEvent _refEvent)
    {
        if (m_refCurState == null)
            return;

        m_hashAnimEvent[m_refCurState.Type].ExecuteFrame(_refEvent);
    }

    public bool ChangeState(eStateType _eState)
    {
        PlayerState refFindState = FindState<PlayerState>(_eState);
        if (refFindState == null || m_refCurState == refFindState)
            return false;

        if (!refFindState.CanEnter())
            return false;

        PreviousState = m_refCurState;
        PreviousState?.Exit();
        m_refCurState = refFindState;
        m_refCurState.Enter();
        return true;
    }

    public void Stop()
    {
        if (m_refCurState == null)
            return;

        PreviousState = m_refCurState;
        m_refCurState.Exit();
        m_refCurState = null;

        Vector3 vVelocity = Rigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        Rigidbody.linearVelocity = vVelocity;
    }

    public T FindState<T>(eStateType _eType) where T : PlayerState
    {
        if (m_hashState.TryGetValue(_eType, out PlayerState refState) == false)
            return null;

        return refState as T;
    }

    public AnimatorEvent FindAnimEvent(eStateType _eType)
    {
        return m_hashAnimEvent[_eType];
    }

    public void ClearGroundContact()
    {
        m_refGroundCollider = null;
    }

    private void OnCollisionEnter(Collision _refCollision)
    {
        UpdateGroundContact(_refCollision);
    }
    private void OnCollisionStay(Collision _refCollision)
    {
        UpdateGroundContact(_refCollision);
    }

    private void OnCollisionExit(Collision _refCollision)
    {
        if (_refCollision.collider == m_refGroundCollider)
            ClearGroundContact();
    }

    private void UpdateGroundContact(Collision _refCollision)
    {
        bool bGroundContact = false;
        if ((m_iGroundLayers.value & (1 << _refCollision.gameObject.layer)) != 0
            && Rigidbody.linearVelocity.y <= 0.1f)
        {
            for (int i = 0; i < _refCollision.contactCount; i++)
            {
                if (_refCollision.GetContact(i).normal.y >= 0.5f)
                {
                    bGroundContact = true;
                    break;
                }
            }
        }

        if (bGroundContact)
            m_refGroundCollider = _refCollision.collider;
        else if (_refCollision.collider == m_refGroundCollider)
            ClearGroundContact();
    }

    public void Dead()
    {
        m_refPlayerView.gameObject.SetActive(false);
        enabled = false;
    }


    [PunRPC]
    private void UseSkillOther(int id, int acNr)
    {
        m_refPlayer.SkillSpawner.UseSkillOther(id, acNr);
    }

    [PunRPC]
    private void UseSkillAOEOther(int id, Vector3 pos, int acNr)
    {
        m_refPlayer.SkillSpawner.UseSkillAOEOther(id, pos, acNr);
    }

    [PunRPC]
    private void ShootProjectileOther(Vector3 rayPosition)
    {
        m_refPlayer.SkillSpawner.ShootProjectileOther(rayPosition);
    }
}
