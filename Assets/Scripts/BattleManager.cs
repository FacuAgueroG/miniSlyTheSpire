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
        // CAMBIO: No filtramos por vida aquí en el Start para evitar el bug de inicialización
        // Simplemente tomamos a todos los que existan en la lista.
        turnOrder = allEnemies.Where(e => e != null).ToList();

        // El Dado (Mezcla)
        for (int i = 0; i < turnOrder.Count; i++) {
            EnemyAI temp = turnOrder[i];
            int randomIndex = Random.Range(i, turnOrder.Count);
            turnOrder[i] = turnOrder[randomIndex];
            turnOrder[randomIndex] = temp;
        }

        // Asignación de visuales
        for (int i = 0; i < turnOrder.Count; i++) {
            // CORRECCIÓN: Chequeamos el componente correcto antes de escribir
            if (turnOrder[i].orderText != null) {
                turnOrder[i].orderText.text = (i + 1).ToString();
            }

            // Si usas un grupo visual (ej. un círculo detrás del número), lo activamos
            if (turnOrder[i].orderVisualGroup != null) {
                turnOrder[i].orderVisualGroup.SetActive(true);
            }

            turnOrder[i].GetComponent<CharacterEffects>().characterSprite.color = waitingColor;

            // Esto es vital: Cada enemigo decide SU INTENT ahora mismo
            turnOrder[i].DecideNextMove();
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
            enemyEffects.SetTurnVisual(true);

            yield return new WaitForSeconds(0.6f);
            enemyEffects.ClearBlock();
            activeEnemy.PerformTurnAction(player);

            yield return new WaitForSeconds(0.8f);

            enemyEffects.characterSprite.color = alreadyActedColor;
            enemyEffects.SetTurnVisual(false);

            // --- CINTURÓN DE SEGURIDAD ---
            if (activeEnemy.orderVisualGroup != null) {
                // Solo lo apagamos si NO es el BattleManager
                if (activeEnemy.orderVisualGroup != this.gameObject) {
                    activeEnemy.orderVisualGroup.SetActive(false);
                }
                else {
                    Debug.LogError($"¡OJO! El enemigo {activeEnemy.name} tiene al BattleManager en el slot OrderVisualGroup. ¡Corrígelo en el Inspector!");
                }
            }

            currentEnemyIndexInRound++;
        }

        // Volver al jugador
        StartPlayerTurn();
    }
}