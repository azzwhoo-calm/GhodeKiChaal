// GameController.cs — the top-level brain (the Unity twin of our web App.jsx).
// It owns THE game state: which page is showing, the board, the settings, the
// records and the timer. Buttons and cells never change state themselves —
// they call a public action here, the state changes in one place, and then a
// single StateChanged event tells every view to repaint itself.

using System;
using UnityEngine;
using Ghode.Audio;
using Ghode.Core;
using Ghode.Data;
using AppScreen = Ghode.Core.Screen; // our enum; alias avoids UnityEngine.Screen clashes

namespace Ghode.Game
{
    /// <summary>
    /// The single source of truth for the whole game. All player actions
    /// (NewGame, TryMove, Undo, UseHint, …) enter here; views subscribe to
    /// <see cref="StateChanged"/> and re-read whatever they need afterwards.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        /// <summary>The player's persisted choices (board size, sound, …).</summary>
        public Settings Settings { get; private set; }

        /// <summary>Best times and recent-game history, persisted.</summary>
        public Records Records { get; private set; }

        /// <summary>The board being played right now (null until the first NewGame).</summary>
        public BoardState Board { get; private set; }

        /// <summary>The pause-safe stopwatch for the current game.</summary>
        public GameTimer Timer { get; } = new GameTimer();

        /// <summary>Which full-screen page is showing.</summary>
        public AppScreen CurrentScreen { get; private set; } = AppScreen.Menu;

        /// <summary>The square the Hint button suggested, until the next hop clears it.</summary>
        public (int r, int c)? ActiveHint { get; private set; }

        /// <summary>Is the pause overlay up? (Cell taps are ignored while paused.)</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Did the game that just ended set a new best time?</summary>
        public bool LastGameWasNewBest { get; private set; }

        /// <summary>The sound desk. Set once by <see cref="Init"/>.</summary>
        public AudioManager Audio { get; private set; }

        /// <summary>Fired after ANY state change. Views repaint when they hear it.</summary>
        public event Action StateChanged;

        ScreenManager _screens;

        // In plain words: when the horse gets stuck we do not record the loss
        // yet — the player may still Undo out of it. This flag remembers the
        // "unpaid" loss until they abandon (Restart/Menu/New Game) or escape.
        bool _pendingLoss;

        /// <summary>
        /// Step 1 of startup (called by GameBootstrap): load saves and hook up audio.
        /// </summary>
        public void Init(AudioManager audio)
        {
            Audio = audio;
            Settings = SettingsStore.Load();
            Records = RecordsStore.Load();

            // Make the sound desk match the loaded settings straight away.
            Audio.SetMuted(!Settings.Sound);
            Audio.SetAmbience(Settings.Ambience);
        }

        /// <summary>
        /// Step 2 of startup: hand over the screen switcher and land on the Menu.
        /// </summary>
        public void AttachScreens(ScreenManager screens)
        {
            _screens = screens;
            GoTo(AppScreen.Menu);
        }

        // ------------------------------------------------------------------
        // Game lifecycle
        // ------------------------------------------------------------------

        /// <summary>Start a fresh game on a board of the given size and show it.</summary>
        public void NewGame(int size)
        {
            RecordPendingLossIfAny();

            Board = new BoardState(size);
            ActiveHint = null;
            IsPaused = false;
            LastGameWasNewBest = false;
            _pendingLoss = false;
            Timer.Reset(); // the clock starts on placement, not here

            Audio.Play(Sfx.Click);
            GoTo(AppScreen.Playing);
        }

        /// <summary>Throw away the current attempt and restart on the same size.</summary>
        public void Restart()
        {
            if (Board == null) return;
            NewGame(Board.Size);
        }

        /// <summary>
        /// A cell was tapped. Routes to placement or to a hop depending on phase.
        /// This is the ONLY entry point BoardView uses.
        /// </summary>
        public void OnCellTapped(int r, int c)
        {
            if (CurrentScreen != AppScreen.Playing || IsPaused || Board == null) return;

            if (Board.Phase == Phase.Placing) PlaceStart(r, c);
            else if (Board.Phase == Phase.Playing) TryMove(r, c);
            // Won/Lost: taps on the board do nothing — the banner/result buttons take over.
        }

