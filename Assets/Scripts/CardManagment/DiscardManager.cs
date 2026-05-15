using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DiscardManager : MonoBehaviour, IPointerClickHandler {
    public static DiscardManager Instance;

    [Header("Discard Data")]
    public List<CardInstance> discardPile = new List<CardInstance>();

    [Header("Visualizador UI")]
    public GameObject discardViewerPanel;
    public Transform contentContainer;
    public CardDisplay cardDisplayPrefab;

    private void Awake() { Instance = this; }

    public void AddCardToDiscard(CardInstance instance) {
        discardPile.Add(instance);
    }

    public void OnPointerClick(PointerEventData eventData) => ToggleDiscardView();

    public void ToggleDiscardView() {
        if (discardViewerPanel == null) return;
        bool isOpening = !discardViewerPanel.activeSelf;
        if (isOpening) {
            if (DeckManager.Instance != null) DeckManager.Instance.CloseView();
            discardViewerPanel.SetActive(true);
            RefreshView();
        }
        else CloseView();
        if (HandView.Instance != null) HandView.Instance.SetHiddenState(isOpening);
    }

    public void CloseView() {
        if (discardViewerPanel != null) {
            discardViewerPanel.SetActive(false);
            if (HandView.Instance != null) HandView.Instance.SetHiddenState(false);
        }
    }

    private void RefreshView() {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        foreach (CardInstance inst in discardPile) {
            CardDisplay newCard = Instantiate(cardDisplayPrefab, contentContainer);
            newCard.SetupCard(inst);
        }
    }
}