using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;
using WebSocketSharp;

public class AuthManager : MonoBehaviour
{
    #region Singleton
    public static AuthManager Instance;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    #endregion

    #region WebSocket
    private WebSocket ws;
    public WebSocket WS => ws;
    private float reconnectDelay = 5f;
    private float reconnectStartTime;
    public string serverURL = "ws://127.0.0.1:8080/chat";

    public void SetServerIP(string ipInput)
    {
        if (string.IsNullOrEmpty(ipInput))
        {
            Debug.LogError("La IP ingresada es nula o vacía.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Debe ingresar una IP válida.", AlertType.Error);
            return;
        }
        if (!ipInput.StartsWith("ws://"))
        {
            ipInput = "ws://" + ipInput;
        }
        string withoutPrefix = ipInput.Substring(5);
        string ipPart = withoutPrefix;
        int colonIndex = ipPart.IndexOf(":");
        if (colonIndex >= 0)
        {
            ipPart = ipPart.Substring(0, colonIndex);
        }
        if (!IPAddress.TryParse(ipPart, out IPAddress ipAddress))
        {
            Debug.LogError("La IP ingresada no es válida.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("La IP ingresada no es válida.", AlertType.Error);
            return;
        }
        if (!ipInput.Contains(":8080"))
        {
            ipInput += ":8080";
        }
        if (!ipInput.EndsWith("/chat"))
        {
            ipInput += "/chat";
        }
        serverURL = ipInput;
        Debug.Log("Servidor configurado en: " + serverURL);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Servidor configurado correctamente.", AlertType.Success);
        PanelManager.Instance.ShowLoginPanel();
    }

    /// <summary>
    /// Coroutine para conectar el WebSocket con try/catch y reintentos con delay.
    /// </summary>
    private IEnumerator ConnectWebSocketRoutine(string username, string password)
    {
        bool connected = false;
        while (!connected)
        {
            try
            {
                ws = new WebSocket(serverURL);

                ws.OnOpen += (sender, e) =>
                {
                    try
                    {
                        Debug.Log("Conexión establecida. Enviando credenciales...");
                        ws.Send($"LOGIN {username} {password}");
                        PanelManager.Instance?.uiAlertManager?.ShowAlert("Conexión establecida.", AlertType.Success);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Excepción en OnOpen: " + ex.Message);
                    }
                };

                ws.OnMessage += OnMessageReceived;

                ws.OnClose += (sender, e) =>
                {
                    Debug.Log("Conexión cerrada. Intentando reconectar...");
                    PanelManager.Instance?.uiAlertManager?.ShowAlert("Conexión cerrada. Intentando reconectar...", AlertType.Warning);
                    reconnectStartTime = Time.time;
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.LogError($"Error en WebSocket: {e.Message}");
                    PanelManager.Instance?.uiAlertManager?.ShowAlert("Error en WebSocket: " + e.Message, AlertType.Error);
                    reconnectStartTime = Time.time;
                };

                ws.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("Excepción en ConnectWebSocketRoutine: " + ex.Message);
                PanelManager.Instance?.uiAlertManager?.ShowAlert("Error al conectar: " + ex.Message, AlertType.Error);
            }
            yield return new WaitForSeconds(2f);
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                connected = true;
                Debug.Log("WebSocket conectado correctamente.");
            }
            else
            {
                Debug.LogWarning("La conexión no se pudo establecer.");
            }
            yield return new WaitForSeconds(5f);
        }
    }

