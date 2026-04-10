using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    public float visionRange = 5f;
    public LayerMask detectionLayer;

    public Transform visibleTarget;

    public bool IgnoreNightPenalty = false;

    void Update()
    {
        DetectTargets();
    }

    void DetectTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange, detectionLayer);

        visibleTarget = null;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            if (HasLineOfSight(hit.transform))
            {
                visibleTarget = hit.transform;
                break;
            }
        }
    }

    bool HasLineOfSight(Transform target)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            (target.position - transform.position).normalized,
            visionRange
        );

        if (hit.collider == null) return false;

        return hit.transform == target;
    }
}
