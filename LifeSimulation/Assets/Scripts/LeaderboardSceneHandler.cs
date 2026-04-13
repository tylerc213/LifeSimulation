// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Leaderboard scene UI
// Requirement:
// Author:		Benjamin Jones
// Date:		04/05/2026
// Version:		0.0.0
//
// Description:
//    Scene navigation for the leaderboard UI (e.g. return to main menu).
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> handles leaderboard scene UI actions </summary>
public class LeaderboardSceneHandler : MonoBehaviour
{
    [Header("Scene Configuration")]
    public string mainMenuSceneName = "MainMenu";

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
