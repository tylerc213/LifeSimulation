// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/04/2026
// Version:		0.2.0
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
    public MapGenerator2D mapGenerator;
    private bool hasLoggedInitialSnapshot;

    void OnEnable()
    {
        MapGenerator2D.OnMapGenerated += OnSimulationMapReady;
    }

    void OnDisable()
    {
        MapGenerator2D.OnMapGenerated -= OnSimulationMapReady;
    }

    /// <summary> Reset tick tracking when a new map is generated (Generate Map), not on scene load. </summary>
    void OnSimulationMapReady()
    {
        currentTick = 0;
        hasLoggedInitialSnapshot = false;
    }

    /// <summary>
    /// When simualtion starts --> log inital population
    /// </summary>
    void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator2D>();
        }
    }

    /// <summary>
    /// Used to log snpashot when simulation ends.
    /// </summary>
    public void LogFinalSnapshot()
    {
        PopSnapshot snapshot = popTracker.GetSnapshot(currentTick);

        simulationLogger.SaveToFile(snapshot);
        LogPopulationEvents(snapshot, currentTick);
    }

    /// <summary> Extinction / endangered / overpopulation lines — same rules as periodic logging. </summary>
    void LogPopulationEvents(PopSnapshot snapshot, int tick)
    {
        // Extinction Events
        if (snapshot.plantCount == 0)
        {
            simulationLogger.LogEvent("Extinction", "Plants have gone extinct", tick);
        }

        if (snapshot.grazerCount == 0)
        {
            simulationLogger.LogEvent("Extinction", "Grazers have gone extinct", tick);
        }

        if (snapshot.predatorCount == 0)
        {
            simulationLogger.LogEvent("Extinction", "Predators have gone extinct", tick);
        }

        // Endangered Events
        if (snapshot.plantCount < 3)
        {
            simulationLogger.LogEvent("Endangered", "Plants population currently endangered", tick);
        }

        if (snapshot.grazerCount < 3)
        {
            simulationLogger.LogEvent("Endangered", "Grazers population currently endangered", tick);
        }

        if (snapshot.predatorCount < 2)
        {
            simulationLogger.LogEvent("Endangered", "Predators population currently endangered", tick);
        }

        // Overpopulation Events
        if (snapshot.plantCount > 50)
        {
            simulationLogger.LogEvent("OverPopulation", "The plants have expanded beyond expectation", tick);
        }

        if (snapshot.grazerCount > 40)
        {
            simulationLogger.LogEvent("OverPopulation", "The grazers have expanded beyond expectation", tick);
        }

        if (snapshot.predatorCount > 30)
        {
            simulationLogger.LogEvent("OverPopulation", "The predators have expanded beyond expectation", tick);
        }
    }

    /// <summary>
    /// Update() called once per frame by unity. Advances tick and triggers
    /// logging when interval is reached or an event is triggered.
    /// </summary>
    void Update()
    {
        if (!IsSimulationStarted())
        {
            return;
        }

        if (!hasLoggedInitialSnapshot)
        {
            // First logged tick is now tied to map generation (true simulation start).
            PopSnapshot startSnapshot = popTracker.GetSnapshot(currentTick);
            simulationLogger.SaveToFile(startSnapshot);
            hasLoggedInitialSnapshot = true;
        }

        currentTick++;

        if (currentTick % logInterval == 0)
        {
            // Pulls snapshot from PopTracker
            PopSnapshot snapshot = popTracker.GetSnapshot(currentTick);
            
            // Sends snapshot to SimulationLogger for file storage
            simulationLogger.SaveToFile(snapshot);

            LogPopulationEvents(snapshot, currentTick);

        }
    }

    /// <summary>
    /// Checks whether the simulation is ready to run.
    /// Ensures map generator exist, simulation has started, map finished generating.
    /// </summary>
    /// <returns>true if simulation started and map is ready</returns>
    private bool IsSimulationStarted()
    {
        return mapGenerator != null && mapGenerator.HasSimulationStarted && mapGenerator.IsMapReady;
    }

}
