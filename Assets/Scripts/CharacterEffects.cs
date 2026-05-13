using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterEffects : MonoBehaviour {
    private CharacterHealth health;

    [Header("Estados")]
    public int currentBlock = 0;
    public int attackBuff = 0;

    [Header("UI")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public Sprite iconBlock;
    public Sprite iconAttackBuff; // Arrastrá el icono de la espadita acá

    [Header("Visuales")]
    public Image characterSprite;
    public Color turnColor = new Color(1f, 0.9f, 0.7f);

    private void Awake() { health = GetComponent<CharacterHealth>(); }

    // Colocalo debajo de Awake o junto a los otros métodos públicos
    public void OnTurnStarted() {
        // Por ahora solo refrescamos la UI al empezar el turno
        UpdateStatusUI();
    }

    // ESTE ES EL MÉTODO QUE TE DABA ERROR:
    public void SetTurnVisual(bool isItsTurn) {
        if (characterSprite != null) {
            characterSprite.color = isItsTurn ? turnColor : Color.white;
        }
    }

    public void ProcessIncomingDamage(int damage) {
        if (damage <= 0) return;

        if (currentBlock > 0) {
            if (damage >= currentBlock) {
                damage -= currentBlock;
                currentBlock = 0;
            }
            else {
                currentBlock -= damage;
                damage = 0;
            }
        }

        if (damage > 0) {
            health.TakeDamage(damage);
            // Si el daño llegó a la vida, perdemos el buff
            attackBuff = 0;
        }
        UpdateStatusUI();
    }

    public void AddBlock(int amount) { currentBlock += amount; UpdateStatusUI(); }
    public void AddAttackBuff(int amount) { attackBuff += amount; UpdateStatusUI(); }
    public void ClearBlock() { currentBlock = 0; UpdateStatusUI(); }

    public void UpdateStatusUI() {
        if (statusContainer == null) return;
        foreach (Transform child in statusContainer) Destroy(child.gameObject);

        if (currentBlock > 0) CreateIcon(iconBlock, currentBlock);
        if (attackBuff > 0) CreateIcon(iconAttackBuff, attackBuff);
    }

    private void CreateIcon(Sprite img, int val) {
        GameObject icon = Instantiate(statusIconPrefab, statusContainer);
        icon.transform.localScale = Vector3.one;
        icon.GetComponentInChildren<Image>().sprite = img;
        icon.GetComponentInChildren<TextMeshProUGUI>().text = val.ToString();
    }
}