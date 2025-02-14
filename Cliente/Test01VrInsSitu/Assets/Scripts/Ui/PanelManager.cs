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

    [Header("Referencias a la UI")]
    public UIAlertManager uiAlertManager; // Asigna este componente en el Inspector

    public List<string> currentConnectedUsernames = new List<string>();
    public List<string> currentSelectedFilePaths = new List<string>();

    private string currentRole = "user";  // "user" o "admin"
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
        ShowServerIPPanel();

        if (uiAlertManager != null)
        {
            uiAlertManager.ShowInstruction(
                "Controles:\n" +
                "- T: Trasladar\n" +
                "- R: Rotar\n" +
                "- S: Escalar\n" +
                "- E: Enviar objeto 3D"
            );
        }
    }

    #region Panel de IP y Login

    public void ShowLoginPanel()
    {
        // Asigna un color distinto al panel de login (por ejemplo, cyan)
        SetPanelColor(loginPanel, Color.cyan);

        loginPanel.SetActive(true);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
        ConfigureLoginPanel();
    }

    public void ShowServerIPPanel()
    {
        // Usa el loginPanel para ingresar la IP y le asigna un color, por ejemplo, un tono de naranja
        SetPanelColor(loginPanel, new Color(1f, 0.65f, 0f, 1f)); // Naranja

        ClearPanel(loginPanel);

        UIUtilities.CreateTitle(loginPanel.transform, "Ingrese la IP del servidor", 24);

        // InputField para la IP (ejemplo: "192.168.1.100")
        TMP_InputField ipInput = UIUtilities.CreateInputField(loginPanel.transform, "192.168.1.100", false, new Vector2(300, 30));

        // Bot�n para confirmar la IP
        UIUtilities.CreateButton(loginPanel.transform, "Confirmar IP", () =>
        {
            AuthManager.Instance.SetServerIP(ipInput.text);
        }, new Vector2(200, 50));

        loginPanel.SetActive(true);
    }

    private void ConfigureLoginPanel()
    {
        ClearPanel(loginPanel);
        UIUtilities.CreateTitle(loginPanel.transform, "Iniciar Sesi�n", 24);
        TMP_InputField usernameInput = UIUtilities.CreateInputField(loginPanel.transform, "Usuario", false, new Vector2(200, 30));
        TMP_InputField passwordInput = UIUtilities.CreateInputField(loginPanel.transform, "Contrase�a", true, new Vector2(200, 30));
        Login.Instance.usernameInput = usernameInput;
        Login.Instance.passwordInput = passwordInput;
        UIUtilities.CreateButton(loginPanel.transform, "Iniciar Sesi�n", () => Login.Instance.HandleLogin(), new Vector2(200, 50));
        loginPanel.SetActive(true);
    }

    #endregion

    #region Configuraci�n de Paneles

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

    #endregion

    #region Panel Frontal (Men� Principal)

    private void ConfigureFrontPanel()
    {
        ClearPanel(frontPanel);
        // Asigna un color al panel frontal, por ejemplo, verde
        SetPanelColor(frontPanel, Color.green);

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
            Debug.LogError("El nombre de la sala es nulo o vac�o.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("El nombre de la sala es nulo o vac�o.", AlertType.Error);
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
        else
        {
            Debug.LogError("El nombre de la sala es nulo o vac�o.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("El nombre de la sala es nulo o vac�o.", AlertType.Error);
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
            Debug.LogError("El nombre de la sala es nulo o vac�o.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("El nombre de la sala es nulo o vac�o.", AlertType.Error);
        }
    }

    #endregion

    #region Panel Izquierdo / Derecho

    private void ConfigureLeftPanel()
    {
        ClearPanel(leftPanel);
        // Asignar un color distinto, por ejemplo, amarillo
        SetPanelColor(leftPanel, Color.yellow);

        UIUtilities.CreateTitle(leftPanel.transform, "Cerrar Sesi�n", 24);
        UIUtilities.CreateButton(leftPanel.transform, "Cerrar Sesi�n", HandleLogout, new Vector2(200, 50));
        leftPanel.SetActive(true);
    }

    private void ConfigureRightPanel()
    {
        ClearPanel(rightPanel);
        // Asignar un color distinto, por ejemplo, magenta
        SetPanelColor(rightPanel, Color.magenta);

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
        // L�gica para mostrar los objetos recibidos
    }

    private void HandleLogout()
    {
        Debug.Log("Cerrando sesi�n...");
        ShowLoginPanel();
    }

    #endregion

    #region Mostrar Listas y Chat

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

    public void ShowChatPanel(string roomName)
    {
        // Ocultar paneles no relevantes
        loginPanel.SetActive(false);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);

        GameObject chatPanel = Instantiate(frontPanel, frontPanel.transform.parent);
        chatPanel.name = "ChatPanel";
        ClearPanel(chatPanel);

        UIUtilities.CreateTitle(chatPanel.transform, "Chat - Sala: " + roomName, 24);

        Transform scrollContent = UIUtilities.CreateScrollViewWithVerticalLayout(
            parent: chatPanel.transform,
            scrollViewName: "ChatScrollView"
        );

        chatContent = scrollContent;

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

        UIUtilities.CreateButton(
            chatPanel.transform,
            "Seleccionar Archivos",
            () =>
            {
                var extensions = new[] {
                    new ExtensionFilter("Archivos", "txt", "pdf", "png", "jpg", "mp4", "mp3", "zip")
                };
                var paths = StandaloneFileBrowser.OpenFilePanel("Seleccionar Archivos", "", extensions, true);
                if (paths.Length > 0)
                {
                    HandleFileSelection(paths);
                }
            },
            new Vector2(200, 50)
        );

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

        chatPanel.SetActive(true);
    }

    private void HandleFileSelection(string[] selectedPaths)
    {
        if (selectedPaths != null && selectedPaths.Length > 0)
        {
            currentSelectedFilePaths.AddRange(selectedPaths);
            UpdateSelectedFilesPanel(currentSelectedFilePaths);
        }
        else
        {
            Debug.LogError("No se seleccionaron archivos.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("No se seleccionaron archivos.", AlertType.Error);
        }
    }

    public void AppendSystemMessage(string message, Color messageColor)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            if (uiAlertManager != null)
               //uiAlertManager.ShowAlert("No se ha inicializado el contenedor del chat.", AlertType.Error);
            return;
        }
        UIUtilities.CreateChatMessage(chatContent, "SystemMessage", message, messageColor, 18, TMPro.TextAlignmentOptions.Left, true);
    }

    public void AppendChatMessage(string message)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("No se ha inicializado el contenedor del chat.", AlertType.Error);
            return;
        }
        UIUtilities.CreateChatMessage(chatContent, "Message", message, Color.black, 18, TMPro.TextAlignmentOptions.Left, true);
    }

    public void ShowTypingIndicator(string userName)
    {
        AppendChatMessage($"{userName} est� escribiendo...");
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
        Debug.Log("Regresando al men� principal despu�s de la eliminaci�n de la sala.");
    }

    #endregion

    #region Manejo de Archivos Seleccionados

    public void UpdateSelectedFilesPanel(List<string> filePaths)
    {
        GameObject panel = CreateSelectedFilesPanel();
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }
        UIUtilities.CreateTitle(panel.transform, "Archivos Seleccionados", 24);
        GameObject filesContainer = new GameObject("FilesContainer");
        filesContainer.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup layoutGroup = filesContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        ScrollRect scrollRect = filesContainer.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.content = filesContainer.GetComponent<RectTransform>();
        RectTransform rt = filesContainer.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 200f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        foreach (string filePath in filePaths)
        {
            GameObject fileContent = new GameObject("FileContent");
            fileContent.transform.SetParent(filesContainer.transform, false);
            HorizontalLayoutGroup horizontalLayoutGroup = fileContent.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayoutGroup.spacing = 5;
            var fileTextObj = UIUtilities.CreateText(fileContent.transform, System.IO.Path.GetFileName(filePath), 16, TMPro.TextAlignmentOptions.Left);
            fileTextObj.color = Color.black;
            GameObject deleteButton = new GameObject("DeleteButton");
            deleteButton.transform.SetParent(fileContent.transform, false);
            var buttonRect = deleteButton.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(30, 30);
            Button btn = deleteButton.AddComponent<Button>();
            UIUtilities.CreateText(deleteButton.transform, "X", 14, TMPro.TextAlignmentOptions.Center).color = Color.red;
            btn.onClick.AddListener(() => RemoveFile(filePath, filePaths));
        }
        UIUtilities.CreateButton(
            panel.transform,
            "Enviar Seleccionados",
            () =>
            {
                List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
                if (connectedUsers != null && connectedUsers.Count > 0)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(panel.transform.position);
                    float desiredZ = screenPos.z;
                    ShowContextMenu(connectedUsers, (selectedUser) =>
                    {
                        SendFilesToUser(selectedUser);
                    }, screenPos, desiredZ, true);
                }
                else
                {
                    Debug.LogWarning("No hay usuarios conectados.");
                    if (uiAlertManager != null)
                        uiAlertManager.ShowAlert("No hay usuarios conectados.", AlertType.Warning);
                }
            },
            new Vector2(200, 50)
        );
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
        rt.localPosition = new Vector3(19.83f, 0.2f, 95.7f);
        rt.localRotation = Quaternion.Euler(0, 45, 0);
        panel.SetActive(true);
        UIUtilities.CreateButton(
            selectedFilesPanel.transform,
            "Enviar Seleccionados",
            () =>
            {
                List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
                if (connectedUsers != null && connectedUsers.Count > 0)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedFilesPanel.transform.position);
                    float desiredZ = screenPos.z;
                    PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
                    {
                        SendFilesToUser(selectedUser);
                    }, screenPos, desiredZ, true);
                }
                else
                {
                    Debug.LogWarning("No hay usuarios conectados.");
                    if (uiAlertManager != null)
                        uiAlertManager.ShowAlert("No hay usuarios conectados.", AlertType.Warning);
                }
            },
            new Vector2(200, 50)
        );
        return panel;
    }

    // M�todo para elegir a un destinatario y enviar los archivos
    private void SendFilesToUser(string targetUser)
    {
        foreach (string filePath in currentSelectedFilePaths)
        {
            ChatClient.Instance.SendFileToUser(targetUser, filePath);
        }
    }

    #endregion

    #region Manejo de Usuarios Conectados / Context Menu

    public void ShowContextMenu(List<string> users, System.Action<string> onUserSelected, Vector2 screenPosition, float desiredZ, bool isFileSending = false)
    {
        if (contextMenuPanel == null)
        {
            Debug.LogError("contextMenuPanel no est� asignado en PanelManager.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("El panel de men� contextual no est� asignado.", AlertType.Error);
            return;
        }
        ClearPanel(contextMenuPanel);
        RectTransform rt = contextMenuPanel.GetComponent<RectTransform>();
        if (isFileSending)
        {
            Vector3 fixedPosition = new Vector3(-25.11f, 0.3f, 104.4f);
            rt.localRotation = Quaternion.Euler(0, -45f, 0);
            rt.localPosition = fixedPosition;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            Canvas canvas = contextMenuPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No se encontr� un Canvas en los padres de contextMenuPanel.");
                if (uiAlertManager != null)
                    uiAlertManager.ShowAlert("No se encontr� un Canvas en el men� contextual.", AlertType.Error);
                return;
            }
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out Vector2 localPoint))
            {
                rt.localPosition = new Vector3(localPoint.x, localPoint.y, desiredZ);
            }
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        if (users != null && users.Count > 0)
        {
            Debug.Log("Mostrando men� contextual con usuarios: " + string.Join(", ", users));
            foreach (string user in users)
            {
                UIUtilities.CreateButton(
                    contextMenuPanel.transform,
                    user,
                    () => { onUserSelected(user); HideContextMenu(); },
                    new Vector2(200, 50)
                );
            }
        }
        else
        {
            Debug.LogWarning("No hay usuarios para mostrar en el men� contextual.");
            if (uiAlertManager != null)
                uiAlertManager.ShowAlert("No hay usuarios conectados para mostrar.", AlertType.Warning);
        }
        contextMenuPanel.SetActive(true);
    }

    public void UpdateRightPanelWithFilePreview(FileChunk fileChunk, string filePath)
    {
        if (fileChunk.FileType.StartsWith("image"))
        {
            Texture2D texture = new Texture2D(2, 2);
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            texture.LoadImage(fileBytes);
            GameObject rightPanel = PanelManager.Instance.rightPanel;
            if (rightPanel == null)
            {
                Debug.LogError("No se encontr� el panel derecho.");
                if (uiAlertManager != null)
                    uiAlertManager.ShowAlert("No se encontr� el panel derecho.", AlertType.Error);
                return;
            }
            GameObject previewObject = rightPanel.transform.Find("ImagePreview")?.gameObject;
            if (previewObject == null)
            {
                previewObject = new GameObject("ImagePreview");
                previewObject.transform.SetParent(rightPanel.transform, false);
                RawImage rawImage = previewObject.AddComponent<RawImage>();
                RectTransform rt = rawImage.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 200f);
            }
            RawImage image = previewObject.GetComponent<RawImage>();
            image.texture = texture;
        }
        else
        {
            Debug.Log("El fragmento de archivo recibido no es una imagen, no se puede mostrar vista previa.");
        }
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
        string marker = "Usuarios Activos:";
        int index = userList.IndexOf(marker);
        if (index >= 0)
        {
            string listPart = userList.Substring(index + marker.Length).Trim();
            Debug.Log("Parte de usuarios: '" + listPart + "'");
            currentConnectedUsernames.Clear();
            if (!string.IsNullOrEmpty(listPart))
            {
                string[] entries = listPart.Split(',');
                foreach (string entry in entries)
                {
                    string trimmed = entry.Trim();
                    int spaceIndex = trimmed.IndexOf(' ');
                    string username = (spaceIndex > 0) ? trimmed.Substring(0, spaceIndex) : trimmed;
                    if (!string.IsNullOrEmpty(username) && !currentConnectedUsernames.Contains(username))
                    {
                        currentConnectedUsernames.Add(username);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ShowConnectedUsers: No se encontr� el marcador 'Usuarios Activos:' en la cadena recibida.");
            currentConnectedUsernames.Clear();
        }
        Debug.Log("Usuarios conectados: " + string.Join(", ", currentConnectedUsernames));
    }

    #endregion

    #region Utility

    private void ClearPanel(GameObject panel)
    {
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Asigna un color de fondo al panel, si tiene un componente Image.
    /// </summary>
    private void SetPanelColor(GameObject panel, Color color)
    {
        Image img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }
    }

    #endregion
}
