// MainMenuScreen.cs — the front door of the game.
// Pick a board size, pick a difficulty, start a new game, read the rules, and
// admire your best times. Every choice goes straight to GameController (which
// saves it), and the screen repaints itself from Settings on StateChanged.

using System;
using UnityEngine;
using UnityEngine.UI;
using Ghode.Core;
using Ghode.Game;
using Ghode.Utils;

namespace Ghode.UI
{
    /// <summary>
    /// The main menu: board-size selector, difficulty selector, New Game,
    /// How to Play, and the best-times display. Built entirely in code.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        GameController _gc;
        SegmentedControl _sizeSelector;
        SegmentedControl _difficultySelector;
        Text _bestTimesText;

        /// <summary>Create the whole menu under the safe area and wire it up.</summary>
        public static MainMenuScreen Build(RectTransform parent, GameController gc)
        {
            var root = UiFactory.CreatePanel("MenuScreen", parent, UiFactory.Palette.WalnutDeep);
            UiFactory.Fill(root.rectTransform);

            var screen = root.gameObject.AddComponent<MainMenuScreen>();
            screen._gc = gc;
            screen.BuildUi(root.rectTransform);

            gc.StateChanged += screen.Refresh;
            screen.Refresh();
            return screen;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // Lay out the menu column, top to bottom.
        void BuildUi(RectTransform root)
        {
            UiFactory.VStack(root, 22f, new RectOffset(90, 90, 40, 40));

            UiFactory.Spacer(root, 1f);

            var title = UiFactory.CreateText("Title", root, "Ghode Ki Chaal", 96, UiFactory.Palette.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(title, preferredHeight: 120f);

            var subtitle = UiFactory.CreateText("Subtitle", root, "The Horse Tour — a Knight's Tour puzzle",
                40, UiFactory.Palette.ParchmentDim);
            UiFactory.Layout(subtitle, preferredHeight: 60f);

            UiFactory.Spacer(root, 0.5f);

            // --- Board size -------------------------------------------------
            var sizeLabel = UiFactory.CreateText("BoardSizeLabel", root, "Board size", 38,
                UiFactory.Palette.ParchmentDim, TextAnchor.MiddleLeft);
            UiFactory.Layout(sizeLabel, preferredHeight: 46f);

            _sizeSelector = SegmentedControl.Build(root, "BoardSizeSelector",
                new[] { "5 × 5", "6 × 6", "7 × 7", "8 × 8" },
                index => _gc.SetBoardSize(KnightLogic.BoardSizes[index])); // index → real size

            // --- Difficulty -------------------------------------------------
            var difficultyLabel = UiFactory.CreateText("DifficultyLabel", root, "Difficulty", 38,
                UiFactory.Palette.ParchmentDim, TextAnchor.MiddleLeft);
            UiFactory.Layout(difficultyLabel, preferredHeight: 46f);

            _difficultySelector = SegmentedControl.Build(root, "DifficultySelector",
                new[] { "Apprentice", "Knight", "Master" },
                index => _gc.SetDifficulty((Difficulty)index)); // enum order matches button order

            UiFactory.Spacer(root, 0.5f);

            // --- Actions ----------------------------------------------------
            var newGame = UiFactory.CreateButton("NewGameButton", root, "New Game", 52,
                () => _gc.NewGame(_gc.Settings.BoardSize),
                UiFactory.Palette.Accent, UiFactory.Palette.Walnut); // the big golden go button
            UiFactory.Layout(newGame, preferredHeight: 140f);

            var instructions = UiFactory.CreateButton("InstructionsButton", root, "How to Play", 44,
                _gc.OpenInstructions);
            UiFactory.Layout(instructions, preferredHeight: 132f);

            UiFactory.Spacer(root, 0.5f);

            // --- Best times -------------------------------------------------
            var bestTitle = UiFactory.CreateText("BestTimesTitle", root, "Best times", 38,
                UiFactory.Palette.ParchmentDim);
            UiFactory.Layout(bestTitle, preferredHeight: 46f);

            _bestTimesText = UiFactory.CreateText("BestTimes", root, "", 42, UiFactory.Palette.Parchment);
            UiFactory.Layout(_bestTimesText, preferredHeight: 210f); // four rows now (7×7 joined)

            UiFactory.Spacer(root, 1f);
        }

        // Repaint from the saved settings and records.
        void Refresh()
        {
            if (_gc == null || _gc.Settings == null) return;

            // Which segmented button matches the saved board size?
            int sizeIndex = Array.IndexOf(KnightLogic.BoardSizes, _gc.Settings.BoardSize);
            if (sizeIndex < 0) sizeIndex = 0; // unknown saved size — show the default
            _sizeSelector.SetSelected(sizeIndex);
            _difficultySelector.SetSelected((int)_gc.Settings.Difficulty);

            // One line of best time per board size, dash when never won.
            string lines = "";
            foreach (int size in KnightLogic.BoardSizes)
            {
                long best = _gc.Records.BestTimeFor(size);
                string time = best >= 0 ? TimeFormat.ToClockPrecise(best) : "—";
                lines += size + "×" + size + "   " + time + "\n";
            }
            _bestTimesText.text = lines.TrimEnd('\n');
        }
    }
}
