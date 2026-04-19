using System.Diagnostics;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Exposes world-space bounds for agent clamping and random spawning.
///
/// Two modes:
///   Camera mode (default) — bounds follow the camera viewport each frame.
///   Map mode — bounds are set explicitly by MapGenerator2D after tile placement
///              and stay fixed until the next map generation.
///
/// MapGenerator2D calls SetMapBounds() after generating tiles. When map bounds
/// are set, they are used instead of the camera, so agents aren't confined to
/// the visible viewport on large maps.
/// </summary>
public class BoundaryManager : MonoBehaviour
{
    public static BoundaryManager Instance { get; private set; }

    [HideInInspector] public float MinX, MaxX, MinY, MaxY;

    [SerializeField] private float padding = 0.5f;

    private Camera _cam;
    private bool _usingMapBounds = false;

    /// <summary> True after <see cref="SetMapBounds"/> — agents use fixed map rect, not the camera viewport. </summary>
    public bool HasMapBounds => _usingMapBounds;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
        RefreshBounds();
    }

    private void Update()
    {
        // Only refresh from camera if no map bounds have been provided
        if (!_usingMapBounds) RefreshBounds();
    }

    /// <summary>
    /// Called by MapGenerator2D after tile placement.
    /// Converts tile grid dimensions to world-space bounds and switches out of
    /// camera mode so agents roam the full map, not just the visible viewport.
    /// </summary>
    public void SetMapBounds(Tilemap tilemap)
    {
        tilemap.CompressBounds();
        Bounds b = tilemap.localBounds;

        // Convert tilemap local bounds to world space
        Vector3 worldMin = tilemap.transform.TransformPoint(b.min);
        Vector3 worldMax = tilemap.transform.TransformPoint(b.max);

        MinX = worldMin.x + padding;
        MaxX = worldMax.x - padding;
        MinY = worldMin.y + padding;
        MaxY = worldMax.y - padding;

        _usingMapBounds = true;

        UnityEngine.Debug.Log($"BoundaryManager: map bounds set — " +
                  $"MinX={MinX:F2} MaxX={MaxX:F2} MinY={MinY:F2} MaxY={MaxY:F2}");
    }

    /// <summary>Revert to camera-based bounds (call before loading a new scene).</summary>
    public void ClearMapBounds()
    {
        _usingMapBounds = false;
        RefreshBounds();
    }

    private void RefreshBounds()
    {
        if (_cam == null) return;
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;
        Vector3 p = _cam.transform.position;
        MinX = p.x - w + padding;
        MaxX = p.x + w - padding;
        MinY = p.y - h + padding;
        MaxY = p.y + h - padding;
    }

    public Vector2 Clamp(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, MinX, MaxX),
            Mathf.Clamp(pos.y, MinY, MaxY));
    }

    public Vector2 RandomPosition()
    {
        return new Vector2(
            UnityEngine.Random.Range(MinX, MaxX),
            UnityEngine.Random.Range(MinY, MaxY));
    }
}
