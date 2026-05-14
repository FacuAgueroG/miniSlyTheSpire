using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    // En CharacterHealth.cs (opcional para limpiar tu desorden del Canvas)
    public List<GameObject> extraVisuals; // Arrastra aquí la barra de vida, iconos, etc que estén sueltos

    private void Die() {
        foreach (var obj in extraVisuals) obj.SetActive(false);
        gameObject.SetActive(false);
    }

    private void UpdateUI() {
        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
    }
}