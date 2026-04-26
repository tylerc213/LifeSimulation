// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        [FROM GITHUB KANBAN BOARD]
// Requirement: [FROM GITHUB KANBAN BOARD]
// Author:      [Name]
// Date:        [MM/DD/YYYY]
// Version:     [#.#.#]
//
// Description:
//    Raycasting-based obstacle avoidance for 2D agents. Uses BoxCast fan rays
//    to detect tagged obstacles and steers agents around them. Supports
//    configurable tag sets and dead-end detection with reversal.
// -----------------------------------------------------------------------------
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

/// <summary>Steers agents around tagged obstacles using a BoxCast fan.</summary>
/// <remarks>
/// Attach to any agent alongside a Rigidbody2D. Call GetAvoidanceVelocity each
/// frame from the agent's movement code and apply the returned velocity.
/// IgnorePlants and AvoidCreatures flags adjust which tags are checked per state.
/// </remarks>
public class SteeringAvoidance : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float agentHalfWidth = 0.2f;
    [SerializeField] private float agentHalfHeight = 0.2f;
    [SerializeField] private int rayCount = 7;
    [SerializeField] private float fanAngle = 120f;

    [Header("Steering")]
    [SerializeField] private float avoidanceStrength = 8f;
    [SerializeField] private float deadEndThreshold = 0.3f;
    [SerializeField] private LayerMask obstacleLayer = ~0;

    private static readonly string[] AvoidTags = { "Obstacle", "Plant" };
    private static readonly string[] AvoidTagsNoPlant = { "Obstacle" };
    private static readonly string[] AvoidTagsWithCreatures = { "Obstacle", "Plant", "Grazer", "Predator" };
    private static readonly string[] AvoidTagsWithCreaturesNoPlant = { "Obstacle", "Grazer", "Predator" };

    /// <summary>When true, plants are excluded from avoidance checks.</summary>
    public bool IgnorePlants { get; set; } = false;

    /// <summary>When true, other creatures are included in avoidance checks.</summary>
    public bool AvoidCreatures { get; set; } = false;

    /// <summary>
    /// Overrides the fan angle for this frame when set above zero.
    /// Reset to -1 each frame by the caller after GetAvoidanceVelocity.
    /// </summary>
    public float FanAngleOverride { get; set; } = -1f;

    /// <summary>True this frame if all rays are blocked and the agent should reverse.</summary>
    public bool IsDeadEnd { get; private set; }

    /// <summary>Returns a steered velocity that avoids tagged obstacles.</summary>
    /// <param name="desiredVelocity">The agent's intended movement velocity.</param>
    /// <returns>Adjusted velocity steering around obstacles.</returns>
    public Vector2 GetAvoidanceVelocity(Vector2 desiredVelocity)
    {
        IsDeadEnd = false;

        if (desiredVelocity.sqrMagnitude < 0.001f) return desiredVelocity;

        float speed = desiredVelocity.magnitude;
        Vector2 forward = desiredVelocity.normalized;
        float activeFan = FanAngleOverride > 0f ? FanAngleOverride : fanAngle;
        float halfFan = activeFan * 0.5f;
        float angleStep = rayCount > 1 ? activeFan / (rayCount - 1) : 0f;

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

        // All rays blocked — signal caller to reverse direction
        if (blockedCount == rayCount)
        {
            IsDeadEnd = true;
            return -forward * speed;
        }

        // Forward path clear — no steering needed
        float forwardClearance = CastBox(forward);
        if (forwardClearance >= lookAheadDistance) return desiredVelocity;

        // Blend toward clearest ray proportional to obstacle proximity
        float t = 1f - (forwardClearance / lookAheadDistance);
        float blend = t * avoidanceStrength * Time.deltaTime * 10f;
        Vector2 steered = Vector2.Lerp(forward, bestDirection, Mathf.Clamp01(blend));

        // Guard against degenerate normalisation producing NaN
        if (steered.sqrMagnitude < 0.001f) return desiredVelocity;
        return steered.normalized * speed;
    }

    /// <summary>Casts a box in a direction and returns the distance to the nearest avoided object.</summary>
    /// <param name="direction">Direction to cast the box.</param>
    /// <returns>Distance to hit, or lookAheadDistance if nothing is hit.</returns>
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

            // Select active tag set based on current state flags
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

    /// <summary>Rotates a 2D vector by a given angle in degrees.</summary>
    /// <param name="v">Vector to rotate.</param>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>Rotated vector.</returns>
    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>Draws debug rays in the Scene view showing clearance per ray direction.</summary>
    private void OnDrawGizmosSelected()
    {
        if (!UnityEngine.Application.isPlaying) return;
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

            // Green ray = clear path; red ray = blocked
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
