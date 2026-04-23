// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:
// Author:		Tyler Craig
// Date:		4/23/2026
// Version:		0.0.0
//
// Description:
//    Captures user inputs from TextMeshPro for credits UI interactions.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> handles credits UI interactions </summary>
public class CreditsHandler : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";

    /// <summary> returns to main menu scene </summary>
    public void BackToMainMenu()
    {
        Debug.Log("Back To Main Menu Selected");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
