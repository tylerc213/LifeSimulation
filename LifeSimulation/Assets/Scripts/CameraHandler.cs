// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		System Visualization
// Requirement:	Camera Mode
// Author:		Robert Amborski
// Date:		04/17/2026
//
// Description:
//    Controls player camera movement, zoom behavior, and ensures the camera
//    view remains constrained within the simulation map boundaries based on
//    selected map size.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Handles camera movement, zoom, and boundary constraints.
/// </summary>
/// <remarks>
/// Uses Rigidbody2D for smooth physics-based movement and clamps the camera
/// view so the player cannot see outside the simulation area.
/// </remarks>
public class CameraHandler : MonoBehaviour
{
    /// <summary>
    /// Defines available simulation map sizes.
    /// </summary>
    public enum MapSize { Small, Medium, Large }

    [Header("Simulation Settings")]
    public MapSize selectedSize = MapSize.Small;

    [Header("Movement")]
    public float moveSpeed = 50f; // Increased for larger maps
    public float acceleration = 10f;

    [Header("Zoom")]
    public float zoomSpeed = 28f;
    public float minSize = 5f;
    public float maxSize = 120f;

    [Tooltip("Scales Input.mouseScrollDelta.y (wheel / trackpads that emit scroll as pixel delta).")]
    public float mouseScrollDeltaScale = 0.03f;

    [Tooltip("Orthographic zoom per pixel of pinch gap change (two-finger pinch, macOS trackpad).")]
    public float pinchZoomSensitivity = 0.012f;

    private float _halfMap;
    private Rigidbody2D _rb;
    private Camera _cam;
    private Vector2 _input;

    /// <summary>
    /// Initializes camera references and applies map size constraints.
    /// </summary>
    void Start()
    {
        // Cache required components for performance and reuse
        _rb = GetComponent<Rigidbody2D>();
        _cam = GetComponentInChildren<Camera>();

        // Prevent unwanted rotation from physics interactions
        _rb.freezeRotation = true;

        // Initialize map boundaries based on selected size
        UpdateTiers();
    }

    /// <summary>
    /// Updates map boundary limits and zoom constraints based on selected size.
    /// </summary>
    public void UpdateTiers()
    {
        // Set half-map size to define movement boundaries
        switch (selectedSize)
        {
            case MapSize.Small: _halfMap = 25f; break;
            case MapSize.Medium: _halfMap = 50f; break;
            case MapSize.Large: _halfMap = 150f; break;
        }

        // Prevent zooming out beyond the visible map area
        maxSize = _halfMap;
    }

    /// <summary>
    /// Captures player movement input and processes zoom input.
    /// </summary>
    void Update()
    {
        // Read raw input to avoid smoothing (gives responsive camera control)
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");
        HandleZoom();
    }

    /// <summary>
    /// Applies smooth physics-based movement and enforces boundaries.
    /// </summary>
    void FixedUpdate()
    {
        // Smoothly interpolate velocity toward target movement direction
        Vector2 targetVelocity = _input.normalized * moveSpeed;
        _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        ApplyBoundaries();
    }

    /// <summary>
    /// Restricts camera position so its visible area stays within the map.
    /// </summary>
    private void ApplyBoundaries()
    {
        if (_cam == null) return;

        // Calculate half-height and width of the visible camera area
        float camHalfHeight = _cam.orthographicSize;
        float camHalfWidth = camHalfHeight * _cam.aspect;

        // Determine max movement range so camera edges do not leave the map
        float limitX = Mathf.Max(0, _halfMap - camHalfWidth);
        float limitY = Mathf.Max(0, _halfMap - camHalfHeight);

        // Clamp position within calculated limits
        float clampedX = Mathf.Clamp(transform.position.x, -limitX, limitX);
        float clampedY = Mathf.Clamp(transform.position.y, -limitY, limitY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    /// <summary>
    /// Handles camera zoom input and enforces zoom limits.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Input.mouseScrollDelta"/> and legacy scroll axis for wheel / Windows trackpads,
    /// and two-finger pinch via <see cref="Input.touchCount"/> for macOS trackpads and touchscreens.
    /// </remarks>
    private void HandleZoom()
    {
        if (_cam == null) return;

        // Two-finger pinch: macOS trackpads report touches; scroll axis is often zero.
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            Vector2 p0Prev = t0.position - t0.deltaPosition;
            Vector2 p1Prev = t1.position - t1.deltaPosition;
            float prevMag = (p0Prev - p1Prev).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            float pinchDelta = prevMag - currMag; // > 0 when fingers move together (zoom in)
            if (Mathf.Abs(pinchDelta) > 0.25f)
                ApplyZoomChange(pinchDelta * pinchZoomSensitivity);
            return;
        }

        // Mouse wheel and trackpads that map zoom to scroll deltas.
        float scrollFromDelta = Input.mouseScrollDelta.y * mouseScrollDeltaScale;
        float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
        float scroll = Mathf.Abs(scrollFromDelta) > 1e-5f ? scrollFromDelta : scrollAxis;
        if (Mathf.Abs(scroll) > 1e-5f)
            ApplyZoomChange(scroll * zoomSpeed);
    }

    /// <summary>
    /// Positive values zoom in (decrease orthographic size), matching scroll-wheel "up" convention.
    /// </summary>
    private void ApplyZoomChange(float amount)
    {
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - amount, minSize, maxSize);
        ApplyBoundaries();
    }
}
