using UnityEngine;

public class DragonBoss : MonoBehaviour
{
    [SerializeField] private MonsterData monsterData;

    private float currentHp;

    private void Awake()
    {
        currentHp = monsterData.MaxHp;
    }
}