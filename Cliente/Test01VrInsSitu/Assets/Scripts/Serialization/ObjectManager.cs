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

    public void InstantiateComplexObject(ComplexObjectData data)
    {
        // Crear un GameObject
        GameObject newObject = new GameObject(data.ObjectName);

        // Crear la malla
        Mesh mesh = CreateMeshFromData(data.MeshData);
        if (mesh == null)
        {
            Debug.LogError("Error: La malla no se creó correctamente.");
            return;
        }

        // Asignar la malla
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Asignar material
        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find(data.MaterialData.ShaderName));
        material.color = data.MaterialData.Color;
        meshRenderer.material = material;

        // Asignar posición, rotación y escala
        newObject.transform.position = data.Position;
        newObject.transform.rotation = data.Rotation;
        newObject.transform.localScale = data.Scale;

        Debug.Log("Objeto instanciado: " + data.ObjectName);
    }

    private Mesh CreateMeshFromData(SerializableMesh meshData)
    {
        Mesh mesh = new Mesh();

        if (meshData.Vertices != null && meshData.Vertices.Length > 0)
            mesh.vertices = ConvertToVector3Array(meshData.Vertices);

        if (meshData.Triangles != null && meshData.Triangles.Length > 0)
            mesh.triangles = meshData.Triangles;

        if (meshData.Normals != null && meshData.Normals.Length == meshData.Vertices.Length)
            mesh.normals = ConvertToVector3Array(meshData.Normals);

        if (meshData.UV != null && meshData.UV.Length == meshData.Vertices.Length)
            mesh.uv = ConvertToUVArray(meshData.UV);

        return mesh;
    }

    private Vector3[] ConvertToVector3Array(float[] array)
    {
        Vector3[] result = new Vector3[array.Length / 3];
        for (int i = 0; i < array.Length; i += 3)
        {
            result[i / 3] = new Vector3(array[i], array[i + 1], array[i + 2]);
        }
        return result;
    }

    private Vector2[] ConvertToUVArray(float[] uvArray)
    {
        Vector2[] uv = new Vector2[uvArray.Length / 2];
        for (int i = 0; i < uvArray.Length; i += 2)
        {
            uv[i / 2] = new Vector2(uvArray[i], uvArray[i + 1]);
        }
        return uv;
    }
}
