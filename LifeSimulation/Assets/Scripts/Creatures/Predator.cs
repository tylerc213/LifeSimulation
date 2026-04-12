using System;
using UnityEngine;

/// <summary>
/// Predator AI — hunts the nearest Grazer when hungry, otherwise patrols.
/// Attacks a Grazer by colliding with it (OnTriggerEnter2D).
///
/// Requires: Rigidbody2D, Collider2D (Is Trigger = true).
/// Tag this GameObject "Predator".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Predator : EntityBase
{
    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float huntSpeed = 5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolInterval = 3f;

    [Header("Detection & Attack")]
    [SerializeField] private float detectRadius = 9f;
    [SerializeField] private float attackDamage = 60f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float nutritionGain = 60f;   // hunger restored per kill

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 30f;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Patrol, Hunt }
    private State _state = State.Patrol;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private Vector2 _patrolTarget;
    private float _patrolTimer;
    private float _attackTimer;
    private float _reproTimer;
    private Transform _prey;

    // ── Unity ─────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _reproTimer = reproductionCooldown;
        PickNewPatrolTarget();
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
        _patrolTimer -= Time.deltaTime;
        _attackTimer -= Time.deltaTime;
        _reproTimer -= Time.deltaTime;
    }

    // ── Perception ────────────────────────────────────────────────────────────
    private void UpdateState()
    {
        // Only hunt when hungry enough to bother
        if (Hunger < reproductionHungerThreshold * 0.9f)
        {
            _prey = FindNearestGrazer();
            _state = (_prey != null) ? State.Hunt : State.Patrol;
        }
        else
        {
            _state = State.Patrol;
        }
    }

    // ── State Execution ───────────────────────────────────────────────────────
    private void ExecuteState()
    {
        switch (_state)
        {
            case State.Hunt: ExecuteHunt(); break;
            case State.Patrol: ExecutePatrol(); break;
        }
    }

    private void ExecuteHunt()
    {
        if (_prey == null) { _state = State.Patrol; return; }

        Vector2 dir = ((Vector2)_prey.position - (Vector2)transform.position).normalized;
        Vector2 vel = dir * huntSpeed;
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            // Dead-end while hunting — drop this prey and patrol until a clear path opens
            if (_avoidance.IsDeadEnd) { _prey = null; PickNewPatrolTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    private void ExecutePatrol()
    {
        Vector2 toTarget = _patrolTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.3f || _patrolTimer <= 0f)
            PickNewPatrolTarget();

        Vector2 vel = toTarget.normalized * patrolSpeed;
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd) PickNewPatrolTarget();
        }
        _rb.linearVelocity = vel;
    }

    // ── Attack on Contact ─────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_attackTimer > 0f) return;
        if (!other.CompareTag("Grazer")) return;

        EntityBase grazer = other.GetComponent<EntityBase>();
        if (grazer == null || grazer.IsDead) return;

        _attackTimer = attackCooldown;
        grazer.TakeDamage(attackDamage);
        Feed(nutritionGain);
        Heal(20f);
    }

    // ── Reproduction ──────────────────────────────────────────────────────────
    private void TryReproduce()
    {
        if (_reproTimer > 0f) return;
        if (Hunger < reproductionHungerThreshold) return;
        _reproTimer = reproductionCooldown;
        EcosystemManager.Instance?.SpawnPredator((Vector2)transform.position);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Transform FindNearestGrazer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (!h.CompareTag("Grazer")) continue;
            EntityBase e = h.GetComponent<EntityBase>();
            if (e != null && e.IsDead) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    private void PickNewPatrolTarget()
    {
        _patrolTimer = patrolInterval;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolRadius;
        _patrolTarget = (Vector2)transform.position + offset;

        if (BoundaryManager.Instance != null)
            _patrolTarget = BoundaryManager.Instance.Clamp(_patrolTarget);
    }

    private void ClampToBounds()
    {
        if (BoundaryManager.Instance == null) return;
        Vector2 clamped = BoundaryManager.Instance.Clamp(transform.position);
        if ((Vector2)transform.position != clamped)
        {
            transform.position = clamped;
            _rb.linearVelocity = Vector2.zero;
            PickNewPatrolTarget();
        }
    }
}