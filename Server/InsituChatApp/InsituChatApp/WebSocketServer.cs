using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;

public enum UserStatus
{
    Active,
    Typing,
    Inactive
}

public class ChatRoom : WebSocketBehavior
{
    private static List<ChatRoomData> chatRooms = new List<ChatRoomData>();
    private static Dictionary<string, User> connectedUsers = new Dictionary<string, User>();
    private static System.Timers.Timer? inactivityTimer;

    private string userName;
    private string roomName;

    protected override void OnOpen()
    {
        if (inactivityTimer == null)
        {
            StartInactivityTimer();  // Iniciar el Timer solo la primera vez
        }
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            string[] messageParts = e.Data.Split(' ');

            if (messageParts.Length > 0)
            {
                string command = messageParts[0].ToUpper();

                switch (command)
                {
                    case "LOGIN":
                        HandleLogin(messageParts);
                        break;
                    case "LOGOUT":
                        HandleLogout();
                        break;
                    case "CREATE_ROOM":
                        HandleCreateRoom(messageParts);
                        break;
                    case "DELETE_ROOM":
                        HandleDeleteRoom(messageParts);
                        break;
                    case "JOIN_ROOM":
                        HandleJoinRoom(messageParts);
                        break;
                    case "DELETE_USER":
                        HandleDeleteUser(messageParts);
                        break;
                    case "VIEW_ROOMS":
                        HandleViewRooms();
                        break;
                    case "VIEW_CONNECTED":
                        HandleViewConnected();
                        break;
                    case "TYPING":
                        HandleTyping();
                        break;
                    case "MESSAGE":
                        HandleMessage(e.Data);
                        break;
                    case "HELP":
                        HandleHelp();
                        break;
                    default:
                        Send("Comando no reconocido. Escribe 'HELP' para ver la lista de comandos disponibles.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Send($"ERROR {ex.Message}");
        }
    }

    // Manejo del Login
    private void HandleLogin(string[] messageParts)
    {
        if (messageParts.Length < 3)
        {
            Send("LOGIN_ERROR Falta usuario o contraseña.");
            return;
        }

        string user = messageParts[1];
        string password = messageParts[2];

        if (!connectedUsers.ContainsKey(user))
        {
            string role = user == "admin" ? "admin" : "user";
            connectedUsers[user] = new User(user, role);
            Console.WriteLine($"{user} se ha registrado como {role}.");
        }

        userName = user;
        connectedUsers[user].LastActivity = DateTime.Now;
        Send($"LOGIN_SUCCESS {connectedUsers[user].Role}");
    }

    // Manejo del Logout
    private void HandleLogout()
    {
        if (connectedUsers.ContainsKey(userName))
        {
            connectedUsers.Remove(userName);
            Console.WriteLine($"{userName} se ha desconectado.");
            Send("LOGOUT_SUCCESS");
        }
        else
        {
            Send("LOGOUT_ERROR Usuario no encontrado.");
        }
    }

    // Crear Sala
    private void HandleCreateRoom(string[] messageParts)
    {
        if (connectedUsers[userName].Role != "admin")
        {
            Send("ERROR No tienes permisos para crear salas.");
            return;
        }

        if (messageParts.Length < 2)
        {
            Send("ERROR Falta el nombre de la sala.");
            return;
        }

        string newRoom = messageParts[1];
        if (!chatRooms.Any(r => r.RoomName == newRoom))
        {
            chatRooms.Add(new ChatRoomData(newRoom));
            Send($"ROOM_CREATED {newRoom}");
        }
        else
        {
            Send("ERROR La sala ya existe.");
        }
    }

    // Eliminar Sala
    private void HandleDeleteRoom(string[] messageParts)
    {
        if (connectedUsers[userName].Role != "admin")
        {
            Send("ERROR No tienes permisos para eliminar salas.");
            return;
        }

        if (messageParts.Length < 2)
        {
            Send("ERROR Falta el nombre de la sala.");
            return;
        }

        string roomToDelete = messageParts[1];
        ChatRoomData room = chatRooms.FirstOrDefault(r => r.RoomName == roomToDelete);

        if (room != null)
        {
            chatRooms.Remove(room);
            Send($"ROOM_DELETED {roomToDelete}");
        }
        else
        {
            Send("ERROR La sala no existe.");
        }
    }

    // Unirse a una Sala
    private void HandleJoinRoom(string[] messageParts)
    {
        if (messageParts.Length < 2)
        {
            Send("ERROR Falta el nombre de la sala.");
            return;
        }

        string requestedRoom = messageParts[1];
        ChatRoomData room = chatRooms.FirstOrDefault(r => r.RoomName == requestedRoom);

        if (room == null)
        {
            Send("ERROR La sala no existe.");
            return;
        }

        if (!room.ConnectedUsers.Any(u => u.UserName == userName))
        {
            room.ConnectedUsers.Add(connectedUsers[userName]);
        }

        roomName = requestedRoom;
        Send($"JOINED_ROOM {roomName}");
    }

    // Eliminar Usuario
    private void HandleDeleteUser(string[] messageParts)
    {
        if (connectedUsers[userName].Role != "admin")
        {
            Send("ERROR No tienes permisos para eliminar usuarios.");
            return;
        }

        if (messageParts.Length < 2)
        {
            Send("ERROR Falta el nombre del usuario.");
            return;
        }

        string userToDelete = messageParts[1];

        if (connectedUsers.ContainsKey(userToDelete))
        {
            connectedUsers.Remove(userToDelete);
            Console.WriteLine($"{userToDelete} ha sido eliminado.");
            Send($"USER_DELETED {userToDelete}");
        }
        else
        {
            Send("ERROR El usuario no existe.");
        }
    }

    // Mostrar las salas y usuarios conectados
    private void HandleViewRooms()
    {
        if (chatRooms.Count == 0)
        {
            Send("No hay salas disponibles.");
            return;
        }

        string roomInfo = "";
        foreach (var room in chatRooms)
        {
            string usersInRoom = string.Join(", ", room.ConnectedUsers.Select(u => u.UserName));
            roomInfo += $"Sala: {room.RoomName} - Usuarios: {usersInRoom}\n";
        }

        Send($"ROOMS_INFO:\n{roomInfo}");
    }

    // Ver usuarios conectados por sala
    private void HandleViewConnected()
    {
        if (chatRooms.Count == 0)
        {
            Send("No hay salas disponibles.");
            return;
        }

        string connectedInfo = "";
        foreach (var room in chatRooms)
        {
            string usersInRoom = string.Join(", ", room.ConnectedUsers.Where(u => u.Status == UserStatus.Active)
                                                                     .Select(u => $"{u.UserName} ({u.Status})"));
            connectedInfo += $"Sala: {room.RoomName} - Usuarios Activos: {usersInRoom}\n";
        }

        Send($"CONNECTED_USERS:\n{connectedInfo}");
    }

    // Manejar estado "escribiendo"
    private void HandleTyping()
    {
        connectedUsers[userName].Status = UserStatus.Typing;
        connectedUsers[userName].LastActivity = DateTime.Now;
        Console.WriteLine($"{userName} está escribiendo...");
    }

    // Manejar mensaje normal
    private void HandleMessage(string message)
    {
        connectedUsers[userName].Status = UserStatus.Active;
        connectedUsers[userName].LastActivity = DateTime.Now;
        Console.WriteLine($"[{userName}] {message}");
    }

    // Iniciar el Timer para verificar la inactividad
    private void StartInactivityTimer()
    {
        inactivityTimer = new System.Timers.Timer(10000);  // Verificar cada 10 segundos
        inactivityTimer.Elapsed += CheckUserInactivity;
        inactivityTimer.AutoReset = true;
        inactivityTimer.Enabled = true;
        Console.WriteLine("Inactivity Timer iniciado.");
    }

    // Verificar la inactividad de los usuarios
    private void CheckUserInactivity(object source, ElapsedEventArgs e)
    {
        foreach (var user in connectedUsers.Values)
        {
            if (user.IsInactive(5))  // 5 minutos de inactividad
            {
                if (user.Status != UserStatus.Inactive)
                {
                    user.Status = UserStatus.Inactive;
                    Console.WriteLine($"{user.UserName} ahora está Inactivo.");
                }
            }
        }
    }

    // Manejar el comando HELP
    private void HandleHelp()
    {
        string helpMessage = "Comandos disponibles:\n" +
                             "LOGIN <usuario> <contraseña> - Iniciar sesión\n" +
                             "LOGOUT - Cerrar sesión\n" +
                             "CREATE_ROOM <nombre_sala> - Crear una nueva sala (solo admin)\n" +
                             "DELETE_ROOM <nombre_sala> - Eliminar una sala (solo admin)\n" +
                             "JOIN_ROOM <nombre_sala> - Unirse a una sala\n" +
                             "DELETE_USER <usuario> - Eliminar un usuario (solo admin)\n" +
                             "VIEW_ROOMS - Ver todas las salas disponibles\n" +
                             "VIEW_CONNECTED - Ver usuarios conectados\n" +
                             "TYPING - Indicar que el usuario está escribiendo\n" +
                             "MESSAGE <mensaje> - Enviar un mensaje a la sala\n" +
                             "HELP - Ver la lista de comandos disponibles";
        Send(helpMessage);
    }
}

class InsituChatServer
{
    static void Main(string[] args)
    {
        WebSocketServer wssv = new WebSocketServer("ws://127.0.0.1:8080");
        wssv.AddWebSocketService<ChatRoom>("/chat");

        bool running = true;
        while (running)
        {
            Console.WriteLine("Escribe 'start' para iniciar el servidor, 'stop' para detenerlo, 'exit' para salir:");
            string command = Console.ReadLine().ToLower();

            switch (command)
            {
                case "start":
                    if (!wssv.IsListening)
                    {
                        wssv.Start();
                        Console.WriteLine("Servidor WebSocket iniciado.");
                    }
                    break;
                case "stop":
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                        Console.WriteLine("Servidor detenido.");
                    }
                    break;
                case "exit":
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                    }
                    running = false;
                    Console.WriteLine("Saliendo...");
                    break;
                default:
                    Console.WriteLine("Comando no reconocido.");
                    break;
            }
        }
    }
}