using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterEffects : MonoBehaviour {
    private CharacterHealth health;

    [Header("Estados")]
    public int currentBlock = 0;
    public int attackBuff = 0;
    public int attackBuffDuration = 0;
    public Dictionary<EnemyAI, int> poisonSources = new Dictionary<EnemyAI, int>();

    [Header("Vinculo de Veneno")]
    public bool isPoisoningPlayer = false;
    public Sprite iconPoisonBond;
    public Transform bondContainer;

    [Header("UI Status")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public Sprite iconBlock, iconAttackBuff, iconPoison, iconAttackBuffTimer;

    [Header("Visuales (Asigna el que corresponda)")]
    public Image characterSprite;             // Para enemigos en CANVAS
    public SpriteRenderer characterSpriteWorld; // Para enemigos en ESCENA
    public Color turnColor = new Color(1f, 0.9f, 0.7f);

    private void Awake() { health = GetComponent<CharacterHealth>(); }

    // --- NUEVO MÉTODO SEGURO PARA EL COLOR ---
    public void SetVisualColor(Color color) {
        if (characterSprite != null) characterSprite.color = color;
        if (characterSpriteWorld != null) characterSpriteWorld.color = color;
    }

    public void SetTurnVisual(bool isItsTurn) {
        SetVisualColor(isItsTurn ? turnColor : Color.white);
    }

    // ... (El resto de tus métodos: OnTurnStarted, ProcessIncomingDamage, etc. se mantienen igual)
    // Asegúrate de mantener la lógica de veneno y daño que ya tenías.

    public void OnTurnStarted() { ProcessPoisonDamage(); UpdateStatusUI(); }
    public void OnTurnEnded() {
        if (attackBuffDuration > 0) {
            attackBuffDuration--;
            if (attackBuffDuration <= 0) { attackBuff = 0; attackBuffDuration = 0; }
        }
        UpdateStatusUI();
    }

    public int ProcessIncomingDamage(int damage) {
        if (damage <= 0) return 0;
        int unblockedDamage = damage;
        if (currentBlock > 0) {
            if (unblockedDamage >= currentBlock) { unblockedDamage -= currentBlock; currentBlock = 0; }
            else { currentBlock -= unblockedDamage; unblockedDamage = 0; }
        }
        if (unblockedDamage > 0) { health.TakeDamage(unblockedDamage); attackBuff = 0; }
        UpdateStatusUI();
        return unblockedDamage;
    }

    public void AddBlock(int amount) { currentBlock += amount; UpdateStatusUI(); }
    public void AddAttackBuff(int amount, int duration = -1) {
        attackBuff += amount;
        if (duration != -1) attackBuffDuration = duration;
        UpdateStatusUI();
    }
    public void ClearBlock() { currentBlock = 0; UpdateStatusUI(); }

    public void UpdateStatusUI() {
        if (statusContainer != null) foreach (Transform child in statusContainer) Destroy(child.gameObject);
        if (bondContainer != null) foreach (Transform child in bondContainer) Destroy(child.gameObject);
        if (currentBlock > 0) CreateIcon(iconBlock, currentBlock.ToString(), statusContainer);
        if (attackBuff > 0) {
            Sprite iconToShow = (attackBuffDuration > 0) ? iconAttackBuffTimer : iconAttackBuff;
            CreateIcon(iconToShow, attackBuff.ToString(), statusContainer);
        }
        int totalPoison = 0;
        foreach (var val in poisonSources.Values) totalPoison += val;
        if (totalPoison > 0) CreateIcon(iconPoison, totalPoison.ToString(), statusContainer);
        if (isPoisoningPlayer && iconPoisonBond != null && bondContainer != null) CreateIcon(iconPoisonBond, "", bondContainer);
    }

    private void CreateIcon(Sprite img, string val, Transform container) {
        if (container == null) return;
        GameObject icon = Instantiate(statusIconPrefab, container, false);
        icon.GetComponentInChildren<Image>().sprite = img;
        icon.GetComponentInChildren<TextMeshProUGUI>().text = val;
    }

    private void ProcessPoisonDamage() {
        int totalPoisonDamage = 0;
        List<EnemyAI> deadSources = new List<EnemyAI>();
        foreach (var kvp in poisonSources) {
            if (kvp.Key == null || kvp.Key.GetComponent<CharacterHealth>().currentHealth <= 0) deadSources.Add(kvp.Key);
            else totalPoisonDamage += kvp.Value;
        }
        foreach (var dead in deadSources) poisonSources.Remove(dead);
        if (totalPoisonDamage > 0) health.TakeDamage(totalPoisonDamage);
    }

    public void ApplyPoison(EnemyAI source, int amount) {
        if (poisonSources.ContainsKey(source)) poisonSources[source] += amount;
        else poisonSources[source] = amount;
        if (source.TryGetComponent<CharacterEffects>(out var e)) { e.isPoisoningPlayer = true; e.UpdateStatusUI(); }
        UpdateStatusUI();
    }
}