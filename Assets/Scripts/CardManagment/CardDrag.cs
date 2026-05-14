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
            // --- CARTA DE APUNTADO ---
            // 1. Bloqueamos el hover en la mano para que no se baje
            HandView.Instance.SetDragLock(rt, true);
            // 2. Apagamos raycasts para que la flecha detecte lo que hay ABAJO (el enemigo)
            canvasGroup.blocksRaycasts = false;
            // 3. Activamos la flecha
            TargetingArrow.Instance.ActivateArrow(rt.position);
        }
        else {
            // --- CARTA NORMAL (AOE/DEFENSA) ---
            originalPosition = rt.anchoredPosition;
            // La quitamos de la mano para moverla libremente
            HandView.Instance.RemoveCard(rt);
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (cardDisplay.cardData.isTargeted) {
            // Solo actualizamos la flecha, la carta NO se mueve
            TargetingArrow.Instance.UpdateArrow(rt.position, eventData.position);
        }
        else {
            // Movemos la carta siguiendo al mouse
            rt.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {

        // Capturamos los objetos antes de reactivar para que la carta no se estorbe a sí misma

        List<GameObject> hoveredObjects = eventData.hovered;

        canvasGroup.blocksRaycasts = true;



        CharacterEffects targetFound = null;

        foreach (var obj in hoveredObjects) {

            // CAMBIO AQUÍ: Usamos GetComponentInParent para encontrar al enemigo

            // aunque el mouse toque una imagen hija (el cuerpo, la sombra, etc.)

            var effects = obj.GetComponentInParent<CharacterEffects>();



            if (effects != null && effects.gameObject != BattleManager.Instance.player.gameObject) {

                targetFound = effects;

                break;

            }

        }



        if (cardDisplay.cardData.isTargeted) {

            TargetingArrow.Instance.DeactivateArrow();

            HandView.Instance.SetDragLock(rt, false);



            if (targetFound != null) {

                TryPlayCard(targetFound);

            }

            // Si no hay targetFound, la carta simplemente vuelve a su lugar en la mano

        }

        else {

            // Lógica para cartas AOE/Defensa (Umbral de altura)

            bool aboveThreshold = rt.position.y > Screen.height * playThresholdPercentage;

            if (aboveThreshold) TryPlayCard(null);

            else ReturnToHand();

        }

    }

    private void TryPlayCard(CharacterEffects target) {
        int cost = cardDisplay.cardData.manaCost;
        if (ManaManager.Instance.HasEnoughMana(cost)) {
            ManaManager.Instance.ConsumeMana(cost);

            // Si la carta era targeted, ahora SÍ debemos quitarla de la mano visualmente antes de destruirla
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
                                    player.ClearPoisonFromSource(enemy);
                                }
                            }
                        }
                        else if (target != null) {
                            target.ProcessIncomingDamage(amount + player.attackBuff);
                            EnemyAI enemyComp = target.GetComponent<EnemyAI>();
                            if (enemyComp != null) player.ClearPoisonFromSource(enemyComp);
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
            if (!cardDisplay.cardData.isTargeted) ReturnToHand();
            // Si es targeted y no hay mana, simplemente se desbloquea el hover (ya hecho arriba)
        }
    }

    private void ReturnToHand() {
        if (HandView.Instance != null) HandView.Instance.AddCard(rt);
    }
}