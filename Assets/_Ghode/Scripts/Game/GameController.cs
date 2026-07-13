// GameController.cs — the top-level brain (the Unity twin of our web App.jsx).
// It owns THE game state: which page is showing, the board, the settings, the
// records and the timer. Buttons and cells never change state themselves —
// they call a public action here, the state changes in one place, and then a
// single StateChanged event tells every view to repaint itself.

using System;
using UnityEngine;
using Ghode.Analytics;
using Ghode.Audio;
using Ghode.Core;
using Ghode.Data;
using Ghode.Haptics;
using Ghode.Monetization;
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

        /// <summary>The player's purchases (cached; billing refreshes it).</summary>
        public Entitlements Entitlements { get; private set; }

        /// <summary>Real-money store wrapper. Null until <see cref="ConnectServices"/>.</summary>
        public BillingService Billing { get; private set; }

        /// <summary>Interstitial orchestration (policy + provider seam).</summary>
        public AdsService Ads { get; } = new AdsService();

        /// <summary>Per-size best-time leaderboards with the offline queue.</summary>
        public LeaderboardService Leaderboards { get; private set; }

        /// <summary>The event diary (debug backend now, Firebase later).</summary>
        public AnalyticsService Analytics { get; } =
            new AnalyticsService(new DebugAnalyticsBackend());

        /// <summary>Does the player own the Royal Stable (themes + no ads)?</summary>
        public bool RoyalStableOwned => Entitlements != null && Entitlements.royalStable;

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
            Entitlements = EntitlementsStore.Load();

            // A locked theme in the save without the purchase to back it
            // (refund? copied save?) quietly falls back to Wood.
            if (Settings.Theme != Theme.Wood && !RoyalStableOwned)
            {
                Settings.Theme = Theme.Wood;
                SettingsStore.Save(Settings);
            }

            // Make the sound desk match the loaded settings straight away,
            // and the same for the vibration desk and the board's costume.
            Audio.SetMuted(!Settings.Sound);
            Audio.SetAmbience(Settings.Ambience);
            HapticsService.Enabled = Settings.Haptics;
            UI.GhodeTheme.Current = Settings.Theme;

            // The offline-friendly services exist from the start; the store
            // connection itself waits for ConnectServices (play mode only).
            Leaderboards = new LeaderboardService(new LocalLeaderboardBackend());
            Ads.Initialize(RoyalStableOwned, () => new FakeInterstitialProvider());
            Analytics.SessionStart(Ads.Policy.SessionNumber);
        }

        /// <summary>
        /// Step 1½ of startup, PLAY MODE ONLY (GameBootstrap calls it; tests
        /// never do): connect to the real-money store and sign in to the
        /// leaderboards. Everything stays playable if either never answers.
        /// </summary>
        public void ConnectServices()
        {
            Billing = new BillingService();
            Billing.OnEntitlementChanged += ApplyEntitlement;
            Billing.Connect();

            // Optional + silent; success flushes the offline score queue.
            Leaderboards.SignIn(ok => Analytics.SignIn(ok));
        }

        /// <summary>
        /// The Royal Stable's delivery point (billing calls this; so do
        /// tests). Persists the entitlement, kills ads, and repaints — the
        /// "instant themes" moment.
        /// </summary>
        public void ApplyEntitlement(bool owned)
        {
            if (Entitlements.royalStable == owned) return;

            Entitlements.royalStable = owned;
            EntitlementsStore.Save(Entitlements);

            if (owned)
            {
                Ads.OnEntitlementGranted();
                Analytics.Iap("completed");
            }
            RaiseChanged();
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
            // Is the player leaving a STUCK board right now? That moment
            // stays ad-free by design, even though the round did finish.
            bool leavingStuck = Board != null && Board.Phase == Phase.Lost;
            RecordPendingLossIfAny();

            // The between-games moment — the only point an interstitial may
            // ever play. AdsPolicy applies every cap; usually this is a no-op.
            int roundsBanked = Ads.Policy != null ? Ads.Policy.RoundsSinceAd : 0;
            if (Ads.TryShowInterstitial(leavingStuck))
            {
                Analytics.AdShown(roundsBanked);
            }

            Board = new BoardState(size);
            ActiveHint = null;
            IsPaused = false;
            LastGameWasNewBest = false;
            _pendingLoss = false;
            Timer.Reset(); // the clock starts on placement, not here

            Audio.Play(Sfx.Click);
            Analytics.GameStart(size, Settings.Difficulty.ToString());
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
            HapticsService.Play(Haptic.Tick);
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
                HapticsService.Play(Haptic.Reject);
                return;
            }

            ActiveHint = null; // any suggestion is stale after a real hop

            if (Board.Phase == Phase.Won)
            {
                Timer.Pause();
                RecordFinished(won: true);
                Audio.Play(Sfx.Win);
                HapticsService.Play(Haptic.Win);
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
                HapticsService.Play(Haptic.Lose);
                RaiseChanged();
                return;
            }

            Audio.Play(Sfx.Hop);
            HapticsService.Play(Haptic.Tick);
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
            Analytics.HintUsed(Board.Size);
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

        /// <summary>Vibration feedback on/off.</summary>
        public void SetHaptics(bool on)
        {
            Settings.Haptics = on;
            HapticsService.Enabled = on;
            if (on) HapticsService.Play(Haptic.Tick); // instant "this is what it feels like"
            SettingsStore.Save(Settings);
            RaiseChanged();
        }

        /// <summary>
        /// Swap the board's costume. Ebony and Marble belong to the Royal
        /// Stable — without it the tap is politely refused (the shop on the
        /// menu explains why).
        /// </summary>
        public void SetTheme(Theme theme)
        {
            if (theme != Theme.Wood && !RoyalStableOwned)
            {
                Audio.Play(Sfx.InvalidThud);
                HapticsService.Play(Haptic.Reject);
                RaiseChanged(); // repaint so the picker snaps back to Wood
                return;
            }

            Settings.Theme = theme;
            UI.GhodeTheme.Current = theme;
            SettingsStore.Save(Settings);
            Analytics.ThemeSelected(theme.ToString());
            RaiseChanged(); // every view repaints, picking up the new colors
        }

        // ------------------------------------------------------------------
        // The shop (Royal Stable) — thin passthroughs to BillingService
        // ------------------------------------------------------------------

        /// <summary>Start the Royal Stable purchase (store dialog opens).</summary>
        public void BuyRoyalStable()
        {
            Audio.Play(Sfx.Click);
            Analytics.Iap("started");
            Billing?.BuyRoyalStable();
            RaiseChanged();
        }

        /// <summary>Re-deliver purchases from an earlier install/device.</summary>
        public void RestorePurchases()
        {
            Audio.Play(Sfx.Click);
            Billing?.RestorePurchases(ok =>
            {
                if (ok) Analytics.Iap("restored");
                RaiseChanged();
            });
            RaiseChanged();
        }

        /// <summary>One line for the shop UI: store state + price when known.</summary>
        public string BillingStatusLine
        {
            get
            {
                if (Billing == null) return "Store not connected";
                return string.IsNullOrEmpty(Billing.RoyalStablePrice)
                    ? Billing.Status
                    : Billing.Status + " · " + Billing.RoyalStablePrice;
            }
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
            Analytics.ScreenView(screen.ToString());
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

            // Every finished round feeds the ad pacing; only WINS may travel
            // to a leaderboard (a fast loss is nobody's high score).
            Ads.OnRoundFinished();
            if (won) Leaderboards.SubmitWin(record.boardSize, record.timeMs);

            Analytics.GameEnd(record.boardSize, Settings.Difficulty.ToString(),
                won, record.moves, record.timeMs, record.hintsUsed);
        }
    }
}
