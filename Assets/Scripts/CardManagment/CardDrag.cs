using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private CardDisplay cardDisplay;

    private Vector2 originalPosition;
    public float playThresholdPercentage = 0.4f;

    private void Awake() {
        rt = GetComponent<RectTransform>();
        cardDisplay = GetComponent<CardDisplay>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (BattleManager.Instance.currentState != BattleManager.BattleState.PlayerTurn) {
            eventData.pointerDrag = null;
            return;
        }

        if (cardDisplay.cardData.isTargeted) {
            HandView.Instance.SetDragLock(rt, true);
            canvasGroup.blocksRaycasts = false;
            TargetingArrow.Instance.ActivateArrow(rt.position);
        }
        else {
            originalPosition = rt.anchoredPosition;
            HandView.Instance.RemoveCard(rt);
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (cardDisplay.cardData.isTargeted) {
            TargetingArrow.Instance.UpdateArrow(rt.position, eventData.position);
        }
        else {
            rt.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        // 1. IMPORTANTE: Reactivamos Raycasts inmediatamente para evitar el "congelamiento" visual
        canvasGroup.blocksRaycasts = true;

        CharacterEffects targetFound = null;

        // --- FIX DEL INPUT SYSTEM ---
        // Usamos eventData.position que ya viene del Input System, 
        // en lugar de Input.mousePosition (que causa el error).
        Vector2 mousePosWorld = Camera.main.ScreenToWorldPoint(eventData.position);

        RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero);

        if (hit.collider != null) {
            var effects = hit.collider.GetComponentInParent<CharacterEffects>();
            if (effects != null && effects.gameObject != BattleManager.Instance.player.gameObject) {
                targetFound = effects;
            }
        }

        if (cardDisplay.cardData.isTargeted) {
            TargetingArrow.Instance.DeactivateArrow();
            HandView.Instance.SetDragLock(rt, false);

            if (targetFound != null) {
                TryPlayCard(targetFound);
            }
            else {
                // Si no hay objetivo, forzamos el reset de la carta en la mano
                HandView.Instance.ClearHovered(rt);
                HandView.Instance.Layout();
            }
        }
        else {
            bool aboveThreshold = rt.position.y > Screen.height * playThresholdPercentage;
            if (aboveThreshold) TryPlayCard(null);
            else ReturnToHand();
        }
    }

    private void TryPlayCard(CharacterEffects target) {
        int cost = cardDisplay.cardData.manaCost;
        if (ManaManager.Instance.HasEnoughMana(cost)) {
            // ... lógica de efectos igual ...
            ManaManager.Instance.ConsumeMana(cost);

            if (cardDisplay.cardData.isTargeted) {
                HandView.Instance.RemoveCard(rt);
            }

            CharacterEffects player = BattleManager.Instance.player;
            foreach (var effect in cardDisplay.cardData.effects) {
                int amount = (int)effect.value1;
                switch (effect.effectType) {
                    case CardEffectType.DealDamage:
                        if (cardDisplay.cardData.isAoE) {
                            foreach (EnemyAI enemy in BattleManager.Instance.allEnemies) {
                                if (enemy != null && enemy.GetComponent<CharacterHealth>().currentHealth > 0) {
                                    enemy.GetComponent<CharacterEffects>().ProcessIncomingDamage(amount + player.attackBuff);
                                }
                            }
                        }
                        else if (target != null) {
                            target.ProcessIncomingDamage(amount + player.attackBuff);
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
            // Si no hay maná, reseteamos el estado visual para que no flote
            if (!cardDisplay.cardData.isTargeted) ReturnToHand();
            else {
                HandView.Instance.ClearHovered(rt);
                HandView.Instance.Layout();
            }
        }
    }

    private void ReturnToHand() {
        if (HandView.Instance != null) HandView.Instance.AddCard(rt);
    }
}