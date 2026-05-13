using UnityEngine;
using TMPro;

public class CharacterHealth : MonoBehaviour {
    [Header("Estadísticas")]
    public int maxHealth = 50;
    public int currentHealth { get; private set; } // Protegido para que nadie lo modifique directo

    [Header("UI")]
    public TextMeshProUGUI healthText;

    private void Start() {
        currentHealth = maxHealth;
        UpdateUI();
    }

    // Solo recibe daño puro (después de pasar por los escudos/efectos)
    public void TakeDamage(int amount) {
        currentHealth -= amount;

        if (currentHealth <= 0) {
            currentHealth = 0;
            Die();
        }
        UpdateUI();
    }

    public void Heal(int amount) {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    private void Die() {
        Debug.Log($"{gameObject.name} ha muerto.");
        // Futuro: Avisarle al BattleManager
    }

    private void UpdateUI() {
        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
    }
}