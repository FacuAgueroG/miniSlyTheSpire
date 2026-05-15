using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class EncounterDirector : MonoBehaviour {
    [Header("Configuración de Datos")]
    public List<EnemyData> allAvailableEnemies;
    public List<ArenaTier> arenaDefinitions;

    [Header("Spawn Points")]
    public Transform[] spawnPoints; // Tus 3 puntos

    [Header("Estado Actual")]
    public int currentArena = 1;
    private bool lastFightWasTooEasy = false;

    [Header("UI Referencias")]
    public TextMeshProUGUI arenaText;
    public GameObject nextArenaButton;

    private void Start() {
        // Generar la primera arena al iniciar el juego
        GenerateNextArena();
    }

    // Esta función es la que llamarás desde tu botón "Siguiente Arena"
    public void GenerateNextArena() {
        ArenaTier config = GetConfigForLevel(currentArena);
        int budget = CalculateBudget(config);

        List<EnemyData> selectedEnemies = SelectEnemiesForBudget(budget, config);
        SpawnEnemies(selectedEnemies);

        currentArena++;
    }

    private int CalculateBudget(ArenaTier config) {
        int min = config.minPoints;
        // Lógica de "Sticky Minimum"
        if (lastFightWasTooEasy) {
            min += 15;
            lastFightWasTooEasy = false; // Se resetea después de aplicarse
        }

        int budget = Random.Range(min, config.maxPoints + 1);
        return budget;
    }

    private List<EnemyData> SelectEnemiesForBudget(int budget, ArenaTier config) {
        List<EnemyData> encounter = new List<EnemyData>();

        // 1. Filtrar enemigos desbloqueados por nivel
        var available = allAvailableEnemies.Where(e => IsEnemyUnlocked(e)).ToList();

        // 2. Lógica de JEFE
        if (config.isBossLevel) {
            var boss = allAvailableEnemies.Find(e => e.type == EnemyData.EnemyType.Boss);
            encounter.Add(boss);
            budget = config.bonusPointsForBoss; // El presupuesto ahora es para los minions
        }

        int currentSpent = 0;
        int poisonCount = 0;
        int buffCount = 0;

        // 3. Llenado de slots (máximo 3 enemigos en total)
        while (encounter.Count < 3) {
            // Filtrar por presupuesto restante y restricciones de tipo
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

        // Check para el "Sticky Minimum" de la PRÓXIMA arena
        if (currentSpent <= (config.minPoints + 10)) lastFightWasTooEasy = true;

        return encounter;
    }

    private bool IsEnemyUnlocked(EnemyData e) {
        // Tus reglas de desbloqueo
        if (e.pointValue == 20 && currentArena < 4) return false; // E3
        if ((e.pointValue == 20 || e.pointValue == 25) && currentArena < 6) return false; // E4 y E5
        if (e.pointValue == 30 && currentArena < 9) return false; // E6
        if (e.type == EnemyData.EnemyType.Boss) return false; // Los jefes se manejan aparte
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
        // 1. Limpiar lista antigua en el BattleManager
        BattleManager.Instance.allEnemies.Clear();

        // 2. Limpiar spawns físicos
        foreach (var sp in spawnPoints) {
            foreach (Transform child in sp) Destroy(child.gameObject);
        }

        // 3. Instanciar nuevos
        for (int i = 0; i < enemies.Count; i++) {
            if (i < spawnPoints.Length) {
                //GameObject newObj = Instantiate(enemies[i].enemyPrefab, spawnPoints[i].position, Quaternion.identity, spawnPoints[i]);
                GameObject newObj = Instantiate(enemies[i].enemyPrefab, spawnPoints[i]);

                // CONEXIÓN CLAVE: Añadir el script EnemyAI del prefab a la lista del BattleManager
                EnemyAI enemyScript = newObj.GetComponent<EnemyAI>();
                if (enemyScript != null) {
                    BattleManager.Instance.allEnemies.Add(enemyScript);
                }
            }
        }

        // 4. Actualizar UI
        if (arenaText != null) arenaText.text = "Arena: " + currentArena;
        if (nextArenaButton != null) nextArenaButton.SetActive(false); // Ocultar botón al empezar pelea

        // 5. Reiniciar el turno en el BattleManager
        BattleManager.Instance.DetermineEnemyOrder();
        BattleManager.Instance.StartPlayerTurn();
    }

    private ArenaTier GetConfigForLevel(int level) {
        // 1. Intentamos buscar si definiste este nivel a mano en el Inspector (para niveles 1 al 20)
        var config = arenaDefinitions.FirstOrDefault(a => a.arenaLevel == level);

        // 2. Si el nivel existe en tu lista manual, lo usamos tal cual
        if (config.arenaLevel != 0) {
            return config;
        }

        // 3. Si NO está en la lista (niveles > 20 o huecos), entramos al MODO INFINITO
        bool esNivelDeJefe = (level % 10 == 0);

        if (esNivelDeJefe) {
            // REGLA: Jefe cada 10 niveles con Max 50 para escoltas
            return new ArenaTier {
                arenaLevel = level,
                minPoints = 30, // Un mínimo para que el jefe no esté tan solo
                maxPoints = 50,
                isBossLevel = true,
                bonusPointsForBoss = 50 // Este es el presupuesto para los minions que acompañan al jefe
            };
        }
        else {
            // REGLA: Niveles normales después del 20 (Max 90)
            return new ArenaTier {
                arenaLevel = level,
                minPoints = 35,
                maxPoints = 90,
                isBossLevel = false
            };
        }
    }
}