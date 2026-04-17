// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Score Summary scene UI
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Post-simulation scene: shows the five Nakama category scores from
//    ScoreSummaryData (filled when leaving Simulation), optional arcade name,
//    Submit (writes each board via NakamaLeaderboardService) or Quit to main
//    menu. Can build a minimal themed panel at runtime if the scene has no UI.
// -----------------------------------------------------------------------------

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary> End-of-run summary UI: display metrics, submit to Nakama, or exit. </summary>
public class ScoreSummaryHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI References")]
    public TMP_InputField nameInput;
    public TMP_Text[] categoryLineTexts;
    public TMP_Text statusText;

    private bool isSubmitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHandlerExists()
    {
        if (SceneManager.GetActiveScene().name != "ScoreSummary")
        {
            return;
        }

        if (FindFirstObjectByType<ScoreSummaryHandler>() != null)
        {
            return;
        }

        GameObject handlerObject = new GameObject("ScoreSummarySceneRoot");
        handlerObject.AddComponent<ScoreSummaryHandler>();
    }

    void Start()
    {
        EnsureScoreSummaryLayout();
        AutoAssignUiReferences();
        RenderSummary();
    }

    /// <summary> Writes all category values to Nakama and returns to main menu. </summary>
    public void SubmitAndQuit()
    {
        if (isSubmitting)
        {
            return;
        }

        StartCoroutine(SubmitAllScoresRoutine());
    }

    /// <summary> Discards score submission and returns to main menu. </summary>
    public void QuitWithoutSubmitting()
    {
        Debug.Log("Score submission discarded by player.");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void RenderSummary()
    {
        ScoreSummaryPayload payload = ScoreSummaryData.CurrentRun ?? new ScoreSummaryPayload();
        long[] values = payload.ToOrderedArray();

        for (int i = 0; i < categoryLineTexts.Length && i < values.Length; i++)
        {
            if (categoryLineTexts[i] != null)
            {
                categoryLineTexts[i].text = $"{LeaderboardBoards.DisplayNames[i]} - {values[i]}";
            }
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    private IEnumerator SubmitAllScoresRoutine()
    {
        isSubmitting = true;
        SetStatus("Submitting scores...");

        NakamaLeaderboardService service = NakamaLeaderboardService.Instance;
        if (service == null)
        {
            SetStatus("Nakama service missing.");
            isSubmitting = false;
            yield break;
        }

        service.BeginNewSubmissionSession();

        string displayName = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text)
            ? nameInput.text.Trim()
            : "Guest";

        long[] values = (ScoreSummaryData.CurrentRun ?? new ScoreSummaryPayload()).ToOrderedArray();
        bool allSucceeded = true;

        for (int i = 0; i < LeaderboardBoards.BoardIds.Length && i < values.Length; i++)
        {
            bool succeeded = false;
            yield return service.SubmitScore(LeaderboardBoards.BoardIds[i], values[i], displayName, ok => succeeded = ok);
            allSucceeded &= succeeded;
        }

        if (!allSucceeded)
        {
            SetStatus("Submit failed. Check Nakama connection.");
            isSubmitting = false;
            yield break;
        }

        SetStatus("Submit complete.");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void AutoAssignUiReferences()
    {
        if (nameInput == null)
        {
            nameInput = FindFirstObjectByType<TMP_InputField>();
        }

        if (categoryLineTexts == null || categoryLineTexts.Length == 0)
        {
            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            categoryLineTexts = allTexts
                .Where(t => t != null && t.gameObject.name.StartsWith("CategoryLine"))
                .OrderBy(t => t.gameObject.name)
                .ToArray();
        }
    }

    private void EnsureScoreSummaryLayout()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            // Mirrors Configuration scene CanvasScaler defaults.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.scaleFactor = 1f;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.physicalUnit = CanvasScaler.Unit.Points;
            scaler.fallbackScreenDPI = 96f;
            scaler.defaultSpriteDPI = 96f;
            scaler.dynamicPixelsPerUnit = 1f;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            // Mirrors Configuration scene EventSystem module choice.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        Transform panel = canvas.transform.Find("ScoreSummaryPanel");
        if (panel == null)
        {
            GameObject panelObject = new GameObject("ScoreSummaryPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            RectTransform createdPanelRect = panelObject.GetComponent<RectTransform>();
            createdPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            createdPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            createdPanelRect.pivot = new Vector2(0.5f, 0.5f);
            createdPanelRect.sizeDelta = new Vector2(1100f, 760f);
            createdPanelRect.anchoredPosition = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color32(20, 34, 45, 235);
            panel = panelObject.transform;
        }
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 760f);

        // Clear pre-existing mixed content so we keep one clean functional copy.
        foreach (Transform child in panel)
        {
            Destroy(child.gameObject);
        }

        CreateLabel(panel, "Title", "Score Summary", new Vector2(0f, 300f), 52, TextAlignmentOptions.Center, Color.white);

        for (int i = 0; i < LeaderboardBoards.DisplayNames.Length; i++)
        {
            float y = 180f - (i * 75f);
            CreateLabel(panel, $"CategoryLine{i + 1}", $"{LeaderboardBoards.DisplayNames[i]} - 0", new Vector2(0f, y), 34, TextAlignmentOptions.Center, Color.white);
        }

        nameInput = CreateNameInput(panel, new Vector2(0f, -230f));
        CreateButton(panel, "SubmitButton", "Submit", new Vector2(-140f, -315f), SubmitAndQuit);
        CreateButton(panel, "QuitButton", "Quit", new Vector2(140f, -315f), QuitWithoutSubmitting);
        statusText = CreateLabel(panel, "StatusText", string.Empty, new Vector2(0f, -270f), 24, TextAlignmentOptions.Center, Color.white);
    }

    private TMP_Text CreateLabel(Transform parent, string objectName, string text, Vector2 position, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 60f);
        rect.anchoredPosition = position;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        return label;
    }

    private TMP_InputField CreateNameInput(Transform parent, Vector2 position)
    {
        GameObject root = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(580f, 64f);
        rootRect.anchoredPosition = position;
        root.GetComponent<Image>().color = new Color32(245, 245, 245, 255);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(root.transform, false);
        RectTransform areaRect = textArea.GetComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0f, 0f);
        areaRect.anchorMax = new Vector2(1f, 1f);
        areaRect.offsetMin = new Vector2(12f, 8f);
        areaRect.offsetMax = new Vector2(-12f, -8f);

        TextMeshProUGUI textComponent = CreateLabel(textArea.transform, "Text", string.Empty, Vector2.zero, 28, TextAlignmentOptions.Left, new Color32(40, 40, 40, 255)) as TextMeshProUGUI;
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholder = CreateLabel(textArea.transform, "Placeholder", "Enter Name", Vector2.zero, 28, TextAlignmentOptions.Left, new Color32(120, 120, 120, 255)) as TextMeshProUGUI;
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0f, 0f);
        placeholderRect.anchorMax = new Vector2(1f, 1f);
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TMP_InputField inputField = root.GetComponent<TMP_InputField>();
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholder;
        inputField.characterLimit = 20;
        return inputField;
    }

    private void CreateButton(Transform parent, string objectName, string labelText, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(260f, 68f);
        buttonRect.anchoredPosition = position;

        buttonObject.GetComponent<Image>().color = new Color32(27, 182, 176, 255);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TMP_Text label = CreateLabel(buttonObject.transform, "Text (TMP)", labelText, Vector2.zero, 30, TextAlignmentOptions.Center, new Color32(45, 45, 45, 255));
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
