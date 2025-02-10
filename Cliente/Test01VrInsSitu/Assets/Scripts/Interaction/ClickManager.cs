using UnityEngine;

public class ClickManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))  // 0 es el botón izquierdo del ratón
        {
            // Convertir la posición del mouse en un rayo desde la cámara principal
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Si el rayo choca con algún objeto que tenga un Collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Se obtiene el objeto impactado
                GameObject clickedObject = hit.collider.gameObject;
                // Llamar al método de ChatClient para procesar el clic
                ChatClient.Instance.OnObjectClicked(clickedObject);
            }
        }
    }
}
