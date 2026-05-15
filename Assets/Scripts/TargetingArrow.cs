using UnityEngine;
using System.Collections.Generic;

public class TargetingArrow : MonoBehaviour {
    public static TargetingArrow Instance;
    public GameObject dotPrefab;
    public GameObject arrowHeadPrefab;
    public int dotCount = 15;
    public float curveHeight = 150f;
    public float arrowAngleOffset = 0f;
    [Range(0, 1)] public float dotSpacing = 1f;

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

    public void ActivateArrow(Vector3 startPos) {
        isActive = true;
        foreach (var dot in dots) dot.gameObject.SetActive(true);
        arrowHead.gameObject.SetActive(true);
        UpdateArrow(startPos, startPos);
    }

    public void UpdateArrow(Vector3 startPos, Vector3 endPos) {
        if (!isActive) return;

        // Aseguramos que la Z sea coherente para que no se oculte tras el fondo
        startPos.z = -1f;
        endPos.z = -1f;

        // Calculamos la distancia para que la curva sea dinámica
        float distance = Vector3.Distance(startPos, endPos);
        Vector3 directionVec = endPos - startPos;

        // Punto de control: Calculamos una altura proporcional a la distancia
        Vector3 controlPoint = startPos + directionVec / 2f;
        controlPoint.y += Mathf.Max(2f, distance * 0.4f); // Se arquea más cuanto más lejos vas
        controlPoint.x -= directionVec.y * 0.2f; // Un pequeño desplazamiento lateral para que sea más orgánica

        Vector3 previousPos = startPos;

        for (int i = 0; i < dotCount; i++) {
            float t = (i / (float)(dotCount - 1)) * dotSpacing;
            float u = 1 - t;

            // Curva de Bezier con los nuevos puntos dinámicos
            Vector3 pointOnCurve = (u * u * startPos) + (2f * u * t * controlPoint) + (t * t * endPos);
            dots[i].position = pointOnCurve;

            // Orientación de la punta (usa el penúltimo punto para saber a dónde mirar)
            if (i == dotCount - 1) {
                arrowHead.position = pointOnCurve;
                Vector3 direction = pointOnCurve - previousPos;
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