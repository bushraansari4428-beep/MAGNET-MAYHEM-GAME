using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagnetMayhem
{
    public enum GameState
    {
        MAIN_MENU,
        LEVEL_SELECT,
        LEVEL_START,
        ATTRACT_MODE,
        REPEL_MODE,
        BALL_DELIVERED,
        LEVEL_COMPLETE,
        LEVEL_FAILED,
        PAUSED
    }

    public enum MagnetMode
    {
        ATTRACT,
        REPEL
    }

    public enum BallState
    {
        SPAWNING,
        APPROACHING,
        ATTRACTED,
        READY_TO_REPEL,
        REPELLING,
        DELIVERED,
        FAILED
    }

    public enum BallColorType
    {
        Blue = 0,
        Red = 1,
        Green = 2,
        Yellow = 3
    }

    public enum BallSpecialType
    {
        Normal,
        Heavy,
        Light,
        Golden,
        Shield,
        Bomb
    }

    public enum TimingResult
    {
        None,
        Perfect,
        Good,
        TooEarly,
        TooLate
    }

    public enum MagnetSkinType
    {
        Classic = 0,
        Neon = 1,
        Golden = 2,
        Electric = 3,
        Galaxy = 4,
        Ice = 5
    }

    [Serializable]
    public class MagnetSkinInfo
    {
        public MagnetSkinType skinType;
        public string displayName;
        public int coinCost;
        public Color attractColor;
        public Color repelColor;
        public Color bodyMetalColor;
    }

    [Serializable]
    public class BallSpawnEntry
    {
        public BallColorType colorType;
        public BallSpecialType specialType;
        public bool spawnFromLeft;
    }

    [Serializable]
    public class LevelConfig
    {
        public int levelNumber;
        public int worldNumber;
        public string worldTitle;
        public string levelTitle;
        public string tutorialText;
        public bool autoAssistTutorial;

        public int requiredDeliveries;
        public float baseBallSpeed;
        public float magnetStrength;

        // Distance thresholds from Magnet center (in world units)
        public float goodWindowOuterRadius;
        public float perfectWindowOuterRadius;
        public float perfectWindowInnerRadius;
        public float magnetHitRadius;

        public float targetOpeningScale;

        // Obstacles (World 3+)
        public bool hasStaticBarrier;
        public bool hasMovingGate;
        public bool hasRotatingBarrier;
        public float obstacleSpeed;

        public List<BallSpawnEntry> ballSequence = new List<BallSpawnEntry>();
    }

    public static class LevelDatabase
    {
        public static readonly List<MagnetSkinInfo> Skins = new List<MagnetSkinInfo>
        {
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Classic,
                displayName = "CLASSIC MAGNET",
                coinCost = 0,
                attractColor = new Color(0.15f, 0.75f, 1.0f),
                repelColor = new Color(1.0f, 0.28f, 0.22f),
                bodyMetalColor = new Color(0.75f, 0.80f, 0.88f)
            },
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Neon,
                displayName = "NEON PULSE",
                coinCost = 100,
                attractColor = new Color(0.0f, 1.0f, 0.92f),
                repelColor = new Color(1.0f, 0.08f, 0.55f),
                bodyMetalColor = new Color(0.22f, 0.26f, 0.42f)
            },
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Golden,
                displayName = "GOLDEN CORE",
                coinCost = 200,
                attractColor = new Color(0.25f, 0.85f, 1.0f),
                repelColor = new Color(1.0f, 0.78f, 0.12f),
                bodyMetalColor = new Color(0.95f, 0.78f, 0.25f)
            },
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Electric,
                displayName = "ELECTRIC VOLT",
                coinCost = 350,
                attractColor = new Color(0.35f, 0.55f, 1.0f),
                repelColor = new Color(0.75f, 0.25f, 1.0f),
                bodyMetalColor = new Color(0.30f, 0.38f, 0.58f)
            },
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Galaxy,
                displayName = "GALAXY VOID",
                coinCost = 500,
                attractColor = new Color(0.55f, 0.35f, 1.0f),
                repelColor = new Color(1.0f, 0.35f, 0.75f),
                bodyMetalColor = new Color(0.18f, 0.12f, 0.32f)
            },
            new MagnetSkinInfo
            {
                skinType = MagnetSkinType.Ice,
                displayName = "CRYSTAL ICE",
                coinCost = 750,
                attractColor = new Color(0.45f, 0.95f, 1.0f),
                repelColor = new Color(1.0f, 0.45f, 0.15f),
                bodyMetalColor = new Color(0.82f, 0.95f, 1.0f)
            }
        };

        public static Color GetBallColor(BallColorType type)
        {
            switch (type)
            {
                case BallColorType.Blue:   return new Color(0.12f, 0.68f, 1.0f);
                case BallColorType.Red:    return new Color(1.0f, 0.22f, 0.32f);
                case BallColorType.Green:  return new Color(0.15f, 0.95f, 0.52f);
                case BallColorType.Yellow: return new Color(1.0f, 0.85f, 0.15f);
                default:                   return new Color(0.12f, 0.68f, 1.0f);
            }
        }

        public static LevelConfig GetLevel(int levelNum)
        {
            levelNum = Mathf.Max(1, levelNum);

            LevelConfig cfg = new LevelConfig
            {
                levelNumber = levelNum,
                baseBallSpeed = 2.6f,
                magnetStrength = 14.0f,
                goodWindowOuterRadius = 2.25f,
                perfectWindowOuterRadius = 1.75f,
                perfectWindowInnerRadius = 1.15f,
                magnetHitRadius = 0.72f,
                targetOpeningScale = 1.0f,
                hasStaticBarrier = false,
                hasMovingGate = false,
                hasRotatingBarrier = false,
                obstacleSpeed = 1.5f
            };

            // WORLD 1: MAGNET BASICS (Levels 1-5)
            if (levelNum <= 5)
            {
                cfg.worldNumber = 1;
                cfg.worldTitle = "WORLD 1: MAGNET BASICS";

                if (levelNum == 1)
                {
                    cfg.levelTitle = "FIRST ATTRACTION";
                    cfg.tutorialText = "THE MAGNET PULLS THE BALL\nTAP WHEN IT ENTERS THE GLOWING RING!";
                    cfg.autoAssistTutorial = true;
                    cfg.requiredDeliveries = 1;
                    cfg.baseBallSpeed = 2.1f;
                    cfg.goodWindowOuterRadius = 2.50f;
                    cfg.perfectWindowOuterRadius = 1.95f;
                    cfg.perfectWindowInnerRadius = 1.00f;
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Blue, BallSpecialType.Normal, true));
                }
                else if (levelNum == 2)
                {
                    cfg.levelTitle = "FIRST REPULSION";
                    cfg.tutorialText = "TAP TO REPEL!\nLAUNCH THE BLUE BALL INTO THE BLUE BASKET";
                    cfg.requiredDeliveries = 1;
                    cfg.baseBallSpeed = 2.3f;
                    cfg.goodWindowOuterRadius = 2.40f;
                    cfg.perfectWindowOuterRadius = 1.85f;
                    cfg.perfectWindowInnerRadius = 1.05f;
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Blue, BallSpecialType.Normal, true));
                }
                else if (levelNum == 3)
                {
                    cfg.levelTitle = "LEARN THE FULL CYCLE";
                    cfg.tutorialText = "MAGNET RESETS TO ATTRACT AFTER DELIVERY\nDELIVER BOTH BLUE BALLS!";
                    cfg.requiredDeliveries = 2;
                    cfg.baseBallSpeed = 2.5f;
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Blue, BallSpecialType.Normal, true));
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Blue, BallSpecialType.Normal, true));
                }
                else if (levelNum == 4)
                {
                    cfg.levelTitle = "INTRODUCE RED";
                    cfg.tutorialText = "SEND EACH BALL TO ITS MATCHING COLOR BASKET!";
                    cfg.requiredDeliveries = 2;
                    cfg.baseBallSpeed = 2.6f;
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Blue, BallSpecialType.Normal, true));
                    cfg.ballSequence.Add(CreateEntry(BallColorType.Red, BallSpecialType.Normal, false));
                }
                else // Level 5
                {
                    cfg.levelTitle = "COLOR CHALLENGE";
                    cfg.tutorialText = "DELIVER ALL 5 MIXED BALLS TO COMPLETE WORLD 1!";
                    cfg.requiredDeliveries = 5;
                    cfg.baseBallSpeed = 2.85f;
                    BallColorType[] seq = { BallColorType.Blue, BallColorType.Red, BallColorType.Blue, BallColorType.Blue, BallColorType.Red };
                    foreach (var c in seq)
                        cfg.ballSequence.Add(CreateEntry(c, BallSpecialType.Normal, c == BallColorType.Blue));
                }
                return cfg;
            }

            // WORLD 2: SPEED AND TIMING (Levels 6-10)
            if (levelNum <= 10)
            {
                cfg.worldNumber = 2;
                cfg.worldTitle = "WORLD 2: SPEED & TIMING";

                if (levelNum == 6)
                {
                    cfg.levelTitle = "FASTER BALLS";
                    cfg.tutorialText = "SPEED INCREASED BY 10% — STAY SHARP!";
                    cfg.requiredDeliveries = 5;
                    cfg.baseBallSpeed = 3.15f;
                }
                else if (levelNum == 7)
                {
                    cfg.levelTitle = "NARROW TIMING WINDOW";
                    cfg.tutorialText = "SMALLER TIMING RING — HIT PERFECT FOR +20 BONUS!";
                    cfg.requiredDeliveries = 6;
                    cfg.baseBallSpeed = 3.25f;
                    cfg.goodWindowOuterRadius = 2.05f;
                    cfg.perfectWindowOuterRadius = 1.65f;
                    cfg.perfectWindowInnerRadius = 1.20f;
                }
                else if (levelNum == 8)
                {
                    cfg.levelTitle = "MIXED BALL SEQUENCE";
                    cfg.tutorialText = "8 RAPID BALLS — WATCH THE INCOMING SIDE & COLOR!";
                    cfg.requiredDeliveries = 8;
                    cfg.baseBallSpeed = 3.4f;
                }
                else if (levelNum == 9)
                {
                    cfg.levelTitle = "COMBO CHALLENGE";
                    cfg.tutorialText = "CHAIN 3+ CONSECUTIVE DELIVERIES FOR COMBO BONUSES!";
                    cfg.requiredDeliveries = 8;
                    cfg.baseBallSpeed = 3.5f;
                    cfg.goodWindowOuterRadius = 2.00f;
                }
                else // Level 10
                {
                    cfg.levelTitle = "WORLD 2 BOSS CHALLENGE";
                    cfg.tutorialText = "DELIVER 10 HIGH-SPEED BALLS TO UNLOCK WORLD 3!";
                    cfg.requiredDeliveries = 10;
                    cfg.baseBallSpeed = 3.7f;
                    cfg.goodWindowOuterRadius = 1.95f;
                    cfg.perfectWindowOuterRadius = 1.60f;
                    cfg.perfectWindowInnerRadius = 1.22f;
                }

                PopulateDeterministicSequence(cfg, levelNum, false);
                return cfg;
            }

            // WORLD 3: OBSTACLES (Levels 11-15)
            if (levelNum <= 15)
            {
                cfg.worldNumber = 3;
                cfg.worldTitle = "WORLD 3: OBSTACLES";
                cfg.baseBallSpeed = 3.4f + (levelNum - 11) * 0.12f;
                cfg.requiredDeliveries = 6 + (levelNum - 11);

                if (levelNum == 11)
                {
                    cfg.levelTitle = "STATIC BARRIER";
                    cfg.tutorialText = "REPEL INSIDE THE RING TO CURVE PAST THE BARRIERS!";
                    cfg.hasStaticBarrier = true;
                }
                else if (levelNum == 12)
                {
                    cfg.levelTitle = "MOVING GATE";
                    cfg.tutorialText = "TIME YOUR REPEL SO THE BALL PASSES THE MOVING GATE!";
                    cfg.hasMovingGate = true;
                    cfg.obstacleSpeed = 1.6f;
                }
                else if (levelNum == 13)
                {
                    cfg.levelTitle = "ROTATING BARRIER";
                    cfg.tutorialText = "WATCH THE ROTATING BLADE BEFORE LAUNCHING!";
                    cfg.hasRotatingBarrier = true;
                    cfg.obstacleSpeed = 65f;
                }
                else if (levelNum == 14)
                {
                    cfg.levelTitle = "NARROW TARGETS";
                    cfg.tutorialText = "SMALLER TARGET OPENINGS — PRECISION REQUIRED!";
                    cfg.targetOpeningScale = 0.76f;
                    cfg.goodWindowOuterRadius = 1.92f;
                }
                else // Level 15
                {
                    cfg.levelTitle = "COMBINED GAUNTLET";
                    cfg.tutorialText = "MOVING GATE + NARROW TARGETS + FAST BALLS!";
                    cfg.hasMovingGate = true;
                    cfg.obstacleSpeed = 2.0f;
                    cfg.targetOpeningScale = 0.82f;
                    cfg.requiredDeliveries = 10;
                }

                PopulateDeterministicSequence(cfg, levelNum, false);
                return cfg;
            }

            // WORLD 4+: ADVANCED MAGNET CONTROL & SPECIAL BALLS (Levels 16+)
            cfg.worldNumber = 4;
            cfg.worldTitle = "WORLD 4: ADVANCED MAGNET";
            cfg.requiredDeliveries = Mathf.Clamp(8 + (levelNum - 16) / 2, 8, 16);
            cfg.baseBallSpeed = Mathf.Clamp(3.6f + (levelNum - 16) * 0.08f, 3.6f, 5.2f);
            cfg.goodWindowOuterRadius = Mathf.Clamp(1.98f - (levelNum - 16) * 0.01f, 1.72f, 1.98f);
            cfg.perfectWindowOuterRadius = 1.58f;
            cfg.perfectWindowInnerRadius = 1.22f;
            cfg.targetOpeningScale = Mathf.Clamp(0.95f - (levelNum - 16) * 0.008f, 0.75f, 0.95f);

            if (levelNum == 16)
            {
                cfg.levelTitle = "HEAVY METAL BALLS";
                cfg.tutorialText = "HEAVY BALLS MOVE SLOWER & GIVE +25 BONUS POINTS!";
            }
            else if (levelNum == 17)
            {
                cfg.levelTitle = "LIGHTNING BALLS";
                cfg.tutorialText = "LIGHT BALLS ACCELERATE QUICKLY — REACT FAST!";
            }
            else if (levelNum == 18)
            {
                cfg.levelTitle = "GOLDEN JACKPOT";
                cfg.tutorialText = "GOLDEN BALLS AWARD +50 POINTS AND BONUS COINS!";
            }
            else if (levelNum == 19)
            {
                cfg.levelTitle = "SHIELD & BOMB HAZARD";
                cfg.tutorialText = "REPEL BOMB BALLS INSIDE THE RING TO DEFUSE THEM DOWN THE CENTER!";
            }
            else
            {
                cfg.levelTitle = "MAYHEM STAGE " + levelNum;
                cfg.tutorialText = "MASTER SPEED, OBSTACLES & SPECIAL BALLS!";
                cfg.hasMovingGate = (levelNum % 2 == 0);
                cfg.hasRotatingBarrier = (levelNum % 3 == 0);
                cfg.obstacleSpeed = (cfg.hasRotatingBarrier) ? 75f : 2.1f;
            }

            PopulateDeterministicSequence(cfg, levelNum, true);
            return cfg;
        }

        private static void PopulateDeterministicSequence(LevelConfig cfg, int seedLevel, bool includeSpecials)
        {
            System.Random rng = new System.Random(seedLevel * 1337 + 42);
            for (int i = 0; i < cfg.requiredDeliveries; i++)
            {
                BallColorType color = (rng.NextDouble() > 0.5) ? BallColorType.Blue : BallColorType.Red;
                BallSpecialType special = BallSpecialType.Normal;

                if (includeSpecials)
                {
                    if (seedLevel == 16 && i % 2 == 1) special = BallSpecialType.Heavy;
                    else if (seedLevel == 17 && i % 2 == 1) special = BallSpecialType.Light;
                    else if (seedLevel == 18 && i % 3 == 1) special = BallSpecialType.Golden;
                    else if (seedLevel >= 19)
                    {
                        int roll = rng.Next(0, 10);
                        if (roll == 0) special = BallSpecialType.Golden;
                        else if (roll == 1 || roll == 2) special = BallSpecialType.Heavy;
                        else if (roll == 3 || roll == 4) special = BallSpecialType.Light;
                        else if (roll == 5) special = BallSpecialType.Shield;
                        else if (roll == 6) special = BallSpecialType.Bomb;
                    }
                }

                // Blue enters from Left (true) so diagonal repulsion sends it to Bottom-Left Blue Basket.
                // Red enters from Right (false) so diagonal repulsion sends it to Bottom-Right Red Basket.
                bool fromLeft = (color == BallColorType.Blue);
                cfg.ballSequence.Add(CreateEntry(color, special, fromLeft));
            }
        }

        private static BallSpawnEntry CreateEntry(BallColorType c, BallSpecialType s, bool left)
        {
            return new BallSpawnEntry
            {
                colorType = c,
                specialType = s,
                spawnFromLeft = left
            };
        }
    }
}
