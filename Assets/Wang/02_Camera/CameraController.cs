using UnityEngine;
using UnityEngine.Serialization;

/*///////////////////////////////////////////
                CameraController
기능 : 시선 방향을 유지하는 백뷰 추적, 마우스 상하 회전과 벽 충돌 보정
 *///////////////////////////////////////////
public class CameraController : MonoBehaviour
{
    [Header("Follow Camera")]
    [SerializeField] private Transform m_refTarget;
    [SerializeField] private Vector3 m_vCameraOffset = new Vector3(0f, 2.3f, -4f);
    [FormerlySerializedAs("cameraLookHeight")]
    [SerializeField] private float m_fCameraLookHeight = 1.3f;
    [SerializeField, Min(0f)] private float m_fPitchSensitivity = 0.15f;
    [SerializeField] private float m_fMinPitch = -35f;
    [SerializeField] private float m_fMaxPitch = 60f;
    [SerializeField, Min(0.01f)] private float m_fCollisionRadius = 0.2f;
    [SerializeField] private LayerMask m_iCollisionLayers = ~0;

    private PlayerController m_refPlayer;
    private InputManager m_refInputManager;
    private float m_fPitch;
    private readonly RaycastHit[] m_arrCameraHits = new RaycastHit[16];

    private void Awake()
    {
        m_refPlayer = m_refTarget.GetComponent<PlayerController>();
        m_refInputManager = InputManager.m_Instance;
    }

    private void LateUpdate()
    {
        if (m_refInputManager.HasGameplayInput)
            m_fPitch = Mathf.Clamp(m_fPitch - m_refInputManager.InputInfo.Delta.y * m_fPitchSensitivity, m_fMinPitch, m_fMaxPitch);

        Quaternion qYaw = m_refPlayer.ViewRotation;
        Vector3 vPivot = m_refTarget.position + Vector3.up * m_fCameraLookHeight;
        Vector3 vOffset = qYaw * Quaternion.Euler(m_fPitch, 0f, 0f)
            * (m_vCameraOffset - Vector3.up * m_fCameraLookHeight);
        float fDistance = vOffset.magnitude;
        if (fDistance <= 0.001f)
            return;

        Vector3 vDirection = vOffset / fDistance;
        int iHitCount = Physics.SphereCastNonAlloc(vPivot, m_fCollisionRadius, vDirection, m_arrCameraHits,
            fDistance, m_iCollisionLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < iHitCount; i++)
        {
            RaycastHit tHit = m_arrCameraHits[i];
            if (!tHit.transform.IsChildOf(m_refTarget))
                fDistance = Mathf.Min(fDistance, Mathf.Max(0.05f, tHit.distance - 0.05f));
        }
        transform.position = vPivot + vDirection * fDistance;
        transform.LookAt(vPivot);
    }
}
