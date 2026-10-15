using UnityEngine;
using UnityEngine.InputSystem;

/*///////////////////////////////////////////
                tInputInfo / InputManager
기능 : Inspector의 액션 참조와 활성화 수명을 관리하고 연속 입력을 읽음
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

    [Header("Input Actions")]
    [SerializeField] private InputActionReference m_refMoveActionReference;
    [SerializeField] private InputActionReference m_refJumpActionReference;
    [SerializeField] private InputActionReference m_refRollActionReference;
    [SerializeField] private InputActionReference m_refRunActionReference;
    [SerializeField] private InputActionReference m_refDeltaActionReference;
    [SerializeField] private InputActionReference m_refSkill0ActionReference;
    [SerializeField] private InputActionReference m_refSkill1ActionReference;
    [SerializeField] private InputActionReference m_refSkill2ActionReference;
    [SerializeField] private InputActionReference m_refSkill3ActionReference;
    [SerializeField] private InputActionReference m_refSkill4ActionReference;

    private InputAction m_refMoveAction;
    private InputAction m_refDeltaAction;
    private InputAction m_refJumpAction;
    private InputAction m_refRollAction;
    private InputAction m_refRunAction;
    private InputAction m_refSkill0Action;
    private InputAction m_refSkill1Action;
    private InputAction m_refSkill2Action;
    private InputAction m_refSkill3Action;
    private InputAction m_refSkill4Action;
    private tInputInfo m_tInputInfo;
    private bool m_bFocused = true;

    public tInputInfo InputInfo => m_tInputInfo;
    public InputAction MoveAction => m_refMoveAction;
    public InputAction JumpAction => m_refJumpAction;
    public InputAction RollAction => m_refRollAction;
    public InputAction Skill0Action => m_refSkill0Action;
    public InputAction Skill1Action => m_refSkill1Action;
    public InputAction Skill2Action => m_refSkill2Action;
    public InputAction Skill3Action => m_refSkill3Action;
    public InputAction Skill4Action => m_refSkill4Action;

    public bool HasGameplayInput => m_bFocused && Time.timeScale > 0f;
    public bool IsRunHeld => HasGameplayInput && m_refRunAction.IsPressed();

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
        Init();
    }

    private void Init()
    {
        m_refMoveAction = m_refMoveActionReference.action;
        m_refDeltaAction = m_refDeltaActionReference.action;
        m_refJumpAction = m_refJumpActionReference.action;
        m_refRollAction = m_refRollActionReference.action;
        m_refRunAction = m_refRunActionReference.action;
        m_refSkill0Action = m_refSkill0ActionReference.action;
        m_refSkill1Action = m_refSkill1ActionReference.action;
        m_refSkill2Action = m_refSkill2ActionReference.action;
        m_refSkill3Action = m_refSkill3ActionReference.action;
        m_refSkill4Action = m_refSkill4ActionReference.action;

    }

    private void OnEnable()
    {
        if (m_Instance != this)
            return;

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

        m_bFocused = false;
        DisableActions();
        CancelInput();
    }

    private void OnDestroy()
    {
        if (m_Instance != this)
            return;

        m_Instance = null;
    }

    //private void OnApplicationFocus(bool _bFocused)
    //{
    //    m_bFocused = _bFocused;
    //    if (_bFocused == false)
    //        CancelInput();
    //}

    private void CancelInput()
    {
        m_tInputInfo = default;
    }

    private void EnableActions()
    {
        m_refSkill0Action.Enable();
        m_refSkill1Action.Enable();
        m_refSkill2Action.Enable();
        m_refSkill3Action.Enable();
        m_refSkill4Action.Enable();
        m_refMoveAction.Enable();
        m_refDeltaAction.Enable();
        m_refJumpAction.Enable();
        m_refRollAction.Enable();
        m_refRunAction.Enable();
    }

    private void DisableActions()
    {
        m_refSkill0Action.Disable();
        m_refSkill1Action.Disable();
        m_refSkill2Action.Disable();
        m_refSkill3Action.Disable();
        m_refSkill4Action.Disable();
        m_refMoveAction.Disable();
        m_refDeltaAction.Disable();
        m_refJumpAction.Disable();
        m_refRollAction.Disable();
        m_refRunAction.Disable();
    }
}
