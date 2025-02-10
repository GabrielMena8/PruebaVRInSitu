using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class UIUtilities
{
    /// <summary>
    /// Crea un GameObject con un RectTransform y el componente T, lo asigna al padre y lo devuelve.
    /// </summary>
    public static T CreateUIElement<T>(string name, Transform parent) where T : Component
    {
        // Crear el GameObject con un RectTransform incluido
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    /// <summary>
    /// Crea y configura un título (TextMeshProUGUI).
    /// </summary>
    public static TextMeshProUGUI CreateTitle(Transform parent, string titleText, int fontSize = 24)
    {
        TextMeshProUGUI title = CreateUIElement<TextMeshProUGUI>("Title", parent);
        title.text = titleText;
        title.fontSize = fontSize;
        title.alignment = TextAlignmentOptions.Center;
        return title;
    }

    /// <summary>
    /// Crea y configura un botón con su texto y acción.
    /// </summary>
    public static Button CreateButton(Transform parent, string buttonText, UnityEngine.Events.UnityAction onClickAction, Vector2 size)
    {
        Button button = CreateUIElement<Button>(buttonText + "Button", parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();
        image.color = Color.gray;

        TextMeshProUGUI text = CreateUIElement<TextMeshProUGUI>("Text", button.transform);
        text.text = buttonText;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;

        button.onClick.AddListener(onClickAction);
        return button;
    }

    /// <summary>
    /// Crea y configura un InputField.
    /// </summary>
    public static TMP_InputField CreateInputField(Transform parent, string placeholderText, bool isPassword = false, Vector2? size = null)
    {
        TMP_InputField inputField = CreateUIElement<TMP_InputField>("InputField", parent);
        RectTransform rect = inputField.GetComponent<RectTransform>();
        rect.sizeDelta = size ?? new Vector2(200, 30);

        TextMeshProUGUI placeholder = CreateUIElement<TextMeshProUGUI>("Placeholder", inputField.transform);
        placeholder.text = placeholderText;
        placeholder.fontSize = 18;
        placeholder.color = Color.gray;
        placeholder.alignment = TextAlignmentOptions.Center;
        inputField.placeholder = placeholder;

        TextMeshProUGUI text = CreateUIElement<TextMeshProUGUI>("Text", inputField.transform);
        text.fontSize = 18;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        inputField.textComponent = text;

        if (isPassword)
            inputField.contentType = TMP_InputField.ContentType.Password;

        return inputField;
    }

    /// <summary>
    /// Crea y configura un texto (TextMeshProUGUI).
    /// </summary>
    public static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize = 18)
    {
        TextMeshProUGUI text = CreateUIElement<TextMeshProUGUI>("Text", parent);
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }
}
