using System.Collections.Generic;
using UnityEngine;

namespace MagnetMayhem
{
    public static class NeonArtGenerator
    {
        private static Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite GetCyberGridBackground(int width = 512, int height = 1024)
        {
            string key = $"bg_{width}_{height}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color topCol = new Color(0.02f, 0.03f, 0.09f, 1f);
            Color botCol = new Color(0.05f, 0.02f, 0.12f, 1f);
            Color gridCol = new Color(0.10f, 0.35f, 0.65f, 0.14f);
            Color glowGridCol = new Color(0.18f, 0.65f, 1.0f, 0.22f);

            int gridStep = 32;
            for (int y = 0; y < height; y++)
            {
                float ty = (float)y / height;
                Color rowBase = Color.Lerp(botCol, topCol, ty);

                for (int x = 0; x < width; x++)
                {
                    float tx = (float)x / width;
                    float centerDist = Vector2.Distance(new Vector2(tx, ty), new Vector2(0.5f, 0.58f));
                    Color c = rowBase + new Color(0.02f, 0.06f, 0.14f, 0f) * Mathf.Clamp01(1f - centerDist * 1.8f);

                    bool isGridX = (x % gridStep == 0);
                    bool isGridY = (y % gridStep == 0);
                    if (isGridX || isGridY)
                    {
                        bool isMajor = (x % (gridStep * 4) == 0) || (y % (gridStep * 4) == 0);
                        c = Color.Lerp(c, isMajor ? glowGridCol : gridCol, isMajor ? 0.32f : 0.16f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 48f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetHorseshoeMagnetSprite(MagnetSkinInfo skin, bool isRepelMode)
        {
            string key = $"magnet_{skin.skinType}_{isRepelMode}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            Color activeAura = isRepelMode ? skin.repelColor : skin.attractColor;
            Color leftPoleCol = new Color(0.12f, 0.68f, 1.0f);   // Blue pole
            Color rightPoleCol = new Color(1.0f, 0.22f, 0.30f);  // Red pole

            Vector2 archCenter = new Vector2(size * 0.5f, size * 0.56f);
            float outerR = size * 0.36f;
            float innerR = size * 0.18f;
            float thickness = (outerR - innerR) * 0.5f;
            float midR = (outerR + innerR) * 0.5f;
            float legBottomY = size * 0.20f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distToArch = float.MaxValue;

                    if (y >= archCenter.y)
                    {
                        float r = Vector2.Distance(p, archCenter);
                        distToArch = Mathf.Abs(r - midR);
                    }
                    else if (y >= legBottomY)
                    {
                        float leftLegX = archCenter.x - midR;
                        float rightLegX = archCenter.x + midR;
                        float dLeft = Mathf.Abs(x - leftLegX);
                        float dRight = Mathf.Abs(x - rightLegX);
                        distToArch = Mathf.Min(dLeft, dRight);
                    }
                    else
                    {
                        float leftLegX = archCenter.x - midR;
                        float rightLegX = archCenter.x + midR;
                        float dLeft = Vector2.Distance(p, new Vector2(leftLegX, legBottomY));
                        float dRight = Vector2.Distance(p, new Vector2(rightLegX, legBottomY));
                        distToArch = Mathf.Min(dLeft, dRight);
                    }

                    if (distToArch <= thickness + 18f)
                    {
                        if (distToArch > thickness)
                        {
                            // Outer neon glow
                            float glowAlpha = Mathf.Clamp01(1f - (distToArch - thickness) / 18f);
                            glowAlpha = glowAlpha * glowAlpha * 0.75f;
                            tex.SetPixel(x, y, new Color(activeAura.r, activeAura.g, activeAura.b, glowAlpha));
                        }
                        else
                        {
                            // Inside horseshoe body
                            float edgeFactor = distToArch / thickness;
                            float highlight = Mathf.Cos(edgeFactor * Mathf.PI * 0.5f);

                            Color baseCol = skin.bodyMetalColor;
                            // Color tips (poles) at bottom of horseshoe legs
                            if (y < archCenter.y - size * 0.12f)
                            {
                                baseCol = (x < archCenter.x) ? leftPoleCol : rightPoleCol;
                                if (y > archCenter.y - size * 0.15f)
                                    baseCol = Color.white; // White metallic separator band
                            }

                            // 3D metallic shading + neon rim
                            Color shaded = baseCol * (0.55f + 0.60f * highlight);
                            if (edgeFactor > 0.82f)
                            {
                                shaded = Color.Lerp(shaded, activeAura, 0.85f);
                            }

                            shaded.a = 1f;
                            tex.SetPixel(x, y, shaded);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 140f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetBallSprite(BallColorType colorType, BallSpecialType specialType)
        {
            string key = $"ball_{colorType}_{specialType}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.34f;
            float glowRadius = size * 0.48f;

            Color baseCol = LevelDatabase.GetBallColor(colorType);
            if (specialType == BallSpecialType.Golden)
                baseCol = Color.Lerp(baseCol, new Color(1.0f, 0.85f, 0.15f), 0.65f);
            else if (specialType == BallSpecialType.Bomb)
                baseCol = new Color(1.0f, 0.35f, 0.05f);

            Vector2 specPos = center + new Vector2(-radius * 0.32f, radius * 0.32f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = Vector2.Distance(p, center);

                    if (d > glowRadius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (d > radius)
                    {
                        float t = 1f - (d - radius) / (glowRadius - radius);
                        Color glow = baseCol;
                        glow.a = t * t * 0.65f;
                        tex.SetPixel(x, y, glow);
                    }
                    else
                    {
                        float normD = d / radius;
                        float sphereShade = Mathf.Sqrt(Mathf.Clamp01(1f - normD * normD));
                        Color col = baseCol * (0.35f + 0.75f * sphereShade);

                        // Specular shine
                        float specD = Vector2.Distance(p, specPos) / (radius * 0.45f);
                        if (specD < 1f)
                        {
                            col = Color.Lerp(col, Color.white, (1f - specD) * 0.85f);
                        }

                        // Special visual rings
                        if (specialType == BallSpecialType.Heavy && normD > 0.72f)
                            col = Color.Lerp(col, new Color(0.85f, 0.90f, 1.0f), 0.7f);
                        else if (specialType == BallSpecialType.Shield && normD > 0.78f)
                            col = new Color(0.3f, 1.0f, 0.95f, 1f);
                        else if (specialType == BallSpecialType.Bomb && normD < 0.35f)
                            col = new Color(0.15f, 0.05f, 0.05f, 1f);

                        col.a = 1f;
                        tex.SetPixel(x, y, col);
                    }
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 160f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetBasketSprite(Color basketColor)
        {
            string key = $"basket_{basketColor.r:F2}_{basketColor.g:F2}_{basketColor.b:F2}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            int w = 256, h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = (float)y / h;

                    bool inOuterX = nx >= 0.10f && nx <= 0.90f;
                    bool inOuterY = ny >= 0.12f && ny <= 0.86f;

                    if (!inOuterX || !inOuterY)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool isLeftWall = nx < 0.20f;
                    bool isRightWall = nx > 0.80f;
                    bool isBottomWall = ny < 0.26f;

                    if (isLeftWall || isRightWall || isBottomWall)
                    {
                        // Bright neon U-frame
                        Color wall = Color.Lerp(basketColor, Color.white, 0.38f);
                        wall.a = 0.98f;
                        tex.SetPixel(x, y, wall);
                    }
                    else
                    {
                        // Glowing interior containment field with horizontal scanlines
                        float scan = (y % 8 < 2) ? 0.42f : 0.22f;
                        float verticalFade = 1f - (ny - 0.26f) / 0.60f;
                        Color interior = basketColor;
                        interior.a = Mathf.Clamp01(scan * verticalFade + 0.10f);
                        tex.SetPixel(x, y, interior);
                    }
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 120f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetRingSprite(int size = 256, float ringThickness = 0.055f)
        {
            string key = $"ring_{size}_{ringThickness:F3}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), c) / maxR;
                    float diff = Mathf.Abs(r - 0.88f);
                    if (diff < ringThickness)
                    {
                        float a = 1f - (diff / ringThickness);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetSoftGlowDiscSprite(int size = 128)
        {
            string key = $"disc_{size}";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / maxR;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }

        public static Sprite GetObstacleBarSprite()
        {
            string key = "obstacle_bar";
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            int w = 192, h = 40;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    bool stripe = ((x + y) % 24) < 12;
                    Color c = border
                        ? new Color(1.0f, 0.55f, 0.15f, 1f)
                        : (stripe ? new Color(0.22f, 0.25f, 0.35f, 0.95f) : new Color(0.12f, 0.14f, 0.22f, 0.95f));
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }
    }
}
