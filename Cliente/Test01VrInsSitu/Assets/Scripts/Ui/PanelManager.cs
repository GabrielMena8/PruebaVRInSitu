using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using SFB;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    [Header("Referencias a los paneles en la escena")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private GameObject selectedFilesPanel;

    public List<string> currentConnectedUsernames = new List<string>();
    public List<string> currentSelectedFilePaths = new List<string>();

    


    private string currentRole = "user";  // Rol actual: "user" o "admin"
    private TextMeshProUGUI roomListText; // Texto para mostrar la lista de salas
    private Transform chatContent;

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
        ShowLoginPanel();
        ConfigureLoginPanel();
    }

    /////////////////////////////////////////////////////////
    // Panel de Inicio de Sesión
    /////////////////////////////////////////////////////////

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
    }

    private void ConfigureLoginPanel()
    {
        ClearPanel(loginPanel);
        UIUtilities.CreateTitle(loginPanel.transform, "Iniciar Sesión", 24);
        TMP_InputField usernameInput = UIUtilities.CreateInputField(loginPanel.transform, "Usuario", false, new Vector2(200, 30));
        TMP_InputField passwordInput = UIUtilities.CreateInputField(loginPanel.transform, "Contraseña", true, new Vector2(200, 30));
        Login.Instance.usernameInput = usernameInput;
        Login.Instance.passwordInput = passwordInput;
        UIUtilities.CreateButton(loginPanel.transform, "Iniciar Sesión", () => Login.Instance.HandleLogin(), new Vector2(200, 50));
        loginPanel.SetActive(true);
    }

    /////////////////////////////////////////////////////////
    // Configuración General de Paneles
    /////////////////////////////////////////////////////////

    public void SetRole(string role)
    {
        currentRole = role;
        ConfigurePanels(role);
    }

    public void ConfigurePanels(string role)
    {
        currentRole = role;
        loginPanel.SetActive(false);
        ConfigureFrontPanel();
        ConfigureLeftPanel();
        ConfigureRightPanel();
    }

    /////////////////////////////////////////////////////////
    // Panel Frontal (Menú Principal)
    /////////////////////////////////////////////////////////

    private void ConfigureFrontPanel()
    {
        ClearPanel(frontPanel);

        if (currentRole == "admin")
        {
            UIUtilities.CreateTitle(frontPanel.transform, "Administrar Salas", 24);
            UIUtilities.CreateButton(frontPanel.transform, "Crear Sala", ShowCreateRoomInput, new Vector2(200, 50));
            UIUtilities.CreateButton(frontPanel.transform, "Ver Salas", HandleViewRooms, new Vector2(200, 50));
            UIUtilities.CreateButton(frontPanel.transform, "Eliminar Sala", ShowDeleteRoomInput, new Vector2(200, 50));
        }
        else if (currentRole == "user")
        {
            UIUtilities.CreateTitle(frontPanel.transform, "Salas Disponibles", 24);
            UIUtilities.CreateButton(frontPanel.transform, "Ver Salas", HandleViewRooms, new Vector2(200, 50));
            UIUtilities.CreateButton(frontPanel.transform, "Unirse a Sala", ShowJoinRoomInput, new Vector2(200, 50));
        }

        frontPanel.SetActive(true);
    }

    private void HandleViewRooms()
    {
        ClearPanel(frontPanel);
        UIUtilities.CreateTitle(frontPanel.transform, "Salas Disponibles", 24);
        roomListText = UIUtilities.CreateText(frontPanel.transform, "Cargando salas...", 18);
        UIUtilities.CreateButton(frontPanel.transform, "Volver", ConfigureFrontPanel, new Vector2(200, 50));
        ChatClient.Instance.ViewRooms();
    }

    private void ShowCreateRoomInput()
    {
        ClearPanel(frontPanel);
        UIUtilities.CreateTitle(frontPanel.transform, "Crear Nueva Sala", 24);
        TMP_InputField roomNameInput = UIUtilities.CreateInputField(frontPanel.transform, "Nombre de la Sala", false, new Vector2(200, 30));
        UIUtilities.CreateButton(frontPanel.transform, "Aceptar", () => HandleCreateRoom(roomNameInput.text), new Vector2(200, 50));
        UIUtilities.CreateButton(frontPanel.transform, "Volver", ConfigureFrontPanel, new Vector2(200, 50));
    }

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
        ConfigureFrontPanel();
    }

    private void ShowDeleteRoomInput()
    {
        ClearPanel(frontPanel);
        UIUtilities.CreateTitle(frontPanel.transform, "Eliminar Sala", 24);
        TMP_InputField roomNameInput = UIUtilities.CreateInputField(frontPanel.transform, "Nombre de la Sala", false, new Vector2(200, 30));
        UIUtilities.CreateButton(frontPanel.transform, "Aceptar", () => HandleDeleteRoom(roomNameInput.text), new Vector2(200, 50));
        UIUtilities.CreateButton(frontPanel.transform, "Volver", ConfigureFrontPanel, new Vector2(200, 50));
    }

    private void HandleDeleteRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            ChatClient.Instance.DeleteRoom(roomName);
        }
        ConfigureFrontPanel();
    }

    private void ShowJoinRoomInput()
    {
        ClearPanel(frontPanel);
        UIUtilities.CreateTitle(frontPanel.transform, "Unirse a Sala", 24);
        TMP_InputField roomNameInput = UIUtilities.CreateInputField(frontPanel.transform, "Nombre de la Sala", false, new Vector2(200, 30));
        UIUtilities.CreateButton(frontPanel.transform, "Aceptar", () => HandleJoinRoom(roomNameInput.text), new Vector2(200, 50));
        UIUtilities.CreateButton(frontPanel.transform, "Volver", ConfigureFrontPanel, new Vector2(200, 50));
    }

    private void HandleJoinRoom(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            Debug.Log($"Intentando unirse a la sala: {roomName}");
            ChatClient.Instance.JoinRoom(roomName);
        }
        else
        {
            Debug.LogError("El nombre de la sala es nulo o vacío.");
        }
    }

    /////////////////////////////////////////////////////////
    // Panel Izquierdo / Derecho
    /////////////////////////////////////////////////////////

    private void ConfigureLeftPanel()
    {
        ClearPanel(leftPanel);
        UIUtilities.CreateTitle(leftPanel.transform, "Cerrar Sesión", 24);
        UIUtilities.CreateButton(leftPanel.transform, "Cerrar Sesión", HandleLogout, new Vector2(200, 50));
        leftPanel.SetActive(true);
    }

    private void ConfigureRightPanel()
    {
        ClearPanel(rightPanel);
        if (currentRole == "admin")
        {
            UIUtilities.CreateTitle(rightPanel.transform, "Opciones de Admin", 24);
            UIUtilities.CreateButton(rightPanel.transform, "Ver Conectados", HandleViewConnectedUsers, new Vector2(200, 50));
            UIUtilities.CreateButton(rightPanel.transform, "Cerrar Servidor", () => Debug.Log("Cerrar Servidor"), new Vector2(200, 50));
        }
        else if (currentRole == "user")
        {
            UIUtilities.CreateTitle(rightPanel.transform, "Objetos Recibidos", 24);
            UIUtilities.CreateButton(rightPanel.transform, "Ver Objetos", HandleViewReceivedItems, new Vector2(200, 50));
        }
        rightPanel.SetActive(true);
    }

    private void HandleViewConnectedUsers()
    {
        ClearPanel(rightPanel);
        UIUtilities.CreateTitle(rightPanel.transform, "Usuarios Conectados", 24);
        roomListText = UIUtilities.CreateText(rightPanel.transform, "Cargando usuarios...", 18);
        UIUtilities.CreateButton(rightPanel.transform, "Volver", ConfigureRightPanel, new Vector2(200, 50));
        ChatClient.Instance.ViewConnectedUsers();
    }

    private void HandleViewReceivedItems()
    {
        ClearPanel(rightPanel);
        UIUtilities.CreateTitle(rightPanel.transform, "Objetos Recibidos", 24);
        roomListText = UIUtilities.CreateText(rightPanel.transform, "Cargando objetos...", 18);
        UIUtilities.CreateButton(rightPanel.transform, "Volver", ConfigureRightPanel, new Vector2(200, 50));
        // Aquí puedes manejar la lógica para mostrar los objetos recibidos
    }

    private void HandleLogout()
    {
        Debug.Log("Cerrando sesión...");
        ShowLoginPanel();
    }

    /////////////////////////////////////////////////////////
    // Mostrar Listas de Salas y Usuarios
    /////////////////////////////////////////////////////////

    public void ShowRoomList(string roomList)
    {
        if (roomListText != null)
        {
            roomListText.text = "Salas disponibles:\n" + roomList;
        }
        else
        {
            roomListText = UIUtilities.CreateText(frontPanel.transform, "Salas disponibles:\n" + roomList, 18, TMPro.TextAlignmentOptions.Left);
        }
        Debug.Log("Lista de salas mostrada correctamente.");
    }


    /////////////////////////////////////////////////////////
    // Chat y Mensajes
    /////////////////////////////////////////////////////////

    public void ShowChatPanel(string roomName)
    {
        // Ocultar paneles no relevantes
        loginPanel.SetActive(false);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);

        // Crear el panel de chat
        GameObject chatPanel = Instantiate(frontPanel, frontPanel.transform.parent);
        chatPanel.name = "ChatPanel";
        ClearPanel(chatPanel);

        // Crear el título
        UIUtilities.CreateTitle(chatPanel.transform, "Chat - Sala: " + roomName, 24);

        // Crear ScrollView con un layout vertical para el historial de mensajes
        Transform scrollContent = UIUtilities.CreateScrollViewWithVerticalLayout(
            parent: chatPanel.transform,
            scrollViewName: "ChatScrollView"
        // size: new Vector2(0, 0) // se ajusta, o si prefieres un size fijo, p.ej. new Vector2(400, 300)
        );

        // Guardamos ese contenedor en chatContent para poder agregar mensajes
        chatContent = scrollContent;

        // Crear el InputField para mensajes
        TMP_InputField chatInput = UIUtilities.CreateInputField(chatPanel.transform, "Escribe tu mensaje...", false, new Vector2(400, 40));
        chatInput.onValueChanged.AddListener((string text) =>
        {
            if (!string.IsNullOrEmpty(text))
            {
                ChatClient.Instance.SendTypingStatus();
            }
        });

        RectTransform inputRect = chatInput.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.05f, 0.05f);
        inputRect.anchorMax = new Vector2(0.75f, 0.15f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        // Botón "Adjuntar"
        UIUtilities.CreateButton(
            chatPanel.transform,
            "Adjuntar",
            () =>
            {
                var extensions = new[] {
                new ExtensionFilter("Archivos", "txt", "pdf", "png", "jpg", "mp4", "mp3", "zip")
                };
                var paths = StandaloneFileBrowser.OpenFilePanel("Seleccionar Archivo", "", extensions, true);
                if (paths.Length > 0)
                {
                    currentSelectedFilePaths.Clear();
                    currentSelectedFilePaths.AddRange(paths);
                    Debug.Log("Archivos seleccionados: " + string.Join(", ", paths));
                }
            },
            new Vector2(100, 40)
        );

        // Botón "Enviar"
        UIUtilities.CreateButton(
            chatPanel.transform,
            "Enviar",
            () =>
            {
                string message = chatInput.text;
                if (!string.IsNullOrEmpty(message))
                {
                    ChatClient.Instance.SendMessageToRoom(message);
                   
                    
        
                }
            },
            new Vector2(100, 40)
        );

        // Botón "Volver"
        UIUtilities.CreateButton(
            chatPanel.transform,
            "Volver",
            () =>
            {
                Destroy(chatPanel);
                ConfigurePanels(currentRole);
            },
            new Vector2(100, 40)
        );

        // Mostrar el panel de chat
        chatPanel.SetActive(true);
    }


    public void AppendSystemMessage(string message, Color messageColor)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            return;
        }
        // Mensaje de sistema con word wrapping activado
        UIUtilities.CreateChatMessage(chatContent, "SystemMessage", message, messageColor, 18, TMPro.TextAlignmentOptions.Left, true);
    }

    public void AppendChatMessage(string message)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            return;
        }
        UIUtilities.CreateChatMessage(chatContent, "Message", message, Color.black, 18, TMPro.TextAlignmentOptions.Left, true);
    }

    public void ShowTypingIndicator(string userName)
    {
        AppendChatMessage($"{userName} está escribiendo...");
    }

    public void CloseChatPanel()
    {
        GameObject chatPanel = GameObject.Find("ChatPanel");
        if (chatPanel != null)
        {
            Destroy(chatPanel);
        }
    }

    public void ShowMainMenu()
    {
        ClearPanel(frontPanel);
        ConfigureFrontPanel();
        Debug.Log("Regresando al menú principal después de la eliminación de la sala.");
    }


    public void UpdateConnectedUsers(string userList)
    {
        // Convertir el string de usuarios a una lista
        currentConnectedUsernames = new List<string>(userList.Split(','));
        Debug.Log("Usuarios conectados actualizados: " + string.Join(", ", currentConnectedUsernames));
    }

    /////////////////////////////////////////////////////////
    // Manejo de Archivos Seleccionados
    /////////////////////////////////////////////////////////

    public void UpdateSelectedFilesPanel(List<string> filePaths)
    {
        GameObject panel = CreateSelectedFilesPanel();
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }
        UIUtilities.CreateTitle(panel.transform, "Archivos Seleccionados", 24);

        foreach (string filePath in filePaths)
        {
            GameObject fileEntry = new GameObject("FileEntry");
            fileEntry.transform.SetParent(panel.transform, false);

            var fileTextObj = UIUtilities.CreateText(fileEntry.transform, System.IO.Path.GetFileName(filePath), 16, TMPro.TextAlignmentOptions.Left);
            fileTextObj.color = Color.black;

            UIUtilities.CreateButton(fileEntry.transform, "X", () => RemoveFile(filePath, filePaths), new Vector2(30, 30));
        }
    }

    private void RemoveFile(string filePath, List<string> filePaths)
    {
        if (filePaths.Contains(filePath))
        {
            filePaths.Remove(filePath);
            UpdateSelectedFilesPanel(filePaths);
        }
    }

    public GameObject CreateSelectedFilesPanel()
    {
        GameObject panel;
        if (selectedFilesPanel == null)
        {
            panel = Instantiate(frontPanel, frontPanel.transform.parent);
            panel.name = "SelectedFilesPanel";
            selectedFilesPanel = panel;
        }
        else
        {
            panel = selectedFilesPanel;
        }

        ClearPanel(panel);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localPosition = new Vector3(11f, -12.2f, 104.4f);
        rt.localRotation = Quaternion.Euler(0, 45, 0);

        panel.SetActive(true);
        return panel;
    }

    /////////////////////////////////////////////////////////
    // Manejo de Usuarios Conectados / Context Menu
    /////////////////////////////////////////////////////////

    public void ShowContextMenu(List<string> users, System.Action<string> onUserSelected, Vector2 screenPosition, float desiredZ)
    {
        if (contextMenuPanel == null)
        {
            Debug.LogError("contextMenuPanel no está asignado en PanelManager.");
            return;
        }

        ClearPanel(contextMenuPanel);
        Canvas canvas = contextMenuPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No se encontró un Canvas en los padres de contextMenuPanel.");
            return;
        }
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Convertir la posición de pantalla a local
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint))
        {
            RectTransform rt = contextMenuPanel.GetComponent<RectTransform>();
            rt.localPosition = new Vector3(localPoint.x, localPoint.y, desiredZ);
        }

        // Verificar si hay usuarios antes de mostrar el menú
        if (users != null && users.Count > 0)
        {

            Debug.Log("Mostrando menú contextual con usuarios: " + string.Join(", ", users));

            foreach (string user in users)
            {
                UIUtilities.CreateButton(
                    contextMenuPanel.transform,
                    user,
                    () => {onUserSelected(user); HideContextMenu(); },
                    new Vector2(200, 50)
                );
            }
        }
        else
        {
            Debug.LogWarning("No hay usuarios para mostrar en el menú contextual.");
        }

        contextMenuPanel.SetActive(true);
    }


    public void HideContextMenu()
    {
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
    }

    public List<string> GetConnectedUsernames()
    {
        return new List<string>(currentConnectedUsernames);
    }


    public void RemoveUserFromUI(string username)
    {
        Debug.Log($"Eliminando a {username} de la UI.");
        if (roomListText != null)
        {
            string[] users = roomListText.text.Split('\n');
            roomListText.text = string.Join("\n", users.Where(user => !user.Contains(username)).ToArray());
        }
    }



    public void ShowConnectedUsers(string userList)
    {
        Debug.Log($"ShowConnectedUsers: Cadena recibida: '{userList}'");

        // Buscamos el marcador "Usuarios Activos:"
        string marker = "Usuarios Activos:";
        int index = userList.IndexOf(marker);

        if (index >= 0)
        {
            // Extraemos la parte que contiene los nombres
            string listPart = userList.Substring(index + marker.Length).Trim();
            Debug.Log("Parte de usuarios: '" + listPart + "'");

            // Reiniciamos la lista interna
            currentConnectedUsernames.Clear();

            // Si la cadena no está vacía, la separamos en base a comas (en este caso puede ser un solo usuario)
            if (!string.IsNullOrEmpty(listPart))
            {
                // Se espera que listPart sea, por ejemplo: "asd (Active)" o "asd (Active), bob (Active)"
                string[] entries = listPart.Split(',');

                foreach (string entry in entries)
                {
                    string trimmed = entry.Trim();
                    // Suponemos que el nombre es la primera palabra antes del primer espacio
                    int spaceIndex = trimmed.IndexOf(' ');
                    string username = (spaceIndex > 0) ? trimmed.Substring(0, spaceIndex) : trimmed;

                    // Agregamos solo si el nombre no está vacío y no tiene caracteres extraños
                    if (!string.IsNullOrEmpty(username) && !currentConnectedUsernames.Contains(username))
                    {
                        currentConnectedUsernames.Add(username);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ShowConnectedUsers: No se encontró el marcador 'Usuarios Activos:' en la cadena recibida.");
            currentConnectedUsernames.Clear();
        }

        // Ver la lista final de usuarios
        Debug.Log("Usuarios conectados: " + string.Join(", ", currentConnectedUsernames));
    }


    /////////////////////////////////////////////////////////
    // Utility
    /////////////////////////////////////////////////////////

    private void ClearPanel(GameObject panel)
    {
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
