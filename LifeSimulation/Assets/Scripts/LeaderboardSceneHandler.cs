// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Leaderboard scene UI
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/05/2026
// Version:		0.0.0
//
// Description:
//    Leaderboard scene: category bar (five buttons under LeaderboardPanel) selects
//    Nakama board IDs; loads top rows and binds Name/Score TMP fields. Back navigates to main menu.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary> Leaderboard UI: fetch Nakama records per category, fill rows. </summary>
public class LeaderboardSceneHandler : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";

    [Header("Leaderboard UI")]
    [Tooltip("RectTransform of CategoryButtonRow under LeaderboardPanel; optional if that name exists in scene.")]
    public RectTransform categoryBarRoot;

    public TMP_Text[] nameRowTexts;
    public TMP_Text[] scoreRowTexts;
    public TMP_Text statusText;
    public RectTransform[] rowContainers;

    static readonly Color CategoryNormalBg = new Color(0.28f, 0.42f, 0.5f, 1f);
    static readonly Color CategorySelectedBg = new Color(0.55f, 0.88f, 0.98f, 1f);
    static readonly Color CategoryNormalText = new Color(0.92f, 0.96f, 1f, 0.9f);
    static readonly Color CategorySelectedText = new Color(0.08f, 0.18f, 0.24f, 1f);

    /// <summary> Short labels for the five category buttons (same order as <see cref="LeaderboardBoards.BoardIds"/>). </summary>
    static readonly string[] CategoryButtonLabels =
    {
        "Overall",
        "Survival",
        "Stability",
        "Diversity",
        "Population"
    };

    Button[] _categoryButtons;
    int _selectedCategoryIndex;

    void Start()
    {
        AutoAssignUiReferences();
        EnsureCategoryBar();
        StartCoroutine(RefreshSelectedCategoryRoutine());
    }

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void AutoAssignUiReferences()
    {
        if (categoryBarRoot == null)
        {
            GameObject row = GameObject.Find("CategoryButtonRow");
            if (row != null)
                categoryBarRoot = row.GetComponent<RectTransform>();
        }

        if (nameRowTexts == null || nameRowTexts.Length == 0 || scoreRowTexts == null || scoreRowTexts.Length == 0)
            AutoAssignRowsFromContainers();
    }

    void EnsureCategoryBar()
    {
        if (categoryBarRoot == null)
        {
            _selectedCategoryIndex = 0;
            _categoryButtons = null;
            return;
        }

        if (categoryBarRoot.childCount >= LeaderboardBoards.BoardIds.Length)
        {
            _categoryButtons = new Button[LeaderboardBoards.BoardIds.Length];
            for (int i = 0; i < _categoryButtons.Length; i++)
                _categoryButtons[i] = categoryBarRoot.GetChild(i).GetComponent<Button>();
        }
        else
        {
            BuildCategoryButtons();
        }

        if (_categoryButtons == null || _categoryButtons.Length != LeaderboardBoards.BoardIds.Length)
        {
            _selectedCategoryIndex = 0;
            return;
        }

        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            int idx = i;
            _categoryButtons[i].onClick.RemoveAllListeners();
            _categoryButtons[i].onClick.AddListener(() => OnCategoryButtonClicked(idx));
        }

        _selectedCategoryIndex = 0;
        UpdateCategoryVisuals();
    }

    void BuildCategoryButtons()
    {
        for (int c = categoryBarRoot.childCount - 1; c >= 0; c--)
            Destroy(categoryBarRoot.GetChild(c).gameObject);

        HorizontalLayoutGroup h = categoryBarRoot.GetComponent<HorizontalLayoutGroup>();
        if (h == null)
            h = categoryBarRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        h.padding = new RectOffset(6, 6, 4, 4);
        h.spacing = 8;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        TMP_FontAsset font = FindFirstFont();

        _categoryButtons = new Button[LeaderboardBoards.BoardIds.Length];
        for (int i = 0; i < LeaderboardBoards.BoardIds.Length; i++)
        {
            GameObject go = new GameObject($"CategoryButton{i}", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(categoryBarRoot, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;
            le.flexibleWidth = 1f;

            Image img = go.GetComponent<Image>();
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.color = CategoryNormalBg;

            Button b = go.GetComponent<Button>();
            b.targetGraphic = img;
            ColorBlock colors = b.colors;
            colors.fadeDuration = 0.08f;
            b.colors = colors;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(4f, 2f);
            lr.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = CategoryButtonLabels[i];
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = 14f;
            tmp.color = CategoryNormalText;
            if (font != null)
                tmp.font = font;

            _categoryButtons[i] = b;
        }
    }

    static TMP_FontAsset FindFirstFont()
    {
        TMP_Text sample = FindFirstObjectByType<TMP_Text>();
        return sample != null ? sample.font : null;
    }

    void OnCategoryButtonClicked(int index)
    {
        index = Mathf.Clamp(index, 0, LeaderboardBoards.BoardIds.Length - 1);
        if (_selectedCategoryIndex == index)
            return;

        _selectedCategoryIndex = index;
        UpdateCategoryVisuals();
        StartCoroutine(RefreshSelectedCategoryRoutine());
    }

    void UpdateCategoryVisuals()
    {
        if (_categoryButtons == null)
            return;

        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            bool sel = i == _selectedCategoryIndex;
            Image img = _categoryButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = sel ? CategorySelectedBg : CategoryNormalBg;

            TextMeshProUGUI tmp = _categoryButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = sel ? CategorySelectedText : CategoryNormalText;
                tmp.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    IEnumerator RefreshSelectedCategoryRoutine()
    {
        SetStatus("Loading...");
        ClearRows();

        NakamaLeaderboardService service = NakamaLeaderboardService.Instance;
        if (service == null)
        {
            SetStatus("Nakama service missing.");
            yield break;
        }

        int index = Mathf.Clamp(_selectedCategoryIndex, 0, LeaderboardBoards.BoardIds.Length - 1);
        string boardId = LeaderboardBoards.BoardIds[index];

        List<NakamaLeaderboardService.LeaderboardRecord> records = null;
        yield return service.FetchTopRecords(boardId, Mathf.Min(nameRowTexts.Length, scoreRowTexts.Length),
            result => records = result);
        records = records ?? new List<NakamaLeaderboardService.LeaderboardRecord>();

        for (int i = 0; i < nameRowTexts.Length && i < scoreRowTexts.Length; i++)
        {
            if (i >= records.Count)
                break;

            NakamaLeaderboardService.LeaderboardRecord record = records[i];
            nameRowTexts[i].text = NakamaLeaderboardService.ResolveDisplayName(record);
            scoreRowTexts[i].text = NakamaLeaderboardService.ResolveScore(record);
        }

        SetStatus(records.Count > 0 ? string.Empty : "No records yet.");
    }

    void ClearRows()
    {
        for (int i = 0; i < nameRowTexts.Length; i++)
        {
            if (nameRowTexts[i] != null)
                nameRowTexts[i].text = "-";
        }

        for (int i = 0; i < scoreRowTexts.Length; i++)
        {
            if (scoreRowTexts[i] != null)
                scoreRowTexts[i].text = "-";
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void AutoAssignRowsFromContainers()
    {
        if (rowContainers == null || rowContainers.Length == 0)
        {
            List<RectTransform> foundRows = new List<RectTransform>();
            for (int i = 1; i <= 20; i++)
            {
                GameObject row = GameObject.Find("Row" + i);
                if (row != null)
                {
                    RectTransform rect = row.GetComponent<RectTransform>();
                    if (rect != null)
                        foundRows.Add(rect);
                }
            }

            if (foundRows.Count == 0)
            {
                GameObject container = GameObject.Find("RowContainer");
                if (container != null)
                {
                    foreach (RectTransform child in container.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (child != null && child.name.StartsWith("Row"))
                            foundRows.Add(child);
                    }
                }
            }

            rowContainers = foundRows.ToArray();
        }

        List<TMP_Text> names = new List<TMP_Text>();
        List<TMP_Text> scores = new List<TMP_Text>();

        foreach (RectTransform row in rowContainers)
        {
            if (row == null)
                continue;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text name = texts.FirstOrDefault(t => t.gameObject.name == "Name");
            TMP_Text score = texts.FirstOrDefault(t => t.gameObject.name == "Score");

            if (name != null && score != null)
            {
                names.Add(name);
                scores.Add(score);
            }
        }

        if (names.Count == 0 || scores.Count == 0)
        {
            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            names = allTexts.Where(t => t != null && t.gameObject.name == "Name").Skip(1).ToList();
            scores = allTexts.Where(t => t != null && t.gameObject.name == "Score").Skip(1).ToList();
        }

        nameRowTexts = names.ToArray();
        scoreRowTexts = scores.ToArray();
    }
}
