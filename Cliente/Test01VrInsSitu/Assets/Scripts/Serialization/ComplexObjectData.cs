// ComplexObjectData.cs
using Newtonsoft.Json;
using UnityEngine;

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

    public static ComplexObjectData FromGameObject(GameObject obj)
    {
        var meshFilter = obj.GetComponent<MeshFilter>();
        var renderer = obj.GetComponent<Renderer>();

        if (meshFilter == null) return null;

        var data = new ComplexObjectData
        {
            ObjectName = obj.name,
            Position = obj.transform.position,
            Rotation = obj.transform.rotation,
            Scale = obj.transform.localScale,
            MeshData = SerializableMesh.FromMesh(meshFilter.mesh),
            MaterialData = renderer != null ? new SerializableMaterial
            {
                ShaderName = renderer.material.shader.name,
                Color = renderer.material.color
            } : null
        };

        return data;
    }
}
