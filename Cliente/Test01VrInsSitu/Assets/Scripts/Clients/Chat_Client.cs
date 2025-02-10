using UnityEngine;
using System;
using System.Collections.Generic;
using WebSocketSharp;
using System.Collections;

public class ChatClient : MonoBehaviour
{
    public static ChatClient Instance;
    private Queue<Action> mainThreadActions = new Queue<Action>();

    [Header("Referencias a otros componentes")]
    public CameraNavigator cameraNavigator;    // Se usa para actualizar la UI de la cámara, etc.
    public PanelManager dynamicPanelManager;     // Se usa para actualizar el panel principal (menú, chat, etc.)
    public Login loginManager;                   // Referencia al script de Login para ocultar la pantalla de login

    private bool isTyping;
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
        if (!isTyping)
        {
            WebSocket ws = AuthManager.Instance.WS;
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                ws.Send("TYPING");
                isTyping = true;  // Marca como que ya está escribiendo
            }
            else
            {
                Debug.LogError("No se puede enviar estado TYPING. El WebSocket no está conectado.");
            }
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

    public void HandleUserDisconnected(string username)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log($"{username} se ha desconectado.");
            PanelManager.Instance.RemoveUserFromUI(username);  // Método para eliminar al usuario de la UI
        });
    }

    // NUEVO: Método para enviar un mensaje a la sala a la que se ha unido.
    public void SendMessageToRoom(string message)
    {
        Debug.Log($"Enviando mensaje: {message}");
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"MESSAGE {message}");
            isTyping = false;  // Reinicia el estado de escritura
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. El WebSocket no está conectado.");
        }
    }

    /// <summary>
    /// Envía el objeto (o sus datos) a un usuario específico.
    /// Se utiliza el comando "SEND_OBJECT" que el servidor procesará para enviar de forma dirigida.
    /// </summary>
    /// <param name="targetUser">Usuario destino</param>
    /// <param name="objectDataJson">Datos del objeto en JSON</param>
    public void SendObjectToUser(string targetUser, string objectDataJson)
    {
        string message = $"SEND_OBJECT {targetUser} {objectDataJson}";
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send(message);
            Debug.Log("Enviando objeto a " + targetUser + ": " + message);
        }
        else
        {
            Debug.LogError("No se puede enviar el objeto. El WebSocket no está conectado.");
        }
    }

    #endregion



    public void OnObjectClicked(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("OnObjectClicked: El objeto es nulo.");
            return;
        }

        // Obtener los datos del objeto
        ComplexObjectData data = GetComplexObjectData(obj);
        if (data == null)
        {
            Debug.LogError("OnObjectClicked: Falló al obtener los datos del objeto.");
            return;
        }

        // Serializar la información del objeto en un mensaje universal
        string objectDataJson = UniversalSerializer.CreateUniversalMessage("OBJECT_COMPLEX", data);

        // Consultar la lista de usuarios conectados
        List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
        if (connectedUsers == null || connectedUsers.Count == 0)
        {
            Debug.LogWarning("OnObjectClicked: No hay usuarios conectados. Solicitando actualización...");
            // Solicitar la actualización de la lista
            ViewConnectedUsers();
            // Reintentar el envío después de un breve retraso
            StartCoroutine(RetryOnObjectClicked(obj, objectDataJson));
            return;
        }

        // Si hay usuarios, mostrar el menú contextual
        PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
        {
            SendObjectToUser(selectedUser, objectDataJson);
        }, Input.mousePosition);
    }

    /// <summary>
    /// Coroutine que reintenta la acción de OnObjectClicked después de un breve retraso.
    /// </summary>
    private IEnumerator RetryOnObjectClicked(GameObject obj, string objectDataJson)
    {
        // Espera 0.5 segundos (ajusta el tiempo según tus necesidades)
        yield return new WaitForSeconds(0.5f);

        // Vuelve a obtener la lista actualizada de usuarios conectados
        List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
        if (connectedUsers != null && connectedUsers.Count > 0)
        {
            Debug.Log("RetryOnObjectClicked: Lista actualizada: " + string.Join(", ", connectedUsers));
            PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
            {
                SendObjectToUser(selectedUser, objectDataJson);
            }, Input.mousePosition);
        }
        else
        {
            Debug.LogWarning("RetryOnObjectClicked: Todavía no hay usuarios conectados tras el retraso.");
        }
    }




    /// <summary>
    /// Función auxiliar para empaquetar la información de un objeto en un ComplexObjectData.
    /// </summary>
    private ComplexObjectData GetComplexObjectData(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        Renderer renderer = obj.GetComponent<Renderer>();

        if (meshFilter == null)
        {
            Debug.LogError("El objeto no tiene MeshFilter.");
            return null;
        }

        ComplexObjectData data = new ComplexObjectData();
        data.ObjectName = obj.name;
        data.Position = obj.transform.position;
        data.Rotation = obj.transform.rotation;
        data.Scale = obj.transform.localScale;

        Mesh mesh = meshFilter.mesh;
        SerializableMesh sMesh = new SerializableMesh();
        Vector3[] vertices = mesh.vertices;
        sMesh.Vertices = new float[vertices.Length * 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            sMesh.Vertices[i * 3] = vertices[i].x;
            sMesh.Vertices[i * 3 + 1] = vertices[i].y;
            sMesh.Vertices[i * 3 + 2] = vertices[i].z;
        }
        sMesh.Triangles = mesh.triangles;
        data.MeshData = sMesh;

        if (renderer != null && renderer.material != null)
        {
            SerializableMaterial sMat = new SerializableMaterial();
            sMat.ShaderName = renderer.material.shader.name;
            sMat.Color = renderer.material.color;
            data.MaterialData = sMat;
        }
        return data;
    }
}
