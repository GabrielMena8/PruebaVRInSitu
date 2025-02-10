using Newtonsoft.Json;

[System.Serializable]
public class SerializableMesh
{
    [JsonProperty("vertices")]
    public float[] Vertices { get; set; }

    [JsonProperty("triangles")]
    public int[] Triangles { get; set; }

    [JsonProperty("normals")]
    public float[] Normals { get; set; }

    [JsonProperty("uv")]
    public float[] UV { get; set; }
}
