using System;
using UnityEngine;

/// <summary>
/// Reads the main camera's orthographic size each frame and exposes world-space
/// bounds. Attach this to a single GameObject (e.g. "Managers").
/// All agents call BoundaryManager.Instance.Clamp() to stay inside the viewport.
/// </summary>
public class BoundaryManager : MonoBehaviour
{
    public static BoundaryManager Instance { get; private set; }

    [HideInInspector] public float MinX, MaxX, MinY, MaxY;

    // Optional padding so agents don't hug the very edge of the screen
    [SerializeField] private float padding = 0.5f;

    private Camera _cam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
        RefreshBounds();
    }

    private void Update() => RefreshBounds();   // handles window resize / camera zoom

    private void RefreshBounds()
    {
        if (_cam == null) return;
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;
        Vector3 pos = _cam.transform.position;
        MinX = pos.x - w + padding;
        MaxX = pos.x + w - padding;
        MinY = pos.y - h + padding;
        MaxY = pos.y + h - padding;
    }

    /// <summary>Returns a position clamped inside the camera boundary.</summary>
    public Vector2 Clamp(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, MinX, MaxX),
            Mathf.Clamp(pos.y, MinY, MaxY));
    }

    /// <summary>Returns a random position inside the boundary.</summary>
    public Vector2 RandomPosition()
    {
        return new Vector2(
            UnityEngine.Random.Range(MinX, MaxX),
            UnityEngine.Random.Range(MinY, MaxY));
    }
}