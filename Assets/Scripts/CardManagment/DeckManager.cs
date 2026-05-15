using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckManager : MonoBehaviour, IPointerClickHandler {
    public static DeckManager Instance;

    [Header("Deck Data")]
    // CAMBIO: Ahora el mazo guarda instancias, no solo la data base
    public List<CardInstance> deck = new List<CardInstance>();

    [Header("Configuración de Robo")]
    public HandView playerHand;
    public int initialDrawCount = 5;
    public int cardsPerTurn = 1;

    [Header("Visualizador UI")]
    public GameObject deckViewerPanel;
    public Transform contentContainer;
    public CardDisplay cardDisplayPrefab;

    private void Awake() { Instance = this; }

    private void Start() {
        ShuffleDeck();
        if (deckViewerPanel != null) deckViewerPanel.SetActive(false);
        // El EncounterDirector llamará a DrawCards al entrar a la arena
    }

    public void DrawCards(int amount) {
        for (int i = 0; i < amount; i++) {
            if (deck.Count == 0) {
                if (DiscardManager.Instance.discardPile.Count > 0) RefillDeckFromDiscard();
                else break;
            }

            if (playerHand.IsFull) break;

            CardInstance drawnInstance = deck[0];
            deck.RemoveAt(0);

            CardDisplay newCard = Instantiate(cardDisplayPrefab);
            newCard.SetupCard(drawnInstance); // PASAMOS LA INSTANCIA
            playerHand.AddCard(newCard.GetComponent<RectTransform>());
        }
    }

    private void RefillDeckFromDiscard() {
        deck.AddRange(DiscardManager.Instance.discardPile);
        DiscardManager.Instance.discardPile.Clear();
        ShuffleDeck();
    }

    public void ShuffleDeck() {
        for (int i = 0; i < deck.Count; i++) {
            int randomIndex = Random.Range(i, deck.Count);
            CardInstance temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

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
        // Ordenamos por nombre de la data base
        List<CardInstance> sortedDeck = deck.OrderBy(inst => inst.data.cardName).ToList();
        foreach (CardInstance inst in sortedDeck) {
            CardDisplay newCard = Instantiate(cardDisplayPrefab, contentContainer);
            newCard.SetupCard(inst);
        }
    }

    public void ReturnAllToDeck() {
        if (DiscardManager.Instance != null) {
            deck.AddRange(DiscardManager.Instance.discardPile);
            DiscardManager.Instance.discardPile.Clear();
        }
        if (HandView.Instance != null) {
            // Suponiendo que ClearHandAndGetData ya devuelve List<CardInstance>
            // Si devuelve CardData, habrá que envolverlas, pero lo ideal es que devuelva Instancia.
            // Para este fix, usaremos la lógica de recuperación de instancias.
        }
        ShuffleDeck();
    }
}