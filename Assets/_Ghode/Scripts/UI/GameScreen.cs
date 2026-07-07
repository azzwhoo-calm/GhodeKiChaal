// GameScreen.cs — the page where the actual puzzle happens.
// It hosts the HUD (top), the BoardView (middle), a helper line while placing,
// a Pause button (bottom), the non-blocking "Stuck!" banner, and the pause
// overlay. It owns no game logic — it only shows/hides its parts per state.

using UnityEngine;
using UnityEngine.UI;
using Ghode.Core;
using Ghode.Game;

namespace Ghode.UI
{
    /// <summary>
    /// The in-game screen: BoardView + Hud + pause button + Stuck banner +
    /// PauseOverlay. Repaints its parts' visibility on every StateChanged.
    /// </summary>
    public class GameScreen : MonoBehaviour
    {
        GameController _gc;
        GameObject _stuckBanner;
        PauseOverlay _pauseOverlay;
        Text _placeHint;

        /// <summary>Create the game screen and everything on it.</summary>
        public static GameScreen Build(RectTransform parent, GameController gc)
        {
            var root = UiFactory.CreatePanel("GameScreen", parent, UiFactory.Palette.WalnutDeep);
            UiFactory.Fill(root.rectTransform);

            var screen = root.gameObject.AddComponent<GameScreen>();
            screen._gc = gc;

            // --- The two big players: HUD strip and the board ---------------
            Hud.Build(root.rectTransform, gc);
            BoardView.Build(root.rectTransform, gc);

            // --- "What do I do?" helper, shown only before placement --------
            screen._placeHint = UiFactory.CreateText("PlaceHint", root.transform,
                "Tap any square to set your horse down", 40, UiFactory.Palette.ParchmentDim);
            var hintRt = (RectTransform)screen._placeHint.transform;
            hintRt.anchorMin = new Vector2(0.5f, 0.5f);
            hintRt.anchorMax = new Vector2(0.5f, 0.5f);
            hintRt.sizeDelta = new Vector2(940f, 70f);
            hintRt.anchoredPosition = new Vector2(0f, -610f); // sits just under the board

            // --- Pause button along the bottom edge -------------------------
            var pause = UiFactory.CreateButton("PauseButton", root.transform, "Pause", 44, gc.PauseGame);
            var pauseRt = (RectTransform)pause.transform;
            pauseRt.anchorMin = new Vector2(0f, 0f);
            pauseRt.anchorMax = new Vector2(1f, 0f);
            pauseRt.pivot = new Vector2(0.5f, 0f);
            pauseRt.offsetMin = new Vector2(90f, 40f);
            pauseRt.offsetMax = new Vector2(-90f, 150f);

            // --- The non-blocking "Stuck!" banner ----------------------------
            // In plain words: when the horse has no hop left we do NOT wipe the
            // board or steal the screen — this banner slides in above the pause
            // button and offers the three ways out.
            var banner = UiFactory.CreatePanel("StuckBanner", root.transform, UiFactory.Palette.Walnut, blocksTaps: true);
            var bannerRt = banner.rectTransform;
            bannerRt.anchorMin = new Vector2(0f, 0f);
            bannerRt.anchorMax = new Vector2(1f, 0f);
            bannerRt.pivot = new Vector2(0.5f, 0f);
            bannerRt.offsetMin = new Vector2(40f, 180f);
            bannerRt.offsetMax = new Vector2(-40f, 480f);
            UiFactory.VStack(bannerRt, 14f, new RectOffset(24, 24, 22, 22));

            var stuckText = UiFactory.CreateText("StuckText", bannerRt,
                "Stuck! No legal hops left — your trail is still on the board.",
                42, UiFactory.Palette.Parchment);
            UiFactory.Layout(stuckText, preferredHeight: 110f);

            var stuckButtons = UiFactory.CreateRect("StuckButtons", bannerRt);
            UiFactory.Layout(stuckButtons, preferredHeight: 110f);
            UiFactory.HStack(stuckButtons, 14f, new RectOffset(0, 0, 0, 0));

            UiFactory.CreateButton("StuckUndo", stuckButtons, "Undo & keep trying", 34, gc.Undo,
                UiFactory.Palette.Accent, UiFactory.Palette.Walnut); // the friendly default
            UiFactory.CreateButton("StuckRestart", stuckButtons, "Restart", 34, gc.Restart);
            UiFactory.CreateButton("StuckMenu", stuckButtons, "Menu", 34, gc.GoMenu);

            screen._stuckBanner = banner.gameObject;

            // --- Pause overlay built LAST so it draws over everything --------
            screen._pauseOverlay = PauseOverlay.Build(root.rectTransform, gc);

            gc.StateChanged += screen.Refresh;
            screen.Refresh();
            return screen;
        }

        void OnDestroy()
        {
            if (_gc != null) _gc.StateChanged -= Refresh;
        }

        // Show/hide the situational parts. The HUD and board repaint themselves.
        void Refresh()
        {
            var board = _gc.Board;
            _placeHint.gameObject.SetActive(board != null && board.Phase == Phase.Placing);
            _stuckBanner.SetActive(board != null && board.Phase == Phase.Lost);
            _pauseOverlay.gameObject.SetActive(_gc.IsPaused);
        }
    }
}
