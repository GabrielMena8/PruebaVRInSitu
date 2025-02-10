using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private static Stopwatch serverUptime = Stopwatch.StartNew();
    private static int totalMessagesSent = 0;
    private static int totalMessagesReceived = 0;
    private static List<long> latencies = new List<long>();

    // NUEVO: Lista de conexiones activas para enviar mensajes (broadcast) a los clientes de la misma sala
    private static List<ChatRoom> clients = new List<ChatRoom>();

    private string userName;
    private string roomName;

    protected override void OnOpen()
    {
        // Agregamos esta conexión a la lista de clientes
        clients.Add(this);
        if (inactivityTimer == null)
        {
            StartInactivityTimer();  // Iniciar el Timer solo la primera vez
        }
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        // Si el comando no es LOGIN y el usuario aún no se ha autenticado, se rechaza el comando.
        if (!e.Data.ToUpper().StartsWith("LOGIN") && string.IsNullOrEmpty(userName))
        {
            Send("ERROR Debes iniciar sesión antes de enviar comandos.");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
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
                    case "STATS":
                        HandleStats();
                        break;
                    default:
                        Send("Comando no reconocido. Escribe 'HELP' para ver la lista de comandos disponibles.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            LogError(ex);
            Send($"ERROR {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
            totalMessagesReceived++;
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        // NUEVO: Removemos esta conexión de la lista de clientes
        clients.Remove(this);

        if (connectedUsers.ContainsKey(userName))
        {
            connectedUsers.Remove(userName);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{userName} se ha desconectado.");
            Console.ResetColor();
        }
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        LogError(new Exception(e.Message));
    }

    private void LogError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {DateTime.Now}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        Console.ResetColor();
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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{user} se ha registrado como {role}.");
            Console.ResetColor();
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{userName} se ha desconectado.");
            Console.ResetColor();
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

        // NUEVO: Notificar a los otros clientes de la sala que este usuario se ha unido
        foreach (ChatRoom client in clients)
        {
            if (client != this && client.roomName == this.roomName)
            {
                client.Send($"SYSTEM: {userName} se ha unido a la sala.");
            }
        }
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{userToDelete} ha sido eliminado.");
            Console.ResetColor();
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
            string usersInRoom = string.Join(", ", room.ConnectedUsers.Select(u => $"{u.UserName} ({u.Status})"));
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
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{userName} está escribiendo...");
        Console.ResetColor();
    }

    // Manejar mensaje normal y difundirlo a los clientes de la misma sala
    // Manejar mensaje normal y difundirlo a los clientes de la misma sala
    private void HandleMessage(string message)
    {
        // Actualiza el estado del usuario a Active y su última actividad
        connectedUsers[userName].Status = UserStatus.Active;
        connectedUsers[userName].LastActivity = DateTime.Now;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[{userName}] {message}");
        Console.ResetColor();
        totalMessagesSent++;

        // Extrae el contenido del mensaje (quitando el prefijo "MESSAGE ")
        string content = message.Length > "MESSAGE ".Length ? message.Substring("MESSAGE ".Length) : "";

        // Formatea el mensaje para incluir el nombre del usuario y su estado
        string formattedMessage = $"[{userName} ({connectedUsers[userName].Status})] {content}";

        // Difunde (broadcast) el mensaje a todas las conexiones que pertenezcan a la misma sala
        foreach (ChatRoom client in clients)
        {
            if (client.roomName == this.roomName)
            {
                client.Send("MESSAGE " + formattedMessage);
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
                             "HELP - Ver la lista de comandos disponibles\n" +
                             "STATS - Ver estadísticas del servidor";
        Send(helpMessage);
    }

    // Manejar el comando STATS
    private void HandleStats()
    {
        double averageLatency = latencies.Count > 0 ? latencies.Average() : 0;
        string statsMessage = $"Estadísticas del servidor:\n" +
                              $"Tiempo de actividad: {serverUptime.Elapsed}\n" +
                              $"Mensajes enviados: {totalMessagesSent}\n" +
                              $"Mensajes recibidos: {totalMessagesReceived}\n" +
                              $"Latencia promedio: {averageLatency} ms";
        Send(statsMessage);
    }

    // Iniciar el Timer para verificar la inactividad
    private void StartInactivityTimer()
    {
        inactivityTimer = new System.Timers.Timer(10000);  // Verificar cada 10 segundos
        inactivityTimer.Elapsed += CheckUserInactivity;
        inactivityTimer.AutoReset = true;
        inactivityTimer.Enabled = true;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Inactivity Timer iniciado.");
        Console.ResetColor();
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
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"{user.UserName} ahora está Inactivo.");
                    Console.ResetColor();
                }
            }
        }
    }

    // Manejar el comando HELP (ya implementado arriba)

    // Manejar el comando STATS (ya implementado arriba)
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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Servidor WebSocket iniciado.");
                        Console.ResetColor();
                    }
                    break;
                case "stop":
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Servidor detenido.");
                        Console.ResetColor();
                    }
                    break;
                case "exit":
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                    }
                    running = false;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Saliendo...");
                    Console.ResetColor();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Comando no reconocido.");
                    Console.ResetColor();
                    break;
            }
        }
    }
}
    