        /// <summary>First tap: set the horse down and start the clock.</summary>
        public void PlaceStart(int r, int c)
        {
            if (Board == null || !Board.PlaceStart(r, c))
            {
                Audio.Play(Sfx.InvalidThud);
                return;
            }

            Timer.Reset();
            Timer.Start();
            Audio.Play(Sfx.SetDown);
            RaiseChanged();
        }

        /// <summary>
        /// Try to hop to (r, c). Legal hops advance the game (and may win or
        /// strand it); illegal taps just thud and change nothing.
        /// </summary>
        public void TryMove(int r, int c)
        {
            if (Board == null) return;

            if (!Board.ApplyMove(r, c))
            {
                // In plain words: the board said no — wrong shape or a used square.
                Audio.Play(Sfx.InvalidThud);
                return;
            }

            ActiveHint = null; // any suggestion is stale after a real hop

            if (Board.Phase == Phase.Won)
            {
                Timer.Pause();
                RecordFinished(won: true);
                Audio.Play(Sfx.Win);
                GoTo(AppScreen.Result);
                return;
            }

            if (Board.Phase == Phase.Lost)
            {
                // Stuck! We stay on the game screen (the board must stay visible
                // so the player can retrace). GameScreen shows the Stuck banner.
                // The clock keeps running — they are still "in" the attempt.
                _pendingLoss = true;
                Audio.Play(Sfx.Lose);
                RaiseChanged();
                return;
            }

            Audio.Play(Sfx.Hop);
            RaiseChanged();
        }

        /// <summary>Take back the last hop. Also the escape hatch out of Stuck.</summary>
        public void Undo()
        {
            if (Board == null) return;

            bool wasLost = Board.Phase == Phase.Lost;
            if (!Board.Undo()) return;

            ActiveHint = null;

            // Escaping a dead end? Then the loss never "happened".
            if (wasLost && Board.Phase == Phase.Playing) _pendingLoss = false;

            if (Board.Phase == Phase.Placing)
            {
                Timer.Reset(); // undid the placement itself — clock waits again
            }
            else if (Board.Phase == Phase.Playing && !Timer.IsRunning)
            {
                Timer.Start(); // e.g. undoing right after a pause-then-stuck detour
            }

            // "Undo & keep trying" pressed on the Result screen must also carry
            // the player back to the board itself.
            if (CurrentScreen == AppScreen.Result && Board.Phase == Phase.Playing)
            {
                GoTo(AppScreen.Playing);
            }

            Audio.Play(Sfx.Click);
            RaiseChanged();
        }

