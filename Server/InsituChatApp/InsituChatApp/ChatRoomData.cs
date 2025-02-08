public class ChatRoomData
{
    public string RoomName { get; set; }
    public List<User> ConnectedUsers { get; set; }

    public ChatRoomData(string roomName)
    {
        RoomName = roomName;
        ConnectedUsers = new List<User>();
    }
}
