using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            if (e.Data.StartsWith("LOGIN_SUCCESS"))
            {
                string[] responseParts = e.Data.Split(' ');
                if (responseParts.Length > 1)
                {
                    string role = responseParts[1];
                    Debug.Log($"Login exitoso. Rol: {role}");
                    OnLoginSuccess?.Invoke(role);
                }
            }
            else if (e.Data.StartsWith("LOGIN_ERROR"))
            {
                Debug.Log("Error de inicio de sesión: " + e.Data);
                OnLoginError?.Invoke(e.Data);
            }
            else if (e.Data.StartsWith("ROOMS_INFO:"))
            {
                // Extraer la información de las salas y actualizar la UI.
                string roomList = e.Data.Substring("ROOMS_INFO:".Length).Trim();
                Debug.Log("Rooms Info received: " + roomList);
                PanelManager.Instance.ShowRoomList(roomList);
            }
            else if (e.Data.StartsWith("CONNECTED_USERS:"))
            {
                // Extraer la información de los usuarios conectados.
                string userList = e.Data.Substring("CONNECTED_USERS:".Length).Trim();
                Debug.Log("Connected Users Info received: " + userList);
                PanelManager.Instance.ShowConnectedUsers(userList);
            }

            else if (e.Data.StartsWith("JOINED_ROOM"))
            {
                // Extraer el nombre de la sala de la respuesta del servidor
                string joinedRoom = e.Data.Substring("JOINED_ROOM".Length).Trim();
                Debug.Log("Se ha unido a la sala: " + joinedRoom);

                // Aquí se llama a ShowChatPanel para mostrar el panel de chat
                PanelManager.Instance.ShowChatPanel(joinedRoom);
            }
            else if (e.Data.StartsWith("MESSAGE"))
            {
                string receivedMessage = e.Data.Substring("MESSAGE ".Length);
                Debug.Log("Mensaje de chat: " + receivedMessage);

                if (receivedMessage.Contains("[Sistema]:"))
                {
                    // Formatea el mensaje del sistema con colores según el tipo
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

                }

                if (receivedMessage.Contains("ha sido eliminada"))
                {
                    // Mostrar el mensaje en el chat y regresar a la pantalla principal
                    PanelManager.Instance.AppendSystemMessage(receivedMessage, Color.red);
                    PanelManager.Instance.ShowMainMenu();  // Método para volver al menú principal
                    PanelManager.Instance.CloseChatPanel();
                }
                else
                {
                    PanelManager.Instance.AppendChatMessage(receivedMessage);
                }


            }
            if (e.Data.StartsWith("OBJECT_DIRECT"))
            {
                // Extrae la parte JSON del mensaje
                string objectJson = e.Data.Substring("OBJECT_DIRECT".Length).Trim();

                // Deserializa el JSON en un objeto ComplexObjectData
                ComplexObjectData data = UniversalSerializer.Deserialize<ComplexObjectData>(objectJson);

                // Llama al método que instanciará el objeto en la escena
                ObjectManager.Instance.InstantiateComplexObject(data);

                Debug.Log("Objeto recibido e instanciado.");
            }

            // NUEVO: Manejo de la notificación de "escribiendo"
            else if (e.Data.StartsWith("TYPING"))
            {
                // Extraer el nombre del usuario que está escribiendo.
                string typingUser = e.Data.Substring("TYPING".Length).Trim();
                Debug.Log($"{typingUser} está escribiendo...");
                // Llamar a un método en PanelManager para actualizar el indicador.
                PanelManager.Instance.ShowTypingIndicator(typingUser);
            }

            else if (e.Data.StartsWith("USER_DISCONNECTED"))
            {
                string disconnectedUser = e.Data.Substring("USER_DISCONNECTED".Length).Trim();
                Debug.Log($"{disconnectedUser} se ha desconectado.");
                ChatClient.Instance.HandleUserDisconnected(disconnectedUser);
            }

            else
            {
                // Otros mensajes, se pueden manejar de la manera que prefieras.
                Debug.Log("Mensaje recibido: " + e.Data);
            }
        });
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
            // Opcional: reenviar LOGIN si fuera necesario
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
