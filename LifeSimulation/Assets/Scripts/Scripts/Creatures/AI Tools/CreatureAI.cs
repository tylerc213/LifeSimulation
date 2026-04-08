using UnityEngine;
using System.Collections.Generic;
using System;

public enum LifeState { Searching, Eating, Fleeing, Resting }

public class CreatureAI : MonoBehaviour
{
    public LifeState CurrentState = LifeState.Searching;
    public float DetectionRadius = 10f;

    private Creature creature;
    private Transform target;

    private void Awake()
    {
        creature = GetComponent<Creature>();
    }

    private void Update()
    {
        UpdateState();
        Act();
    }

    private void UpdateState()
    {
        switch (CurrentState)
        {
            case LifeState.Searching:
                // Look for food or threat
                target = DetectFoodOrThreat();
                if (target != null)
                {
                    if (target.GetComponent<Plant>() != null)
                        CurrentState = LifeState.Eating;
                    else if (target.GetComponent<Predator>() != null)
                        CurrentState = LifeState.Fleeing;
                }
                break;

            case LifeState.Eating:
                if (target == null || target.GetComponent<Plant>() == null)
                    CurrentState = LifeState.Searching;
                break;

            case LifeState.Fleeing:
                if (target == null)
                    CurrentState = LifeState.Searching;
                break;

            case LifeState.Resting:
                if (creature.CurrentHealth >= creature.MaxHealth)
                    CurrentState = LifeState.Searching;
                break;
        }
    }

    private void Act()
    {
        switch (CurrentState)
        {
            case LifeState.Searching:
                Wander();
                break;
            case LifeState.Eating:
                Grazer grazer = creature as Grazer;
                if (grazer != null && target != null)
                {
                    grazer.EatPlant(target.GetComponent<Plant>());
                    CurrentState = LifeState.Searching;
                }
                break;
            case LifeState.Fleeing:
                FleeFrom(target);
                break;
            case LifeState.Resting:
                Recover();
                break;
        }
    }

    private Transform DetectFoodOrThreat()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DetectionRadius);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            // Detect plants for grazers
            if (creature is Grazer && hit.GetComponent<Plant>() != null)
                return hit.transform;

            // Detect predators for grazers
            if (creature is Grazer && hit.GetComponent<Predator>() != null)
                return hit.transform;

            // Detect grazers for predators
            if (creature is Predator && hit.GetComponent<Grazer>() != null)
                return hit.transform;
        }
        return null;
    }

    private void Wander()
    {
        // Simple wandering: move in random direction
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        transform.Translate(dir * Time.deltaTime * GetSpeed());
    }

    private void FleeFrom(Transform threat)
    {
        if (threat == null) return;
        Vector2 dir = (transform.position - threat.position).normalized;
        transform.Translate(dir * Time.deltaTime * GetSpeed() * 1.2f);
    }

    private void Recover()
    {
        creature.CurrentHealth += 5f * Time.deltaTime;
        if (creature.CurrentHealth > creature.MaxHealth)
            creature.CurrentHealth = creature.MaxHealth;
    }

    private float GetSpeed()
    {
        float baseSpeed = 1f;
        foreach (var trait in creature.ActiveTraits)
        {
            if (trait is NimbleTrait)
                baseSpeed *= 1.2f;
        }
        return baseSpeed;
    }
}