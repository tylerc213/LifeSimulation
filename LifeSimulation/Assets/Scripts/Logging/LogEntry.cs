// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/26/2026
// Version:		0.1.0
//
// Description:
//    Serves as a wrapper for all log entries, provides standardized structure
//    that includes entry type, current tick, and associated data. Ensures 
//    consistency across snapshot, event, and interaction logs.
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

    /// <summary>
    /// Creates new structured log entry that wraps simulation data with 
    /// type and timing information.
    /// </summary>
    /// <param name="entryType">Type of log entry being recorded</param>
    /// <param name="tick">Simulation tick when entry occurs</param>
    /// <param name="data">Serialized JSON string of associated data</param>
    public LogEntry(string entryType, int tick, string data)
    {
        this.entryType = entryType;
        this.tick = tick;
        this.data = data;
    }
}
