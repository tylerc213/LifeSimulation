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

using System;
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
        ApplyLeaderboardRowTypography();
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

        if (ShouldReassignLeaderboardRows())
            AutoAssignRowsFromContainers();
    }

    bool ShouldReassignLeaderboardRows()
    {
        if (nameRowTexts == null || scoreRowTexts == null)
            return true;
        if (nameRowTexts.Length == 0 || scoreRowTexts.Length == 0)
            return true;
        if (nameRowTexts.Length != scoreRowTexts.Length)
            return true;
        for (int i = 0; i < nameRowTexts.Length; i++)
        {
            if (nameRowTexts[i] == null)
                return true;
        }

        for (int i = 0; i < scoreRowTexts.Length; i++)
        {
            if (scoreRowTexts[i] == null)
                return true;
        }

        return false;
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
        TMP_FontAsset roboto = LifeSimUI.ButtonFont;
        if (roboto != null)
            return roboto;
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

        if (nameRowTexts == null || scoreRowTexts == null || nameRowTexts.Length == 0 ||
            nameRowTexts.Length != scoreRowTexts.Length)
        {
            SetStatus("Leaderboard rows not set up.");
            yield break;
        }

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
            if (nameRowTexts[i] == null || scoreRowTexts[i] == null)
                continue;

            NakamaLeaderboardService.LeaderboardRecord record = records[i];
            string displayName = NakamaLeaderboardService.ResolveDisplayName(record);
            nameRowTexts[i].text = string.IsNullOrEmpty(displayName)
                ? string.Empty
                : displayName.ToUpperInvariant();
            scoreRowTexts[i].text = NakamaLeaderboardService.ResolveScore(record);
        }

        SetStatus(records.Count > 0 ? string.Empty : "No records yet.");
    }

    void ClearRows()
    {
        if (nameRowTexts != null)
        {
            for (int i = 0; i < nameRowTexts.Length; i++)
            {
                if (nameRowTexts[i] != null)
                    nameRowTexts[i].text = "-";
            }
        }

        if (scoreRowTexts != null)
        {
            for (int i = 0; i < scoreRowTexts.Length; i++)
            {
                if (scoreRowTexts[i] != null)
                    scoreRowTexts[i].text = "-";
            }
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void AutoAssignRowsFromContainers()
    {
        // Prefer Row1..Row20 so each Name/Score pair comes from the same row (order matches Nakama rows).
        List<TMP_Text> names = CollectNameScoreFromNumberedRows(out List<TMP_Text> scores);
        if (names.Count > 0 && scores.Count == names.Count)
        {
            nameRowTexts = names.ToArray();
            scoreRowTexts = scores.ToArray();
            return;
        }

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
                        if (child != null && child.name.StartsWith("Row", StringComparison.Ordinal))
                            foundRows.Add(child);
                    }
                }
            }

            rowContainers = foundRows.ToArray();
        }

        names = new List<TMP_Text>();
        scores = new List<TMP_Text>();
        foreach (RectTransform row in rowContainers)
        {
            if (row == null)
                continue;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text name = texts.FirstOrDefault(t => t != null && t.gameObject.name == "Name");
            TMP_Text score = texts.FirstOrDefault(t => t != null && t.gameObject.name == "Score");

            if (name != null && score != null)
            {
                names.Add(name);
                scores.Add(score);
            }
        }

        if (names.Count == 0 || scores.Count == 0)
        {
            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            IOrderedEnumerable<TMP_Text> orderedNames = allTexts
                .Where(t => t != null && t.gameObject.name == "Name" && IsUnderDataRow(t.transform))
                .OrderBy(t => RowSortOrder(t.transform.parent));
            IOrderedEnumerable<TMP_Text> orderedScores = allTexts
                .Where(t => t != null && t.gameObject.name == "Score" && IsUnderDataRow(t.transform))
                .OrderBy(t => RowSortOrder(t.transform.parent));
            names = orderedNames.ToList();
            scores = orderedScores.ToList();
        }

        nameRowTexts = names.ToArray();
        scoreRowTexts = scores.ToArray();
    }

    static List<TMP_Text> CollectNameScoreFromNumberedRows(out List<TMP_Text> scoresOut)
    {
        var names = new List<TMP_Text>();
        scoresOut = new List<TMP_Text>();
        for (int i = 1; i <= 20; i++)
        {
            GameObject rowGo = GameObject.Find("Row" + i);
            if (rowGo == null)
                continue;

            TMP_Text nameTxt = null;
            TMP_Text scoreTxt = null;
            foreach (TMP_Text t in rowGo.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null)
                    continue;
                if (t.gameObject.name == "Name")
                    nameTxt = t;
                else if (t.gameObject.name == "Score")
                    scoreTxt = t;
            }

            if (nameTxt != null && scoreTxt != null)
            {
                names.Add(nameTxt);
                scoresOut.Add(scoreTxt);
            }
        }

        return names;
    }

    static bool IsUnderDataRow(Transform t)
    {
        Transform row = t.parent;
        return row != null && IsDataRowName(row.name);
    }

    static bool IsDataRowName(string rowName)
    {
        if (string.IsNullOrEmpty(rowName) || !rowName.StartsWith("Row", StringComparison.Ordinal) || rowName.Length <= 3)
            return false;
        string suffix = rowName.Substring(3);
        foreach (char c in suffix)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    static int RowSortOrder(Transform row)
    {
        if (row == null || !row.name.StartsWith("Row", StringComparison.Ordinal) || row.name.Length <= 3)
            return 9999;
        return int.TryParse(row.name.Substring(3), out int n) ? n : 9999;
    }

    /// <summary> Match row entry text to the rest of the UI: shared TMP font, semibold, slight tracking. </summary>
    void ApplyLeaderboardRowTypography()
    {
        TMP_FontAsset font = FindFirstFont();

        void StyleDataCell(TMP_Text t)
        {
            if (t == null)
                return;
            if (font != null)
                t.font = font;
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 1.25f;
            t.enableWordWrapping = false;
            t.fontSize = 24f;
        }

        if (nameRowTexts != null)
        {
            foreach (TMP_Text t in nameRowTexts)
                StyleDataCell(t);
        }

        if (scoreRowTexts != null)
        {
            foreach (TMP_Text t in scoreRowTexts)
                StyleDataCell(t);
        }
    }
}
