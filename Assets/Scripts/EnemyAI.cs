using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAI : MonoBehaviour {
    public enum EnemyAction { Attack, Defend }
    public EnemyAction nextAction;
    public int attackDamage = 6;
    public int defendAmount = 5;

    public Image intentIcon;
    public TextMeshProUGUI intentValueText;
    public Sprite attackSprite, defendSprite;

    [Header("UI de Orden")]
    public TextMeshProUGUI orderText; // El texto que mostrará 1, 2 o 3
    public GameObject orderVisualGroup; // Opcional: El objeto que contiene al texto para prenderlo/apagarlo

    public void DecideNextMove() {
        nextAction = (Random.value < 0.6f) ? EnemyAction.Attack : EnemyAction.Defend;
        UpdateIntentUI();
    }

    public void UpdateIntentUI() {
        if (intentIcon == null) return;
        intentIcon.sprite = (nextAction == EnemyAction.Attack) ? attackSprite : defendSprite;
        int val = (nextAction == EnemyAction.Attack) ? attackDamage : defendAmount;
        if (intentValueText != null) intentValueText.text = val.ToString();
    }

    public void PerformTurnAction(CharacterEffects playerTarget) {
        if (nextAction == EnemyAction.Attack) {
            playerTarget.ProcessIncomingDamage(attackDamage);
        }
        else {
            GetComponent<CharacterEffects>().AddBlock(defendAmount);
        }
    }
}