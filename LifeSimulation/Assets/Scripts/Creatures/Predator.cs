// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeform States
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    AI controller for predator entities. Implements a two-state machine:
//    Hunt (hungry, prey in range) > Patrol. Supports venom damage over time,
//    ambush bonus damage, apex prey targeting, and Mendelian reproduction.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary>Two-state AI controller for predator entities.</summary>
/// <remarks>
/// Tag this GameObject "Predator". Requires PredatorGenetics, SteeringAvoidance,
/// StateLabel child, and VisionCone on the same prefab for full functionality.
/// Attack uses OnTriggerStay2D so multiple hits land while overlapping a target.
/// </remarks>
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
    [SerializeField] private float maxChaseDistance = 20f;  // give up if prey gets this far away

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 30f;

    // Predator hunts when hunger drops below this — kept separate from
    // reproductionHungerThreshold so a single kill doesn't immediately stop the hunt
    [SerializeField] private float huntHungerThreshold = 60f;

    private enum State { Patrol, Hunt }
    private State _state = State.Patrol;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private PredatorGenetics _genetics;
    private StateLabel _label;
    private VisionCone _cone;
    private Vector2 _patrolTarget;
    private float _patrolTimer;
    private float _attackTimer;
    private float _reproTimer;
    private Transform _prey;

    // Venom tracking
    private EntityBase _venomTarget;
    private float _venomTimer;
    private const float VenomDuration = 8f;

    // Stuck detection
    private Vector2 _lastPosition;
    private float _stuckTimer = 0f;
    private float _deadEndCooldown = 0f;
    private const float StuckThreshold = 3f;
    private const float StuckMinMove = 0.15f;
    private const float DeadEndCooldownTime = 0.5f;

    /// <summary>Caches all required components and initialises patrol state.</summary>
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _genetics = GetComponent<PredatorGenetics>();
        _label = GetComponentInChildren<StateLabel>();
        _cone = GetComponent<VisionCone>();
        _reproTimer = reproductionCooldown;
        PickNewPatrolTarget();
        _lastPosition = transform.position;
    }

    /// <summary>Ticks venom, updates AI state, and runs movement each frame.</summary>
    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        // Tick venom on current target
        if (_venomTarget != null && !_venomTarget.IsDead)
        {
            _venomTimer -= Time.deltaTime;
            _venomTarget.TakeDamageSilent(PredatorGenetics.VenomDamagePerSec * Time.deltaTime);
            if (_venomTimer <= 0f) _venomTarget = null;
        }

        UpdateTimers();
        UpdateState();
        ExecuteState();
        ClampToBounds();
        CheckIfStuck();
        TryReproduce();
    }

    /// <summary>Decrements all cooldown and interval timers each frame.</summary>
    private void UpdateTimers()
    {
        _patrolTimer -= Time.deltaTime;
        _attackTimer -= Time.deltaTime;
        _reproTimer -= Time.deltaTime;
        _deadEndCooldown -= Time.deltaTime;
    }

    /// <summary>Evaluates whether to hunt, continue chasing, or patrol.</summary>
    private void UpdateState()
    {
        // If currently hunting a live prey, keep hunting unless it gets too far away
        if (_state == State.Hunt && _prey != null
            && _prey.gameObject.activeInHierarchy)
        {
            EntityBase e = _prey.GetComponent<EntityBase>();
            if (e != null && !e.IsDead)
            {
                float distToPrey = Vector2.Distance(transform.position, _prey.position);
                if (distToPrey <= maxChaseDistance)
                    return;   // still in range — keep chasing

                // Prey escaped — give up and patrol
                _prey = null;
                _state = State.Patrol;
                return;
            }
        }

        // Start a new hunt only when hungry enough
        if (Hunger < huntHungerThreshold)
        {
            _prey = (_genetics != null && _genetics.IsApexPredator)
                ? FindNearestPrey()
                : FindNearestGrazer();

            _state = (_prey != null) ? State.Hunt : State.Patrol;
        }
        else
        {
            _prey = null;
            _state = State.Patrol;
        }
    }

    /// <summary>Dispatches movement execution to the active state handler.</summary>
    private void ExecuteState()
    {
        switch (_state)
        {
            case State.Hunt: ExecuteHunt(); break;
            case State.Patrol: ExecutePatrol(); break;
        }
        UpdateLabel();
        _cone?.UpdateCone(_rb.linearVelocity, _currentStateLabel);

        // Avoid other creatures only while patrolling — not while actively hunting
        if (_avoidance != null)
            _avoidance.AvoidCreatures = (_state == State.Patrol);
    }

    private string _currentStateLabel = StateLabel.Patrol;

    /// <summary>Updates the state label and caches the current label string.</summary>
    private void UpdateLabel()
    {
        if (_label == null) return;
        if (IsDead) { _label.SetState(StateLabel.Dead); return; }
        _currentStateLabel = _state == State.Hunt ? StateLabel.Hunt : StateLabel.Patrol;
        _label.SetState(_currentStateLabel);
    }

    /// <summary>Moves toward the current prey target at hunt speed.</summary>
    private void ExecuteHunt()
    {
        if (_prey == null) { _state = State.Patrol; return; }

        // Ambusher: try to approach from outside the prey's LOS
        Vector2 dir = ((Vector2)_prey.position - (Vector2)transform.position).normalized;
        Vector2 vel = dir * huntSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
        {
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        }
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; _prey = null; PickNewPatrolTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Moves toward the current patrol target at patrol speed.</summary>
    private void ExecutePatrol()
    {
        Vector2 toTarget = _patrolTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.3f || _patrolTimer <= 0f)
            PickNewPatrolTarget();

        Vector2 vel = toTarget.normalized * patrolSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
        {
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        }
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; PickNewPatrolTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Deals attack damage to overlapping prey on each cooldown tick.</summary>
    /// <remarks>Uses OnTriggerStay2D so the predator keeps attacking while overlapping.</remarks>
    private void OnTriggerStay2D(Collider2D other)
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
                damage *= PredatorGenetics.AmbushDamageBonus;
        }

        target.TakeDamage(damage);

        // Only feed on kill — prevents a single hit from aborting the hunt
        if (target.IsDead)
        {
            Feed(nutritionGain);
            Heal(20f);
        }

        // Apply venom
        if (_genetics != null && _genetics.IsVenomous)
        {
            _venomTarget = target;
            _venomTimer = VenomDuration;
        }
    }

    /// <summary>Spawns a predator offspring when hunger and cooldown conditions are met.</summary>
    private void TryReproduce()
    {
        if (_reproTimer > 0f) return;
        if (Hunger < reproductionHungerThreshold) return;
        _reproTimer = reproductionCooldown;

        Genome myGenome = _genetics != null ? _genetics.Genome : Genome.RandomPredator();
        Genome mateGenome = FindMateGenome() ?? myGenome;
        EcosystemManager.Instance?.SpawnPredatorOffspring((Vector2)transform.position, myGenome, mateGenome);
    }

    /// <summary>Finds the genome of a nearby predator to use as a reproduction mate.</summary>
    /// <returns>Genome of the nearest valid predator mate, or null if none found.</returns>
    private Genome FindMateGenome()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;

            Transform root = h.transform;
            while (root.parent != null && !root.CompareTag("Predator"))
                root = root.parent;

            if (!root.CompareTag("Predator")) continue;
            if (root == transform) continue;  // skip self

            PredatorGenetics g = root.GetComponent<PredatorGenetics>();
            if (g != null && g.Genome != null) return g.Genome;
        }
        return null;
    }

    /// <summary>Finds the nearest living grazer within detection range.</summary>
    /// <returns>Transform of the nearest grazer, or null if none found.</returns>
    private Transform FindNearestGrazer()
    {
        float currentRadius = GetCurrentDetectRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);
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

    /// <summary>Finds the nearest living grazer or predator (Apex mode) within detection range.</summary>
    /// <returns>Transform of the nearest valid prey, or null if none found.</returns>
    private Transform FindNearestPrey()
    {
        float currentRadius = GetCurrentDetectRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);
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

    /// <summary>Detects when the predator is stuck and selects a new patrol target to escape.</summary>
    private void CheckIfStuck()
    {
        Vector2 goal = _state == State.Hunt && _prey != null
            ? (Vector2)_prey.position
            : _patrolTarget;

        float distToGoal = Vector2.Distance(transform.position, goal);
        float moved = Vector2.Distance(transform.position, _lastPosition);

        if (moved > StuckMinMove && distToGoal < Vector2.Distance(_lastPosition, goal) + 0.5f)
        {
            _stuckTimer = 0f;
            _lastPosition = transform.position;
            return;
        }

        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= StuckThreshold)
        {
            _stuckTimer = 0f;
            _deadEndCooldown = 0f;
            _rb.linearVelocity = Vector2.zero;

            if (_state == State.Hunt)
            {
                // Drop this prey and pick a new patrol point to route around the obstacle
                _prey = null;
                _state = State.Patrol;
            }

            _patrolTimer = 0f;
            PickNewPatrolTarget();
            _lastPosition = transform.position;
        }
    }

    /// <summary>Selects a new patrol destination biased toward the map centre when near walls.</summary>
    private void PickNewPatrolTarget()
    {
        _patrolTimer = patrolInterval;

        if (BoundaryManager.Instance == null)
        {
            _patrolTarget = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * patrolRadius;
            return;
        }

        var b = BoundaryManager.Instance;
        float wallMargin = 1.5f;
        float safeMinX = Mathf.Min(b.MinX + wallMargin, b.MaxX - 0.1f);
        float safeMaxX = Mathf.Max(b.MaxX - wallMargin, b.MinX + 0.1f);
        float safeMinY = Mathf.Min(b.MinY + wallMargin, b.MaxY - 0.1f);
        float safeMaxY = Mathf.Max(b.MaxY - wallMargin, b.MinY + 0.1f);

        Vector2 pos = transform.position;
        Vector2 mapCenter = new Vector2((b.MinX + b.MaxX) * 0.5f, (b.MinY + b.MaxY) * 0.5f);

        float halfW = (b.MaxX - b.MinX) * 0.5f;
        float halfH = (b.MaxY - b.MinY) * 0.5f;
        float edgeX = Mathf.Clamp01(Mathf.Abs(pos.x - mapCenter.x) / halfW);
        float edgeY = Mathf.Clamp01(Mathf.Abs(pos.y - mapCenter.y) / halfH);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        Vector2 candidate;
        if (edgeFactor > 0.6f)
        {
            Vector2 toCenter = (mapCenter - pos).normalized;
            float pullDist = Mathf.Lerp(patrolRadius, 6f, edgeFactor);
            candidate = pos + toCenter * pullDist + (Vector2)(UnityEngine.Random.insideUnitCircle * patrolRadius * 0.5f);
        }
        else
        {
            candidate = pos + UnityEngine.Random.insideUnitCircle.normalized * patrolRadius;
        }

        candidate.x = Mathf.Clamp(candidate.x, safeMinX, safeMaxX);
        candidate.y = Mathf.Clamp(candidate.y, safeMinY, safeMaxY);
        _patrolTarget = candidate;
    }

    /// <summary>Snaps the predator back inside map bounds and picks a new inward patrol target.</summary>
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

    private float GetCurrentDetectRadius()
    {
        float visionMult = 1.0f;

        if (EnvironmentHandler.Instance != null)
        {
            float intensity = EnvironmentHandler.Instance.SunlightIntensity;

            // If they have the trait, their vision floor is 0.8 (Night Hunter)
            // If they don't, they are blind like everyone else (0.2 floor)
            bool hasNightVision = _genetics != null && _genetics.HasNightVision;
            float floor = hasNightVision ? 0.8f : 0.2f;

            visionMult = Mathf.Lerp(floor, 1.0f, intensity);
        }

        return detectRadius * visionMult;
    }
}
