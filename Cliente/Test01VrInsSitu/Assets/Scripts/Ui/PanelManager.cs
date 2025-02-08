using UnityEngine;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class PanelConstellationManager : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;  // Referencia a la c�mara del jugador
    [SerializeField] private Transform[] panels;      // Array de paneles en el espacio
    [SerializeField] private GameObject dynamicPanel; // Panel base
    [SerializeField] private GameObject buttonPrefab; // Prefab de botones para crear opciones
    [SerializeField] private TextMeshProUGUI titleText; // T�tulo del panel
    [SerializeField] private CanvasGroup panelCanvasGroup; // Para animaci�n de Fade In/Out

    private LineRenderer lineRenderer;
    private string currentRole = "user"; // Rol actual (por defecto "user")
    private int currentPanelIndex = 0; // Panel actual (0: Frente, 1: Izquierda, 2: Derecha)

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = panels.Length;
        lineRenderer.loop = true; // Para cerrar el loop de la constelaci�n
        lineRenderer.startWidth = 0.01f; // Ancho inicial de la l�nea
        lineRenderer.endWidth = 0.01f; // Ancho final de la l�nea
        UpdateLineRendererPositions();
        UpdatePanel(); // Configurar el panel inicial
    }

    void Update()
    {
        foreach (Transform panel in panels)
        {
            RotateTowardsPlayer(panel);
        }
        UpdateLineRendererPositions();
    }

    // M�todo para rotar los paneles hacia el jugador
    void RotateTowardsPlayer(Transform panel)
    {
        Vector3 direction = playerCamera.position - panel.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion rotationOffset = Quaternion.Euler(0, 180, 0); // Rotaci�n de 180 grados en el eje Y
        panel.rotation = Quaternion.Slerp(panel.rotation, targetRotation * rotationOffset, Time.deltaTime * 2f); // Rotaci�n suave con offset
    }

    // M�todo para actualizar las posiciones del LineRenderer
    void UpdateLineRendererPositions()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            lineRenderer.SetPosition(i, panels[i].position);
        }
    }

    // Actualizar el contenido del panel seg�n el �ndice y el rol
    public void UpdatePanel()
    {
        ClearPanel();

        switch (currentRole)
        {
            case "admin":
                ConfigureAdminPanel();
                break;
            case "user":
                ConfigureUserPanel();
                break;
        }

        StartCoroutine(FadeInPanel());
    }

    // Configurar los paneles para Admin
    private void ConfigureAdminPanel()
    {
        switch (currentPanelIndex)
        {
            case 0: // Frente
                titleText.text = "Administrar Salas";
                AddButton("Crear Sala", () => Debug.Log("Crear Sala"));
                AddButton("Ver Salas", () => Debug.Log("Ver Salas"));
                AddButton("Eliminar Sala", () => Debug.Log("Eliminar Sala"));
                break;

            case 1: // Izquierda
                titleText.text = "Usuarios Conectados";
                AddButton("Ver Usuarios", () => Debug.Log("Ver Usuarios Conectados"));
                AddButton("Cerrar Servidor", () => Debug.Log("Cerrar Servidor"));
                break;

            case 2: // Derecha
                titleText.text = "Cerrar Sesi�n";
                AddButton("Desloguear", () => Debug.Log("Cerrar Sesi�n"));
                break;
        }
    }

    // Configurar los paneles para User
    private void ConfigureUserPanel()
    {
        switch (currentPanelIndex)
        {
            case 0: // Frente
                titleText.text = "Salas Disponibles";
                AddButton("Ver Salas", () => Debug.Log("Ver Salas Disponibles"));
                AddButton("Unirse a Sala", () => Debug.Log("Unirse a Sala"));
                AddButton("Enviar Mensaje", () => Debug.Log("Enviar Mensaje"));
                break;

            case 1: // Izquierda
                titleText.text = "Cerrar Sesi�n";
                AddButton("Desloguear", () => Debug.Log("Cerrar Sesi�n"));
                break;

            case 2: // Derecha
                titleText.text = "Objetos Recibidos";
                AddButton("Ver Objetos", () => Debug.Log("Ver Objetos Recibidos"));
                break;
        }
    }

    // M�todo para limpiar el contenido actual del panel
    private void ClearPanel()
    {
        foreach (Transform child in dynamicPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // M�todo para agregar un bot�n din�micamente
    private void AddButton(string buttonText, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject newButton = Instantiate(buttonPrefab, dynamicPanel.transform);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        newButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(onClickAction);
    }

    // M�todo para cambiar entre paneles
    public void NavigateToNextPanel()
    {
        currentPanelIndex = (currentPanelIndex + 1) % 3;
        UpdatePanel();
    }

    public void NavigateToPreviousPanel()
    {
        currentPanelIndex = (currentPanelIndex - 1 + 3) % 3;
        UpdatePanel();
    }

    // Animaci�n de Fade In para el panel
    private System.Collections.IEnumerator FadeInPanel()
    {
        panelCanvasGroup.alpha = 0;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 1;
    }

    // M�todo para cambiar el rol actual
    public void SetRole(string role)
    {
        currentRole = role;
        currentPanelIndex = 0;
        UpdatePanel();
    }
}