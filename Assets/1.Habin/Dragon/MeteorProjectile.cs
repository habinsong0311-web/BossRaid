using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class MeteorProjectile : MonoBehaviour
{
    [Header("낙하")]
    [Min(0f)]
    [SerializeField] private float fallSpeed = 30f;

    [Header("삭제")]
    [SerializeField] private LayerMask groundLayers;

    [Min(0.01f)]
    [SerializeField] private float maxLifetime = 5f;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearVelocity = Vector3.down * fallSpeed;

        // 바닥을 놓치더라도 계속 남아 있지 않게 합니다.
        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        int collidedLayerMask = 1 << collision.gameObject.layer;

        if ((groundLayers.value & collidedLayerMask) != 0)
            Destroy(gameObject);
    }
}