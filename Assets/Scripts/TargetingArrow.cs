using UnityEngine;
using System.Collections.Generic;

public class TargetingArrow : MonoBehaviour {
    public static TargetingArrow Instance;

    [Header("Configuración Visual")]
    public GameObject dotPrefab;
    public GameObject arrowHeadPrefab;
    public int dotCount = 15;
    public float curveHeight = 150f;

    [Header("Ajustes de Flecha")]
    public float arrowAngleOffset = 0f; // Por si tu sprite de flecha no mira a la derecha
    [Range(0, 1)] public float dotSpacing = 1f; // Para ajustar qué tan estirada se ve la línea

    private List<RectTransform> dots = new List<RectTransform>();
    private RectTransform arrowHead;
    private bool isActive = false;

    private void Awake() {
        Instance = this;
        for (int i = 0; i < dotCount; i++) {
            GameObject dot = Instantiate(dotPrefab, transform);
            dot.SetActive(false);
            dots.Add(dot.GetComponent<RectTransform>());
        }
        GameObject head = Instantiate(arrowHeadPrefab, transform);
        head.SetActive(false);
        arrowHead = head.GetComponent<RectTransform>();
    }

    public void ActivateArrow(Vector2 startPos) {
        isActive = true;
        foreach (var dot in dots) dot.gameObject.SetActive(true);
        arrowHead.gameObject.SetActive(true);
        UpdateArrow(startPos, startPos);
    }

    public void UpdateArrow(Vector2 startPos, Vector2 endPos) {
        if (!isActive) return;

        // Punto de control para la curva Bezier
        Vector2 controlPoint = startPos + (endPos - startPos) / 2;
        controlPoint.y += curveHeight;

        Vector2 previousPos = startPos;

        for (int i = 0; i < dotCount; i++) {
            // Calculamos t considerando el espaciado
            float t = (i / (float)(dotCount - 1)) * dotSpacing;
            float u = 1 - t;

            // Fórmula Bezier Cuadrática
            Vector2 pointOnCurve = (u * u * startPos) + (2 * u * t * controlPoint) + (t * t * endPos);
            dots[i].position = pointOnCurve;

            // Orientación de la punta de la flecha
            if (i == dotCount - 1) {
                arrowHead.position = pointOnCurve;
                Vector2 direction = pointOnCurve - previousPos;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrowHead.rotation = Quaternion.Euler(0, 0, angle + arrowAngleOffset);
            }
            previousPos = pointOnCurve;
        }
    }

    public void DeactivateArrow() {
        isActive = false;
        foreach (var dot in dots) dot.gameObject.SetActive(false);
        if (arrowHead != null) arrowHead.gameObject.SetActive(false);
    }
}