using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

/// <summary>
/// Clase que maneja la conexión del cliente de chat y la autenticación.
/// </summary>
public class ChatClient : MonoBehaviour
{
    // WebSocket para la conexión del chat
    private WebSocket ws;
    private float reconnectDelay = 5f;
    private float reconnectStartTime;

    // Nueva cola para manejar acciones en el hilo principal
    private Queue<Action> mainThreadActions = new Queue<Action>();

    // Referencias a otros componentes
    [Header("Referencias a otros componentes")]
    public CameraNavigator cameraNavigator;  // Referencia al navegador de cámara para la UI
    public PanelManager dynamicPanelManager;  // Referencia al administrador de paneles dinámicos
    public Login loginManager;  // Referencia al administrador de login

    #region Unity Methods

    /// <summary>
    /// Método de Unity llamado al iniciar el script.
    /// </summary>
    private void Start()
    {
        // Suscribirse a los eventos de autenticación
        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnLoginError += HandleLoginError;
    }

    /// <summary>
    /// Método de Unity llamado una vez por frame.
    /// Procesa las acciones de la cola en el hilo principal.
    /// </summary>
    private void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }

        // Intentar reconectar si la conexión está cerrada
        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.time - reconnectStartTime >= reconnectDelay)
        {
            ReconnectWebSocket();
        }
    }

    /// <summary>
    /// Método de Unity llamado al cerrar la aplicación.
    /// </summary>
    private void OnApplicationQuit()
    {
        // Cerrar la conexión WebSocket si está abierta
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
    }

    #endregion

    #region WebSocket Methods

    /// <summary>
    /// Intentar iniciar sesión con el nombre de usuario y la contraseña proporcionados.
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    public void AttemptLogin(string username, string password)
    {
        AuthManager.Instance.AttemptLogin(username, password);
    }

    /// <summary>
    /// Manejar el éxito del inicio de sesión.
    /// </summary>
    /// <param name="role">Rol del usuario</param>
    private void HandleLoginSuccess(string role)
    {
        loginManager.loginPanel.SetActive(false);
        dynamicPanelManager.SetRole(role);
        cameraNavigator.SetLoggedIn(true);
    }

    /// <summary>
    /// Manejar el error del inicio de sesión.
    /// </summary>
    /// <param name="error">Mensaje de error</param>
    private void HandleLoginError(string error)
    {
        Debug.Log("Error de inicio de sesión: " + error);
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
    /// Conectar el WebSocket para la reconexión.
    /// </summary>
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

    /// <summary>
    /// Manejar los mensajes recibidos del WebSocket.
    /// </summary>
    /// <param name="sender">El remitente del mensaje</param>
    /// <param name="e">Datos del mensaje</param>
    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log("Mensaje recibido: " + e.Data);

        // Añadir la acción a la cola para procesarla en el hilo principal
        mainThreadActions.Enqueue(() =>
        {
            if (e.Data.StartsWith("LOGIN_SUCCESS"))
            {
                string[] responseParts = e.Data.Split(' ');
                string role = responseParts[1];
                Debug.Log($"Login exitoso. Rol: {role}");

                AuthManager.Instance.InvokeOnLoginSuccess(role);
            }
            else if (e.Data.StartsWith("LOGIN_ERROR"))
            {
                Debug.Log("Error de inicio de sesión: " + e.Data);
                AuthManager.Instance.InvokeOnLoginError(e.Data);
            }
            else if (e.Data.StartsWith("MESSAGE"))
            {
                string receivedMessage = e.Data.Substring(8);
                Debug.Log($"Mensaje de chat: {receivedMessage}");
                // Aquí puedes manejar los mensajes de chat y actualizar la UI
            }
        });
    }

    #endregion
}
