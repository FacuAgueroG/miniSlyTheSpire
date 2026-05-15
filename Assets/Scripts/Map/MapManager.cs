using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // Asegúrate de tener esto arriba

public class MapManager : MonoBehaviour {
    public static MapManager Instance;

    [Header("Control de Vistas (Cámaras)")]
    public GameObject arenaCamera;
    public GameObject mapCamera;
    public GameObject mapCanvasRoot; // El Canvas padre que contiene el ScrollView

    [Header("Referencias UI")]
    public RectTransform playerIcon;
    public GameObject rewardPanel;
    public CanvasGroup mapCanvasGroup; // Recuerda, este va en el Scroll View


    [Header("Configuración del Draft de Cartas")]
    public GameObject draftPanel;          // El nuevo panel que creaste
    public GameObject cardDraftPrefab;     // El prefab de la carta (SIN CardDrag)
    public Transform[] draftSlots;         // Los 3 objetos que sirven de contenedores
    public List<CardData> cardPool = new List<CardData>(); // Arrastra aquí las 4 cartas base

    [Header("Configuración")]
    public float moveSpeed = 500f;
    public int currentSection = 1;

    private bool isMoving = false;

    private void Awake() {
        Instance = this;
    }

    private void Start() {

        // ESTA LÍNEA ES CLAVE:

        // Obliga al juego a esconder el mapa y mostrar la arena apenas arranca.

        GoToArena();



        UpdateNodesState();

    }

    // --- MÉTODOS DE TRANSICIÓN DE CÁMARA ---
    public void GoToMap() {
        arenaCamera.SetActive(false);
        mapCamera.SetActive(true);
        mapCanvasRoot.SetActive(true);
        UpdateNodesState(); // Aseguramos que los botones estén listos
    }

    public void GoToArena() {
        mapCamera.SetActive(false);
        mapCanvasRoot.SetActive(false);
        arenaCamera.SetActive(true);
    }
    // ---------------------------------------

    public void OnNodeClicked(MapNode targetNode) {
        if (isMoving || targetNode.sectionIndex != currentSection + 1) return;
        StartCoroutine(MovePlayerRoutine(targetNode));
    }

    private IEnumerator MovePlayerRoutine(MapNode targetNode) {
        isMoving = true;
        mapCanvasGroup.blocksRaycasts = false;

        Vector2 targetPos = targetNode.GetComponent<RectTransform>().localPosition;
        targetPos += (Vector2)targetNode.transform.parent.localPosition;

        while (Vector2.Distance(playerIcon.localPosition, targetPos) > 1f) {
            playerIcon.localPosition = Vector2.MoveTowards(playerIcon.localPosition, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        playerIcon.localPosition = targetPos;
        yield return new WaitForSeconds(1f);

        currentSection = targetNode.sectionIndex;

        mapCanvasGroup.blocksRaycasts = true;
        isMoving = false;

        HandleNodeAction(targetNode);
    }

    private void HandleNodeAction(MapNode node) {
        if (node.type == MapNode.NodeType.Reward) {
            rewardPanel.SetActive(true);
            mapCanvasGroup.interactable = false; // Bloquea clics en el mapa
        }
        else if (node.type == MapNode.NodeType.Arena) {
            Debug.Log("Generando nueva arena...");
            StartCoroutine(PrepareArenaTransition());
        }
    }

    private IEnumerator PrepareArenaTransition() {
        // 1. Congelamos el mapa para que no toque nada más
        mapCanvasGroup.interactable = false;

        // 2. Generamos la nueva arena por detrás de escena
        EncounterDirector.Instance.GenerateNextArena();

        // 3. Esperamos 1 segundo de "tensión/preparación"
        yield return new WaitForSeconds(1f);

        // 4. Volvemos a encender el mapa para cuando regrese, pero cambiamos de cámara
        mapCanvasGroup.interactable = true;
        GoToArena();
    }

    public void UpdateNodesState() {
        MapNode[] allNodes = FindObjectsByType<MapNode>(FindObjectsSortMode.None);
        foreach (MapNode node in allNodes) {
            node.SetInteractable(node.sectionIndex == currentSection + 1);
        }
    }

    public void CloseRewardPanel() {
        rewardPanel.SetActive(false);
        mapCanvasGroup.interactable = true;
        UpdateNodesState();
    }

    [Header("Balance de Recompensas")]
    public int healAmount = 10; // Aparecerá en el Inspector para que lo edites a gusto

    // Esta es la función que debés conectar al botón de curación en el UI
    public void SelectHealReward() {
        // 1. Buscamos al jugador a través del BattleManager
        if (BattleManager.Instance != null && BattleManager.Instance.player != null) {
            CharacterHealth playerHealth = BattleManager.Instance.player.GetComponent<CharacterHealth>();

            if (playerHealth != null) {
                playerHealth.Heal(healAmount);
                Debug.Log("Recompensa: Player curado +" + healAmount);
            }
        }

        // 2. Cerramos el panel y volvemos al mapa como pediste
        CloseRewardPanel();
    }

    public void OpenCardDraft() {
        rewardPanel.SetActive(false); // Cerramos el panel de los 3 botones
        draftPanel.SetActive(true);   // Abrimos el de las 3 cartas

        GenerateDraftOptions();
    }

    private void GenerateDraftOptions() {
        // Limpiamos los slots por si había algo antes
        foreach (Transform slot in draftSlots) {
            foreach (Transform child in slot) Destroy(child.gameObject);
        }

        // Creamos una copia de la lista para no alterar la original y barajamos
        List<CardData> tempPool = new List<CardData>(cardPool);

        for (int i = 0; i < draftSlots.Length; i++) {
            if (tempPool.Count == 0) break;

            // Elegimos una al azar de la copia
            int randomIndex = Random.Range(0, tempPool.Count);
            CardData selectedData = tempPool[randomIndex];
            tempPool.RemoveAt(randomIndex); // La quitamos para no repetir

            // Instanciamos en el slot
            GameObject newCardObj = Instantiate(cardDraftPrefab, draftSlots[i]);

            // Ajustamos para que haga STRETCH (se estire al tamaño del slot)
            RectTransform rt = newCardObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Configuramos los datos visuales
            CardDisplay display = newCardObj.GetComponent<CardDisplay>();
            if (display != null) display.SetupCard(selectedData);

            // Configuramos el botón para que al hacer clic se agregue al deck
            Button btn = newCardObj.GetComponent<Button>();
            if (btn != null) {
                btn.onClick.AddListener(() => OnCardDraftSelected(selectedData));
            }
        }
    }

    private void OnCardDraftSelected(CardData pickedCard) {
        // 1. Agregamos la carta al mazo persistente
        if (DeckManager.Instance != null) {
            DeckManager.Instance.deck.Add(pickedCard);
            Debug.Log("Mazo: Agregada carta " + pickedCard.cardName);
        }

        // 2. Cerramos todo y volvemos al mapa
        draftPanel.SetActive(false);
        CloseRewardPanel(); // Esta función ya limpia el estado del mapa
    }
}