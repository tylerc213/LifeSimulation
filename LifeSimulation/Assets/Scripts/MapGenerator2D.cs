// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Simulation GUI
// Requirement: Sim Editor
// Author:      Robert Amborski
// Date:        3/25/2026
// Version:     0.0.1
//
// Description:
//    Generates a square tilemap using a provided seed and specified X/Y dimensions.
//    After map generation, spawns initial obstacles, plants, grazers, and predators.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary> Handles procedural tile placement and initial entity spawning </summary>
public class MapGenerator2D : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap squareTilemap;
    public TileBase baseSquareTile;

    [Header("Obstacle Generation")]
    public GameObject obstaclePrefab;
    [Range(0f, 1f)]
    public float obstacleSpawnChance = 0.08f;   // probability per tile of spawning an obstacle
    public int obstacleMinCluster = 1;        // min obstacles per cluster
    public int obstacleMaxCluster = 3;        // max obstacles per cluster

    [Header("Initial Population")]
    public int startPlants = 15;
    public int startGrazers = 8;
    public int startPredators = 2;

    // Stores world-space centers of all non-obstacle tiles so we can pick
    // random valid spawn positions without landing inside an obstacle.
    private List<Vector3> _openTiles = new List<Vector3>();

    /// <summary> True only after <see cref="GenerateMap"/> has finished (tiles + obstacles). Used to block editor spawns before the first generate.</summary>
    public bool IsMapReady { get; private set; }
    /// <summary> True once the user has generated at least one map in this scene session.</summary>
    public bool HasSimulationStarted { get; private set; }

    /// <summary> Executes generation of selected tier of sim map </summary>
    /// <remarks> Uses SetTilesBlock for performance on large maps </remarks>
    public void GenerateMap()
    {
        IsMapReady = false;
        HasSimulationStarted = false;

        // Determine dimensions from central spectator settings
        int sizeDim = 500;
        var camHandler = FindFirstObjectByType<CameraHandler>();
        if (camHandler != null)
        {
            switch (camHandler.selectedSize)
            {
                case CameraHandler.MapSize.Small: sizeDim = 150; break;
                case CameraHandler.MapSize.Medium: sizeDim = 300; break;
                case CameraHandler.MapSize.Large: sizeDim = 500; break;
            }
        }

        // Wipe existing data to prepare for new generation
        squareTilemap.ClearAllTiles();
        _openTiles.Clear();

        // Destroy any entities left over from a previous generation
        DestroyTagged("Plant");
        DestroyTagged("Grazer");
        DestroyTagged("Predator");
        DestroyTagged("Obstacle");

        // Initialize unique seed for sim run
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
        float offsetX = UnityEngine.Random.Range(0f, 99999f);
        float offsetY = UnityEngine.Random.Range(0f, 99999f);

        // calculate offset for centering
        int halfDim = sizeDim / 2;
        Vector3Int startPos = new Vector3Int(-halfDim, -halfDim, 0);
        Vector3Int size = new Vector3Int(sizeDim, sizeDim, 1);

        // Batch set tiles to avoid CPU overhead
        BoundsInt bounds = new BoundsInt(startPos, size);
        TileBase[] tileArray = new TileBase[sizeDim * sizeDim];

        for (int i = 0; i < tileArray.Length; i++) tileArray[i] = baseSquareTile;
        squareTilemap.SetTilesBlock(bounds, tileArray);

        // Apply visual variety and track valid spawns
        for (int x = -halfDim; x < halfDim; x++)
        {
            for (int y = -halfDim; y < halfDim; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                // Determine tile color via noise
                float perlin = Mathf.PerlinNoise((x + halfDim) * 0.1f + offsetX, (y + halfDim) * 0.1f + offsetY);
                Color tileColor = (perlin > 0.67f) ? new Color(0.72f, 0.61f, 0.45f) : Color.tan;

                // Unlock tile flags to allow coloring
                squareTilemap.SetTileFlags(tilePos, TileFlags.None);
                squareTilemap.SetColor(tilePos, tileColor);

                // Add cell center to walkable list
                _openTiles.Add(new Vector3(x + 0.5f, y + 0.5f, 0));
            }
        }

        // ── Obstacle generation ───────────────────────────────────────────────
        if (obstaclePrefab != null)
            SpawnObstacles();

        IsMapReady = true;
        HasSimulationStarted = true;

        // ── Initial entity spawning ───────────────────────────────────────────
        // Use Invoke so EcosystemManager and BoundaryManager have had a frame
        // to initialise before we ask them to spawn anything.
        Invoke(nameof(SpawnInitialEntities), 0.1f);
    }

    /// <summary> Random walkable tile center (excludes obstacle cells). Use for UI button spawns.</summary>
    public bool TryGetRandomOpenTileWorldPosition(out Vector3 worldCenter)
    {
        worldCenter = default;
        if (!IsMapReady || _openTiles.Count == 0)
        {
            return false;
        }

        worldCenter = _openTiles[UnityEngine.Random.Range(0, _openTiles.Count)];
        return true;
    }

    // ── Obstacle Spawning ─────────────────────────────────────────────────────

    private void SpawnObstacles()
    {
        // Work on a shuffled copy so clusters land in random positions
        List<Vector3> shuffled = new List<Vector3>(_openTiles);
        Shuffle(shuffled);

        // Keep a set of occupied positions so clusters don't double-place
        HashSet<Vector3> occupied = new HashSet<Vector3>();

        foreach (Vector3 tileCenter in shuffled)
        {
            if (occupied.Contains(tileCenter)) continue;
            if (UnityEngine.Random.value > obstacleSpawnChance) continue;

            int clusterSize = UnityEngine.Random.Range(obstacleMinCluster, obstacleMaxCluster + 1);
            PlaceObstacleCluster(tileCenter, clusterSize, occupied);
        }

        // Remove obstacle positions from _openTiles so entities don't spawn there
        _openTiles.RemoveAll(t => occupied.Contains(t));
    }

    private void PlaceObstacleCluster(Vector3 origin, int count, HashSet<Vector3> occupied)
    {
        // Cardinal neighbour offsets in tile space
        Vector3Int[] dirs = {
            Vector3Int.zero,
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up,    Vector3Int.down
        };

        Vector3Int originCell = squareTilemap.WorldToCell(origin);
        int placed = 0;

        foreach (Vector3Int dir in dirs)
        {
            if (placed >= count) break;

            Vector3Int cell = originCell + dir;
            Vector3 worldPos = squareTilemap.GetCellCenterWorld(cell);

            if (!squareTilemap.HasTile(cell)) continue;
            if (occupied.Contains(worldPos)) continue;

            Instantiate(obstaclePrefab, worldPos, Quaternion.identity);
            occupied.Add(worldPos);
            placed++;
        }
    }

    // ── Initial Entity Spawning ───────────────────────────────────────────────

    private void SpawnInitialEntities()
    {
        if (EcosystemManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning("MapGenerator2D: EcosystemManager not found — skipping entity spawn.");
            return;
        }

        for (int i = 0; i < startPlants; i++) EcosystemManager.Instance.ManualSpawnPlant(RandomOpenTile());
        for (int i = 0; i < startGrazers; i++) EcosystemManager.Instance.ManualSpawnGrazer(RandomOpenTile());
        for (int i = 0; i < startPredators; i++) EcosystemManager.Instance.ManualSpawnPredator(RandomOpenTile());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 RandomOpenTile()
    {
        if (_openTiles.Count > 0)
            return _openTiles[UnityEngine.Random.Range(0, _openTiles.Count)];

        // Fallback: let BoundaryManager pick a position if tile list is empty
        return BoundaryManager.Instance != null
            ? BoundaryManager.Instance.RandomPosition()
            : Vector2.zero;
    }

    private static void DestroyTagged(string tag)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
            Destroy(go);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}