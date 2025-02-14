// La clase FileChunk representa un fragmento de un archivo que se está dividiendo en partes más pequeñas para su procesamiento o transmisión.
public class FileChunk
{
    // Nombre del archivo original.
    public string FileName { get; set; }

    // Contenido del fragmento del archivo codificado en Base64.
    public string ContentBase64 { get; set; }

    // Número total de fragmentos en los que se ha dividido el archivo.
    public int TotalChunks { get; set; }

    // Número de este fragmento en la secuencia de fragmentos.
    public int CurrentChunk { get; set; }

    // Tipo de archivo (por ejemplo, "txt", "jpg", etc.).
    public string FileType { get; set; }  // Asegúrate de que este campo esté presente
}
