using System.Collections.Generic;
using UnityEngine;

public class HandView : MonoBehaviour {
    // 1. Convertimos la mano en Singleton para poder hablarle desde los mánagers
    public static HandView Instance { get; private set; }

    [Header("Visibilidad (UX)")]
    [Tooltip("Porcentaje de la pantalla que baja la mano al ocultarse (0.4 = 40%)")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenYOffsetPercentage = 0.4f;
    private bool isHidden = false; // Estado actual de la mano

    [Header("Limits")]
    [SerializeField] private int maxCards = 10;

    [Header("Fan Layout")]
    [SerializeField] private float cardSpacing = 90f;
    [Range(0f, 160f)]
    [SerializeField] private float fanAngle = 60f;
    [SerializeField] private float curveRadius = 500f;   // usar positivo
    [SerializeField] private float baseYOffset = 0f;
    [SerializeField] private float extraRotation = 0f;

    [Header("Hover (Hearthstone-like)")]
    [SerializeField] private float hoverLiftY = 140f;          // cuánto sube la carta
    [SerializeField] private float hoverStraightenSpeed = 20f;  // si smooth está ON
    [SerializeField] private float hoverPushX = 70f;            // cuánto empuja a vecinas
    [SerializeField] private float hoverPushFalloff = 2f;       // 1 = empuja muchas, 3 = solo vecinas
    [SerializeField] private float hoverScale = 1.08f;          // opcional

    [Header("Smoothing")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float smoothSpeed = 18f;

    [Header("References")]
    [SerializeField] private RectTransform container;

    private readonly List<RectTransform> cards = new();

    private RectTransform hovered;
    private int hoveredIndex = -1;

    private readonly List<Vector2> targetPos = new();
    private readonly List<float> targetRotZ = new();
    private readonly List<float> targetScale = new();

    private void Awake() {
        if (Instance == null) Instance = this;

        if (container == null)
            container = (RectTransform)transform;
    }

    private void Update() {
        if (!Application.isPlaying) return;
        if (!smooth) return;

        for (int i = 0; i < cards.Count; i++) {
            var rt = cards[i];
            if (rt == null) continue;

            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos[i], Time.deltaTime * smoothSpeed);

            float rotSpeed = (i == hoveredIndex) ? hoverStraightenSpeed : smoothSpeed;
            rt.localRotation = Quaternion.Slerp(rt.localRotation, Quaternion.Euler(0f, 0f, targetRotZ[i]), Time.deltaTime * rotSpeed);

            float s = Mathf.Lerp(rt.localScale.x, targetScale[i], Time.deltaTime * smoothSpeed);
            rt.localScale = new Vector3(s, s, 1f);
        }
    }

    public bool IsFull => cards.Count >= maxCards;
    public int Count => cards.Count;

    // 2. Este es el método que los Mánagers llamarán para esconder/mostrar la mano
    public void SetHiddenState(bool hidden) {
        isHidden = hidden;
        Layout(); // Al llamar a Layout, recalculamos los TargetPos y el Update hace la animación sola
    }

    public void AddCard(RectTransform cardRt) {
        if (cardRt == null) return;
        if (IsFull) return;

        cardRt.SetParent(container, worldPositionStays: false);
        cards.Add(cardRt);

        targetPos.Add(cardRt.anchoredPosition);
        targetRotZ.Add(cardRt.localEulerAngles.z);
        targetScale.Add(1f);

        var hover = cardRt.GetComponent<CardHover>();
        if (hover == null) hover = cardRt.gameObject.AddComponent<CardHover>();
        hover.SetHand(this);

        Layout();
    }

    public void RemoveCard(RectTransform cardRt) {
        if (cardRt == null) return;

        int idx = cards.IndexOf(cardRt);
        if (idx < 0) return;

        cards.RemoveAt(idx);
        targetPos.RemoveAt(idx);
        targetRotZ.RemoveAt(idx);
        targetScale.RemoveAt(idx);

        if (hovered == cardRt) {
            hovered = null;
            hoveredIndex = -1;
        }

        Layout();
    }

    public void SetHovered(RectTransform rt) {
        if (rt == null) return;
        hovered = rt;
        hoveredIndex = cards.IndexOf(rt);
        Layout();
    }

    public void ClearHovered(RectTransform rt) {
        if (rt == null) return;
        if (hovered != rt) return;
        hovered = null;
        hoveredIndex = -1;
        Layout();
    }

    public void Layout() {
        int n = cards.Count;
        if (n == 0) return;

        float half = (n - 1) * 0.5f;

        for (int i = 0; i < n; i++) {
            var rt = cards[i];
            if (rt == null) continue;

            float t = (n == 1) ? 0f : (i - half) / half;
            float angle = t * (fanAngle * 0.5f);

            float x = (i - half) * cardSpacing;

            float theta = angle * Mathf.Deg2Rad;
            float y = baseYOffset + (curveRadius - Mathf.Cos(theta) * curveRadius);

            // 3. LA MAGIA RESPONSIVA AQUÍ: 
            // Si la mano debe esconderse, le restamos el % de la pantalla actual al eje Y
            if (isHidden) {
                y -= Screen.height * hiddenYOffsetPercentage;
            }

            float rotZ = -angle + extraRotation;
            float scale = 1f;

            if (hoveredIndex >= 0) {
                int dist = Mathf.Abs(i - hoveredIndex);
                float fall = Mathf.Exp(-(dist - 1) * hoverPushFalloff);
                float push = (dist == 0) ? 0f : hoverPushX * fall;

                if (i < hoveredIndex) x -= push;
                if (i > hoveredIndex) x += push;

                if (i == hoveredIndex) {
                    y += hoverLiftY;
                    rotZ = 0f;
                    scale = hoverScale;
                    rt.SetAsLastSibling();
                }
            }

            targetPos[i] = new Vector2(x, y);
            targetRotZ[i] = rotZ;
            targetScale[i] = scale;

            if (!smooth || !Application.isPlaying) {
                rt.anchoredPosition = targetPos[i];
                rt.localRotation = Quaternion.Euler(0f, 0f, targetRotZ[i]);
                rt.localScale = new Vector3(scale, scale, 1f);
            }

            if (hoveredIndex < 0)
                rt.SetSiblingIndex(i);
        }
    }

    private void OnValidate() {
        if (container == null) container = (RectTransform)transform;
        Layout();
    }
}