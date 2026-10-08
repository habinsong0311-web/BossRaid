using UnityEngine;
using UnityEngine.Serialization;

/*///////////////////////////////////////////
                PlayerController
기능 : Rigidbody 속도로 플레이어를 이동하고
       왼쪽 Shift 달리기와 마우스 Y축 회전을 제어
 *///////////////////////////////////////////

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour
{
    //이거는 AnimCon으로 변경
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int GaitHash = Animator.StringToHash("Gait");
    private static readonly int StrafeHash = Animator.StringToHash("Strafe");
    private static readonly int TurnHash = Animator.StringToHash("Turn");

    private InputManager m_refInputManager;
    private bool m_bRunStay;
    private bool m_bStrafeStay;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float m_fWalkSpeed = 2f;
    [SerializeField, Min(0f)] private float m_fRunSpeed = 4f;
    [SerializeField, Min(0f)] private float m_fSprintSpeed = 6f;

    [Tooltip("이동 방향 전환 속도(도/초). 360이면 전진에서 대각선까지 약 0.125초 걸립니다.")]
    [SerializeField, Min(1f)] private float m_fDirectionTurnSpeed = 360f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float m_fJumpSpeed = 5f;
    [SerializeField, Min(0f)] private float m_fGroundCheckDistance = 0.08f;
    [SerializeField] private LayerMask m_iGroundLayers = ~0;


    [Header("Rotate")]
    [SerializeField, Min(0f)] private float m_fRotateSensitivity = 3f;
    [SerializeField, Min(1f)] private float m_fRotateSpeed = 90f;

    private Animator m_refAnimator;
    private Rigidbody m_refRigidbody;
    private CapsuleCollider m_refCapsuleCollider;
    private bool m_bJump;
    private Vector2 m_vMoveInput;
    private Vector2 m_vMoveDirection;
    private float m_fMoveSpeed;
    private float m_fPendingYaw;

    
    private void Awake()
    {
        m_refAnimator = GetComponent<Animator>();
        m_refRigidbody = GetComponent<Rigidbody>();
        m_refCapsuleCollider = GetComponent<CapsuleCollider>();

        ResetMovement();
    }

    private void Start()
    {
        m_refInputManager = InputManager.m_Instance;

        m_refInputManager.OnRunButtonStarted += OnRunStarted;
        m_refInputManager.OnRunButtonReleased += OnRunReleased;
        m_refInputManager.OnStrafeButtonStarted += OnStrafeStarted;
        m_refInputManager.OnStrafeButtonReleased += OnStrafeReleased;
        m_refInputManager.OnJumpButtonPressed += OnJumpPressed;
        m_refInputManager.OnInputCanceled += OnInputCanceled;
    }

    private void Update()
    {
        float fDeltaTime = Time.deltaTime;
        if (fDeltaTime <= 0f)
            return;

        tInputInfo tInput = m_refInputManager.InputInfo;
        ApplyInput(tInput.MoveDir, m_bRunStay, m_bStrafeStay, tInput.Delta.x, fDeltaTime);
    }

    private void FixedUpdate()
    {
        // 마우스 변위는 누적 후 한 번만 소비해 물리 틱 수에 따라 회전량이 달라지지 않게 합니다.
        Quaternion qRotation = m_refRigidbody.rotation * Quaternion.Euler(0f, m_fPendingYaw, 0f);
        m_refRigidbody.MoveRotation(qRotation);
        m_fPendingYaw = 0f;

        Vector3 vVelocity = qRotation * new Vector3(m_vMoveDirection.x, 0f, m_vMoveDirection.y)
            * (m_fMoveSpeed * m_vMoveInput.magnitude);
        // Y축 속도를 보존해 중력과 지면 충돌은 Rigidbody가 처리합니다.
        vVelocity.y = m_refRigidbody.linearVelocity.y;

        if (m_bJump == true && vVelocity.y <= 0.1f)
            vVelocity.y = m_fJumpSpeed;
        m_bJump = false;
        m_refRigidbody.linearVelocity = vVelocity;
    }

 

    private void OnDisable()
    {
        // Awake 전에 비활성화되는 생명주기 경계에서는 캐시가 없을 수 있습니다.
        if (m_refAnimator == null)
            return;

        ResetMovement();
    }

 
    private void OnRunStarted()
    {
        m_bRunStay = true;
    }

    private void OnRunReleased()
    {
        m_bRunStay = false;
    }

    private void OnStrafeStarted()
    {
        m_bStrafeStay = true;
    }

    private void OnStrafeReleased()
    {
        m_bStrafeStay = false;
    }

    private void OnJumpPressed()
    {
        if (isActiveAndEnabled == true)
            m_bJump = true;
    }

    private void OnInputCanceled()
    {
        ResetMovement();
    }


    private void ApplyInput(Vector2 _vInput, bool _bRun, bool _bStrafe, float _fMouseX, float _fDeltaTime)
    {
        float fYaw = _fMouseX * m_fRotateSensitivity;
        m_fPendingYaw += fYaw;

        bool bWasMoving = m_vMoveInput.sqrMagnitude > 0.001f;
        m_vMoveInput = Vector2.ClampMagnitude(_vInput, 1f);
        bool bMoving = m_vMoveInput.sqrMagnitude > 0.001f;

        // 시작·정지는 즉시 반영하고 이동 중 방향만 보간해 속도가 줄어들지 않게 합니다.
        if (bMoving == false)
            m_vMoveDirection = Vector2.zero;
        else if (bWasMoving == false)
            m_vMoveDirection = m_vMoveInput.normalized;
        else
        {
            float fCurrentAngle = Mathf.Atan2(m_vMoveDirection.x, m_vMoveDirection.y) * Mathf.Rad2Deg;
            float fTargetAngle = Mathf.Atan2(m_vMoveInput.x, m_vMoveInput.y) * Mathf.Rad2Deg;
            float fDirectionAngle = Mathf.MoveTowardsAngle(fCurrentAngle, fTargetAngle,
                m_fDirectionTurnSpeed * _fDeltaTime) * Mathf.Deg2Rad;
            m_vMoveDirection = new Vector2(Mathf.Sin(fDirectionAngle), Mathf.Cos(fDirectionAngle));
        }

        float fGait = 0f;
        if (_bRun == true && bMoving == true)
            fGait = _bStrafe == false && m_vMoveInput.y >= 0f ? 2f : 1f;

        m_fMoveSpeed = fGait == 2f ? m_fSprintSpeed : fGait == 1f ? m_fRunSpeed : m_fWalkSpeed;

        Vector2 vAnimationInput = m_vMoveDirection * m_vMoveInput.magnitude;
        m_refAnimator.SetFloat(MoveXHash, vAnimationInput.x);
        m_refAnimator.SetFloat(MoveYHash, vAnimationInput.y);

        m_refAnimator.SetFloat(GaitHash, fGait);
        m_refAnimator.SetFloat(StrafeHash, _bStrafe == true ? 1f : 0f);

        float fTurn = 0f;
        if (m_vMoveInput.sqrMagnitude < 0.001f)
            fTurn = Mathf.Clamp(fYaw / (Mathf.Max(_fDeltaTime, 0.0001f) * m_fRotateSpeed), -1f, 1f);
        m_refAnimator.SetFloat(TurnHash, fTurn);
    }

    private void ResetMovement()
    {
        m_vMoveInput = Vector2.zero;
        m_vMoveDirection = Vector2.zero;
        m_fMoveSpeed = 0f;
        m_fPendingYaw = 0f;
        m_bJump = false;

        Vector3 vVelocity = m_refRigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refRigidbody.linearVelocity = vVelocity;

        m_refAnimator.SetFloat(MoveXHash, 0f);
        m_refAnimator.SetFloat(MoveYHash, 0f);
        m_refAnimator.SetFloat(GaitHash, 0f);
        m_refAnimator.SetFloat(StrafeHash, 0f);
        m_refAnimator.SetFloat(TurnHash, 0f);
    }
}
