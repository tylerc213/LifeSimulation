// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
//
// Description:
//    Handles UI interactions for map generation and editor controls,
//    including toggling visibility of spawn buttons and managing
//    simulation start state.
// -----------------------------------------------------------------------------

using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles UI interactions and simulation control triggers.
/// </summary>
/// <remarks>
/// Responsible for enabling/disabling editor controls based on simulation state.
/// </remarks>
public class UIHandler : MonoBehaviour
{
    [Header("Generator Reference")]
    public MapGenerator2D mapGenerator;

    [Header("Spawn Button Visibility")]
    [Tooltip("Optional explicit button list. If empty, buttons are auto-found by name.")]
    public List<GameObject> spawnEntityButtons = new List<GameObject>();

    [Header("Generate Map Button")]
    [Tooltip("If unset, looks for GameObject named GenerateMapButton. Hidden once simulation has started.")]
    public GameObject generateMapButton;

    /// <summary>
    /// Initializes UI references and default visibility.
    /// </summary>
    void Start()
    {
        AutoAssignSpawnButtonsIfNeeded();
        AutoAssignGenerateMapButtonIfNeeded();

        // Hide spawn buttons until simulation begins
        UpdateSpawnButtonsVisibility(false);

        // Ensure generate button is visible at startup
        SetGenerateMapButtonVisible(true);
    }

    /// <summary>
    /// Handles generate button click and starts simulation.
    /// </summary>
    public void OnClickGenerate()
    {
        // Prevent null reference if generator is not assigned
        if (mapGenerator == null)
        {
            return;
        }

        // Trigger map generation process
        mapGenerator.GenerateMap();

        // Determine if simulation successfully started
        bool started = mapGenerator.IsMapReady && mapGenerator.HasSimulationStarted;

        // Enable spawn controls only after simulation begins
        UpdateSpawnButtonsVisibility(started);

        // Hide generate button after simulation starts
        SetGenerateMapButtonVisible(!started);

        // Notify editor panel of simulation state change
        if (started && EditorPanelController.Instance != null)
            EditorPanelController.Instance.NotifySimulationStarted();
    }

    /// <summary>
    /// Finds spawn buttons automatically if not manually assigned.
    /// </summary>
    private void AutoAssignSpawnButtonsIfNeeded()
    {
        // Skip if buttons are already assigned in inspector
        if (spawnEntityButtons != null && spawnEntityButtons.Count > 0)
        {
            return;
        }

        string[] buttonNames =
        {
            "SpawnGrazerButton",
            "SpawnPredatorButton",
            "SpawnPlantButton",
            "SpawnObstacleButton"
        };

        // Search scene for expected button names
        foreach (string buttonName in buttonNames)
        {
            GameObject button = GameObject.Find(buttonName);
            if (button != null)
            {
                spawnEntityButtons.Add(button);
            }
        }
    }

    /// <summary>
    /// Toggles visibility of spawn entity buttons.
    /// </summary>
    /// <param name="isVisible">Whether buttons should be visible.</param>
    private void UpdateSpawnButtonsVisibility(bool isVisible)
    {
        if (spawnEntityButtons == null)
        {
            return;
        }

        // Apply visibility state to all tracked buttons
        foreach (GameObject button in spawnEntityButtons)
        {
            if (button != null)
            {
                button.SetActive(isVisible);
            }
        }
    }

    /// <summary>
    /// Finds generate map button automatically if not assigned.
    /// </summary>
    private void AutoAssignGenerateMapButtonIfNeeded()
    {
        // Skip if already assigned
        if (generateMapButton != null)
        {
            return;
        }

        generateMapButton = GameObject.Find("GenerateMapButton");
    }

    /// <summary>
    /// Toggles visibility of generate map button.
    /// </summary>
    /// <param name="isVisible">Whether button should be visible.</param>
    private void SetGenerateMapButtonVisible(bool isVisible)
    {
        if (generateMapButton != null)
        {
            generateMapButton.SetActive(isVisible);
        }
    }
}
