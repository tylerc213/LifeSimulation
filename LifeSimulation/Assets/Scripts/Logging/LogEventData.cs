// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation Analysis
// Requirement:	Event Log
// Author:		Caden Nieves
// Date:		03/24/2026
// Version:		0.1.0
//
// Description:
//    Represents a data structure containing a snapshot of population events
//    at a specific simulation tick and includes an identifying message. To be
//    called by the LogManager.cs with specfic population fluctuation.
// -----------------------------------------------------------------------------

using System;

/// <summary>
/// Stores event data for single event in simulation. Used by logging
/// system to record events like extinction.
/// </summary>
[Serializable]
public class LogEventData
{
    public string eventType;
    public int tick;
    public string message;

    /// <summary>
    /// Creates a "snapshot" of when an event occured and what the
    /// event was.
    /// </summary>
    /// <param name="eventType">Occured event - i.e. extinction, overpopulation</param>
    /// <param name="tick">current frame when event triggered</param>
    /// <param name="message">Event message</param>
    public LogEventData(string eventType, int tick, string message)
    {
        this.eventType = eventType;
        this.tick = tick;
        this.message = message;
    }
}
