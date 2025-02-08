using UnityEngine;
using System.Collections;

/// <summary>
/// Clase que maneja la navegación de la cámara entre los paneles.
/// </summary>
public class CameraNavigator : MonoBehaviour
{
    [Header("Referencias a los componentes de la cámara")]
    public Transform playerCamera;  // Cámara del jugador
    private Transform[] panels;  // Paneles a los que navegar

    private int currentPanelIndex = 0;
    private bool isLoggedIn = false;  // Variable para controlar el login

    #region Unity Methods

    /// <summary>
    /// Método de Unity llamado al iniciar el script.
    /// </summary>
    private void Start()
    {
        // Suscribirse a los eventos de autenticación
        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnLoginError += HandleLoginError;
    }

    /// <summary>
    /// Método de Unity llamado una vez por frame.
    /// </summary>
    private void Update()
    {
        // Navegar entre los paneles usando las teclas de flecha
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NavigateToNextPanel();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            NavigateToPreviousPanel();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Panel actual: " + currentPanelIndex);
        }
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Manejar el éxito del inicio de sesión.
    /// </summary>
    /// <param name="role">Rol del usuario</param>
    private void HandleLoginSuccess(string role)
    {
        isLoggedIn = true;
    }

    /// <summary>
    /// Manejar el error del inicio de sesión.
    /// </summary>
    /// <param name="error">Mensaje de error</param>
    private void HandleLoginError(string error)
    {
        isLoggedIn = false;
    }

    /// <summary>
    /// Método para establecer el estado de login.
    /// </summary>
    /// <param name="status">Estado de login</param>
    public void SetLoggedIn(bool status)
    {
        isLoggedIn = status;
    }

    /// <summary>
    /// Método para establecer los paneles.
    /// </summary>
    /// <param name="panelTransforms">Array de Transforms de los paneles</param>
    public void SetPanels(GameObject[] panelObjects)
    {
        panels = new Transform[panelObjects.Length];
        for (int i = 0; i < panelObjects.Length; i++)
        {
            panels[i] = panelObjects[i].transform;
        }
    }

    /// <summary>
    /// Método para navegar al siguiente panel.
    /// </summary>
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

    /// <summary>
    /// Método para navegar al panel anterior.
    /// </summary>
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

    /// <summary>
    /// Método para mover y rotar la cámara hacia el panel especificado.
    /// </summary>
    /// <param name="index">Índice del panel</param>
    void MoveAndRotateCameraToPanel(int index)
    {
        Vector3 targetPosition = panels[index].position + panels[index].forward * -2f;
        Vector3 directionToPanel = panels[index].position - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPanel);

        StartCoroutine(SmoothMoveAndRotate(playerCamera, targetPosition, targetRotation, 1f));
    }

    /// <summary>
    /// Corrutina para mover y rotar la cámara suavemente.
    /// </summary>
    /// <param name="target">Transform de la cámara</param>
    /// <param name="destination">Posición destino</param>
    /// <param name="rotation">Rotación destino</param>
    /// <param name="duration">Duración de la animación</param>
    /// <returns></returns>
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

    #endregion
}