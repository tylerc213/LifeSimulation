using UnityEngine;
using System.Collections.Generic;

public class Pack : MonoBehaviour
{
    public List<Creature> Members = new List<Creature>();
    public bool IsGrazerPack;

    public void UpdatePackBehavior()
    {
        if (IsGrazerPack)
        {
            // Herd mentalities: stick together
            foreach (var member in Members)
            {
                if (member == null) continue;
                Vector2 avgPosition = Vector2.zero;
                int count = 0;
                foreach (var m in Members)
                {
                    if (m != null)
                    {
                        avgPosition += (Vector2)m.transform.position;
                        count++;
                    }
                }
                avgPosition /= Mathf.Max(1, count);
                member.transform.position = Vector2.Lerp(member.transform.position, avgPosition, 0.02f);
            }
        }
        else
        {
            // Predator packs: coordinate attacks
            foreach (var member in Members)
            {
                if (member is Predator predator)
                {
                    Creature target = FindClosestGrazer(predator);
                    if (target != null)
                        predator.Attack(target);
                }
            }
        }
    }

    private Creature FindClosestGrazer(Predator predator)
    {
        float minDist = float.MaxValue;
        Creature closest = null;
        foreach (var member in Members)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(predator.transform.position, 10f);
            foreach (var hit in hits)
            {
                Grazer g = hit.GetComponent<Grazer>();
                if (g != null)
                {
                    float dist = Vector2.Distance(predator.transform.position, g.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = g;
                    }
                }
            }
        }
        return closest;
    }
}