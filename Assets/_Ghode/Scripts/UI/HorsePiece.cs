// HorsePiece.cs — the actual horse that stands on the board and HOPS.
// BoardView tells it where to be and how to get there (spawn pop, hop arc,
// undo slide, or an instant snap for reduced motion). A soft procedural
// shadow stays on the ground during hops so the jump reads as real height.
// All positions are in the board layer's anchored space: origin at the
// layer's top-left corner, x right, y NEGATIVE going down.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>
    /// The animated horse piece and its ground shadow. Owns nothing but
    /// motion: BoardView decides WHAT happens, this class makes it pretty.
    /// If the horse art is missing it stays hidden and the board's gold
    /// "current" square carries the game on its own.
    /// </summary>
    public class HorsePiece : MonoBehaviour
    {
        // Timings from the production plan (§C4): spawn 320 ms, hop 380 ms
        // with the arc's apex at 0.55 × cell size. Undo is a quicker retreat.
        const float SpawnSeconds = 0.32f;
        const float HopSeconds = 0.38f;
        const float SlideSeconds = 0.22f;
        const float ApexPerCell = 0.55f;

        RectTransform _pieceRt;
        Image _piece;
        RectTransform _shadowRt;
        Image _shadow;

        float _cell;          // current cell size in reference pixels
        Vector2 _restPos;     // where the horse logically stands right now
        Coroutine _anim;

        /// <summary>False when the horse art is missing — the piece stays hidden.</summary>
        public bool HasArt => _piece != null && _piece.sprite != null;

        /// <summary>Create the shadow + horse images under the given board layer.</summary>
        public static HorsePiece Build(RectTransform layer)
        {
            var piece = layer.gameObject.AddComponent<HorsePiece>();

            // Shadow first so the horse always draws on top of it.
            var shadowRt = UiFactory.CreateRect("Shadow", layer);
            AnchorTopLeft(shadowRt);
            piece._shadowRt = shadowRt;
            piece._shadow = shadowRt.gameObject.AddComponent<Image>();
            piece._shadow.sprite = GhodeArt.SoftShadow;
            piece._shadow.color = new Color(0f, 0f, 0f, 0.35f);
            piece._shadow.raycastTarget = false;

            var pieceRt = UiFactory.CreateRect("Horse", layer);
            AnchorTopLeft(pieceRt);
            piece._pieceRt = pieceRt;
            piece._piece = pieceRt.gameObject.AddComponent<Image>();
            piece._piece.sprite = GhodeArt.Horse;
            piece._piece.preserveAspect = true;
            piece._piece.raycastTarget = false; // taps must reach the cells below

            piece.HideInstant();
            return piece;
        }

        static void AnchorTopLeft(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Resize the piece for the current board's cell size.</summary>
        public void Configure(float cellSize)
        {
            _cell = cellSize;
            _pieceRt.sizeDelta = new Vector2(cellSize * 0.98f, cellSize * 0.98f);
            _shadowRt.sizeDelta = new Vector2(cellSize * 0.62f, cellSize * 0.24f);
        }

        /// <summary>Vanish immediately (empty board, menu, missing art).</summary>
        public void HideInstant()
        {
            StopAnim();
            _piece.enabled = false;
            _shadow.enabled = false;
        }

        /// <summary>Stand on a square with no animation (reduced motion / repaints).</summary>
        public void SnapTo(Vector2 pos)
        {
            if (!HasArt) { HideInstant(); return; }
            StopAnim();
            _restPos = pos;
            PlacePiece(pos, 0f, 1f, 1f);
            _piece.enabled = true;
            _shadow.enabled = true;
        }

        /// <summary>The first placement: pop in with a small friendly bounce.</summary>
        public void Spawn(Vector2 pos)
        {
            if (!HasArt) { HideInstant(); return; }
            StopAnim();
            _restPos = pos;
            _piece.enabled = true;
            _shadow.enabled = true;
            _anim = StartCoroutine(SpawnRoutine(pos));
        }

        /// <summary>A real knight hop: parabolic arc with the shadow staying grounded.</summary>
        public void HopTo(Vector2 to)
        {
            if (!HasArt) { HideInstant(); return; }
            StopAnim();
            Vector2 from = _restPos;
            _restPos = to;
            _piece.enabled = true;
            _shadow.enabled = true;
            _anim = StartCoroutine(HopRoutine(from, to));
        }

        /// <summary>Undo: a quick low retreat back to the previous square.</summary>
        public void SlideTo(Vector2 to)
        {
            if (!HasArt) { HideInstant(); return; }
            StopAnim();
            Vector2 from = _restPos;
            _restPos = to;
            _piece.enabled = true;
            _shadow.enabled = true;
            _anim = StartCoroutine(SlideRoutine(from, to));
        }

        void StopAnim()
        {
            if (_anim != null)
            {
                StopCoroutine(_anim);
                _anim = null;
            }
        }

        // ------------------------------------------------------------------
        // The routines. All use unscaled time: pausing the game must never
        // freeze a horse in mid-air forever.
        // ------------------------------------------------------------------

        IEnumerator SpawnRoutine(Vector2 pos)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / SpawnSeconds)
            {
                // Scale pops 0.5 → 1.06 → 1 while the alpha fades in early.
                float s = t < 0.75f
                    ? Mathf.Lerp(0.5f, 1.06f, Smooth(t / 0.75f))
                    : Mathf.Lerp(1.06f, 1f, Smooth((t - 0.75f) / 0.25f));
                float alpha = Mathf.Clamp01(t / 0.4f);
                PlacePiece(pos, 0f, s, alpha);
                yield return null;
            }
            PlacePiece(pos, 0f, 1f, 1f);
            _anim = null;
        }

        IEnumerator HopRoutine(Vector2 from, Vector2 to)
        {
            float apex = _cell * ApexPerCell;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / HopSeconds)
            {
                float k = Smooth(Mathf.Clamp01(t));
                Vector2 ground = Vector2.Lerp(from, to, k);
                float height = 4f * apex * k * (1f - k); // parabola, apex mid-hop
                float scale = 1f + 0.08f * 4f * k * (1f - k); // grows slightly at the top

                PlacePiece(ground, height, scale, 1f);
                PlaceShadow(ground, 1f - 0.45f * 4f * k * (1f - k)); // shrinks at apex
                yield return null;
            }
            PlacePiece(to, 0f, 1f, 1f);
            PlaceShadow(to, 1f);
            _anim = null;
        }

        IEnumerator SlideRoutine(Vector2 from, Vector2 to)
        {
            float apex = _cell * 0.18f; // just a hint of lift — this is a retreat
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / SlideSeconds)
            {
                float k = Smooth(Mathf.Clamp01(t));
                Vector2 ground = Vector2.Lerp(from, to, k);
                PlacePiece(ground, 4f * apex * k * (1f - k), 1f, 1f);
                PlaceShadow(ground, 1f);
                yield return null;
            }
            PlacePiece(to, 0f, 1f, 1f);
            PlaceShadow(to, 1f);
            _anim = null;
        }

        // In plain words: the piece hovers `height` above its ground point
        // (which in our y-down anchored space means ADDING to y), sitting a
        // little high in the cell so it reads as standing, not floating.
        void PlacePiece(Vector2 ground, float height, float scale, float alpha)
        {
            _pieceRt.anchoredPosition = new Vector2(ground.x, ground.y + _cell * 0.06f + height);
            _pieceRt.localScale = new Vector3(scale, scale, 1f);
            var c = _piece.color;
            _piece.color = new Color(c.r, c.g, c.b, alpha);

            if (height <= 0.01f) PlaceShadow(ground, 1f);
        }

        void PlaceShadow(Vector2 ground, float scale)
        {
            _shadowRt.anchoredPosition = new Vector2(ground.x, ground.y - _cell * 0.34f);
            _shadowRt.localScale = new Vector3(scale, scale, 1f);
        }

        static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
