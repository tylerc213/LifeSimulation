// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Scene Navigation
// Author:		Robert Amborski
// Date:		03/25/2026
//
// Description:
//    Handles main menu navigation by routing user selections to the
//    appropriate scenes within the simulation application.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages main menu button interactions and scene transitions.
/// </summary>
/// <remarks>
/// Each method is intended to be hooked directly to UI button OnClick events.
/// Centralizes scene routing to keep menu logic simple and maintainable.
/// </remarks>
public class MainMenuHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string simulationSceneName = "Simulation";
    public string configurationSceneName = "Configuration";
    public string leaderboardSceneName = "Leaderboard";
    public string settingsSceneName = "Settings";
    public string creditsSceneName = "Credits";

    /// <summary>
    /// Loads the simulation scene to begin a run.
    /// </summary>
    public void StartSimulation()
    {
        // Log selection for debugging UI interaction flow
        Debug.Log("Start Simulation Selected");

        // Transition to main simulation environment
        SceneManager.LoadScene(simulationSceneName);
    }

    /// <summary>
    /// Loads the configuration scene for adjusting simulation parameters.
    /// </summary>
    public void OpenConfiguration()
    {
        Debug.Log("Open Configuration Selected");

        // Navigate to configuration/setup interface
        SceneManager.LoadScene(configurationSceneName);
    }

    /// <summary>
    /// Loads the leaderboard scene to view past results.
    /// </summary>
    public void OpenLeaderboard()
    {
        Debug.Log("Open Leaderboard Selected");

        // Navigate to performance comparison screen
        SceneManager.LoadScene(leaderboardSceneName);
    }

    /// <summary>
    /// Placeholder for settings scene navigation.
    /// </summary>
    /// <remarks>
    /// Scene loading is currently disabled; method retained for future expansion.
    /// </remarks>
    public void OpenSettings()
    {
        Debug.Log("Open Settings Selected");

        // Scene intentionally disabled until settings scene is implemented
        // SceneManager.LoadScene(settingsSceneName);
    }

    /// <summary>
    /// Loads the credits scene.
    /// </summary>
    public void OpenCredits()
    {
        Debug.Log("Open Credits Selected");

        // Navigate to credits/attribution screen
        SceneManager.LoadScene(creditsSceneName);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    /// <remarks>
    /// Will not stop play mode in the Unity editor; only works in a built application.
    /// </remarks>
    public void Quit()
    {
        Debug.Log("Application Quit Selected");

        // Close application runtime
        Application.Quit();
    }
}
