using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

using SF = UnityEngine.SerializeField;

public class LightningVortex : SkillBase, IProjectile
{
    [Header("직렬화 컴포넌트")]
    [SF] private float projectileSpeed = 15;
    [SF] private int lifeTime = 7;
    [SF] private Rigidbody rb;

    public void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public override void DamageTrigger(ITakeDamgeable target, Player player)
    {
        int damage =
            Mathf.RoundToInt(player.ObjectInfo.CurrentHP
            * (SkillDataLoader.Instance.SkillData[Id].Value / 100f));

        tAttackInfo info = new()
        {
            Pos = Vector3.zero,
            Damage = damage
        };

        target.TakeDamage(info);
        hitEffect.gameObject.SetActive(true);
    }

    public void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Move(Vector3 direction, float speed)
    {
        rb.linearVelocity = speed * direction;
    }

    public void Shoot(Vector3 rayPosition)
    {
        Vector3 direction = (rayPosition - transform.position).normalized;
        Move(direction, projectileSpeed);
        LifeTimeEnd().Forget();
    }

    public async UniTask LifeTimeEnd()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(lifeTime);
        int ms = (int)timeSpan.TotalMilliseconds;

        await UniTask.Delay(ms);
        if (gameObject != null && gameObject.activeSelf)
            pool.Release(this);
        Stop();
    }
}
