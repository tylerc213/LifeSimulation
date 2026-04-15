using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grazer AI — FLEE (predator nearby) > SEEK PLANT (hungry) > WANDER.
/// Integrates with GrazerGenetics for stat modifiers and special behaviours.
/// Tag this GameObject "Grazer".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Grazer : EntityBase
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float wanderRadius = 2f;
    [SerializeField] private float wanderInterval = 2f;

    [Header("Detection")]
    [SerializeField] private float plantDetectRadius = 5f;
    [SerializeField] private float predatorDetectRadius = 7f;

    [Header("Eating")]
    [SerializeField] private float eatDistance = 0.4f;
    [SerializeField] private float eatCooldown = 1f;

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 20f;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Wander, SeekPlant, Flee }
    private State _state = State.Wander;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private GrazerGenetics _genetics;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private float _eatTimer;
    private float _reproTimer;
    private Plant _targetPlant;
    private Transform _nearestPredator;

    // Poison tracking
    private bool _isPoisoned = false;
    private float _poisonDamageRate = 0f;

    // ── Unity ─────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _genetics = GetComponent<GrazerGenetics>();
        _reproTimer = reproductionCooldown;
        PickNewWanderTarget();
    }

    private void Start()
    {
        GrazerPack.Instance?.Register(_genetics);
    }

    protected virtual void OnDestroy()
    {
        GrazerPack.Instance?.Unregister(_genetics);
        if (_genetics != null && _genetics.IsHerdLeader)
            GrazerPack.UnregisterLeader(_genetics);
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        if (_isPoisoned)
            TakeDamage(_poisonDamageRate * Time.deltaTime);

        UpdateTimers();
        UpdateState();
        ExecuteState();
        ClampToBounds();
        TryReproduce();
    }

    // ── Timers ────────────────────────────────────────────────────────────────
    private void UpdateTimers()
    {
        _wanderTimer -= Time.deltaTime;
        _eatTimer -= Time.deltaTime;
        _reproTimer -= Time.deltaTime;
    }

    // ── Perception ────────────────────────────────────────────────────────────
    private void UpdateState()
    {
        // Camouflage: if next to any object, chance to avoid predator detection
        bool camouflaged = false;
        if (_genetics != null && _genetics.HasCamouflage)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach (var c in nearby)
            {
                if (c.gameObject == gameObject) continue;
                if (UnityEngine.Random.value < GrazerGenetics.EffectiveCamouflageChance) { camouflaged = true; break; }
            }
        }

        if (!camouflaged)
        {
            // Check shared herd LOS first, then personal vision
            _nearestPredator = FindNearestFromSet(GrazerPack.Instance?.SharedPredators)
                            ?? FindNearest("Predator", predatorDetectRadius);

            if (_nearestPredator != null)
            {
                GrazerPack.Instance?.BroadcastPredator(_nearestPredator);
                _state = State.Flee;
                return;
            }
        }

        _nearestPredator = null;

        if (Hunger < reproductionHungerThreshold)
        {
            _targetPlant = FindBestPlant();
            if (_targetPlant != null) { _state = State.SeekPlant; return; }
        }

        _state = State.Wander;
    }

    // ── State Execution ───────────────────────────────────────────────────────
    private void ExecuteState()
    {
        switch (_state)
        {
            case State.Flee: ExecuteFlee(); break;
            case State.SeekPlant: ExecuteSeekPlant(); break;
            case State.Wander: ExecuteWander(); break;
        }
    }

    private void ExecuteFlee()
    {
        Vector2 awayDir = ((Vector2)transform.position - (Vector2)_nearestPredator.position).normalized;
        Vector2 vel = awayDir * fleeSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd) PickNewWanderTarget();
        }
        _rb.linearVelocity = vel;
    }

    private void ExecuteSeekPlant()
    {
        if (_targetPlant == null) { _state = State.Wander; return; }

        Vector2 dir = (Vector2)_targetPlant.transform.position - (Vector2)transform.position;
        float dist = dir.magnitude;

        if (dist < eatDistance)
        {
            TryEat(_targetPlant);
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 vel = dir.normalized * moveSpeed;
            if (_genetics != null) vel *= _genetics.SpeedMultiplier;
            if (_avoidance != null)
            {
                vel = _avoidance.GetAvoidanceVelocity(vel);
                if (_avoidance.IsDeadEnd) { _targetPlant = null; PickNewWanderTarget(); }
            }
            _rb.linearVelocity = vel;
        }
    }

    private void ExecuteWander()
    {
        Vector2 toTarget = _wanderTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.2f || _wanderTimer <= 0f)
            PickNewWanderTarget();

        Vector2 vel = toTarget.normalized * (moveSpeed * 0.6f);

        // Herd mentality: blend toward pack centroid if active
        if (_genetics != null && _genetics.HasHerdMentality
            && GrazerPack.Instance != null
            && GrazerPack.Instance.HasPackCompanion(_genetics))
        {
            vel += GrazerPack.Instance.GetFlockOffset(_genetics);
        }

        if (_genetics != null) vel *= _genetics.SpeedMultiplier;

        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd) PickNewWanderTarget();
        }
        _rb.linearVelocity = vel;
    }

    // ── Actions ───────────────────────────────────────────────────────────────
    private void TryEat(Plant plant)
    {
        if (_eatTimer > 0f || plant == null) return;

        // Attractiveness check — bitter/tasty/poisonous affect willingness
        if (UnityEngine.Random.value > plant.EatAttractiveness) { _targetPlant = null; return; }

        _eatTimer = eatCooldown;
        Feed(plant.NutritionValue);
        Heal(10f);

        if (plant.IsPoisonous)
        {
            _isPoisoned = true;
            _poisonDamageRate = plant.PoisonDamagePerSec;
        }

        plant.Consume();
        _targetPlant = null;
    }

    /// <summary>Reflect damage back to attacker if Spiky trait is active.</summary>
    public override void TakeDamage(float amount)
    {
        if (_genetics != null && _genetics.HasSpiky)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Predator")) continue;
                h.GetComponent<EntityBase>()?.TakeDamage(amount * GrazerGenetics.EffectiveSpikyReflect);
                break;
            }
        }
        base.TakeDamage(amount);
    }

    private void TryReproduce()
    {
        if (_reproTimer > 0f) return;
        if (Hunger < reproductionHungerThreshold) return;
        _reproTimer = reproductionCooldown;

        // Find a nearby mate to inherit from
        Genome myGenome = _genetics != null ? _genetics.Genome : Genome.RandomGrazer();
        Genome mateGenome = FindMateGenome("Grazer") ?? myGenome;
        EcosystemManager.Instance?.SpawnGrazerOffspring((Vector2)transform.position, myGenome, mateGenome);
    }

    private Genome FindMateGenome(string tag)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var h in hits)
        {
            if (!h.CompareTag(tag) || h.gameObject == gameObject) continue;
            GrazerGenetics g = h.GetComponent<GrazerGenetics>();
            if (g != null) return g.Genome;
        }
        return null;
    }

    private void PickNewWanderTarget()
    {
        _wanderTimer = wanderInterval;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * wanderRadius;
        _wanderTarget = (Vector2)transform.position + offset;
        if (BoundaryManager.Instance != null)
            _wanderTarget = BoundaryManager.Instance.Clamp(_wanderTarget);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Transform FindNearest(string tag, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.CompareTag(tag)) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    private Transform FindNearestFromSet(IEnumerable<Transform> set)
    {
        if (set == null) return null;
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var t in set)
        {
            if (t == null) continue;
            float d = Vector2.Distance(transform.position, t.position);
            if (d < minDist) { minDist = d; nearest = t; }
        }
        return nearest;
    }

    /// <summary>
    /// Picks the most attractive fully-grown plant in range.
    /// Weighs attractiveness score against distance so grazers don't ignore nearby plants
    /// in favour of a very tasty but far-away one.
    /// </summary>
    private Plant FindBestPlant()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, plantDetectRadius);
        Plant best = null;
        float bestScore = -1f;
        foreach (var h in hits)
        {
            if (!h.CompareTag("Plant")) continue;
            Plant p = h.GetComponent<Plant>();
            if (p == null || !p.IsFullyGrown) continue;
            float dist = Vector2.Distance(transform.position, h.transform.position);
            float score = p.EatAttractiveness / (dist + 0.1f);
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    private void ClampToBounds()
    {
        if (BoundaryManager.Instance == null) return;
        Vector2 clamped = BoundaryManager.Instance.Clamp(transform.position);
        if ((Vector2)transform.position != clamped)
        {
            transform.position = clamped;
            _rb.linearVelocity = Vector2.zero;
            PickNewWanderTarget();
        }
    }
}