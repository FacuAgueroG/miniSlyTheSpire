using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "EnemiesConfig/Enemy Data")]
public class EnemyData : ScriptableObject {
    public string enemyName;
    public GameObject enemyPrefab;
    public int pointValue;

    public enum EnemyType { Normal, Poison, Buff, Boss }
    public EnemyType type;
}