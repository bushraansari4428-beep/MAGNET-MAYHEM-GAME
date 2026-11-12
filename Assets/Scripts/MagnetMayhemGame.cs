using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MagnetMayhem
{
    public class MagnetMayhemGame : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindObjectOfType<MagnetMayhemGame>() == null)
            {
                GameObject go = new GameObject("MagnetMayhem_MasterEngine");
                go.AddComponent<MagnetMayhemGame>();
            }
        }

        public GameState CurrentState { get; private set; } = GameState.MAIN_MENU;
        public MagnetMode CurrentMagnetMode { get; private set; } = MagnetMode.ATTRACT;

        // Player Persistence
        private int _unlockedLevel = 1;
        private int _currentLevelIndex = 1;
        private int _bestScore = 0;
        private int _coins = 0;
        private int _equippedSkinIndex = 0;

        // Current Level Runtime State
        private LevelConfig _activeLevel;
        private int _currentScore = 0;
        private int _levelStartScore = 0;
        private int _deliveredCount = 0;
        private int _spawnIndex = 0;
        private int _comboCount = 0;
        private MagnetBall _activeBall;
        private List<ObstacleController> _obstacles = new List<ObstacleController>();

        // World Positions (Portrait 9:16 Orthographic Size = 5.0)
        private readonly Vector2 _magnetPos = new Vector2(0f, 1.35f);
        private readonly Vector2 _blueBasketPos = new Vector2(-1.48f, -3.35f);
        private readonly Vector2 _redBasketPos = new Vector2(1.48f, -3.35f);
        private readonly Vector2 _leftSpawnPos = new Vector2(-2.95f, 2.55f);
        private readonly Vector2 _rightSpawnPos = new Vector2(2.95f, 2.55f);

        // World Visual Transforms & Renderers
        private Camera _mainCam;
        private Vector3 _camBasePos = new Vector3(0f, 0f, -10f);
        private float _shakeTimer = 0f;
        private float _shakeMagnitude = 0f;

        private Transform _worldRoot;
        private Transform _magnetTransform;
        private SpriteRenderer _magnetRenderer;
        private SpriteRenderer _magnetGlowRenderer;
        private SpriteRenderer _outerWindowRing;
        private SpriteRenderer _perfectWindowRing;
        private SpriteRenderer _pulseWaveRing;
        private float _pulseWaveProgress = 0f;

        private Transform _blueBasketTransform;
        private Transform _redBasketTransform;

        // UI Panels & HUD Elements
        private Font _uiFont;
        private Canvas _mainCanvas;
        private GameObject _mainMenuPanel;
        private GameObject _levelSelectPanel;
        private GameObject _shopPanel;
        private GameObject _settingsPanel;
        private GameObject _hudPanel;
        private GameObject _pausePanel;
        private GameObject _levelCompletePanel;
        private GameObject _gameOverPanel;

        // HUD References
        private Text _hudLevelText;
        private Text _hudScoreText;
        private Text _hudProgressText;
        private Image _hudProgressFill;
        private Text _hudModeBadgeText;
        private Image _hudModeBadgeBg;
        private Text _hudTutorialText;
        private GameObject _hudTutorialBanner;
        private Text _floatingFeedbackText;
        private float _feedbackTimer = 0f;

        // Menu Text References
        private Text _menuBestScoreText;
        private Text _menuCoinsText;
        private Text _shopCoinsText;
        private Text _winScoreText;
        private Text _winBonusText;
        private Text _failReasonText;
        private Text _failScoreText;
        private Text _failBestText;

        private Transform _levelGridContainer;
        private Transform _shopGridContainer;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            LoadPlayerProgress();
            SetupCameraAndAudio();
            BuildWorldArena();
            BuildCompleteUIDashboard();
            ShowState(GameState.MAIN_MENU);
        }

        private void LoadPlayerProgress()
        {
            _unlockedLevel = PlayerPrefs.GetInt("MM_UNLOCKED_LVL", 1);
            _bestScore = PlayerPrefs.GetInt("MM_BEST_SCORE", 0);
            _coins = PlayerPrefs.GetInt("MM_COINS", 150);
            _equippedSkinIndex = PlayerPrefs.GetInt("MM_SKIN", 0);
            PlayerPrefs.SetInt("MM_SKIN_OWNED_0", 1);
        }

        private void SavePlayerProgress()
        {
            if (_currentScore > _bestScore)
                _bestScore = _currentScore;

            PlayerPrefs.SetInt("MM_UNLOCKED_LVL", _unlockedLevel);
            PlayerPrefs.SetInt("MM_BEST_SCORE", _bestScore);
            PlayerPrefs.SetInt("MM_COINS", _coins);
            PlayerPrefs.SetInt("MM_SKIN", _equippedSkinIndex);
            PlayerPrefs.Save();
        }

        private void SetupCameraAndAudio()
        {
            _mainCam = Camera.main;
            if (_mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                _mainCam = camObj.AddComponent<Camera>();
            }
            _mainCam.orthographic = true;
            _mainCam.orthographicSize = 5.0f;
            _mainCam.backgroundColor = new Color(0.02f, 0.03f, 0.09f, 1f);
            _mainCam.transform.position = _camBasePos;

            if (FindObjectOfType<ProceduralAudioController>() == null)
            {
                GameObject audioObj = new GameObject("ProceduralAudioController");
                audioObj.AddComponent<ProceduralAudioController>();
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildWorldArena()
        {
            _worldRoot = new GameObject("WorldArena").transform;

            // 1. Cyber Grid Background
            GameObject bgObj = new GameObject("CyberBackground");
            bgObj.transform.SetParent(_worldRoot);
            bgObj.transform.position = Vector3.zero;
            bgObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = NeonArtGenerator.GetCyberGridBackground();
            bgSr.sortingOrder = -20;

            // 2. Central Magnet + Glow + Timing Rings
            GameObject magObj = new GameObject("CentralMagnet");
            magObj.transform.SetParent(_worldRoot);
            magObj.transform.position = _magnetPos;
            _magnetTransform = magObj.transform;

            _magnetRenderer = magObj.AddComponent<SpriteRenderer>();
            _magnetRenderer.sortingOrder = 10;

            GameObject glowObj = new GameObject("MagnetAura");
            glowObj.transform.SetParent(_magnetTransform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 2.4f;
            _magnetGlowRenderer = glowObj.AddComponent<SpriteRenderer>();
            _magnetGlowRenderer.sprite = NeonArtGenerator.GetSoftGlowDiscSprite();
            _magnetGlowRenderer.sortingOrder = 5;

            GameObject outerRingObj = new GameObject("OuterTimingRing");
            outerRingObj.transform.SetParent(_worldRoot);
            outerRingObj.transform.position = _magnetPos;
            _outerWindowRing = outerRingObj.AddComponent<SpriteRenderer>();
            _outerWindowRing.sprite = NeonArtGenerator.GetRingSprite(256, 0.035f);
            _outerWindowRing.sortingOrder = 6;

            GameObject perfRingObj = new GameObject("PerfectTimingRing");
            perfRingObj.transform.SetParent(_worldRoot);
            perfRingObj.transform.position = _magnetPos;
            _perfectWindowRing = perfRingObj.AddComponent<SpriteRenderer>();
            _perfectWindowRing.sprite = NeonArtGenerator.GetRingSprite(256, 0.055f);
            _perfectWindowRing.sortingOrder = 7;

            GameObject waveObj = new GameObject("MagneticWavePulse");
            waveObj.transform.SetParent(_worldRoot);
            waveObj.transform.position = _magnetPos;
            _pulseWaveRing = waveObj.AddComponent<SpriteRenderer>();
            _pulseWaveRing.sprite = NeonArtGenerator.GetRingSprite(256, 0.045f);
            _pulseWaveRing.sortingOrder = 8;

            // 3. Blue & Red Target Baskets
            _blueBasketTransform = CreateTargetBasket("BlueBasket", _blueBasketPos, LevelDatabase.GetBallColor(BallColorType.Blue));
            _redBasketTransform = CreateTargetBasket("RedBasket", _redBasketPos, LevelDatabase.GetBallColor(BallColorType.Red));

            ApplyMagnetSkinVisuals();
        }

        private Transform CreateTargetBasket(string name, Vector2 pos, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_worldRoot);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = NeonArtGenerator.GetBasketSprite(col);
            sr.sortingOrder = 12;

            // Subtle floor glow under basket
            GameObject glow = new GameObject("BasketGlow");
            glow.transform.SetParent(go.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(1.8f, 1.2f, 1f);
            SpriteRenderer gSr = glow.AddComponent<SpriteRenderer>();
            gSr.sprite = NeonArtGenerator.GetSoftGlowDiscSprite();
            gSr.color = new Color(col.r, col.g, col.b, 0.28f);
            gSr.sortingOrder = 11;

            return go.transform;
        }

        private void ApplyMagnetSkinVisuals()
        {
            MagnetSkinInfo skin = LevelDatabase.Skins[Mathf.Clamp(_equippedSkinIndex, 0, LevelDatabase.Skins.Count - 1)];
            bool isRepel = (CurrentMagnetMode == MagnetMode.REPEL);
            _magnetRenderer.sprite = NeonArtGenerator.GetHorseshoeMagnetSprite(skin, isRepel);

            Color activeCol = isRepel ? skin.repelColor : skin.attractColor;
            _magnetGlowRenderer.color = new Color(activeCol.r, activeCol.g, activeCol.b, 0.32f);
            _pulseWaveRing.color = new Color(activeCol.r, activeCol.g, activeCol.b, 0.55f);

            if (_hudModeBadgeText != null)
            {
                _hudModeBadgeText.text = isRepel ? "MODE: REPEL (OUTWARD)" : "MODE: ATTRACT (PULLING)";
                _hudModeBadgeBg.color = new Color(activeCol.r * 0.65f, activeCol.g * 0.65f, activeCol.b * 0.65f, 0.90f);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Idle floating magnet animation
            if (_magnetTransform != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 3.2f) * 0.06f;
                _magnetTransform.position = _magnetPos + new Vector2(0f, bob);
            }

            // Animated inward (ATTRACT) or outward (REPEL) magnetic energy waves
            AnimateMagneticFieldWaves(dt);

            // Screen shake decay
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                Vector2 offset = Random.insideUnitCircle * _shakeMagnitude;
                _mainCam.transform.position = _camBasePos + new Vector3(offset.x, offset.y, 0f);
                if (_shakeTimer <= 0f) _mainCam.transform.position = _camBasePos;
            }

            // Floating feedback fade
            if (_feedbackTimer > 0f && _floatingFeedbackText != null)
            {
                _feedbackTimer -= dt;
                Color c = _floatingFeedbackText.color;
                c.a = Mathf.Clamp01(_feedbackTimer * 2f);
                _floatingFeedbackText.color = c;
            }

            // Gameplay Active Loop
            if (CurrentState == GameState.ATTRACT_MODE || CurrentState == GameState.REPEL_MODE)
            {
                // Detect One-Touch Tap anywhere on playable area (ignoring top pause button)
                if (IsGameplayTapDetected())
                {
                    TriggerPlayerRepelTap();
                }

                if (_activeBall != null)
                {
                    _activeBall.TickPhysics(dt, CurrentMagnetMode);

                    // Highlight timing ring when ball enters launch zone
                    bool ready = (_activeBall.CurrentState == BallState.READY_TO_REPEL);
                    _perfectWindowRing.color = ready
                        ? new Color(0.15f, 1.0f, 0.55f, 0.85f)
                        : new Color(0.25f, 0.75f, 1.0f, 0.28f);

                    float dist = Vector2.Distance(_activeBall.transform.position, _magnetPos);
                    float prox01 = Mathf.Clamp01(1f - (dist / 4.2f));
                    ProceduralAudioController.Instance?.UpdateAttractHum(CurrentMagnetMode == MagnetMode.ATTRACT, prox01);
                }

                for (int i = 0; i < _obstacles.Count; i++)
                {
                    if (_obstacles[i] != null)
                        _obstacles[i].TickObstacle(dt, _activeBall, this);
                }
            }
            else
            {
                ProceduralAudioController.Instance?.UpdateAttractHum(false, 0f);
            }
        }

        private void AnimateMagneticFieldWaves(float dt)
        {
            if (_pulseWaveRing == null) return;

            _pulseWaveProgress += dt * 1.35f;
            if (_pulseWaveProgress > 1f) _pulseWaveProgress -= 1f;

            // In ATTRACT mode, waves move INWARD toward the magnet; in REPEL mode, waves expand OUTWARD
            float t = (CurrentMagnetMode == MagnetMode.ATTRACT) ? (1f - _pulseWaveProgress) : _pulseWaveProgress;
            float scale = Mathf.Lerp(0.45f, 2.35f, t);
            _pulseWaveRing.transform.localScale = new Vector3(scale, scale, 1f);

            Color c = _pulseWaveRing.color;
            c.a = Mathf.Sin(_pulseWaveProgress * Mathf.PI) * 0.55f;
            _pulseWaveRing.color = c;
        }

        private bool IsGameplayTapDetected()
        {
            bool pressed = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pressed = true;
                screenPos = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                pressed = true;
                screenPos = Input.mousePosition;
            }

            if (!pressed) return false;

            // Ignore taps on the top 11% of the screen where the Pause button sits
            if (screenPos.y > Screen.height * 0.89f)
                return false;

            return true;
        }

        public void TriggerPlayerRepelTap()
        {
            // Important Control Rule from GDD:
            // Only switch from ATTRACT -> REPEL while active ball is approaching.
            // Do not allow accidental second tap while current ball is completing its delivery.
            if (CurrentState != GameState.ATTRACT_MODE || _activeBall == null)
                return;

            CurrentMagnetMode = MagnetMode.REPEL;
            CurrentState = GameState.REPEL_MODE;
            ApplyMagnetSkinVisuals();

            TimingResult timing = _activeBall.ActivateRepel();
            bool isPerfect = (timing == TimingResult.Perfect);
            ProceduralAudioController.Instance?.PlayRepel(isPerfect);

            // Brief magnetic pulse scale animation
            StopCoroutine(nameof(PulseMagnetScaleRoutine));
            StartCoroutine(nameof(PulseMagnetScaleRoutine));

            if (timing == TimingResult.Perfect)
            {
                ShowFloatingFeedback("PERFECT TIMING! +20", new Color(0.2f, 1.0f, 0.55f));
                TriggerScreenShake(0.12f, 0.08f);
            }
            else if (timing == TimingResult.Good)
            {
                ShowFloatingFeedback("GOOD LAUNCH!", new Color(0.35f, 0.85f, 1.0f));
            }
            else if (timing == TimingResult.TooEarly)
            {
                ShowFloatingFeedback("TOO EARLY!", new Color(1.0f, 0.45f, 0.2f));
            }
        }

        private IEnumerator PulseMagnetScaleRoutine()
        {
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin((t / 0.18f) * Mathf.PI) * 0.22f;
                _magnetTransform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _magnetTransform.localScale = Vector3.one;
        }

        public void StartLevel(int levelNumber)
        {
            _currentLevelIndex = Mathf.Max(1, levelNumber);
            _activeLevel = LevelDatabase.GetLevel(_currentLevelIndex);

            ClearActiveEntities();

            _levelStartScore = _currentScore;
            _deliveredCount = 0;
            _spawnIndex = 0;
            _comboCount = 0;

            // Configure timing rings scale to match level radii
            float outerScale = _activeLevel.goodWindowOuterRadius * 0.90f;
            float perfScale = _activeLevel.perfectWindowOuterRadius * 0.90f;
            _outerWindowRing.transform.localScale = new Vector3(outerScale, outerScale, 1f);
            _outerWindowRing.color = new Color(0.25f, 0.65f, 1.0f, 0.22f);
            _perfectWindowRing.transform.localScale = new Vector3(perfScale, perfScale, 1f);

            // Configure target baskets (Level 1-3 only require Blue basket, Level 4+ shows both)
            _blueBasketTransform.localScale = new Vector3(_activeLevel.targetOpeningScale, 1f, 1f);
            _redBasketTransform.localScale = new Vector3(_activeLevel.targetOpeningScale, 1f, 1f);
            _redBasketTransform.gameObject.SetActive(_currentLevelIndex >= 4);

            // Spawn World 3/4 obstacles if enabled
            SpawnLevelObstacles();

            CurrentMagnetMode = MagnetMode.ATTRACT;
            ApplyMagnetSkinVisuals();

            ShowState(GameState.LEVEL_START);
            UpdateHudUI();

            // Display tutorial banner if present
            if (!string.IsNullOrEmpty(_activeLevel.tutorialText))
            {
                _hudTutorialBanner.SetActive(true);
                _hudTutorialText.text = _activeLevel.tutorialText;
            }
            else
            {
                _hudTutorialBanner.SetActive(false);
            }

            SpawnNextBall();
        }

        private void SpawnLevelObstacles()
        {
            if (_activeLevel.hasStaticBarrier)
            {
                CreateObstacle(false, false, 0f, new Vector3(-0.75f, -1.05f, 0f));
                CreateObstacle(false, false, 0f, new Vector3(0.75f, -1.05f, 0f));
            }
            if (_activeLevel.hasMovingGate)
            {
                CreateObstacle(true, false, _activeLevel.obstacleSpeed, new Vector3(0f, -1.15f, 0f));
            }
            if (_activeLevel.hasRotatingBarrier)
            {
                CreateObstacle(false, true, _activeLevel.obstacleSpeed, new Vector3(0f, -0.95f, 0f));
            }
        }

        private void CreateObstacle(bool moving, bool rotating, float speed, Vector3 pos)
        {
            GameObject go = new GameObject("Obstacle");
            go.transform.SetParent(_worldRoot);
            ObstacleController oc = go.AddComponent<ObstacleController>();
            oc.Setup(moving, rotating, speed, pos);
            _obstacles.Add(oc);
        }

        private void SpawnNextBall()
        {
            if (_activeBall != null)
                Destroy(_activeBall.gameObject);

            if (_spawnIndex >= _activeLevel.ballSequence.Count)
                return;

            BallSpawnEntry entry = _activeLevel.ballSequence[_spawnIndex];
            Vector2 spawnPos = entry.spawnFromLeft ? _leftSpawnPos : _rightSpawnPos;

            GameObject ballObj = new GameObject($"Ball_{_spawnIndex + 1}_{entry.colorType}");
            ballObj.transform.SetParent(_worldRoot);
            _activeBall = ballObj.AddComponent<MagnetBall>();
            _activeBall.Initialize(this, _activeLevel, entry, spawnPos, _magnetPos, _blueBasketPos, _redBasketPos);

            CurrentMagnetMode = MagnetMode.ATTRACT;
            CurrentState = GameState.ATTRACT_MODE;
            ApplyMagnetSkinVisuals();
            UpdateHudUI();
        }

        public void OnBallDelivered(MagnetBall ball, bool isPerfect)
        {
            if (CurrentState == GameState.LEVEL_COMPLETE || CurrentState == GameState.LEVEL_FAILED)
                return;

            CurrentState = GameState.BALL_DELIVERED;
            _deliveredCount++;
            _spawnIndex++;
            _comboCount++;

            // Scoring Calculation per GDD Section 12
            int points = isPerfect ? 20 : 10;
            if (ball.SpecialType == BallSpecialType.Golden)
            {
                points += 50;
                _coins += 15;
            }
            else if (ball.SpecialType == BallSpecialType.Heavy)
            {
                points += 15;
            }

            string comboMsg = "";
            if (_comboCount == 3)
            {
                points += 15;
                comboMsg = "  COMBO x3! (+15)";
            }
            else if (_comboCount >= 5)
            {
                points += 30;
                comboMsg = $"  COMBO x{_comboCount}! (+30)";
            }
            else if (_comboCount > 1)
            {
                comboMsg = $"  COMBO x{_comboCount}!";
            }

            _currentScore += points;
            ProceduralAudioController.Instance?.PlayDelivery(_comboCount);

            // Basket bounce & burst visual effect
            Transform targetBasket = (ball.ColorType == BallColorType.Blue) ? _blueBasketTransform : _redBasketTransform;
            StartCoroutine(BounceTransformRoutine(targetBasket));
            SpawnDeliveryParticles(ball.transform.position, LevelDatabase.GetBallColor(ball.ColorType));

            ShowFloatingFeedback($"+{points} PTS{comboMsg}", new Color(0.25f, 1.0f, 0.65f));

            Destroy(ball.gameObject);
            _activeBall = null;
            UpdateHudUI();

            if (_deliveredCount >= _activeLevel.requiredDeliveries)
            {
                StartCoroutine(CompleteLevelSequence());
            }
            else
            {
                StartCoroutine(DelayedNextBallRoutine(0.42f));
            }
        }

        private IEnumerator DelayedNextBallRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (CurrentState == GameState.BALL_DELIVERED)
                SpawnNextBall();
        }

        private IEnumerator CompleteLevelSequence()
        {
            yield return new WaitForSeconds(0.35f);

            int levelBonus = 25;
            _currentScore += levelBonus;
            _coins += 20;

            if (_currentLevelIndex >= _unlockedLevel)
                _unlockedLevel = _currentLevelIndex + 1;

            // Level 5 unlocks Neon Skin automatically as GDD reward
            if (_currentLevelIndex == 5)
                PlayerPrefs.SetInt("MM_SKIN_OWNED_1", 1);

            SavePlayerProgress();
            ProceduralAudioController.Instance?.PlayLevelComplete();

            _winScoreText.text = $"SCORE: {_currentScore}";
            _winBonusText.text = $"LEVEL BONUS: +{levelBonus} PTS   |   +20 COINS";
            ShowState(GameState.LEVEL_COMPLETE);
        }

        public void OnBallFailed(MagnetBall ball, string reason)
        {
            if (CurrentState == GameState.LEVEL_FAILED || CurrentState == GameState.LEVEL_COMPLETE)
                return;

            CurrentState = GameState.LEVEL_FAILED;
            _comboCount = 0;

            ProceduralAudioController.Instance?.PlayFail();
            TriggerScreenShake(0.28f, 0.18f);

            if (ball != null)
            {
                SpawnDeliveryParticles(ball.transform.position, new Color(1f, 0.25f, 0.2f));
                Destroy(ball.gameObject);
                _activeBall = null;
            }

            SavePlayerProgress();

            _failReasonText.text = reason;
            _failScoreText.text = $"SCORE: {_currentScore}   |   LEVEL {_currentLevelIndex}";
            _failBestText.text = $"BEST SCORE: {_bestScore}";

            StartCoroutine(ShowGameOverDelayed(0.45f));
        }

        private IEnumerator ShowGameOverDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowState(GameState.LEVEL_FAILED);
        }

        private IEnumerator BounceTransformRoutine(Transform target)
        {
            if (target == null) yield break;
            Vector3 baseScale = new Vector3(_activeLevel.targetOpeningScale, 1f, 1f);
            float t = 0f;
            while (t < 0.20f)
            {
                t += Time.deltaTime;
                float bump = 1f + Mathf.Sin((t / 0.20f) * Mathf.PI) * 0.24f;
                target.localScale = baseScale * bump;
                yield return null;
            }
            target.localScale = baseScale;
        }

        private void SpawnDeliveryParticles(Vector3 pos, Color color)
        {
            for (int i = 0; i < 14; i++)
            {
                GameObject p = new GameObject("Spark");
                p.transform.SetParent(_worldRoot);
                p.transform.position = pos;
                p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.24f);
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = NeonArtGenerator.GetSoftGlowDiscSprite(64);
                sr.color = color;
                sr.sortingOrder = 25;

                Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(1.8f, 4.5f);
                StartCoroutine(AnimateParticleRoutine(p.transform, sr, vel));
            }
        }

        private IEnumerator AnimateParticleRoutine(Transform tr, SpriteRenderer sr, Vector2 vel)
        {
            float life = 0.42f;
            float t = 0f;
            Color startCol = sr.color;
            while (t < life && tr != null)
            {
                t += Time.deltaTime;
                tr.position += (Vector3)(vel * Time.deltaTime);
                vel *= 0.94f;
                float a = 1f - (t / life);
                sr.color = new Color(startCol.r, startCol.g, startCol.b, a);
                yield return null;
            }
            if (tr != null) Destroy(tr.gameObject);
        }

        private void TriggerScreenShake(float duration, float magnitude)
        {
            _shakeTimer = duration;
            _shakeMagnitude = magnitude;
        }

        private void ShowFloatingFeedback(string message, Color col)
        {
            if (_floatingFeedbackText == null) return;
            _floatingFeedbackText.text = message;
            _floatingFeedbackText.color = col;
            _feedbackTimer = 1.15f;
        }

        private void ClearActiveEntities()
        {
            if (_activeBall != null)
            {
                Destroy(_activeBall.gameObject);
                _activeBall = null;
            }
            for (int i = 0; i < _obstacles.Count; i++)
            {
                if (_obstacles[i] != null)
                    Destroy(_obstacles[i].gameObject);
            }
            _obstacles.Clear();
        }

        private void UpdateHudUI()
        {
            if (_activeLevel == null) return;
            _hudLevelText.text = $"LEVEL {_activeLevel.levelNumber}\n<size=20>{_activeLevel.levelTitle}</size>";
            _hudScoreText.text = $"SCORE\n{_currentScore}";
            _hudProgressText.text = $"BALLS DELIVERED: {_deliveredCount} / {_activeLevel.requiredDeliveries}";

            float progress = (float)_deliveredCount / Mathf.Max(1, _activeLevel.requiredDeliveries);
            _hudProgressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        public void ShowState(GameState newState)
        {
            CurrentState = newState;

            _mainMenuPanel.SetActive(newState == GameState.MAIN_MENU);
            _levelSelectPanel.SetActive(newState == GameState.LEVEL_SELECT);
            _shopPanel.SetActive(false);
            _settingsPanel.SetActive(false);

            bool inGameplay = (newState == GameState.LEVEL_START ||
                               newState == GameState.ATTRACT_MODE ||
                               newState == GameState.REPEL_MODE ||
                               newState == GameState.BALL_DELIVERED ||
                               newState == GameState.PAUSED ||
                               newState == GameState.LEVEL_COMPLETE ||
                               newState == GameState.LEVEL_FAILED);

            _hudPanel.SetActive(inGameplay);
            _pausePanel.SetActive(newState == GameState.PAUSED);
            _levelCompletePanel.SetActive(newState == GameState.LEVEL_COMPLETE);
            _gameOverPanel.SetActive(newState == GameState.LEVEL_FAILED);

            Time.timeScale = (newState == GameState.PAUSED) ? 0f : 1f;

            if (newState == GameState.MAIN_MENU)
            {
                ClearActiveEntities();
                _menuBestScoreText.text = $"BEST SCORE: {_bestScore}";
                _menuCoinsText.text = $"COINS: {_coins}";
            }
            else if (newState == GameState.LEVEL_SELECT)
            {
                RefreshLevelSelectGrid();
            }
        }

        // =====================================================================
        // MODERN NEON DASHBOARD UI BUILDER (1080 x 1920 PORTRAIT)
        // =====================================================================
        private void BuildCompleteUIDashboard()
        {
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null) _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasObj = new GameObject("MagnetMayhem_UICanvas");
            _mainCanvas = canvasObj.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            BuildMainMenuPanel(canvasObj.transform);
            BuildLevelSelectPanel(canvasObj.transform);
            BuildShopPanel(canvasObj.transform);
            BuildSettingsPanel(canvasObj.transform);
            BuildGameplayHudPanel(canvasObj.transform);
            BuildPauseOverlay(canvasObj.transform);
            BuildLevelCompleteOverlay(canvasObj.transform);
            BuildGameOverOverlay(canvasObj.transform);
        }

        private void BuildMainMenuPanel(Transform root)
        {
            _mainMenuPanel = CreatePanel(root, "MainMenuPanel", new Color(0.02f, 0.03f, 0.10f, 0.78f));

            // Header Badge
            CreateLabel(_mainMenuPanel.transform, "ARCADE EDITION", 26, new Color(0.25f, 0.85f, 1f), new Vector2(0, 720), new Vector2(600, 50));

            // Glowing Game Logo
            Text logo1 = CreateLabel(_mainMenuPanel.transform, "MAGNET", 112, new Color(0.15f, 0.78f, 1f), new Vector2(0, 610), new Vector2(950, 140));
            AddGlowOutline(logo1.gameObject, new Color(0f, 0.35f, 0.85f, 0.85f), 4f);

            Text logo2 = CreateLabel(_mainMenuPanel.transform, "MAYHEM", 112, new Color(1.0f, 0.25f, 0.38f), new Vector2(0, 495), new Vector2(950, 140));
            AddGlowOutline(logo2.gameObject, new Color(0.75f, 0.05f, 0.20f, 0.85f), 4f);

            // Stats Card
            GameObject statsCard = CreateCard(_mainMenuPanel.transform, new Vector2(0, 320), new Vector2(760, 130), new Color(0.06f, 0.10f, 0.22f, 0.92f));
            _menuBestScoreText = CreateLabel(statsCard.transform, "BEST SCORE: 0", 36, Color.white, new Vector2(-170, 0), new Vector2(380, 70));
            _menuCoinsText = CreateLabel(statsCard.transform, "COINS: 150", 36, new Color(1f, 0.85f, 0.2f), new Vector2(190, 0), new Vector2(340, 70));

            // Prominent PLAY Button
            CreateNeonButton(_mainMenuPanel.transform, "PLAY NOW", new Vector2(0, 60), new Vector2(680, 150),
                new Color(0.10f, 0.72f, 1.0f), 52, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _currentScore = 0;
                    StartLevel(_unlockedLevel);
                });

            // LEVEL SELECT Button
            CreateNeonButton(_mainMenuPanel.transform, "LEVEL SELECT", new Vector2(0, -135), new Vector2(680, 125),
                new Color(0.22f, 0.38f, 0.78f), 40, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(GameState.LEVEL_SELECT);
                });

            // MAGNET SHOP Button
            CreateNeonButton(_mainMenuPanel.transform, "MAGNET SHOP", new Vector2(0, -305), new Vector2(680, 125),
                new Color(0.65f, 0.18f, 0.85f), 40, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    OpenShopPanel();
                });

            // SETTINGS Button
            CreateNeonButton(_mainMenuPanel.transform, "SETTINGS & AUDIO", new Vector2(0, -475), new Vector2(680, 125),
                new Color(0.18f, 0.24f, 0.38f), 38, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _settingsPanel.SetActive(true);
                });
        }

        private void BuildLevelSelectPanel(Transform root)
        {
            _levelSelectPanel = CreatePanel(root, "LevelSelectPanel", new Color(0.02f, 0.04f, 0.12f, 0.96f));
            CreateLabel(_levelSelectPanel.transform, "SELECT LEVEL", 68, new Color(0.2f, 0.85f, 1f), new Vector2(0, 760), new Vector2(800, 100));
            CreateLabel(_levelSelectPanel.transform, "WORLD 1 TO WORLD 4 PROGRESSION", 28, new Color(0.7f, 0.8f, 0.95f), new Vector2(0, 685), new Vector2(800, 50));

            GameObject gridObj = new GameObject("LevelGrid");
            gridObj.transform.SetParent(_levelSelectPanel.transform, false);
            RectTransform rt = gridObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(920, 1220);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(195, 195);
            grid.spacing = new Vector2(32, 32);
            grid.childAlignment = TextAnchor.MiddleCenter;
            _levelGridContainer = gridObj.transform;

            CreateNeonButton(_levelSelectPanel.transform, "BACK TO MENU", new Vector2(0, -760), new Vector2(520, 115),
                new Color(0.25f, 0.32f, 0.48f), 38, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(GameState.MAIN_MENU);
                });
        }

        private void RefreshLevelSelectGrid()
        {
            foreach (Transform child in _levelGridContainer)
                Destroy(child.gameObject);

            for (int i = 1; i <= 20; i++)
            {
                int lvl = i;
                bool isCompleted = (lvl < _unlockedLevel);
                bool isCurrent = (lvl == _unlockedLevel);
                bool isLocked = (lvl > _unlockedLevel);

                Color btnCol = isCompleted
                    ? new Color(0.12f, 0.72f, 0.42f) // Green check
                    : (isCurrent ? new Color(0.12f, 0.68f, 1.0f) : new Color(0.18f, 0.20f, 0.28f)); // Bright blue or Grey lock

                string statusLabel = isCompleted ? $"LVL {lvl}\n✓ DONE" : (isCurrent ? $"LVL {lvl}\n★ PLAY" : $"LVL {lvl}\nLOCKED");

                Button b = CreateNeonButton(_levelGridContainer, statusLabel, Vector2.zero, new Vector2(195, 195), btnCol, 32, () =>
                {
                    if (!isLocked)
                    {
                        ProceduralAudioController.Instance?.PlayClick();
                        _currentScore = 0;
                        StartLevel(lvl);
                    }
                });
                b.interactable = !isLocked;
            }
        }

        private void BuildShopPanel(Transform root)
        {
            _shopPanel = CreatePanel(root, "MagnetShopPanel", new Color(0.03f, 0.04f, 0.14f, 0.97f));
            CreateLabel(_shopPanel.transform, "MAGNET COSMETIC SHOP", 60, new Color(1f, 0.82f, 0.2f), new Vector2(0, 760), new Vector2(900, 90));
            _shopCoinsText = CreateLabel(_shopPanel.transform, "COINS: 150", 40, Color.white, new Vector2(0, 675), new Vector2(600, 60));

            GameObject gridObj = new GameObject("ShopGrid");
            gridObj.transform.SetParent(_shopPanel.transform, false);
            RectTransform rt = gridObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(940, 1180);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(430, 340);
            grid.spacing = new Vector2(36, 36);
            grid.childAlignment = TextAnchor.MiddleCenter;
            _shopGridContainer = gridObj.transform;

            CreateNeonButton(_shopPanel.transform, "CLOSE SHOP", new Vector2(0, -760), new Vector2(520, 115),
                new Color(0.25f, 0.32f, 0.48f), 38, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _shopPanel.SetActive(false);
                    _menuCoinsText.text = $"COINS: {_coins}";
                });
        }

        private void OpenShopPanel()
        {
            _shopPanel.SetActive(true);
            _shopCoinsText.text = $"YOUR COINS: {_coins}";

            foreach (Transform child in _shopGridContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < LevelDatabase.Skins.Count; i++)
            {
                int idx = i;
                MagnetSkinInfo skin = LevelDatabase.Skins[idx];
                bool owned = PlayerPrefs.GetInt($"MM_SKIN_OWNED_{idx}", idx == 0 ? 1 : 0) == 1;
                bool equipped = (_equippedSkinIndex == idx);

                Color cardCol = equipped
                    ? new Color(0.12f, 0.65f, 0.45f)
                    : (owned ? new Color(0.15f, 0.45f, 0.82f) : new Color(0.22f, 0.24f, 0.36f));

                string actionText = equipped ? "EQUIPPED ✓" : (owned ? "TAP TO EQUIP" : $"UNLOCK ({skin.coinCost} COINS)");
                string fullLabel = $"{skin.displayName}\n\n{actionText}";

                CreateNeonButton(_shopGridContainer, fullLabel, Vector2.zero, new Vector2(430, 340), cardCol, 30, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    if (owned)
                    {
                        _equippedSkinIndex = idx;
                        SavePlayerProgress();
                        ApplyMagnetSkinVisuals();
                        OpenShopPanel();
                    }
                    else if (_coins >= skin.coinCost)
                    {
                        _coins -= skin.coinCost;
                        PlayerPrefs.SetInt($"MM_SKIN_OWNED_{idx}", 1);
                        _equippedSkinIndex = idx;
                        SavePlayerProgress();
                        ApplyMagnetSkinVisuals();
                        OpenShopPanel();
                    }
                });
            }
        }

        private void BuildSettingsPanel(Transform root)
        {
            _settingsPanel = CreatePanel(root, "SettingsPanel", new Color(0.02f, 0.03f, 0.10f, 0.95f));
            GameObject card = CreateCard(_settingsPanel.transform, Vector2.zero, new Vector2(820, 880), new Color(0.07f, 0.11f, 0.24f, 0.98f));

            CreateLabel(card.transform, "SETTINGS", 58, new Color(0.2f, 0.85f, 1f), new Vector2(0, 310), new Vector2(600, 80));

            Text sfxLabel = null;
            Button sfxBtn = CreateNeonButton(card.transform, "SOUND EFFECTS: ON", new Vector2(0, 120), new Vector2(620, 120),
                new Color(0.15f, 0.60f, 0.85f), 36, () =>
                {
                    bool next = !(ProceduralAudioController.Instance?.SfxEnabled ?? true);
                    ProceduralAudioController.Instance?.SetSfxEnabled(next);
                    ProceduralAudioController.Instance?.PlayClick();
                    if (sfxLabel != null) sfxLabel.text = next ? "SOUND EFFECTS: ON" : "SOUND EFFECTS: OFF";
                });
            sfxLabel = sfxBtn.GetComponentInChildren<Text>();

            Text musicLabel = null;
            Button musicBtn = CreateNeonButton(card.transform, "SYNTH MUSIC: ON", new Vector2(0, -40), new Vector2(620, 120),
                new Color(0.45f, 0.25f, 0.82f), 36, () =>
                {
                    bool next = !(ProceduralAudioController.Instance?.MusicEnabled ?? true);
                    ProceduralAudioController.Instance?.SetMusicEnabled(next);
                    ProceduralAudioController.Instance?.PlayClick();
                    if (musicLabel != null) musicLabel.text = next ? "SYNTH MUSIC: ON" : "SYNTH MUSIC: OFF";
                });
            musicLabel = musicBtn.GetComponentInChildren<Text>();

            CreateNeonButton(card.transform, "DONE", new Vector2(0, -260), new Vector2(440, 110),
                new Color(0.22f, 0.30f, 0.45f), 36, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _settingsPanel.SetActive(false);
                });
        }

        private void BuildGameplayHudPanel(Transform root)
        {
            _hudPanel = CreatePanel(root, "GameplayHUD", Color.clear);

            // Top Dashboard Bar
            GameObject topBar = CreateCard(_hudPanel.transform, new Vector2(0, 855), new Vector2(1020, 170), new Color(0.05f, 0.08f, 0.20f, 0.88f));

            _hudLevelText = CreateLabel(topBar.transform, "LEVEL 1", 34, Color.white, new Vector2(-310, 0), new Vector2(340, 120));
            _hudScoreText = CreateLabel(topBar.transform, "SCORE\n0", 36, new Color(0.2f, 0.9f, 1f), new Vector2(60, 0), new Vector2(280, 120));

            CreateNeonButton(topBar.transform, "II", new Vector2(410, 0), new Vector2(115, 115),
                new Color(0.22f, 0.32f, 0.52f), 40, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    if (CurrentState == GameState.ATTRACT_MODE || CurrentState == GameState.REPEL_MODE)
                        ShowState(GameState.PAUSED);
                });

            // Progress Bar Section ("BALLS DELIVERED: 3 / 5")
            GameObject progBg = CreateCard(_hudPanel.transform, new Vector2(0, 720), new Vector2(720, 56), new Color(0.08f, 0.12f, 0.26f, 0.92f));
            GameObject progFillObj = new GameObject("ProgressFill");
            progFillObj.transform.SetParent(progBg.transform, false);
            _hudProgressFill = progFillObj.AddComponent<Image>();
            _hudProgressFill.color = new Color(0.12f, 0.82f, 0.55f, 0.85f);
            RectTransform fillRt = _hudProgressFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            _hudProgressText = CreateLabel(progBg.transform, "BALLS DELIVERED: 0 / 1", 26, Color.white, Vector2.zero, new Vector2(700, 50));

            // Prominent Mode Indicator Badge ("ATTRACT" / "REPEL")
            GameObject modeBadge = CreateCard(_hudPanel.transform, new Vector2(0, 625), new Vector2(540, 76), new Color(0.10f, 0.55f, 0.85f, 0.90f));
            _hudModeBadgeBg = modeBadge.GetComponent<Image>();
            _hudModeBadgeText = CreateLabel(modeBadge.transform, "MODE: ATTRACT", 32, Color.white, Vector2.zero, new Vector2(520, 70));

            // Tutorial Overlay Banner
            _hudTutorialBanner = CreateCard(_hudPanel.transform, new Vector2(0, -120), new Vector2(920, 145), new Color(0.04f, 0.08f, 0.22f, 0.88f));
            _hudTutorialText = CreateLabel(_hudTutorialBanner.transform, "THE MAGNET PULLS THE BALL", 30, new Color(1f, 0.92f, 0.35f), Vector2.zero, new Vector2(880, 125));

            // Floating Combo / Perfect Feedback Label
            _floatingFeedbackText = CreateLabel(_hudPanel.transform, "", 46, new Color(0.2f, 1f, 0.6f, 0f), new Vector2(0, 25), new Vector2(900, 90));
            AddGlowOutline(_floatingFeedbackText.gameObject, Color.black, 3f);
        }

        private void BuildPauseOverlay(Transform root)
        {
            _pausePanel = CreatePanel(root, "PauseOverlay", new Color(0.01f, 0.02f, 0.08f, 0.88f));
            GameObject card = CreateCard(_pausePanel.transform, Vector2.zero, new Vector2(780, 760), new Color(0.06f, 0.10f, 0.24f, 0.98f));

            CreateLabel(card.transform, "GAME PAUSED", 60, Color.white, new Vector2(0, 230), new Vector2(650, 90));

            CreateNeonButton(card.transform, "RESUME", new Vector2(0, 50), new Vector2(560, 125),
                new Color(0.12f, 0.72f, 0.52f), 42, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(CurrentMagnetMode == MagnetMode.ATTRACT ? GameState.ATTRACT_MODE : GameState.REPEL_MODE);
                });

            CreateNeonButton(card.transform, "RESTART LEVEL", new Vector2(0, -105), new Vector2(560, 115),
                new Color(0.18f, 0.52f, 0.88f), 38, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _currentScore = _levelStartScore;
                    StartLevel(_currentLevelIndex);
                });

            CreateNeonButton(card.transform, "MAIN MENU", new Vector2(0, -255), new Vector2(560, 115),
                new Color(0.32f, 0.25f, 0.45f), 38, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(GameState.MAIN_MENU);
                });
        }

        private void BuildLevelCompleteOverlay(Transform root)
        {
            _levelCompletePanel = CreatePanel(root, "LevelCompleteOverlay", new Color(0.02f, 0.04f, 0.12f, 0.90f));
            GameObject card = CreateCard(_levelCompletePanel.transform, Vector2.zero, new Vector2(840, 860), new Color(0.06f, 0.12f, 0.26f, 0.98f));

            CreateLabel(card.transform, "LEVEL COMPLETE!", 64, new Color(0.2f, 1.0f, 0.58f), new Vector2(0, 270), new Vector2(760, 90));
            _winScoreText = CreateLabel(card.transform, "SCORE: 100", 52, Color.white, new Vector2(0, 135), new Vector2(700, 80));
            _winBonusText = CreateLabel(card.transform, "LEVEL BONUS: +25 PTS", 32, new Color(1f, 0.85f, 0.25f), new Vector2(0, 45), new Vector2(740, 60));

            CreateNeonButton(card.transform, "NEXT LEVEL", new Vector2(0, -115), new Vector2(600, 135),
                new Color(0.12f, 0.78f, 0.48f), 46, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    StartLevel(_currentLevelIndex + 1);
                });

            CreateNeonButton(card.transform, "MAIN MENU", new Vector2(0, -280), new Vector2(520, 110),
                new Color(0.24f, 0.32f, 0.48f), 36, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(GameState.MAIN_MENU);
                });
        }

        private void BuildGameOverOverlay(Transform root)
        {
            _gameOverPanel = CreatePanel(root, "GameOverOverlay", new Color(0.05f, 0.01f, 0.06f, 0.92f));
            GameObject card = CreateCard(_gameOverPanel.transform, Vector2.zero, new Vector2(840, 900), new Color(0.12f, 0.06f, 0.16f, 0.98f));

            CreateLabel(card.transform, "GAME OVER", 68, new Color(1.0f, 0.25f, 0.32f), new Vector2(0, 290), new Vector2(740, 90));
            _failReasonText = CreateLabel(card.transform, "MISSED TARGET!", 34, new Color(1f, 0.75f, 0.35f), new Vector2(0, 190), new Vector2(760, 65));
            _failScoreText = CreateLabel(card.transform, "SCORE: 0", 44, Color.white, new Vector2(0, 95), new Vector2(700, 70));
            _failBestText = CreateLabel(card.transform, "BEST SCORE: 0", 34, new Color(0.65f, 0.85f, 1f), new Vector2(0, 20), new Vector2(700, 60));

            CreateNeonButton(card.transform, "RETRY LEVEL", new Vector2(0, -135), new Vector2(600, 135),
                new Color(1.0f, 0.28f, 0.35f), 46, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    _currentScore = _levelStartScore;
                    StartLevel(_currentLevelIndex);
                });

            CreateNeonButton(card.transform, "HOME", new Vector2(0, -300), new Vector2(520, 110),
                new Color(0.26f, 0.28f, 0.42f), 36, () =>
                {
                    ProceduralAudioController.Instance?.PlayClick();
                    ShowState(GameState.MAIN_MENU);
                });
        }

        // =====================================================================
        // UI HELPER METHODS
        // =====================================================================
        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = (bgColor.a > 0.05f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private GameObject CreateCard(Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject go = new GameObject("Card");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            AddGlowOutline(go, new Color(0.25f, 0.65f, 1.0f, 0.45f), 2f);
            return go;
        }

        private Text CreateLabel(Transform parent, string content, int fontSize, Color color, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.font = _uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return txt;
        }

        private Button CreateNeonButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgCol, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = bgCol;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            AddGlowOutline(go, Color.Lerp(bgCol, Color.white, 0.55f), 3f);
            CreateLabel(go.transform, label, fontSize, Color.white, Vector2.zero, size - new Vector2(20, 10));
            return btn;
        }

        private void AddGlowOutline(GameObject target, Color col, float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = col;
            outline.effectDistance = new Vector2(distance, -distance);
        }
    }
}
