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
    /// <summary>
    /// Generates population snapshot for current tick
    /// </summary>
    /// <param name="tick">Current tick used to timestamp snapshot</param>
    /// <returns>PopSnapshot containing population counts</returns>
    public PopSnapshot GetSnapshot(int tick)
    {
        //Temporary placeholder values for independent prototype
        int plants = Random.Range(50, 150);
        int grazers = Random.Range(10, 50);
        int predators = Random.Range(5, 20);

        return new PopSnapshot(tick, plants, grazers, predators);
    }
}
