// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Simulation GUI
// Requirement: Sim Editor
// Author:      Robert Amborski
// Date:        3/25/2026
// Version:     0.0.1
//
// Description:
//    Generates a square tilemap set map sizes and random placement of obstacles.
//    After map generation, spawns initial obstacles, plants, grazers, and predators.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Obstacle Generation")]
    public GameObject obstaclePrefab;
    [Tooltip("Probability per tile of attempting a rock cluster. Keep low on large maps.")]
    [Range(0.0005f, 0.015f)]
    public float rockSpawnChance = 0.004f;
    public int obstacleMinCluster = 1;
    public int obstacleMaxCluster = 2;

    [Header("Terrain coloring (Perlin — water vs land)")]
    [Tooltip("Feature size for Perlin noise.")]
    public float perlinScale = 0.1f;
    [Tooltip("0 = least water, 1 = most water (blue tiles).")]
    [Range(0f, 1f)]
    public float waterSpawnRate = 0.5f;

    [Header("Initial Population")]
    public int startPlants = 15;
    public int startGrazers = 8;
    public int startPredators = 2;

    private float _currentOffsetX;
    private float _currentOffsetY;

    // Stores world-space centers of all non-obstacle tiles so we can pick
    // random valid spawn positions without landing inside an obstacle.
    private List<Vector3> _openTiles = new List<Vector3>();

    /// <summary> True only after <see cref="GenerateMap"/> has finished (tiles + obstacles). Used to block editor spawns before the first generate.</summary>
    public bool IsMapReady { get; private set; }
    /// <summary> True once the user has generated at least one map in this scene session.</summary>
    public bool HasSimulationStarted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MapGenerator2D: Multiple instances — destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary> Executes map generation logic then spawns initial entities </summary>
    public void GenerateMap()
    {
        IsMapReady = false;
        HasSimulationStarted = false;

        SimulationManager sim = SimulationManager.Instance;
        sim?.ResetPopulationState();
        sim?.SetSuppressPopulationSync(true);

        // Determine dimensions from central spectator settings
        int sizeDim = 300;
        var camHandler = FindFirstObjectByType<CameraHandler>();
        if (camHandler != null)
        {
            switch (camHandler.selectedSize)
            {
                case CameraHandler.MapSize.Small: sizeDim = 50; break;
                case CameraHandler.MapSize.Medium: sizeDim = 100; break;
                case CameraHandler.MapSize.Large: sizeDim = 300; break;
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

        sim?.SetSuppressPopulationSync(false);

        // Initialize unique seed for sim run
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
        _currentOffsetX = UnityEngine.Random.Range(0f, 99999f);
        _currentOffsetY = UnityEngine.Random.Range(0f, 99999f);

        // calculate offset for centering
        int halfDim = sizeDim / 2;
        Vector3Int startPos = new Vector3Int(-halfDim, -halfDim, 0);
        Vector3Int size = new Vector3Int(sizeDim, sizeDim, 1);

        // Batch set tiles to avoid CPU overhead
        BoundsInt bounds = new BoundsInt(startPos, size);
        TileBase[] tileArray = new TileBase[sizeDim * sizeDim];

        for (int i = 0; i < tileArray.Length; i++) tileArray[i] = baseSquareTile;
        squareTilemap.SetTilesBlock(bounds, tileArray);

        // ── Tile placement ────────────────────────────────────────────────────
        // Fetch current seasonal palette from EnvironmentHandler
        var palette = EnvironmentHandler.Instance != null
            ? EnvironmentHandler.Instance.GetCurrentPalette()
            : new SeasonalPalette(Color.tan, new Color(0.1f, 0.2f, 0.5f));

        for (int x = -halfDim; x < halfDim; x++)
        {
            for (int y = -halfDim; y < halfDim; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                squareTilemap.SetTile(tilePos, baseSquareTile);

                float perlin = Mathf.PerlinNoise((x + halfDim) * perlinScale + _currentOffsetX, (y + halfDim) * perlinScale + _currentOffsetY);

                // Use the Palette colors based on perlin noise (water vs land)
                Color tileColor = (perlin > 0.65f) ? palette.waterColor : palette.landColor;

                squareTilemap.SetTileFlags(tilePos, TileFlags.None);
                squareTilemap.SetColor(tilePos, tileColor);

                Vector3 worldCenter = squareTilemap.GetCellCenterWorld(tilePos);
                _openTiles.Add(worldCenter);
            }
        }

        // ── Notify BoundaryManager of the true map extents ────────────────────
        // This must happen before obstacle/entity spawning so RandomPosition()
        // and Clamp() use the full map area, not just the camera viewport.
        if (BoundaryManager.Instance != null)
            BoundaryManager.Instance.SetMapBounds(squareTilemap);

        if (camHandler != null)
            camHandler.UpdateTiers();

        // ── Obstacle generation ───────────────────────────────────────────────
        if (obstaclePrefab != null)
            SpawnObstacles();

        IsMapReady = true;
        HasSimulationStarted = true;

        // This notifies SimulationLogger and LogManager to start their work.
        OnMapGenerated?.Invoke();

        // ── Initial entity spawning ───────────────────────────────────────────
        // Use Invoke so EcosystemManager and BoundaryManager have had a frame
        // to initialise before we ask them to spawn anything.
        Invoke(nameof(SpawnInitialEntities), 0.1f);
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

    /// <summary> Updates the colors of existing tiles to match the current season </summary>
    public void RefreshTileColors()
    {
        if (squareTilemap == null) return;

        var palette = EnvironmentHandler.Instance != null
            ? EnvironmentHandler.Instance.GetCurrentPalette()
            : new SeasonalPalette(Color.tan, new Color(0.1f, 0.2f, 0.5f));

        // We need to fetch the original offsets used during GenerateMap
        // Since they aren't stored as variables, we have to ensure they are accessible.
        // For now, let's assume you're using the same perlinScale.

        int halfDim = squareTilemap.cellBounds.size.x / 2;

        foreach (var pos in squareTilemap.cellBounds.allPositionsWithin)
        {
            if (squareTilemap.HasTile(pos))
            {
                // IMPORTANT: We use the same math from GenerateMap to determine tile type
                // Note: If you want this to be 100% perfect, you should store the 
                // offsetX and offsetY as class variables in MapGenerator2D.
                float perlin = Mathf.PerlinNoise((pos.x + halfDim) * perlinScale + _currentOffsetX, (pos.y + halfDim) * perlinScale + _currentOffsetY);

                bool isWater = perlin > 0.65f;

                Color newColor = isWater ? palette.waterColor : palette.landColor;

                squareTilemap.SetTileFlags(pos, TileFlags.None);
                squareTilemap.SetColor(pos, newColor);
            }
        }
    }
}

