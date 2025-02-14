public class FileChunkTracker
{
    // Propiedad que almacena los fragmentos del archivo
    public string[] Chunks { get; }

    // Contador de fragmentos recibidos
    private int _receivedCount = 0;

    // Ruta temporal del archivo
    public string TempFilePath { get; }

    // Constructor que inicializa el número total de fragmentos y la ruta temporal del archivo
    public FileChunkTracker(int totalChunks, string tempFilePath)
    {
        Chunks = new string[totalChunks];
        TempFilePath = tempFilePath;
    }

    // Método para agregar un fragmento al arreglo de fragmentos
    public void AddChunk(int index, string content)
    {
        if (index >= 0 && index < Chunks.Length)
        {
            Chunks[index] = content;
            _receivedCount++;
        }
    }

    // Método que verifica si todos los fragmentos han sido recibidos
    public bool IsComplete() => _receivedCount == Chunks.Length;

    // Método para combinar los fragmentos y obtener los datos completos del archivo
    public byte[] CombineChunks()
    {
        // Unimos todos los fragmentos en un solo string
        string combinedContent = string.Join("", Chunks);

        // Convertimos el contenido combinado de nuevo a bytes
        return Convert.FromBase64String(combinedContent);
    }
}
