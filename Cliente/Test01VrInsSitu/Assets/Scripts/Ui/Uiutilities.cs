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
    /// <summary>
    /// Crea y agrega un componente TextMeshProUGUI a un elemento padre.
    /// </summary>
    /// <param name="parent">El transform del elemento padre.</param>
    /// <param name="content">El texto que se mostrará.</param>
    /// <param name="fontSize">El tamaño de la fuente (por defecto 18).</param>
    /// <param name="alignment">La alineación del texto (por defecto Center).</param>
    /// <returns>El componente TextMeshProUGUI creado.</returns>
    public static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize = 18, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI text = CreateUIElement<TextMeshProUGUI>("Text", parent);
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        return text;
    }


    //////////////////////////////////////////////
    // NUEVOS MÉTODOS PARA REFÁCTOR DE UI
    //////////////////////////////////////////////

    /// <summary>
    /// Crea y configura un VerticalLayoutGroup con parámetros básicos.
    /// </summary>
    public static VerticalLayoutGroup CreateVerticalLayoutGroup(
        Transform parent,
        float spacing = 5f,
        bool childForceExpandWidth = false,
        bool childForceExpandHeight = false,
        bool childControlWidth = true,
        bool childControlHeight = true)
    {
        var layoutGroup = CreateUIElement<VerticalLayoutGroup>("VerticalLayout", parent);
        layoutGroup.spacing = spacing;
        layoutGroup.childForceExpandWidth = childForceExpandWidth;
        layoutGroup.childForceExpandHeight = childForceExpandHeight;
        layoutGroup.childControlWidth = childControlWidth;
        layoutGroup.childControlHeight = childControlHeight;
        return layoutGroup;
    }

    /// <summary>
    /// Crea y configura un HorizontalLayoutGroup con parámetros básicos.
    /// </summary>
    public static HorizontalLayoutGroup CreateHorizontalLayoutGroup(
        Transform parent,
        float spacing = 10f,
        bool childForceExpandWidth = false,
        bool childForceExpandHeight = false,
        bool childControlWidth = true,
        bool childControlHeight = true)
    {
        var layoutGroup = CreateUIElement<HorizontalLayoutGroup>("HorizontalLayout", parent);
        layoutGroup.spacing = spacing;
        layoutGroup.childForceExpandWidth = childForceExpandWidth;
        layoutGroup.childForceExpandHeight = childForceExpandHeight;
        layoutGroup.childControlWidth = childControlWidth;
        layoutGroup.childControlHeight = childControlHeight;
        return layoutGroup;
    }

    /// <summary>
    /// Crea un botón de remover (por ejemplo, con texto "X"),
    /// le asigna tamaño, color, y la acción onClick.
    /// </summary>
    public static Button CreateRemoveButton(
        Transform parent,
        string label = "X",
        System.Action onClickAction = null,
        int fontSize = 16,
        Color? textColor = null,
        Vector2? size = null)
    {
        // 1) Creamos un objeto "RemoveButton"
        Button button = CreateUIElement<Button>("RemoveButton", parent);

        // 2) Ajustamos su tamaño
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size ?? new Vector2(30, 30); // por defecto 30x30

        // 3) Fondo del botón
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.2f); // un color semitransparente, por ejemplo

        // 4) Texto del botón
        TextMeshProUGUI tmpText = CreateUIElement<TextMeshProUGUI>("Text", button.transform);
        tmpText.text = label;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = textColor ?? Color.red; // por defecto rojo

        // 5) onClick
        if (onClickAction != null)
        {
            button.onClick.AddListener(() => onClickAction());
        }

        return button;
    }

    public static void CreateChatMessage(
      Transform chatContent,
      string name,
      string message,
      Color color,
      int fontSize = 16,
      TextAlignmentOptions alignment = TextAlignmentOptions.Center,
      bool wordWrap = false)
    {
        if (chatContent == null)
        {
            Debug.LogError("chatContent no está inicializado. No se puede crear el mensaje.");
            return;
        }

        GameObject messageObject = new GameObject(name, typeof(RectTransform));
        messageObject.transform.SetParent(chatContent, false);

        TextMeshProUGUI messageText = messageObject.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.fontSize = fontSize;
        messageText.alignment = alignment;
        messageText.color = color;

        // Activar/desactivar word wrapping
        messageText.enableWordWrapping = wordWrap;


        ContentSizeFitter csf = messageObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
    }

    /// <summary>
    /// Crea un contenedor con un VerticalLayoutGroup y un ContentSizeFitter opcional.
    /// Retorna el GameObject que contiene el layout.
    /// </summary>
    public static GameObject CreateVerticalLayout(
        Transform parent,
        string name = "VerticalLayout",
        Vector2 anchorMin = default,
        Vector2 anchorMax = default,
        Vector2 pivot = default,
        float spacing = 5f,
        bool addContentSizeFitter = true)
    {
        // Crear contenedor
        GameObject layoutObj = new GameObject(name, typeof(RectTransform));
        layoutObj.transform.SetParent(parent, false);

        RectTransform rt = layoutObj.GetComponent<RectTransform>();

        // Si no se especificaron valores, usamos defaults
        if (anchorMin == default) anchorMin = new Vector2(0, 0);
        if (anchorMax == default) anchorMax = new Vector2(1, 1);
        if (pivot == default) pivot = new Vector2(0.5f, 0.5f);

        // Ajustar anclas
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Agregar VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = layoutObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.spacing = spacing;

        // Agregar ContentSizeFitter si se desea
        if (addContentSizeFitter)
        {
            ContentSizeFitter fitter = layoutObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        return layoutObj;
    }

    /// <summary>
    /// Crea un ScrollView (400×200 por defecto) con esta estructura interna:
    /// ScrollView (ScrollRect + Image)
    /// ??? Viewport (Mask + Image)
    ///     ??? Content (VerticalLayoutGroup + ContentSizeFitter)
    /// Retorna el transform del 'Content', donde se añaden los mensajes.
    /// </summary>
    /// <param name="parent">El transform padre donde se creará el ScrollView.</param>
    /// <param name="scrollViewName">El nombre del GameObject ScrollView.</param>
    /// <param name="size">Tamaño del ScrollView. Por defecto, (400, 200).</param>
    /// <returns>El transform del contenedor (Content) con layout vertical.</returns>
    public static Transform CreateScrollViewWithVerticalLayout(
        Transform parent,
        string scrollViewName = "ChatScrollView",
        Vector2? size = null)
    {
        // 1) Crear el GameObject del ScrollView
        GameObject scrollViewObj = new GameObject(scrollViewName, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollViewObj.transform.SetParent(parent, false);

        // 2) Configurar su RectTransform
        RectTransform svRect = scrollViewObj.GetComponent<RectTransform>();
        if (!size.HasValue) size = new Vector2(400, 100); // Valor por defecto
        svRect.sizeDelta = size.Value;
        svRect.anchorMin = new Vector2(0, 0);
        svRect.anchorMax = new Vector2(0, 0);
        svRect.pivot = new Vector2(0, 1);

        // 3) Configurar el ScrollRect
        ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // 4) Configurar la imagen de fondo (opcional) - color muy claro
        Image bg = scrollViewObj.GetComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.1f);

        // 5) Crear el 'Viewport': un hijo con Mask para recortar el contenido
        GameObject viewportObj = new GameObject("Viewport", typeof(RectMask2D), typeof(Image));
        viewportObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
        vpRect.anchorMin = new Vector2(0, 0);
        vpRect.anchorMax = new Vector2(1, 1);
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        vpRect.pivot = new Vector2(0.5f, 0.5f);

        // La imagen del viewport sirve para tener un color distinto o dejarlo transparente
        Image vpImage = viewportObj.GetComponent<Image>();
        vpImage.color = new Color(1, 1, 1, 0f); // totalmente transparente
                                                // Ajustamos la propiedad de la Mask
        RectMask2D mask2D = viewportObj.GetComponent<RectMask2D>();

        // Vincular el viewport al ScrollRect
        scrollRect.viewport = vpRect;

        // 6) Crear el 'Content': un hijo dentro del viewport con Layout vertical
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;

        // Añadir VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 5;

        // ContentSizeFitter
        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Enlazar el content al ScrollRect
        scrollRect.content = contentRect;

        // 7) Retornar el transform del 'Content' para que se añadan ahí los mensajes
        return contentRect;
    }
}

