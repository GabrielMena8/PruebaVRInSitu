using UnityEngine;

public class ClickManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))  // 0 es el bot�n izquierdo del rat�n
        {
            // Convertir la posici�n del mouse en un rayo desde la c�mara principal
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Si el rayo choca con alg�n objeto que tenga un Collider
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Se obtiene el objeto impactado
                GameObject clickedObject = hit.collider.gameObject;
                // Llamar al m�todo de ChatClient para procesar el clic
                ChatClient.Instance.OnObjectClicked(clickedObject);
            }
        }
    }
}
