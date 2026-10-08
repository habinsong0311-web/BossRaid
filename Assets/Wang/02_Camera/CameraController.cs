using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
   
    [Header("Optional Follow Camera")]
    [SerializeField] private Transform m_refTarget = null;
    [SerializeField] private Vector3 m_vCameraOffset = new Vector3(0f, 2.3f, -4f);
    [FormerlySerializedAs("cameraLookHeight")]
    [SerializeField] private float m_fCameraLookHeight = 1.3f;



    private void LateUpdate()
    {
        if (m_refTarget == null)
            return;

        transform.position = m_refTarget.position + m_refTarget.rotation * m_vCameraOffset;
        transform.LookAt(m_refTarget.position + Vector3.up * m_fCameraLookHeight);
    }
}
