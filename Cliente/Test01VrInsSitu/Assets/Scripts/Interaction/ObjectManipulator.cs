using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    public enum ManipulationMode { Translate, Rotate, Scale, Send }
    public ManipulationMode currentMode = ManipulationMode.Translate;

    private Camera mainCamera;
    private Vector3 offset;
    private float zCoord;

    // Guarda la escala inicial (opcional)
    private Vector3 initialScale;

    void Start()
    {
        mainCamera = Camera.main;
        initialScale = transform.localScale;
    }

    void Update()
    {
        // Permite cambiar el modo de manipulaci�n mediante teclas:
        if (Input.GetKeyDown(KeyCode.T))
        {
            currentMode = ManipulationMode.Translate;
            Debug.Log("Modo: Translate");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            currentMode = ManipulationMode.Rotate;
            Debug.Log("Modo: Rotate");
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentMode = ManipulationMode.Scale;
            Debug.Log("Modo: Scale");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentMode = ManipulationMode.Send;
            Debug.Log("Modo: Send");

        }



    }

    void OnMouseDown()
    {

    // Si es un clic izquierdo, inicia la manipulaci�n
        if (Input.GetMouseButtonDown(0))
        {
            if (currentMode == ManipulationMode.Translate)
            {
                // Obtiene la coordenada Z del objeto en pantalla
                zCoord = mainCamera.WorldToScreenPoint(transform.position).z;
                // Calcula el offset entre la posici�n del objeto y la posici�n del mouse en el mundo
                offset = transform.position - GetMouseWorldPos();


            }

            else if (currentMode == ManipulationMode.Send)
            {
                ChatClient.Instance.OnObjectClicked(gameObject);
                return;
            }

        }
    }

    // Convierte la posici�n del mouse (con la Z correcta) en posici�n en el mundo.
    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoord;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    void OnMouseDrag()
    {
        // Solo se procesa el arrastre si se mantiene presionado el bot�n izquierdo.
        if (!Input.GetMouseButton(0)) return;

        switch (currentMode)
        {
            case ManipulationMode.Translate:
                transform.position = GetMouseWorldPos() + offset;
                break;
            case ManipulationMode.Rotate:
                float rotationSpeed = 100f;
                float mouseX = Input.GetAxis("Mouse X");
                transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
                break;
            case ManipulationMode.Scale:
                float scaleSpeed = 0.01f;
                float mouseY = Input.GetAxis("Mouse Y");
                Vector3 newScale = transform.localScale + Vector3.one * mouseY * scaleSpeed;
                newScale = Vector3.Max(newScale, Vector3.one * 0.1f);
                transform.localScale = newScale;
                break;
        }
    }
}
