using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic; // IMPORTANTE para el Diccionario

public class CharacterEffects : MonoBehaviour {
    private CharacterHealth health;

    [Header("Estados")]
    public int currentBlock = 0;
    public int attackBuff = 0;

    // --- NUEVO: SISTEMA DE VENENO ---
    // Diccionario que guarda: ¿Qué enemigo me envenenó? -> ¿Cuántas cargas (stacks)?
    public Dictionary<EnemyAI, int> poisonSources = new Dictionary<EnemyAI, int>();

    [Header("UI")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public Sprite iconBlock;
    public Sprite iconAttackBuff;
    public Sprite iconPoison; // ¡Arrastra un icono de veneno/gota verde aquí!
    public Sprite iconAttackBuffTimer; // Icono de buff con reloj/numero de turnos

    // --- Agrega estas variables en la sección de [Header("Estados")] ---
    public int attackBuffDuration = 0; // Turnos que quedan de buff    

    [Header("Visuales")]
    public Image characterSprite;
    public Color turnColor = new Color(1f, 0.9f, 0.7f);

    private void Awake() { health = GetComponent<CharacterHealth>(); }

    public void OnTurnStarted() {
        // El veneno se procesa al INICIO del turno
        ProcessPoisonDamage();
        UpdateStatusUI();
    }

    public void OnTurnEnded() {
        // El buff de ataque se descuenta al FINAL del turno
        // Así garantizamos que si tiene "2 turnos", pueda atacar 2 veces con el buff.
        if (attackBuffDuration > 0) {
            attackBuffDuration--;
            if (attackBuffDuration <= 0) {
                attackBuff = 0; // Se acabó el tiempo, el daño extra vuelve a 0
                attackBuffDuration = 0; // Reset por seguridad
            }
        }
        UpdateStatusUI();
    }

    //public void OnTurnStarted() {
    //    // Si hay un buff con duración, restamos un turno al empezar
    //    if (attackBuffDuration > 0) {
    //        attackBuffDuration--;
    //        if (attackBuffDuration <= 0) {
    //            attackBuff = 0; // Se acabó el tiempo, vuelve a la normalidad
    //        }
    //    }

    //    ProcessPoisonDamage();
    //    UpdateStatusUI();
    //}


    // --- LÓGICA DE VENENO ---
    public void ApplyPoison(EnemyAI source, int amount) {
        if (poisonSources.ContainsKey(source)) {
            poisonSources[source] += amount; // Si ya me había envenenado, se stakea (suma)
        }
        else {
            poisonSources[source] = amount; // Si es nuevo, lo registra
        }
        UpdateStatusUI();
    }

    public void ClearPoisonFromSource(EnemyAI source) {
        if (poisonSources.ContainsKey(source)) {
            poisonSources.Remove(source); // Me curo el veneno de ESTE enemigo específico
            UpdateStatusUI();
        }
    }

    private void ProcessPoisonDamage() {
        int totalPoisonDamage = 0;
        List<EnemyAI> deadSources = new List<EnemyAI>();

        // Sumamos el veneno de todos los que siguen vivos
        foreach (var kvp in poisonSources) {
            if (kvp.Key == null || kvp.Key.GetComponent<CharacterHealth>().currentHealth <= 0) {
                deadSources.Add(kvp.Key); // Si el enemigo murió por otra cosa, lo anotamos para borrarlo
            }
            else {
                totalPoisonDamage += kvp.Value;
            }
        }

        // Limpiamos la basura (enemigos muertos)
        foreach (var dead in deadSources) poisonSources.Remove(dead);

        // Si hay veneno acumulado, pega directo a la vida
        if (totalPoisonDamage > 0) {
            health.TakeDamage(totalPoisonDamage);
        }
    }

    // --- MODIFICADO: Ahora devuelve un int (el daño que traspasó el escudo) ---
    public int ProcessIncomingDamage(int damage) {
        if (damage <= 0) return 0;
        int unblockedDamage = damage; // Calculamos cuánto daño puro pasa

        if (currentBlock > 0) {
            if (unblockedDamage >= currentBlock) {
                unblockedDamage -= currentBlock;
                currentBlock = 0;
            }
            else {
                currentBlock -= unblockedDamage;
                unblockedDamage = 0;
            }
        }

        if (unblockedDamage > 0) {
            health.TakeDamage(unblockedDamage);
            attackBuff = 0; // Se pierde el buff de ataque
        }
        UpdateStatusUI();

        return unblockedDamage; // Devuelve > 0 si logró tocar la vida real
    }

    public void SetTurnVisual(bool isItsTurn) {
        if (characterSprite != null) characterSprite.color = isItsTurn ? turnColor : Color.white;
    }

    public void AddBlock(int amount) { currentBlock += amount; UpdateStatusUI(); }

    // --- Modifica el método AddAttackBuff para que acepte duración ---
    // El parámetro 'duration' es opcional. Si es -1, es permanente (como el del player)
    public void AddAttackBuff(int amount, int duration = -1) {
        attackBuff += amount;
        if (duration != -1) {
            attackBuffDuration = duration;
        }
        UpdateStatusUI();
    }
    public void ClearBlock() { currentBlock = 0; UpdateStatusUI(); }

    // --- Actualiza UpdateStatusUI para mostrar la duración ---
    public void UpdateStatusUI() {
        if (statusContainer == null) return;
        foreach (Transform child in statusContainer) Destroy(child.gameObject);

        if (currentBlock > 0) CreateIcon(iconBlock, currentBlock);

        // Si el buff tiene duración, usamos un icono distinto o mostramos los turnos
        if (attackBuff > 0) {
            Sprite iconToShow = (attackBuffDuration > 0) ? iconAttackBuffTimer : iconAttackBuff;
            // En el texto mostramos: "Daño (Turnos)" si tiene duración
            string valText = (attackBuffDuration > 0) ? $"{attackBuff}({attackBuffDuration})" : attackBuff.ToString();
            CreateIcon(iconToShow, valText);
        }

        int totalPoison = 0;
        foreach (var kvp in poisonSources) totalPoison += kvp.Value;
        if (totalPoison > 0) CreateIcon(iconPoison, totalPoison.ToString());
    }

    // Cambiamos el CreateIcon para que acepte String (para el "Daño(Turnos)")
    private void CreateIcon(Sprite img, string val) {
        GameObject icon = Instantiate(statusIconPrefab, statusContainer);
        icon.transform.localScale = Vector3.one;
        icon.GetComponentInChildren<Image>().sprite = img;
        icon.GetComponentInChildren<TextMeshProUGUI>().text = val;
    }

    // Mantenemos esta sobrecarga para no romper el resto del código
    private void CreateIcon(Sprite img, int val) { CreateIcon(img, val.ToString()); }
}