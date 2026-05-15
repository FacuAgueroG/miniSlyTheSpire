using UnityEngine;

[System.Serializable]
public struct ArenaTier {
    public int arenaLevel;
    public int minPoints;
    public int maxPoints;
    public bool isBossLevel;
    public int bonusPointsForBoss; // Puntos extra para escoltas
}