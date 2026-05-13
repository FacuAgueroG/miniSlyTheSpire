using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterStats : MonoBehaviour {
    [Header("Identidad")]
    public bool isPlayer; // Para saber a quién estamos atacando

    [Header("Estadísticas")]
    public int maxHealth = 50;
    public int currentHealth;
    public int currentBlock = 0; // El escudo (Armadura)

    [Header("Interfaz de Usuario")]
    public TextMeshProUGUI healthText;
    public Image healthBarFill; // Opcional: Si usas una imagen en modo "Filled"
    public GameObject blockGroup; // Un objeto que contenga el icono de escudo y su texto
    public TextMeshProUGUI blockText;

    private void Start() {
        currentHealth = maxHealth;
        UpdateUI();
    }

    // Método principal para recibir golpes
    public void TakeDamage(int damage) {
        // 1. El escudo absorbe el daño primero
        if (currentBlock > 0) {
            if (damage >= currentBlock) {
                damage -= currentBlock; // Rompe el escudo y sobra daño
                currentBlock = 0;
            }
            else {
                currentBlock -= damage; // El escudo aguanta el golpe
                damage = 0;
            }
        }

        // 2. El daño sobrante va a la vida
        currentHealth -= damage;

        // 3. Verificamos si murió
        if (currentHealth <= 0) {
            currentHealth = 0;
            Die();
        }

        UpdateUI();
    }

    // Para tus cartas de defensa
    public void AddBlock(int amount) {
        currentBlock += amount;
        UpdateUI();
    }

    // En Slay the Spire, el escudo se pierde al inicio de tu turno
    public void LoseAllBlock() {
        currentBlock = 0;
        UpdateUI();
    }

    private void Die() {
        Debug.Log($"{(isPlayer ? "Jugador" : "Enemigo")} ha muerto.");
        // Aquí luego le avisaremos al BattleManager que alguien ganó
    }

    public void UpdateUI() {
        if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";

        // Si tienes una barra visual (Image de tipo Filled)
        if (healthBarFill != null) healthBarFill.fillAmount = (float)currentHealth / maxHealth;

        // Mostrar/Ocultar el escudo visualmente solo si tenemos escudo
        if (blockGroup != null) {
            bool hasBlock = currentBlock > 0;
            blockGroup.SetActive(hasBlock);
            if (hasBlock && blockText != null) blockText.text = currentBlock.ToString();
        }
    }
}