    /// <summary>
    /// Coroutine para reconectar el WebSocket en caso de desconexión.
    /// </summary>
    private IEnumerator ReconnectWebSocketRoutine()
    {
        Debug.Log("Intentando reconectar...");
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Intentando reconectar...", AlertType.Info);
        bool reconnected = false;
        while (!reconnected)
        {
            try
            {
                ws = new WebSocket(serverURL);

                ws.OnOpen += (sender, e) =>
                {
                    try
                    {
                        Debug.Log("Reconexión establecida.");
                        PanelManager.Instance?.uiAlertManager?.ShowAlert("Reconexión establecida.", AlertType.Success);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Excepción en OnOpen (reconexión): " + ex.Message);
                    }
                };

                ws.OnMessage += OnMessageReceived;

                ws.OnClose += (sender, e) =>
                {
                    Debug.Log("Conexión cerrada nuevamente. Intentando reconectar...");
                    PanelManager.Instance?.uiAlertManager?.ShowAlert("Conexión cerrada nuevamente. Intentando reconectar...", AlertType.Warning);
                    reconnectStartTime = Time.time;
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.LogError($"Error en la reconexión: {e.Message}");
                    PanelManager.Instance?.uiAlertManager?.ShowAlert("Error en la reconexión: " + e.Message, AlertType.Error);
                    reconnectStartTime = Time.time;
                };

                ws.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("Excepción en ReconnectWebSocketRoutine: " + ex.Message);
                PanelManager.Instance?.uiAlertManager?.ShowAlert("Error en reconexión: " + ex.Message, AlertType.Error);
            }
            yield return new WaitForSeconds(2f);
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                reconnected = true;
                Debug.Log("WebSocket reconectado correctamente.");
            }
            else
            {
                Debug.LogWarning("Reconexión fallida.");
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private void ReconnectWebSocket()
    {
        StartCoroutine(ReconnectWebSocketRoutine());
    }
    #endregion

    #region Login
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginError;

    public void AttemptLogin(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Por favor, completa ambos campos.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Por favor, completa ambos campos.", AlertType.Warning);
            return;
        }

        if (ws == null || ws.ReadyState == WebSocketState.Closed)
        {
            StartCoroutine(ConnectWebSocketRoutine(username, password));
        }
        else if (ws.ReadyState == WebSocketState.Open)
        {
            StartCoroutine(DelayedSendLogin(username, password, 5f));
        }
    }

    private IEnumerator DelayedSendLogin(string username, string password, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            Debug.Log("Enviando credenciales después del retraso.");
            ws.Send($"LOGIN {username} {password}");
        }
        else
        {
            Debug.LogError("El WebSocket no está abierto tras el retraso.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Error: No se puede enviar el login. Conexión perdida.", AlertType.Error);
        }
    }

    private void HandleLoginSuccess(string payload)
    {
        string[] responseParts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (responseParts.Length >= 1)
        {
            string role = responseParts[0];
            Debug.Log($"Login exitoso. Rol: {role}");
            PanelManager.Instance.ConfigurePanels(role);
            OnLoginSuccess?.Invoke(role);
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Inicio de sesión exitoso.", AlertType.Success);
        }
    }

    private void HandleLoginError(string payload)
    {
        Debug.Log("Error de inicio de sesión: " + payload);
        OnLoginError?.Invoke(payload);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Error de inicio de sesión: " + payload, AlertType.Error);
    }
    #endregion

