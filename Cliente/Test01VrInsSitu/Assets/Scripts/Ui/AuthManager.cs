using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

public class AuthManager : MonoBehaviour
{
    #region Singleton
    // Instancia singleton
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
    // WebSocket para la comunicaci�n
    private WebSocket ws;
    public WebSocket WS => ws; // Propiedad para que otros scripts puedan acceder al WebSocket

    // Variables de reconexi�n
    private float reconnectDelay = 5f;
    private float reconnectStartTime;

    // M�todo para conectar el WebSocket
    private void ConnectWebSocket(string username, string password)
    {
        ws = new WebSocket("ws://127.0.0.1:8080/chat");
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Conexi�n establecida. Enviando credenciales...");
            ws.Send($"LOGIN {username} {password}");
            // Notificar al usuario que la conexi�n fue exitosa
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Conexi�n establecida.", AlertType.Success);
        };
        ws.OnMessage += OnMessageReceived;
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Conexi�n cerrada. Intentando reconectar...");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Conexi�n cerrada. Intentando reconectar...", AlertType.Warning);
            reconnectStartTime = Time.time;
        };
        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en WebSocket: {e.Message}");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error en WebSocket: " + e.Message, AlertType.Error);
            reconnectStartTime = Time.time;
        };
        ws.ConnectAsync();
    }

    // M�todo para reconectar el WebSocket
    private void ReconnectWebSocket()
    {
        Debug.Log("Intentando reconectar...");
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Intentando reconectar...", AlertType.Info);
        ws = new WebSocket("ws://127.0.0.1:8080/chat");
        ConnectWebSocketForReconnect();
    }

    // M�todo para conectar el WebSocket durante la reconexi�n
    private void ConnectWebSocketForReconnect()
    {
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Reconexi�n establecida.");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Reconexi�n establecida.", AlertType.Success);
        };
        ws.OnMessage += OnMessageReceived;
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Conexi�n cerrada nuevamente. Intentando reconectar...");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Conexi�n cerrada nuevamente. Intentando reconectar...", AlertType.Warning);
            reconnectStartTime = Time.time;
        };
        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en la reconexi�n: {e.Message}");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error en la reconexi�n: " + e.Message, AlertType.Error);
            reconnectStartTime = Time.time;
        };
        ws.ConnectAsync();
    }
    #endregion

    #region Login
    // Eventos para el inicio de sesi�n
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginError;

    // M�todo para intentar iniciar sesi�n
    public void AttemptLogin(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Por favor, completa ambos campos.");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Por favor, completa ambos campos.", AlertType.Warning);
            return;
        }

        if (ws == null || ws.ReadyState == WebSocketState.Closed)
        {
            ConnectWebSocket(username, password);
        }
        else if (ws.ReadyState == WebSocketState.Open)
        {
            Debug.Log("WebSocket ya est� conectado. Enviando credenciales...");
            ws.Send($"LOGIN {username} {password}");
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
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Inicio de sesi�n exitoso.", AlertType.Success);
        }
    }

    private void HandleLoginError(string payload)
    {
        Debug.Log("Error de inicio de sesi�n: " + payload);
        OnLoginError?.Invoke(payload);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Error de inicio de sesi�n: " + payload, AlertType.Error);
    }
    #endregion

    #region Update
    // Cola de acciones para el hilo principal
    private Queue<Action> mainThreadActions = new Queue<Action>();

    // M�todo Update para manejar acciones en el hilo principal y reconexi�n de WebSocket
    private void Update()
    {
        // Ejecutar acciones en la cola del hilo principal
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }

        // Intentar reconectar si el WebSocket est� cerrado
        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.time - reconnectStartTime >= reconnectDelay)
        {
            ReconnectWebSocket();
        }
    }
    #endregion

    #region MessageHandling
    // M�todo para manejar mensajes recibidos del WebSocket
    private void OnMessageReceived(object sender, MessageEventArgs e)
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
                ("TYPING",          HandleTyping),
                ("USER_DISCONNECTED",HandleUserDisconnected),
            };

            // Buscar el comando adecuado para manejar el mensaje recibido
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
                // Opcional: podr�as mostrar una alerta informativa
                // if(PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                //     PanelManager.Instance.uiAlertManager.ShowAlert("Mensaje no reconocido.", AlertType.Info);
            }
        });
    }

    // M�todos de manejo para cada tipo de mensaje:
    private void HandleRoomsInfo(string payload)
    {
        string roomList = payload.Trim();
        Debug.Log("Rooms Info received: " + roomList);
        PanelManager.Instance.ShowRoomList(roomList);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Informaci�n de salas recibida.", AlertType.Info);
    }

    private void HandleConnectedUsers(string payload)
    {
        string userList = payload.Trim();
        Debug.Log("Usuarios conectados recibidos: " + userList);
        PanelManager.Instance.ShowConnectedUsers(userList);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Informaci�n de usuarios conectados recibida.", AlertType.Info);
    }

    private void HandleJoinedRoom(string payload)
    {
        string joinedRoom = payload.Trim();
        Debug.Log("Se ha unido a la sala: " + joinedRoom);
        PanelManager.Instance.ShowChatPanel(joinedRoom);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Te has unido a la sala: " + joinedRoom, AlertType.Success);
    }

    private void HandleMessage(string payload)
    {
        string receivedMessage = payload;
        Debug.Log("Mensaje de chat: " + receivedMessage);

        if (receivedMessage.Contains("[Sistema]:"))
        {
            if (receivedMessage.Contains("se ha unido a la sala"))
            {
                PanelManager.Instance.AppendSystemMessage(receivedMessage, Color.green);
            }
            else if (receivedMessage.Contains("se ha desconectado"))
            {
                PanelManager.Instance.AppendSystemMessage(receivedMessage, Color.red);
            }
            else if (receivedMessage.Contains("ahora est� inactivo"))
            {
                PanelManager.Instance.AppendSystemMessage(receivedMessage, Color.yellow);
            }

            if (receivedMessage.Contains("ha sido eliminada"))
            {
                PanelManager.Instance.AppendSystemMessage(receivedMessage, Color.red);
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
            Debug.LogError("OBJECT_DIRECT: JSON recibido vac�o.");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error: JSON para objeto vac�o.", AlertType.Error);
            return;
        }

        string decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

        var wrapper = JsonConvert.DeserializeObject<MessageWrapper<ComplexObjectData>>(decodedJson);
        if (wrapper?.payload == null)
        {
            Debug.LogError("El JSON no tiene payload o wrapper es nulo");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error: JSON no tiene payload.", AlertType.Error);
            return;
        }

        ComplexObjectData data = wrapper.payload;
        if (data.MeshData == null)
        {
            Debug.LogError("Error: MeshData es nulo.");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error: MeshData es nulo.", AlertType.Error);
            return;
        }

        ObjectManager.Instance.InstantiateComplexObject(data);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert("Objeto recibido.", AlertType.Success);
    }

    private void HandleTyping(string payload)
    {
        string typingUser = payload.Trim();
        Debug.Log($"{typingUser} est� escribiendo...");
        PanelManager.Instance.ShowTypingIndicator(typingUser);
        // Opcional: no se muestra alerta para typing
    }

    private void HandleUserDisconnected(string payload)
    {
        string disconnectedUser = payload.Trim();
        Debug.Log($"{disconnectedUser} se ha desconectado.");
        ChatClient.Instance.HandleUserDisconnected(disconnectedUser);
        if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
            PanelManager.Instance.uiAlertManager.ShowAlert($"{disconnectedUser} se ha desconectado.", AlertType.Warning);
    }
    #endregion

    #region FileHandling
    // Diccionario para rastrear los fragmentos de archivo
    private static Dictionary<string, List<string>> fileChunksInProgress = new Dictionary<string, List<string>>();

    // M�todo para manejar la recepci�n de fragmentos de archivo
    public void HandleFileDirect(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogError("ERROR: JSON para archivo est� vac�o.");
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("ERROR: JSON para archivo vac�o.", AlertType.Error);
            return;
        }

        try
        {
            Debug.Log($"Recibiendo archivo con datos: {payload}");

            // Deserializar el fragmento de archivo
            FileChunk fileChunk = JsonConvert.DeserializeObject<FileChunk>(payload);
            if (fileChunk == null)
            {
                Debug.LogError("ERROR: FileChunk es nulo.");
                if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                    PanelManager.Instance.uiAlertManager.ShowAlert("ERROR: FileChunk es nulo.", AlertType.Error);
                return;
            }

            Debug.Log($"Recibiendo fragmento de archivo: {fileChunk.FileName}, Tipo: {fileChunk.FileType}, Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks}");

            // Crear la ruta de guardado en la carpeta Descargas del usuario
            string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\";
            string folderPath = Path.Combine(downloadsFolder, "InsituChatApp");

            // Verificar si el directorio existe, si no, crearlo
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Ruta completa donde se guardar� el archivo
            string savePath = Path.Combine(folderPath, fileChunk.FileName);

            // Agregar los fragmentos y reconstruir el archivo si ya tenemos todos los fragmentos
            string transferKey = $"{fileChunk.FileName}_{fileChunk.CurrentChunk}";
            if (!fileChunksInProgress.ContainsKey(transferKey))
            {
                fileChunksInProgress[transferKey] = new List<string>();
            }

            // Agregar el fragmento recibido
            fileChunksInProgress[transferKey].Add(fileChunk.ContentBase64);

            // Verificar si hemos recibido todos los fragmentos
            if (fileChunksInProgress[transferKey].Count == fileChunk.TotalChunks)
            {
                // Reconstruir el archivo completo
                string fullContent = string.Join("", fileChunksInProgress[transferKey]);
                byte[] fileBytes = Convert.FromBase64String(fullContent);

                // Guardar el archivo en la ruta especificada
                System.IO.File.WriteAllBytes(savePath, fileBytes);
                Debug.Log($"Archivo completo recibido y guardado en: {savePath}");
                if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                    PanelManager.Instance.uiAlertManager.ShowAlert("Archivo recibido y guardado.", AlertType.Success);

                // Limpiar el diccionario de fragmentos una vez completado el archivo
                fileChunksInProgress.Remove(transferKey);

                // Actualizar el panel derecho con la vista previa
                PanelManager.Instance.UpdateRightPanelWithFilePreview(fileChunk, savePath);
            }
            else
            {
                Debug.Log($"Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks} recibido.");
                if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                    PanelManager.Instance.uiAlertManager.ShowAlert($"Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks} recibido.", AlertType.Info);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al procesar archivo: " + ex.Message);
            if (PanelManager.Instance != null && PanelManager.Instance.uiAlertManager != null)
                PanelManager.Instance.uiAlertManager.ShowAlert("Error al procesar archivo: " + ex.Message, AlertType.Error);
        }
    }
    #endregion
}
