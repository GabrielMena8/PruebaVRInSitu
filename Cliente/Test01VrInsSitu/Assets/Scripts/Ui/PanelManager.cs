using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    [Header("Referencias a los paneles en la escena")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;

    private string currentRole = "user";  // Rol actual: "user" o "admin"

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

    private void Start()
    {
        ShowLoginPanel(); // Mostrar el panel de login al inicio
        ConfigureLoginPanel();
    }

    #endregion

    #region Panel Methods

    /// <summary>
    /// Muestra el panel de inicio de sesi�n y limpia los paneles de navegaci�n.
    /// </summary>
    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
    }

    public void SetRole(string role)
    {
        currentRole = role;
        ConfigurePanels(role);  // Llama a la configuraci�n de los paneles
    }

    /// <summary>
    /// Configura los paneles de navegaci�n seg�n el rol del usuario.
    /// </summary>
    /// <param name="role">El rol del usuario ("admin" o "user").</param>
    public void ConfigurePanels(string role)
    {
        currentRole = role;
        loginPanel.SetActive(false);

        ConfigureFrontPanel();
        ConfigureLeftPanel();
        ConfigureRightPanel();
    }

    private void ConfigureLoginPanel()
    {
        ClearPanel(loginPanel);
        AddTitleToPanel(loginPanel, "Iniciar Sesi�n");

        // Crear y asignar los InputFields
        TMP_InputField usernameInput = AddInputFieldToPanel(loginPanel, "Usuario");
        TMP_InputField passwordInput = AddInputFieldToPanel(loginPanel, "Contrase�a", true);

        // Asignar los InputFields a la clase Login
        Login.Instance.usernameInput = usernameInput;
        Login.Instance.passwordInput = passwordInput;

        AddButtonToPanel(loginPanel, "Iniciar Sesi�n", () => Login.Instance.HandleLogin());
        loginPanel.SetActive(true);
    }

    /// <summary>
    /// Configura el contenido del panel frontal.
    /// </summary>
    private void ConfigureFrontPanel()
    {
        ClearPanel(frontPanel);

        if (currentRole == "admin")
        {
            AddTitleToPanel(frontPanel, "Administrar Salas");
            AddButtonToPanel(frontPanel, "Crear Sala", () => Debug.Log("Crear Sala"));
            AddButtonToPanel(frontPanel, "Ver Salas", () => Debug.Log("Ver Salas"));
            AddButtonToPanel(frontPanel, "Eliminar Sala", () => Debug.Log("Eliminar Sala"));
        }
        else if (currentRole == "user")
        {
            AddTitleToPanel(frontPanel, "Salas Disponibles");
            AddButtonToPanel(frontPanel, "Ver Salas", () => Debug.Log("Ver Salas Disponibles"));
            AddButtonToPanel(frontPanel, "Unirse a Sala", () => Debug.Log("Unirse a Sala"));
            AddButtonToPanel(frontPanel, "Enviar Mensaje", () => Debug.Log("Enviar Mensaje"));
        }

        frontPanel.SetActive(true);
    }

    /// <summary>
    /// Configura el contenido del panel izquierdo.
    /// </summary>
    private void ConfigureLeftPanel()
    {
        ClearPanel(leftPanel);

        AddTitleToPanel(leftPanel, "Cerrar Sesi�n");
        AddButtonToPanel(leftPanel, "Cerrar Sesi�n", () => HandleLogout());

        leftPanel.SetActive(true);
    }

    /// <summary>
    /// Configura el contenido del panel derecho.
    /// </summary>
    private void ConfigureRightPanel()
    {
        ClearPanel(rightPanel);

        if (currentRole == "admin")
        {
            AddTitleToPanel(rightPanel, "Opciones de Admin");
            AddButtonToPanel(rightPanel, "Ver Conectados", () => Debug.Log("Ver Usuarios Conectados"));
            AddButtonToPanel(rightPanel, "Cerrar Servidor", () => Debug.Log("Cerrar Servidor"));
        }
        else if (currentRole == "user")
        {
            AddTitleToPanel(rightPanel, "Objetos Recibidos");
            AddButtonToPanel(rightPanel, "Ver Objetos", () => Debug.Log("Lista de objetos recibidos"));
        }

        rightPanel.SetActive(true);
    }

    /// <summary>
    /// Maneja el proceso de cierre de sesi�n.
    /// </summary>
    private void HandleLogout()
    {
        Debug.Log("Cerrando sesi�n...");
        ShowLoginPanel();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Limpia el contenido de un panel espec�fico.
    /// </summary>
    /// <param name="panel">El panel a limpiar.</param>
    private void ClearPanel(GameObject panel)
    {
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// A�ade un t�tulo a un panel.
    /// </summary>
    /// <param name="panel">El panel donde a�adir el t�tulo.</param>
    /// <param name="titleText">El texto del t�tulo.</param>
    private void AddTitleToPanel(GameObject panel, string titleText)
    {
        GameObject titleObject = new GameObject("Title");
        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = titleText;
        title.fontSize = 24;
        title.alignment = TextAlignmentOptions.Center;
        titleObject.transform.SetParent(panel.transform, false);
    }

    /// <summary>
    /// A�ade un bot�n a un panel.
    /// </summary>
    /// <param name="panel">El panel donde a�adir el bot�n.</param>
    /// <param name="buttonText">El texto del bot�n.</param>
    /// <param name="onClickAction">La acci�n a ejecutar al hacer clic en el bot�n.</param>
    private void AddButtonToPanel(GameObject panel, string buttonText, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject buttonObject = new GameObject(buttonText);
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        Button button = buttonObject.AddComponent<Button>();
        buttonObject.transform.SetParent(panel.transform, false);

        GameObject textObject = new GameObject("Text");
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.transform.SetParent(buttonObject.transform, false);

        button.onClick.AddListener(onClickAction);
    }

    /// <summary>
    /// A�ade un campo de entrada a un panel.
    /// </summary>
    /// <param name="panel">El panel donde a�adir el campo de entrada.</param>
    /// <param name="placeholderText">El texto del marcador de posici�n.</param>
    /// <param name="isPassword">Indica si el campo es para una contrase�a.</param>
    private TMP_InputField AddInputFieldToPanel(GameObject panel, string placeholderText, bool isPassword = false)
    {
        GameObject inputFieldObject = new GameObject("InputField");
        RectTransform rectTransform = inputFieldObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 30);

        TMP_InputField inputField = inputFieldObject.AddComponent<TMP_InputField>();

        GameObject placeholder = new GameObject("Placeholder");
        TextMeshProUGUI placeholderTextComponent = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderTextComponent.text = placeholderText;
        placeholderTextComponent.fontSize = 18;
        placeholderTextComponent.color = Color.gray;
        placeholder.transform.SetParent(inputFieldObject.transform, false);

        GameObject text = new GameObject("Text");
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 18;
        textComponent.color = Color.black;
        text.transform.SetParent(inputFieldObject.transform, false);

        inputField.placeholder = placeholderTextComponent;
        inputField.textComponent = textComponent;

        if (isPassword)
        {
            inputField.contentType = TMP_InputField.ContentType.Password;
        }

        inputFieldObject.transform.SetParent(panel.transform, false);

        return inputField;
    }

    #endregion
}