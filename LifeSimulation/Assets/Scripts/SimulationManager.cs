// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
//
// Description:
//    Manages global simulation state including time scaling, pause control,
//    and population tracking. Handles simulation termination when a species
//    reaches zero population.
// -----------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls simulation time, pause state, and population tracking.
/// </summary>
/// <remarks>
/// Acts as a central authority for simulation flow and termination conditions.
/// </remarks>
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    /// <summary> Tracks population counts per species ID. </summary>
    public Dictionary<string, int> population = new Dictionary<string, int>();

    [Header("Simulation State")]
    public float currentSpeed = 1.0f;

    private bool isHalted = false;
    private bool isUserPaused = false;

    public SimulationSceneHandler simulationSceneHandler;

    /// <summary> Initializes singleton instance. </summary>
    void Awake() => Instance = this;

    /// <summary>
    /// Sets simulation speed and updates time scale.
    /// </summary>
    /// <param name="speed">Desired simulation speed multiplier.</param>
    public void SetSimulationSpeed(float speed)
    {
        // Clamp speed to prevent invalid or extreme values
        currentSpeed = Mathf.Clamp(speed, 0f, 10f);
        RefreshTimeScale();
    }

    /// <summary>
    /// Applies speed from external configuration.
    /// </summary>
    /// <param name="speed">Configured simulation speed.</param>
    public void ApplySettingsSpeed(float speed) => SetSimulationSpeed(speed);

    /// <summary>
    /// Toggles user pause state if simulation is not halted.
    /// </summary>
    public void TogglePause()
    {
        // Prevent pausing if simulation is already terminated
        if (isHalted)
            return;

        isUserPaused = !isUserPaused;
        RefreshTimeScale();
    }

    /// <summary>
    /// Explicitly sets pause state.
    /// </summary>
    /// <param name="paused">Whether simulation should be paused.</param>
    public void SetPaused(bool paused)
    {
        // Prevent override if simulation is halted
        if (isHalted)
            return;

        isUserPaused = paused;
        RefreshTimeScale();
    }

    /// <summary> Returns whether user pause is active. </summary>
    public bool IsUserPaused => isUserPaused;

    /// <summary> Returns whether simulation is halted. </summary>
    public bool IsHalted => isHalted;

    /// <summary>
    /// Prevents population updates during map regeneration.
    /// </summary>
    public bool SuppressPopulationSync { get; private set; }

    /// <summary>
    /// Resets simulation state and population counts.
    /// </summary>
    /// <remarks>
    /// Called before generating a new simulation run.
    /// </remarks>
    public void ResetPopulationState()
    {
        // Clear all tracked populations
        population.Clear();

        // Reset termination and sync flags
        isHalted = false;
        SuppressPopulationSync = false;

        RefreshTimeScale();
    }

    /// <summary>
    /// Enables or disables population synchronization.
    /// </summary>
    /// <param name="suppress">Whether to suppress updates.</param>
    public void SetSuppressPopulationSync(bool suppress)
    {
        SuppressPopulationSync = suppress;
    }

    /// <summary>
    /// Applies correct Time.timeScale based on simulation state.
    /// </summary>
    void RefreshTimeScale()
    {
        // If halted, completely stop simulation
        if (isHalted)
        {
            Time.timeScale = 0f;
            return;
        }

        // If paused, stop time but keep physics stable
        if (isUserPaused)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f;
            return;
        }

        // Apply simulation speed scaling
        Time.timeScale = currentSpeed;

        // Adjust physics timestep to match simulation speed
        Time.fixedDeltaTime = 0.02f * Mathf.Max(Time.timeScale, 0.0001f);
    }

    /// <summary>
    /// Updates population count for a species and checks termination condition.
    /// </summary>
    /// <param name="uniqueID">Species identifier.</param>
    /// <param name="change">Population change amount.</param>
    public void UpdatePopulation(string uniqueID, int change)
    {
        // Skip updates during regeneration to avoid false triggers
        if (SuppressPopulationSync)
            return;

        // Initialize species entry if not present
        if (!population.ContainsKey(uniqueID))
        {
            population.Add(uniqueID, 0);
        }

        // Apply population change
        population[uniqueID] += change;

        // Trigger simulation halt if species reaches zero
        if (population[uniqueID] <= 0 && !isHalted)
        {
            isHalted = true;

            // Stop simulation time immediately
            Time.timeScale = 0;

            // Transition to summary screen if handler exists
            if (simulationSceneHandler != null)
            {
                simulationSceneHandler.QuitToScoreSummary();
            }
        }
    }
}
