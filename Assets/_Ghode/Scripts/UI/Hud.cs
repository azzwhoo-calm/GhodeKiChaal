// Hud.cs — the strip across the top of the game screen.
// Top row: the live numbers (board size, filled count, moves, clock, hints).
// Bottom row: the action buttons (Hint / Undo / Restart / Sound / Menu).
// It repaints on StateChanged and ticks the clock text every frame it is visible.

using UnityEngine;
using UnityEngine.UI;
using Ghode.Game;
using Ghode.Utils;

namespace Ghode.UI
{
    /// <summary>
    /// Heads-up display for the game screen. Reads everything from
    /// <see cref="GameController"/>; its buttons call controller actions.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        GameController _gc;

        Text _sizeText;
        Text _filledText;
        Text _movesText;
        Text _timerText;
        Text _hintsText;
        Button _hintButton;
        Text _soundLabel; // the Sound button's label flips between Sound/Muted

        /// <summary>Build the HUD strip across the top of the game screen.</summary>
        public static Hud Build(RectTransform parent, GameController gc)
        {
            // Anchored to the top edge, stretching the full width.
            var rt = UiFactory.CreateRect("HUD", parent);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(20f, -300f); // 300 ref-px tall, 20 px side margins
            rt.offsetMax = new Vector2(-20f, 0f);

            var hud = rt.gameObject.AddComponent<Hud>();
            hud._gc = gc;

            UiFactory.VStack(rt, 10f, new RectOffset(6, 6, 16, 6));

            // ---- Row 1: the numbers ----------------------------------------
            var stats = UiFactory.CreateRect("Stats", rt);
            UiFactory.Layout(stats, preferredHeight: 80f);
            UiFactory.HStack(stats, 8f, new RectOffset(0, 0, 0, 0));

            hud._sizeText = StatText(stats, "Size");
            hud._filledText = StatText(stats, "Filled");
            hud._movesText = StatText(stats, "Moves");
            hud._timerText = StatText(stats, "Timer");
            hud._hintsText = StatText(stats, "Hints");

            // ---- Row 2: the buttons ----------------------------------------
            var buttons = UiFactory.CreateRect("Buttons", rt);
            UiFactory.Layout(buttons, preferredHeight: 120f);
            UiFactory.HStack(buttons, 14f, new RectOffset(0, 0, 0, 0));

            hud._hintButton = UiFactory.CreateButton("HintButton", buttons, "Hint", 40, gc.UseHint);
            UiFactory.CreateButton("UndoButton", buttons, "Undo", 40, gc.Undo);
            UiFactory.CreateButton("RestartButton", buttons, "Restart", 40, gc.Restart);

            var soundButton = UiFactory.CreateButton("SoundButton", buttons, "Sound", 40,
                () => gc.SetSound(!gc.Settings.Sound)); // flip the mute setting
            hud._soundLabel = soundButton.GetComponentInChildren<Text>();

            UiFactory.CreateButton("MenuButton", buttons, "Menu", 40, gc.GoMenu);

            gc.StateChanged += hud.Refresh;
            hud.Refresh();
            return hud;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        void Update()
        {
            // The clock is the only thing that changes without a StateChanged,
            // so it alone updates every frame (only while the HUD is visible).
            if (_gc != null && _gc.Board != null)
            {
                _timerText.text = TimeFormat.ToClock(_gc.Timer.ReadMs());
            }
        }

        // Repaint the numbers and button states from the controller.
        void Refresh()
        {
            if (_gc == null || _gc.Board == null) return;
            var board = _gc.Board;

            _sizeText.text = board.Size + "×" + board.Size; // e.g. "5×5"
            _filledText.text = "Filled " + board.VisitedCount + "/" + board.TotalCells;
            _movesText.text = "Moves " + Mathf.Max(0, board.VisitedCount - 1); // hops, not squares
            _hintsText.text = "Hints " + board.HintsUsed;
            _timerText.text = TimeFormat.ToClock(_gc.Timer.ReadMs());

            // The Hint button only exists while the setting allows hints.
            _hintButton.gameObject.SetActive(_gc.Settings.Hints);
            _soundLabel.text = _gc.Settings.Sound ? "Sound" : "Muted";
        }

        // One equally-stretched stat label for the top row.
        static Text StatText(RectTransform row, string name)
        {
            var text = UiFactory.CreateText(name, row, "", 36, UiFactory.Palette.Parchment);
            UiFactory.Layout(text, flexibleWidth: 1f);
            return text;
        }
    }
}
