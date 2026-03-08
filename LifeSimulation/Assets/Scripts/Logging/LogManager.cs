// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/04/2026
// Version:		0.1.0
//
// Description:
//    Controls the logging process by tracking frame progression and triggering
//    population logging at specific intervals. Coordinates communication
//    between the PopTracker and SimulationLogger systems.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Manages when snapshots are recorded during runtime and tracks frame 
/// progression (tick rate).
/// </summary>
public class LogManager : MonoBehaviour
{
    public int currentTick = 0;
    public int logInterval = 600;

    public PopTracker popTracker;
    public SimulationLogger simulationLogger;

    /// <summary>
    /// Update() called once per frame by unity. Advances tick and triggers
    /// logging when interval is reached
    /// </summary>
    void Update()
    {
        currentTick++;

        if (currentTick % logInterval == 0)
        {
            // Pulls snapshot from PopTracker
            PopSnapshot snapshot = popTracker.GetSnapshot(currentTick);
            
            // Sends snapshot to SimulationLogger for file storage
            simulationLogger.SaveToFile(snapshot);
        }
    }
}
