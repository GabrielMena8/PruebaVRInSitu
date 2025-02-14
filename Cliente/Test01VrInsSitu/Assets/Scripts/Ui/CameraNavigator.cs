using UnityEngine;
using System.Collections;

public class CameraNavigator : MonoBehaviour
{
    // Referencia a la cámara del jugador
    public Transform playerCamera;

    // Paneles a los que se puede navegar
    [SerializeField] private Transform[] panels;

    // Índice del panel actual
    private int currentPanelIndex = 0;

    // Controla si se puede navegar
    private bool canNavigate = false;

    /// <summary>
    /// Método para habilitar o deshabilitar la navegación.
    /// </summary>
    /// <param name="status">`true` para habilitar, `false` para deshabilitar.</param>
    public void SetLoggedIn(bool status)
    {
        canNavigate = status;
        Debug.Log($"Navegación habilitada: {status}");
    }

    private void Update()
    {
        // No permite la navegación si no está logueado
        if (!canNavigate) return;

        // Navegar al siguiente panel con la flecha derecha
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NavigateToNextPanel();
        }
        // Navegar al panel anterior con la flecha izquierda
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            NavigateToPreviousPanel();
        }
    }

    /// <summary>
    /// Navega al siguiente panel.
    /// </summary>
    public void NavigateToNextPanel()
    {
        if (panels.Length == 0) return;
        currentPanelIndex = (currentPanelIndex + 1) % panels.Length;
        MoveToPanel(currentPanelIndex);
    }

    /// <summary>
    /// Navega al panel anterior.
    /// </summary>
    public void NavigateToPreviousPanel()
    {
        if (panels.Length == 0) return;
        currentPanelIndex = (currentPanelIndex - 1 + panels.Length) % panels.Length;
        MoveToPanel(currentPanelIndex);
    }

    /// <summary>
    /// Mueve la cámara al panel especificado.
    /// </summary>
    /// <param name="index">Índice del panel al que se va a mover.</param>
    private void MoveToPanel(int index)
    {
        if (panels[index] == null) return;

        Vector3 targetPosition = panels[index].position + panels[index].forward * -2f;
        Vector3 directionToPanel = panels[index].position - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPanel);

        StartCoroutine(SmoothMoveAndRotate(playerCamera, targetPosition, targetRotation, 1f));
    }

    /// <summary>
    /// Corrutina para mover y rotar suavemente la cámara.
    /// </summary>
    /// <param name="target">Transform de la cámara.</param>
    /// <param name="destination">Posición destino.</param>
    /// <param name="rotation">Rotación destino.</param>
    /// <param name="duration">Duración del movimiento.</param>
    /// <returns></returns>
    private IEnumerator SmoothMoveAndRotate(Transform target, Vector3 destination, Quaternion rotation, float duration)
    {
        Vector3 startPosition = target.position;
        Quaternion startRotation = target.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.position = Vector3.Lerp(startPosition, destination, elapsed / duration);
            target.rotation = Quaternion.Slerp(startRotation, rotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.position = destination;
        target.rotation = rotation;
    }
}
