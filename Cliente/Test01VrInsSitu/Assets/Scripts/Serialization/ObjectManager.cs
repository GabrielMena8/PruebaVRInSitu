using UnityEngine;


public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Instancia un objeto en la escena utilizando los datos del objeto complejo.
    /// </summary>
    /// <param name="data">Datos serializados del objeto.</param>
    public void InstantiateComplexObject(ComplexObjectData data)
    {
        if (data == null)
        {
            Debug.LogError("InstantiateComplexObject: Los datos del objeto son nulos.");
            return;
        }

        // Crear un nuevo GameObject con el nombre recibido
        GameObject obj = new GameObject(data.ObjectName);
        obj.transform.position = data.Position;
        obj.transform.rotation = data.Rotation;
        obj.transform.localScale = data.Scale;

        // Crear la malla a partir de los datos serializados
        Mesh mesh = new Mesh();
        int vertexCount = data.MeshData.Vertices.Length / 3;
        Vector3[] vertices = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = new Vector3(
                data.MeshData.Vertices[i * 3],
                data.MeshData.Vertices[i * 3 + 1],
                data.MeshData.Vertices[i * 3 + 2]);
        }
        mesh.vertices = vertices;
        mesh.triangles = data.MeshData.Triangles;
        // (Opcional) Asigna normales, UV, etc. si están en data.MeshData

        // Agregar componentes necesarios
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        // Asignar el material si está disponible
        if (data.MaterialData != null)
        {
            Material mat = new Material(Shader.Find(data.MaterialData.ShaderName));
            mat.color = data.MaterialData.Color;
            mr.material = mat;
        }
        else
        {
            mr.material = new Material(Shader.Find("Standard"));
        }

        // (Opcional) Agregar un script de manipulación si lo deseas
        // obj.AddComponent<ObjectManipulator>();

        Debug.Log($"Instanciado objeto: {data.ObjectName}");
    }
}
