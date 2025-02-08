using UnityEngine;
using WebSocketSharp;

public class ChatClient : MonoBehaviour
{
    private WebSocket ws;
    private float reconnectDelay = 5f;
    private float reconnectStartTime;
    public CameraNavigator cameraNavigator;  // Referencia al navegador de cámara para la UI
    public PanelConstellationManager dynamicPanelManager;  // Referencia al administrador de paneles dinámicos
    public LoginManager loginManager;  // Referencia al administrador de login

    // Método para intentar el login
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
        else
        {
            Debug.Log("WebSocket está en estado de conexión. Por favor, espera...");
        }
    }

    // Método para conectar el WebSocket y enviar credenciales
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
            Debug.Log($"Conexión cerrada. Intentando reconectar...");
            reconnectStartTime = Time.time;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en WebSocket: {e.Message}");
            reconnectStartTime = Time.time;
        };

        ws.ConnectAsync();
    }

    // Método para manejar los mensajes recibidos del WebSocket
    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log("Mensaje recibido: " + e.Data);

        if (e.Data.StartsWith("LOGIN_SUCCESS"))
        {
            string[] responseParts = e.Data.Split(' ');
            string role = responseParts[1];
            Debug.Log($"Login exitoso. Rol: {role}");

            loginManager.loginPanel.SetActive(false);
            dynamicPanelManager.SetRole(role);
            cameraNavigator.SetLoggedIn(true);
        }
        else if (e.Data.StartsWith("LOGIN_ERROR"))
        {
            Debug.Log("Error de inicio de sesión: " + e.Data);
        }
        else if (e.Data.StartsWith("MESSAGE"))
        {
            string receivedMessage = e.Data.Substring(8);
            Debug.Log($"Mensaje de chat: {receivedMessage}");
            // Puedes manejar los mensajes aquí (por ejemplo, agregarlo a un chat visual)
        }
    }

    // Método para actualizar el estado del WebSocket y manejar la reconexión
    private void Update()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.time - reconnectStartTime >= reconnectDelay)
        {
            ReconnectWebSocket();
        }
    }

    // Método para reconectar el WebSocket
    private void ReconnectWebSocket()
    {
        Debug.Log("Intentando reconectar...");
        ws = new WebSocket("ws://127.0.0.1:8080/chat");
        ConnectWebSocketForReconnect();
    }

    // Método para configurar el WebSocket para la reconexión
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

    // Método para cerrar el WebSocket al salir de la aplicación
    private void OnApplicationQuit()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
    }
}