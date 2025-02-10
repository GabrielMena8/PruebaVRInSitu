using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class ComplexObjectData
{
    [JsonProperty("objectName")]
    public string ObjectName { get; set; }

    [JsonProperty("meshData")]
    public SerializableMesh MeshData { get; set; }

    [JsonProperty("materialData")]
    public SerializableMaterial MaterialData { get; set; }

    [JsonProperty("position")]
    [JsonConverter(typeof(Vector3ToJsonConverter))]
    public Vector3 Position { get; set; }

    [JsonProperty("rotation")]
    [JsonConverter(typeof(QuaternionJsonConverter))]
    public Quaternion Rotation { get; set; }

    [JsonProperty("scale")]
    [JsonConverter(typeof(Vector3ToJsonConverter))]
    public Vector3 Scale { get; set; }
}
