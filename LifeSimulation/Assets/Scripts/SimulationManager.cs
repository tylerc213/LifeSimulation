// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    public Dictionary<string, int> population = new Dictionary<string, int>();

    [Header("Simulation State")]
    public float currentSpeed = 1.0f;
    private bool isHalted = false;
    private bool isUserPaused = false;
    public SimulationSceneHandler simulationSceneHandler;

    void Awake() => Instance = this;

    /// <summary> Updates desired speed and applies time scale unless paused or halted. </summary>
    public void SetSimulationSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, 0f, 10f);
        RefreshTimeScale();
    }

    /// <summary> Used by SimulationSettingsStore when applying JSON. </summary>
    public void ApplySettingsSpeed(float speed) => SetSimulationSpeed(speed);

    public void TogglePause()
    {
        if (isHalted)
            return;

        isUserPaused = !isUserPaused;
        RefreshTimeScale();
    }

    public void SetPaused(bool paused)
    {
        if (isHalted)
            return;

        isUserPaused = paused;
        RefreshTimeScale();
    }

    public bool IsUserPaused => isUserPaused;

    public bool IsHalted => isHalted;

    /// <summary> Skipped during map regen so destroy callbacks do not desync counts or trigger game over. </summary>
    public bool SuppressPopulationSync { get; private set; }

    /// <summary> Clears per-species tallies, clears halt, and resumes time scale — call at the start of a new map. </summary>
    public void ResetPopulationState()
    {
        population.Clear();
        isHalted = false;
        SuppressPopulationSync = false;
        RefreshTimeScale();
    }

    public void SetSuppressPopulationSync(bool suppress)
    {
        SuppressPopulationSync = suppress;
    }

    void RefreshTimeScale()
    {
        if (isHalted)
        {
            Time.timeScale = 0f;
            return;
        }

        if (isUserPaused)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f;
            return;
        }

        Time.timeScale = currentSpeed;
        Time.fixedDeltaTime = 0.02f * Mathf.Max(Time.timeScale, 0.0001f);
    }

    public void UpdatePopulation(string uniqueID, int change)
    {
        if (SuppressPopulationSync)
            return;

        if (!population.ContainsKey(uniqueID))
        {
            population.Add(uniqueID, 0);
        }

        population[uniqueID] += change;

        if (population[uniqueID] <= 0 && !isHalted)
        {
            isHalted = true;
            Time.timeScale = 0;

            if (simulationSceneHandler != null)
            {
                simulationSceneHandler.QuitToScoreSummary();
            }
        }
    }
}
