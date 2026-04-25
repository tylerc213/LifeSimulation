using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VisionCone : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private int segments = 16;

    [Header("Smoothing")]
    [SerializeField] private float facingSmoothing = 8f;    // higher = snappier rotation
    [SerializeField] private float colorSmoothing = 6f;    // higher = snappier color change
    [SerializeField] private float rebuildThreshold = 0.01f; // min facing change before rebuild

    [Header("Colors")]
    [SerializeField] private Color wanderColor = new Color(0.5f, 1f, 0.5f, 0.12f);
    [SerializeField] private Color fleeColor = new Color(1f, 0.3f, 0.3f, 0.18f);
    [SerializeField] private Color seekColor = new Color(0.4f, 0.7f, 1f, 0.15f);
    [SerializeField] private Color huntColor = new Color(1f, 0.5f, 0.1f, 0.18f);
    [SerializeField] private Color patrolColor = new Color(1f, 0.9f, 0.3f, 0.12f);

    private Rigidbody2D _rb;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Material _mat;

    private Vector2 _smoothFacing = Vector2.up;
    private Vector2 _builtFacing = Vector2.up;
    private Color _currentColor;
    private Color _targetColor;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

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

        _mat = new Material(Shader.Find("Sprites/Default"));
        _currentColor = wanderColor;
        _targetColor = wanderColor;
        _mat.color = wanderColor;
        _meshRenderer.material = _mat;
        _meshRenderer.sortingLayerName = "Simulation";
        _meshRenderer.sortingOrder = 1;

        _mesh = new Mesh { name = "VisionConeMesh" };
        _meshFilter.mesh = _mesh;

        BuildMesh(_smoothFacing);
    }

    public void UpdateCone(Vector2 rawFacing, string state)
    {
        // Smooth the facing direction so it doesn't snap every frame
        if (rawFacing.sqrMagnitude > 0.01f)
            _smoothFacing = Vector2.Lerp(_smoothFacing, rawFacing.normalized,
                                         facingSmoothing * Time.deltaTime).normalized;

        // Only rebuild mesh when facing has changed enough — prevents per-frame flicker
        if (Vector2.Distance(_smoothFacing, _builtFacing) > rebuildThreshold)
        {
            BuildMesh(_smoothFacing);
            _builtFacing = _smoothFacing;
        }

        // Smoothly interpolate color instead of snapping
        _targetColor = ColorForState(state);
        _currentColor = Color.Lerp(_currentColor, _targetColor, colorSmoothing * Time.deltaTime);
        _mat.color = _currentColor;
    }

    private void BuildMesh(Vector2 facing)
    {
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        float baseAngle = Mathf.Atan2(facing.y, facing.x);

        int vertCount = segments + 2;
        var verts = new Vector3[vertCount];
        var tris = new int[segments * 3];

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

    private Color ColorForState(string state) => state switch
    {
        StateLabel.Flee => fleeColor,
        StateLabel.Seek => seekColor,
        StateLabel.Eat => seekColor,
        StateLabel.Hunt => huntColor,
        StateLabel.Patrol => patrolColor,
        _ => wanderColor,
    };
}