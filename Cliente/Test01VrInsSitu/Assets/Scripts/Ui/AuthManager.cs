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
        public static AuthManager Instance;
        private WebSocket ws;
        public WebSocket WS => ws; // Propiedad para que otros scripts puedan acceder al WebSocket

        private float reconnectDelay = 5f;
        private float reconnectStartTime;
        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginError;
        private Queue<Action> mainThreadActions = new Queue<Action>();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

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

        public void AttemptLogin(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.Log("Por favor, completa ambos campos.");
                return;
            }

            if (ws == null || ws.ReadyState == WebSocketState.Closed)
            {
                ConnectWebSocket(username, password);
            }
            else if (ws.ReadyState == WebSocketState.Open)
            {
                Debug.Log("WebSocket ya está conectado. Enviando credenciales...");
                ws.Send($"LOGIN {username} {password}");
            }
        }

        private void ConnectWebSocket(string username, string password)
        {
            ws = new WebSocket("ws://127.0.0.1:8080/chat");
            ws.OnOpen += (sender, e) =>
            {
                Debug.Log("Conexión establecida. Enviando credenciales...");
                ws.Send($"LOGIN {username} {password}");
            };
            ws.OnMessage += OnMessageReceived;
            ws.OnClose += (sender, e) =>
            {
                Debug.Log("Conexión cerrada. Intentando reconectar...");
                reconnectStartTime = Time.time;
            };
            ws.OnError += (sender, e) =>
            {
                Debug.LogError($"Error en WebSocket: {e.Message}");
                reconnectStartTime = Time.time;
            };
            ws.ConnectAsync();
        }

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

                // Buscamos el comando adecuado para manejar el mensaje recibido
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


        // Métodos de manejo para cada tipo de mensaje:

        private void HandleLoginSuccess(string payload)
        {
            string[] responseParts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (responseParts.Length >= 1)
            {
                string role = responseParts[0];
                Debug.Log($"Login exitoso. Rol: {role}");
                PanelManager.Instance.ConfigurePanels(role);
                OnLoginSuccess?.Invoke(role);
            }
        }

        private void HandleLoginError(string payload)
        {
            Debug.Log("Error de inicio de sesión: " + payload);
            OnLoginError?.Invoke(payload);
        }

        private void HandleRoomsInfo(string payload)
        {
            string roomList = payload.Trim();
            Debug.Log("Rooms Info received: " + roomList);
            PanelManager.Instance.ShowRoomList(roomList);
        }
      private void HandleConnectedUsers(string payload)
        {
            string userList = payload.Trim();
            Debug.Log("Usuarios conectados recibidos: " + userList);

            // Actualiza la lista de usuarios conectados en PanelManager
            PanelManager.Instance.ShowConnectedUsers(userList);  // Aquí se actualiza la lista

        }

  

        private void HandleJoinedRoom(string payload)
        {
            string joinedRoom = payload.Trim();
            Debug.Log("Se ha unido a la sala: " + joinedRoom);
            PanelManager.Instance.ShowChatPanel(joinedRoom);
        }

    // En AuthManager, agrega un diccionario para rastrear los fragmentos de archivo.
    private static Dictionary<string, List<string>> fileChunksInProgress = new Dictionary<string, List<string>>();

    // Método para manejar la recepción de fragmentos de archivo
    public void HandleFileDirect(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogError("ERROR: JSON para archivo está vacío.");
            return;
        }

        try
        {
            Debug.Log($"Recibiendo archivo con datos: {payload}");

            // Deserializar el fragmento de archivo (usando FileChunk)
            FileChunk fileChunk = JsonConvert.DeserializeObject<FileChunk>(payload);
            if (fileChunk == null)
            {
                Debug.LogError("ERROR: FileChunk es nulo.");
                return;
            }

            Debug.Log($"Recibiendo fragmento de archivo: {fileChunk.FileName}, Tipo: {fileChunk.FileType}, Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks}");

            // Crear la ruta de guardado en la carpeta Descargas del usuario
            string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\";
            string folderPath = Path.Combine(downloadsFolder, "InsituChatApp");  // Subcarpeta "InsituChatApp" dentro de Descargas

            // Verificar si el directorio existe, si no, crearlo
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);  // Crea la subcarpeta si no existe
            }

            // Ruta completa donde se guardará el archivo
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

                // Limpiar el diccionario de fragmentos una vez completado el archivo
                fileChunksInProgress.Remove(transferKey);

                // Llamar a la función para actualizar el panel derecho con la vista previa
                PanelManager.Instance.UpdateRightPanelWithFilePreview(fileChunk, savePath);
            }
            else
            {
                Debug.Log($"Fragmento {fileChunk.CurrentChunk}/{fileChunk.TotalChunks} recibido.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al procesar archivo: " + ex.Message);
        }
    }


    private void HandleMessage(string payload)
        {
            string receivedMessage = payload; // e.Data ya le quitamos "MESSAGE"
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
                else if (receivedMessage.Contains("ahora está inactivo"))
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
                Debug.LogError("OBJECT_DIRECT: JSON recibido vacío.");
                return;
            }

            string decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

            var wrapper = JsonConvert.DeserializeObject<MessageWrapper<ComplexObjectData>>(decodedJson);
            if (wrapper?.payload == null)
            {
                Debug.LogError("El JSON no tiene payload o wrapper es nulo");
                return;
            }

            ComplexObjectData data = wrapper.payload;
            if (data.MeshData == null)
            {
                Debug.LogError("Error: MeshData es nulo.");
                return;
            }

            ObjectManager.Instance.InstantiateComplexObject(data);
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
        }

        private void ReconnectWebSocket()
        {
            Debug.Log("Intentando reconectar...");
            ws = new WebSocket("ws://127.0.0.1:8080/chat");
            ConnectWebSocketForReconnect();
        }

        private void ConnectWebSocketForReconnect()
        {
            ws.OnOpen += (sender, e) =>
            {
                Debug.Log("Reconexión establecida.");
            };
            ws.OnMessage += OnMessageReceived;
            ws.OnClose += (sender, e) =>
            {
                Debug.Log("Conexión cerrada nuevamente. Intentando reconectar...");
                reconnectStartTime = Time.time;
            };
            ws.OnError += (sender, e) =>
            {
                Debug.LogError($"Error en la reconexión: {e.Message}");
                reconnectStartTime = Time.time;
            };
            ws.ConnectAsync();
        }
    }
