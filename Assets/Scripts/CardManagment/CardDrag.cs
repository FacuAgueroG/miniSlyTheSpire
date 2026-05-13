using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private CardDisplay cardDisplay;
    private HandView handView;

    private Vector2 originalPosition;
    public float playThresholdPercentage = 0.4f;

    private void Awake() {
        rt = GetComponent<RectTransform>();
        cardDisplay = GetComponent<CardDisplay>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start() {
        handView = Object.FindFirstObjectByType<HandView>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (BattleManager.Instance.currentState != BattleManager.BattleState.PlayerTurn) {
            eventData.pointerDrag = null;
            return;
        }
        originalPosition = rt.anchoredPosition;
        if (handView != null) handView.RemoveCard(rt);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) { rt.position = eventData.position; }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true;

        bool aboveThreshold = rt.position.y > Screen.height * playThresholdPercentage;
        CharacterEffects targetFound = null;

        // Buscamos enemigo (Raycast UI)
        foreach (var obj in eventData.hovered) {
            var effects = obj.GetComponent<CharacterEffects>();
            if (effects != null && obj != BattleManager.Instance.player.gameObject) {
                targetFound = effects;
                break;
            }
        }

        if (aboveThreshold) {
            // Si la carta requiere un target pero no soltamos sobre uno, vuelve a la mano
            if (cardDisplay.cardData.isTargeted) {
                if (targetFound != null) TryPlayCard(targetFound);
                else ReturnToHand();
            }
            else {
                // Si es AoE o Buff (no targeted), se juega sin target
                TryPlayCard(null);
            }
        }
        else {
            ReturnToHand();
        }
    }

    private void TryPlayCard(CharacterEffects target) {
        int cost = cardDisplay.cardData.manaCost;
        if (ManaManager.Instance.HasEnoughMana(cost)) {
            ManaManager.Instance.ConsumeMana(cost);

            CharacterEffects player = BattleManager.Instance.player;

            // Iteramos sobre los efectos de la carta
            foreach (var effect in cardDisplay.cardData.effects) {
                int amount = (int)effect.value1;

                switch (effect.effectType) {
                    case CardEffectType.DealDamage:
                        // --- NUEVA LÓGICA DE ATAQUE Y VENENO ---
                        if (cardDisplay.cardData.isAoE) {
                            // ATAQUE EN ÁREA: Golpeamos a todos los enemigos de la lista
                            foreach (EnemyAI enemy in BattleManager.Instance.allEnemies) {
                                if (enemy != null && enemy.GetComponent<CharacterHealth>().currentHealth > 0) {
                                    enemy.GetComponent<CharacterEffects>().ProcessIncomingDamage(amount + player.attackBuff);
                                    // Al golpear a cada uno, nos quitamos SU veneno
                                    player.ClearPoisonFromSource(enemy);
                                }
                            }
                        }
                        else {
                            // ATAQUE SINGLE TARGET
                            if (target != null) {
                                target.ProcessIncomingDamage(amount + player.attackBuff);

                                // Si el target es un enemigo, nos quitamos su veneno
                                EnemyAI enemyComp = target.GetComponent<EnemyAI>();
                                if (enemyComp != null) {
                                    player.ClearPoisonFromSource(enemyComp);
                                }
                            }
                        }
                        break;

                    case CardEffectType.GainBlock:
                        player.AddBlock(amount);
                        break;

                    case CardEffectType.GainAttackBuff:
                        player.AddAttackBuff(amount);
                        break;
                }
            }

            DiscardManager.Instance.AddCardToDiscard(cardDisplay.cardData);
            Destroy(gameObject);
        }
        else {
            ReturnToHand();
        }
    }

    private void ReturnToHand() {
        if (handView != null) handView.AddCard(rt);
        else rt.anchoredPosition = originalPosition;
    }
}