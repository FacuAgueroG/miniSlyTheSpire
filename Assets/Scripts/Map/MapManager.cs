using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
}