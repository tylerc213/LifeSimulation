// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Map Coordinates
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Determines map coordinate system
// -----------------------------------------------------------------------------

using UnityEngine;

[System.Serializable]
public struct MapCoordinates
{
    public int x;
    public int y;
    public MapCoordinates(int x, int y)
    {
        this.x = x; this.y = y;
    }
}
