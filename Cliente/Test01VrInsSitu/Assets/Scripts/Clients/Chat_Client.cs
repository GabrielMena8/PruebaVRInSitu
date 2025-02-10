using UnityEngine;
using System;
using System.Collections.Generic;
using WebSocketSharp;

public class ChatClient : MonoBehaviour
{
    public static ChatClient Instance;
    private Queue<Action> mainThreadActions = new Queue<Action>();

    [Header("Referencias a otros componentes")]
    public CameraNavigator cameraNavigator;    // Se usa para actualizar la UI de la cámara, etc.
    public PanelManager dynamicPanelManager;     // Se usa para actualizar el panel principal (menú, chat, etc.)
    public Login loginManager;                   // Referencia al script de Login para ocultar la pantalla de login

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Suscribirse a los eventos de autenticación que se disparan desde AuthManager
        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnLoginError += HandleLoginError;
    }

    private void Update()
    {
        // Procesar las acciones pendientes en el hilo principal
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    private void HandleLoginSuccess(string role)
    {
        // Cuando el login es exitoso, se actualiza la UI: se oculta el panel de login y se configura el menú principal.
        mainThreadActions.Enqueue(() =>
        {
            loginManager.loginPanel.SetActive(false);
            dynamicPanelManager.SetRole(role);
            cameraNavigator.SetLoggedIn(true);
        });
    }

    private void HandleLoginError(string error)
    {
        // Se muestra el error en la consola o se actualiza la UI para informar al usuario.
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log("Error de inicio de sesión: " + error);
        });
    }

    public void SendTypingStatus()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send("TYPING");
        }
        else
        {
            Debug.LogError("No se puede enviar estado TYPING. El WebSocket no está conectado.");
        }
    }

    #region Métodos para Enviar Comandos

    public void CreateRoom(string roomName)
    {
        Debug.Log($"Intentando crear sala: {roomName}");
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"CREATE_ROOM {roomName}");
        }
        else
        {
            Debug.LogError("No se puede crear la sala. El WebSocket no está conectado.");
        }
    }

    public void DeleteRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"DELETE_ROOM {roomName}");
        }
    }

    public void DeleteUser(string username)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"DELETE_USER {username}");
        }
    }

    public void ViewRooms()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send("VIEW_ROOMS");
        }
    }

    public void ViewConnectedUsers()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send("VIEW_CONNECTED");
        }
    }

    // NUEVO: Método para unirse a una sala.
    public void JoinRoom(string roomName)
    {
        Debug.Log($"Intentando unirse a sala: {roomName}");
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"JOIN_ROOM {roomName}");
        }
        else
        {
            Debug.LogError("No se puede unir a la sala. El WebSocket no está conectado.");
        }
    }

    // NUEVO: Método para enviar un mensaje a la sala a la que se ha unido.
    public void SendMessageToRoom(string message)
    {
        Debug.Log($"Enviando mensaje: {message}");
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"MESSAGE {message}");
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. El WebSocket no está conectado.");
        }
    }

    #endregion
}
