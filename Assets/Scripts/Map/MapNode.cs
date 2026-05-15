using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour {
    public enum NodeType { Arena, Reward, Boss }
    public NodeType type;
    public int sectionIndex; // Arena 1 = 1, Section 2 = 2, etc.

    private Button button;

    private void Awake() {
        button = GetComponent<Button>();
        // Conectamos el clic automáticamente al Manager
        button.onClick.AddListener(() => MapManager.Instance.OnNodeClicked(this));
    }

    public void SetInteractable(bool state) {
        if (button != null) button.interactable = state;
    }
}