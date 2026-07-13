// GhodeArt.cs — the game's art cupboard. All the wooden sprites live under
// Assets/_Ghode/Art/Resources/Ghode/ and are loaded here by name, once.
// EVERY sprite is optional: if a file is missing (or someone strips the art),
// every getter returns null and the views fall back to flat palette colors —
// the game must always run, with or without its costume.

using UnityEngine;

namespace Ghode.UI
{
    /// <summary>
    /// Lazy-loading access to the wooden art set (board frame, tiles, horse)
    /// plus one small procedural sprite (a soft round shadow) that we generate
    /// in code so no artist ever has to draw a blurry ellipse.
    /// </summary>
    public static class GhodeArt
    {
        static bool _loaded;
        static Sprite _tileLight;
        static Sprite _tileDark;
        static Sprite _frame;
        static Sprite _horse;
        static Sprite _softShadow;

        /// <summary>Light board square texture (null → flat parchment color).</summary>
        public static Sprite TileLight { get { EnsureLoaded(); return _tileLight; } }

        /// <summary>Dark board square texture (null → flat walnut color).</summary>
        public static Sprite TileDark { get { EnsureLoaded(); return _tileDark; } }

        /// <summary>The wooden picture-frame around the board (transparent middle).</summary>
        public static Sprite Frame { get { EnsureLoaded(); return _frame; } }

        /// <summary>The carved horse piece (background keyed out to alpha).</summary>
        public static Sprite Horse { get { EnsureLoaded(); return _horse; } }

        /// <summary>
        /// A soft radial shadow blob, generated once in code. Used as the
        /// horse's ground shadow so hops read as real height.
        /// </summary>
        public static Sprite SoftShadow
        {
            get
            {
                if (_softShadow == null) _softShadow = MakeSoftShadow();
                return _softShadow;
            }
        }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _tileLight = Load("Wooden_tile_light");
            _tileDark = Load("Wooden_tile_dark");
            _frame = Load("Wooden_Frame");
            _horse = Load("Wooden_Horse");
        }

        // Loads one sprite from Resources/Ghode/. Handles both Single and
        // Multiple sprite import modes, and shrugs (null) when nothing exists.
        static Sprite Load(string name)
        {
            var sprite = Resources.Load<Sprite>("Ghode/" + name);
            if (sprite != null) return sprite;

            // A texture imported in "Multiple" mode hides its sub-sprites from
            // the simple Load — LoadAll digs them out.
            var all = Resources.LoadAll<Sprite>("Ghode/" + name);
            return all.Length > 0 ? all[0] : null;
        }

        // In plain words: paint a 64×64 circle whose alpha fades smoothly from
        // the middle to the rim, then wrap it as a sprite. Tinted and squashed
        // by whoever uses it.
        static Sprite MakeSoftShadow()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GhodeSoftShadow",
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (size - 1) / 2f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Solid-ish middle, feathering out to nothing at the rim.
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep for a soft roll-off
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true); // upload and free the CPU copy

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
