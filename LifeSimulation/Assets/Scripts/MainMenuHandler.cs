// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Captures user inputs from TextMeshPro for main menu UI interactions.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> handles main menu UI interactions </summary>
public class MainMenuHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string simulationSceneName = "Simulation";
    public string configurationSceneName = "Configuration";
    public string leaderboardSceneName = "Leaderboard";
    public string settingsSceneName = "Settings";
    public string creditsSceneName = "Credits";

    /// <summary> transitions user to simulation scene </summary>
    public void StartSimulation()
    {
        Debug.Log("Start Simulation Selected");
        SceneManager.LoadScene(simulationSceneName);
    }

    /// <summary> transitions user to configuration scene </summary>
    public void OpenConfiguration()
    {
        Debug.Log("Open Configuration Selected");
        SceneManager.LoadScene(configurationSceneName);
    }

    /// <summary> transitions user to leaderboard scene </summary>
    public void OpenLeaderboard()
    {
        Debug.Log("Open Leaderboard Selected");
        SceneManager.LoadScene(leaderboardSceneName);
    }

    /// <summary> transitions user to settings scene </summary>
    public void OpenSettings()
    {
        Debug.Log("Open Settings Selected");
        //SceneManager.LoadScene(settingsSceneName);
    }

    /// <summary> transitions user to credits scene </summary>
    public void OpenCredits()
    {
        Debug.Log("Open Credits Selected");
        SceneManager.LoadScene(creditsSceneName);
    }

    /// <summary> ends application </summary>
    public void Quit()
    {
        Debug.Log("Application Quit Selected");
        Application.Quit();
    }
}
