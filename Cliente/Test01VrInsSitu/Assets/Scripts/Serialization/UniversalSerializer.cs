using Newtonsoft.Json;

public static class UniversalSerializer
{
    // Serializa un objeto a JSON usando Newtonsoft.Json
    public static string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    // Deserializa un JSON a un objeto del tipo T
    public static T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }

    // Crea un mensaje universal a partir de un comando y un objeto payload
    public static string CreateUniversalMessage<T>(string command, T payload)
    {
        UniversalMessage msg = new UniversalMessage
        {
            Command = command,
            Payload = Serialize(payload)
        };
        return Serialize(msg);
    }

    // Extrae el payload del mensaje universal según el tipo esperado
    public static T ExtractPayload<T>(string universalMessageJson)
    {
        UniversalMessage msg = Deserialize<UniversalMessage>(universalMessageJson);
        return Deserialize<T>(msg.Payload);
    }
}
