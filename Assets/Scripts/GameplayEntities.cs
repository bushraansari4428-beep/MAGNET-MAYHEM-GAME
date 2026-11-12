using UnityEngine;

namespace MagnetMayhem
{
    public class MagnetBall : MonoBehaviour
    {
        public BallColorType ColorType { get; private set; }
        public BallSpecialType SpecialType { get; private set; }
        public BallState CurrentState { get; private set; }
        public TimingResult LaunchTiming { get; private set; }
        public bool SpawnedFromLeft { get; private set; }

        private Vector2 _velocity;
        private float _mass = 1.0f;
        private float _speedMultiplier = 1.0f;
        private float _shieldTimer = 0f;
        private float _spawnGraceTimer = 0f;

        private Vector2 _magnetPos;
        private Vector2 _correctTargetPos;
        private Vector2 _wrongTargetPos;
        private Vector2 _bombSafePos;
        private LevelConfig _levelConfig;
        private MagnetMayhemGame _game;

        private SpriteRenderer _sr;
        private TrailRenderer _trail;
        private Transform _visualBadge;

        public void Initialize(
            MagnetMayhemGame game,
            LevelConfig config,
            BallSpawnEntry entry,
            Vector2 spawnPos,
            Vector2 magnetPos,
            Vector2 blueBasketPos,
            Vector2 redBasketPos)
        {
            _game = game;
            _levelConfig = config;
            ColorType = entry.colorType;
            SpecialType = entry.specialType;
            SpawnedFromLeft = entry.spawnFromLeft;

            _magnetPos = magnetPos;
            _correctTargetPos = (ColorType == BallColorType.Blue) ? blueBasketPos : redBasketPos;
            _wrongTargetPos = (ColorType == BallColorType.Blue) ? redBasketPos : blueBasketPos;
            _bombSafePos = new Vector2(0f, -3.55f); // Center bottom safe defuse zone for Bomb balls

            transform.position = spawnPos;
            CurrentState = BallState.SPAWNING;
            LaunchTiming = TimingResult.None;
            _spawnGraceTimer = 0.50f;

            // Configure mass & speed per Special Ball Type
            _mass = 1.0f;
            _speedMultiplier = 1.0f;
            float scale = 0.85f;

            if (SpecialType == BallSpecialType.Heavy)
            {
                _mass = 1.45f;
                _speedMultiplier = 0.78f;
                scale = 1.05f;
            }
            else if (SpecialType == BallSpecialType.Light)
            {
                _mass = 0.72f;
                _speedMultiplier = 1.25f;
                scale = 0.72f;
            }
            else if (SpecialType == BallSpecialType.Golden)
            {
                _speedMultiplier = 1.08f;
                scale = 0.90f;
            }
            else if (SpecialType == BallSpecialType.Shield)
            {
                _shieldTimer = 0.35f;
            }

            transform.localScale = Vector3.one * scale;

            // Visual setup
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = NeonArtGenerator.GetBallSprite(ColorType, SpecialType);
            _sr.sortingOrder = 20;

            // Glowing Trail
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.28f;
            _trail.startWidth = 0.32f * scale;
            _trail.endWidth = 0.0f;
            _trail.material = new Material(Shader.Find("Sprites/Default"));
            Color c = (SpecialType == BallSpecialType.Bomb)
                ? new Color(1f, 0.4f, 0.1f)
                : LevelDatabase.GetBallColor(ColorType);
            _trail.startColor = new Color(c.r, c.g, c.b, 0.75f);
            _trail.endColor = new Color(c.r, c.g, c.b, 0.0f);
            _trail.sortingOrder = 18;

            // Initial approach velocity toward the magnetic field
            Vector2 dirToMagnet = (_magnetPos - (Vector2)transform.position).normalized;
            _velocity = dirToMagnet * (_levelConfig.baseBallSpeed * 0.75f * _speedMultiplier);
            CurrentState = BallState.APPROACHING;
        }

