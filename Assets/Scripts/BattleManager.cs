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

    [Header("Orden de Turnos")]
    private List<EnemyAI> turnOrder = new List<EnemyAI>();
    private int currentEnemyIndexInRound = 0;

    [Header("Configuración Visual")]
    public Color waitingColor = Color.white;
    public Color alreadyActedColor = Color.gray;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        // IMPORTANTE: Primero forzamos a que todos decidan qué hacer y se muestren
        // Antes de que el jugador pueda siquiera tocar una carta.
        DetermineEnemyOrder();
        StartPlayerTurn();
    }

    public void DetermineEnemyOrder() {
        // SOLUCIÓN AL RANDOMIZER: Solo metemos en el bombo a los que tengan vida > 0
        // Esto automáticamente saca a los muertos de la ecuación de turnos.
        turnOrder = allEnemies.Where(e => e != null && e.GetComponent<CharacterHealth>().currentHealth > 0).ToList();

        for (int i = 0; i < turnOrder.Count; i++) {
            EnemyAI enemy = turnOrder[i];

            // 1. Asignar número de turno
            if (enemy.orderText != null) {
                enemy.orderText.text = (i + 1).ToString();
            }

            if (enemy.orderVisualGroup != null) {
                enemy.orderVisualGroup.SetActive(true);
            }

            // 2. Cambiar color usando el método seguro (CORRECCIÓN LÍNEA 58)
            if (enemy.TryGetComponent<CharacterEffects>(out var effects)) {
                effects.SetVisualColor(waitingColor);
            }

            // 3. Decidir siguiente movimiento (esto ahora SÍ se ejecutará)
            enemy.DecideNextMove();
        }
        currentEnemyIndexInRound = 0;
    }

    public void StartPlayerTurn() {
        // Si ya todos actuaron, toca re-organizar
        if (currentEnemyIndexInRound >= turnOrder.Count) {
            DetermineEnemyOrder();
        }

        currentState = BattleState.PlayerTurn;
        player.OnTurnStarted();
        player.ClearBlock();
        player.SetTurnVisual(true);

        ManaManager.Instance.ResetMana();
        DeckManager.Instance.DrawCards(DeckManager.Instance.cardsPerTurn);
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

            // --- PASO 1: EMPIEZA EL TURNO DEL ENEMIGO ---
            enemyEffects.OnTurnStarted(); // Aquí recibe daño de veneno si lo tiene
            enemyEffects.SetTurnVisual(true);

            yield return new WaitForSeconds(0.6f);
            enemyEffects.ClearBlock();

            // --- PASO 2: REALIZA LA ACCIÓN ---
            // Aquí el ataque usará el buff porque aún no lo hemos descontado
            activeEnemy.PerformTurnAction(player);

            yield return new WaitForSeconds(0.8f);

            // --- PASO 3: TERMINA EL TURNO DEL ENEMIGO ---
            enemyEffects.OnTurnEnded(); // AQUÍ restamos la duración del buff

            enemyEffects.SetVisualColor(alreadyActedColor);
            enemyEffects.SetTurnVisual(false);

            if (activeEnemy.orderVisualGroup != null && activeEnemy.orderVisualGroup != this.gameObject) {
                activeEnemy.orderVisualGroup.SetActive(false);
            }

            currentEnemyIndexInRound++;
        }

        StartPlayerTurn();
    }
}