// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		System Visualization
// Requirement:	Camera Mode
// Author:		Robert Amborski
// Date:		04/17/2026
// Version:		0.0.1
//
// Description:
//    Handles physics-based spectator movement, orthographic zooming, and 
//    dynamic boundary clamping based on selected map tiers.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary> Manages user camera </summary>
public class CameraHandler : MonoBehaviour
{
    // Define map tiers of simulation
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

    [Tooltip("Multiplies map half-extent for pan limits so the viewport edge is not glued to the terrain (1 = tight to map).")]
    [SerializeField] float panBoundsMargin = 2.5f;

    private float _halfMap;
    private float _panHalfX;
    private float _panHalfY;
    /// <summary> Pan/clamp origin; updated when the tilemap bounds are known so limits stay aligned with the map. </summary>
    private float _panOriginX;
    private float _panOriginY;

    private Rigidbody2D _rb;
    private Camera _cam;
    private Vector2 _input;

    /// <summary> Initializes components </summary>
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = GetComponentInChildren<Camera>();
        _rb.freezeRotation = true;

        ApplyPresetHalfMap();
    }

    /// <summary> Re-applies tier half-size and max zoom from <see cref="selectedSize"/> (e.g. when the preset changes in the UI). </summary>
    public void ApplyPresetHalfMap()
    {
        switch (selectedSize)
        {
            case MapSize.Small: _halfMap = 25f; break;
            case MapSize.Medium: _halfMap = 50f; break;
            case MapSize.Large: _halfMap = 150f; break;
        }

        float margin = Mathf.Max(1f, panBoundsMargin);
        _panHalfX = _halfMap * margin;
        _panHalfY = _halfMap * margin;
        _panOriginX = 0f;
        _panOriginY = 0f;
    }

    /// <summary> Call after <see cref="BoundaryManager.SetMapBounds"/> so the rig sits on the map center (pan limits use the same preset half-size as before). </summary>
    public void AlignPanOriginToMapBounds()
    {
        float margin = Mathf.Max(1f, panBoundsMargin);

        if (BoundaryManager.Instance == null || !BoundaryManager.Instance.HasMapBounds)
        {
            _panOriginX = 0f;
            _panOriginY = 0f;
            _panHalfX = _halfMap * margin;
            _panHalfY = _halfMap * margin;
            return;
        }

        BoundaryManager b = BoundaryManager.Instance;
        _panOriginX = (b.MinX + b.MaxX) * 0.5f;
        _panOriginY = (b.MinY + b.MaxY) * 0.5f;

        _panHalfX = (b.MaxX - b.MinX) * 0.5f * margin;
        _panHalfY = (b.MaxY - b.MinY) * 0.5f * margin;

        transform.position = new Vector3(_panOriginX, _panOriginY, transform.position.z);
        ApplyBoundaries();
    }

    /// <summary> Captures user input </summary>
    void Update()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");
        HandleZoom();
    }

    /// <summary> Applies physics and movement area constraints </summary>
    void FixedUpdate()
    {
        // Apply smooth physics movement
        Vector2 targetVelocity = _input.normalized * moveSpeed;
        _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        ApplyBoundaries();
    }

    /// <summary> Confines the camera frustum within the map boundaries </summary>
    private void ApplyBoundaries()
    {
        if (_cam == null) return;

        // Calculate current visible half-size
        float camHalfHeight = _cam.orthographicSize;
        float camHalfWidth = camHalfHeight * _cam.aspect;

        // How far the rig can move before the view rect hits the (loosened) pan box
        float limitX = Mathf.Max(0, _panHalfX - camHalfWidth);
        float limitY = Mathf.Max(0, _panHalfY - camHalfHeight);

        float clampedX = Mathf.Clamp(transform.position.x, _panOriginX - limitX, _panOriginX + limitX);
        float clampedY = Mathf.Clamp(transform.position.y, _panOriginY - limitY, _panOriginY + limitY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    /// <summary> Adjusts orthographic size and forces boundary re-check </summary>
    private void HandleZoom()
    {
        if (_cam == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - (scroll * zoomSpeed), minSize, maxSize);

            // Re-apply boundaries immediately so the zoom "pushes" the camera back in
            ApplyBoundaries();
        }
    }
}
