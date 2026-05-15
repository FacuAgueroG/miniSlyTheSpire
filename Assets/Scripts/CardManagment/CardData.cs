using UnityEngine;
using System.Collections.Generic;

// --- DEFINICIONES DE TIPOS (Mantenidas de tu código original) ---
public enum CardEffectType {
    None,
    DealDamage,
    GainBlock,
    GainAttackBuff
}

public enum CardBackgroundColor {
    Rojo, Azul, Verde, Naranja, Violeta
}

[System.Serializable]
public struct CardEffect {
    public CardEffectType effectType;
    public float value1;
    public float value2;

    [Header("Valores tras Mejora")]
    public float value1Upgrade; // El valor que tomará value1 si la carta se mejora
}

// --- EL SCRIPTABLE OBJECT ---
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject {
    [Header("Core Gameplay Base")]
    public string cardName;
    public int manaCost;
    [TextArea(2, 4)]
    public string description;

    [Header("Core Gameplay MEJORADO")]
    public int upgradedManaCost;
    [TextArea(2, 4)]
    public string upgradedDescription;

    [Header("Configuración de Ataque")]
    public bool isTargeted;
    public bool isAoE = false;

    [Header("Visuals")]
    public Sprite artwork;
    public CardBackgroundColor backgroundColor;

    [Header("Art Framing (Ajuste para la Máscara)")]
    public Vector2 artOffset;
    [Range(0.1f, 3f)]
    public float artScale = 1f;

    [Header("Acciones de la Carta")]
    public List<CardEffect> effects = new List<CardEffect>();
}