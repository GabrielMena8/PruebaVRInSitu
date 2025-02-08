using UnityEngine;
using WebSocketSharp;

public class ChatClient : MonoBehaviour
{
    private WebSocket ws;
    private float reconnectDelay = 5f;
    private float reconnectStartTime;
    public CameraNavigator cameraNavigator;  // Referencia al navegador de c�mara para la UI
    public PanelConstellationManager dynamicPanelManager;  // Referencia al administrador de paneles din�micos
    public LoginManager loginManager;  // Referencia al administrador de login

    // M�todo para intentar el login
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
            Debug.Log("WebSocket ya est� conectado. Enviando credenciales...");
            ws.Send($"LOGIN {username} {password}");
        }
        else
        {
            Debug.Log("WebSocket est� en estado de conexi�n. Por favor, espera...");
        }
    }

    // M�todo para conectar el WebSocket y enviar credenciales
    private void ConnectWebSocket(string username, string password)
    {
        ws = new WebSocket("ws://127.0.0.1:8080/chat");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Conexi�n establecida. Enviando credenciales...");
            ws.Send($"LOGIN {username} {password}");
        };

        ws.OnMessage += OnMessageReceived;
        ws.OnClose += (sender, e) =>
        {
            Debug.Log($"Conexi�n cerrada. Intentando reconectar...");
            reconnectStartTime = Time.time;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en WebSocket: {e.Message}");
            reconnectStartTime = Time.time;
        };

        ws.ConnectAsync();
    }

    // M�todo para manejar los mensajes recibidos del WebSocket
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
            Debug.Log("Error de inicio de sesi�n: " + e.Data);
        }
        else if (e.Data.StartsWith("MESSAGE"))
        {
            string receivedMessage = e.Data.Substring(8);
            Debug.Log($"Mensaje de chat: {receivedMessage}");
            // Puedes manejar los mensajes aqu� (por ejemplo, agregarlo a un chat visual)
        }
    }

    // M�todo para actualizar el estado del WebSocket y manejar la reconexi�n
    private void Update()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.time - reconnectStartTime >= reconnectDelay)
        {
            ReconnectWebSocket();
        }
    }

    // M�todo para reconectar el WebSocket
    private void ReconnectWebSocket()
    {
        Debug.Log("Intentando reconectar...");
        ws = new WebSocket("ws://127.0.0.1:8080/chat");
        ConnectWebSocketForReconnect();
    }

    // M�todo para configurar el WebSocket para la reconexi�n
    private void ConnectWebSocketForReconnect()
    {
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Reconexi�n establecida.");
        };

        ws.OnMessage += OnMessageReceived;
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Conexi�n cerrada nuevamente. Intentando reconectar...");
            reconnectStartTime = Time.time;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en la reconexi�n: {e.Message}");
            reconnectStartTime = Time.time;
        };

        ws.ConnectAsync();
    }

    // M�todo para cerrar el WebSocket al salir de la aplicaci�n
    private void OnApplicationQuit()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
    }
}