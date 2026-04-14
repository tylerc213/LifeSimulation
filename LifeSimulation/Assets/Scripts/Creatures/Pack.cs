// -----------------------------------------------------------------------------
// Pack.cs
// Manages herd/pack social behaviours for Grazers and Predators.
// GrazerPack  — herd mentality, herd leader, camouflage detection suppression
// PredatorPack — herd hunter, apex predator singleton enforcement
// Attach ONE GrazerPack and ONE PredatorPack to your Managers GameObject.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
// GRAZER PACK
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Tracks grazer groups. Grazers with HerdMentality flock together.
/// Shared line-of-sight is handled by exposing known predator positions.
/// </summary>
public class GrazerPack : MonoBehaviour
{
    public static GrazerPack Instance { get; private set; }

    [SerializeField] private float packRadius = 6f;   // how close grazers must be to form a pack
    [SerializeField] private float flockStrength = 0.4f; // how strongly herd members pull toward centroid

    // All grazers registered this frame
    private List<GrazerGenetics> _allGrazers = new List<GrazerGenetics>();

    // Shared predator sightings from any herd member (cleared each frame)
    private HashSet<Transform> _sharedPredators = new HashSet<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void LateUpdate()
    {
        _sharedPredators.Clear();
        _allGrazers.RemoveAll(g => g == null || g.GetComponent<EntityBase>().IsDead);
    }

    // ── Registration ──────────────────────────────────────────────────────

    public void Register(GrazerGenetics g)
    {
        if (!_allGrazers.Contains(g)) _allGrazers.Add(g);
    }

    public void Unregister(GrazerGenetics g) => _allGrazers.Remove(g);

    /// <summary>Herd members broadcast spotted predators to the whole pack.</summary>
    public void BroadcastPredator(Transform predator) => _sharedPredators.Add(predator);

    /// <summary>Returns all predators spotted by any herd member this frame.</summary>
    public IEnumerable<Transform> SharedPredators => _sharedPredators;

    // ── Herd Mentality ────────────────────────────────────────────────────

    /// <summary>
    /// Returns a flocking offset for a grazer with HerdMentality.
    /// Pulls the grazer toward the centroid of nearby herd members.
    /// </summary>
    public Vector2 GetFlockOffset(GrazerGenetics self)
    {
        Vector2 centroid = Vector2.zero;
        int count = 0;

        foreach (var g in _allGrazers)
        {
            if (g == self || !g.HasHerdMentality) continue;
            float dist = Vector2.Distance(self.transform.position, g.transform.position);
            if (dist > packRadius) continue;
            centroid += (Vector2)g.transform.position;
            count++;
        }

        if (count < 1) return Vector2.zero;   // no nearby herd members
        centroid /= count;
        return ((centroid - (Vector2)self.transform.position) * flockStrength);
    }

    /// <summary>
    /// Returns true if this grazer has 1+ herd companions nearby (needed to activate HerdMentality).
    /// </summary>
    public bool HasPackCompanion(GrazerGenetics self)
    {
        foreach (var g in _allGrazers)
        {
            if (g == self) continue;
            float dist = Vector2.Distance(self.transform.position, g.transform.position);
            if (dist <= packRadius) return true;
        }
        return false;
    }

    // ── Herd Leader ───────────────────────────────────────────────────────

    private static GrazerGenetics _currentLeader;

    /// <summary>
    /// Call during GrazerGenetics.ApplyTraits. Returns false if a leader already exists,
    /// preventing multiple leaders from activating.
    /// </summary>
    public static bool TryBecomeLeader(GrazerGenetics candidate)
    {
        if (_currentLeader != null && _currentLeader.gameObject != null) return false;
        _currentLeader = candidate;
        return true;
    }

    public static void UnregisterLeader(GrazerGenetics leader)
    {
        if (_currentLeader == leader) _currentLeader = null;
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// PREDATOR PACK
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Tracks predator groups. Predators with HerdHunter focus the same target.
/// Enforces the Apex Predator singleton.
/// </summary>
public class PredatorPack : MonoBehaviour
{
    public static PredatorPack Instance { get; private set; }

    [SerializeField] private float packRadius = 8f;

    private List<PredatorGenetics> _allPredators = new List<PredatorGenetics>();

    // Shared hunt target — all HerdHunters attack this
    public Transform SharedTarget { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void LateUpdate()
    {
        _allPredators.RemoveAll(p => p == null || p.GetComponent<EntityBase>().IsDead);

        // Pick the shared target: the nearest grazer to the pack centroid
        if (HasActiveHerdHunters())
            SharedTarget = FindPackTarget();
        else
            SharedTarget = null;
    }

    // ── Registration ──────────────────────────────────────────────────────

    public void Register(PredatorGenetics p)
    {
        if (!_allPredators.Contains(p)) _allPredators.Add(p);
    }

    public void Unregister(PredatorGenetics p) => _allPredators.Remove(p);

    // ── Herd Hunter ───────────────────────────────────────────────────────

    private bool HasActiveHerdHunters()
    {
        int count = 0;
        foreach (var p in _allPredators)
            if (p.HasHerdHunter) count++;
        return count >= 2;
    }

    private Transform FindPackTarget()
    {
        // Centroid of all herd hunters
        Vector2 centroid = Vector2.zero;
        int count = 0;
        foreach (var p in _allPredators)
        {
            if (!p.HasHerdHunter) continue;
            centroid += (Vector2)p.transform.position;
            count++;
        }
        if (count == 0) return null;
        centroid /= count;

        // Find nearest grazer to centroid
        Collider2D[] hits = Physics2D.OverlapCircleAll(centroid, 20f);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.CompareTag("Grazer")) continue;
            float d = Vector2.Distance(centroid, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    // ── Apex Predator singleton ────────────────────────────────────────────

    private static PredatorGenetics _apex;

    public static bool CanBecomeApex() => _apex == null || _apex.gameObject == null;
    public static void RegisterApex(PredatorGenetics p) => _apex = p;
    public static void UnregisterApex() => _apex = null;
}