using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AttackDamage : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private DragonBoss boss;

    private readonly HashSet<Player> hitPlayers = new HashSet<Player>();
    public LayerMask PlayerLayers => layerMask;
    public void SetBoss(DragonBoss newBoss)
    {
        boss = newBoss;
    }
    public void ResetHits()
    {
        hitPlayers.Clear();
    }
    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }
    public void TryDamage(Collider other)
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;
        int collidedLayerMask = 1 << other.gameObject.layer;
        if ((layerMask.value & collidedLayerMask) == 0 || boss == null)
            return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null || !player.IsAlive || !hitPlayers.Add(player))
            return;
        var tInfo = new tAttackInfo(transform.position, boss.Attack);
        player.TakeDamage(tInfo);
        Debug.Log($"대미지 피해 {tInfo.Damage} 위치:{tInfo.Pos}");
    }
}
