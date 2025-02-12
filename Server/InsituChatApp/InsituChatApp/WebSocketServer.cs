    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Timers;
    using WebSocketSharp;
    using WebSocketSharp.Server;
using Newtonsoft.Json;

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

                        case "SEND_OBJECT":
                            HandleSendObject(messageParts);
                            break;
                      
                        case "SEND_FILE_USER":
                            HandleSendFileUser(messageParts);
                            break;
                        case "SEND_FILE_ROOM":
                        HandleSendFileRoom(messageParts);
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
            clients.Remove(this);

            if (connectedUsers.ContainsKey(userName))
            {
                // Eliminar al usuario de la sala
                ChatRoomData room = chatRooms.FirstOrDefault(r => r.RoomName == roomName);
                if (room != null)
                {
                    room.ConnectedUsers.RemoveAll(u => u.UserName == userName);
                }

                // Notificar a todos los clientes en la misma sala que el usuario se ha desconectado
                foreach (ChatRoom client in clients)
                {
                    if (client.roomName == this.roomName && client != this)
                    {
                        client.Send($"MESSAGE [Sistema]: {userName} se ha desconectado.");
                    }
                }

                // Eliminar al usuario de la lista global de usuarios conectados
                connectedUsers.Remove(userName);
                Console.WriteLine($"{userName} se ha desconectado.");
                BroadcastConnectedUsers();
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
                // Notificar a todos los usuarios de la sala que será eliminada
                foreach (ChatRoom client in clients)
                {
                    if (client.roomName == room.RoomName)
                    {
                        client.Send($"MESSAGE [Sistema]: La sala '{room.RoomName}' ha sido eliminada.");
                        client.roomName = null;  // Eliminar la referencia a la sala para este cliente
                    }
                }

                // Eliminar a todos los usuarios de la sala
                room.ConnectedUsers.Clear();

                // Eliminar la sala de la lista global de salas
                chatRooms.Remove(room);
                Console.WriteLine($"La sala '{room.RoomName}' ha sido eliminada.");
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

            // NUEVO: Notificar a todos los clientes en la misma sala que este usuario se ha unido
            foreach (ChatRoom client in clients)
            {
                if (client.roomName == this.roomName && client != this)
                {
                    client.Send($"MESSAGE [Sistema]: {userName} se ha unido a la sala.");
                }
            }

            Console.WriteLine($"{userName} se ha unido a la sala {roomName}.");
            BroadcastConnectedUsers();
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
            // Si el cliente no está en ninguna sala, se notifica el error.
            if (string.IsNullOrEmpty(roomName))
            {
                Send("ERROR: No estás en ninguna sala.");
                return;
            }

            // Buscar la sala en la que se encuentra el cliente.
            ChatRoomData room = chatRooms.FirstOrDefault(r => r.RoomName == roomName);
            if (room == null)
            {
                Send("ERROR: Sala no encontrada.");
                return;
            }

            // Filtrar y formatear la lista de usuarios activos (con estado Active).
            string usersInRoom = string.Join(", ", room.ConnectedUsers
                                                        .Where(u => u.Status == UserStatus.Active)
                                                        .Select(u => $"{u.UserName} ({u.Status})"));

            // Envía un mensaje que comienza con "CONNECTED_USERS:" seguido de la información.
            Send($"CONNECTED_USERS:\nSala: {room.RoomName} - Usuarios Activos: {usersInRoom}");
        }
    private void HandleSendFileRoom(string[] parts)
    {
        Console.WriteLine("[DEBUG] Iniciando el manejo de archivo para la sala.");

        if (parts.Length < 3)
        {
            Console.WriteLine("[ERROR] Formato de SEND_FILE_ROOM incorrecto.");
            Send("ERROR: Formato de SEND_FILE_ROOM incorrecto.");
            return;
        }

        string roomName = parts[1].Trim();
        string fileDataJson = parts[2].Trim();

        // Intentar deserializar el archivo JSON
        try
        {
            Console.WriteLine("[DEBUG] Intentando deserializar el archivo JSON.");

            FileData fileData = JsonConvert.DeserializeObject<FileData>(fileDataJson);

            if (fileData == null)
            {
                Console.WriteLine("[ERROR] Datos de archivo inválidos.");
                Send("ERROR: Datos de archivo inválidos.");
                return;
            }

            Console.WriteLine("[DEBUG] Archivo deserializado correctamente. Nombre: " + fileData.FileName);

            // Enviar el archivo a todos los clientes en la misma sala
            foreach (ChatRoom client in clients)
            {
                if (client.roomName == roomName)
                {
                    client.Send("FILE_DIRECT " + fileDataJson);
                    Console.WriteLine($"[DEBUG] Enviando archivo a {client.userName} en la sala {roomName}: {fileData.FileName}");
                }
            }

            Console.WriteLine($"[DEBUG] Archivo {fileData.FileName} enviado a todos los usuarios de la sala {roomName}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error al procesar el archivo: {ex.Message}");
            Send("ERROR: No se pudo procesar el archivo.");
        }
    }


    // Manejar estado "escribiendo"

    private void HandleTyping()
        {
            // Actualiza el estado del usuario
            if (!string.IsNullOrEmpty(userName) && connectedUsers.ContainsKey(userName))
            {
                connectedUsers[userName].Status = UserStatus.Typing;
                connectedUsers[userName].LastActivity = DateTime.Now;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{userName} está escribiendo...");
                Console.ResetColor();

                // Difundir el estado TYPING a los demás clientes en la misma sala
                foreach (ChatRoom client in clients)
                {
                    if (client.roomName == this.roomName && client.userName != this.userName)
                    {
                        client.Send("TYPING " + userName);
                    }
                }
            }
        }


        private void BroadcastConnectedUsers()
        {
            // Buscar la sala a la que pertenece el cliente actual.
            ChatRoomData room = chatRooms.FirstOrDefault(r => r.RoomName == roomName);
            if (room == null)
                return;

            // Construir la cadena de usuarios conectados.
            string users = string.Join(", ", room.ConnectedUsers.Select(u => $"{u.UserName} ({u.Status})"));
            string message = $"CONNECTED_USERS:\nSala: {roomName} - Usuarios Activos: {users}";

            // Difundir el mensaje a todos los clientes en la misma sala.
            foreach (ChatRoom client in clients)
            {
                if (client.roomName == roomName)
                {
                    client.Send(message);
                }
            }
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


        /// <summary>
        /// Maneja el comando SEND_OBJECT.
        /// Formato esperado: "SEND_OBJECT <targetUser> <objectDataJson>"
        /// </summary>
        private void HandleSendObject(string[] fullMessage)
        {
            if (fullMessage.Length < 3)
            {
                Send("ERROR: Formato de SEND_OBJECT incorrecto.");
                return;
            }

            string targetUser = fullMessage[1].Trim();
            string encodedJson = fullMessage[2];

            bool enviado = false;
            foreach (ChatRoom client in clients)
            {
                if (client.userName == this.userName)
                {
                    continue;  // No enviar el objeto al usuario que lo envió
                }

                if (client.roomName == this.roomName && client.userName.Equals(targetUser, StringComparison.OrdinalIgnoreCase))
                {
                    client.Send("OBJECT_DIRECT " + encodedJson);
                    enviado = true;
                    break;
                }
            }

            if (!enviado)
            {
                Send("ERROR: Usuario destino no encontrado en la sala.");
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

        private void CheckUserInactivity(object source, ElapsedEventArgs e)
        {
            foreach (var user in connectedUsers.Values)
            {
                if (user.IsInactive(60))  //  segundos de inactividad
                {
                    if (user.Status != UserStatus.Inactive)
                    {
                        user.Status = UserStatus.Inactive;
                        Console.WriteLine($"{user.UserName} ahora está Inactivo.");

                        // Notificar a todos los clientes en la misma sala
                        foreach (ChatRoom client in clients)
                        {
                            if (client.roomName == this.roomName)
                            {
                                client.Send($"MESSAGE [Sistema]: {user.UserName} ahora está inactivo.");
                            }
                        }
                    }
                }
            }
        }

    //Handle de files


    // Diccionario para almacenar los fragmentos recibidos
    private static Dictionary<string, List<string>> fileChunksInProgress = new Dictionary<string, List<string>>();

    // Manejar el comando SEND_FILE_USER para recibir los fragmentos
    private void HandleSendFileUser(string[] parts)
    {
        Console.WriteLine("[DEBUG] Iniciando el manejo de archivo para un usuario específico.");

        if (parts.Length < 3)
        {
            Console.WriteLine("[ERROR] Formato de SEND_FILE_USER incorrecto.");
            Send("ERROR: Formato de SEND_FILE_USER incorrecto.");
            return;
        }

        string targetUser = parts[1].Trim();
        string fileDataJson = parts[2].Trim();

        // Eliminar espacios adicionales antes de deserializar
        fileDataJson = string.Join(" ", fileDataJson.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        // Verifica el JSON recibido
        Console.WriteLine("[DEBUG] Datos del archivo JSON recibidos: " + fileDataJson);

        // Intentar deserializar el archivo JSON
        try
        {
            Console.WriteLine("[DEBUG] Intentando deserializar el archivo JSON.");

            FileChunk fileChunk = JsonConvert.DeserializeObject<FileChunk>(fileDataJson);

            if (fileChunk == null)
            {
                Console.WriteLine("[ERROR] Datos de archivo inválidos.");
                Send("ERROR: Datos de archivo inválidos.");
                return;
            }

            Console.WriteLine("[DEBUG] Fragmento recibido. Nombre: " + fileChunk.FileName);

            // Almacenar el fragmento
            if (!fileChunksInProgress.ContainsKey(fileChunk.FileName))
            {
                fileChunksInProgress[fileChunk.FileName] = new List<string>();
            }

            fileChunksInProgress[fileChunk.FileName].Add(fileChunk.ContentBase64);

            // Verificar si se han recibido todos los fragmentos
            if (fileChunksInProgress[fileChunk.FileName].Count == fileChunk.TotalChunks)
            {
                Console.WriteLine("[DEBUG] Todos los fragmentos recibidos. Reconstruyendo el archivo.");

                // Reconstruir el archivo completo
                string fullBase64Content = string.Join("", fileChunksInProgress[fileChunk.FileName]);
                byte[] fileBytes = Convert.FromBase64String(fullBase64Content);

                // Guardar el archivo en el servidor
                string savePath = Path.Combine("ruta_donde_guardar", fileChunk.FileName);
                File.WriteAllBytes(savePath, fileBytes);
                Console.WriteLine($"[DEBUG] Archivo {fileChunk.FileName} guardado en {savePath}");

                // Limpiar el diccionario de fragmentos para el archivo
                fileChunksInProgress.Remove(fileChunk.FileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error al procesar el archivo: {ex.Message}");
            Send("ERROR: No se pudo procesar el archivo.");
        }
    }


    // Clase que representa un fragmento de archivo
    public class FileChunk
    {
        public string FileName { get; set; }
        public string ContentBase64 { get; set; }
        public int TotalChunks { get; set; }
        public int CurrentChunk { get; set; }
    }






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


