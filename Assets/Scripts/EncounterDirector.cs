using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class EncounterDirector : MonoBehaviour {
    public static EncounterDirector Instance;

    [Header("Configuración de Datos")]
    public List<EnemyData> allAvailableEnemies;
    public List<ArenaTier> arenaDefinitions;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Estado Actual")]
    public int currentArena = 1;
    private bool lastFightWasTooEasy = false;

    [Header("UI Referencias")]
    public TextMeshProUGUI arenaText;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        // Al darle play, generamos la primera arena automáticamente
        GenerateNextArena();
    }

    public void GenerateNextArena() {
        ArenaTier config = GetConfigForLevel(currentArena);
        int budget = CalculateBudget(config);

        List<EnemyData> selectedEnemies = SelectEnemiesForBudget(budget, config);
        SpawnEnemies(selectedEnemies);

        currentArena++;
    }

    private int CalculateBudget(ArenaTier config) {
        int min = config.minPoints;
        if (lastFightWasTooEasy) {
            min += 15;
            lastFightWasTooEasy = false;
        }
        return Random.Range(min, config.maxPoints + 1);
    }

    private List<EnemyData> SelectEnemiesForBudget(int budget, ArenaTier config) {
        List<EnemyData> encounter = new List<EnemyData>();
        var available = allAvailableEnemies.Where(e => IsEnemyUnlocked(e)).ToList();

        if (config.isBossLevel) {
            var boss = allAvailableEnemies.Find(e => e.type == EnemyData.EnemyType.Boss);
            if (boss != null) encounter.Add(boss);
            budget = config.bonusPointsForBoss;
        }

        int currentSpent = 0;
        int poisonCount = 0;
        int buffCount = 0;

        while (encounter.Count < 3) {
            var candidates = available.Where(e =>
                (currentSpent + e.pointValue <= budget) &&
                CheckTypeRestrictions(e, poisonCount, buffCount)
            ).ToList();

            if (candidates.Count == 0) break;

            EnemyData picked = candidates[Random.Range(0, candidates.Count)];
            encounter.Add(picked);
            currentSpent += picked.pointValue;

            if (picked.type == EnemyData.EnemyType.Poison) poisonCount++;
            if (picked.type == EnemyData.EnemyType.Buff) buffCount++;
        }

        if (currentSpent <= (config.minPoints + 10)) lastFightWasTooEasy = true;

        return encounter;
    }

    private bool IsEnemyUnlocked(EnemyData e) {
        if (e.pointValue == 20 && currentArena < 4) return false;
        if ((e.pointValue == 20 || e.pointValue == 25) && currentArena < 6) return false;
        if (e.pointValue == 30 && currentArena < 9) return false;
        if (e.type == EnemyData.EnemyType.Boss) return false;
        return true;
    }

    private bool CheckTypeRestrictions(EnemyData e, int pCount, int bCount) {
        if (currentArena <= 10) {
            if (e.type == EnemyData.EnemyType.Poison && pCount >= 1) return false;
            if (e.type == EnemyData.EnemyType.Buff && bCount >= 1) return false;
        }
        else if (currentArena <= 20) {
            if (e.type == EnemyData.EnemyType.Poison && pCount >= 2) return false;
            if (e.type == EnemyData.EnemyType.Buff && bCount >= 2) return false;
        }
        return true;
    }

    private void SpawnEnemies(List<EnemyData> enemies) {
        BattleManager.Instance.allEnemies.Clear();

        foreach (var sp in spawnPoints) {
            foreach (Transform child in sp) Destroy(child.gameObject);
        }

        for (int i = 0; i < enemies.Count; i++) {
            if (i < spawnPoints.Length) {
                GameObject newObj = Instantiate(enemies[i].enemyPrefab, spawnPoints[i]);
                //newObj.transform.localPosition = Vector3.zero;
                //newObj.transform.localRotation = enemies[i].enemyPrefab.transform.rotation;

                EnemyAI enemyScript = newObj.GetComponent<EnemyAI>();
                if (enemyScript != null) {
                    BattleManager.Instance.allEnemies.Add(enemyScript);
                }
            }
        }

        if (arenaText != null) arenaText.text = "Arena: " + currentArena;

        BattleManager.Instance.DetermineEnemyOrder();
        BattleManager.Instance.StartPlayerTurn();
    }

    private ArenaTier GetConfigForLevel(int level) {
        var config = arenaDefinitions.FirstOrDefault(a => a.arenaLevel == level);
        if (config.arenaLevel != 0) {
            return config;
        }

        bool esNivelDeJefe = (level % 10 == 0);
        if (esNivelDeJefe) {
            return new ArenaTier { arenaLevel = level, minPoints = 30, maxPoints = 50, isBossLevel = true, bonusPointsForBoss = 50 };
        }
        else {
            return new ArenaTier { arenaLevel = level, minPoints = 35, maxPoints = 90, isBossLevel = false };
        }
    }
}