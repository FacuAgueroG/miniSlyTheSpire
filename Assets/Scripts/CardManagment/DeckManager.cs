using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckManager : MonoBehaviour, IPointerClickHandler {
    public static DeckManager Instance;

    [Header("Deck Data")]
    public List<CardData> deck = new List<CardData>();

    [Header("Configuración de Robo")]
    public HandView playerHand;
    [Tooltip("Cartas al empezar la batalla (Mano Inicial)")]
    public int initialDrawCount = 5;
    [Tooltip("Cartas que robas cada vez que vuelve a ser tu turno")]
    public int cardsPerTurn = 1; // <--- ESTA ES LA VARIABLE QUE FALTABA

    [Header("Visualizador UI")]
    public GameObject deckViewerPanel;
    public Transform contentContainer;
    public CardDisplay cardDisplayPrefab;

    private void Awake() { Instance = this; }

    private void Start() {
        ShuffleDeck();
        if (deckViewerPanel != null) deckViewerPanel.SetActive(false);

        // REGLA: Robamos la mano inicial al empezar
        DrawCards(initialDrawCount);
    }

    public void DrawCards(int amount) {
        for (int i = 0; i < amount; i++) {
            // REGLA: Si el mazo se vacía, reciclamos el descarte
            if (deck.Count == 0) {
                if (DiscardManager.Instance.discardPile.Count > 0) {
                    RefillDeckFromDiscard();
                }
                else {
                    Debug.LogWarning("¡Sin cartas en mazo ni descarte!");
                    break;
                }
            }

            if (playerHand.IsFull) break;

            CardData drawnCardData = deck[0];
            deck.RemoveAt(0);

            CardDisplay newCard = Instantiate(cardDisplayPrefab);
            newCard.SetupCard(drawnCardData);
            playerHand.AddCard(newCard.GetComponent<RectTransform>());
        }
    }

    private void RefillDeckFromDiscard() {
        Debug.Log("Reciclando descarte y barajando...");
        deck.AddRange(DiscardManager.Instance.discardPile);
        DiscardManager.Instance.discardPile.Clear();
        ShuffleDeck();
    }

    public void ShuffleDeck() {
        for (int i = 0; i < deck.Count; i++) {
            int randomIndex = Random.Range(i, deck.Count);
            CardData temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // Métodos de UI (Iguales a los anteriores)
    public void OnPointerClick(PointerEventData eventData) => ToggleDeckView();
    public void ToggleDeckView() {
        if (deckViewerPanel == null) return;
        bool isOpening = !deckViewerPanel.activeSelf;
        if (isOpening) {
            if (DiscardManager.Instance != null) DiscardManager.Instance.CloseView();
            deckViewerPanel.SetActive(true);
            RefreshDeckView();
        }
        else CloseView();
        if (HandView.Instance != null) HandView.Instance.SetHiddenState(isOpening);
    }
    public void CloseView() {
        if (deckViewerPanel != null) {
            deckViewerPanel.SetActive(false);
            if (HandView.Instance != null) HandView.Instance.SetHiddenState(false);
        }
    }
    private void RefreshDeckView() {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        List<CardData> sortedDeck = deck.OrderBy(card => card.cardName).ToList();
        foreach (CardData cardData in sortedDeck) {
            CardDisplay newCard = Instantiate(cardDisplayPrefab, contentContainer);
            newCard.SetupCard(cardData);
        }
    }
}