        public void TickPhysics(float dt, MagnetMode currentMagnetMode)
        {
            if (CurrentState == BallState.DELIVERED || CurrentState == BallState.FAILED)
                return;

            // Subtle spin animation
            transform.Rotate(0f, 0f, -240f * dt * (SpawnedFromLeft ? 1f : -1f));

            Vector2 pos = transform.position;
            Vector2 toMagnet = _magnetPos - pos;
            float distToMagnet = toMagnet.magnitude;

            if (_shieldTimer > 0f)
                _shieldTimer -= dt;

            if (_spawnGraceTimer > 0f)
                _spawnGraceTimer -= dt;

            if (CurrentState == BallState.APPROACHING ||
                CurrentState == BallState.ATTRACTED ||
                CurrentState == BallState.READY_TO_REPEL)
            {
                // Update state transitions based on distance to Magnet
                if (distToMagnet <= _levelConfig.goodWindowOuterRadius &&
                    distToMagnet >= _levelConfig.perfectWindowInnerRadius * 0.85f)
                {
                    CurrentState = BallState.READY_TO_REPEL;

                    // Level 1 Tutorial Auto-Assist Demo if player waits until dead-center of Perfect ring
                    if (_levelConfig.autoAssistTutorial && distToMagnet <= (_levelConfig.perfectWindowOuterRadius + _levelConfig.perfectWindowInnerRadius) * 0.5f)
                    {
                        _game.TriggerPlayerRepelTap();
                        return;
                    }
                }
                else
                {
                    CurrentState = BallState.ATTRACTED;
                }

                // Inverse-Square Attraction Physics: Force = MagnetStrength / Distance^2
                if (currentMagnetMode == MagnetMode.ATTRACT && _shieldTimer <= 0f)
                {
                    float clampedDist = Mathf.Max(distToMagnet, 0.85f);
                    float forceMag = (_levelConfig.magnetStrength / (clampedDist * clampedDist)) / _mass;
                    Vector2 attractAccel = toMagnet.normalized * forceMag;

                    _velocity += attractAccel * dt;
                    float maxSpeed = _levelConfig.baseBallSpeed * 1.45f * _speedMultiplier;
                    if (_velocity.magnitude > maxSpeed)
                        _velocity = _velocity.normalized * maxSpeed;

                    // Ensure deterministic inward pull toward magnet center
                    _velocity = Vector2.Lerp(_velocity.normalized, toMagnet.normalized, dt * 6.5f) * _velocity.magnitude;
                }

                pos += _velocity * dt;
                transform.position = pos;

                // Check collision with Magnet core (Too Late!)
                if (distToMagnet <= _levelConfig.magnetHitRadius)
                {
                    CurrentState = BallState.FAILED;
                    LaunchTiming = TimingResult.TooLate;
                    _game.OnBallFailed(this, "BALL HIT THE MAGNET! (TOO LATE)");
                    return;
                }
            }
            else if (CurrentState == BallState.REPELLING)
            {
                // Apply continuous outward magnetic repulsion force: Force = MagnetStrength / Distance^2
                Vector2 fromMagnet = pos - _magnetPos;
                float clampedDist = Mathf.Max(fromMagnet.magnitude, 0.90f);
                float repelForceMag = (_levelConfig.magnetStrength * 0.65f / (clampedDist * clampedDist)) / _mass;

                _velocity += _velocity.normalized * (repelForceMag * dt);
                float maxRepelSpeed = _levelConfig.baseBallSpeed * 2.55f * _speedMultiplier;
                if (_velocity.magnitude > maxRepelSpeed)
                    _velocity = _velocity.normalized * maxRepelSpeed;

                pos += _velocity * dt;
                transform.position = pos;

                CheckTargetsAndBounds(pos);
            }
        }

        public TimingResult ActivateRepel()
        {
            if (CurrentState == BallState.DELIVERED || CurrentState == BallState.FAILED)
                return TimingResult.None;

            // Grace period: ball was just spawned, ignore any premature tap
            if (_spawnGraceTimer > 0f)
                return TimingResult.None;

            float dist = Vector2.Distance(transform.position, _magnetPos);

            // If ball is still far away near the spawn launcher (> 1.25x good window radius), ignore tap completely
            if (dist > _levelConfig.goodWindowOuterRadius * 1.25f)
            {
                return TimingResult.None;
            }

            CurrentState = BallState.REPELLING;
            float launchSpeed = _levelConfig.baseBallSpeed * 2.15f * _speedMultiplier;

            // 1. Check if inside PERFECT timing window
            if (dist <= _levelConfig.perfectWindowOuterRadius && dist >= _levelConfig.perfectWindowInnerRadius)
            {
                LaunchTiming = TimingResult.Perfect;
                Vector2 target = (SpecialType == BallSpecialType.Bomb) ? _bombSafePos : _correctTargetPos;
                Vector2 idealDir = (target - (Vector2)transform.position).normalized;
                _velocity = idealDir * (launchSpeed * 1.12f);
                return LaunchTiming;
            }

            // 2. Check if inside GOOD timing window
            if (dist <= _levelConfig.goodWindowOuterRadius && dist >= _levelConfig.magnetHitRadius)
            {
                LaunchTiming = TimingResult.Good;
                Vector2 target = (SpecialType == BallSpecialType.Bomb) ? _bombSafePos : _correctTargetPos;
                Vector2 idealDir = (target - (Vector2)transform.position).normalized;
                _velocity = idealDir * launchSpeed;
                return LaunchTiming;
            }

            // 3. True "Too Early" tap (Player tapped when ball was nearing the ring, between 1.0x and 1.25x good radius)
            LaunchTiming = TimingResult.TooEarly;
            Vector2 awayFromMagnet = ((Vector2)transform.position - _magnetPos).normalized;
            // Deflect off-target toward upper corners so player clearly sees it repelled too early
            Vector2 earlyMissDir = (awayFromMagnet + new Vector2(SpawnedFromLeft ? -0.6f : 0.6f, 0.35f)).normalized;
            _velocity = earlyMissDir * launchSpeed;
            return LaunchTiming;
        }

