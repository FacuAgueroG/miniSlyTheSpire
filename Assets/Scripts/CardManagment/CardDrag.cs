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

        if (cardDisplay.cardInstance.data.isTargeted) {
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
        if (cardDisplay.cardInstance.data.isTargeted) {
            Camera arenaCam = MapManager.Instance.arenaCamera.GetComponent<Camera>();
            RectTransform arrowRect = TargetingArrow.Instance.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToWorldPointInRectangle(arrowRect, eventData.position, arenaCam, out Vector3 mouseWorldPos);
            TargetingArrow.Instance.UpdateArrow(rt.position, mouseWorldPos);
        }
        else {
            Camera arenaCam = MapManager.Instance.arenaCamera.GetComponent<Camera>();
            RectTransformUtility.ScreenPointToWorldPointInRectangle(rt.parent as RectTransform, eventData.position, arenaCam, out Vector3 cardWorldPos);
            rt.position = cardWorldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true;

        if (MapManager.Instance == null) {
            ReturnToHand();
            return;
        }

        Camera arenaCam = MapManager.Instance.arenaCamera.GetComponent<Camera>();
        Vector3 mousePosPixels = new Vector3(eventData.position.x, eventData.position.y, 10f);
        Vector2 mousePosWorld = arenaCam.ScreenToWorldPoint(mousePosPixels);
        RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero);

        CharacterEffects targetFound = null;
        if (hit.collider != null) {
            targetFound = hit.collider.GetComponentInParent<CharacterEffects>();
        }

        if (cardDisplay.cardInstance.data.isTargeted) {
            TargetingArrow.Instance.DeactivateArrow();
            HandView.Instance.SetDragLock(rt, false);

            if (targetFound != null && targetFound.gameObject != BattleManager.Instance.player.gameObject) {
                TryPlayCard(targetFound);
            }
            else {
                HandView.Instance.Layout();
            }
        }
        else {
            bool aboveThreshold = eventData.position.y > Screen.height * playThresholdPercentage;

            if (aboveThreshold) TryPlayCard(null);
            else ReturnToHand();
        }
    }

    private void TryPlayCard(CharacterEffects target) {
        // LEEMOS DE LA INSTANCIA: Ahora el costo depende de si está mejorada o no
        int cost = cardDisplay.cardInstance.GetManaCost();

        if (ManaManager.Instance.HasEnoughMana(cost)) {
            ManaManager.Instance.ConsumeMana(cost);

            if (cardDisplay.cardInstance.data.isTargeted) {
                HandView.Instance.RemoveCard(rt);
            }

            CharacterEffects player = BattleManager.Instance.player;

            // Recorremos los efectos, pero obtenemos sus valores de la INSTANCIA
            for (int i = 0; i < cardDisplay.cardInstance.data.effects.Count; i++) {
                var effect = cardDisplay.cardInstance.data.effects[i];
                int amount = (int)cardDisplay.cardInstance.GetEffectValue(i); // <-- AQUÍ SE APLICA LA MEJORA

                switch (effect.effectType) {
                    case CardEffectType.DealDamage:
                        if (cardDisplay.cardInstance.data.isAoE) {
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

            // Enviamos la INSTANCIA al descarte
            DiscardManager.Instance.AddCardToDiscard(cardDisplay.cardInstance);
            Destroy(gameObject);
        }
        else {
            if (!cardDisplay.cardInstance.data.isTargeted) ReturnToHand();
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