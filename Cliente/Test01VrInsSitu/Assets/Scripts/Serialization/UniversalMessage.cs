using Newtonsoft.Json;

[System.Serializable]
public class UniversalMessage
{
    [JsonProperty("command")]
    public string Command { get; set; }

    [JsonProperty("payload")]
    public string Payload { get; set; }
}
