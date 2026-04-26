// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		World Representation
// Requirement:	Map Coordinate System
// Author:		Robert Amborski
// Date:		03/25/2026
//
// Description:
//    Defines a lightweight integer-based coordinate structure used for
//    referencing positions within the simulation grid.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Represents a discrete 2D coordinate within the simulation space.
/// </summary>
/// <remarks>
/// Used for grid-based calculations such as map generation, entity placement,
/// and spatial indexing where floating-point precision is unnecessary.
/// </remarks>
[System.Serializable]
public struct MapCoordinates
{
    /// <summary> Horizontal grid position </summary>
    public int x;

    /// <summary> Vertical grid position </summary>
    public int y;

    /// <summary>
    /// Creates a new coordinate pair.
    /// </summary>
    /// <param name="x">Horizontal grid value</param>
    /// <param name="y">Vertical grid value</param>
    public MapCoordinates(int x, int y)
    {
        // Assign integer grid positions directly for deterministic placement
        this.x = x;
        this.y = y;
    }
}
