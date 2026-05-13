using UnityEngine;
using UnityEngine.EventSystems;

// Estas interfaces de EventSystems son las que detectan el puntero nativo de Unity
public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private HandView hand;
    private RectTransform rt;

    // Tu HandView llama a este método al agregar la carta
    public void SetHand(HandView h) {
        hand = h;
        rt = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (hand != null && rt != null)
            hand.SetHovered(rt);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (hand != null && rt != null)
            hand.ClearHovered(rt);
    }
}