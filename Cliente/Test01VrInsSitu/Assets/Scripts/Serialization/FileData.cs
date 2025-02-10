using Newtonsoft.Json;

[System.Serializable]
public class FileData
{
    [JsonProperty("fileName")]
    public string FileName { get; set; }

    [JsonProperty("fileType")]
    public string FileType { get; set; }

    [JsonProperty("contentBase64")]
    public string ContentBase64 { get; set; }
}
