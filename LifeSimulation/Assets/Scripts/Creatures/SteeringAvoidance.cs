using UnityEngine;

/// <summary>
/// Raycasting-based obstacle avoidance for 2D agents with box colliders.
/// Uses BoxCast for accurate rectangle edge detection.
/// If all rays are blocked (dead-end), the agent reverses and picks a new target.
///
/// Add to Grazer and Predator prefabs.
/// Tag obstacle GameObjects "Obstacle".
/// </summary>
public class SteeringAvoidance : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float agentHalfWidth = 0.2f;   // half the agent's sprite width
    [SerializeField] private float agentHalfHeight = 0.2f;   // half the agent's sprite height
    [SerializeField] private int rayCount = 7;      // must be odd for a centre ray
    [SerializeField] private float fanAngle = 120f;

    [Header("Steering")]
    [SerializeField] private float avoidanceStrength = 8f;
    [SerializeField] private float deadEndThreshold = 0.3f;   // fraction of lookAheadDistance below which a ray counts as blocked
    [SerializeField] private LayerMask obstacleLayer = ~0;

    private static readonly string[] AvoidTags = { "Obstacle", "Plant" };
    private static readonly string[] AvoidTagsNoPlant = { "Obstacle" };
    private static readonly string[] AvoidTagsWithCreatures = { "Obstacle", "Plant", "Grazer", "Predator" };
    private static readonly string[] AvoidTagsWithCreaturesNoPlant = { "Obstacle", "Grazer", "Predator" };

    // Set to true when the agent is actively seeking a plant so it doesn't
    // steer away from the very plant it's trying to reach
    public bool IgnorePlants { get; set; } = false;

    // Set to true during wander/patrol so creatures don't pile up on each other
    public bool AvoidCreatures { get; set; } = false;

    // Exposed so Grazer/Predator can react to a dead-end (e.g. pick a new wander target)
    public bool IsDeadEnd { get; private set; }

    /// <summary>
    /// Call every frame from the agent. Pass desired velocity, get back a
    /// steered velocity that avoids obstacles. IsDeadEnd is set to true this
    /// frame if the agent should reverse and pick a new destination.
    /// </summary>
    public Vector2 GetAvoidanceVelocity(Vector2 desiredVelocity)
    {
        IsDeadEnd = false;

        if (desiredVelocity.sqrMagnitude < 0.001f) return desiredVelocity;

        float speed = desiredVelocity.magnitude;
        Vector2 forward = desiredVelocity.normalized;

        float halfFan = fanAngle * 0.5f;
        float angleStep = rayCount > 1 ? fanAngle / (rayCount - 1) : 0f;

        Vector2 bestDirection = forward;
        float bestClearance = -1f;
        int blockedCount = 0;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFan + angleStep * i;
            Vector2 rayDir = Rotate(forward, angle);
            float clearance = CastBox(rayDir);

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                bestDirection = rayDir;
            }

            if (clearance < lookAheadDistance * deadEndThreshold)
                blockedCount++;
        }

        // Dead-end: every ray is heavily blocked — reverse and signal caller
        if (blockedCount == rayCount)
        {
            IsDeadEnd = true;
            return -forward * speed;
        }

        // Forward ray is clear — no steering needed
        float forwardClearance = CastBox(forward);
        if (forwardClearance >= lookAheadDistance) return desiredVelocity;

        // Blend toward the clearest direction proportional to how close the obstacle is
        float t = 1f - (forwardClearance / lookAheadDistance);   // 0=far, 1=very close
        float blend = t * avoidanceStrength * Time.deltaTime * 10f;
        Vector2 steered = Vector2.Lerp(forward, bestDirection, Mathf.Clamp01(blend)).normalized;
        return steered * speed;
    }

    /// <summary>
    /// Casts a box in the given direction. Returns distance to nearest obstacle,
    /// or lookAheadDistance if nothing is hit.
    /// </summary>
    private float CastBox(Vector2 direction)
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            origin: transform.position,
            size: new Vector2(agentHalfWidth * 2f, agentHalfHeight * 2f),
            angle: 0f,
            direction: direction,
            distance: lookAheadDistance,
            layerMask: obstacleLayer);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;

            // Pick active tag set based on current flags
            string[] activeTags;
            if (AvoidCreatures && !IgnorePlants) activeTags = AvoidTagsWithCreatures;
            else if (AvoidCreatures && IgnorePlants) activeTags = AvoidTagsWithCreaturesNoPlant;
            else if (!AvoidCreatures && IgnorePlants) activeTags = AvoidTagsNoPlant;
            else activeTags = AvoidTags;

            bool shouldAvoid = false;
            foreach (string tag in activeTags)
                if (hit.collider.CompareTag(tag)) { shouldAvoid = true; break; }
            if (!shouldAvoid) continue;

            return hit.distance;
        }

        return lookAheadDistance;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null || rb.linearVelocity.sqrMagnitude < 0.001f) return;

        Vector2 forward = rb.linearVelocity.normalized;
        float halfFan = fanAngle * 0.5f;
        float angleStep = rayCount > 1 ? fanAngle / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfFan + angleStep * i;
            Vector2 rayDir = Rotate(forward, angle);
            float clearance = CastBox(rayDir);

            // Green = clear, red = blocked
            Gizmos.color = clearance >= lookAheadDistance ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, rayDir * clearance);
        }

        if (IsDeadEnd)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
