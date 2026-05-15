using UnityEngine;
using UnityEngine.EventSystems;

public class CardUpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    private CardDisplay display;
    private CardInstance myInstance;

    public void Setup(CardInstance instance) {
        myInstance = instance;
        display = GetComponent<CardDisplay>();
        display.SetupCard(myInstance);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        // Si la carta ya está mejorada, no mostramos preview
        if (myInstance.isUpgraded) return;

        // PREVIEW: Creamos una instancia temporal mejorada solo para mostrarla
        CardInstance preview = new CardInstance(myInstance.data);
        preview.isUpgraded = true;
        display.SetupCard(preview);

        // Efecto visual: Podés cambiar el color del borde o escala
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData) {
        // Volvemos a mostrar la versión real (la que no está mejorada todavía)
        display.SetupCard(myInstance);
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData) {
        // Si ya está mejorada, no hacemos nada (o podrías sonar un error)
        if (myInstance.isUpgraded) return;

        // LLamamos al MapManager para confirmar la mejora
        MapManager.Instance.SelectUpgrade(myInstance);
    }
}