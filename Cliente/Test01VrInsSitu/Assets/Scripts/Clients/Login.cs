using UnityEngine;
using TMPro;

/// <summary>
/// Clase que maneja el proceso de inicio de sesión.
/// </summary>
public class Login : MonoBehaviour
{
    public static Login Instance;

    // Referencias a los componentes de la UI
    [Header("Referencias a los componentes de la UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;
    public AuthManager authService;

    #region Unity Methods

    /// <summary>
    /// Método de Unity llamado al iniciar el script.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Suscribirse a los eventos de autenticación
        authService.OnLoginSuccess += HandleLoginSuccess;
        authService.OnLoginError += HandleLoginError;
    }

    #endregion

    #region Login Methods

    /// <summary>
    /// Manejar el intento de inicio de sesión.
    /// </summary>
    public void HandleLogin()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            authService.AttemptLogin(username, password);
        }
        else
        {
            Debug.Log("Por favor, completa ambos campos.");
        }
    }

    /// <summary>
    /// Manejar el éxito del inicio de sesión.
    /// </summary>
    /// <param name="role">Rol del usuario</param>
    private void HandleLoginSuccess(string role)
    {
        loginPanel.SetActive(false);
        PanelManager.Instance.SetRole(role);
    }

    /// <summary>
    /// Manejar el error del inicio de sesión.
    /// </summary>
    /// <param name="error">Mensaje de error</param>
    private void HandleLoginError(string error)
    {
        Debug.Log("Error de inicio de sesión: " + error);
    }

    #endregion
}   