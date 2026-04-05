// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Generates a hexagonal tilemap using a provided seed and specified X/Y Dimensions
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary> Handles procedural tile placement on a hexagonal grid </summary>
public class MapGenerator2D : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap hexTilemap;
    public TileBase baseHexTile;

    /// <summary> Executes map generation logic </summary>
    /// <param name="seed"> String used to seed the RNG </param>
    /// <param name="width"> Width of map in tiles </param>
    /// <param name="height"> Height of map in tiles </param>
    public void GenerateMap(string seed, int width, int height)
    {
        // Wipe existing data to prepare for new generation
        hexTilemap.ClearAllTiles();

        // Convert string hash to initialize random state
        Random.InitState(seed.GetHashCode());

        // Generate tiles along by height then width
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Set starting position to gird origin
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                // Place tile asset onto the grid
                hexTilemap.SetTile(tilePos, baseHexTile);

                // Apply random color to placed tile
                //float rand = Random.value;
                //Color tileColor = (rand > 0.95f) ? Color.teal : Color.tan;

                // Apply tan color to placed tile
                Color tileColor = Color.tan;

                // Set flags to allow script-based color overriding
                hexTilemap.SetColor(tilePos, tileColor);
                hexTilemap.SetTileFlags(tilePos, TileFlags.None);
            }
        }
    }
}
