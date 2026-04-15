// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Captures user inputs from TextMeshPro and triggers map generation
// -----------------------------------------------------------------------------

using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary> Handles UI element and simulation interaction </summary>
public class UIHandler : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField seedInput;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;

    [Header("Generator Reference")]
    public MapGenerator2D mapGenerator;
    [Header("Spawn Button Visibility")]
    [Tooltip("Optional explicit button list. If empty, buttons are auto-found by name.")]
    public List<GameObject> spawnEntityButtons = new List<GameObject>();

    [Header("Generate Map Button")]
    [Tooltip("If unset, looks for GameObject named GenerateMapButton. Hidden once simulation has started.")]
    public GameObject generateMapButton;

    void Start()
    {
        AutoAssignSpawnButtonsIfNeeded();
        AutoAssignGenerateMapButtonIfNeeded();
        UpdateSpawnButtonsVisibility(false);
        SetGenerateMapButtonVisible(true);
    }

    /// <summary> Triggers upon Map Generate button being clicked </summary>
    public void OnClickGenerate()
    {
        // Takes string data from seed field input to be used in map generation
        string seed = seedInput.text;

        if (mapGenerator == null)
        {
            return;
        }

        int width = 250;
        int height = 250;
        if (SimulationSettingsStore.Instance != null)
        {
            width = SimulationSettingsStore.Instance.Current.terrain.mapWidth;
            height = SimulationSettingsStore.Instance.Current.terrain.mapHeight;
        }

        // Pass taken data to map generation script (dimensions come from saved settings, not UI fields)
        mapGenerator.GenerateMap(seed, width, height);
        bool started = mapGenerator.IsMapReady && mapGenerator.HasSimulationStarted;
        UpdateSpawnButtonsVisibility(started);
        SetGenerateMapButtonVisible(!started);
    }

    private void AutoAssignSpawnButtonsIfNeeded()
    {
        if (spawnEntityButtons != null && spawnEntityButtons.Count > 0)
        {
            return;
        }

        string[] buttonNames =
        {
            "SpawnGrazerButton",
            "SpawnPredatorButton",
            "SpawnPlantButton"
        };

        foreach (string buttonName in buttonNames)
        {
            GameObject button = GameObject.Find(buttonName);
            if (button != null)
            {
                spawnEntityButtons.Add(button);
            }
        }
    }

    private void UpdateSpawnButtonsVisibility(bool isVisible)
    {
        if (spawnEntityButtons == null)
        {
            return;
        }

        foreach (GameObject button in spawnEntityButtons)
        {
            if (button != null)
            {
                button.SetActive(isVisible);
            }
        }
    }

    private void AutoAssignGenerateMapButtonIfNeeded()
    {
        if (generateMapButton != null)
        {
            return;
        }

        generateMapButton = GameObject.Find("GenerateMapButton");
    }

    private void SetGenerateMapButtonVisible(bool isVisible)
    {
        if (generateMapButton != null)
        {
            generateMapButton.SetActive(isVisible);
        }
    }
}
