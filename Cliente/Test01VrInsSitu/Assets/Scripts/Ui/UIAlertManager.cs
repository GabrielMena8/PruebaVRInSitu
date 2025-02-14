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
    [Header("Prefab de Alerta")]
    [Tooltip("Prefab que debe contener un RectTransform y un TextMeshProUGUI.")]
    public GameObject alertPrefab;

    [Header("Configuraci�n de la Alerta")]
    [Tooltip("Duraci�n (en segundos) que se mostrar� la alerta.")]
    public float alertDuration = 5f;
    [Tooltip("Distancia desde la c�mara para posicionar la alerta (por ejemplo, 1.2 unidades).")]
    public float distanceFromCamera = 1.2f;
    [Tooltip("Offset base en world units respecto a la posici�n calculada (usando los vectores right y up de la c�mara).")]
    public Vector2 baseOffset = new Vector2(0.1f, -0.1f);
    [Tooltip("Espacio vertical entre alertas para que no se superpongan (en world units).")]
    public float spacingBetweenAlerts = 0.1f;

    // Lista para llevar el control de las alertas activas (RectTransforms)
    private List<RectTransform> activeAlerts = new List<RectTransform>();

    /// <summary>
    /// Muestra una alerta con el mensaje y tipo especificado.
    /// La alerta se posiciona en relaci�n a la posici�n y rotaci�n de la c�mara.
    /// </summary>
    /// <param name="message">El mensaje a mostrar.</param>
    /// <param name="alertType">El tipo de alerta (Error, Warning, Info, Success).</param>
    public void ShowAlert(string message, AlertType alertType)
    {
        Debug.Log("Mostrando alerta: " + message + " | Tipo: " + alertType.ToString());

        // Instanciar el prefab como hijo de este objeto (idealmente, este objeto est� en un Canvas en World Space)
        GameObject alertGO = Instantiate(alertPrefab, transform);
        alertGO.SetActive(true);

        // Estilizar el mensaje seg�n el tipo de alerta
        TextMeshProUGUI alertText = alertGO.GetComponentInChildren<TextMeshProUGUI>();
        if (alertText != null)
        {
            alertText.text = message;
            alertText.fontSize = 24;
            alertText.alignment = TextAlignmentOptions.Center;
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

        // Obtener el RectTransform y agregarlo a la lista de alertas activas
        RectTransform rt = alertGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            activeAlerts.Add(rt);
        }

        // Programar la destrucci�n del objeto y su remoci�n de la lista despu�s de 'alertDuration' segundos
        Destroy(alertGO, alertDuration);
        StartCoroutine(RemoveAlertAfterDelay(rt, alertDuration));
    }

    private IEnumerator RemoveAlertAfterDelay(RectTransform rt, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeAlerts.Remove(rt);
    }

    private void LateUpdate()
    {
        if (Camera.main == null)
            return;

        // Calcular la posici�n base en relaci�n a la c�mara:
        // Tomamos la posici�n de la c�mara y le sumamos un desplazamiento hacia adelante
        Vector3 basePosition = Camera.main.transform.position + Camera.main.transform.forward * distanceFromCamera;

        // Calculamos un offset en world units usando la direcci�n derecha y up de la c�mara
        Vector3 offset = Camera.main.transform.right * baseOffset.x + Camera.main.transform.up * baseOffset.y;

        // Para cada alerta activa, se aplica un offset adicional para apilarlas sin superposici�n
        for (int i = 0; i < activeAlerts.Count; i++)
        {
            RectTransform rt = activeAlerts[i];
            if (rt != null)
            {
                Vector3 stackingOffset = -Camera.main.transform.up * (i * spacingBetweenAlerts);
                rt.position = basePosition + offset + stackingOffset;
            }
        }
    }
}
