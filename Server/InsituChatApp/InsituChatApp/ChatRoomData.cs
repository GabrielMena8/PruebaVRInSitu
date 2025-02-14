// Clase que representa los datos de una sala de chat
public class ChatRoomData
{
    // Nombre de la sala de chat
    public string RoomName { get; set; }

    // Lista de usuarios conectados a la sala de chat
    public List<User> ConnectedUsers { get; set; }

    // Constructor que inicializa el nombre de la sala y la lista de usuarios conectados
    public ChatRoomData(string roomName)
    {
        RoomName = roomName;
        ConnectedUsers = new List<User>();
    }
}
