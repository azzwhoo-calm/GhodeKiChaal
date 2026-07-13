// GhodeTheme.cs — the board's wardrobe. Three costumes (Wood / Ebony /
// Marble), each a full set of colors the views repaint from. Wood keeps the
// real wooden tile art; Ebony and Marble are flat recolors for now — this is
// the THEME SKELETON from the plan. When the recolored art lands (art
// pipeline item), each theme grows its own sprites and nothing else changes.
// Label colors are part of every set on purpose: a parchment number that
// reads beautifully on dark wood vanishes on marble.

using UnityEngine;
using Ghode.Core;

namespace Ghode.UI
{
    /// <summary>One theme's complete color set. Views never invent colors.</summary>
    public class ThemeColors
    {
        /// <summary>Use the wooden tile sprites? (Only Wood, until new art lands.)</summary>
        public bool UseWoodTiles;

        public Color CellLight;
        public Color CellDark;
        public Color CellLegal;
        public Color CellBest;
        public Color CellVisited;
        public Color CellCurrent;
        public Color CellDead;

        /// <summary>Move numbers on visited squares.</summary>
        public Color LabelDefault;
        /// <summary>Dots and numbers on highlighted squares (legal/best/current).</summary>
        public Color LabelOnAction;

        /// <summary>Tint multiplied over the wooden frame sprite (or the flat frame color).</summary>
        public Color FrameTint;
        /// <summary>The breadcrumb ribbon.</summary>
        public Color Trail;
        /// <summary>Tint multiplied over the horse sprite.</summary>
        public Color HorseTint;
    }

    /// <summary>
    /// Holds the active <see cref="Theme"/> (GameController keeps it in sync
    /// with Settings) and serves its color set. Views read
    /// <see cref="Colors"/> on every repaint, so switching themes is just
    /// "set Current + RaiseChanged".
    /// </summary>
    public static class GhodeTheme
    {
        /// <summary>The costume currently worn. Mirrors Settings.Theme.</summary>
        public static Theme Current = Theme.Wood;

        /// <summary>The active theme's full color set.</summary>
        public static ThemeColors Colors
        {
            get
            {
                switch (Current)
                {
                    case Theme.Ebony: return Ebony;
                    case Theme.Marble: return Marble;
                    default: return Wood;
                }
            }
        }

        // ---- Wood: the free default — exactly the classic palette ----------
        static readonly ThemeColors Wood = new ThemeColors
        {
            UseWoodTiles = true,
            CellLight = UiFactory.Palette.CellLight,
            CellDark = UiFactory.Palette.CellDark,
            CellLegal = UiFactory.Palette.CellLegal,
            CellBest = UiFactory.Palette.CellBest,
            CellVisited = UiFactory.Palette.CellVisited,
            CellCurrent = UiFactory.Palette.CellCurrent,
            CellDead = UiFactory.Palette.CellDead,
            LabelDefault = UiFactory.Palette.Parchment,
            LabelOnAction = UiFactory.Palette.Walnut,
            FrameTint = Color.white,
            Trail = new Color(0xEA / 255f, 0xD9 / 255f, 0xB0 / 255f, 0.5f),
            HorseTint = Color.white
        };

        // ---- Ebony: charcoal board, gold accents ----------------------------
        static readonly ThemeColors Ebony = new ThemeColors
        {
            UseWoodTiles = false,
            CellLight = new Color32(0x4A, 0x46, 0x42, 0xFF),
            CellDark = new Color32(0x2B, 0x28, 0x25, 0xFF),
            CellLegal = new Color32(0x8A, 0x6F, 0x33, 0xFF),
            CellBest = new Color32(0xE0, 0xA8, 0x3C, 0xFF),
            CellVisited = new Color32(0x15, 0x13, 0x11, 0xFF),
            CellCurrent = new Color32(0xC8, 0x90, 0x2F, 0xFF),
            CellDead = new Color32(0x0D, 0x0C, 0x0B, 0xFF),
            LabelDefault = UiFactory.Palette.Parchment,
            LabelOnAction = new Color32(0x15, 0x13, 0x11, 0xFF),
            FrameTint = new Color(0.32f, 0.29f, 0.28f, 1f), // darkens the wood frame to near-black
            Trail = new Color(0xE0 / 255f, 0xA8 / 255f, 0x3C / 255f, 0.4f), // gold thread
            HorseTint = new Color(0.55f, 0.52f, 0.5f, 1f)   // smoke-darkened horse
        };

        // ---- Marble: pale stone board, walnut ink ---------------------------
        static readonly ThemeColors Marble = new ThemeColors
        {
            UseWoodTiles = false,
            CellLight = new Color32(0xEC, 0xEA, 0xE4, 0xFF),
            CellDark = new Color32(0xB8, 0xB3, 0xAA, 0xFF),
            CellLegal = new Color32(0x9F, 0xC2, 0x8F, 0xFF),  // soft semantic green
            CellBest = new Color32(0xE0, 0xA8, 0x3C, 0xFF),
            CellVisited = new Color32(0x7E, 0x78, 0x6E, 0xFF),
            CellCurrent = new Color32(0xE0, 0xA8, 0x3C, 0xFF),
            CellDead = new Color32(0x55, 0x51, 0x4B, 0xFF),
            LabelDefault = Color.white,                        // on the mid-gray visited squares
            LabelOnAction = new Color32(0x3A, 0x2A, 0x18, 0xFF),
            FrameTint = new Color(0.92f, 0.90f, 0.88f, 1f),    // bleaches the frame toward stone
            Trail = new Color(0x3A / 255f, 0x2A / 255f, 0x18 / 255f, 0.35f), // walnut ink line
            HorseTint = Color.white
        };
    }
}
