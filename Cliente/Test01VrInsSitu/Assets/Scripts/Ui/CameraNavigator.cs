using UnityEngine;
using System.Collections;

public class CameraNavigator : MonoBehaviour
{
    public Transform playerCamera;
    [SerializeField] private Transform[] panels;
    private int currentPanelIndex = 0;
    private bool canNavigate = false;  // Controla si se puede navegar

 

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
        if (!canNavigate) return;  // No permite la navegación si no está logueado

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NavigateToNextPanel();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            NavigateToPreviousPanel();
        }
    }

    public void NavigateToNextPanel()
    {
        if (panels.Length == 0) return;
        currentPanelIndex = (currentPanelIndex + 1) % panels.Length;
        MoveToPanel(currentPanelIndex);
    }

    public void NavigateToPreviousPanel()
    {
        if (panels.Length == 0) return;
        currentPanelIndex = (currentPanelIndex - 1 + panels.Length) % panels.Length;
        MoveToPanel(currentPanelIndex);
    }

    private void MoveToPanel(int index)
    {
        if (panels[index] == null) return;

        Vector3 targetPosition = panels[index].position + panels[index].forward * -2f;
        Vector3 directionToPanel = panels[index].position - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPanel);

        StartCoroutine(SmoothMoveAndRotate(playerCamera, targetPosition, targetRotation, 1f));
    }

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