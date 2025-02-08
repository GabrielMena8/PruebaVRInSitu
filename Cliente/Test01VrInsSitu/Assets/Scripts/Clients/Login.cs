using UnityEngine;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;
    public ChatClient chatClient;

    public void HandleLogin()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            chatClient.AttemptLogin(username, password);
        }
        else
        {
            Debug.Log("Por favor, completa ambos campos.");
        }
    }
}
