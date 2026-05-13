using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAI : MonoBehaviour {
    public enum EnemyAction { Attack, Defend }
    public EnemyAction nextAction;
    public int attackDamage = 6;
    public int defendAmount = 5;

    [Header("UI de Intención")]
    public Image intentIcon;
    public TextMeshProUGUI intentValueText;
    public Sprite attackSprite, defendSprite;

    [Header("UI de Orden")]
    public TextMeshProUGUI orderText;
    public GameObject orderVisualGroup;

    // Se llama desde el BattleManager al inicio de la ronda
    public void DecideNextMove() {
        nextAction = (Random.value < 0.6f) ? EnemyAction.Attack : EnemyAction.Defend;
        UpdateIntentUI();
    }

    public void UpdateIntentUI() {
        if (intentIcon != null) {
            intentIcon.sprite = (nextAction == EnemyAction.Attack) ? attackSprite : defendSprite;
        }

        if (intentValueText != null) {
            int val = (nextAction == EnemyAction.Attack) ? attackDamage : defendAmount;
            intentValueText.text = val.ToString();
        }
    }

    public void PerformTurnAction(CharacterEffects playerTarget) {
        if (nextAction == EnemyAction.Attack) {
            playerTarget.ProcessIncomingDamage(attackDamage);
        }
        else {
            GetComponent<CharacterEffects>().AddBlock(defendAmount);
        }

        // Opcional: después de actuar, ocultamos el icono de intención hasta la próxima ronda
        // intentIcon.gameObject.SetActive(false); 
    }
}