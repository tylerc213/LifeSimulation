// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/04/2026
// Version:		0.2.0
//
// Description:
//    Represents a data structure containing a snapshot of population counts
//    at a specific simulation tick, including plants, grazers, and predators.
// -----------------------------------------------------------------------------

using System;

/// <summary>
/// Stores population data for a single moment of the simulation.
/// Used by logging system to record population counts over time.
/// </summary>
[Serializable]
public class PopSnapshot
{
    public int tick;
    public int plantCount;
    public int grazerCount;
    public int predatorCount;
    public int totalPop;

    /// <summary>
    /// Creates new population snapshot containg lifeform counts at a 
    /// specific tick.
    /// </summary>
    /// <param name="tick">Sim tick representing when snapshot occured</param>
    /// <param name="plants"># of plants present in sim</param>
    /// <param name="grazers"># of grazers present in sim</param>
    /// <param name="predators"># of predators present in sim</param>
    public PopSnapshot(int tick, int plants, int grazers, int predators)
    {
        this.tick = tick;
        plantCount = plants;
        grazerCount = grazers;
        predatorCount = predators;
        totalPop = plants + grazers + predators;
    }

    /// <summary>Required for JsonUtility.FromJson in SummaryGenerator.</summary>
    public PopSnapshot() { }
}
