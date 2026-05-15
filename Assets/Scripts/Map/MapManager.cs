using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapManager : MonoBehaviour {
    public static MapManager Instance;

    [Header("Referencias")]
    public RectTransform playerIcon; // El punto verde
    public GameObject rewardPanel;
    public CanvasGroup mapCanvasGroup; // Para bloquear clics mientras se mueve

    [Header("Configuración")]
    public float moveSpeed = 500f;
    public int currentSection = 1;

    private bool isMoving = false;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        UpdateNodesState();
    }

    public void OnNodeClicked(MapNode targetNode) {
        // Regla de Oro: Solo podemos movernos a la sección siguiente (n+1)
        if (isMoving || targetNode.sectionIndex != currentSection + 1) return;

        StartCoroutine(MovePlayerRoutine(targetNode));
    }

    private IEnumerator MovePlayerRoutine(MapNode targetNode) {
        isMoving = true;
        mapCanvasGroup.blocksRaycasts = false; // Bloquea clics durante el viaje

        // Obtenemos la posición local del nodo respecto al Content
        Vector2 targetPos = targetNode.GetComponent<RectTransform>().localPosition;
        // Si el nodo está dentro de una "Sección", sumamos la posición de la sección
        targetPos += (Vector2)targetNode.transform.parent.localPosition;

        // Desplazamiento suave (Lerp)
        while (Vector2.Distance(playerIcon.localPosition, targetPos) > 1f) {
            playerIcon.localPosition = Vector2.MoveTowards(playerIcon.localPosition, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        playerIcon.localPosition = targetPos; // Ajuste final exacto

        // Esperar 1 segundo para "asentarse"
        yield return new WaitForSeconds(1f);

        // Al llegar, actualizamos la sección actual
        currentSection = targetNode.sectionIndex;
        isMoving = false;
        mapCanvasGroup.blocksRaycasts = true;

        HandleNodeAction(targetNode);
        UpdateNodesState();
    }

    private void HandleNodeAction(MapNode node) {
        if (node.type == MapNode.NodeType.Reward) {
            rewardPanel.SetActive(true);
            // IMPORTANTE: Bloqueamos el mapa mientras el panel está abierto
            mapCanvasGroup.interactable = false;
        }
        else if (node.type == MapNode.NodeType.Arena) {
            Debug.Log("Preparate para pelear...");
            // Aquí podrías llamar a una función que cambie a la cámara de pelea
        }
    }

    // Activa solo los botones de la siguiente sección
    public void UpdateNodesState() {
        // Cambiamos FindObjectsOfType por FindObjectsByType
        // Usamos FindObjectsSortMode.None porque no nos importa el orden en que los encuentre
        MapNode[] allNodes = FindObjectsByType<MapNode>(FindObjectsSortMode.None);

        foreach (MapNode node in allNodes) {
            node.SetInteractable(node.sectionIndex == currentSection + 1);
        }
    }

    public void CloseRewardPanel() {
        rewardPanel.SetActive(false);

        // 1. Devolvemos la interacción al mapa
        mapCanvasGroup.interactable = true;
        mapCanvasGroup.blocksRaycasts = true;

        // 2. FORZAMOS la actualización de los nodos para habilitar el siguiente escalón
        UpdateNodesState();
    }
}