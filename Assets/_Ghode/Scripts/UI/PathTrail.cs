// PathTrail.cs — the breadcrumb ribbon showing where the horse has been.
// BoardView hands it the visited squares' centre points each repaint and it
// draws thin translucent segments connecting them in order (the same trail
// the web version draws). Segments are pooled: a 8×8 tour needs at most 63,
// created once and recycled forever — zero allocations during normal play.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>
    /// Draws the visited-path ribbon on its own board layer (between the
    /// tiles and the horse). Purely visual; input passes straight through.
    /// </summary>
    public class PathTrail : MonoBehaviour
    {
        static readonly Color TrailColor = new Color(0xEA / 255f, 0xD9 / 255f, 0xB0 / 255f, 0.5f);

        readonly List<RectTransform> _segments = new List<RectTransform>();
        RectTransform _layer;

        /// <summary>Attach a trail to the given (already positioned) board layer.</summary>
        public static PathTrail Build(RectTransform layer)
        {
            var trail = layer.gameObject.AddComponent<PathTrail>();
            trail._layer = layer;
            return trail;
        }

        /// <summary>
        /// Redraw the ribbon through these centre points (anchored space,
        /// y-down). One point or none = no ribbon. <paramref name="thickness"/>
        /// scales with the cell so big boards get daintier lines.
        /// </summary>
        public void Render(IReadOnlyList<Vector2> points, float thickness)
        {
            int needed = Mathf.Max(0, points.Count - 1);

            EnsurePool(needed);

            for (int i = 0; i < needed; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;

                var rt = _segments[i];
                rt.gameObject.SetActive(true);
                rt.anchoredPosition = mid;
                // Slightly longer than the gap so joints overlap instead of cracking.
                rt.sizeDelta = new Vector2(delta.magnitude + thickness * 0.5f, thickness);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            // Park the leftovers (e.g. after an undo shortened the path).
            for (int i = needed; i < _segments.Count; i++)
            {
                _segments[i].gameObject.SetActive(false);
            }
        }

        // Grow the pool up to `needed` segments (never shrinks — see header).
        void EnsurePool(int needed)
        {
            while (_segments.Count < needed)
            {
                var rt = UiFactory.CreateRect("Segment" + _segments.Count, _layer);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var image = rt.gameObject.AddComponent<Image>();
                image.color = TrailColor;
                image.raycastTarget = false; // never eat a tap

                _segments.Add(rt);
            }
        }
    }
}
