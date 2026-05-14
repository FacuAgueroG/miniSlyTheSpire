using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterEffects : MonoBehaviour {
    private CharacterHealth health;

    [Header("Estados")]
    public int currentBlock = 0;
    public int attackBuff = 0;

    // --- NUEVO: SISTEMA DE VENENO ---
    public Dictionary<EnemyAI, int> poisonSources = new Dictionary<EnemyAI, int>();

    [Header("Vinculo de Veneno (Enemigos)")]
    public bool isPoisoningPlayer = false;
    public Sprite iconPoisonBond;
    public Transform bondContainer; // <--- NUEVO: Arrastra aquí el GameObject de UI para el vínculo

    [Header("UI")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public Sprite iconBlock;
    public Sprite iconAttackBuff;
    public Sprite iconPoison;
    public Sprite iconAttackBuffTimer;

    [Header("Estados (Variables Internas)")]
    public int attackBuffDuration = 0;

    [Header("Visuales")]
    public Image characterSprite;
    public Color turnColor = new Color(1f, 0.9f, 0.7f);

    private void Awake() { health = GetComponent<CharacterHealth>(); }

    public void OnTurnStarted() {
        ProcessPoisonDamage();
        UpdateStatusUI();
    }

    public void OnTurnEnded() {
        if (attackBuffDuration > 0) {
            attackBuffDuration--;
            if (attackBuffDuration <= 0) {
                attackBuff = 0;
                attackBuffDuration = 0;
            }
        }
        UpdateStatusUI();
    }

    public void ApplyPoison(EnemyAI source, int amount) {
        if (poisonSources.ContainsKey(source)) {
            poisonSources[source] += amount;
        }
        else {
            poisonSources[source] = amount;
        }

        CharacterEffects enemyEffects = source.GetComponent<CharacterEffects>();
        if (enemyEffects != null) {
            enemyEffects.isPoisoningPlayer = true;
            enemyEffects.UpdateStatusUI();
        }
        UpdateStatusUI();
    }

    public void ClearPoisonFromSource(EnemyAI source) {
        if (poisonSources.ContainsKey(source)) {
            poisonSources.Remove(source);

            CharacterEffects enemyEffects = source.GetComponent<CharacterEffects>();
            if (enemyEffects != null) {
                enemyEffects.isPoisoningPlayer = false;
                enemyEffects.UpdateStatusUI();
            }
            UpdateStatusUI();
        }
    }

    private void ProcessPoisonDamage() {
        int totalPoisonDamage = 0;
        List<EnemyAI> deadSources = new List<EnemyAI>();

        foreach (var kvp in poisonSources) {
            if (kvp.Key == null || kvp.Key.GetComponent<CharacterHealth>().currentHealth <= 0) {
                deadSources.Add(kvp.Key);
            }
            else {
                totalPoisonDamage += kvp.Value;
            }
        }

        foreach (var dead in deadSources) poisonSources.Remove(dead);

        if (totalPoisonDamage > 0) {
            health.TakeDamage(totalPoisonDamage);
        }
    }

    public int ProcessIncomingDamage(int damage) {
        if (damage <= 0) return 0;
        int unblockedDamage = damage; // Calculamos cuánto daño puro pasa

        if (currentBlock > 0) {
            if (unblockedDamage >= currentBlock) {
                unblockedDamage -= currentBlock; // Rompe el escudo
                currentBlock = 0;
            }
            else {
                currentBlock -= unblockedDamage; // El escudo aguanta el golpe
                unblockedDamage = 0; // ESTO ES VITAL: El daño sobrante a la vida es 0
            }
        }

        if (unblockedDamage > 0) {
            health.TakeDamage(unblockedDamage);
            attackBuff = 0;
        }
        UpdateStatusUI();

        // DEBE retornar 0 si el escudo absorbió todo el golpe
        return unblockedDamage;
    }

    public void SetTurnVisual(bool isItsTurn) {
        if (characterSprite != null) characterSprite.color = isItsTurn ? turnColor : Color.white;
    }

    public void AddBlock(int amount) { currentBlock += amount; UpdateStatusUI(); }

    public void AddAttackBuff(int amount, int duration = -1) {
        attackBuff += amount;
        if (duration != -1) {
            attackBuffDuration = duration;
        }
        UpdateStatusUI();
    }

    public void ClearBlock() { currentBlock = 0; UpdateStatusUI(); }

    // --- ACTUALIZADO: Limpia ambos contenedores y separa la creación ---
    public void UpdateStatusUI() {
        // Limpiar contenedor de estados normales
        if (statusContainer != null) {
            foreach (Transform child in statusContainer) Destroy(child.gameObject);
        }

        // Limpiar contenedor de vínculos
        if (bondContainer != null) {
            foreach (Transform child in bondContainer) Destroy(child.gameObject);
        }

        // Crear iconos en el contenedor de estados
        if (currentBlock > 0) CreateIcon(iconBlock, currentBlock.ToString(), statusContainer);

        if (attackBuff > 0) {
            Sprite iconToShow = (attackBuffDuration > 0) ? iconAttackBuffTimer : iconAttackBuff;
            string valText = (attackBuffDuration > 0) ? $"{attackBuff}({attackBuffDuration})" : attackBuff.ToString();
            CreateIcon(iconToShow, valText, statusContainer);
        }

        int totalPoison = 0;
        foreach (var kvp in poisonSources) totalPoison += kvp.Value;
        if (totalPoison > 0) CreateIcon(iconPoison, totalPoison.ToString(), statusContainer);

        // --- NUEVO: Crear icono de VÍNCULO en su propio contenedor ---
        if (isPoisoningPlayer && iconPoisonBond != null && bondContainer != null) {
            CreateIcon(iconPoisonBond, "", bondContainer); // Se envía al bondContainer
        }
    }

    // Método de creación ahora recibe el contenedor como parámetro
    // Método de creación corregido para respetar el contenedor de UI
    private void CreateIcon(Sprite img, string val, Transform container) {
        if (container == null) return;

        // Instanciamos el prefab como hijo del contenedor
        GameObject icon = Instantiate(statusIconPrefab, container, false);

        RectTransform rect = icon.GetComponent<RectTransform>();
        if (rect != null) {
            // 1. Forzamos los anclajes para que sean "Stretch/Stretch" (0 a 1)
            rect.anchorMin = Vector2.zero; // Esquina inferior izquierda (0,0)
            rect.anchorMax = Vector2.one;  // Esquina superior derecha (1,1)

            // 2. Reseteamos los offsets para que no tenga "margen" respecto al padre
            // Esto hace que el tamaño sea exactamente igual al del contenedor violeta
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 3. Aseguramos que la escala sea 1 para evitar deformaciones
            rect.localScale = Vector3.one;
        }

        Image imgComp = icon.GetComponentInChildren<Image>();
        if (imgComp != null) {
            imgComp.sprite = img;
            // Opcional: Esto asegura que la imagen no se deforme si el cuadro violeta es rectangular
            imgComp.preserveAspect = true;
        }

        TextMeshProUGUI txtComp = icon.GetComponentInChildren<TextMeshProUGUI>();
        if (txtComp != null) txtComp.text = val;
    }
}