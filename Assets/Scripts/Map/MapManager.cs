using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MapManager : MonoBehaviour {
    public static MapManager Instance;

    [Header("Control de Vistas (Cámaras)")]
    public GameObject arenaCamera;
    public GameObject mapCamera;
    public GameObject mapCanvasRoot;

    [Header("Referencias UI")]
    public RectTransform playerIcon;
    public GameObject rewardPanel;
    public CanvasGroup mapCanvasGroup;


    [Header("Configuración del Draft de Cartas")]
    public GameObject draftPanel;
    public GameObject cardDraftPrefab;
    public Transform[] draftSlots;
    public List<CardData> cardPool = new List<CardData>();

    [Header("Configuración de Mejoras")]
    public GameObject upgradePanel;         // El panel con el ScrollView
    public Transform upgradeContent;        // El 'Content' del ScrollView
    public GameObject upgradeCardPrefab;    // Prefab de carta (con el script CardUpgradeButton)

    [Header("Configuración")]
    public float moveSpeed = 500f;
    public int currentSection = 1;

    private bool isMoving = false;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        GoToArena();
        UpdateNodesState();
    }

    public void GoToMap() {
        arenaCamera.SetActive(false);
        mapCamera.SetActive(true);
        mapCanvasRoot.SetActive(true);
        UpdateNodesState();
    }

    public void GoToArena() {
        mapCamera.SetActive(false);
        mapCanvasRoot.SetActive(false);
        arenaCamera.SetActive(true);
    }

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
            mapCanvasGroup.interactable = false;
        }
        else if (node.type == MapNode.NodeType.Arena) {
            StartCoroutine(PrepareArenaTransition());
        }
    }

    private IEnumerator PrepareArenaTransition() {
        mapCanvasGroup.interactable = false;
        EncounterDirector.Instance.GenerateNextArena();

        yield return new WaitForSeconds(1f);

        mapCanvasGroup.interactable = true;
        GoToArena();

        if (BattleManager.Instance != null) {
            BattleManager.Instance.StartPlayerTurn(true);
        }
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
    public int healAmount = 10;

    public void SelectHealReward() {
        if (BattleManager.Instance != null && BattleManager.Instance.player != null) {
            CharacterHealth playerHealth = BattleManager.Instance.player.GetComponent<CharacterHealth>();

            if (playerHealth != null) {
                playerHealth.Heal(healAmount);
                Debug.Log("Recompensa: Player curado +" + healAmount);
            }
        }
        CloseRewardPanel();
    }

    public void OpenCardDraft() {
        rewardPanel.SetActive(false);
        draftPanel.SetActive(true);
        GenerateDraftOptions();
    }

    private void GenerateDraftOptions() {
        foreach (Transform slot in draftSlots) {
            foreach (Transform child in slot) Destroy(child.gameObject);
        }

        List<CardData> tempPool = new List<CardData>(cardPool);

        for (int i = 0; i < draftSlots.Length; i++) {
            if (tempPool.Count == 0) break;

            int randomIndex = Random.Range(0, tempPool.Count);
            CardData selectedData = tempPool[randomIndex];
            tempPool.RemoveAt(randomIndex);

            GameObject newCardObj = Instantiate(cardDraftPrefab, draftSlots[i]);

            RectTransform rt = newCardObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            CardDisplay display = newCardObj.GetComponent<CardDisplay>();

            // CORRECCIÓN VITAL: Creamos la instancia ANTES de mostrarla
            CardInstance draftInstance = new CardInstance(selectedData);
            if (display != null) display.SetupCard(draftInstance);

            Button btn = newCardObj.GetComponent<Button>();
            if (btn != null) {
                // Le pasamos la INSTANCIA a la función de selección
                btn.onClick.AddListener(() => OnCardDraftSelected(draftInstance));
            }
        }
    }

    // CORRECCIÓN: Ahora recibe la Instancia en lugar de la Data
    private void OnCardDraftSelected(CardInstance pickedCardInstance) {
        if (DeckManager.Instance != null) {
            DeckManager.Instance.deck.Add(pickedCardInstance);
            Debug.Log("Mazo: Agregada carta " + pickedCardInstance.data.cardName);
        }

        draftPanel.SetActive(false);
        CloseRewardPanel();
    }

    // 1. Se conecta al botón "Mejorar Carta" del RewardPanel
    public void OpenUpgradeMenu() {
        rewardPanel.SetActive(false);
        upgradePanel.SetActive(true);

        // Limpiamos el contenido anterior
        foreach (Transform child in upgradeContent) Destroy(child.gameObject);

        // Llenamos con el mazo completo (que ya está reseteado por la Fase 1)
        foreach (CardInstance inst in DeckManager.Instance.deck) {
            GameObject newObj = Instantiate(upgradeCardPrefab, upgradeContent);

            // Si la carta ya está mejorada, podrías opacarla o filtrarla
            CardUpgradeButton upgradeBtn = newObj.GetComponent<CardUpgradeButton>();
            if (upgradeBtn != null) upgradeBtn.Setup(inst);

            // Si ya está mejorada, la ponemos un poco transparente para que se note
            if (inst.isUpgraded) {
                newObj.GetComponent<CanvasGroup>().alpha = 0.5f;
            }
        }
    }

    // 2. Se llama desde el CardUpgradeButton al hacer clic
    public void SelectUpgrade(CardInstance instanceToUpgrade) {
        // ¡LA MEJORA REAL!
        instanceToUpgrade.isUpgraded = true;

        Debug.Log("Mazo: Carta " + instanceToUpgrade.data.cardName + " mejorada permanentemente.");

        // Cerramos el panel y volvemos al mapa
        upgradePanel.SetActive(false);
        CloseRewardPanel();
    }
}