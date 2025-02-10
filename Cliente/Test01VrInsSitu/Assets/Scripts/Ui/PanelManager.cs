    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    using System.Linq;

    public class PanelManager : MonoBehaviour
    {
        public static PanelManager Instance;

        [Header("Referencias a los paneles en la escena")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject frontPanel;
        [SerializeField] private GameObject leftPanel;
        [SerializeField] private GameObject rightPanel;

        private string currentRole = "user";  // Rol actual: "user" o "admin"
        private TextMeshProUGUI roomListText;   // Texto para mostrar la lista de salas
    private Transform chatContent;


    

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
        /// Configura el contenido del panel frontal (menú principal).
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
                // Se podría agregar un botón para enviar mensajes directamente, pero el chat se maneja en un panel aparte.
            }

            frontPanel.SetActive(true);
        }

        /// <summary>
        /// Maneja la solicitud para ver la lista de salas.
        /// Limpia el panel frontal, muestra un mensaje de "Cargando salas..."
        /// y solicita la lista al servidor a través de ChatClient.
        /// </summary>
        private void HandleViewRooms()
        {
            Debug.Log("Iniciando HandleViewRooms");
            ClearPanel(frontPanel);
            AddTitleToPanel(frontPanel, "Salas Disponibles");

            // Muestra un mensaje temporal mientras se cargan las salas
            roomListText = AddTextToPanel(frontPanel, "Cargando salas...");

            // Agrega un botón para volver al menú principal, en caso de ser necesario
            AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);

            // Solicitar la lista de salas al servidor
            ChatClient.Instance.ViewRooms();
        }


        /// <summary>
        /// Muestra el campo de entrada para crear una sala y un botón "Aceptar".
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
            AddButtonToPanel(frontPanel, "Aceptar", () =>
            {
                string roomName = roomNameInput.text;
                if (!string.IsNullOrEmpty(roomName))
                {
                    Debug.Log($"Intentando unirse a la sala: {roomName}");
                    ChatClient.Instance.JoinRoom(roomName);
                    // No se vuelve inmediatamente al menú; se espera la respuesta del servidor para mostrar el chat.
                }
                else
                {
                    Debug.LogError("El nombre de la sala es nulo o vacío.");
                }
            });
            AddButtonToPanel(frontPanel, "Volver", ConfigureFrontPanel);
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
        /// Maneja la visualización de los objetos recibidos y muestra un botón "Volver".
        /// </summary>
        private void HandleViewReceivedItems()
        {
            ClearPanel(rightPanel);
            AddTitleToPanel(rightPanel, "Objetos Recibidos");

            roomListText = AddTextToPanel(rightPanel, "Cargando objetos...");
            AddButtonToPanel(rightPanel, "Volver", ConfigureRightPanel);

            // Aquí puedes manejar la lógica para mostrar los objetos recibidos
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

    #region Métodos para el Chat

    /// <summary>
    /// Muestra dinámicamente un panel de chat para la sala a la que se ha unido.
    /// Este método es llamado desde el ChatClient cuando se recibe la respuesta "JOINED_ROOM".
    /// </summary>
    /// <param name="roomName">Nombre de la sala a la que se ha unido</param>
    /// 
    /// <summary>
    /// Muestra dinámicamente un panel de chat para la sala a la que se ha unido.
    /// </summary>
    /// <param name="roomName">Nombre de la sala a la que se ha unido</param>
    public void ShowChatPanel(string roomName)
    {
        // Ocultar los paneles de menú
        loginPanel.SetActive(false);
        frontPanel.SetActive(false);
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);

        // Clonar el frontPanel para usarlo como base para el chatPanel (respetando su estilo)
        GameObject chatPanel = Instantiate(frontPanel, frontPanel.transform.parent);
        chatPanel.name = "ChatPanel";
        ClearPanel(chatPanel);

        // Añadir título al panel de chat
        UIUtilities.CreateTitle(chatPanel.transform, "Chat - Sala: " + roomName);

        // Crear un ScrollView para el historial del chat con componentes necesarios:
        // RectTransform, ScrollRect, Image y Mask
        GameObject scrollViewObject = new GameObject("ScrollView",
            typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scrollViewObject.transform.SetParent(chatPanel.transform, false);
        RectTransform scrollRectTransform = scrollViewObject.GetComponent<RectTransform>();
        // Establecer anclajes para ubicar el ScrollView (estos valores son un ejemplo)
        scrollRectTransform.anchorMin = new Vector2(0.05f, 0.3f);
        scrollRectTransform.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        // Definir explícitamente width y height (puedes ajustar estos valores según tus necesidades)
        scrollRectTransform.sizeDelta = new Vector2(400, 100);

        // Configurar el componente ScrollRect
        ScrollRect scrollRect = scrollViewObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Configurar la imagen de fondo y la máscara para ocultar el contenido desbordado
        Image scrollViewImage = scrollViewObject.GetComponent<Image>();
        scrollViewImage.color = new Color(1, 1, 1, 0.5f);
        Mask mask = scrollViewObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

     /*   // Crear y asignar un Scrollbar vertical al ScrollView
        GameObject verticalScrollbar = new GameObject("VerticalScrollbar",
            typeof(RectTransform), typeof(Scrollbar), typeof(Image));
        verticalScrollbar.transform.SetParent(scrollViewObject.transform, false);
        RectTransform scrollbarRect = verticalScrollbar.GetComponent<RectTransform>();
        // Ubicar el scrollbar en el lado derecho del ScrollView
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(20, 0); // Ancho de 20; la altura se ajusta automáticamente
        Image scrollbarImage = verticalScrollbar.GetComponent<Image>();
        scrollbarImage.color = Color.gray;
        /*Scrollbar verticalScrollbarComponent = verticalScrollbar.GetComponent<Scrollbar>();
        // Asignar el scrollbar al ScrollRect
        scrollRect.verticalScrollbar = verticalScrollbarComponent;
        */
        // Crear el contenedor (Content) para los mensajes
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scrollViewObject.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        // Configurar el Content para que se extienda horizontalmente y se adapte verticalmente
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        // Añadir VerticalLayoutGroup para distribuir los mensajes
        VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 5;

        // Añadir ContentSizeFitter para que el Content se ajuste automáticamente al contenido
        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Asignar el RectTransform del Content al ScrollRect
        scrollRect.content = contentRect;

        // Almacenar la referencia al contenedor de mensajes para agregar nuevos mensajes
        chatContent = content.transform;

        // Crear el InputField para escribir mensajes
        TMP_InputField chatInput = UIUtilities.CreateInputField(chatPanel.transform, "Escribe tu mensaje...");
        RectTransform inputRect = chatInput.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.05f, 0.05f);
        inputRect.anchorMax = new Vector2(0.75f, 0.15f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        // Suscribirse al evento onValueChanged para enviar el estado "TYPING"
        chatInput.onValueChanged.AddListener((text) =>
        {
            if (!string.IsNullOrEmpty(text))
            {
                ChatClient.Instance.SendTypingStatus();
            }
        });

        // Crear el botón "Enviar" y posicionarlo en la parte inferior derecha del panel
        Button sendButton = UIUtilities.CreateButton(chatPanel.transform, "Enviar", () =>
        {
            string msg = chatInput.text;
            if (!string.IsNullOrEmpty(msg))
            {
                ChatClient.Instance.SendMessageToRoom(msg);
                chatInput.text = "";
            }
        }, new Vector2(200, 50));
        RectTransform sendRect = sendButton.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(0.8f, 0.05f);
        sendRect.anchorMax = new Vector2(0.95f, 0.15f);
        sendRect.offsetMin = Vector2.zero;
        sendRect.offsetMax = Vector2.zero;

        // Crear el botón "Volver"
        Button backButton = UIUtilities.CreateButton(chatPanel.transform, "Volver", () =>
        {
            Destroy(chatPanel);
            ConfigurePanels(currentRole);
        }, new Vector2(200, 50));
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.05f, 0.05f);
        backRect.anchorMax = new Vector2(0.3f, 0.15f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        // Mostrar el chatPanel
        chatPanel.SetActive(true);
    }
    public void CloseChatPanel()
    {
        GameObject chatPanel = GameObject.Find("ChatPanel");
        if (chatPanel != null)
        {
            Destroy(chatPanel);  // Destruye el panel de chat para evitar que quede activo
        }
    }

    public void ShowMainMenu()
    {
        ClearPanel(frontPanel);
        ConfigureFrontPanel();  // Configura nuevamente el panel frontal para mostrar el menú principal
        Debug.Log("Regresando al menú principal después de la eliminación de la sala.");
    }



    public void AppendSystemMessage(string message, Color messageColor)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            return;
        }

        GameObject messageObject = new GameObject("SystemMessage", typeof(RectTransform));
        messageObject.transform.SetParent(chatContent, false);

        TextMeshProUGUI messageText = messageObject.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.fontSize = 18;
        messageText.alignment = TextAlignmentOptions.Left;
        messageText.color = messageColor;  // Color específico para el mensaje

        ContentSizeFitter csf = messageObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
    }


    /// <summary>
    /// Muestra un indicador de que un usuario está escribiendo.
    /// </summary>
    /// <param name="userName">El nombre del usuario que está escribiendo</param>
    public void ShowTypingIndicator(string userName)
    {
        // Simplemente se añade un mensaje informativo al historial
        AppendChatMessage($"{userName} está escribiendo...");
    }

    /// <summary>
    /// Agrega un mensaje al historial del chat.
    /// Cada mensaje se crea como un GameObject hijo del contenedor 'chatContent'
    /// que ya cuenta con un VerticalLayoutGroup para organizar los mensajes.
    /// </summary>
    /// <param name="message">Mensaje a agregar</param>
    public void AppendChatMessage(string message)
    {
        if (chatContent == null)
        {
            Debug.LogError("No se ha inicializado el contenedor del chat (chatContent).");
            return;
        }

        // Crear un nuevo GameObject para el mensaje
        GameObject messageObject = new GameObject("Message", typeof(RectTransform));
        messageObject.transform.SetParent(chatContent, false);

        // Añadir y configurar el componente TextMeshProUGUI para mostrar el mensaje
        TextMeshProUGUI messageText = messageObject.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.fontSize = 18;
        messageText.alignment = TextAlignmentOptions.Left;
        messageText.textWrappingMode = TextWrappingModes.NoWrap;

        // Agregar un ContentSizeFitter para que la altura se ajuste al contenido del mensaje
        ContentSizeFitter csf = messageObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Opcional: forzar la actualización del layout para que se vea inmediatamente
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
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

    public void RemoveUserFromUI(string username)
    {
        Debug.Log($"Eliminando a {username} de la UI.");
        // Lógica para encontrar y eliminar el usuario de la lista de usuarios conectados en la UI.
        // Por ejemplo, si estás mostrando los usuarios en un TextMeshProUGUI:
        if (roomListText != null)
        {
            string[] users = roomListText.text.Split('\n');
            roomListText.text = string.Join("\n", users.Where(user => !user.Contains(username)).ToArray());
        }
    }

    /// <summary>
    /// Añade un texto a un panel.
    /// </summary>
    /// <param name="panel">El panel donde añadir el texto.</param>
    /// <param name="textContent">El contenido del texto.</param>
    /// <param name="fontSize">El tamaño de la fuente (por defecto 18).</param>
    private TextMeshProUGUI AddTextToPanel(GameObject panel, string textContent, int fontSize = 18)
        {
            GameObject textObject = new GameObject("Text");
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = textContent;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.transform.SetParent(panel.transform, false);

            return text;
        }

        #endregion
    }
