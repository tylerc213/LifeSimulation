using System;
using UnityEngine;

/// <summary>
/// Predator AI — HUNT (hungry) > PATROL.
/// Integrates with PredatorGenetics for stat modifiers and special behaviours.
/// Tag this GameObject "Predator".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Predator : EntityBase
{
    [Header("Movement")]
    [SerializeField] private float huntSpeed = 5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolInterval = 3f;

    [Header("Detection & Attack")]
    [SerializeField] private float detectRadius = 9f;
    [SerializeField] private float attackDamage = 60f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float nutritionGain = 60f;

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 30f;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Patrol, Hunt }
    private State _state = State.Patrol;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private PredatorGenetics _genetics;
    private Vector2 _patrolTarget;
    private float _patrolTimer;
    private float _attackTimer;
    private float _reproTimer;
    private Transform _prey;

    // Venom tracking: list of (target, damagePerSec) applied each Update
    private EntityBase _venomTarget;
    private float _venomTimer;
    private const float VenomDuration = 8f;

    // ── Unity ─────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _genetics = GetComponent<PredatorGenetics>();
        _reproTimer = reproductionCooldown;
        PickNewPatrolTarget();
    }

    private void Start()
    {
        PredatorPack.Instance?.Register(_genetics);
    }

    protected virtual void OnDestroy()
    {
        PredatorPack.Instance?.Unregister(_genetics);
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        // Tick venom on current target
        if (_venomTarget != null && !_venomTarget.IsDead)
        {
            _venomTimer -= Time.deltaTime;
            _venomTarget.TakeDamage(PredatorGenetics.EffectiveVenomDamagePerSec * Time.deltaTime);
            if (_venomTimer <= 0f) _venomTarget = null;
        }

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
        if (Hunger < reproductionHungerThreshold * 0.9f)
        {
            // Apex predator can also attack other predators
            _prey = (_genetics != null && _genetics.IsApexPredator)
                ? FindNearestPrey()
                : FindNearestGrazer();

            // Herd hunter uses shared pack target if available
            if (_genetics != null && _genetics.HasHerdHunter
                && PredatorPack.Instance?.SharedTarget != null)
            {
                _prey = PredatorPack.Instance.SharedTarget;
            }

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

        // Ambusher: try to approach from outside the prey's LOS
        Vector2 dir = ((Vector2)_prey.position - (Vector2)transform.position).normalized;
        Vector2 vel = dir * huntSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
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
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
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

        bool hitsGrazer = other.CompareTag("Grazer");
        bool hitsPredator = _genetics != null && _genetics.IsApexPredator && other.CompareTag("Predator");

        if (!hitsGrazer && !hitsPredator) return;
        if (hitsPredator && other.gameObject == gameObject) return;  // don't self-hit

        EntityBase target = other.GetComponent<EntityBase>();
        if (target == null || target.IsDead) return;

        _attackTimer = attackCooldown;

        float damage = attackDamage;
        if (_genetics != null) damage *= _genetics.DamageMultiplier;

        // Ambusher bonus: extra damage if target doesn't see this predator
        if (_genetics != null && _genetics.IsAmbusher)
        {
            Grazer grazer = other.GetComponent<Grazer>();
            // If the grazer is not currently fleeing, it hasn't spotted us
            if (grazer != null)
                damage *= PredatorGenetics.EffectiveAmbushDamageBonus;
        }

        target.TakeDamage(damage);
        Feed(nutritionGain);
        Heal(20f);

        // Apply venom
        if (_genetics != null && _genetics.IsVenomous)
        {
            _venomTarget = target;
            _venomTimer = VenomDuration;
        }
    }

    // ── Reproduction ──────────────────────────────────────────────────────────
    private void TryReproduce()
    {
        if (_reproTimer > 0f) return;
        if (Hunger < reproductionHungerThreshold) return;
        _reproTimer = reproductionCooldown;

        Genome myGenome = _genetics != null ? _genetics.Genome : Genome.RandomPredator();
        Genome mateGenome = FindMateGenome() ?? myGenome;
        EcosystemManager.Instance?.SpawnPredatorOffspring((Vector2)transform.position, myGenome, mateGenome);
    }

    private Genome FindMateGenome()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var h in hits)
        {
            if (!h.CompareTag("Predator") || h.gameObject == gameObject) continue;
            PredatorGenetics g = h.GetComponent<PredatorGenetics>();
            if (g != null) return g.Genome;
        }
        return null;
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

    private Transform FindNearestPrey()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            bool isGrazer = h.CompareTag("Grazer");
            bool isPredator = h.CompareTag("Predator") && h.gameObject != gameObject;
            if (!isGrazer && !isPredator) continue;
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