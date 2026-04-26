// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeform Visuals
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    Renders a semi-transparent mesh cone in the creature's facing direction.
//    Facing and color are smoothed to prevent flickering. Color reflects the
//    creature's current AI state.
// -----------------------------------------------------------------------------
using UnityEngine;

/// <summary>Renders a smoothed semi-transparent vision cone on a creature.</summary>
/// <remarks>
/// Attach to the creature root alongside a Rigidbody2D. The cone child
/// GameObject and MeshRenderer are created automatically at runtime.
/// Call UpdateCone each frame from the creature's ExecuteState method.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class VisionCone : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private int segments = 16;

    [Header("Smoothing")]
    // Higher values make the cone snap to direction changes faster
    [SerializeField] private float facingSmoothing = 8f;
    // Higher values make color transitions snap faster
    [SerializeField] private float colorSmoothing = 6f;
    // Minimum facing direction change before a mesh rebuild is triggered
    [SerializeField] private float rebuildThreshold = 0.01f;

    [Header("Colors")]
    [SerializeField] private Color wanderColor = new Color(0.5f, 1f, 0.5f, 0.12f);
    [SerializeField] private Color fleeColor = new Color(1f, 0.3f, 0.3f, 0.18f);
    [SerializeField] private Color seekColor = new Color(0.4f, 0.7f, 1f, 0.15f);
    [SerializeField] private Color huntColor = new Color(1f, 0.5f, 0.1f, 0.18f);
    [SerializeField] private Color patrolColor = new Color(1f, 0.9f, 0.3f, 0.12f);
    [SerializeField] private Color stalkColor = new Color(0.5f, 0.2f, 0.7f, 0.14f);
    [SerializeField] private Color dashColor = new Color(1f, 0f, 0.5f, 0.25f);

    private Rigidbody2D _rb;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Material _mat;

    private Vector2 _smoothFacing = Vector2.up;
    private Vector2 _builtFacing = Vector2.up;
    private Color _currentColor;
    private Color _targetColor;

    /// <summary>Creates the cone child GameObject, mesh, and material at runtime.</summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Create child if not already present in the prefab hierarchy
        Transform child = transform.Find("VisionCone");
        if (child == null)
        {
            child = new GameObject("VisionCone").transform;
            child.SetParent(transform);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
        }

        _meshFilter = child.gameObject.AddComponent<MeshFilter>();
        _meshRenderer = child.gameObject.AddComponent<MeshRenderer>();

        // Use Sprites/Default for transparent rendering without extra packages
        _mat = new Material(Shader.Find("Sprites/Default"));
        _currentColor = wanderColor;
        _targetColor = wanderColor;
        _mat.color = wanderColor;
        _meshRenderer.material = _mat;
        _meshRenderer.sortingLayerName = "Default";
        // Render above tilemap but below creature sprites
        _meshRenderer.sortingOrder = 1;

        _mesh = new Mesh { name = "VisionConeMesh" };
        _meshFilter.mesh = _mesh;

        BuildMesh(_smoothFacing);
    }

    /// <summary>Updates the cone direction and color based on current velocity and state.</summary>
    /// <param name="rawFacing">Current agent velocity used to determine facing direction.</param>
    /// <param name="state">Current AI state string from StateLabel constants.</param>
    public void UpdateCone(Vector2 rawFacing, string state)
    {
        // Smooth facing to prevent per-frame direction snapping
        if (rawFacing.sqrMagnitude > 0.01f)
            _smoothFacing = Vector2.Lerp(_smoothFacing, rawFacing.normalized,
                                         facingSmoothing * Time.deltaTime).normalized;

        // Only rebuild the mesh when the direction has changed meaningfully
        if (Vector2.Distance(_smoothFacing, _builtFacing) > rebuildThreshold)
        {
            BuildMesh(_smoothFacing);
            _builtFacing = _smoothFacing;
        }

        // Lerp toward target color to smooth state transitions
        _targetColor = ColorForState(state);
        _currentColor = Color.Lerp(_currentColor, _targetColor, colorSmoothing * Time.deltaTime);
        _mat.color = _currentColor;
    }

    /// <summary>Rebuilds the cone mesh triangles for a given facing direction.</summary>
    /// <param name="facing">Normalised direction the cone should point.</param>
    private void BuildMesh(Vector2 facing)
    {
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        float baseAngle = Mathf.Atan2(facing.y, facing.x);

        int vertCount = segments + 2;
        var verts = new Vector3[vertCount];
        var tris = new int[segments * 3];

        // Cone tip is at the entity's origin
        verts[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = baseAngle - halfAngle + t * angle * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }

    /// <summary>Returns the cone color corresponding to an AI state string.</summary>
    /// <param name="state">AI state string from StateLabel constants.</param>
    /// <returns>Configured color for that state.</returns>
    private Color ColorForState(string state) => state switch
    {
        StateLabel.Flee => fleeColor,
        StateLabel.Seek => seekColor,
        StateLabel.Eat => seekColor,
        StateLabel.Hunt => huntColor,
        StateLabel.Stalk => stalkColor,
        StateLabel.Dash => dashColor,
        StateLabel.Patrol => patrolColor,
        _ => wanderColor,
    };
}
