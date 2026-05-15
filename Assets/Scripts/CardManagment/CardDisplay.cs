using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways] // <-- Esto fuerza a Unity a correr el script incluso sin darle Play
public class CardDisplay : MonoBehaviour {
    [Header("Data Core")]
    [Tooltip("El Scriptable Object que define qué es esta carta")]
    public CardData cardData;

    [Header("Referencias UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI manaText;
    public Image artworkImage;
    public Image backgroundImage;

    public CardInstance cardInstance;

    public void SetupCard(CardInstance newInstance) {
        cardInstance = newInstance;
        CardData data = cardInstance.data;

        // Nombre con "+" si está mejorada
        nameText.text = data.cardName + (cardInstance.isUpgraded ? "+" : "");

        // Costo y descripción automáticos desde la instancia
        manaText.text = cardInstance.GetManaCost().ToString();
        descriptionText.text = cardInstance.GetDescription();

        // Arte (se mantiene igual, ya que el arte no cambia con la mejora)
        if (data.artwork != null && artworkImage != null) {
            artworkImage.sprite = data.artwork;
            artworkImage.rectTransform.anchoredPosition = data.artOffset;
            artworkImage.rectTransform.localScale = Vector3.one * data.artScale;
        }

        ApplyBackgroundColor(data.backgroundColor);
    }

    private void ApplyBackgroundColor(CardBackgroundColor color) {
        if (backgroundImage == null) return;
        switch (color) {
            case CardBackgroundColor.Rojo: backgroundImage.color = new Color(0.8f, 0.2f, 0.2f); break;
            case CardBackgroundColor.Azul: backgroundImage.color = new Color(0.2f, 0.4f, 0.8f); break;
            case CardBackgroundColor.Verde: backgroundImage.color = new Color(0.2f, 0.8f, 0.3f); break;
            case CardBackgroundColor.Naranja: backgroundImage.color = new Color(0.9f, 0.6f, 0.1f); break;
            case CardBackgroundColor.Violeta: backgroundImage.color = new Color(0.6f, 0.2f, 0.8f); break;
        }
    }

    private void ApplyBackgroundColor() {
        if (backgroundImage == null || cardData == null) return;

        switch (cardData.backgroundColor) {
            case CardBackgroundColor.Rojo:
                backgroundImage.color = new Color(0.8f, 0.2f, 0.2f);
                break;
            case CardBackgroundColor.Azul:
                backgroundImage.color = new Color(0.2f, 0.4f, 0.8f);
                break;
            case CardBackgroundColor.Verde:
                backgroundImage.color = new Color(0.2f, 0.8f, 0.3f);
                break;
            case CardBackgroundColor.Naranja:
                backgroundImage.color = new Color(0.9f, 0.6f, 0.1f);
                break;
            case CardBackgroundColor.Violeta:
                backgroundImage.color = new Color(0.6f, 0.2f, 0.8f);
                break;
            default:
                backgroundImage.color = Color.white;
                break;
        }
    }

#if UNITY_EDITOR
    void Update() {
        if (!Application.isPlaying && cardData != null) {
            // Creamos una instancia "fantasma" solo para previsualizar en el editor
            CardInstance tempInst = new CardInstance(cardData);
            SetupCard(tempInst);
        }
    }
#endif
}