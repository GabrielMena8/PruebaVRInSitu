public class MessageWrapper<T>
{
    public string command { get; set; }
    public T payload { get; set; }
}
