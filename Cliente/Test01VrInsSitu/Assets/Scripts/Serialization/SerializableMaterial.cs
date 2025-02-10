using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class SerializableMaterial
{
    [JsonProperty("shaderName")]
    public string ShaderName { get; set; }

    [JsonProperty("color")]
    [JsonConverter(typeof(ColorToJsonConverter))]

    public Color Color { get; set; }
}