        private void CheckTargetsAndBounds(Vector2 pos)
        {
            float catchRadius = 0.88f * _levelConfig.targetOpeningScale;

            // Bomb Ball special handling
            if (SpecialType == BallSpecialType.Bomb)
            {
                if (Vector2.Distance(pos, _bombSafePos) <= 0.90f)
                {
                    CurrentState = BallState.DELIVERED;
                    _game.OnBallDelivered(this, LaunchTiming == TimingResult.Perfect);
                    return;
                }
                if (Vector2.Distance(pos, _correctTargetPos) <= catchRadius ||
                    Vector2.Distance(pos, _wrongTargetPos) <= catchRadius)
                {
                    CurrentState = BallState.FAILED;
                    _game.OnBallFailed(this, "BOMB BALL DESTROYED A BASKET!");
                    return;
                }
            }
            else
            {
                // Correct Target Basket check
                if (Vector2.Distance(pos, _correctTargetPos) <= catchRadius)
                {
                    CurrentState = BallState.DELIVERED;
                    _game.OnBallDelivered(this, LaunchTiming == TimingResult.Perfect);
                    return;
                }

                // Wrong Target Basket check
                if (Vector2.Distance(pos, _wrongTargetPos) <= catchRadius)
                {
                    CurrentState = BallState.FAILED;
                    _game.OnBallFailed(this, "WRONG COLOR BASKET!");
                    return;
                }
            }

            // Out of playable area check
            if (Mathf.Abs(pos.x) > 3.8f || pos.y < -5.4f || pos.y > 5.8f)
            {
                CurrentState = BallState.FAILED;
                string reason = (LaunchTiming == TimingResult.TooEarly)
                    ? "TAPPED TOO EARLY — MISSED TARGET!"
                    : "BALL LEFT PLAY AREA!";
                _game.OnBallFailed(this, reason);
            }
        }
    }

    public class ObstacleController : MonoBehaviour
    {
        private bool _isMovingGate;
        private bool _isRotating;
        private float _speed;
        private Vector3 _startPos;
        private float _timeOffset;

        public void Setup(bool movingGate, bool rotating, float speed, Vector3 startPos)
        {
            _isMovingGate = movingGate;
            _isRotating = rotating;
            _speed = speed;
            _startPos = startPos;
            transform.position = startPos;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = NeonArtGenerator.GetObstacleBarSprite();
            sr.sortingOrder = 15;
            transform.localScale = new Vector3(0.72f, 0.55f, 1f);
        }

        public void TickObstacle(float dt, MagnetBall activeBall, MagnetMayhemGame game)
        {
            _timeOffset += dt;

            if (_isMovingGate)
            {
                // Oscillate horizontally leaving a clean gap when timed inside the ring
                float xOffset = Mathf.Sin(_timeOffset * _speed) * 1.15f;
                transform.position = _startPos + new Vector3(xOffset, 0f, 0f);
            }

            if (_isRotating)
            {
                transform.Rotate(0f, 0f, _speed * dt);
            }

            // Check collision with active repelling ball if timing was poor
            if (activeBall != null && activeBall.CurrentState == BallState.REPELLING)
            {
                // Fair rule: Perfect timing always curves cleanly past static/moving obstacles
                if (activeBall.LaunchTiming == TimingResult.TooEarly)
                {
                    float dist = Vector2.Distance(activeBall.transform.position, transform.position);
                    if (dist < 0.55f)
                    {
                        game.OnBallFailed(activeBall, "BLOCKED BY BARRIER!");
                    }
                }
            }
        }
    }
}
