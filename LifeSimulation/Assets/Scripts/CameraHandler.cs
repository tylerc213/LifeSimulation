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
    public float zoomSpeed = 10f;
    public float minSize = 5f;
    public float maxSize = 120f;

    private float _halfMap;
    private Rigidbody2D _rb;
    private Camera _cam;
    private Vector2 _input;

    /// <summary> Initializes components </summary>
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = GetComponentInChildren<Camera>();
        _rb.freezeRotation = true;

        // Set the boundary based on the selection
        switch (selectedSize)
        {
            case MapSize.Small: _halfMap = 75f; break;
            case MapSize.Medium: _halfMap = 150f; break;
            case MapSize.Large: _halfMap = 250f; break;
        }

        // Lock maxSize to the half-height so they can't zoom out past the map
        maxSize = _halfMap;
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

        // Determine how far the center can move before an edge hits the map limit
        // We use Mathf.Max(0, ...) to lock the camera to 0 if the view is too wide
        float limitX = Mathf.Max(0, _halfMap - camHalfWidth);
        float limitY = Mathf.Max(0, _halfMap - camHalfHeight);

        // Symmetric clamp centered at 0,0
        float clampedX = Mathf.Clamp(transform.position.x, -limitX, limitX);
        float clampedY = Mathf.Clamp(transform.position.y, -limitY, limitY);

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
