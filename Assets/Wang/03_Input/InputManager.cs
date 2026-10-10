using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*///////////////////////////////////////////
                tInputInfo / InputManager
기능 : Inspector의 액션을 읽고 연속 입력과 C# 버튼 이벤트 전달
       포커스 해제·비활성화 시 입력 취소
 *///////////////////////////////////////////
public struct tInputInfo
{
    public Vector2 MoveDir;
    public Vector2 Delta;
}

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
    private InputAction m_refRunAction;
    private InputAction m_refStrafeAction;
    private tInputInfo m_tInputInfo;
    private bool m_bFocused = true;

    public tInputInfo InputInfo => m_tInputInfo;
    public bool HasGameplayInput => m_bFocused && Time.timeScale > 0f;
    public bool IsRunHeld => HasGameplayInput && m_refRunAction.IsPressed();
    public bool IsStrafeHeld => HasGameplayInput && m_refStrafeAction.IsPressed();

    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            enabled = false;
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
    }

    private void OnEnable()
    {
        if (m_Instance != this)
            return;

        m_refJumpAction.performed += OnJumpButtonPerformed;
        m_refRunAction.started += OnRunButtonStartedPerformed;
        m_refRunAction.canceled += OnRunButtonCanceledPerformed;
        m_refStrafeAction.started += OnStrafeButtonStartedPerformed;
        m_refStrafeAction.canceled += OnStrafeButtonCanceledPerformed;

        EnableActions();
        m_bFocused = true;
        CancelInput();
    }

    private void Update()
    {
        if (HasGameplayInput == false)
            return;

        m_tInputInfo.MoveDir = m_refMoveAction.ReadValue<Vector2>();
        m_tInputInfo.Delta = m_refDeltaAction.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        if (m_Instance != this)
            return;

        m_refJumpAction.performed -= OnJumpButtonPerformed;
        m_refRunAction.started -= OnRunButtonStartedPerformed;
        m_refRunAction.canceled -= OnRunButtonCanceledPerformed;
        m_refStrafeAction.started -= OnStrafeButtonStartedPerformed;
        m_refStrafeAction.canceled -= OnStrafeButtonCanceledPerformed;
        DisableActions();
        CancelInput();
    }

    private void OnDestroy()
    {
        if (m_Instance == this)
            m_Instance = null;
    }

    private void OnApplicationFocus(bool _bFocused)
    {
        m_bFocused = _bFocused;
        if (_bFocused == false)
            CancelInput();
    }

    private void OnJumpButtonPerformed(InputAction.CallbackContext _tContext)
    {
        if (HasGameplayInput)
            OnJumpButtonPressed?.Invoke();
    }

    private void OnRunButtonStartedPerformed(InputAction.CallbackContext _tContext)
    {
        if (HasGameplayInput)
            OnRunButtonStarted?.Invoke();
    }

    private void OnRunButtonCanceledPerformed(InputAction.CallbackContext _tContext) => OnRunButtonReleased?.Invoke();
    private void OnStrafeButtonStartedPerformed(InputAction.CallbackContext _tContext)
    {
        if (HasGameplayInput)
            OnStrafeButtonStarted?.Invoke();
    }
    private void OnStrafeButtonCanceledPerformed(InputAction.CallbackContext _tContext) => OnStrafeButtonReleased?.Invoke();

    private void CancelInput()
    {
        m_tInputInfo = default;
        OnRunButtonReleased?.Invoke();
        OnStrafeButtonReleased?.Invoke();
        OnInputCanceled?.Invoke();
    }

    private void EnableActions()
    {
        m_refMoveAction.Enable();
        m_refDeltaAction.Enable();
        m_refJumpAction.Enable();
        m_refRunAction.Enable();
        m_refStrafeAction.Enable();
    }

    private void DisableActions()
    {
        m_refMoveAction.Disable();
        m_refDeltaAction.Disable();
        m_refJumpAction.Disable();
        m_refRunAction.Disable();
        m_refStrafeAction.Disable();
    }
}
