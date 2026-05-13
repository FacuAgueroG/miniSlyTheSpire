using UnityEngine;
using TMPro;

public class CharacterHealth : MonoBehaviour {
    [Header("Estadísticas")]
    public int maxHealth = 50;
    public int currentHealth { get; private set; } // Protegido para que nadie lo modifique directo

    [Header("UI")]
    public TextMeshProUGUI healthText;

    // En CharacterHealth.cs
    private void Awake() {
        currentHealth = maxHealth; // Esto garantiza que BattleManager vea la vida llena al empezar.
    }

    private void Start() {
        //currentHealth = maxHealth;
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
        // Por ahora, simplemente desaparecemos al enemigo para que no estorbe
        gameObject.SetActive(false);

        // Si era un enemigo, avisamos al Manager que su turno ya no cuenta
        // (Esto lo limpiaremos más adelante con un sistema de eventos)
    }

    private void UpdateUI() {
        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
    }
}