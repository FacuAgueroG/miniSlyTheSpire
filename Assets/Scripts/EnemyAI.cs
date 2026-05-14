using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAI : MonoBehaviour {
    // --- Añade a la Enum ---
    public enum EnemyAction { Attack, Defend, PoisonAttack, BuffAllies }
    public EnemyAction nextAction;

    [Header("Estadísticas Base")]
    public int attackDamage = 6;
    public int defendAmount = 5;

    [Header("Configuración de Veneno")]
    public bool canPoison = false; // Tilda esto en el Prefab del nuevo enemigo
    public int poisonDamage = 3;   // Cuánto veneno aplica
    public int poisonAttackHit = 4; // El daño directo que hace el golpe envenenado

    [Header("UI de Intención")]
    public Image intentIcon;
    public TextMeshProUGUI intentValueText;
    public Sprite attackSprite, defendSprite, poisonSprite; // Arrastra el icono de veneno aquí

    [Header("UI de Orden")]
    public TextMeshProUGUI orderText;
    public GameObject orderVisualGroup;

    [Header("Configuración de Buff")]
    public bool canBuffAllies = false; // Check en el inspector
    public int buffAmount = 2;         // Cuánto daño extra da
    public int buffTurnDuration = 2;   // Cuántos turnos dura

    [Header("Iconos de Buff")]
    public Sprite buffIntentSprite;    // Icono que sale sobre su cabeza al decidir buffear

    public void DecideNextMove() {
        float roll = Random.value;

        if (canBuffAllies && roll > 0.7f) { // 30% de chance de buffear si puede
            nextAction = EnemyAction.BuffAllies;
        }
        else if (canPoison && roll > 0.4f) {
            nextAction = EnemyAction.PoisonAttack;
        }
        else {
            nextAction = (Random.value < 0.5f) ? EnemyAction.Attack : EnemyAction.Defend;
        }
        UpdateIntentUI();
    }

    public void UpdateIntentUI() {
        if (intentIcon != null) {
            if (nextAction == EnemyAction.Attack) intentIcon.sprite = attackSprite;
            else if (nextAction == EnemyAction.Defend) intentIcon.sprite = defendSprite;
            else if (nextAction == EnemyAction.PoisonAttack) intentIcon.sprite = poisonSprite;
        }

        if (intentValueText != null) {
            int val = 0;
            if (nextAction == EnemyAction.Attack) val = attackDamage;
            else if (nextAction == EnemyAction.Defend) val = defendAmount;
            else if (nextAction == EnemyAction.PoisonAttack) val = poisonAttackHit; // Muestra el daño del golpe

            intentValueText.text = val.ToString();
        }

        if (nextAction == EnemyAction.BuffAllies) {
            intentIcon.sprite = buffIntentSprite;
            intentValueText.text = ""; // No mostramos daño, solo el icono de flechita arriba
        }
    }

    // --- En PerformTurnAction ---
    public void PerformTurnAction(CharacterEffects playerTarget) {
        switch (nextAction) {
            case EnemyAction.Attack:
                playerTarget.ProcessIncomingDamage(attackDamage + GetComponent<CharacterEffects>().attackBuff);
                break;

            case EnemyAction.Defend:
                GetComponent<CharacterEffects>().AddBlock(defendAmount);
                break;

            case EnemyAction.PoisonAttack:
                // 1. Calculamos el daño total (base + buff)
                int damageToDeal = poisonAttackHit + GetComponent<CharacterEffects>().attackBuff;

                // 2. El player procesa el daño. Esta función devuelve el daño que SÍ llegó a la vida.
                int unblocked = playerTarget.ProcessIncomingDamage(damageToDeal);

                // 3. SOLO si el daño traspasó el escudo (unblocked > 0), inyectamos el veneno
                if (unblocked > 0) {
                    playerTarget.ApplyPoison(this, poisonDamage);
                    Debug.Log("¡Veneno aplicado!");
                }
                else {
                    Debug.Log("El escudo bloqueó el veneno.");
                }
                break;

            case EnemyAction.BuffAllies:
                foreach (EnemyAI ally in BattleManager.Instance.allEnemies) {
                    if (ally != null && ally != this && ally.GetComponent<CharacterHealth>().currentHealth > 0) {
                        ally.GetComponent<CharacterEffects>().AddAttackBuff(buffAmount, buffTurnDuration);
                    }
                }
                break;
        }
    }
}