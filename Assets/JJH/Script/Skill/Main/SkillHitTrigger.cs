using Photon.Pun;
using UnityEngine;
using static Constant_JJH;

public class SkillHitTrigger : MonoBehaviour
{
    private SkillBase parentSkill;
    private LayerMask bossLayer;

    private bool onHit;

    private void Awake()
    {
        parentSkill = GetComponentInParent<SkillBase>();
        bossLayer = LayerMask.NameToLayer(BOSS_TAG);
    }

    private void OnDisable()
    {
        onHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(BOSS_TAG)) return;
        if (parentSkill == null) { Debug.LogWarning("스킬 정보가 없습니다."); return; }

        // 이후 플레이어 객체가 정해질 경우 보스
        // (ITakeDamageable은 최상단에 있으므로 root로 추적)
        // 만 피격되도록 변경
        if (other.transform.root.TryGetComponent(out ITakeDamgeable target) && 
            other.gameObject.layer == bossLayer)
        {            // 이후 Player에 콜백이 달린다면 그것으로 교체
            if (parentSkill.UsePlayer.photonView.IsMine && !onHit)
            {
                parentSkill.DamageTrigger(target, parentSkill.UsePlayer);
                parentSkill.HealTrigger(parentSkill.UsePlayer);
                parentSkill.DebuffTrigger(parentSkill.UsePlayer);
                parentSkill.BuffTrigger(parentSkill.UsePlayer);
                onHit = true;
            }

            if(parentSkill.hitEffect != null)
            {
                parentSkill.hitEffect.transform.position = other.transform.position;
                parentSkill.hitEffect.gameObject.SetActive(true);
            }   
        }
    }
}
