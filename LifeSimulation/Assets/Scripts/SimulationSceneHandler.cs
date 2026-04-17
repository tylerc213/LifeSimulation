// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation scene UI
// Requirement:	Leaderboard
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Simulation scene: on quit/finish, finalizes logging, builds
//    ScoreSummaryPayload (SummaryGenerator or fallback from PopTracker), stores
//    it in ScoreSummaryData, then loads ScoreSummary. Ensures Quit control on
//    EditorPanel when present.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary> End-of-run transition from Simulation to score summary scene. </summary>
public class SimulationSceneHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string scoreSummarySceneName = "ScoreSummary";

    [Header("References")]
    public LogManager logManager;
    public SimulationLogger simulationLogger;
    public PopTracker popTracker;
    public MapGenerator2D mapGenerator;

    void Start()
    {
        AutoAssignReferences();
        EnsureEditorPanelQuitButton();
    }

    /// <summary> Finalizes run values and opens score summary scene. </summary>
    public void QuitToScoreSummary()
    {
        Debug.Log("Simulation Quit Selected");

        if (!HasSimulationStarted())
        {
            // Leave the scene anytime; no run was started so scores stay default zeros.
            ScoreSummaryData.SetCurrentRun(new ScoreSummaryPayload());
        }
        else if (logManager != null && simulationLogger != null)
        {
            logManager.LogFinalSnapshot();
            ScoreSummaryPayload generated = SummaryGenerator.GenerateSummaryPayload(simulationLogger.filepath);
            ScoreSummaryData.SetCurrentRun(generated);
        }
        else
        {
            ScoreSummaryData.SetCurrentRun(BuildFallbackPayload());
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(scoreSummarySceneName);
    }

    /// <summary> UI alias that mirrors other scene button naming. </summary>
    public void Quit()
    {
        QuitToScoreSummary();
    }

    private ScoreSummaryPayload BuildFallbackPayload()
    {
        PopSnapshot snapshot;
        if (popTracker != null)
        {
            snapshot = popTracker.GetSnapshot(0);
        }
        else if (EcosystemManager.Instance != null)
        {
            EcosystemManager eco = EcosystemManager.Instance;
            snapshot = new PopSnapshot(0, eco.PlantCount, eco.GrazerCount, eco.PredatorCount);
        }
        else
        {
            snapshot = new PopSnapshot(0, 0, 0, 0);
        }

        int diversity = 0;
        diversity += snapshot.plantCount > 0 ? 1 : 0;
        diversity += snapshot.grazerCount > 0 ? 1 : 0;
        diversity += snapshot.predatorCount > 0 ? 1 : 0;

        return new ScoreSummaryPayload
        {
            longestSurvivalTime = 0,
            highestPopulationPeak = snapshot.totalPop,
            highestDiversity = diversity,
            highestStability = 0,
            overallScore = snapshot.totalPop
        };
    }

    private void EnsureEditorPanelQuitButton()
    {
        GameObject editorPanel = GameObject.Find("EditorPanel");
        if (editorPanel == null)
        {
            return;
        }

        Transform existing = editorPanel.transform.Find("QuitToSummaryButton");
        if (existing == null)
        {
            existing = editorPanel.transform.Find("Quit");
        }

        if (existing != null)
        {
            existing.name = "Quit";
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
            {
                existingButton.onClick.RemoveAllListeners();
                existingButton.onClick.AddListener(Quit);
            }

            return;
        }

        GameObject buttonObject = new GameObject("Quit", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(editorPanel.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 30f);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(0f, -200f);

        Image image = buttonObject.GetComponent<Image>();
        image.type = Image.Type.Sliced;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(Quit);

        GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Quit";
        label.alignment = TextAlignmentOptions.Center;
        CopyTmpFontFromScene(label);

        ApplyQuitToolbarStyle(buttonObject.transform);
    }

    static void ApplyQuitToolbarStyle(Transform quitRoot)
    {
        LifeSimUIButtonStyle.ApplyStripButton(quitRoot.gameObject, LifeSimUI.Theme, false);
        TextMeshProUGUI label = quitRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            CopyTmpFontFromScene(label);
    }

    static void CopyTmpFontFromScene(TextMeshProUGUI target)
    {
        TextMeshProUGUI sample = Object.FindFirstObjectByType<TextMeshProUGUI>();
        if (sample != null && target != null && sample.font != null)
            target.font = sample.font;
    }

    private void AutoAssignReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator2D>();
        }

        if (logManager == null)
        {
            logManager = FindFirstObjectByType<LogManager>();
        }

        if (simulationLogger == null)
        {
            simulationLogger = FindFirstObjectByType<SimulationLogger>();
        }

        if (popTracker == null)
        {
            popTracker = FindFirstObjectByType<PopTracker>();
        }
    }

    private bool HasSimulationStarted()
    {
        return mapGenerator != null && mapGenerator.HasSimulationStarted && mapGenerator.IsMapReady;
    }
}
