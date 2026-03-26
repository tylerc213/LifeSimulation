// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/26/2026
// Version:		0.1.0
//
// Description:
//    
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

    public InteractionData(string interactionType, string source, string target)
    {
        this.interactionType = interactionType;
        this.source = source;
        this.target = target;
    }
}
