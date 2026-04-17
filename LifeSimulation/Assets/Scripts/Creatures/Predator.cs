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
    [SerializeField] private float maxChaseDistance = 20f;  // give up if prey gets this far away

    [Header("Reproduction")]
    [SerializeField] private float reproductionHungerThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 30f;

    // Predator hunts when hunger drops below this — kept separate from
    // reproductionHungerThreshold so a single kill doesn't immediately stop the hunt
    [SerializeField] private float huntHungerThreshold = 60f;

    // ── State ─────────────────────────────────────────────────────────────────
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

    // ── Unity ─────────────────────────────────────────────────────────────────
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

    // ── Timers ────────────────────────────────────────────────────────────────
    private void UpdateTimers()
    {
        _patrolTimer -= Time.deltaTime;
        _attackTimer -= Time.deltaTime;
        _reproTimer -= Time.deltaTime;
        _deadEndCooldown -= Time.deltaTime;
    }

    // ── Perception ────────────────────────────────────────────────────────────
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

    // ── State Execution ───────────────────────────────────────────────────────
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

    private void UpdateLabel()
    {
        if (_label == null) return;
        if (IsDead) { _label.SetState(StateLabel.Dead); return; }
        _currentStateLabel = _state == State.Hunt ? StateLabel.Hunt : StateLabel.Patrol;
        _label.SetState(_currentStateLabel);
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
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; _prey = null; PickNewPatrolTarget(); }
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
            if (_avoidance.IsDeadEnd && _deadEndCooldown <= 0f) { _deadEndCooldown = DeadEndCooldownTime; PickNewPatrolTarget(); }
        }
        _rb.linearVelocity = vel;
    }

    // ── Attack on Contact ─────────────────────────────────────────────────────
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