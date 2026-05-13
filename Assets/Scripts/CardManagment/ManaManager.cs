using UnityEngine;
using TMPro;

public class ManaManager : MonoBehaviour {
    // Singleton: Nos permite acceder al maná desde cualquier script con ManaManager.Instance
    public static ManaManager Instance;

    [Header("Estadísticas de Maná")]
    public int maxMana = 3;
    public int currentMana;

    [Header("Referencias UI")]
    public TextMeshProUGUI manaText;

    private void Awake() {
        // CORRECCIÓN 1: Quitamos el "if (Instance == null)". 
        // Ahora forzamos a que ESTE objeto sobreescriba a cualquier "fantasma" en la escena.
        Instance = this;
        currentMana = maxMana;
    }

    private void Start() {
        // CORRECCIÓN 2: En lugar de solo actualizar la UI, llamamos a ResetMana() 
        // para asegurarnos de que la lógica y la interfaz arranquen al máximo (3/3).
        ResetMana();
    }

    // Se llamará al inicio de cada ronda
    public void ResetMana() {
        currentMana = maxMana;
        UpdateUI();
    }

    // Para tus futuras mejoras permanentes
    public void IncreaseMaxMana(int amount) {
        maxMana += amount;
        UpdateUI();
    }

    public bool HasEnoughMana(int cost) {
        return currentMana >= cost;
    }

    public void ConsumeMana(int cost) {
        currentMana -= cost;
        UpdateUI();
    }

    private void UpdateUI() {
        if (manaText != null) {
            manaText.text = $"{currentMana}/{maxMana}";
        }
    }
}