using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum AlertType
{
    Error,
    Warning,
    Info,
    Success
}

public class UIAlertManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab para alertas transitorias que debe tener un RectTransform y un TextMeshProUGUI.")]
    public GameObject alertPrefab;
    [Tooltip("Prefab para el panel de instrucciones persistente. Si no se asigna, se reutilizará alertPrefab.")]
    public GameObject instructionPrefab;

    [Header("Configuración de Alertas")]
    [Tooltip("Duración (en segundos) que se mostrará la alerta transitoria.")]
    public float alertDuration = 5f;
    [Tooltip("Distancia desde la cámara para posicionar las alertas.")]
    public float alertDistanceFromCamera = 1.2f;
    [Tooltip("Offset base en world units para las alertas respecto a la posición calculada.")]
    public Vector2 alertBaseOffset = new Vector2(0.1f, -0.1f);
    [Tooltip("Espacio vertical entre alertas (en world units).")]
    public float spacingBetweenAlerts = 0.1f;

    [Header("Configuración de Instrucciones")]
    [Tooltip("Distancia desde la cámara para posicionar el panel de instrucciones.")]
    public float instructionDistanceFromCamera = 1.2f;
    [Tooltip("Offset en world units para el panel de instrucciones respecto a la posición calculada.")]
    public Vector2 instructionOffset = new Vector2(0.0f, -0.3f);

    // Lista para llevar el control de las alertas transitorias
    private List<RectTransform> activeAlerts = new List<RectTransform>();

    // Referencia al panel de instrucciones actual (persistente)
    private RectTransform currentInstruction;

    /// <summary>
    /// Muestra una alerta transitoria con el mensaje y tipo especificado.
    /// Se reduce el tamaño del panel y el texto se adapta al mismo.
    /// </summary>
    public void ShowAlert(string message, AlertType alertType)
    {
        Debug.Log("Mostrando alerta: " + message + " | Tipo: " + alertType.ToString());
        // Instanciar el prefab como hijo del objeto que contiene este script (en un Canvas en World Space)
        GameObject alertGO = Instantiate(alertPrefab, transform);
        alertGO.SetActive(true);

        // Obtener el RectTransform del panel de alerta
        RectTransform rt = alertGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Reducir el tamaño: ancho a la mitad y restar 50 píxeles a la altura
            Vector2 currentSize = rt.sizeDelta;
            rt.sizeDelta = new Vector2(currentSize.x * 0.5f, currentSize.y - 50f);
            activeAlerts.Add(rt);
        }

        // Configurar el componente de texto para que se ajuste al tamaño del panel
        TextMeshProUGUI alertText = alertGO.GetComponentInChildren<TextMeshProUGUI>();
        if (alertText != null)
        {
            alertText.text = message;
            alertText.enableAutoSizing = true;
            alertText.fontSizeMin = 10;
            alertText.fontSizeMax = 24;
            alertText.enableWordWrapping = true;
            alertText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            alertText.alignment = TextAlignmentOptions.Center;
            // Establecer el color según el tipo de alerta
            switch (alertType)
            {
                case AlertType.Error:
                    alertText.color = Color.red;
                    break;
                case AlertType.Warning:
                    alertText.color = new Color(1f, 0.64f, 0f); // Naranja
                    break;
                case AlertType.Info:
                    alertText.color = Color.white;
                    break;
                case AlertType.Success:
                    alertText.color = Color.green;
                    break;
                default:
                    alertText.color = Color.white;
                    break;
            }
        }

        // Destruir la alerta después de alertDuration y removerla de la lista
        Destroy(alertGO, alertDuration);
        StartCoroutine(RemoveAlertAfterDelay(rt, alertDuration));
    }


    private IEnumerator RemoveAlertAfterDelay(RectTransform rt, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeAlerts.Remove(rt);
    }

    /// <summary>
    /// Muestra (o actualiza) un panel de instrucciones persistente.
    /// Se reutiliza el mismo prefab (o instructionPrefab si se asigna) y se le aplica un estilo distinto.
    /// </summary>
    public void ShowInstruction(string message)
    {
        if (currentInstruction == null)
        {
            // Si se asignó un prefab de instrucciones, se usa; de lo contrario se reutiliza el alertPrefab
            GameObject instructionGO = Instantiate(instructionPrefab != null ? instructionPrefab : alertPrefab, transform);
            instructionGO.SetActive(true);
            currentInstruction = instructionGO.GetComponent<RectTransform>();

            // Reducir el tamaño del panel de instrucciones: ancho a la mitad y restar 50 píxeles a la altura
            if (currentInstruction != null)
            {
                Vector2 currentSize = currentInstruction.sizeDelta;
                currentInstruction.sizeDelta = new Vector2(currentSize.x * 0.5f, currentSize.y - 50f);
            }

            // Cambiar el fondo (si tiene Image) y configurar el texto
            Image bg = instructionGO.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            TextMeshProUGUI text = instructionGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
                text.enableAutoSizing = true;
                text.fontSizeMin = 10;
                text.fontSizeMax = 18;
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Overflow;
                text.alignment = TextAlignmentOptions.Justified;
                text.color = Color.white;
            }
        }
        else
        {
            TextMeshProUGUI text = currentInstruction.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = message;
        }
    }

    /// <summary>
    /// Oculta el panel de instrucciones (opcional).
    /// </summary>
    public void HideInstruction()
    {
        if (currentInstruction != null)
        {
            Destroy(currentInstruction.gameObject);
            currentInstruction = null;
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null)
            return;

        // Actualizar alertas transitorias
        Vector3 baseAlertPosition = Camera.main.transform.position + Camera.main.transform.forward * alertDistanceFromCamera;
        Vector3 alertOffset = Camera.main.transform.right * alertBaseOffset.x + Camera.main.transform.up * alertBaseOffset.y;
        for (int i = 0; i < activeAlerts.Count; i++)
        {
            RectTransform rt = activeAlerts[i];
            if (rt != null)
            {
                Vector3 stackingOffset = -Camera.main.transform.up * (i * spacingBetweenAlerts);
                rt.position = baseAlertPosition + alertOffset + stackingOffset;
                rt.rotation = Camera.main.transform.rotation;
            }
        }

        // Actualizar panel de instrucciones para que siempre esté en pantalla
        if (currentInstruction != null)
        {
            Vector3 baseInstructionPosition = Camera.main.transform.position + Camera.main.transform.forward * instructionDistanceFromCamera;
            Vector3 instructionPos = baseInstructionPosition + Camera.main.transform.right * instructionOffset.x + Camera.main.transform.up * instructionOffset.y;
            currentInstruction.position = instructionPos;
            currentInstruction.rotation = Camera.main.transform.rotation;
        }
    }
}
