using Newtonsoft.Json;
using System;

public static class UniversalSerializer
{
    public static string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.None);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// Crea un mensaje universal con un comando y un payload.
    /// Se evita la doble serialización del payload.
    /// </summary>
    public static string CreateUniversalMessage<T>(string command, T payload)
    {
        var message = new
        {
            command = command,
            payload = payload
        };

        return JsonConvert.SerializeObject(message, Formatting.None);
    }

}
