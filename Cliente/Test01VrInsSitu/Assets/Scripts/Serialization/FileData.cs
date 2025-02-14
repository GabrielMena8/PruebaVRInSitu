// La clase FileChunk representa un fragmento de un archivo que se est� dividiendo en partes m�s peque�as para su procesamiento o transmisi�n.
public class FileChunk
{
    // Nombre del archivo original.
    public string FileName { get; set; }

    // Contenido del fragmento del archivo codificado en Base64.
    public string ContentBase64 { get; set; }

    // N�mero total de fragmentos en los que se ha dividido el archivo.
    public int TotalChunks { get; set; }

    // N�mero de este fragmento en la secuencia de fragmentos.
    public int CurrentChunk { get; set; }

    // Tipo de archivo (por ejemplo, "txt", "jpg", etc.).
    public string FileType { get; set; }  // Aseg�rate de que este campo est� presente
}
