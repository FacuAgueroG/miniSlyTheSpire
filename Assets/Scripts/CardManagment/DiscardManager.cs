using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DiscardManager : MonoBehaviour, IPointerClickHandler {
    public static DiscardManager Instance;

    [Header("Discard Data")]
    public List<CardData> discardPile = new List<CardData>();

    [Header("Visualizador UI (Opcional)")]
    public GameObject discardViewerPanel;
    public Transform contentContainer;
    public CardDisplay cardDisplayPrefab;

    private void Awake() {
        // Aseguramos el Singleton
        Instance = this;
        if (discardViewerPanel != null) discardViewerPanel.SetActive(false);
    }

    public void AddCardToDiscard(CardData card) {
        discardPile.Add(card);
    }

    public void OnPointerClick(PointerEventData eventData) {
        ToggleDiscardView();
    }

    public void ToggleDiscardView() {
        if (discardViewerPanel == null) return;
        bool isOpening = !discardViewerPanel.activeSelf;

        if (isOpening) {
            if (DeckManager.Instance != null) DeckManager.Instance.CloseView();
            discardViewerPanel.SetActive(true);
            RefreshView();
        }
        else {
            CloseView();
        }

        // Le avisamos a la mano que se esconda o se muestre
        if (HandView.Instance != null) HandView.Instance.SetHiddenState(isOpening);
    }

    public void CloseView() {
        if (discardViewerPanel != null) {
            discardViewerPanel.SetActive(false);
            // Si nos cerramos externamente, volvemos a mostrar la mano
            if (HandView.Instance != null) HandView.Instance.SetHiddenState(false);
        }
    }

    private void RefreshView() {
        foreach (Transform child in contentContainer) {
            Destroy(child.gameObject);
        }

        foreach (CardData cardData in discardPile) {
            CardDisplay newCard = Instantiate(cardDisplayPrefab, contentContainer);
            newCard.SetupCard(cardData);
        }
    }
}