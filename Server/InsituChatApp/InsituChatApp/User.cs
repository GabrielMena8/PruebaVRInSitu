public class User
{
    public string UserName { get; set; }
    public UserStatus Status { get; set; }
    public DateTime LastActivity { get; set; }
    public string Role { get; set; }  // "admin" o "user"

    public User(string userName, string role)
    {
        UserName = userName;
        Role = role;
        Status = UserStatus.Active;
        LastActivity = DateTime.Now;
    }

    public bool IsInactive(int thresholdInSeconds)
    {
        return (DateTime.Now - LastActivity).TotalSeconds >= thresholdInSeconds;
    }
}
