// PauseOverlay.cs — the dark curtain that drops when the player pauses.
// A dim film blocks the board (no sneaky peeking-taps), and a walnut panel
// offers Resume / Restart / Menu plus the three live setting toggles.
// GameScreen decides WHEN it shows (from GameController.IsPaused).

using UnityEngine;
using UnityEngine.UI;
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
        ToggleControl _hintsToggle;
        ToggleControl _ambienceToggle;

        /// <summary>Create the (initially hidden) overlay over the game screen.</summary>
        public static PauseOverlay Build(RectTransform parent, GameController gc)
        {
            // The film: dim everything and swallow every tap behind the panel.
            var film = UiFactory.CreatePanel("PauseOverlay", parent, UiFactory.Palette.OverlayDim, blocksTaps: true);
            UiFactory.Fill(film.rectTransform);

            var overlay = film.gameObject.AddComponent<PauseOverlay>();
            overlay._gc = gc;

            // The centered walnut panel with everything on it.
            var panel = UiFactory.CreatePanel("Panel", film.transform, UiFactory.Palette.Walnut, blocksTaps: true);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(860f, 1100f);
            UiFactory.VStack(panelRt, 18f, new RectOffset(40, 40, 40, 40));

            var title = UiFactory.CreateText("Title", panelRt, "Paused", 64, UiFactory.Palette.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(title, preferredHeight: 90f);

            var resume = UiFactory.CreateButton("ResumeButton", panelRt, "Resume", 48, gc.ResumeGame,
                UiFactory.Palette.Accent, UiFactory.Palette.Walnut);
            UiFactory.Layout(resume, preferredHeight: 120f);

            var restart = UiFactory.CreateButton("RestartButton", panelRt, "Restart", 44, gc.Restart);
            UiFactory.Layout(restart, preferredHeight: 100f);

            var menu = UiFactory.CreateButton("MenuButton", panelRt, "Menu", 44, gc.GoMenu);
            UiFactory.Layout(menu, preferredHeight: 100f);

            UiFactory.Spacer(panelRt, 0.3f);

            var settingsLabel = UiFactory.CreateText("SettingsLabel", panelRt, "Settings", 38,
                UiFactory.Palette.ParchmentDim);
            UiFactory.Layout(settingsLabel, preferredHeight: 50f);

            // Live toggles — each writes through the controller so it saves.
            overlay._soundToggle = ToggleControl.Build(panelRt, "SoundToggle", "Sound", gc.SetSound);
            overlay._hintsToggle = ToggleControl.Build(panelRt, "HintsToggle", "Hints", gc.SetHints);
            overlay._ambienceToggle = ToggleControl.Build(panelRt, "AmbienceToggle", "Ambience", gc.SetAmbience);

            UiFactory.Spacer(panelRt, 1f);

            gc.StateChanged += overlay.Refresh;
            overlay.Refresh();
            return overlay;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // Keep the three switches honest with the saved settings.
        void Refresh()
        {
            if (_gc == null || _gc.Settings == null) return;
            _soundToggle.SetValue(_gc.Settings.Sound);
            _hintsToggle.SetValue(_gc.Settings.Hints);
            _ambienceToggle.SetValue(_gc.Settings.Ambience);
        }
    }
}
