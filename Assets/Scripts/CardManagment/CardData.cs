using UnityEngine;
using System.Collections.Generic;

public enum CardEffectType {
    None,
    DealDamage,
    GainBlock,
    GainAttackBuff // <-- El nuevo efecto para potenciar ataques
}

[System.Serializable]
public struct CardEffect {
    public CardEffectType effectType;

    [Tooltip("Ej: Cantidad de daño, curación, o turnos del buff")]
    public float value1;

    [Tooltip("Ej: Multiplicador (1.5x) o daño extra del buff")]
    public float value2;
}

public enum CardBackgroundColor {
    Rojo, Azul, Verde, Naranja, Violeta
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject {
    [Header("Core Gameplay")]
    public string cardName;
    public int manaCost;
    [TextArea(2, 4)]
    public string description;
    public bool isTargeted;

    // Añade esta variable donde tengas tus otras variables (daño, costo, etc.)
    [Header("Tipo de Ataque")]
    public bool isAoE = false; // Si es true, pegará a todos

    [Header("Visuals")]
    public Sprite artwork;
    public CardBackgroundColor backgroundColor;

    [Header("Art Framing (Ajuste para la Máscara)")]
    public Vector2 artOffset;
    [Range(0.1f, 3f)] // Rango ampliado por si la imagen es muy grande
    public float artScale = 1f;

    [Header("Acciones de la Carta")]
    public List<CardEffect> effects = new List<CardEffect>();
}