using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class BattleManager : MonoBehaviour {
    public static BattleManager Instance;
    public enum BattleState { PlayerTurn, EnemyTurn, Busy }
    public BattleState currentState;

    [Header("Entidades")]
    public CharacterEffects player;
    public List<EnemyAI> allEnemies = new List<EnemyAI>();

    private List<EnemyAI> turnOrder = new List<EnemyAI>();
    private int currentEnemyIndexInRound = 0;

    public Color waitingColor = Color.white;
    public Color alreadyActedColor = Color.gray;

    private void Awake() { Instance = this; }

    private void Start() {
        DetermineEnemyOrder();
        StartPlayerTurn();
    }

    public void DetermineEnemyOrder() {
        turnOrder = allEnemies.Where(e => e != null && e.GetComponent<CharacterHealth>().currentHealth > 0).ToList();
        for (int i = 0; i < turnOrder.Count; i++) {
            EnemyAI enemy = turnOrder[i];
            if (enemy.orderText != null) enemy.orderText.text = (i + 1).ToString();
            if (enemy.orderVisualGroup != null) enemy.orderVisualGroup.SetActive(true);
            if (enemy.TryGetComponent<CharacterEffects>(out var effects)) effects.SetVisualColor(waitingColor);
            enemy.DecideNextMove();
        }
        currentEnemyIndexInRound = 0;
    }

    public void StartPlayerTurn(bool isFirstTurn = false) {
        if (currentEnemyIndexInRound >= turnOrder.Count) {
            DetermineEnemyOrder();
        }

        currentState = BattleState.PlayerTurn;
        player.OnTurnStarted();
        player.ClearBlock();
        player.SetTurnVisual(true);

        ManaManager.Instance.ResetMana();

        // Si es el primer turno de la batalla, robamos la mano inicial (5)
        // Si no, robamos lo normal por turno (1)
        int cantidadARobar = isFirstTurn ? DeckManager.Instance.initialDrawCount : DeckManager.Instance.cardsPerTurn;
        DeckManager.Instance.DrawCards(cantidadARobar);
    }

    public void OnEndTurnButton() {
        if (currentState != BattleState.PlayerTurn) return;
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine() {
        currentState = BattleState.Busy;
        player.SetTurnVisual(false);
        EnemyAI activeEnemy = null;
        while (currentEnemyIndexInRound < turnOrder.Count) {
            var target = turnOrder[currentEnemyIndexInRound];
            if (target != null && target.GetComponent<CharacterHealth>().currentHealth > 0) {
                activeEnemy = target;
                break;
            }
            currentEnemyIndexInRound++;
        }

        if (activeEnemy != null) {
            CharacterEffects enemyEffects = activeEnemy.GetComponent<CharacterEffects>();
            enemyEffects.OnTurnStarted();
            enemyEffects.SetTurnVisual(true);
            yield return new WaitForSeconds(0.6f);
            enemyEffects.ClearBlock();
            activeEnemy.PerformTurnAction(player);
            yield return new WaitForSeconds(0.8f);
            enemyEffects.OnTurnEnded();
            enemyEffects.SetVisualColor(alreadyActedColor);
            enemyEffects.SetTurnVisual(false);
            if (activeEnemy.orderVisualGroup != null) activeEnemy.orderVisualGroup.SetActive(false);
            currentEnemyIndexInRound++;
        }
        StartPlayerTurn();
    }

    // --- ESTA ES LA ÚNICA VERSIÓN QUE DEBE QUEDAR ---
    public void CheckBattleOver() {
        bool allDead = allEnemies.All(e => e == null || e.GetComponent<CharacterHealth>().currentHealth <= 0);
        if (allDead) {
            Debug.Log("¡Victoria! Transicionando al mapa...");
            StartCoroutine(EndBattleTransition());
        }
    }

    private IEnumerator EndBattleTransition() {
        yield return new WaitForSeconds(1.5f);

        // 1. Limpiamos mano y descarte antes de ir al mapa
        if (DeckManager.Instance != null) {
            DeckManager.Instance.ReturnAllToDeck();
        }

        // 2. Volvemos al mapa
        if (MapManager.Instance != null) {
            MapManager.Instance.GoToMap();
        }
    }
}