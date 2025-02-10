using System;
using Newtonsoft.Json;
using UnityEngine;

public class ColorToJsonConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        // Serializamos el color como un objeto con propiedades r, g, b, a
        writer.WriteStartObject();
        writer.WritePropertyName("r");
        writer.WriteValue(value.r);
        writer.WritePropertyName("g");
        writer.WriteValue(value.g);
        writer.WritePropertyName("b");
        writer.WriteValue(value.b);
        writer.WritePropertyName("a");
        writer.WriteValue(value.a);
        writer.WriteEndObject();
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        float r = 0, g = 0, b = 0, a = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
                break;

            if (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = (string)reader.Value;
                if (!reader.Read())
                    continue;

                switch (propertyName)
                {
                    case "r":
                        r = Convert.ToSingle(reader.Value);
                        break;
                    case "g":
                        g = Convert.ToSingle(reader.Value);
                        break;
                    case "b":
                        b = Convert.ToSingle(reader.Value);
                        break;
                    case "a":
                        a = Convert.ToSingle(reader.Value);
                        break;
                }
            }
        }
        return new Color(r, g, b, a);
    }
}
