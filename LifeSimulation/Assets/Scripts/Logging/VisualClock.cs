// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Sim Clock
// Author:		Caden Nieves
// Date:		04/16/2026
// Version:		0.1.0
//
// Description:
//    Provides a visual timer of simulation length in standard format.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stores all clock related functions, start, stop, calculate
/// </summary>
public class VisualClock : MonoBehaviour
{
    public Text timerText;
    public float baseTimeScale = 2f;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    /// <summary>
    /// Update clock time and format to MM:SS:MIMIMI
    /// </summary>
    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime / baseTimeScale;

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            int milliseconds = Mathf.FloorToInt((elapsedTime * 1000) % 1000);

            timerText.text = string.Format("{0:00}:{1:00}:{2:000}",
                minutes, seconds, milliseconds);
        }
    }

    /// <summary>
    /// Start Clock
    /// </summary>
    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    /// <summary>
    /// Stop Clock
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

}
