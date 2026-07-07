// UiFactory.cs — the little workshop every view uses to build its uGUI pieces.
// Since our whole interface is created from code (no prefabs yet), this file
// keeps that code short and consistent: one place makes panels, texts, buttons
// and layout stacks, and one Palette holds the warm wood-and-parchment colors.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>
    /// Static helpers for building uGUI elements in code, plus the game's
    /// color <see cref="Palette"/>. Every screen and control builds itself
    /// out of these pieces so the whole game looks like one carved set.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>
        /// The brand colors: dark walnut wood and light parchment, with a warm
        /// gold accent. Placeholder cells use these too, so even without real
        /// art the game already looks on-brand.
        /// </summary>
        public static class Palette
        {
            /// <summary>Page background — deeper than the brand walnut so panels pop.</summary>
            public static readonly Color WalnutDeep = new Color32(0x2A, 0x1D, 0x10, 0xFF);

            /// <summary>Brand dark walnut brown (#3A2A18) — panels and banners.</summary>
            public static readonly Color Walnut = new Color32(0x3A, 0x2A, 0x18, 0xFF);

            /// <summary>Buttons at rest — a lighter cut of walnut.</summary>
            public static readonly Color ButtonFace = new Color32(0x5C, 0x44, 0x26, 0xFF);

            /// <summary>Brand light parchment (#EAD9B0) — primary text and light squares.</summary>
            public static readonly Color Parchment = new Color32(0xEA, 0xD9, 0xB0, 0xFF);

            /// <summary>Quieter parchment for secondary labels.</summary>
            public static readonly Color ParchmentDim = new Color32(0xC4, 0xB0, 0x86, 0xFF);

            /// <summary>Warm gold accent — highlights, selection, the horse's square.</summary>
            public static readonly Color Accent = new Color32(0xE0, 0xA8, 0x3C, 0xFF);

            // ---- Board square colors ----
            /// <summary>Light squares (parchment).</summary>
            public static readonly Color CellLight = new Color32(0xEA, 0xD9, 0xB0, 0xFF);
            /// <summary>Dark squares (mid walnut).</summary>
            public static readonly Color CellDark = new Color32(0xA9, 0x88, 0x5B, 0xFF);
            /// <summary>A square the horse has stamped (shows its move number).</summary>
            public static readonly Color CellVisited = new Color32(0x6B, 0x4E, 0x2E, 0xFF);
            /// <summary>The square the horse is standing on right now.</summary>
            public static readonly Color CellCurrent = new Color32(0xE0, 0xA8, 0x3C, 0xFF);
            /// <summary>A legal hop target (Apprentice/Knight difficulties).</summary>
            public static readonly Color CellLegal = new Color32(0xE8, 0xC8, 0x78, 0xFF);
            /// <summary>Warnsdorff's recommended hop (Apprentice, or after Hint).</summary>
            public static readonly Color CellBest = new Color32(0xF5, 0xA9, 0x44, 0xFF);
            /// <summary>Unreached squares once the horse is stuck — the sad ones.</summary>
            public static readonly Color CellDead = new Color32(0x5A, 0x50, 0x44, 0xFF);

            /// <summary>Dim film behind the pause overlay.</summary>
            public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.65f);
        }

        static Font _font;

        /// <summary>
        /// The built-in font that ships inside every Unity player.
        /// TODO(azzwhoo): swap for our real display font once art lands
        /// (import the .ttf under Assets/_Ghode/Fonts/ and load it here).
        /// </summary>
        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    // "Arial.ttf" was removed in modern Unity; this is its successor.
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        /// <summary>Make an empty RectTransform child — the blank sheet of uGUI.</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false); // false = keep local scale/position sane
            return rt;
        }

        /// <summary>
        /// A solid-color rectangle. Set <paramref name="blocksTaps"/> true only
        /// when the panel should swallow clicks (e.g. the pause overlay's film).
        /// </summary>
        public static Image CreatePanel(string name, Transform parent, Color color, bool blocksTaps = false)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksTaps;
            return image;
        }

        /// <summary>Stretch a RectTransform to completely fill its parent.</summary>
        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>A text label (legacy uGUI Text — no TMP assets required).</summary>
        public static Text CreateText(string name, Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow; // never silently vanish
            text.raycastTarget = false; // labels should not steal taps
            return text;
        }

        /// <summary>
        /// A tappable button: colored face + centered label, wired to onClick.
        /// This is the ONE way input enters the game — uGUI Buttons work with
        /// both editor mouse and device touch, no platform branching needed.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, int fontSize,
            UnityAction onClick, Color? face = null, Color? textColor = null)
        {
            var image = CreatePanel(name, parent, face ?? Palette.ButtonFace, blocksTaps: true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; // uGUI tints this on press automatically
            if (onClick != null) button.onClick.AddListener(onClick);

            var text = CreateText("Label", image.transform, label, fontSize, textColor ?? Palette.Parchment);
            Fill((RectTransform)text.transform);
            return button;
        }

        /// <summary>Stack children top-to-bottom with spacing and padding.</summary>
        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var stack = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = spacing;
            stack.padding = padding;
            stack.childAlignment = align;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            return stack;
        }

        /// <summary>Lay children out left-to-right with spacing and padding.</summary>
        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var stack = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            stack.spacing = spacing;
            stack.padding = padding;
            stack.childAlignment = align;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            return stack;
        }

        /// <summary>
        /// Give an element sizing hints for the layout system. Pass -1 to leave
        /// a value alone. (LayoutElement is how a child tells its stack
        /// "I want to be THIS tall" or "let me soak up spare room".)
        /// </summary>
        public static LayoutElement Layout(Component target, float minHeight = -1f, float preferredHeight = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            if (minHeight >= 0f) element.minHeight = minHeight;
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) element.flexibleHeight = flexibleHeight;
            return element;
        }

        /// <summary>An invisible stretchy gap for stacks (like a spring).</summary>
        public static RectTransform Spacer(Transform parent, float flexible = 1f)
        {
            var rt = CreateRect("Spacer", parent);
            Layout(rt, flexibleHeight: flexible);
            return rt;
        }
    }
}
