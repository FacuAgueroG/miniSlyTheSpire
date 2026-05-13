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
    public List<EnemyAI> allEnemies = new List<EnemyAI>(); // Arrastrá tus 3 enemigos acá

    [Header("Orden de Turnos")]
    private List<EnemyAI> turnOrder = new List<EnemyAI>();
    private int currentEnemyIndexInRound = 0;

    [Header("Configuración Visual")]
    public Color waitingColor = Color.white;
    public Color alreadyActedColor = Color.gray; // Color cuando ya atacó

    private void Awake() { Instance = this; }

    private void Start() {
        // Al inicio, definimos el orden por primera vez
        DetermineEnemyOrder();
        StartPlayerTurn();
    }

    // El "Dado" que decide quién va 1, 2 o 3
    public void DetermineEnemyOrder() {
        // Filtramos solo los que están vivos
        turnOrder = allEnemies.Where(e => e != null && e.GetComponent<CharacterHealth>().currentHealth > 0).ToList();

        // Mezclamos la lista (El Dado)
        for (int i = 0; i < turnOrder.Count; i++) {
            EnemyAI temp = turnOrder[i];
            int randomIndex = Random.Range(i, turnOrder.Count);
            turnOrder[i] = turnOrder[randomIndex];
            turnOrder[randomIndex] = temp;
        }

        // Inyectamos los números en los Textos de cada enemigo y reseteamos color
        for (int i = 0; i < turnOrder.Count; i++) {
            if (turnOrder[i].intentValueText != null) {
                // Ponemos el número de orden (1, 2, 3) en el texto que ya tenías o uno nuevo
                turnOrder[i].orderText.text = (i + 1).ToString();
            }
            turnOrder[i].GetComponent<CharacterEffects>().characterSprite.color = waitingColor;
            turnOrder[i].DecideNextMove();
        }
        currentEnemyIndexInRound = 0;
    }

    public void StartPlayerTurn() {
        // Si ya todos los enemigos de la ronda actuaron, tiramos el dado de nuevo
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

        // Buscamos al enemigo que le toca (saltando muertos)
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

            // Cambio de color porque ya actuó
            enemyEffects.characterSprite.color = alreadyActedColor;
            enemyEffects.SetTurnVisual(false);

            currentEnemyIndexInRound++;
        }

        // Volvemos al player (Intercalado)
        StartPlayerTurn();
    }
}