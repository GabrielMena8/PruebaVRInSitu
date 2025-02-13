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

        UIUtilities.CreateButton(
                chatPanel.transform,
                "Seleccionar Archivos",
                () =>
                {
                    // Abrir el explorador de archivos
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


    private void HandleFileSelection(string[] selectedPaths)
    {
        // Verificamos que se haya seleccionado al menos un archivo
        if (selectedPaths != null && selectedPaths.Length > 0)
        {
            // Agregar los archivos seleccionados a la lista actual
            currentSelectedFilePaths.AddRange(selectedPaths);

            // Actualizamos el panel de archivos seleccionados
            UpdateSelectedFilesPanel(currentSelectedFilePaths);
        }
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




    /////////////////////////////////////////////////////////
    // Manejo de Archivos Seleccionados
    /////////////////////////////////////////////////////////

    public void UpdateSelectedFilesPanel(List<string> filePaths)
    {
        // Crear el panel de archivos seleccionados
        GameObject panel = CreateSelectedFilesPanel();

        // Limpiar panel
        foreach (Transform child in panel.transform)
        {
            Destroy(child.gameObject);
        }

        // Título del panel
        UIUtilities.CreateTitle(panel.transform, "Archivos Seleccionados", 24);

        // Crear contenedor para los archivos seleccionados con VerticalLayoutGroup
        GameObject filesContainer = new GameObject("FilesContainer");
        filesContainer.transform.SetParent(panel.transform, false);

        // Añadir un VerticalLayoutGroup para organizar los archivos
        VerticalLayoutGroup layoutGroup = filesContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // Alineación de los elementos
        layoutGroup.spacing = 10; // Espaciado entre los elementos
        layoutGroup.padding = new RectOffset(10, 10, 10, 10); // Ajuste de los márgenes

        // Añadir ScrollRect para que los elementos sean desplazables
        ScrollRect scrollRect = filesContainer.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; // Solo desplazamiento vertical
        scrollRect.vertical = true;
        scrollRect.content = filesContainer.GetComponent<RectTransform>();

        // Establecer tamaño del contenedor a 300px x 200px
        RectTransform rt = filesContainer.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 200f); // 300px x 200px
        rt.anchorMin = new Vector2(0.5f, 0.5f); // Centrado
        rt.anchorMax = new Vector2(0.5f, 0.5f); // Centrado

        // Crear contenedor de archivos directamente en el contenedor de archivos
        foreach (string filePath in filePaths)
        {
            // Crear contenedor de cada archivo
            GameObject fileContent = new GameObject("FileContent");
            fileContent.transform.SetParent(filesContainer.transform, false);

            // Añadir un LayoutGroup horizontal para el texto y el botón "X"
            HorizontalLayoutGroup horizontalLayoutGroup = fileContent.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayoutGroup.spacing = 5; // Espaciado entre el texto y el botón "X"

            // Crear el texto con el nombre del archivo
            var fileTextObj = UIUtilities.CreateText(fileContent.transform, System.IO.Path.GetFileName(filePath), 16, TMPro.TextAlignmentOptions.Left);
            fileTextObj.color = Color.black;

            // Crear el botón "X" para eliminar el archivo
            GameObject deleteButton = new GameObject("DeleteButton");
            deleteButton.transform.SetParent(fileContent.transform, false);

            var buttonRect = deleteButton.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(30, 30);

            Button btn = deleteButton.AddComponent<Button>();
            UIUtilities.CreateText(deleteButton.transform, "X", 14, TMPro.TextAlignmentOptions.Center).color = Color.red;

            // Función para eliminar el archivo
            btn.onClick.AddListener(() => RemoveFile(filePath, filePaths));
        }

        // Crear el botón "Enviar Seleccionados"
        UIUtilities.CreateButton(
            panel.transform,
            "Enviar Seleccionados",
            () =>
            {
                // Obtener usuarios conectados
                List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
                if (connectedUsers != null && connectedUsers.Count > 0)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(panel.transform.position);
                    float desiredZ = screenPos.z;

                    // Mostrar menú contextual con los usuarios conectados
                    ShowContextMenu(connectedUsers, (selectedUser) =>
                    {
                        // Enviar los archivos seleccionados al usuario seleccionado
                        SendFilesToUser(selectedUser);
                    }, screenPos, desiredZ, true); // Usamos el flag para mover el menú a la izquierda
                }
                else
                {
                    Debug.LogWarning("No hay usuarios conectados.");
                }
            },
            new Vector2(200, 50) // Tamaño del botón
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

        // Botón "Enviar Seleccionados" para abrir el menú contextual
        UIUtilities.CreateButton(
            selectedFilesPanel.transform,
            "Enviar Seleccionados",
            () =>
            {
                // Obtener usuarios conectados
                List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
                if (connectedUsers != null && connectedUsers.Count > 0)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedFilesPanel.transform.position);
                    float desiredZ = screenPos.z;

                    // Mostrar menú contextual con los usuarios conectados
                    PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
                    {
                        // Enviar los archivos seleccionados al usuario seleccionado
                        SendFilesToUser(selectedUser);
                    }, screenPos, desiredZ, true); // Usamos el flag para mover el menú a la izquierda
                }
                else
                {
                    Debug.LogWarning("No hay usuarios conectados.");
                }
            },
            new Vector2(200, 50)
        );

        return panel;
    }


    // Método para elegir a un destinatario y enviar los archivos
    private void SendFilesToUser(string targetUser)
    {
        foreach (string filePath in currentSelectedFilePaths)
        {
            ChatClient.Instance.SendFileToUser(targetUser, filePath); // Delegamos el envío al ChatClient
        }
       // Debug.Log($"Archivos enviados a {targetUser}");
    }

    /////////////////////////////////////////////////////////
    // Manejo de Usuarios Conectados / Context Menu
    /////////////////////////////////////////////////////////

    public void ShowContextMenu(List<string> users, System.Action<string> onUserSelected, Vector2 screenPosition, float desiredZ, bool isFileSending = false)
    {
        if (contextMenuPanel == null)
        {
            Debug.LogError("contextMenuPanel no está asignado en PanelManager.");
            return;
        }

        // Limpiar el panel
        ClearPanel(contextMenuPanel);

        RectTransform rt = contextMenuPanel.GetComponent<RectTransform>();

        // Si estamos enviando archivos, usamos las coordenadas fijas
        if (isFileSending)
        {
            // Coordenadas fijas para el contexto del envío de archivos
            Vector3 fixedPosition = new Vector3(-25.11f, 0.3f, 104.4f);

            rt.localRotation = Quaternion.Euler(0, -45f, 0);

            // Establecer posición con las coordenadas fijas
            rt.localPosition = fixedPosition;

            // Establecer anclajes y pivotes en el centro
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            // Si no estamos enviando archivos, usamos el cálculo de posición normal
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
                rt.localPosition = new Vector3(localPoint.x, localPoint.y, desiredZ);
            }
            // Establecer anclajes y pivotes en el centro
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
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
                    () => { onUserSelected(user); HideContextMenu(); },
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

    public void UpdateRightPanelWithFilePreview(FileChunk fileChunk, string filePath)
    {
        // Verifica si el fragmento de archivo es de tipo imagen
        if (fileChunk.FileType.StartsWith("image"))
        {
            // Cargar la imagen desde la ruta proporcionada
            Texture2D texture = new Texture2D(2, 2); // Inicializa la textura
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            texture.LoadImage(fileBytes);  // Carga la imagen en la textura

            // Buscar el panel derecho donde se mostrará la vista previa
            GameObject rightPanel = PanelManager.Instance.rightPanel;
            if (rightPanel == null)
            {
                Debug.LogError("No se encontró el panel derecho.");
                return;
            }

            // Crear o actualizar el objeto de vista previa de la imagen
            GameObject previewObject = rightPanel.transform.Find("ImagePreview")?.gameObject;
            if (previewObject == null)
            {
                // Crear el objeto si no existe
                previewObject = new GameObject("ImagePreview");
                previewObject.transform.SetParent(rightPanel.transform, false);

                // Crear el componente RawImage para la vista previa
                RawImage rawImage = previewObject.AddComponent<RawImage>();
                RectTransform rt = rawImage.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 200f);  // Establece el tamaño de la vista previa
            }

            // Asignar la textura cargada al RawImage para mostrar la imagen
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
