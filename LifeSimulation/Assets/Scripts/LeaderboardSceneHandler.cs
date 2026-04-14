// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Leaderboard scene UI
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/05/2026
// Version:		0.0.0
//
// Description:
//    Leaderboard scene: category dropdown selects Nakama board IDs; loads top
//    rows and binds Name/Score TMP fields. Back navigates to main menu.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary> Leaderboard UI: fetch Nakama records per category, fill rows. </summary>
public class LeaderboardSceneHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Leaderboard UI")]
    public TMP_Dropdown categoryDropdown;
    public TMP_Text[] nameRowTexts;
    public TMP_Text[] scoreRowTexts;
    public TMP_Text statusText;
    public RectTransform[] rowContainers;

    void Start()
    {
        AutoAssignUiReferences();
        InitializeDropdown();
        StartCoroutine(RefreshSelectedCategoryRoutine());
    }

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary> Handles category dropdown selection changes. </summary>
    public void OnCategoryChanged()
    {
        StartCoroutine(RefreshSelectedCategoryRoutine());
    }

    private void InitializeDropdown()
    {
        if (categoryDropdown == null)
        {
            return;
        }

        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(new List<string>(LeaderboardBoards.DisplayNames));
        categoryDropdown.onValueChanged.RemoveAllListeners();
        categoryDropdown.onValueChanged.AddListener(_ => OnCategoryChanged());
        categoryDropdown.value = 0;
        categoryDropdown.RefreshShownValue();
    }

    private void AutoAssignUiReferences()
    {
        if (categoryDropdown == null)
        {
            categoryDropdown = FindFirstObjectByType<TMP_Dropdown>();
        }

        if ((nameRowTexts == null || nameRowTexts.Length == 0) || (scoreRowTexts == null || scoreRowTexts.Length == 0))
        {
            AutoAssignRowsFromContainers();
        }
    }

    private IEnumerator RefreshSelectedCategoryRoutine()
    {
        SetStatus("Loading...");
        ClearRows();

        NakamaLeaderboardService service = NakamaLeaderboardService.Instance;
        if (service == null)
        {
            SetStatus("Nakama service missing.");
            yield break;
        }

        int index = categoryDropdown != null ? Mathf.Clamp(categoryDropdown.value, 0, LeaderboardBoards.BoardIds.Length - 1) : 0;
        string boardId = LeaderboardBoards.BoardIds[index];

        List<NakamaLeaderboardService.LeaderboardRecord> records = null;
        yield return service.FetchTopRecords(boardId, Mathf.Min(nameRowTexts.Length, scoreRowTexts.Length), result => records = result);
        records = records ?? new List<NakamaLeaderboardService.LeaderboardRecord>();

        for (int i = 0; i < nameRowTexts.Length && i < scoreRowTexts.Length; i++)
        {
            if (i >= records.Count)
            {
                break;
            }

            NakamaLeaderboardService.LeaderboardRecord record = records[i];
            nameRowTexts[i].text = NakamaLeaderboardService.ResolveDisplayName(record);
            scoreRowTexts[i].text = NakamaLeaderboardService.ResolveScore(record);
        }

        SetStatus(records.Count > 0 ? string.Empty : "No records yet.");
    }

    private void ClearRows()
    {
        for (int i = 0; i < nameRowTexts.Length; i++)
        {
            if (nameRowTexts[i] != null)
            {
                nameRowTexts[i].text = "-";
            }
        }

        for (int i = 0; i < scoreRowTexts.Length; i++)
        {
            if (scoreRowTexts[i] != null)
            {
                scoreRowTexts[i].text = "-";
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void AutoAssignRowsFromContainers()
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
                    {
                        foundRows.Add(rect);
                    }
                }
            }

            if (foundRows.Count == 0)
            {
                // Fallback for scenes where rows are grouped under a single RowContainer object.
                GameObject container = GameObject.Find("RowContainer");
                if (container != null)
                {
                    foreach (RectTransform child in container.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (child != null && child.name.StartsWith("Row"))
                        {
                            foundRows.Add(child);
                        }
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
            {
                continue;
            }

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
            // Final fallback: collect all row texts named Name/Score and skip header labels.
            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            names = allTexts.Where(t => t != null && t.gameObject.name == "Name").Skip(1).ToList();
            scores = allTexts.Where(t => t != null && t.gameObject.name == "Score").Skip(1).ToList();
        }

        nameRowTexts = names.ToArray();
        scoreRowTexts = scores.ToArray();
    }
}
