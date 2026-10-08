using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/*///////////////////////////////////////////
                tInputInfo
기능 : 이번 프레임의 연속 입력인 이동 방향과 마우스 변위 전달
 *///////////////////////////////////////////
public struct tInputInfo
{
    public Vector2 MoveDir;
    public Vector2 Delta;
}

/*///////////////////////////////////////////
                InputManager
기능 : Inspector에서 연결한 InputActionReference를 읽고 연속 입력은 값으로 노출
       버튼 입력은 R3 이벤트로 전달하며 커서 잠금 관리
 *///////////////////////////////////////////
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class InputManager : MonoBehaviour
{
    public static InputManager m_Instance { get; private set; }

    public event Action OnJumpButtonPressed;

    public event Action OnInputCanceled;

    public event Action OnRunButtonStarted;
    public event Action OnRunButtonReleased;
    public event Action OnStrafeButtonStarted;
    public event Action OnStrafeButtonReleased;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference m_refMoveActionReference;
    [SerializeField] private InputActionReference m_refJumpActionReference;
    [SerializeField] private InputActionReference m_refRunActionReference;
    [SerializeField] private InputActionReference m_refStrafeActionReference;
    [SerializeField] private InputActionReference m_refDeltaActionReference;

    private InputAction m_refMoveAction;
    private InputAction m_refDeltaAction;
    private InputAction m_refJumpAction;
    private InputAction m_refChargeAction;
    private InputAction m_refRunAction;
    private InputAction m_refStrafeAction;
    private tInputInfo m_tInputInfo;
    public tInputInfo InputInfo => m_tInputInfo;


    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_Instance = this;
        DontDestroyOnLoad(gameObject);


        m_refMoveAction = m_refMoveActionReference.action;
        m_refDeltaAction = m_refDeltaActionReference.action;
        m_refJumpAction = m_refJumpActionReference.action;
        m_refRunAction = m_refRunActionReference.action;
        m_refStrafeAction = m_refStrafeActionReference.action;

        m_tInputInfo = default;

    }

    private void OnEnable()
    {
        
        m_refJumpAction.performed += OnJumpButtonPerformed;
        m_refRunAction.started += OnRunButtonStartedPerformed;
        m_refRunAction.canceled += OnRunButtonCanceledPerformed;
        m_refStrafeAction.started += OnStrafeButtonStartedPerformed;
        m_refStrafeAction.canceled += OnStrafeButtonCanceledPerformed;
        EnableActions();

    }

    private void Update()
    {
       
        m_tInputInfo.MoveDir = m_refMoveAction.ReadValue<Vector2>();
        Vector2 vDelta = m_refDeltaAction.ReadValue<Vector2>();

        if (vDelta.sqrMagnitude > 0f)
        {
            vDelta = Vector2.zero;
        }

        m_tInputInfo.Delta = vDelta;
    }

    private void OnDisable()
    {
        if (m_Instance != this || m_refMoveAction == null)
            return;

        m_refJumpAction.performed -= OnJumpButtonPerformed;
        m_refRunAction.started -= OnRunButtonStartedPerformed;
        m_refRunAction.canceled -= OnRunButtonCanceledPerformed;
        m_refStrafeAction.started -= OnStrafeButtonStartedPerformed;
        m_refStrafeAction.canceled -= OnStrafeButtonCanceledPerformed;
        DisableActions();

        CancelInput();
    }


    private void OnJumpButtonPerformed(InputAction.CallbackContext _tContext)
    {
        OnJumpButtonPressed?.Invoke();
    }


    private void OnRunButtonStartedPerformed(InputAction.CallbackContext _tContext)
    {
        OnRunButtonStarted?.Invoke();
    }

    private void OnRunButtonCanceledPerformed(InputAction.CallbackContext _tContext)
    {
        OnRunButtonReleased?.Invoke();
    }

    private void OnStrafeButtonStartedPerformed(InputAction.CallbackContext _tContext)
    {
        OnStrafeButtonStarted?.Invoke();
    }

    private void OnStrafeButtonCanceledPerformed(InputAction.CallbackContext _tContext)
    {
        OnStrafeButtonReleased?.Invoke();
    }

    private void OnUnlockCursorPerformed(InputAction.CallbackContext _tContext)
    {
        CancelInput();
    }

    private void CancelInput()
    {
        m_tInputInfo = default;
    }


    private void EnableActions()
    {
        m_refMoveAction.Enable();
        m_refDeltaAction.Enable();
        m_refJumpAction.Enable();
        m_refChargeAction.Enable();
        m_refRunAction.Enable();
        m_refStrafeAction.Enable();
    }

    private void DisableActions()
    {
        m_refMoveAction.Disable();
        m_refDeltaAction.Disable();
        m_refJumpAction.Disable();
        m_refChargeAction.Disable();
        m_refRunAction.Disable();
        m_refStrafeAction.Disable();
    }
}
