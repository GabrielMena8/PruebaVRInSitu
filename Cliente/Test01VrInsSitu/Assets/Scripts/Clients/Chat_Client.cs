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

    // Singleton Instance
    public static ChatClient Instance;

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
        // Suscribirse a los eventos de autenticación
        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnLoginError += HandleLoginError;

        // Inicializar WebSocket
        InitializeWebSocket();
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
    /// Inicializar el WebSocket.
    /// </summary>
    private void InitializeWebSocket()
    {
        ws = new WebSocket("ws://127.0.0.1:8080/chat");

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Conexión WebSocket establecida.");
        };

        ws.OnMessage += OnMessageReceived;

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Conexión WebSocket cerrada.");
            reconnectStartTime = Time.time;
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"Error en la conexión WebSocket: {e.Message}");
            reconnectStartTime = Time.time;
        };

        ws.ConnectAsync();
    }

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
        mainThreadActions.Enqueue(() =>
        {
            loginManager.loginPanel.SetActive(false);
            dynamicPanelManager.SetRole(role);
            cameraNavigator.SetLoggedIn(true);
        });
    }

    /// <summary>
    /// Manejar el error del inicio de sesión.
    /// </summary>
    /// <param name="error">Mensaje de error</param>
    private void HandleLoginError(string error)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log("Error de inicio de sesión: " + error);
        });
    }

    /// <summary>
    /// Reconectar el WebSocket.
    /// </summary>
    private void ReconnectWebSocket()
    {
        Debug.Log("Intentando reconectar...");
        InitializeWebSocket();
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
            else if (e.Data.StartsWith("ROOMS_INFO"))
            {
                string roomList = e.Data.Substring("ROOMS_INFO:".Length).Trim();
                Debug.Log($"Lista de salas: {roomList}");
                PanelManager.Instance.ShowRoomList(roomList);
            }
            else if (e.Data.StartsWith("CONNECTED_USERS"))
            {
                string userList = e.Data.Substring("CONNECTED_USERS:".Length).Trim();
                Debug.Log($"Lista de usuarios conectados: {userList}");
                PanelManager.Instance.ShowConnectedUsers(userList);
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

    #region Admin Commands

    /// <summary>
    /// Crear una nueva sala.
    /// </summary>
    /// <param name="roomName">Nombre de la sala</param>
    public void CreateRoom(string roomName)
    {
        Debug.Log("Intentando crear sala: " + roomName);
        if (ws != null)
        {
            Debug.Log("Estado del WebSocket: " + ws.ReadyState);
        }

        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            Debug.Log("Enviando comando CREATE_ROOM al servidor");
            ws.Send($"CREATE_ROOM {roomName}");
        }
        else
        {
            Debug.LogError("No se puede crear la sala. El WebSocket no está conectado.");
        }
    }

    /// <summary>
    /// Eliminar una sala existente.
    /// </summary>
    /// <param name="roomName">Nombre de la sala</param>
    public void DeleteRoom(string roomName)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"DELETE_ROOM {roomName}");
        }
    }

    /// <summary>
    /// Eliminar un usuario.
    /// </summary>
    /// <param name="username">Nombre del usuario</param>
    public void DeleteUser(string username)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"DELETE_USER {username}");
        }
    }

    /// <summary>
    /// Ver todas las salas disponibles.
    /// </summary>
    public void ViewRooms()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            Debug.Log("Desde CC Rooms");
            ws.Send("VIEW_ROOMS");
        }
    }

    /// <summary>
    /// Ver usuarios conectados.
    /// </summary>
    public void ViewConnectedUsers()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send("VIEW_CONNECTED");
        }
    }

    #endregion
}