    #region Update
    private Queue<Action> mainThreadActions = new Queue<Action>();
    private void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }

        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.time - reconnectStartTime >= reconnectDelay)
        {
            ReconnectWebSocket();
        }
    }
    #endregion

    #region MessageHandling
    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        try
        {
            Debug.Log("Mensaje recibido: " + e.Data);
            mainThreadActions.Enqueue(() =>
            {
                string data = e.Data;
                bool handled = false;

                var commandMap = new (string prefix, Action<string> handler)[]
                {
                    ("LOGIN_SUCCESS",   HandleLoginSuccess),
                    ("LOGIN_ERROR",     HandleLoginError),
                    ("ROOMS_INFO:",     HandleRoomsInfo),
                    ("CONNECTED_USERS:",HandleConnectedUsers),
                    ("JOINED_ROOM",     HandleJoinedRoom),
                    ("MESSAGE",         HandleMessage),
                    ("OBJECT_DIRECT",   HandleObjectDirect),
                    ("FILE_DIRECT",     HandleFileDirect),
                    ("FILE_RECEIVED",   HandleFileReceived),
                    ("TYPING",          HandleTyping),
                    ("USER_DISCONNECTED",HandleUserDisconnected),
                };

                foreach (var (prefix, action) in commandMap)
                {
                    if (data.StartsWith(prefix))
                    {
                        string payload = data.Substring(prefix.Length).Trim();
                        action(payload);
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                {
                    Debug.Log("Mensaje no reconocido: " + e.Data);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error en OnMessageReceived: " + ex.Message);
        }
    }

    private void HandleRoomsInfo(string payload)
    {
        string roomList = payload.Trim();
        Debug.Log("Rooms Info received: " + roomList);
        PanelManager.Instance.ShowRoomList(roomList);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Información de salas recibida.", AlertType.Info);
    }

    private void HandleConnectedUsers(string payload)
    {
        string userList = payload.Trim();
        Debug.Log("Usuarios conectados recibidos: " + userList);
        PanelManager.Instance.ShowConnectedUsers(userList);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Información de usuarios conectados recibida.", AlertType.Info);
    }

    private void HandleJoinedRoom(string payload)
    {
        string joinedRoom = payload.Trim();
        Debug.Log("Se ha unido a la sala: " + joinedRoom);
        PanelManager.Instance.ShowChatPanel(joinedRoom);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Te has unido a la sala: " + joinedRoom, AlertType.Success);
    }

    private void HandleMessage(string payload)
    {
        string receivedMessage = payload;
        Debug.Log("Mensaje de chat: " + receivedMessage);
        if (receivedMessage.ToLowerInvariant().Contains("[sistema]:"))
        {
            Color messageColor = Color.white;
            if (receivedMessage.ToLowerInvariant().Contains("se ha unido a la sala"))
                messageColor = Color.green;
            else if (receivedMessage.ToLowerInvariant().Contains("se ha desconectado"))
                messageColor = Color.red;
            else if (receivedMessage.ToLowerInvariant().Contains("ahora está inactivo"))
                messageColor = Color.yellow;

            PanelManager.Instance.AppendSystemMessage(receivedMessage, messageColor);

            if (receivedMessage.ToLowerInvariant().Contains("ha sido eliminada"))
            {
                PanelManager.Instance.ShowMainMenu();
                PanelManager.Instance.CloseChatPanel();
            }
        }
        else
        {
            PanelManager.Instance.AppendChatMessage(receivedMessage);
        }
    }

    private void HandleObjectDirect(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogError("OBJECT_DIRECT: JSON recibido vacío.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Error: JSON para objeto vacío.", AlertType.Error);
            return;
        }
        string decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var wrapper = JsonConvert.DeserializeObject<MessageWrapper<ComplexObjectData>>(decodedJson);
        if (wrapper?.payload == null)
        {
            Debug.LogError("El JSON no tiene payload o wrapper es nulo");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Error: JSON no tiene payload.", AlertType.Error);
            return;
        }
        ComplexObjectData data = wrapper.payload;
        if (data.MeshData == null)
        {
            Debug.LogError("Error: MeshData es nulo.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Error: MeshData es nulo.", AlertType.Error);
            return;
        }
        ObjectManager.Instance.InstantiateComplexObject(data);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Objeto recibido.", AlertType.Success);
    }

    private void HandleTyping(string payload)
    {
        string typingUser = payload.Trim();
        Debug.Log($"{typingUser} está escribiendo...");
        PanelManager.Instance.ShowTypingIndicator(typingUser);
    }

    private void HandleUserDisconnected(string payload)
    {
        string disconnectedUser = payload.Trim();
        Debug.Log($"{disconnectedUser} se ha desconectado.");
        ChatClient.Instance.HandleUserDisconnected(disconnectedUser);
        PanelManager.Instance?.uiAlertManager?.ShowAlert($"{disconnectedUser} se ha desconectado.", AlertType.Warning);
    }

    // --- Manejo de archivos ---
    private void HandleFileDirect(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogError("ERROR: JSON para archivo está vacío.");
            PanelManager.Instance?.uiAlertManager?.ShowAlert("ERROR: JSON para archivo vacío.", AlertType.Error);
            return;
        }

        try
        {
            Debug.Log($"Recibiendo archivo con datos: {payload}");
            FileChunk fileChunk = JsonConvert.DeserializeObject<FileChunk>(payload);
            if (fileChunk == null)
            {
                Debug.LogError("ERROR: FileChunk es nulo.");
                PanelManager.Instance?.uiAlertManager?.ShowAlert("ERROR: FileChunk es nulo.", AlertType.Error);
                return;
            }

            Debug.Log($"Recibiendo fragmento de archivo: {fileChunk.FileName}, Tipo: {fileChunk.FileType}, Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks}");

            string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\";
            string folderPath = System.IO.Path.Combine(downloadsFolder, "InsituChatApp");
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
            string savePath = System.IO.Path.Combine(folderPath, fileChunk.FileName);

            // Aquí se podría acumular los fragmentos si se enviaran de a poco.
            // En este ejemplo, se asume que el mensaje FILE_DIRECT contiene la información de un fragmento individual.
            // Se podría implementar un tracker similar al del servidor para reconstruir el archivo.

            // Por simplicidad, mostramos alerta (esto es opcional)
            Debug.Log($"Fragmento recibido para el archivo {fileChunk.FileName}.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al procesar archivo: " + ex.Message);
            PanelManager.Instance?.uiAlertManager?.ShowAlert("Error al procesar archivo: " + ex.Message, AlertType.Error);
        }
    }

    private void HandleFileReceived(string payload)
    {
        Debug.Log("Archivo recibido (HandleFileReceived): " + payload);
        PanelManager.Instance?.uiAlertManager?.ShowAlert("Archivo recibido con éxito.", AlertType.Success);
    }
    #endregion
}
