using UnityEngine;
using UnityEngine.Rendering;

/*///////////////////////////////////////////
                PlayerController
기능 : 입력 방향과 시점에 맞춘 Rigidbody 이동 및 이동 애니메이션
       Update에서 입력·회전을 처리하고 FixedUpdate에서 속도 적용
 *///////////////////////////////////////////

public sealed class PlayerController : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int StrafeHash = Animator.StringToHash("Strafe");
    private static readonly int GroundHash = Animator.StringToHash("Ground");
    private static readonly int JumpVertical = Animator.StringToHash("JumpVertical");
    private static readonly int IdleJumpTriggerHash = Animator.StringToHash("Jumping");
    private static readonly int RunningJumpTriggerHash = Animator.StringToHash("RunningJump");
    private static readonly int JumpFinishedTriggerHash = Animator.StringToHash("JumpFinished");

    [Header("Movement")]
    [SerializeField, Min(0f)] private float m_fWalkSpeed = 2f;
    [SerializeField, Min(0f)] private float m_fRunSpeed = 4f;
    [SerializeField, Min(0f)] private float m_fSprintSpeed = 6f;
    [Tooltip("이동 중 방향 전환 속도(도/초)")]
    [SerializeField, Min(1f)] private float m_fDirectionTurnSpeed = 360f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float m_fJumpSpeed = 5f;
    [SerializeField] private LayerMask m_iGroundLayers = ~0;

    [Header("Rotate")]
    [SerializeField, Min(0f)] private float m_fRotateSensitivity = 3f;
    [SerializeField, Min(1f)] private float m_fRotateSpeed = 90f;

    private InputManager m_refInputManager;
    private Animator m_refAnimator;
    private Rigidbody m_refRigidbody;

    private bool m_bJump;
    private bool m_bGrounded;

    private float m_fJumpAnimationSpeed = 1f;
    private Collider m_refGroundCollider;

    private Vector2 m_vMoveInput;
    private Vector2 m_vMoveDir;

    private float m_fMoveSpeed;
    private float m_fRotateY;

    public bool IsGrounded => m_bGrounded;
    public bool IsJumping => m_bJump;
    public Quaternion ViewRotation => Quaternion.Euler(0f, m_fRotateY, 0f);

    private void Awake()
    {
        m_refAnimator = GetComponent<Animator>();
        m_refRigidbody = GetComponent<Rigidbody>();
        m_refInputManager = InputManager.m_Instance;
        m_fRotateY = m_refRigidbody.rotation.eulerAngles.y;
    }

    private void OnEnable()
    {
        BindInput();
    }

    private void BindInput()
    {
        m_refInputManager.OnJumpButtonPressed += StartJumpAnimation;
        m_refInputManager.OnInputCanceled += ResetMovement;
    }

    private void Update()
    {

        ApplyInput();
    }

    private void FixedUpdate()
    {
        Vector3 vVelocity = ViewRotation * new Vector3(m_vMoveDir.x, 0f, m_vMoveDir.y) * (m_fMoveSpeed * m_vMoveInput.magnitude);
        vVelocity.y = m_refRigidbody.linearVelocity.y;

        m_refRigidbody.linearVelocity = vVelocity;

        m_refAnimator.SetBool(GroundHash, m_bGrounded);
        m_refAnimator.SetFloat(JumpVertical, vVelocity.y);
    }


    private void OnDisable()
    {
        m_refInputManager.OnJumpButtonPressed -= StartJumpAnimation;
        m_refInputManager.OnInputCanceled -= ResetMovement;
        ResetMovement();
        ResumeJumpAnimation();

        m_refAnimator.ResetTrigger(IdleJumpTriggerHash);
        m_refAnimator.ResetTrigger(RunningJumpTriggerHash);
        m_refAnimator.ResetTrigger(JumpFinishedTriggerHash);
        m_refGroundCollider = null;
        m_bGrounded = false;
    }


    private void StartJumpAnimation()
    {
        if (!m_bGrounded || m_bJump )
            return;

        m_refAnimator.ResetTrigger(JumpFinishedTriggerHash);
        m_refAnimator.SetBool(GroundHash, false);
        if (m_vMoveInput.sqrMagnitude > 0.001f)
            m_refAnimator.SetTrigger(RunningJumpTriggerHash);
        else
            m_refAnimator.SetTrigger(IdleJumpTriggerHash);
    }

    // 발이 떨어지는 프레임에서 Animation Event
    public void Jump()
    {
        if (!isActiveAndEnabled || m_bJump || !m_bGrounded)
            return;

        m_bJump = true;
        m_bGrounded = false;
        m_refGroundCollider = null;

        Vector3 vVelocity = m_refRigidbody.linearVelocity;
        vVelocity.y = m_fJumpSpeed;
        m_refRigidbody.linearVelocity = vVelocity;
        m_refAnimator.SetBool(GroundHash, false);
    }

    // 공중에서 유지할 프레임에 Animation Event
    public void StayJump()
    {
        if (!m_bJump || m_bGrounded || m_refAnimator.speed <= 0f)
            return;

        m_fJumpAnimationSpeed = m_refAnimator.speed;
        m_refAnimator.speed = 0f;
    }

    // 착지 동작의 마지막 프레임에 Animation Event
    public void FinishJump()
    {
        m_refAnimator.SetTrigger(JumpFinishedTriggerHash);
    }

    private void ResumeJumpAnimation()
    {
        if (m_refAnimator.speed > 0f)
            return;

        m_refAnimator.speed = m_fJumpAnimationSpeed;
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
        if (_refCollision.collider != m_refGroundCollider)
            return;

        m_refGroundCollider = null;
        m_bGrounded = false;
        m_refAnimator.SetBool(GroundHash, false);
    }

    private void UpdateGroundContact(Collision _refCollision)
    {
        bool bGroundContact = false;
        if ((m_iGroundLayers.value & (1 << _refCollision.gameObject.layer)) != 0
            && m_refRigidbody.linearVelocity.y <= 0.1f)
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
        {
            m_refGroundCollider = _refCollision.collider;
            m_bGrounded = true;
            m_bJump = false;

            ResumeJumpAnimation();
        }
        else if (_refCollision.collider == m_refGroundCollider)
        {
            m_refGroundCollider = null;
            m_bGrounded = false;
        }

        m_refAnimator.SetBool(GroundHash, m_bGrounded);
    }

    private void ApplyInput()
    {
        bool bRun = m_refInputManager.IsRunHeld;
        bool bStrafe = m_refInputManager.IsStrafeHeld;

        tInputInfo tInput = m_refInputManager.InputInfo;

        Vector2 vInput = tInput.MoveDir;
        float fMouseX = tInput.Delta.x;

        float fY = fMouseX * m_fRotateSensitivity;
        m_fRotateY = Mathf.Repeat(m_fRotateY + fY, 360f);
        m_refRigidbody.rotation = ViewRotation;

        m_vMoveInput = Vector2.ClampMagnitude(vInput, 1f);

        bool bMoving = m_vMoveInput.sqrMagnitude > 0.001f;
        if (bMoving == false)
            m_vMoveDir = Vector2.zero;
        else
        {
            //이전 각도와 현재 각도의 차이값
            float fCurrentAngle = Mathf.Atan2(m_vMoveDir.x, m_vMoveDir.y) * Mathf.Rad2Deg;
            float fTargetAngle = Mathf.Atan2(m_vMoveInput.x, m_vMoveInput.y) * Mathf.Rad2Deg;
            float fDirectionAngle = Mathf.MoveTowardsAngle(fCurrentAngle, fTargetAngle,
                m_fDirectionTurnSpeed * Time.deltaTime) * Mathf.Deg2Rad;

            m_vMoveDir = new Vector2(Mathf.Sin(fDirectionAngle), Mathf.Cos(fDirectionAngle));
        }

        float fSpeed;
        if (bRun && bMoving)
        {
            if (!bStrafe && m_vMoveInput.y >= 0f)
            {
                fSpeed = 2f;
                m_fMoveSpeed = m_fSprintSpeed;
            }
            else
            {
                fSpeed = 1f;
                m_fMoveSpeed = m_fRunSpeed;
            }
        }
        else
        {
            fSpeed = 0f;
            m_fMoveSpeed = m_fWalkSpeed;
        }

        Vector2 vAnimationValue = m_vMoveDir * m_vMoveInput.magnitude;
        m_refAnimator.SetFloat(MoveXHash, vAnimationValue.x);
        m_refAnimator.SetFloat(MoveYHash, vAnimationValue.y);
        m_refAnimator.SetFloat(SpeedHash, fSpeed);
        m_refAnimator.SetFloat(StrafeHash, bStrafe ? 1f : 0f);
    }

    private void ResetMovement()
    {
        m_vMoveInput = Vector2.zero;
        m_vMoveDir = Vector2.zero;
        m_fMoveSpeed = 0f;

        Vector3 vVelocity = m_refRigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refRigidbody.linearVelocity = vVelocity;

        m_refAnimator.SetFloat(MoveXHash, 0f);
        m_refAnimator.SetFloat(MoveYHash, 0f);
        m_refAnimator.SetFloat(SpeedHash, 0f);
        m_refAnimator.SetFloat(StrafeHash, 0f);

        if (m_bGrounded)
        {
            m_refAnimator.ResetTrigger(IdleJumpTriggerHash);
            m_refAnimator.ResetTrigger(RunningJumpTriggerHash);
            m_refAnimator.SetBool(GroundHash, m_bGrounded);
            m_refAnimator.SetTrigger(JumpFinishedTriggerHash);
        }
    }
}
