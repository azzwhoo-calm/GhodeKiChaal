// PauseOverlay.cs — the dark curtain that drops when the player pauses.
// A dim film blocks the board (no sneaky peeking-taps), and a walnut panel
// offers Resume / Restart / Menu plus the live setting rows: five toggles
// and the theme picker. GameScreen decides WHEN it shows (from
// GameController.IsPaused). The panel sizes itself to its content, so
// adding a row never silently pushes another one off the bottom again.

using UnityEngine;
using UnityEngine.UI;
using Ghode.Core;
using Ghode.Game;

namespace Ghode.UI
{
    /// <summary>
    /// The pause menu overlay. Its toggles write straight to the controller's
    /// settings (which persist immediately) and repaint on StateChanged.
    /// Visibility is owned by GameScreen — this class never SetActives itself.
    /// </summary>
    public class PauseOverlay : MonoBehaviour
    {
        GameController _gc;
        ToggleControl _soundToggle;
        ToggleControl _hapticsToggle;
        ToggleControl _hintsToggle;
        ToggleControl _ambienceToggle;
        ToggleControl _reducedMotionToggle;
        SegmentedControl _themeSelector;

        /// <summary>Create the (initially hidden) overlay over the game screen.</summary>
        public static PauseOverlay Build(RectTransform parent, GameController gc)
        {
            // The film: dim everything and swallow every tap behind the panel.
            var film = UiFactory.CreatePanel("PauseOverlay", parent, UiFactory.Palette.OverlayDim, blocksTaps: true);
            UiFactory.Fill(film.rectTransform);

            var overlay = film.gameObject.AddComponent<PauseOverlay>();
            overlay._gc = gc;

            // The centered walnut panel. Width is fixed; HEIGHT grows to fit
            // whatever rows live inside (ContentSizeFitter + the VStack).
            var panel = UiFactory.CreatePanel("Panel", film.transform, UiFactory.Palette.Walnut, blocksTaps: true);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(860f, 0f); // height comes from content
            UiFactory.VStack(panelRt, 18f, new RectOffset(40, 40, 40, 40));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var title = UiFactory.CreateText("Title", panelRt, "Paused", 64, UiFactory.Palette.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(title, preferredHeight: 90f);

            var resume = UiFactory.CreateButton("ResumeButton", panelRt, "Resume", 48, gc.ResumeGame,
                UiFactory.Palette.Accent, UiFactory.Palette.Walnut);
            UiFactory.Layout(resume, preferredHeight: 140f);

            var restart = UiFactory.CreateButton("RestartButton", panelRt, "Restart", 44, gc.Restart);
            UiFactory.Layout(restart, preferredHeight: 132f);

            var menu = UiFactory.CreateButton("MenuButton", panelRt, "Menu", 44, gc.GoMenu);
            UiFactory.Layout(menu, preferredHeight: 132f);

            Gap(panelRt, 14f);

            var settingsLabel = UiFactory.CreateText("SettingsLabel", panelRt, "Settings", 38,
                UiFactory.Palette.ParchmentDim);
            UiFactory.Layout(settingsLabel, preferredHeight: 50f);

            // Live toggles — each writes through the controller so it saves.
            overlay._soundToggle = ToggleControl.Build(panelRt, "SoundToggle", "Sound", gc.SetSound);
            overlay._hapticsToggle = ToggleControl.Build(panelRt, "HapticsToggle", "Haptics", gc.SetHaptics);
            overlay._hintsToggle = ToggleControl.Build(panelRt, "HintsToggle", "Hints", gc.SetHints);
            overlay._ambienceToggle = ToggleControl.Build(panelRt, "AmbienceToggle", "Ambience", gc.SetAmbience);
            overlay._reducedMotionToggle = ToggleControl.Build(panelRt, "ReducedMotionToggle", "Reduced motion", gc.SetReducedMotion);

            Gap(panelRt, 8f);

            var themeLabel = UiFactory.CreateText("ThemeLabel", panelRt, "Theme", 38,
                UiFactory.Palette.ParchmentDim, TextAnchor.MiddleLeft);
            UiFactory.Layout(themeLabel, preferredHeight: 46f);

            // TODO(azzwhoo): once billing lands, lock Ebony/Marble behind the
            // "Royal Stable" entitlement (show a small padlock on the options).
            // During closed testing all three stay open on purpose.
            overlay._themeSelector = SegmentedControl.Build(panelRt, "ThemeSelector",
                new[] { "Wood", "Ebony", "Marble" },
                index => gc.SetTheme((Theme)index)); // enum order matches button order

            gc.StateChanged += overlay.Refresh;
            overlay.Refresh();
            return overlay;
        }

        // A fixed-height breather between panel sections (the old flexible
        // spacers collapse to zero inside a size-fitted panel).
        static void Gap(RectTransform parent, float height)
        {
            var rt = UiFactory.CreateRect("Gap", parent);
            UiFactory.Layout(rt, preferredHeight: height);
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // Keep the switches and the theme picker honest with the saved settings.
        void Refresh()
        {
            if (_gc == null || _gc.Settings == null) return;
            _soundToggle.SetValue(_gc.Settings.Sound);
            _hapticsToggle.SetValue(_gc.Settings.Haptics);
            _hintsToggle.SetValue(_gc.Settings.Hints);
            _ambienceToggle.SetValue(_gc.Settings.Ambience);
            _reducedMotionToggle.SetValue(_gc.Settings.ReducedMotion);
            _themeSelector.SetSelected((int)_gc.Settings.Theme);
        }
    }
}
