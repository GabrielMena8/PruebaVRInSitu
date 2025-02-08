using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    [Header("Referencias a los componentes de la UI")]
    [SerializeField] private Transform playerCamera;  // Referencia a la cámara del jugador
    [SerializeField] private Vector3[] panelPositions; // Array de posiciones de los paneles en el espacio
    [SerializeField] private GameObject dynamicPanel; // Panel base
    [SerializeField] private CameraNavigator cameraNavigator; // Referencia al navegador de cámara
    [SerializeField] private Login loginManager; // Referencia al administrador de login

    private LineRenderer lineRenderer;
    private string currentRole = "user"; // Rol actual (por defecto "user")
    private int currentPanelIndex = 0; // Panel actual (0: Frente, 1: Izquierda, 2: Derecha)
    private GameObject[] panels; // Array de paneles creados dinámicamente
    private CanvasGroup panelCanvasGroup;
    private TextMeshProUGUI titleText;

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = panelPositions.Length;
        lineRenderer.loop = true; // Para cerrar el loop de la constelación
        lineRenderer.startWidth = 0.01f; // Ancho inicial de la línea
        lineRenderer.endWidth = 0.01f; // Ancho final de la línea

        CreatePanels();
        CreateCanvasGroupAndTitleText();
        UpdateLineRendererPositions();
        ShowLoginPanel(); // Mostrar el panel de login al inicio
    }

    void Update()
    {
        foreach (GameObject panel in panels)
        {
            RotateTowardsPlayer(panel.transform);
        }
        UpdateLineRendererPositions();
    }

    #endregion

    #region Panel Methods

    // Método para crear los paneles en las posiciones especificadas
    void CreatePanels()
    {
        panels = new GameObject[panelPositions.Length];
        for (int i = 0; i < panelPositions.Length; i++)
        {
            GameObject panel = new GameObject($"Panel_{i}");
            panel.transform.position = panelPositions[i];
            panel.transform.SetParent(transform);
            panels[i] = panel;
        }

        // Pasar las referencias de los paneles a CameraNavigator
        cameraNavigator.SetPanels(panels);
    }

    // Método para crear y asignar el CanvasGroup y el título del panel
    void CreateCanvasGroupAndTitleText()
    {
        GameObject canvasGroupObject = new GameObject("CanvasGroup");
        panelCanvasGroup = canvasGroupObject.AddComponent<CanvasGroup>();
        panelCanvasGroup.transform.SetParent(dynamicPanel.transform, false);

        GameObject titleTextObject = new GameObject("TitleText");
        titleText = titleTextObject.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.transform.SetParent(dynamicPanel.transform, false);
    }

    // Método para rotar los paneles hacia el jugador
    void RotateTowardsPlayer(Transform panel)
    {
        Vector3 direction = playerCamera.position - panel.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion rotationOffset = Quaternion.Euler(0, 180, 0); // Rotación de 180 grados en el eje Y
        panel.rotation = Quaternion.Slerp(panel.rotation, targetRotation * rotationOffset, Time.deltaTime * 2f); // Rotación suave con offset
    }

    // Método para actualizar las posiciones del LineRenderer
    void UpdateLineRendererPositions()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            lineRenderer.SetPosition(i, panels[i].transform.position);
        }
    }

    // Mostrar el panel de login
    private void ShowLoginPanel()
    {
        ClearPanel();
        titleText.text = "Login";

        // Crear el panel de login
        GameObject loginPanel = new GameObject("LoginPanel");
        loginPanel.transform.SetParent(dynamicPanel.transform, false);
        RectTransform loginPanelRect = loginPanel.AddComponent<RectTransform>();
        loginPanelRect.sizeDelta = new Vector2(400, 300);
        loginPanelRect.anchoredPosition = Vector2.zero;

        // Añadir VerticalLayoutGroup al panel de login
        VerticalLayoutGroup layoutGroup = loginPanel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;

        // Crear InputField para el nombre de usuario
        TMP_InputField usernameInput = CreateInputField("Username", 18);
        usernameInput.transform.SetParent(loginPanel.transform, false);

        // Crear InputField para la contraseña
        TMP_InputField passwordInput = CreateInputField("Password", 18);
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.transform.SetParent(loginPanel.transform, false);

        // Asignar los InputFields al loginManager
        loginManager.usernameInput = usernameInput;
        loginManager.passwordInput = passwordInput;
        loginManager.loginPanel = loginPanel;

        // Crear botón de login
        GameObject loginButton = CreateButton("Login", () => HandleLogin(usernameInput.text, passwordInput.text), 18);
        loginButton.transform.SetParent(loginPanel.transform, false);

        panelCanvasGroup.alpha = 1;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    // Mostrar el panel de mensajes
    private void ShowMessagePanel()
    {
        ClearPanel();
        titleText.text = "Mensajes";

        // Crear ScrollView para los mensajes
        GameObject scrollView = CreateScrollView();
        scrollView.transform.SetParent(dynamicPanel.transform, false);

        // Crear InputField para escribir mensajes
        TMP_InputField messageInput = CreateInputField("Escribe un mensaje...", 18);
        messageInput.transform.SetParent(dynamicPanel.transform, false);

        // Crear botón para enviar mensajes
        GameObject sendButton = CreateButton("Enviar", () => SendChatMessage(messageInput.text), 18);
        sendButton.transform.SetParent(dynamicPanel.transform, false);

        panelCanvasGroup.alpha = 1;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    // Método para crear un InputField
    private TMP_InputField CreateInputField(string placeholderText, int fontSize)
    {
        GameObject inputFieldObject = new GameObject("InputField");
        RectTransform rectTransform = inputFieldObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 30);

        TMP_InputField inputField = inputFieldObject.AddComponent<TMP_InputField>();

        GameObject placeholder = new GameObject("Placeholder");
        TextMeshProUGUI placeholderTextComponent = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderTextComponent.text = placeholderText;
        placeholderTextComponent.fontSize = fontSize;
        placeholderTextComponent.color = Color.gray;
        placeholder.transform.SetParent(inputFieldObject.transform, false);

        GameObject text = new GameObject("Text");
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = fontSize;
        textComponent.color = Color.black;
        text.transform.SetParent(inputFieldObject.transform, false);

        inputField.placeholder = placeholderTextComponent;
        inputField.textComponent = textComponent;

        return inputField;
    }

    // Método para crear un botón
    private GameObject CreateButton(string buttonText, UnityEngine.Events.UnityAction onClickAction, int fontSize)
    {
        GameObject buttonObject = new GameObject("Button");
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 30);

        Button button = buttonObject.AddComponent<Button>();

        GameObject text = new GameObject("Text");
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.text = buttonText;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.black;
        text.transform.SetParent(buttonObject.transform, false);

        button.onClick.AddListener(onClickAction);

        return buttonObject;
    }

    // Método para crear un ScrollView
    private GameObject CreateScrollView()
    {
        GameObject scrollViewObject = new GameObject("ScrollView");
        RectTransform rectTransform = scrollViewObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 300);

        ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        RectTransform viewportRectTransform = viewport.AddComponent<RectTransform>();
        viewportRectTransform.sizeDelta = new Vector2(200, 300);
        viewport.transform.SetParent(scrollViewObject.transform, false);

        GameObject content = new GameObject("Content");
        RectTransform contentRectTransform = content.AddComponent<RectTransform>();
        contentRectTransform.sizeDelta = new Vector2(200, 300);
        content.transform.SetParent(viewport.transform, false);

        scrollRect.content = contentRectTransform;

        return scrollViewObject;
    }

    // Método para manejar el login
    private void HandleLogin(string username, string password)
    {
        AuthManager.Instance.AttemptLogin(username, password);
    }

    // Método para enviar mensajes
    private void SendChatMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Debug.Log("Mensaje enviado: " + message);
            // Aquí puedes agregar la lógica para enviar el mensaje
        }
        else
        {
            Debug.Log("Por favor, escribe un mensaje.");
        }
    }

    // Actualizar el contenido del panel según el índice y el rol
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
                titleText.text = "Cerrar Sesión";
                AddButton("Desloguear", () => Debug.Log("Cerrar Sesión"));
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
                titleText.text = "Cerrar Sesión";
                AddButton("Desloguear", () => Debug.Log("Cerrar Sesión"));
                break;

            case 2: // Derecha
                titleText.text = "Objetos Recibidos";
                AddButton("Ver Objetos", () => Debug.Log("Ver Objetos Recibidos"));
                break;
        }
    }

    // Método para limpiar el contenido actual del panel
    private void ClearPanel()
    {
        foreach (Transform child in dynamicPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Método para agregar un botón dinámicamente
    private void AddButton(string buttonText, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject newButton = CreateButton(buttonText, onClickAction, 18);
        newButton.transform.SetParent(dynamicPanel.transform, false);
    }

    // Método para cambiar entre paneles
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

    // Animación de Fade In para el panel
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

    // Método para cambiar el rol actual
    public void SetRole(string role)
    {
        currentRole = role;
        currentPanelIndex = 0;
        ShowMessagePanel(); // Mostrar el panel de mensajes después del login
        UpdatePanel();
    }

    #endregion
}