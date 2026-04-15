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
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary> Handles procedural tile placement and initial entity spawning </summary>
public class MapGenerator2D : MonoBehaviour
{
    public static MapGenerator2D Instance { get; private set; }

    /// <summary> True after the first successful <see cref="GenerateMap"/> in this session (tiles + obstacles ready). </summary>
    public static bool IsSimulationRunActive =>
        Instance != null && Instance.IsMapReady && Instance.HasSimulationStarted;

    /// <summary> Fired when <see cref="GenerateMap"/> completes (tiles + obstacles). Logging should start here, not on scene load. </summary>
    public static event Action OnMapGenerated;

    [Header("Tilemap References")]
    public Tilemap squareTilemap;
    public TileBase baseSquareTile;

    [Header("Obstacle Generation (rocks)")]
    public GameObject obstaclePrefab;
    [Tooltip("Probability per tile of attempting a rock cluster. Keep low on large maps.")]
    [Range(0f, 0.03f)]
    [FormerlySerializedAs("obstacleSpawnChance")]
    public float rockSpawnChance = 0.01f;
    public int obstacleMinCluster = 1;
    public int obstacleMaxCluster = 2;

    [Header("Terrain coloring (Perlin — water vs land)")]
    [Tooltip("Feature size for Perlin noise.")]
    public float perlinScale = 0.1f;
    [Tooltip("0 = least water, 1 = most water (blue tiles).")]
    [Range(0f, 1f)]
    public float waterSpawnRate = 0.5f;

    [Header("Initial Population")]
    public int startPlants = 30;
    public int startGrazers = 15;
    public int startPredators = 4;

    // Stores world-space centers of all non-obstacle tiles so we can pick
    // random valid spawn positions without landing inside an obstacle.
    private List<Vector3> _openTiles = new List<Vector3>();

    /// <summary> True only after <see cref="GenerateMap"/> has finished (tiles + obstacles). Used to block editor spawns before the first generate.</summary>
    public bool IsMapReady { get; private set; }
    /// <summary> True once the user has generated at least one map in this scene session.</summary>
    public bool HasSimulationStarted { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MapGenerator2D: multiple instances — only one expected.");
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary> Executes map generation logic then spawns initial entities </summary>
    /// <param name="seed"> String used to seed the RNG </param>
    /// <param name="width"> Width of map in tiles </param>
    /// <param name="height"> Height of map in tiles </param>
    public void GenerateMap(string seed, int width, int height)
    {
        IsMapReady = false;
        HasSimulationStarted = false;

        // Wipe existing data to prepare for new generation
        squareTilemap.ClearAllTiles();
        _openTiles.Clear();

        // Destroy any entities left over from a previous generation
        DestroyTagged("Plant");
        DestroyTagged("Grazer");
        DestroyTagged("Predator");
        DestroyTagged("Obstacle");

        // Convert string hash to initialize random state
        UnityEngine.Random.InitState(seed.GetHashCode());

        // ── Tile placement ────────────────────────────────────────────────────
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                squareTilemap.SetTile(tilePos, baseSquareTile);

                float scale = Mathf.Max(perlinScale, 0.001f);
                float perlin = Mathf.PerlinNoise(x * scale, y * scale);
                float waterThresholdEffective = Mathf.Lerp(0.9f, 0.5f, Mathf.Clamp01(waterSpawnRate));
                Color tileColor = (perlin > waterThresholdEffective)
                    ? new Color(0.1f, 0.2f, 0.5f)
                    : Color.tan;

                squareTilemap.SetTileFlags(tilePos, TileFlags.None);
                squareTilemap.SetColor(tilePos, tileColor);

                // Track the world-space center of this tile for later spawning
                _openTiles.Add(squareTilemap.GetCellCenterWorld(tilePos));
            }
        }

        // ── Obstacle generation ───────────────────────────────────────────────
        if (obstaclePrefab != null)
            SpawnObstacles();

        IsMapReady = true;
        HasSimulationStarted = true;

        OnMapGenerated?.Invoke();

        // ── Initial entity spawning ───────────────────────────────────────────
        // Use Invoke so EcosystemManager and BoundaryManager have had a frame
        // to initialise before we ask them to spawn anything.
        Invoke(nameof(SpawnInitialEntities), 0.1f);
    }

    // ── Obstacle Spawning ─────────────────────────────────────────────────────

    private void SpawnObstacles()
    {
        if (rockSpawnChance <= 0f)
            return;

        // Work on a shuffled copy so clusters land in random positions
        List<Vector3> shuffled = new List<Vector3>(_openTiles);
        Shuffle(shuffled);

        // Keep a set of occupied positions so clusters don't double-place
        HashSet<Vector3> occupied = new HashSet<Vector3>();

        foreach (Vector3 tileCenter in shuffled)
        {
            if (occupied.Contains(tileCenter)) continue;
            if (UnityEngine.Random.value > rockSpawnChance) continue;

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