// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Lifeform States
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.1
//
// Description:
//    AI controller for predator entities. Implements a priority state machine:
//    Stalk/Dash (ambusher, cover available) > Hunt (hungry) > Patrol. Supports
//    venom damage over time, ambush cover-hiding and dash lunge, apex prey
//    targeting, night vision, reptile speed scaling, and Mendelian reproduction.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

/// <summary>Priority state machine AI for predator entities.</summary>
/// <remarks>
/// Tag this GameObject "Predator". Requires PredatorGenetics, SteeringAvoidance,
/// StateLabel child, and VisionCone on the same prefab for full functionality.
/// Attack uses OnTriggerStay2D so multiple hits land while overlapping a target.
/// Ambushers gain Stalk and Dash states: creep to cover, then burst at prey.
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
    // Chase is abandoned when prey exceeds this distance from the predator
    [SerializeField] private float maxChaseDistance = 20f;

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 30f;

    // Separate from reproductionHungerThreshold so a single kill doesn't abort the hunt
    [SerializeField] private float huntHungerThreshold = 60f;

    [Header("Ambush")]
    // How close to a cover object before holding position
    [SerializeField] private float ambushCoverRadius = 1.2f;
    // Burst speed during the dash lunge
    [SerializeField] private float ambushDashSpeed = 14f;
    // How long the dash lasts in seconds
    [SerializeField] private float ambushDashDuration = 0.4f;
    // Max distance to prey before launching a dash
    [SerializeField] private float ambushTriggerRange = 5f;
    // Slow creep speed while moving toward cover
    [SerializeField] private float ambushStalkSpeed = 1.2f;

    private enum State { Patrol, Hunt, Stalk, Dash }
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
    private string _currentStateLabel = StateLabel.Patrol;

    // Ambush state tracking
    private Vector2 _coverPosition;
    private float _dashTimer = 0f;

    // Venom tracking
    private EntityBase _venomTarget;
    private float _venomTimer;
    private const float VenomDuration = 8f;

    // Stuck detection uses goal progress rather than raw movement
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
        // Start reproduction timer full so predators don't breed immediately on spawn
        _reproTimer = reproductionCooldown;
        PickNewPatrolTarget();
        _lastPosition = transform.position;
    }

    /// <summary>Ticks venom, updates AI state, and runs movement each frame.</summary>
    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        // Apply venom damage silently so it doesn't trigger hit flash each tick
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
        if (_dashTimer > 0f) _dashTimer -= Time.deltaTime;
    }

    /// <summary>Evaluates whether to stalk, dash, hunt, or patrol.</summary>
    private void UpdateState()
    {
        bool isAmbusher = _genetics != null && _genetics.IsAmbusher;

        // Dash is time-limited — let it finish before re-evaluating
        if (_state == State.Dash)
        {
            if (_dashTimer > 0f) return;
            // Dash finished — switch to normal hunt to close any remaining gap
            _state = State.Hunt;
        }

        // Hold current hunt/stalk target while it is alive and within chase range
        if ((_state == State.Hunt || _state == State.Stalk) && _prey != null
            && _prey.gameObject.activeInHierarchy)
        {
            EntityBase e = _prey.GetComponent<EntityBase>();
            if (e != null && !e.IsDead)
            {
                float distToPrey = Vector2.Distance(transform.position, _prey.position);
                if (distToPrey > maxChaseDistance)
                {
                    // Prey escaped — give up and patrol
                    _prey = null;
                    _state = State.Patrol;
                    return;
                }

                // Ambusher: check whether to launch a dash from cover
                if (isAmbusher && _state == State.Stalk)
                {
                    if (IsHiddenByCover() && distToPrey <= ambushTriggerRange)
                    {
                        _state = State.Dash;
                        _dashTimer = ambushDashDuration;
                    }
                }
                return;
            }
        }

        // Begin a new hunt only when hungry enough
        if (Hunger < huntHungerThreshold)
        {
            // Apex Predator can target other predators as well as grazers
            _prey = (_genetics != null && _genetics.IsApexPredator)
                ? FindNearestPrey()
                : FindNearestGrazer();

            if (_prey != null)
            {
                // Ambushers stalk toward cover first; normal predators hunt directly
                _state = (isAmbusher && FindNearestCover() != Vector2.negativeInfinity)
                    ? State.Stalk
                    : State.Hunt;
            }
            else
            {
                _state = State.Patrol;
            }
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
            case State.Stalk: ExecuteStalk(); break;
            case State.Dash: ExecuteDash(); break;
        }
        UpdateLabel();
        _cone?.UpdateCone(_rb.linearVelocity, _currentStateLabel);

        // Enable creature avoidance only while patrolling to avoid disrupting hunts
        if (_avoidance != null)
            _avoidance.AvoidCreatures = (_state == State.Patrol);
    }

    /// <summary>Updates the state label and caches the current label string.</summary>
    private void UpdateLabel()
    {
        if (_label == null) return;
        if (IsDead) { _label.SetState(StateLabel.Dead); return; }
        _currentStateLabel = _state switch
        {
            State.Hunt => StateLabel.Hunt,
            State.Stalk => StateLabel.Stalk,
            State.Dash => StateLabel.Dash,
            _ => StateLabel.Patrol,
        };
        _label.SetState(_currentStateLabel);
    }

    /// <summary>Moves toward the current prey target at hunt speed.</summary>
    private void ExecuteHunt()
    {
        if (_prey == null) { _state = State.Patrol; return; }

        Vector2 toprey = (Vector2)_prey.position - (Vector2)transform.position;
        if (toprey.sqrMagnitude < 0.001f) return;

        Vector2 dir = toprey.normalized;
        Vector2 vel = dir * huntSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f)
            {
                _deadEndCooldown = DeadEndCooldownTime;
                _prey = null;
                PickNewPatrolTarget();
            }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Moves toward the current patrol target at patrol speed.</summary>
    private void ExecutePatrol()
    {
        Vector2 toTarget = _patrolTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.3f || _patrolTimer <= 0f)
            PickNewPatrolTarget();

        toTarget = _patrolTarget - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.001f) return;

        Vector2 vel = toTarget.normalized * patrolSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f)
            {
                _deadEndCooldown = DeadEndCooldownTime;
                PickNewPatrolTarget();
            }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Creeps toward the nearest cover object, then holds position waiting to dash.</summary>
    private void ExecuteStalk()
    {
        if (_prey == null) { _state = State.Patrol; return; }

        Vector2 cover = FindNearestCover();
        if (cover == Vector2.negativeInfinity)
        {
            // No cover available — fall back to normal hunt
            _state = State.Hunt;
            return;
        }

        _coverPosition = cover;
        Vector2 tocover = _coverPosition - (Vector2)transform.position;
        float distToCover = tocover.magnitude;

        Vector2 vel;
        if (distToCover > ambushCoverRadius && distToCover > 0.001f)
        {
            // Creep toward cover slowly to avoid alerting nearby grazers
            vel = tocover.normalized * ambushStalkSpeed;
        }
        else
        {
            // In cover — hold still and wait for prey to enter dash range
            vel = Vector2.zero;
        }

        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        if (_avoidance != null && vel.sqrMagnitude > 0.001f)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f)
            {
                _deadEndCooldown = DeadEndCooldownTime;
                // Cover unreachable — switch to direct hunt
                _state = State.Hunt;
            }
        }

        // Guard against NaN before assigning — can occur if avoidance
        // normalises a near-zero vector internally
        if (float.IsNaN(vel.x) || float.IsNaN(vel.y)) vel = Vector2.zero;
        _rb.linearVelocity = vel;
    }

    /// <summary>Bursts straight toward prey at high speed for a short fixed duration.</summary>
    private void ExecuteDash()
    {
        if (_prey == null) { _state = State.Patrol; return; }

        Vector2 toprey = (Vector2)_prey.position - (Vector2)transform.position;
        if (toprey.sqrMagnitude < 0.001f) return;

        // Dash ignores avoidance so nothing interrupts the lunge
        float speed = ambushDashSpeed * (_genetics != null ? _genetics.SpeedMultiplier : 1f);
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
            speed *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        _rb.linearVelocity = toprey.normalized * speed;
    }

    /// <summary>Deals attack damage to overlapping prey on each cooldown tick.</summary>
    /// <remarks>Uses OnTriggerStay2D so the predator keeps attacking while overlapping.</remarks>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (_attackTimer > 0f) return;

        bool hitsGrazer = other.CompareTag("Grazer");
        // Apex Predator can also attack other predators
        bool hitsPredator = _genetics != null && _genetics.IsApexPredator && other.CompareTag("Predator");

        if (!hitsGrazer && !hitsPredator) return;
        // Prevent Apex Predator from self-hitting
        if (hitsPredator && other.gameObject == gameObject) return;

        EntityBase target = other.GetComponent<EntityBase>();
        if (target == null || target.IsDead) return;

        _attackTimer = attackCooldown;

        float damage = attackDamage;
        if (_genetics != null) damage *= _genetics.DamageMultiplier;

        // Ambush bonus applies when the target hasn't spotted this predator
        if (_genetics != null && _genetics.IsAmbusher)
        {
            Grazer grazer = other.GetComponent<Grazer>();
            if (grazer != null)
                damage *= PredatorGenetics.AmbushDamageBonus;
        }

        target.TakeDamage(damage);

        // Nutrition and healing awarded only on kill, not on every hit
        if (target.IsDead)
        {
            Feed(nutritionGain);
            Heal(20f);
        }

        // Start venom if the Venomous gene is expressed
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
        // Find a nearby mate; fall back to self-genome if none available
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
            // Skip trigger colliders to avoid matching detection zones
            if (h.isTrigger) continue;

            // Walk to the root tagged object in case the collider is on a child
            Transform root = h.transform;
            while (root.parent != null && !root.CompareTag("Predator"))
                root = root.parent;

            if (!root.CompareTag("Predator")) continue;
            // Exclude self by comparing root transforms
            if (root == transform) continue;

            PredatorGenetics g = root.GetComponent<PredatorGenetics>();
            if (g != null && g.Genome != null) return g.Genome;
        }
        return null;
    }

    /// <summary>Finds the nearest living grazer within the current vision-adjusted detection range.</summary>
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
            // Apex Predator treats other predators as valid prey except itself
            bool isPredator = h.CompareTag("Predator") && h.gameObject != gameObject;
            if (!isGrazer && !isPredator) continue;
            EntityBase e = h.GetComponent<EntityBase>();
            if (e != null && e.IsDead) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h.transform; }
        }
        return nearest;
    }

    /// <summary>Returns true when an obstacle or plant blocks the line of sight to prey.</summary>
    private bool IsHiddenByCover()
    {
        if (_prey == null) return false;

        Vector2 toprey = (Vector2)_prey.position - (Vector2)transform.position;
        float dist = toprey.magnitude;
        if (dist < 0.001f) return false;

        // A hit on Obstacle or Plant means the predator is hidden from prey
        RaycastHit2D hit = Physics2D.Raycast(transform.position, toprey.normalized, dist);
        if (hit.collider == null) return false;
        return hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Plant");
    }

    /// <summary>Finds the world position of the nearest cover object between predator and prey.</summary>
    /// <returns>Cover world position, or Vector2.negativeInfinity if none found.</returns>
    private Vector2 FindNearestCover()
    {
        if (_prey == null) return Vector2.negativeInfinity;

        float searchRadius = Vector2.Distance(transform.position, _prey.position);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        Vector2 best = Vector2.negativeInfinity;
        float bestScore = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Obstacle") && !h.CompareTag("Plant")) continue;

            Vector2 coverPos = h.bounds.center;
            float distToCover = Vector2.Distance(transform.position, coverPos);
            float coverToPrey = Vector2.Distance(coverPos, _prey.position);

            // Only consider cover that sits roughly between predator and prey
            if (coverToPrey > searchRadius) continue;

            // Prefer the closest usable cover
            if (distToCover < bestScore) { bestScore = distToCover; best = coverPos; }
        }

        return best;
    }

    /// <summary>Detects when the predator is stuck and selects a new patrol target to escape.</summary>
    private void CheckIfStuck()
    {
        // Use prey position as goal while hunting/stalking, patrol target otherwise
        Vector2 goal = (_state == State.Hunt || _state == State.Stalk) && _prey != null
            ? (Vector2)_prey.position
            : _patrolTarget;

        float distToGoal = Vector2.Distance(transform.position, goal);
        float moved = Vector2.Distance(transform.position, _lastPosition);

        // Reset timer when making meaningful progress toward the goal
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

            // Drop prey and patrol around the obstacle rather than staying stuck
            if (_state == State.Hunt || _state == State.Stalk)
            {
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

        // Normalise distance from centre to 0 (centre) to 1 (wall edge)
        float halfW = (b.MaxX - b.MinX) * 0.5f;
        float halfH = (b.MaxY - b.MinY) * 0.5f;
        float edgeX = Mathf.Clamp01(Mathf.Abs(pos.x - mapCenter.x) / halfW);
        float edgeY = Mathf.Clamp01(Mathf.Abs(pos.y - mapCenter.y) / halfH);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        Vector2 candidate;
        if (edgeFactor > 0.6f)
        {
            // Pull patrol target toward centre when close to a wall
            Vector2 toCenter = mapCenter - pos;
            if (toCenter.sqrMagnitude < 0.001f) toCenter = Vector2.up;
            float pullDist = Mathf.Lerp(patrolRadius, 6f, edgeFactor);
            candidate = pos + toCenter.normalized * pullDist + (Vector2)(UnityEngine.Random.insideUnitCircle * patrolRadius * 0.5f);
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

    /// <summary>Returns the detection radius scaled by current sunlight and night vision trait.</summary>
    /// <returns>Vision-adjusted detection radius in world units.</returns>
    private float GetCurrentDetectRadius()
    {
        float visionMult = 1.0f;

        if (EnvironmentHandler.Instance != null)
        {
            float intensity = EnvironmentHandler.Instance.SunlightIntensity;

            // Night Vision raises the vision floor; without it the predator is nearly blind in dark
            bool hasNightVision = _genetics != null && _genetics.HasNightVision;
            float floor = hasNightVision ? 0.8f : 0.2f;

            visionMult = Mathf.Lerp(floor, 1.0f, intensity);
        }

        return detectRadius * visionMult;
    }
}
