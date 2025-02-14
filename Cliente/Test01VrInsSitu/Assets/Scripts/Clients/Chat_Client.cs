using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using SFB;
using System.IO;

public class ChatClient : MonoBehaviour
{
    public static ChatClient Instance;
    private Queue<Action> mainThreadActions = new Queue<Action>();
    private bool isTyping;

    [Header("Referencias a otros componentes")]
    public CameraNavigator cameraNavigator;
    public PanelManager dynamicPanelManager;
    public Login loginManager;

    private void Awake()
    {
        // Patrón Singleton para asegurar que solo exista una instancia de ChatClient
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Escuchar eventos de AuthManager para éxito/error de inicio de sesión
        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnLoginError += HandleLoginError;
    }

    private void Update()
    {
        // Ejecutar acciones pendientes en el hilo principal
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    #region Manejo de Login

    // Manejar inicio de sesión exitoso
    private void HandleLoginSuccess(string role)
    {
        mainThreadActions.Enqueue(() =>
        {
            loginManager.loginPanel.SetActive(false);
            dynamicPanelManager.SetRole(role);
            cameraNavigator.SetLoggedIn(true);
        });
    }

    // Manejar error de inicio de sesión
    private void HandleLoginError(string error)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log("Error de inicio de sesión: " + error);
        });
    }

    #endregion

    #region Manejo de Usuarios

    // Manejar desconexión de usuario
    public void HandleUserDisconnected(string username)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log($"{username} se ha desconectado.");
            PanelManager.Instance.RemoveUserFromUI(username);
        });
    }

    // Enviar estado de escritura al servidor
    public void SendTypingStatus()
    {
        if (!isTyping)
        {
            WebSocket ws = AuthManager.Instance.WS;
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                ws.Send("TYPING");
                isTyping = true;
            }
            else
            {
                Debug.LogError("No se puede enviar estado TYPING. El WebSocket no está conectado.");
            }
        }
    }

    #endregion

    #region Métodos para Enviar Comandos (Salas, Usuarios, Mensajes)

    // Crear una nueva sala de chat
    public void CreateRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"CREATE_ROOM {roomName}");
        else
            Debug.LogError("No se puede crear la sala. El WebSocket no está conectado.");
    }

    // Eliminar una sala de chat existente
    public void DeleteRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"DELETE_ROOM {roomName}");
    }

    // Eliminar un usuario del chat
    public void DeleteUser(string username)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"DELETE_USER {username}");
    }

    // Ver la lista de salas de chat
    public void ViewRooms()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send("VIEW_ROOMS");
    }

    // Ver la lista de usuarios conectados
    public void ViewConnectedUsers()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send("VIEW_CONNECTED");
    }

    // Unirse a una sala de chat
    public void JoinRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"JOIN_ROOM {roomName}");
        else
            Debug.LogError("No se puede unir a la sala. El WebSocket no está conectado.");
    }

    // Enviar un mensaje a la sala de chat
    public void SendMessageToRoom(string message)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"MESSAGE {message}");
            isTyping = false;
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. El WebSocket no está conectado.");
        }
    }

    #endregion

    #region Envío de Objetos 3D

    // Manejar evento de clic en objeto
    public void OnObjectClicked(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("OnObjectClicked: El objeto es nulo.");
            return;
        }

        // Serializar el objeto en ComplexObjectData
        ComplexObjectData data = GetComplexObjectData(obj);
        if (data == null)
        {
            Debug.LogError("OnObjectClicked: Falló al obtener los datos del objeto.");
            return;
        }

        // Enviar con el envoltorio "OBJECT_COMPLEX" + payload
        string objectDataJson = UniversalSerializer.CreateUniversalMessage("OBJECT_COMPLEX", data);
        // Codificar a Base64
        string encodedJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(objectDataJson));
        Debug.Log("Objeto serializado en Base64: " + encodedJson);

        // Obtener la lista de usuarios conectados para mostrar el menú contextual
        List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
        if (connectedUsers == null || connectedUsers.Count == 0)
        {
            Debug.LogWarning("OnObjectClicked: No hay usuarios conectados. Solicitando actualización...");
            ViewConnectedUsers();
            StartCoroutine(RetryOnObjectClicked(obj));
            return;
        }

        // Convertir la posición del objeto a coordenadas de pantalla para colocar el menú
        Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
        float desiredZ = screenPos.z;

        // Mostrar el menú contextual con la lista de usuarios
        PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
        {
            // Enviar el objeto (encodedJson) al usuario seleccionado
            SendObjectToUser(selectedUser, encodedJson);
        }, screenPos, desiredZ);
    }

    // Reintentar evento de clic en objeto después de un retraso
    private IEnumerator RetryOnObjectClicked(GameObject obj)
    {
        yield return new WaitForSeconds(0.5f);

        List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
        if (connectedUsers != null && connectedUsers.Count > 0)
        {
            Debug.Log("RetryOnObjectClicked: Lista actualizada: " + string.Join(", ", connectedUsers));

            // Serializar el objeto nuevamente
            ComplexObjectData data = GetComplexObjectData(obj);
            if (data == null)
            {
                Debug.LogError("No se pudo obtener los datos del objeto.");
                yield break;
            }

            string serializedObjectData = UniversalSerializer.CreateUniversalMessage("OBJECT_COMPLEX", data);
            string encodedObjectData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(serializedObjectData));

            Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
            float desiredZ = screenPos.z;

            PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
            {
                SendObjectToUser(selectedUser, encodedObjectData);
            }, screenPos, desiredZ);
        }
        else
        {
            Debug.LogWarning("RetryOnObjectClicked: Todavía no hay usuarios conectados tras el retraso.");
        }
    }

    // Enviar el objeto a un usuario específico
    public void SendObjectToUser(string targetUser, string encodedJson)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            // Enviar el mensaje con el comando SEND_OBJECT
            ws.Send($"SEND_OBJECT {targetUser} {encodedJson}");
            Debug.Log("Objeto enviado a " + targetUser);
        }
        else
        {
            Debug.LogError("No se puede enviar el objeto. El WebSocket no está conectado.");
        }
    }

    #endregion

    #region Envío de Archivos

    // Enviar un archivo a un usuario específico
    public void SendFileToUser(string targetUser, string filePath)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);
        string base64Content = Convert.ToBase64String(fileBytes);

        int chunkSize = 1024 * 1024;  // 1MB por fragmento
        int totalChunks = (int)Math.Ceiling((double)base64Content.Length / chunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int startIndex = i * chunkSize;
            int length = Math.Min(chunkSize, base64Content.Length - startIndex);
            string chunk = base64Content.Substring(startIndex, length);

            // Crear el fragmento de archivo
            var fileChunk = new FileChunk
            {
                FileName = Path.GetFileName(filePath),
                ContentBase64 = chunk,
                TotalChunks = totalChunks,
                CurrentChunk = i + 1  // Comenzar desde 1
            };

            string fileChunkJson = JsonConvert.SerializeObject(fileChunk);
            WebSocket ws = AuthManager.Instance.WS;
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                ws.Send($"SEND_FILE_USER {targetUser} {fileChunkJson}");
            }
            else
            {
                Debug.LogError("No se puede enviar el archivo. El WebSocket no está conectado.");
                break;
            }

            Debug.Log($"Enviando fragmento {i + 1}/{totalChunks}.");
        }
    }

    // Obtener el tipo MIME de un archivo basado en su extensión
    private string GetMimeType(string filePath)
    {
        string extension = System.IO.Path.GetExtension(filePath).ToLower();
        switch (extension)
        {
            case ".pdf": return "application/pdf";
            case ".jpg": return "image/jpeg";
            case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            case ".mp4": return "video/mp4";
            case ".mp3": return "audio/mp3";
            case ".txt": return "text/plain";
            case ".zip": return "application/zip";
            default: return "application/octet-stream";  // Tipo genérico si la extensión no es reconocida
        }
    }

    #endregion

    #region Utilidades

    // Escapar caracteres especiales en una cadena JSON
    private string EscapeJsonString(string input)
    {
        // Reemplazar caracteres especiales como comillas y saltos de línea
        string escapedString = input.Replace("\\", "\\\\")
                                    .Replace("\"", "\\\"")
                                    .Replace("\n", "\\n")
                                    .Replace("\r", "\\r");

        // Eliminar espacios en blanco innecesarios (solo si no forman parte de la estructura)
        escapedString = string.Join(" ", escapedString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return escapedString;
    }

    // Obtener datos de objeto complejo desde un GameObject
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

        data.MeshData = SerializableMesh.FromMesh(meshFilter.mesh);

        if (renderer != null && renderer.material != null)
        {
            SerializableMaterial sMat = new SerializableMaterial();
            sMat.ShaderName = renderer.material.shader.name;
            sMat.Color = renderer.material.color;
            data.MaterialData = sMat;
        }
        return data;
    }

    #endregion
}
