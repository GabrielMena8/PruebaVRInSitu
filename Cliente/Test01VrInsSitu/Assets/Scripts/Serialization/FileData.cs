using Newtonsoft.Json;

[System.Serializable]
public class FileData
{

    public string FileName { get; set; }

   
    public string FileType { get; set; }

   
    public string ContentBase64 { get; set; }
}
