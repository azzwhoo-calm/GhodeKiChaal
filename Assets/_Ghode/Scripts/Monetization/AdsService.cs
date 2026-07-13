// AdsService.cs — the game's one door to interstitial ads.
// The RULES live in Ghode.Core.AdsPolicy (pure, unit-tested); the actual ad
// SDK hides behind IInterstitialProvider so the game never touches AdMob
// types directly. Until the AdMob plugin lands (see the drop-in note below)
// the provider is a logging fake, which keeps every code path exercisable
// in the editor today.
//
// THE ENTITLEMENT PROMISE: when the Royal Stable is owned at startup, the
// provider is never created and no ad SDK code runs at all — "zero init
// when entitled", exactly as the plan demands.
//
// TODO(azzwhoo): AdMob drop-in, once the AdMob app id exists:
//   1. Import the Google Mobile Ads Unity plugin (+ UMP is bundled).
//   2. Write AdMobInterstitialProvider : IInterstitialProvider that runs the
//      UMP consent flow in Initialize(), loads interstitials, and reports
//      IsReady/Show. Use TEST ad unit ids until the RC build.
//   3. Swap the provider here (keep the fake for the editor).
// The policy, caps and entitlement kill-switch all stay exactly as they are.

using System;
using UnityEngine;
using Ghode.Core;
using Ghode.Data;

namespace Ghode.Monetization
{
    /// <summary>What AdsService needs from a real ad SDK — nothing more.</summary>
    public interface IInterstitialProvider
    {
        /// <summary>Start the SDK (consent flow, first ad load…).</summary>
        void Initialize();

        /// <summary>Is an ad loaded and ready to play right now?</summary>
        bool IsReady { get; }

        /// <summary>Play the ad; call <paramref name="closed"/> when it ends.</summary>
        void Show(Action closed);
    }

    /// <summary>
    /// The stand-in provider: pretends an ad is always ready and "plays" it
    /// instantly with a log line. Lets the whole policy pipeline run in the
    /// editor and in tests without any SDK.
    /// </summary>
    public class FakeInterstitialProvider : IInterstitialProvider
    {
        public int ShownCount { get; private set; }

        public void Initialize() { }

        public bool IsReady => true;

        public void Show(Action closed)
        {
            ShownCount++;
            Debug.Log("[Ads] (fake) interstitial #" + ShownCount + " would play here.");
            closed?.Invoke();
        }
    }

    /// <summary>
    /// Orchestrates interstitials: persists the session count, asks the
    /// <see cref="AdsPolicy"/> for permission at between-game moments, and
    /// only then tells the provider to play. Created by GameController.
    /// </summary>
    public class AdsService
    {
        const string StateFile = "ads.v1.json";

        [Serializable]
        class AdsState
        {
            public int schemaVersion = 1;
            public int sessionCount = 0;
        }

        /// <summary>Milliseconds "now" — injectable so tests own the clock.</summary>
        public Func<long> NowMs { get; set; } =
            () => (long)(Time.realtimeSinceStartupAsDouble * 1000.0);

        AdsPolicy _policy;
        IInterstitialProvider _provider;

        /// <summary>The rulebook (exposed for tests and the debug HUD).</summary>
        public AdsPolicy Policy => _policy;

        /// <summary>
        /// Count this launch as a session and set up the policy. The provider
        /// is created by <paramref name="providerFactory"/> ONLY when the
        /// player is not entitled — an owner's device never runs ad SDK code.
        /// </summary>
        public void Initialize(bool entitled, Func<IInterstitialProvider> providerFactory)
        {
            var state = SaveService.LoadJson<AdsState>(StateFile, s => s.schemaVersion == 1)
                ?? new AdsState();
            state.sessionCount++;
            SaveService.SaveJson(StateFile, state);

            _policy = new AdsPolicy(state.sessionCount, entitled);

            if (!entitled && providerFactory != null)
            {
                _provider = providerFactory();
                _provider.Initialize();
            }
        }

        /// <summary>A game just finished (win, loss, or abandoned attempt).</summary>
        public void OnRoundFinished()
        {
            _policy?.NoteRoundFinished();
        }

        /// <summary>
        /// The Royal Stable arrived (mid-session purchase or restore):
        /// ads end immediately and the provider is dropped for good.
        /// </summary>
        public void OnEntitlementGranted()
        {
            if (_policy != null) _policy.Entitled = true;
            _provider = null;
        }

        /// <summary>
        /// A between-games moment: play an interstitial if every rule allows
        /// it. Returns true when an ad actually played.
        /// </summary>
        public bool TryShowInterstitial(bool stuckVisible, Action closed = null)
        {
            if (_policy == null || _provider == null) return false;

            long now = NowMs();
            if (!_policy.MayShow(now, stuckVisible)) return false;
            if (!_provider.IsReady) return false; // slow network is not the player's problem

            _policy.NoteAdShown(now);
            _provider.Show(closed);
            return true;
        }
    }
}
