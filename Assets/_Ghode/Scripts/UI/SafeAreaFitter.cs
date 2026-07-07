// SafeAreaFitter.cs — keeps the UI out of the phone's notch and gesture bar.
// Sits on one RectTransform directly under the Canvas; everything else lives
// inside it. It reads Screen.safeArea and shrinks itself to match, re-checking
// whenever the reported safe area changes (rotation, foldables, editor resize).

using UnityEngine;

namespace Ghode.UI
{
    /// <summary>
    /// Pins this RectTransform to the device's safe area. Purely visual
    /// plumbing: no game logic, no dependencies, just anchor math.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rt;
        Rect _applied = new Rect(-1f, -1f, -1f, -1f); // impossible value forces first apply

        void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            // In plain words: if the phone reports a different safe area than
            // the one we shaped ourselves to, reshape. Cheap comparison per frame.
            if (Screen.safeArea != _applied) Apply();
        }

        void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;

            if (Screen.width <= 0 || Screen.height <= 0) return; // editor edge case

            // Convert the safe area from pixels into 0..1 anchor fractions.
            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
