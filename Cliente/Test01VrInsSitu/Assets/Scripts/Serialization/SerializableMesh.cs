// SerializableMesh.cs
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

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

    public static SerializableMesh FromMesh(Mesh mesh)
    {
        var serializableMesh = new SerializableMesh
        {
            Vertices = mesh.vertices.SelectMany(v => new float[] { v.x, v.y, v.z }).ToArray(),
            Triangles = mesh.triangles,
            Normals = mesh.normals.SelectMany(n => new float[] { n.x, n.y, n.z }).ToArray(),
            UV = mesh.uv.SelectMany(u => new float[] { u.x, u.y }).ToArray()
        };

        return serializableMesh;
    }
}
