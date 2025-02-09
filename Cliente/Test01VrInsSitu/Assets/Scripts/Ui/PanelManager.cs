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
    private TextMeshProUGUI roomListText; // Texto para mostrar la lista de salas

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
    /// Muestra el panel de inicio de sesión y limpia los paneles de navegación.
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
        ConfigurePanels(role);  // Llama a la configuración de los paneles
    }

    /// <summary>
    /// Configura los paneles de navegación según el rol del usuario.
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
        AddTitleToPanel(loginPanel, "Iniciar Sesión");

        // Crear y asignar los InputFields
        TMP_InputField usernameInput = AddInputFieldToPanel(loginPanel, "Usuario");
        TMP_InputField passwordInput = AddInputFieldToPanel(loginPanel, "Contraseña", true);

        // Asignar los InputFields a la clase Login
        Login.Instance.usernameInput = usernameInput;
        Login.Instance.passwordInput = passwordInput;

        AddButtonToPanel(loginPanel, "Iniciar Sesión", () => Login.Instance.HandleLogin());
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
            AddButtonToPanel(frontPanel, "Crear Sala", ShowCreateRoomInput);
            AddButtonToPanel(frontPanel, "Ver Salas", HandleViewRooms);
            AddButtonToPanel(frontPanel, "Eliminar Sala", ShowDeleteRoomInput);
        }
        else if (currentRole == "user")
        {
            AddTitleToPanel(frontPanel, "Salas Disponibles");
            AddButtonToPanel(frontPanel, "Ver Salas", HandleViewRooms);
            AddButtonToPanel(frontPanel, "Unirse a Sala", ShowJoinRoomInput);
            AddButtonToPanel(frontPanel, "Enviar Mensaje", ShowSendMessageInput);
        }

        frontPanel.SetActive(true);
    }

    /// <summary>
    /// Muestra el campo de entrada para el nombre de la sala y un botón "Aceptar".
    /// </summary>
    private void ShowCreateRoomInput()
    {
        ClearPanel(frontPanel);
        AddTitleToPanel(frontPanel, "Crear Nueva Sala");

        TMP_InputField roomNameInput = AddInputFieldToPanel(frontPanel, "Nombre de la Sala");
        AddButtonToPanel(frontPanel, "Aceptar", () =>
        {
            string roomName = roomNameInput.text;
            Debug.Log($"Nombre de la sala ingresado: {roomName}");
            HandleCreateRoom(roomName);
        });
        AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);
    }

    /// <summary>
    /// Maneja la creación de la sala y vuelve al estado normal del panel.
    /// </summary>
    /// <param name="roomName">Nombre de la sala</param>
    private void HandleCreateRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            Debug.Log($"Creando sala con nombre: {roomName}");
            ChatClient.Instance.CreateRoom(roomName);
        }
        else
        {
            Debug.LogError("El nombre de la sala es nulo o vacío.");
        }
        ConfigureFrontPanel();  // Volver al estado normal del panel
    }

    /// <summary>
    /// Muestra el campo de entrada para eliminar una sala y un botón "Aceptar".
    /// </summary>
    private void ShowDeleteRoomInput()
    {
        ClearPanel(frontPanel);
        AddTitleToPanel(frontPanel, "Eliminar Sala");

        TMP_InputField roomNameInput = AddInputFieldToPanel(frontPanel, "Nombre de la Sala");
        AddButtonToPanel(frontPanel, "Aceptar", () => HandleDeleteRoom(roomNameInput.text));
        AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);
    }

    /// <summary>
    /// Maneja la eliminación de la sala y vuelve al estado normal del panel.
    /// </summary>
    /// <param name="roomName">Nombre de la sala</param>
    private void HandleDeleteRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            ChatClient.Instance.DeleteRoom(roomName);
        }
        ConfigureFrontPanel();  // Volver al estado normal del panel
    }

    /// <summary>
    /// Muestra el campo de entrada para unirse a una sala y un botón "Aceptar".
    /// </summary>
    private void ShowJoinRoomInput()
    {
        ClearPanel(frontPanel);
        AddTitleToPanel(frontPanel, "Unirse a Sala");

        TMP_InputField roomNameInput = AddInputFieldToPanel(frontPanel, "Nombre de la Sala");
        AddButtonToPanel(frontPanel, "Aceptar", () => HandleJoinRoom(roomNameInput.text));
        AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);
    }

    /// <summary>
    /// Maneja la unión a la sala y vuelve al estado normal del panel.
    /// </summary>
    /// <param name="roomName">Nombre de la sala</param>
    private void HandleJoinRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            // Aquí puedes manejar la lógica para unirse a la sala
            Debug.Log($"Unido a la sala: {roomName}");
        }
        ConfigureFrontPanel();  // Volver al estado normal del panel
    }

    /// <summary>
    /// Muestra el campo de entrada para enviar un mensaje y un botón "Aceptar".
    /// </summary>
    private void ShowSendMessageInput()
    {
        ClearPanel(frontPanel);
        AddTitleToPanel(frontPanel, "Enviar Mensaje");

        TMP_InputField messageInput = AddInputFieldToPanel(frontPanel, "Mensaje");
        AddButtonToPanel(frontPanel, "Aceptar", () => HandleSendMessage(messageInput.text));
        AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);
    }

    /// <summary>
    /// Maneja el envío del mensaje y vuelve al estado normal del panel.
    /// </summary>
    /// <param name="message">Mensaje a enviar</param>
    private void HandleSendMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            // Aquí puedes manejar la lógica para enviar el mensaje
            Debug.Log($"Mensaje enviado: {message}");
        }
        ConfigureFrontPanel();  // Volver al estado normal del panel
    }

    /// <summary>
    /// Maneja la visualización de las salas y muestra un botón "Volver".
    /// </summary>
    private void HandleViewRooms()
    {
        Debug.Log("Iniciando HandleViewRooms");
        ClearPanel(frontPanel);
        AddTitleToPanel(frontPanel, "Salas Disponibles");

        roomListText = AddTextToPanel(frontPanel, "Cargando salas...");
        AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);

        // Solicitar la lista de salas al servidor
        Debug.Log("Llamando a ChatClient.Instance.ViewRooms()");
        ChatClient.Instance.ViewRooms();

        Debug.Log("Viendo salas");
    }

    /// <summary>
    /// Muestra la lista de salas en el panel frontal.
    /// </summary>
    /// <param name="roomList">Lista de salas</param>
    public void ShowRoomList(string roomList)
    {
        Debug.Log("ShowRoomList llamado");
        if (roomListText != null)
        {
            Debug.Log("Actualizando roomListText");
            roomListText.text = "Salas disponibles:\n" + roomList;
        }
        else
        {
            roomListText = AddTextToPanel(frontPanel, "Salas disponibles:\n" + roomList);
        }
    }

    /// <summary>
    /// Configura el contenido del panel izquierdo.
    /// </summary>
    private void ConfigureLeftPanel()
    {
        ClearPanel(leftPanel);

        AddTitleToPanel(leftPanel, "Cerrar Sesión");
        AddButtonToPanel(leftPanel, "Cerrar Sesión", () => HandleLogout());

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
            AddButtonToPanel(rightPanel, "Ver Conectados", HandleViewConnectedUsers);
            AddButtonToPanel(rightPanel, "Cerrar Servidor", () => Debug.Log("Cerrar Servidor"));
        }
        else if (currentRole == "user")
        {
            AddTitleToPanel(rightPanel, "Objetos Recibidos");
            AddButtonToPanel(rightPanel, "Ver Objetos", HandleViewReceivedItems);
        }

        rightPanel.SetActive(true);
    }

    /// <summary>
    /// Maneja la visualización de los usuarios conectados y muestra un botón "Volver".
    /// </summary>
    private void HandleViewConnectedUsers()
    {
        ClearPanel(rightPanel);
        AddTitleToPanel(rightPanel, "Usuarios Conectados");

        roomListText = AddTextToPanel(rightPanel, "Cargando usuarios...");
        AddButtonToPanel(rightPanel, "Volver", ConfigureRightPanel);

        // Solicitar la lista de usuarios conectados al servidor
        ChatClient.Instance.ViewConnectedUsers();
    }

    /// <summary>
    /// Muestra la lista de usuarios conectados en el panel derecho.
    /// </summary>
    /// <param name="userList">Lista de usuarios conectados</param>
    public void ShowConnectedUsers(string userList)
    {
        Debug.Log($"Actualizando la lista de usuarios conectados en el panel: {userList}");
        if (roomListText != null)
        {
            roomListText.text = "Usuarios conectados:\n" + userList;
        }
        else
        {
            roomListText = AddTextToPanel(rightPanel, "Usuarios conectados:\n" + userList);
        }
    }

    /// <summary>
    /// Maneja la visualización de los objetos recibidos y muestra un botón "Volver".
    /// </summary>
    private void HandleViewReceivedItems()
    {
        ClearPanel(rightPanel);
        AddTitleToPanel(rightPanel, "Objetos Recibidos");

        roomListText = AddTextToPanel(rightPanel, "Cargando objetos...");
        AddButtonToPanel(rightPanel, "Volver", ConfigureRightPanel);

        // Aquí puedes manejar la lógica para mostrar los objetos recibidos
        // Por ejemplo, puedes llamar a un método en ChatClient para obtener la lista de objetos
        // ChatClient.Instance.ViewReceivedItems();
    }

    /// <summary>
    /// Muestra la lista de objetos recibidos en el panel derecho.
    /// </summary>
    /// <param name="itemList">Lista de objetos recibidos</param>
    public void ShowReceivedItems(string itemList)
    {
        if (roomListText != null)
        {
            roomListText.text = "Objetos recibidos:\n" + itemList;
        }
        else
        {
            roomListText = AddTextToPanel(rightPanel, "Objetos recibidos:\n" + itemList);
        }
    }

    /// <summary>
    /// Maneja el proceso de cierre de sesión.
    /// </summary>
    private void HandleLogout()
    {
        Debug.Log("Cerrando sesión...");
        ShowLoginPanel();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Limpia el contenido de un panel específico.
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
    /// Añade un título a un panel.
    /// </summary>
    /// <param name="panel">El panel donde añadir el título.</param>
    /// <param name="titleText">El texto del título.</param>
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
    /// Añade un botón a un panel.
    /// </summary>
    /// <param name="panel">El panel donde añadir el botón.</param>
    /// <param name="buttonText">El texto del botón.</param>
    /// <param name="onClickAction">La acción a ejecutar al hacer clic en el botón.</param>
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
    /// Añade un campo de entrada a un panel.
    /// </summary>
    /// <param name="panel">El panel donde añadir el campo de entrada.</param>
    /// <param name="placeholderText">El texto del marcador de posición.</param>
    /// <param name="isPassword">Indica si el campo es para una contraseña.</param>
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

    /// <summary>
    /// Añade un texto a un panel.
    /// </summary>
    /// <param name="panel">El panel donde añadir el texto.</param>
    /// <param name="textContent">El contenido del texto.</param>
    private TextMeshProUGUI AddTextToPanel(GameObject panel, string textContent)
    {
        GameObject textObject = new GameObject("Text");
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textContent;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.transform.SetParent(panel.transform, false);

        return text;
    }

    #endregion
}