using UnityEngine;

/*///////////////////////////////////////////
                PlayerController
기능 : 입력 방향과 시점에 맞춘 Rigidbody 이동 및 이동 애니메이션
       Update에서 입력·회전을 처리하고 FixedUpdate에서 속도 적용
 *///////////////////////////////////////////
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerController : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int StrafeHash = Animator.StringToHash("Strafe");
    private static readonly int GroundHash = Animator.StringToHash("Ground");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

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

    private Vector2 m_vMoveInput;
    private Vector2 m_vMoveDir;

    private float m_fMoveSpeed;
    private float m_fRotateY;

    public bool IsGrounded => m_bGrounded;
    public Quaternion ViewRotation => Quaternion.Euler(0f, m_fRotateY, 0f);

    private void Awake()
    {
        m_refAnimator = GetComponentInChildren<Animator>();
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
        m_refInputManager.OnJumpButtonPressed += OnJumpPressed;
        m_refInputManager.OnInputCanceled += ResetMovement;
    }

    private void Update()
    {
        if (!m_refInputManager.HasGameplayInput)
        {
            ResetMovement();
            return;
        }
        ApplyInput();
    }

    private void FixedUpdate()
    {
        m_bGrounded = m_refRigidbody.linearVelocity.y <= 0.1f;

        Vector3 vVelocity = ViewRotation * new Vector3(m_vMoveDir.x, 0f, m_vMoveDir.y) * (m_fMoveSpeed * m_vMoveInput.magnitude);
        vVelocity.y = m_refRigidbody.linearVelocity.y;

        //if (m_bJump && m_bGrounded)
        //{
        //    vVelocity.y = m_fJumpSpeed;
        //    m_bGrounded = false;
        //}
        //m_bJump = false;

        m_refRigidbody.linearVelocity = vVelocity;

        m_refAnimator.SetBool(GroundHash, m_bGrounded);
        m_refAnimator.SetFloat(VerticalSpeedHash, vVelocity.y);
    }


    private void OnDisable()
    {
        m_refInputManager.OnJumpButtonPressed -= OnJumpPressed;
        m_refInputManager.OnInputCanceled -= ResetMovement;
        ResetMovement();
    }

    private void OnJumpPressed()
    {
        if (isActiveAndEnabled)
            m_bJump = true;
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
        m_bJump = false;
        Vector3 vVelocity = m_refRigidbody.linearVelocity;
        vVelocity.x = 0f;
        vVelocity.z = 0f;
        m_refRigidbody.linearVelocity = vVelocity;

        m_refAnimator.SetFloat(MoveXHash, 0f);
        m_refAnimator.SetFloat(MoveYHash, 0f);
        m_refAnimator.SetFloat(SpeedHash, 0f);
        m_refAnimator.SetFloat(StrafeHash, 0f);
    }
}
