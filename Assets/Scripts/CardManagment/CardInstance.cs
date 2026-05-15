using System.Collections.Generic;

[System.Serializable]
public class CardInstance {
    public CardData data;      // Referencia a la carta original (.asset)
    public bool isUpgraded;    // Estado de mejora de ESTA instancia

    public CardInstance(CardData baseData) {
        data = baseData;
        isUpgraded = false;
    }

    // --- MÉTODOS DE ACCESO INTELIGENTE ---
    // Estos métodos devuelven el valor base o el mejorado según el estado de la instancia

    public int GetManaCost() => isUpgraded ? data.upgradedManaCost : data.manaCost;

    public string GetDescription() {
        string desc = isUpgraded ? data.upgradedDescription : data.description;
        // Si hay efectos, reemplazamos el tag [v1] por el valor real
        if (data.effects.Count > 0) {
            desc = desc.Replace("[v1]", "<b>" + GetEffectValue(0).ToString() + "</b>");
        }
        return desc;
    }

    public float GetEffectValue(int effectIndex) {
        if (effectIndex >= data.effects.Count) return 0;
        return isUpgraded ? data.effects[effectIndex].value1Upgrade : data.effects[effectIndex].value1;
    }
}