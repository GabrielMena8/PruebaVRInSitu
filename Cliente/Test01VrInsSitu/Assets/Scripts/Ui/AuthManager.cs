using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System;

/// <summary>
/// Clase que maneja la autenticaci�n y la conexi�n WebSocket.
/// </summary>
public class AuthManager : MonoBehaviour
{
    private WebSocket ws;
    private float reconnectDelay = 5f;
    private float reconnectStartTime;

    // Singleton Instance
    public static AuthManager Instance;

    // Eventos de autenticaci�n
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginError;

    // Nueva cola para manejar acciones en el hilo principal
    private Queue<Action> mainThreadActions = new Queue<Action>();

    #region Unity Methods

    /// <summary>
    /// M�todo de Unity llamado al iniciar el script.
    /// </summary>
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

    /// <summary>
    /// M�todo de Unity llamado una vez por frame.
    /// Procesa las acciones de UI en el hilo principal.
    /// </summary>
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

    /// <summary>
    /// Intentar iniciar sesi�n con el nombre de usuario y la contrase�a proporcionados.
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contrase�a</param>
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
    }

    /// <summary>
    /// Conectar el WebSocket y enviar credenciales.
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contrase�a</param>
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
            Debug.Log("Conexi�n cerrada. Intentando reconectar...");
            reconnectStartTime = Time.time;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en WebSocket: {e.Message}");
            reconnectStartTime = Time.time;
        };

        ws.ConnectAsync();
    }

    /// <summary>
    /// Manejar los mensajes recibidos del WebSocket.
    /// </summary>
    /// <param name="sender">El remitente del mensaje</param>
    /// <param name="e">Datos del mensaje</param>
    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log("Mensaje recibido: " + e.Data);

        // A�adir acciones a la cola para procesarlas en el hilo principal
        mainThreadActions.Enqueue(() =>
        {
            if (e.Data.StartsWith("LOGIN_SUCCESS"))
            {
                string[] responseParts = e.Data.Split(' ');
                string role = responseParts[1];
                Debug.Log($"Login exitoso. Rol: {role}");
                InvokeOnLoginSuccess(role);
            }
            else if (e.Data.StartsWith("LOGIN_ERROR"))
            {
                Debug.Log("Error de inicio de sesi�n: " + e.Data);
                InvokeOnLoginError(e.Data);
            }
        });
    }

    /// <summary>
    /// Reconectar el WebSocket.
    /// </summary>
    private void ReconnectWebSocket()
    {
        Debug.Log("Intentando reconectar...");
        ws = new WebSocket("ws://127.0.0.1:8080/chat");
        ConnectWebSocketForReconnect();
    }

    /// <summary>
    /// Conectar el WebSocket para la reconexi�n.
    /// </summary>
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

    /// <summary>
    /// Invocar el evento de �xito de inicio de sesi�n.
    /// </summary>
    /// <param name="role">Rol del usuario</param>
    public void InvokeOnLoginSuccess(string role)
    {
        OnLoginSuccess?.Invoke(role);
    }

    /// <summary>
    /// Invocar el evento de error de inicio de sesi�n.
    /// </summary>
    /// <param name="error">Mensaje de error</param>
    public void InvokeOnLoginError(string error)
    {
        OnLoginError?.Invoke(error);
    }

    #endregion
}