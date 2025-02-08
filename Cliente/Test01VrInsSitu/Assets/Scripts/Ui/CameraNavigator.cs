using UnityEngine;
using System.Collections;

public class CameraNavigator : MonoBehaviour
{
    public Transform playerCamera;       // Cámara del jugador
    public Transform[] panels;           // Paneles a los que navegar
    private int currentPanelIndex = 0;
    private bool isLoggedIn = false;  // Variable para controlar el login

    // Método para establecer el estado de login
    public void SetLoggedIn(bool status)
    {
        isLoggedIn = status;
    }

    // Método para navegar al siguiente panel
    public void NavigateToNextPanel()
    {
        if (!isLoggedIn)
        {
            Debug.Log("Debes iniciar sesión primero.");
            return;
        }

        currentPanelIndex = (currentPanelIndex + 1) % panels.Length;
        MoveAndRotateCameraToPanel(currentPanelIndex);
    }

    // Método para navegar al panel anterior
    public void NavigateToPreviousPanel()
    {
        if (!isLoggedIn)
        {
            Debug.Log("Debes iniciar sesión primero.");
            return;
        }

        currentPanelIndex = (currentPanelIndex - 1 + panels.Length) % panels.Length;
        MoveAndRotateCameraToPanel(currentPanelIndex);
    }

    // Método para mover y rotar la cámara hacia el panel especificado
    void MoveAndRotateCameraToPanel(int index)
    {
        Vector3 targetPosition = panels[index].position + panels[index].forward * -2f;
        Vector3 directionToPanel = panels[index].position - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPanel);

        StartCoroutine(SmoothMoveAndRotate(playerCamera, targetPosition, targetRotation, 1f));
    }

    // Corrutina para mover y rotar la cámara suavemente
    IEnumerator SmoothMoveAndRotate(Transform target, Vector3 destination, Quaternion rotation, float duration)
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (isLoggedIn)
            {
                NavigateToNextPanel();
            }
            else
            {
                Debug.Log("Debes iniciar sesión primero.");
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (isLoggedIn)
            {
                NavigateToPreviousPanel();
            }
            else
            {
                Debug.Log("Debes iniciar sesión primero.");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Panel actual: " + currentPanelIndex);
        }
    }
}