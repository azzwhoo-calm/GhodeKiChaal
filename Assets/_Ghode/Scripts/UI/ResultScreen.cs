// ResultScreen.cs — the end-of-game page: how did it go, how fast, and what next.
// On a win: "Tour Complete!", your time, the best time, and a NEW BEST badge
// when earned. It also carries a full loss branch (message + "Undo & keep
// trying") even though losses currently show the in-game Stuck banner instead.

using UnityEngine;
using UnityEngine.UI;
using Ghode.Core;
using Ghode.Game;
using Ghode.Utils;

namespace Ghode.UI
{
    /// <summary>
    /// Win/lose summary screen: result message, time, best time, new-best flag,
    /// and the Play Again / Menu buttons (+ Undo &amp; keep trying on a loss).
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        GameController _gc;
        Text _title;
        Text _timeText;
        Text _bestText;
        Text _newBestBadge;
        Button _undoButton;

        /// <summary>Create the result page under the safe area.</summary>
        public static ResultScreen Build(RectTransform parent, GameController gc)
        {
            var root = UiFactory.CreatePanel("ResultScreen", parent, UiFactory.Palette.WalnutDeep);
            UiFactory.Fill(root.rectTransform);

            var screen = root.gameObject.AddComponent<ResultScreen>();
            screen._gc = gc;

            UiFactory.VStack(root.rectTransform, 24f, new RectOffset(90, 90, 60, 60));

            UiFactory.Spacer(root.rectTransform, 1f);

            screen._title = UiFactory.CreateText("Title", root.transform, "", 84, UiFactory.Palette.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(screen._title, preferredHeight: 120f);

            screen._timeText = UiFactory.CreateText("Time", root.transform, "", 52, UiFactory.Palette.Parchment);
            UiFactory.Layout(screen._timeText, preferredHeight: 80f);

            screen._bestText = UiFactory.CreateText("Best", root.transform, "", 44, UiFactory.Palette.ParchmentDim);
            UiFactory.Layout(screen._bestText, preferredHeight: 70f);

            screen._newBestBadge = UiFactory.CreateText("NewBest", root.transform, "NEW BEST TIME!", 48,
                UiFactory.Palette.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(screen._newBestBadge, preferredHeight: 70f);

            UiFactory.Spacer(root.rectTransform, 0.4f);

            var playAgain = UiFactory.CreateButton("PlayAgainButton", root.transform, "Play Again", 50,
                gc.Restart, UiFactory.Palette.Accent, UiFactory.Palette.Walnut);
            UiFactory.Layout(playAgain, preferredHeight: 140f);

            // Loss-only escape hatch: hop back one move and return to the board.
            screen._undoButton = UiFactory.CreateButton("UndoKeepTryingButton", root.transform,
                "Undo & keep trying", 44, gc.Undo);
            UiFactory.Layout(screen._undoButton, preferredHeight: 132f);

            var menu = UiFactory.CreateButton("MenuButton", root.transform, "Menu", 44, gc.GoMenu);
            UiFactory.Layout(menu, preferredHeight: 132f);

            UiFactory.Spacer(root.rectTransform, 1f);

            gc.StateChanged += screen.Refresh;
            screen.Refresh();
            return screen;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // Fill in the story of the game that just ended.
        void Refresh()
        {
            var board = _gc.Board;
            if (board == null) return; // no game has happened yet

            bool won = board.Phase == Phase.Won;

            _title.text = won ? "Tour Complete!" : "Stuck this time!";
            _timeText.text = "Your time: " + TimeFormat.ToClockPrecise(_gc.Timer.ReadMs());

            long best = _gc.Records.BestTimeFor(board.Size);
            _bestText.text = "Best for " + board.Size + "×" + board.Size + ": "
                + (best >= 0 ? TimeFormat.ToClockPrecise(best) : "—");

            _newBestBadge.gameObject.SetActive(won && _gc.LastGameWasNewBest);

            // TODO(azzwhoo): today this screen only appears on a WIN (losses show
            // the in-game Stuck banner); this loss branch stands ready for a
            // future "Give up" flow that routes to Result instead.
            _undoButton.gameObject.SetActive(!won);
        }
    }
}
