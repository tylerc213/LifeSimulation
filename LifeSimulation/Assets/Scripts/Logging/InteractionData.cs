// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/26/2026
// Version:		1.1.0
//
// Description:
//    Represents a data structure containing a snapshot of Interaction events
//    at a specific simulation tick between one lifeform and another lifeform.
// -----------------------------------------------------------------------------

using System;

/// <summary>
/// Stores interaction data for single interaction in simulation. Used by logging
/// system to record interactions like fleeing and predation.
/// </summary>
[Serializable]
public class InteractionData
{
    public string interactionType;
    public string source;
    public string target;

    /// <summary>
    /// Creates a "snapshot" of when an interaction occured and what the
    /// interaction was.
    /// </summary>
    /// <param name="interactionType">type of interaction occuring(ex.predation)</param>
    /// <param name="source">what triggered the interaction</param>
    /// <param name="target">the thing/target being affected by the interaction</param>
    public InteractionData(string interactionType, string source, string target)
    {
        this.interactionType = interactionType;
        this.source = source;
        this.target = target;
    }
}
