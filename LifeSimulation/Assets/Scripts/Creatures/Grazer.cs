// -----------------------------------------------------------------------------
// Project:     EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:        Interactions
// Requirement: Lifeform Simulation
// Author:      Luke Kivett
// Date:        04/06/2026
// Version:     0.0.0
//
// Description:
//    AI controller for grazer entities. Implements a priority state machine:
//    Flee (predator nearby) > SeekPlant (hungry) > Wander. Integrates with
//    GrazerGenetics for stat modifiers, SteeringAvoidance for obstacle routing,
//    and supports Mendelian reproduction with nearby mates.
// -----------------------------------------------------------------------------
using System;
using UnityEngine;

/// <summary>Priority state machine AI for grazer entities.</summary>
/// <remarks>
/// Tag this GameObject "Grazer". Requires GrazerGenetics, SteeringAvoidance,
/// StateLabel child, and VisionCone on the same prefab for full functionality.
/// Each component is optional; the grazer degrades gracefully without them.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class Grazer : EntityBase
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderInterval = 3f;

    [Header("Detection")]
    [SerializeField] private float plantDetectRadius = 5f;
    [SerializeField] private float predatorDetectRadius = 7f;

    [Header("Eating")]
    [Tooltip("Max gap between grazer and plant colliders to bite (not center-to-center). Overlapping colliders always count as in range.")]
    [SerializeField] private float eatDistance = 0.45f;
    [SerializeField] private float eatCooldown = 1f;

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 20f;

    private enum State { Wander, SeekPlant, Flee }
    private State _state = State.Wander;
    private Rigidbody2D _rb;
    private SteeringAvoidance _avoidance;
    private GrazerGenetics _genetics;
    private StateLabel _label;
    private VisionCone _cone;
    private Collider2D _selfCollider;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private float _eatTimer;
    private float _reproTimer;
    private Plant _targetPlant;
    private Transform _nearestPredator;

    // Cached last known predator position for flee continuation after losing sight
    private Vector2 _lastKnownPredatorPos;
    // Timer keeping the grazer in Flee state briefly after a predator leaves range
    private float _fleeTimer = 0f;
    private const float MinFleeDuration = 1.5f;

    // Poison tracking
    private bool _isPoisoned = false;
    private float _poisonDamageRate = 0f;

    // Stuck detection
    private Vector2 _lastPosition;
    private float _stuckTimer = 0f;
    private float _deadEndCooldown = 0f;
    private const float StuckThreshold = 3f;    // seconds before declaring stuck
    private const float StuckMinMove = 0.15f; // units — less than this = stuck
    private const float DeadEndCooldownTime = 0.5f;

    /// <summary>Caches all required components and initialises movement state.</summary>
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _avoidance = GetComponent<SteeringAvoidance>();
        _genetics = GetComponent<GrazerGenetics>();
        _label = GetComponentInChildren<StateLabel>();
        _cone = GetComponent<VisionCone>();
        _selfCollider = GetComponent<Collider2D>();
        _reproTimer = reproductionCooldown;
        PickNewWanderTarget();
        _lastPosition = transform.position;
    }

    /// <summary>Ticks poison damage, updates AI state, and runs movement each frame.</summary>
    protected override void Update()
    {
        base.Update();
        if (IsDead) return;

        if (_isPoisoned)
            TakeDamageSilent(_poisonDamageRate * Time.deltaTime);

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
        _wanderTimer -= Time.deltaTime;
        _eatTimer -= Time.deltaTime;
        _reproTimer -= Time.deltaTime;
        _deadEndCooldown -= Time.deltaTime;
        _fleeTimer -= Time.deltaTime;
    }

    /// <summary>Evaluates perception and selects the highest-priority AI state.</summary>
    private void UpdateState()
    {
        bool camouflaged = false;
        if (_genetics != null && _genetics.HasCamouflage)
        {
            // Check for nearby objects that could provide cover for camouflage
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach (var c in nearby)
            {
                if (c.gameObject == gameObject) continue;
                if (UnityEngine.Random.value < GrazerGenetics.CamouflageChance) { camouflaged = true; break; }
            }
        }

        if (!camouflaged)
        {
            _nearestPredator = FindNearest("Predator", predatorDetectRadius);
            if (_nearestPredator != null)
            {
                _lastKnownPredatorPos = _nearestPredator.position;
                _fleeTimer = MinFleeDuration;
                _state = State.Flee;

                return;
            }
        }

        // Hysteresis: keep fleeing briefly after predator leaves detection range
        // to prevent rapid Flee/SeekPlant flickering at the boundary edge
        if (_fleeTimer > 0f)
        {
            _state = State.Flee;
            return;
        }

        _nearestPredator = null;

        if (Hunger < reproductionHungerThreshold)
        {
            if (_state == State.SeekPlant && _targetPlant != null
                && _targetPlant.gameObject.activeInHierarchy)
            {
                return;
            }

            _targetPlant = FindBestPlant();
            if (_targetPlant != null)
            {
                _state = State.SeekPlant;
                return;
            }
        }

        _state = State.Wander;
    }

    /// <summary>Dispatches movement execution to the active state handler.</summary>
    private void ExecuteState()
    {
        switch (_state)
        {
            case State.Flee: ExecuteFlee(); break;
            case State.SeekPlant: ExecuteSeekPlant(); break;
            case State.Wander: ExecuteWander(); break;
        }
        UpdateLabel();
        _cone?.UpdateCone(_rb.linearVelocity, _currentStateLabel);

        // Disable plant avoidance when seeking a plant so the grazer
        // doesn't get deflected away from the plant it's trying to eat
        if (_avoidance != null)
        {
            _avoidance.IgnorePlants = (_state == State.SeekPlant);
            _avoidance.AvoidCreatures = (_state == State.Wander);
        }
    }

    private string _currentStateLabel = StateLabel.Wander;

    /// <summary>Updates the state label text and caches the current label string.</summary>
    private void UpdateLabel()
    {
        if (_label == null) return;
        if (IsDead) { _label.SetState(StateLabel.Dead); return; }
        _currentStateLabel = _state switch
        {
            State.Flee => StateLabel.Flee,
            State.SeekPlant => _eatTimer > 0f ? StateLabel.Eat : StateLabel.Seek,
            _ => StateLabel.Wander,
        };
        _label.SetState(_currentStateLabel);
    }

    /// <summary>Moves away from the last known predator position at flee speed.</summary>
    private void ExecuteFlee()
    {
        // Use last known position so flee keeps working during the timer cooldown
        // even after the predator leaves detection range
        Vector2 from = _nearestPredator != null
                        ? (Vector2)_nearestPredator.position
                        : _lastKnownPredatorPos;
        Vector2 awayDir = ((Vector2)transform.position - from).normalized;
        Vector2 vel = awayDir * fleeSpeed;
        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
        {
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        }
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; PickNewWanderTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Moves toward the target plant and takes a bite on arrival.</summary>
    private void ExecuteSeekPlant()
    {
        if (_targetPlant == null) { _state = State.Wander; return; }

        Vector2 dir = (Vector2)_targetPlant.transform.position - (Vector2)transform.position;

        // Use collider separation, not transform distance: sprite centers stay
        // ~0.7–1.4+ units apart when boxes are flush/overlapping, so a small
        // eatDistance vs. center distance never triggers eating.
        if (IsWithinEatRange(_targetPlant))
        {
            TryEat(_targetPlant);
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 vel = dir.normalized * moveSpeed;
            if (_genetics != null) vel *= _genetics.SpeedMultiplier;
            if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
            {
                vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
            }
            if (_avoidance != null)
            {
                // Widen the avoidance fan while seeking to catch obstacle corners earlier
                _avoidance.FanAngleOverride = 160f;
                vel = _avoidance.GetAvoidanceVelocity(vel);
                _avoidance.FanAngleOverride = -1f;
                if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; _targetPlant = null; PickNewWanderTarget(); }
            }
            _rb.linearVelocity = vel;
        }
    }

    /// <summary>Moves toward the wander target with optional herd flocking offset.</summary>
    private void ExecuteWander()
    {
        Vector2 toTarget = _wanderTarget - (Vector2)transform.position;
        if (toTarget.magnitude < 0.2f || _wanderTimer <= 0f)
            PickNewWanderTarget();

        Vector2 vel = toTarget.normalized * (moveSpeed * 0.6f);

        if (_genetics != null) vel *= _genetics.SpeedMultiplier;
        if (_genetics != null && _genetics.IsReptile && EnvironmentHandler.Instance != null)
        {
            vel *= EnvironmentHandler.Instance.GetReptileSpeedMultiplier();
        }
        if (_avoidance != null)
        {
            vel = _avoidance.GetAvoidanceVelocity(vel);
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; PickNewWanderTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    /// <summary>Attempts to take one bite from a plant subject to attractiveness and cooldown.</summary>
    /// <param name="plant">The plant to attempt eating.</param>
    private void TryEat(Plant plant)
    {
        if (_eatTimer > 0f || plant == null) return;

        // Attractiveness check — bitter/tasty/poisonous affect willingness
        if (UnityEngine.Random.value > plant.EatAttractiveness) return;

        _eatTimer = eatCooldown;

        // Take one bite — plant shrinks and browns, returns nutrition for this bite
        float nutrition = plant.BeingEaten();
        Feed(nutrition);
        Heal(nutrition * 0.25f);

        // Apply poison on first bite
        if (plant.IsPoisonous && !_isPoisoned)
        {
            _isPoisoned = true;
            _poisonDamageRate = plant.PoisonDamagePerSec;
        }

        // Plant destroys itself on the last bite; null check clears our reference
        if (plant == null || !plant.gameObject.activeInHierarchy)
            _targetPlant = null;

    }

    /// <summary>Reflects a portion of incoming damage back to the attacker if Spiky is expressed.</summary>
    /// <param name="amount">Incoming damage amount.</param>
    public override void TakeDamage(float amount)
    {
        if (_genetics != null && _genetics.HasSpiky)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
            foreach (var h in hits)
            {
                if (!h.CompareTag("Predator")) continue;
                h.GetComponent<EntityBase>()?.TakeDamage(amount * GrazerGenetics.SpikyReflect);
                break;
            }
        }
        base.TakeDamage(amount);
    }

    /// <summary>Spawns a grazer offspring when hunger and cooldown conditions are met.</summary>
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

    /// <summary>Finds the genome of a nearby same-species creature to use as a reproduction mate.</summary>
    /// <param name="tag">Tag of the target species.</param>
    /// <returns>Genome of the nearest valid mate, or null if none found.</returns>
    private Genome FindMateGenome(string tag)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;

            // Walk to root to get the actual creature GameObject
            Transform root = h.transform;
            while (root.parent != null && !root.CompareTag(tag))
                root = root.parent;

            if (!root.CompareTag(tag)) continue;
            if (root == transform) continue;  // skip self

            GrazerGenetics g = root.GetComponent<GrazerGenetics>();
            if (g != null && g.Genome != null) return g.Genome;
        }
        return null;
    }

    /// <summary>Detects when the agent is stuck and picks a new goal to escape.</summary>
    private void CheckIfStuck()
    {
        if (_state == State.SeekPlant && _targetPlant != null)
        {
            float d = Mathf.Min(
                GetPlantColliderSeparation(_targetPlant),
                Vector2.Distance(transform.position, _targetPlant.transform.position));
            if (d <= Mathf.Max(eatDistance * 3f, 1.25f))
            {
                _stuckTimer = 0f;
                _lastPosition = transform.position;
                return;
            }
        }

        // Determine the current goal position based on state
        Vector2 goal;
        switch (_state)
        {
            case State.Flee:
                // When fleeing, goal is away from predator — use opposite direction
                Vector2 from = _nearestPredator != null
                    ? (Vector2)_nearestPredator.position
                    : _lastKnownPredatorPos;
                goal = (Vector2)transform.position + ((Vector2)transform.position - from).normalized * 5f;
                break;
            case State.SeekPlant:
                goal = _targetPlant != null ? (Vector2)_targetPlant.transform.position : _wanderTarget;
                break;
            default:
                goal = _wanderTarget;
                break;
        }

        float distToGoal = Vector2.Distance(transform.position, goal);
        float moved = Vector2.Distance(transform.position, _lastPosition);

        // Not stuck if: moving meaningfully AND making progress toward goal
        if (moved > StuckMinMove && distToGoal < Vector2.Distance(_lastPosition, goal) + 0.5f)
        {
            _stuckTimer = 0f;
            _lastPosition = transform.position;
            return;
        }

        _stuckTimer += Time.deltaTime;
        // SeekPlant uses a tighter threshold since corners trap grazers faster
        float stuckThreshold = (_state == State.SeekPlant) ? 1.2f : StuckThreshold;
        if (_stuckTimer >= stuckThreshold)
        {
            _stuckTimer = 0f;
            _deadEndCooldown = 0f;
            _rb.linearVelocity = Vector2.zero;

            // State-specific unstick behaviour
            if (_state == State.SeekPlant)
            {
                // Abandon this plant and wander to a new area
                _targetPlant = null;
                _state = State.Wander;
            }
            else if (_state == State.Flee)
            {
                // Pick a flee target in a random safe direction instead
                PickNewWanderTarget();
            }

            _wanderTimer = 0f;
            PickNewWanderTarget();
            _lastPosition = transform.position;
        }
    }

    /// <summary>Selects a new wander destination biased toward the map centre when near walls.</summary>
    private void PickNewWanderTarget()
    {
        _wanderTimer = wanderInterval;

        if (BoundaryManager.Instance == null)
        {
            _wanderTarget = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * wanderRadius;
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

        // edgeFactor: 0 = at centre, 1 = at wall
        float halfW = (b.MaxX - b.MinX) * 0.5f;
        float halfH = (b.MaxY - b.MinY) * 0.5f;
        float edgeX = Mathf.Clamp01(Mathf.Abs(pos.x - mapCenter.x) / halfW);
        float edgeY = Mathf.Clamp01(Mathf.Abs(pos.y - mapCenter.y) / halfH);
        float edgeFactor = Mathf.Max(edgeX, edgeY);

        Vector2 candidate;
        if (edgeFactor > 0.6f)
        {
            // Near a wall — pull strongly toward map centre
            Vector2 toCenter = (mapCenter - pos).normalized;
            float pullDist = Mathf.Lerp(wanderRadius, 6f, edgeFactor);
            candidate = pos + toCenter * pullDist + (Vector2)(UnityEngine.Random.insideUnitCircle * wanderRadius * 0.5f);
        }
        else
        {
            candidate = pos + UnityEngine.Random.insideUnitCircle.normalized * wanderRadius;
        }

        candidate.x = Mathf.Clamp(candidate.x, safeMinX, safeMaxX);
        candidate.y = Mathf.Clamp(candidate.y, safeMinY, safeMaxY);
        _wanderTarget = candidate;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// <summary>Closest distance between grazer and plant hulls; 0 if overlapping.</summary>
    private float GetPlantColliderSeparation(Plant plant)
    {
        if (plant == null) return float.MaxValue;
        Collider2D plantCol = plant.GetComponent<Collider2D>()
            ?? plant.GetComponentInChildren<Collider2D>();
        if (_selfCollider == null || plantCol == null)
            return Vector2.Distance(transform.position, plant.transform.position);

        ColliderDistance2D cd = Physics2D.Distance(_selfCollider, plantCol);
        if (cd.isOverlapped) return 0f;
        return cd.distance;
    }

    private bool IsWithinEatRange(Plant plant)
    {
        return GetPlantColliderSeparation(plant) <= eatDistance;
    }

    /// <summary>Finds the nearest non-trigger root transform with a given tag within a radius.</summary>
    /// <param name="tag">Tag to search for.</param>
    /// <param name="radius">Search radius in world units.</param>
    /// <returns>Transform of the nearest matching entity root, or null if none found.</returns>
    private Transform FindNearest(string tag, float radius)
    {
        // Apply visibility penalty to the search radius
        float currentVisionRadius = radius;
        if (EnvironmentHandler.Instance != null)
        {
            currentVisionRadius *= EnvironmentHandler.Instance.VisibilityMultiplier;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentVisionRadius);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var h in hits)
        {
            // Skip trigger colliders — those are detection zones, not physical bodies.
            // We only want to detect the actual root entity position.
            if (h.isTrigger) continue;
            Plant p = h.GetComponentInParent<Plant>();

            // Walk up to the root tagged object in case the collider is on a child
            Transform root = h.transform;
            while (root.parent != null && !root.CompareTag(tag))
                root = root.parent;

            if (!root.CompareTag(tag)) continue;

            // Measure distance to the root, not the collider
            float d = Vector2.Distance(transform.position, root.position);

            // Hard-check: ignore anything actually outside our radius
            if (d > radius) continue;

            if (d < minDist) { minDist = d; nearest = root; }
        }
        return nearest;
    }

    /// <summary>Finds the highest-scoring fully-grown plant within detection range.</summary>
    /// <returns>Best plant weighted by attractiveness over distance, or null if none found.</returns>
    private Plant FindBestPlant()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, plantDetectRadius);
        Plant best = null;
        float bestScore = -1f;
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            Plant p = h.GetComponentInParent<Plant>();
            if (p == null || !p.IsFullyGrown) continue;
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist > plantDetectRadius) continue;
            float score = p.EatAttractiveness / (dist + 0.1f);
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    /// <summary>Snaps the grazer back inside map bounds and picks a new inward target.</summary>
    private void ClampToBounds()
    {
        if (BoundaryManager.Instance == null) return;
        var b = BoundaryManager.Instance;
        Vector2 pos = transform.position;
        bool outOfBounds = pos.x < b.MinX || pos.x > b.MaxX
                        || pos.y < b.MinY || pos.y > b.MaxY;
        if (!outOfBounds) return;

        transform.position = b.Clamp(pos);
        _rb.linearVelocity = Vector2.zero;
        _wanderTimer = 0f;   // force immediate retarget toward centre
        PickNewWanderTarget();
    }
}
