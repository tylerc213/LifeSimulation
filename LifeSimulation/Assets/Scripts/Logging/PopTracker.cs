// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/04/2026
// Version:		0.1.0
//
// Description:
//    Tracks current population counts for each lifeform type and generates
//    PopSnapshot objects when requested by the logging system.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Provides populaation data for logging system by generating PopSnapshot
/// objects when requested by system.
/// </summary>
public class PopTracker : MonoBehaviour
{
    [Header("Optional Sources")]
    public SimulationManager simulationManager;

    public string plantPopulationKey = "Plant";
    public string grazerPopulationKey = "Grazer";
    public string predatorPopulationKey = "Predator";

    /// <summary>
    /// Generates population snapshot for current tick
    /// </summary>
    /// <param name="tick">Current tick used to timestamp snapshot</param>
    /// <returns>PopSnapshot containing population counts</returns>
    public PopSnapshot GetSnapshot(int tick)
    {
        int plants = TryGetPopulation(plantPopulationKey);
        int grazers = TryGetPopulation(grazerPopulationKey);
        int predators = TryGetPopulation(predatorPopulationKey);

        if (plants == 0 && grazers == 0 && predators == 0)
        {
            // Temporary fallback when sim dictionary keys are not configured yet.
            plants = Random.Range(50, 150);
            grazers = Random.Range(10, 50);
            predators = Random.Range(5, 20);
        }

        return new PopSnapshot(tick, plants, grazers, predators);
    }

    private int TryGetPopulation(string key)
    {
        if (simulationManager == null || string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        if (simulationManager.population == null || !simulationManager.population.TryGetValue(key, out int count))
        {
            return 0;
        }

        return Mathf.Max(0, count);
    }
}
