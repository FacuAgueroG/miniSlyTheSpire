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

    public void SetupCard(CardData newData) {
        cardData = newData;

        // 1. Mapear textos básicos
        if (nameText != null) nameText.text = cardData.cardName;
        if (manaText != null) manaText.text = cardData.manaCost.ToString();

        // --- INYECCIÓN DINÁMICA DE DESCRIPCIÓN (NUEVO) ---
        if (descriptionText != null) {
            string finalDescription = cardData.description;

            // Si la carta tiene al menos un efecto, inyectamos el valor en [v1]
            if (cardData.effects != null && cardData.effects.Count > 0) {
                // Reemplazamos [v1] por el valor real. 
                // Usamos <b></b> para que el número resalte en negrita (opcional)
                finalDescription = finalDescription.Replace("[v1]", "<b>" + cardData.effects[0].value1.ToString() + "</b>");
            }

            descriptionText.text = finalDescription;
        }

        // 2. Mapear arte y aplicar Framing (TU LÓGICA ORIGINAL)
        if (cardData.artwork != null && artworkImage != null) {
            artworkImage.sprite = cardData.artwork;

            // Movemos y escalamos la imagen en tiempo real según el ScriptableObject
            artworkImage.rectTransform.anchoredPosition = cardData.artOffset;
            artworkImage.rectTransform.localScale = Vector3.one * cardData.artScale;
        }

        // 3. Aplicar color de fondo (TU LÓGICA ORIGINAL)
        ApplyBackgroundColor();
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

    // 4. El motor de actualización en tiempo real para el modo Edición
#if UNITY_EDITOR
    void Update() {
        // Solo actualizamos frame a frame si NO estamos en Play Mode y la carta tiene datos
        if (!Application.isPlaying && cardData != null) {
            SetupCard(cardData);
        }
    }
#endif
}