        /// <summary>
        /// Spend a hint: highlight Warnsdorff's recommended hop and count it.
        /// Does nothing when hints are disabled or there is nothing to suggest.
        /// </summary>
        public void UseHint()
        {
            if (Board == null || Board.Phase != Phase.Playing || !Settings.Hints) return;

            var best = KnightLogic.WarnsdorffBest(Board);
            if (best == null) return;

            Board.NoteHintUsed();
            ActiveHint = best;
            Audio.Play(Sfx.Click);
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // Navigation
        // ------------------------------------------------------------------

        /// <summary>Back to the main menu (pauses the clock, settles any stuck loss).</summary>
        public void GoMenu()
        {
            RecordPendingLossIfAny();
            Timer.Pause();
            IsPaused = false;
            Audio.Play(Sfx.Click);
            GoTo(AppScreen.Menu);
        }

        /// <summary>Show the how-to-play page.</summary>
        public void OpenInstructions()
        {
            Audio.Play(Sfx.Click);
            GoTo(AppScreen.Instructions);
        }

        /// <summary>Leave the how-to-play page, back to the menu.</summary>
        public void CloseInstructions()
        {
            Audio.Play(Sfx.Click);
            GoTo(AppScreen.Menu);
        }

        /// <summary>Freeze the game and show the pause overlay.</summary>
        public void PauseGame()
        {
            PauseGame(playSound: true);
        }

        // The silent flavor exists for backgrounding: pausing because the OS
        // took the screen away should not click at the player.
        void PauseGame(bool playSound)
        {
            if (CurrentScreen != AppScreen.Playing || IsPaused) return;
            IsPaused = true;
            Timer.Pause();
            if (playSound) Audio.Play(Sfx.Click);
            RaiseChanged();
        }

        /// <summary>Close the pause overlay and let the clock run again.</summary>
        public void ResumeGame()
        {
            if (!IsPaused) return;
            IsPaused = false;
            if (Board != null && Board.Phase == Phase.Playing && Board.Current != null)
            {
                Timer.Start();
            }
            Audio.Play(Sfx.Click);
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // Settings — each setter saves immediately and repaints the UI
        // ------------------------------------------------------------------

        /// <summary>Pick the board size used by the NEXT new game.</summary>
        public void SetBoardSize(int size)
        {
            Settings.BoardSize = size;
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>Pick how much highlighting help the player gets.</summary>
        public void SetDifficulty(Difficulty difficulty)
        {
            Settings.Difficulty = difficulty;
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>Sound effects on/off (also mutes ambience).</summary>
        public void SetSound(bool on)
        {
            Settings.Sound = on;
            Audio.SetMuted(!on);
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>Allow or hide the Hint button.</summary>
        public void SetHints(bool on)
        {
            Settings.Hints = on;
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>Background ambience loop on/off.</summary>
        public void SetAmbience(bool on)
        {
            Settings.Ambience = on;
            Audio.SetAmbience(on);
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>Accessibility: snap the horse instead of animating hops.</summary>
        public void SetReducedMotion(bool on)
        {
            Settings.ReducedMotion = on;
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        // ------------------------------------------------------------------
        // App lifecycle (Android home button, calls, task switching, kill)
        // ------------------------------------------------------------------

        /// <summary>
        /// The OS is taking us to the background (home button, incoming call,
        /// task switch). Freeze the attempt exactly where it is: the pause
        /// overlay comes up (silently) and the clock stops, so ten minutes on
        /// WhatsApp costs the player zero seconds. Settings and records are
        /// already on disk — they save at every change.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (!paused) return; // resuming is the player's move (Resume button)
            if (CurrentScreen == AppScreen.Playing && Board != null
                && Board.Phase == Phase.Playing && Board.Current != null)
            {
                PauseGame(playSound: false);
            }
        }

        /// <summary>
        /// The app is shutting down for real. A stuck game the player never
        /// resolved counts as a loss — settle it now while we still can.
        /// (An OS force-kill can skip this callback; that one dropped loss is
        /// an accepted gap — see the note in RecordPendingLossIfAny.)
        /// </summary>
        void OnApplicationQuit()
        {
            RecordPendingLossIfAny();
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        void GoTo(AppScreen screen)
        {
            CurrentScreen = screen;
            _screens?.Show(screen);
            RaiseChanged();
        }

        void RaiseChanged()
        {
            StateChanged?.Invoke();
        }

        // A stuck game that the player walks away from counts as a loss.
        // Walking away includes quitting the app (OnApplicationQuit above).
        // Known accepted gap: an OS force-kill while stuck skips every
        // callback, so that one loss is dropped — persisting a "pending loss"
        // flag was judged not worth the save-file churn for a puzzle game.
        void RecordPendingLossIfAny()
        {
            if (!_pendingLoss || Board == null) return;
            _pendingLoss = false;
            Timer.Pause();
            RecordFinished(won: false);
        }

        void RecordFinished(bool won)
        {
            // TODO(azzwhoo): confirm these fields against the Save-Data Spec doc
            // (names must match the web version so records feel continuous).
            var record = new GameRecord
            {
                boardSize = Board.Size,
                won = won,
                timeMs = Timer.ReadMs(),
                moves = Math.Max(0, Board.VisitedCount - 1), // hops, not squares
                hintsUsed = Board.HintsUsed,
                playedAtIso = DateTime.UtcNow.ToString("o")
            };

            LastGameWasNewBest = Records.RecordGame(record) && won;
            RecordsStore.Save(Records);
        }
    }
}
