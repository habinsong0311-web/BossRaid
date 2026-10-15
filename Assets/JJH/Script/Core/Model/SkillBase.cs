using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.Pool;

public abstract class SkillBase : MonoBehaviourPunCallbacks
{

    [Header("필요시 설정")]
    public Collider hitbox;

    [Header("필수 설정")]
    public ParticleSystem rootParticle;
    public IObjectPool<SkillBase> pool;
    public HitEffect hitEffect;
    public int Id;
    public int UpgradeType { get; private set; }
    public Player UsePlayer { get; private set; }

    public void Init(int id, IObjectPool<SkillBase> pool,Player UsePlayer, int UpgradeType = 0)
    {
        Id = id;
        this.pool = pool;
        this.UpgradeType = UpgradeType;
        this.UsePlayer = UsePlayer;
    }

    public void SkillColliderOn()
    {
        if(hitbox != null)
            hitbox.enabled = true;
    }

    public void SkillColliderOff()
    {
        if(hitbox != null)
            hitbox.enabled = false;
    }

    /// <summary>
    /// 피해를 주는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DamageTrigger(ITakeDamgeable target, Player player)
    {

    }

    /// <summary>
    /// 회복 시키는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void HealTrigger(Player player)
    {

    }

    /// <summary>
    /// 디버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void DebuffTrigger(Player player)
    {

    }

    /// <summary>
    /// 버프를 부여하는 효과가 존재할 경우 구현
    /// </summary>
    public virtual void BuffTrigger(Player player)
    {

    }

    private void OnParticleSystemStopped()
    {
        if (hitbox != null && hitbox.enabled) hitbox.enabled = false;
        pool.Release(this);
    }

    public void OnParticleSystemStoppedByChild()
    {
        pool.Release(this);
    }
}
