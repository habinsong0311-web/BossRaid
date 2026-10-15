using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using SF = UnityEngine.SerializeField;

[RequireComponent(typeof(ParticleSystem))]
public class SkillProjectileTrigger : MonoBehaviour
{
    [SF] private SkillBase skillRoot;
    [SF] private ParticleSystem ps;
    private readonly List<ParticleCollisionEvent> collisionEvents = new();
    private bool onHit = false;
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // 파티클이 무언가와 부딪혔을 때 유니티가 자동으로 실행하는 함수
    void OnParticleCollision(GameObject other)
    {
        Debug.Log("일단 부딪치긴 함");
        if (other.TryGetComponent(out ITakeDamgeable target))
        {
            // 이후 Player에 콜백이 달린다면 그것으로 교체
            if(skillRoot.UsePlayer.photonView.IsMine)
            {
                skillRoot.DamageTrigger(target, skillRoot.UsePlayer);
                skillRoot.HealTrigger(skillRoot.UsePlayer);
                skillRoot.BuffTrigger(skillRoot.UsePlayer);
                skillRoot.DebuffTrigger(skillRoot.UsePlayer);
            }
        }

        int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

        if (numCollisionEvents > 0 && collisionEvents.Count > 0)
        {
            Vector3 hitPoint = collisionEvents[0].intersection;
            skillRoot.hitEffect.transform.position = hitPoint;
            skillRoot.hitEffect.gameObject.SetActive(true);
        }
        if(skillRoot.TryGetComponent(out IProjectile projectile))
        {
            skillRoot.rootParticle.Stop();
            projectile.Stop();
        }
        var main = ps.main;

        if (main.loop) main.loop = false;
    }
}

