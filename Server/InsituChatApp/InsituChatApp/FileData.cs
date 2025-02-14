// Definimos la clase FileChunk que representa un fragmento de archivo
public class FileChunk
{
    // Propiedad que almacena el nombre del archivo
    public required string FileName { get; set; }

    // Propiedad que almacena el contenido del archivo en formato Base64
    public required string ContentBase64 { get; set; }

    // Propiedad que indica el número total de fragmentos en los que se ha dividido el archivo
    public int TotalChunks { get; set; }

    // Propiedad que indica el número del fragmento actual
    public int CurrentChunk { get; set; }
}
