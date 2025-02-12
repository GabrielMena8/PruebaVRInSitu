using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using SFB;

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
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Escuchar eventos de AuthManager cuando hay �xito/error de login
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

    private void HandleLoginSuccess(string role)
    {
        mainThreadActions.Enqueue(() =>
        {
            loginManager.loginPanel.SetActive(false);
            dynamicPanelManager.SetRole(role);
            cameraNavigator.SetLoggedIn(true);
        });
    }

    private void HandleLoginError(string error)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log("Error de inicio de sesi�n: " + error);
        });
    }

    public void HandleUserDisconnected(string username)
    {
        mainThreadActions.Enqueue(() =>
        {
            Debug.Log($"{username} se ha desconectado.");
            PanelManager.Instance.RemoveUserFromUI(username);
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
                isTyping = true;
            }
            else
            {
                Debug.LogError("No se puede enviar estado TYPING. El WebSocket no est� conectado.");
            }
        }
    }

    #region M�todos para Enviar Comandos (Salas, Usuarios, Mensajes)

    public void CreateRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"CREATE_ROOM {roomName}");
        else
            Debug.LogError("No se puede crear la sala. El WebSocket no est� conectado.");
    }

    public void DeleteRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"DELETE_ROOM {roomName}");
    }

    public void DeleteUser(string username)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"DELETE_USER {username}");
    }

    public void ViewRooms()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send("VIEW_ROOMS");
    }

    public void ViewConnectedUsers()
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send("VIEW_CONNECTED");
    }

    public void JoinRoom(string roomName)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Send($"JOIN_ROOM {roomName}");
        else
            Debug.LogError("No se puede unir a la sala. El WebSocket no est� conectado.");
    }

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
            Debug.LogError("No se puede enviar el mensaje. El WebSocket no est� conectado.");
        }
    }
    #endregion

    #region Env�o de Objetos 3D

    /// <summary>
    /// Se llama cuando se hace clic en un objeto 3D.
    /// </summary>
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
            Debug.LogError("OnObjectClicked: Fall� al obtener los datos del objeto.");
            return;
        }

        // Enviar con el wrapper "OBJECT_COMPLEX" + payload
        // O si prefieres, remove el wrapper y haz: JsonConvert.SerializeObject(data)
        string objectDataJson = UniversalSerializer.CreateUniversalMessage("OBJECT_COMPLEX", data);
        // Codificar a Base64
        string encodedJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(objectDataJson));
        Debug.Log("Objeto serializado en Base64: " + encodedJson);

        // Obtener la lista de usuarios conectados para mostrar el men� contextual
        List<string> connectedUsers = PanelManager.Instance.GetConnectedUsernames();
        if (connectedUsers == null || connectedUsers.Count == 0)
        {
            Debug.LogWarning("OnObjectClicked: No hay usuarios conectados. Solicitando actualizaci�n...");
            ViewConnectedUsers();
            StartCoroutine(RetryOnObjectClicked(obj));
            return;
        }

        // Convertir la posici�n del objeto a coordenadas de pantalla para colocar el men�
        Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
        float desiredZ = screenPos.z;

        // Mostrar el men� contextual con la lista de usuarios
        PanelManager.Instance.ShowContextMenu(connectedUsers, (selectedUser) =>
        {
            // Enviar el objeto (encodedJson) al usuario seleccionado
            SendObjectToUser(selectedUser, encodedJson);
        }, screenPos, desiredZ);
    }

    /// <summary>
    /// Reintenta mostrar el men� contextual tras un breve retraso, en caso de que no hubiera usuarios.
    /// </summary>
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
            Debug.LogWarning("RetryOnObjectClicked: Todav�a no hay usuarios conectados tras el retraso.");
        }
    }

    /// <summary>
    /// Env�a el objeto serializado (encodedJson) al usuario destino.
    /// </summary>
    public void SendObjectToUser(string targetUser, string encodedJson)
    {
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            // Env�a el mensaje con el comando SEND_OBJECT
            ws.Send($"SEND_OBJECT {targetUser} {encodedJson}");
            Debug.Log("Objeto enviado a " + targetUser);
        }
        else
        {
            Debug.LogError("No se puede enviar el objeto. El WebSocket no est� conectado.");
        }
    }





    public void SendFileToRoom(string roomName, string filePath)
    {
        // Lee el archivo
        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        string base64Content = Convert.ToBase64String(fileBytes);

        // Crea un FileData (o la clase que uses)
        FileData fileData = new FileData
        {
            FileName = System.IO.Path.GetFileName(filePath),
            FileType = "application/octet-stream",  // o "image/png", etc.
            ContentBase64 = base64Content
        };

        // Serializa a JSON
        string fileDataJson = JsonConvert.SerializeObject(fileData);

        // Env�as con un comando "SEND_FILE_ROOM"
        // (Necesitas que tu servidor maneje "SEND_FILE_ROOM {roomName} {fileDataJson}" y 
        // reenv�e "FILE_DIRECT" a todos en la sala. O la forma que hayas definido)
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"SEND_FILE_ROOM {roomName} {fileDataJson}");
            Debug.Log($"Archivo [{fileData.FileName}] enviado a la sala {roomName}");
        }
        else
        {
            Debug.LogError("No se puede enviar el archivo. El WebSocket no est� conectado.");
        }
    }

    public void SendFilesToRoom(List<string> filePaths)
    {
        // Aqu� debes obtener la sala actual; en este ejemplo se usa un valor fijo
        string roomName = "SalaActual"; // Reemplaza con tu l�gica real

        foreach (string filePath in filePaths)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string base64Content = Convert.ToBase64String(fileBytes);

            FileData fileData = new FileData
            {
                FileName = System.IO.Path.GetFileName(filePath),
                FileType = "application/octet-stream",  // Ajusta seg�n la extensi�n
                ContentBase64 = base64Content
            };

            string fileDataJson = JsonConvert.SerializeObject(fileData);
            WebSocket ws = AuthManager.Instance.WS;
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                ws.Send($"SEND_FILE_ROOM {roomName} {fileDataJson}");
                Debug.Log($"Archivo [{fileData.FileName}] enviado a la sala {roomName}");
            }
            else
            {
                Debug.LogError("No se puede enviar el archivo. El WebSocket no est� conectado.");
            }
        }
    }


    private void SendFileToUser(string targetUser, string filePath)
    {
        // Lee el archivo
        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        string base64Content = Convert.ToBase64String(fileBytes);
        // Crea un FileData (o la clase que uses)
        FileData fileData = new FileData
        {
            FileName = System.IO.Path.GetFileName(filePath),
            FileType = "application/octet-stream",  // o "image/png", etc.
            ContentBase64 = base64Content
        };
        // Serializa a JSON
        string fileDataJson = JsonConvert.SerializeObject(fileData);
        // Env�as con un comando "SEND_FILE_USER"
        // (Necesitas que tu servidor maneje "SEND_FILE_USER {targetUser} {fileDataJson}" y 
        // reenv�e "FILE_DIRECT" al usuario. O la forma que hayas definido)
        WebSocket ws = AuthManager.Instance.WS;
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send($"SEND_FILE_USER {targetUser} {fileDataJson}");
            Debug.Log($"Archivo [{fileData.FileName}] enviado a {targetUser}");
        }
        else
        {
            Debug.LogError("No se puede enviar el archivo. El WebSocket no est� conectado.");
        }
    }




    /// <summary>
    /// Serializa un objeto 3D de Unity (Mesh, Material, Transform) a ComplexObjectData
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
