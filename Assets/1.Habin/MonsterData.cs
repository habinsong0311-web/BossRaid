using UnityEngine;


[CreateAssetMenu(menuName = "Game Data/Monster Data")]
public class MonsterData : ScriptableObject
{
    [SerializeField] private string monsterName;
    [SerializeField] private float monsterMaxHp;
    [SerializeField] private float monsterCurrentHp;
    [SerializeField] private float monsterAttack;
    [SerializeField] private float monsterDefense;

    public string MonsterName => monsterName;
    public float MaxHp => monsterMaxHp;
    public float Attack => monsterAttack;
    public float Defense => monsterDefense;
}
