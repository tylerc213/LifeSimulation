// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Simulation GUI
// Requirement:	Sim Editor
// Author:		Robert Amborski
// Date:		3/25/2026
// Version:		0.0.0
//
// Description:
//    Generates a square tilemap using a provided seed and specified X/Y Dimensions
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary> Handles procedural tile placement on a square grid </summary>
public class MapGenerator2D : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap squareTilemap;
    public TileBase baseSquareTile;

    /// <summary> Executes map generation logic </summary>
    /// <param name="seed"> String used to seed the RNG </param>
    /// <param name="width"> Width of map in tiles </param>
    /// <param name="height"> Height of map in tiles </param>
    public void GenerateMap(string seed, int width, int height)
    {
        // Wipe existing data to prepare for new generation
        squareTilemap.ClearAllTiles();

        // Convert string hash to initialize random state
        Random.InitState(seed.GetHashCode());

        // Generate tiles along by height then width
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Set starting position to gird origin
                MapCoordinates customPos = new MapCoordinates(x, y);

                // Place tile asset onto the grid
                Vector3Int tilePos = new Vector3Int(customPos.x, customPos.y, 0);

                squareTilemap.SetTile(tilePos, baseSquareTile);

                float perlin = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);

                Color tileColor = (perlin > 0.67f) ? new Color(0.1f, 0.2f, 0.5f) : Color.tan;

                // Set flags to allow script-based color overriding
                squareTilemap.SetTileFlags(tilePos, TileFlags.None);
                squareTilemap.SetColor(tilePos, tileColor);
            }
        }
    }
}

