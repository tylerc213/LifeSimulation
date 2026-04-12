using System;
using UnityEngine;

/// <summary>
/// Grazer AI — a two-state machine: FLEE (predator nearby) or EAT (seek plants).
/// Falls back to WANDER when neither stimulus is present.
///
/// Requires: Rigidbody2D, Collider2D (trigger on a child for detection).
/// Tag this GameObject "Grazer".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Grazer : EntityBase
{
    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float wanderRadius = 2f;
    [SerializeField] private float wanderInterval = 2f;

    [Header("Detection")]
    [SerializeField] private float plantDetectRadius = 5f;
    [SerializeField] private float predatorDetectRadius = 7f;

    [Header("Eating")]
    [SerializeField] private float eatDistance = 0.4f;    // how close before eating
    [SerializeField] private float eatCooldown = 1f;

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 20f;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Wander, SeekPlant, Flee }
    private State _state = State.Wander;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private float _eatTimer;
    private float _reproTimer;
    private Plant _targetPlant;
    private Transform _nearestPredator;

    // ── Unity ─────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _reproTimer = reproductionCooldown;
        PickNewWanderTarget();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

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
        // Predator check (highest priority)
        _nearestPredator = FindNearest("Predator", predatorDetectRadius);
        if (_nearestPredator != null) { _state = State.Flee; return; }

        // Plant check (only when hungry)
        if (Hunger < reproductionHungerThreshold)
        {
            _targetPlant = FindNearestPlant();
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

        Vector2 dir = ((Vector2)_targetPlant.transform.position - (Vector2)transform.position);
        float dist = dir.magnitude;

        if (dist < eatDistance)
        {
            TryEat(_targetPlant);
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 vel = dir.normalized * moveSpeed;
            if (_avoidance != null)
            {
                vel = _avoidance.GetAvoidanceVelocity(vel);
                // Dead-end while seeking a plant — abandon this plant and wander
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
        _eatTimer = eatCooldown;
        Feed(plant.NutritionValue);
        Heal(10f);
        plant.Consume();
        _targetPlant = null;
    }

    private void TryReproduce()
    {
        if (_reproTimer > 0f) return;
        if (Hunger < reproductionHungerThreshold) return;
        _reproTimer = reproductionCooldown;
        EcosystemManager.Instance?.SpawnGrazer((Vector2)transform.position);
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

    private Plant FindNearestPlant()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, plantDetectRadius);
        Plant best = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.CompareTag("Plant")) continue;
            Plant p = h.GetComponent<Plant>();
            if (p == null || !p.IsFullyGrown) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; best = p; }
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