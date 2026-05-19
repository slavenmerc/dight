using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private const string MenuSceneName = "menu";

    private GameObject menuRoot;
    private MouseLook[] mouseLooks;
    private bool[] mouseLookStates;
    private bool isPaused;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForCurrentScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name == MenuSceneName || FindFirstObjectByType<PauseMenuController>() != null)
        {
            return;
        }

        new GameObject("Pause Menu Controller").AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        BuildMenu();
        EnsureEventSystem();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        menuRoot.SetActive(true);

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mouseLooks = FindObjectsByType<MouseLook>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        mouseLookStates = new bool[mouseLooks.Length];

        for (int i = 0; i < mouseLooks.Length; i++)
        {
            mouseLookStates[i] = mouseLooks[i].enabled;
            mouseLooks[i].enabled = false;
        }
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;
        menuRoot.SetActive(false);
        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        if (mouseLooks == null || mouseLookStates == null)
        {
            return;
        }

        for (int i = 0; i < mouseLooks.Length; i++)
        {
            if (mouseLooks[i] != null)
            {
                mouseLooks[i].enabled = mouseLookStates[i];
            }
        }
    }

    public void ReturnToMenu()
    {
        SaveGameManager.SaveCurrentGame();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MenuSceneName);
    }

    private void BuildMenu()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("Pause Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        menuRoot = new GameObject("Pause Menu");
        menuRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = menuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image overlay = menuRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject window = new GameObject("Return To Menu Window");
        window.transform.SetParent(menuRoot.transform, false);

        RectTransform windowRect = window.AddComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(520f, 250f);

        Image windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.08f, 0.08f, 0.09f, 0.96f);

        GameObject title = CreateText("Exit?", font, 38, TextAnchor.MiddleCenter);
        title.transform.SetParent(window.transform, false);

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.52f);
        titleRect.anchorMax = new Vector2(1f, 0.95f);
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-24f, 0f);

        GameObject noButton = CreateButton("No", font, new Color(0.2f, 0.2f, 0.22f), ResumeGame);
        noButton.transform.SetParent(window.transform, false);
        SetButtonRect(noButton, 0.1f, 0.45f);

        GameObject yesButton = CreateButton("Yes", font, new Color(0.55f, 0.14f, 0.14f), ReturnToMenu);
        yesButton.transform.SetParent(window.transform, false);
        SetButtonRect(yesButton, 0.55f, 0.9f);

        menuRoot.SetActive(false);
    }

    private static void SetButtonRect(GameObject buttonObject, float minX, float maxX)
    {
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, 0.14f);
        rect.anchorMax = new Vector2(maxX, 0.39f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateText(string text, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(text);
        textObject.AddComponent<RectTransform>();

        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;

        return textObject;
    }

    private static GameObject CreateButton(string labelText, Font font, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(labelText + " Button");
        buttonObject.AddComponent<RectTransform>();

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject label = CreateText(labelText, font, 28, TextAnchor.MiddleCenter);
        label.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObject;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
