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
/// Wrapper used to structure logs.
/// </summary>
[Serializable]
public class LogEntry
{
    public string entryType;
    public int tick;
    public string data;

    public LogEntry(string entryType, int tick, string data)
    {
        this.entryType = entryType;
        this.tick = tick;
        this.data = data;
    }
}
