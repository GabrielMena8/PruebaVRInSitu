    using System.Diagnostics;
    using System.Timers;
    using WebSocketSharp;
    using WebSocketSharp.Server;
    using Newtonsoft.Json;

    /// <summary>
    /// Enum para representar el estado del usuario.
    /// </summary>
    public enum UserStatus
    {
        Active,
        Typing,
        Inactive
    }

    /// <summary>
    /// Clase que representa una sala de chat y maneja la comunicación WebSocket.
    /// </summary>
    public class ChatRoom : WebSocketBehavior
    {
        // ==========================
        // Variables estáticas
        // ==========================

        // Lista estática de todas las salas de chat.
        private static List<ChatRoomData> chatRooms = new List<ChatRoomData>();

        // Diccionario estático de usuarios conectados.
        private static Dictionary<string, User> connectedUsers = new Dictionary<string, User>();

        // Timer para verificar la inactividad de los usuarios.
        private static System.Timers.Timer? inactivityTimer;

        // Cronómetro para medir el tiempo de actividad del servidor.
        private static Stopwatch serverUptime = Stopwatch.StartNew();

        // Contadores de mensajes enviados y recibidos.
        private static int totalMessagesSent = 0;
        private static int totalMessagesReceived = 0;

        // Lista de latencias de mensajes.
        private static List<long> latencies = new List<long>();

        // Diccionario para rastrear fragmentos de archivos en progreso.
        private static Dictionary<string, List<string>> fileChunksInProgress = new Dictionary<string, List<string>>();

        // Diccionario para rastrear transferencias de archivos.
        private static Dictionary<string, FileChunkTracker> fileTransfers = new Dictionary<string, FileChunkTracker>();

        // Lista de conexiones activas para enviar mensajes (broadcast) a los clientes de la misma sala.
        private static List<ChatRoom> clients = new List<ChatRoom>();

        // ==========================
        // Variables de instancia
        // ==========================

        // Nombre de usuario y nombre de la sala para esta conexión.
        private string userName;
        private string roomName;

        // ==========================
        // Métodos de WebSocketBehavior
        // ==========================

        /// <summary>
        /// Método llamado cuando se abre una nueva conexión WebSocket.
        /// </summary>
        protected override void OnOpen()
        {
            // Agregamos esta conexión a la lista de clientes.
            clients.Add(this);
            if (inactivityTimer == null)
            {
                StartInactivityTimer();  // Iniciar el Timer solo la primera vez.
            }
        }

        /// <summary>
        /// Método llamado cuando se recibe un mensaje a través del WebSocket.
        /// </summary>
        /// <param name="e">Argumentos del evento de mensaje.</param>
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
        private void HandleMessage(string message)
        {
            // Actualiza el estado del usuario a Active y su última actividad
            connectedUsers[userName].Status = UserStatus.Active;
            connectedUsers[userName].LastActivity = DateTime.Now;

            // Log del mensaje recibido
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

        // Verificar la inactividad de los usuarios
        private void CheckUserInactivity(object source, ElapsedEventArgs e)
        {
            foreach (var user in connectedUsers.Values)
            {
                if (user.IsInactive(60))  // 60 segundos de inactividad
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

        // Método para escapar las comillas y otros caracteres especiales en JSON
        private string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("\\", "\\\\")  // Escapa barras invertidas
                        .Replace("\"", "\\\"")  // Escapa comillas dobles
                        .Replace("\n", "\\n")   // Escapa saltos de línea
                        .Replace("\r", "\\r");  // Escapa retornos de carro
        }





    // Manejar el envío de archivos a un usuario
    private void HandleSendFileUser(string[] parts)
    {
        // Evitar que el usuario se envíe un archivo a sí mismo
        if (userName.Equals(parts[1], StringComparison.OrdinalIgnoreCase))
        {
            Send("ERROR: No puedes enviarte un archivo a ti mismo.");
            return;
        }

        // El segundo parámetro es el usuario receptor
        string targetUser = parts[1];

        try
        {
            FileChunk chunk = JsonConvert.DeserializeObject<FileChunk>(parts[2]);
            if (chunk == null)
            {
                Send("FILE_ERROR: No se pudo deserializar el fragmento.");
                return;
            }

            // Modificar la clave de transferencia para incluir receptor
            string transferKey = $"{chunk.FileName}_{userName}_{targetUser}";
            Console.WriteLine($"Recibiendo fragmento de archivo: {chunk.FileName}, Fragmento: {chunk.CurrentChunk}/{chunk.TotalChunks}, Clave: {transferKey}");

            if (!fileTransfers.ContainsKey(transferKey))
            {
                Console.WriteLine("Creando nuevo tracker para: " + transferKey);
                string tempFolder = Path.Combine(Path.GetTempPath(), "InsituChatApp");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);
                string tempFilePath = Path.Combine(tempFolder, $"{chunk.FileName}.temp");
                fileTransfers[transferKey] = new FileChunkTracker(chunk.TotalChunks, tempFilePath);
            }
            else
            {
                Console.WriteLine("Tracker existente para: " + transferKey);
            }

            // Guardar el fragmento (índice basado en 0)
            fileTransfers[transferKey].AddChunk(chunk.CurrentChunk - 1, chunk.ContentBase64);

            if (fileTransfers[transferKey].IsComplete())
            {
                Console.WriteLine($"Archivo completo recibido: {chunk.FileName}");
                byte[] fileBytes = fileTransfers[transferKey].CombineChunks();
                string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\";
                string folderPath = Path.Combine(downloadsFolder, "InsituChatApp");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                string savePath = Path.Combine(folderPath, chunk.FileName);
                File.WriteAllBytes(savePath, fileBytes);
                Console.WriteLine($"Archivo {chunk.FileName} recibido y guardado en {savePath}");

                // Enviar alerta de archivo recibido al usuario receptor
                foreach (ChatRoom client in clients)
                {
                    if (client.userName.Equals(targetUser, StringComparison.OrdinalIgnoreCase))
                    {
                        client.Send("FILE_RECEIVED SUCCESS");
                    }
                }

                // Limpiar tracker y eliminar archivo temporal
                var tracker = fileTransfers[transferKey];
                fileTransfers.Remove(transferKey);
                File.Delete(tracker.TempFilePath);
            }
            else
            {
                Console.WriteLine($"Fragmento {chunk.CurrentChunk}/{chunk.TotalChunks} recibido.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al procesar el archivo: {ex.Message}");
            Send("FILE_ERROR " + ex.Message);
        }
    }



}