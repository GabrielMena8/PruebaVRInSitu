public class FileChunk
{
    public string FileName { get; set; }
    public string ContentBase64 { get; set; }
    public int TotalChunks { get; set; }
    public int CurrentChunk { get; set; }
}
