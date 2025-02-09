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
