// AudioManager.cs — the game's one and only sound desk.
// Other scripts just say "play the Hop sound" and this class worries about
// clips, mute state and the looping background ambience. The clips are the
// baked WAVs under Resources/Ghode/Audio (rendered offline from the synth
// recipes in the Tech doc). Every clip is OPTIONAL: with a file missing the
// game stays perfectly playable, just silent — audio must never block play.

using System.Collections;
using UnityEngine;

namespace Ghode.Audio
{
    /// <summary>The named sound effects the game can ask for.</summary>
    public enum Sfx
    {
        Hop,          // a normal successful knight hop (round-robins 6 variants)
        SetDown,      // the very first placement of the horse
        InvalidThud,  // tapping a square you cannot hop to
        Click,        // generic UI button press
        Win,          // tour complete!
        Lose          // stuck with no moves left
    }

    /// <summary>
    /// Plays sound effects and the ambience loop. Created in code by
    /// GameBootstrap. Mute is a fast 20 ms volume ramp (never a hard cut, so
    /// toggling Sound mid-note cannot click). Hops rotate through six baked
    /// variants so a long tour never sounds like a woodpecker.
    /// No AudioMixer asset: mixers cannot be created from code, and two
    /// sources with ramped volumes deliver the same behavior for this scale.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const float MuteRampSeconds = 0.02f; // the spec'd 20 ms ramp
        const float AmbienceVolume = 0.35f;  // ambience sits under the SFX

        AudioSource _sfxSource;      // one-shot effects share this source
        AudioSource _ambienceSource; // the loop gets its own source

        AudioClip[] _hopClips;       // sfx_hop_01..06, round-robined
        AudioClip _setDownClip;
        AudioClip _invalidClip;
        AudioClip _clickClip;
        AudioClip _winClip;
        AudioClip _loseClip;
        AudioClip _ambienceClip;

        int _nextHop;                // round-robin cursor into _hopClips
        bool _muted;
        bool _ambienceWanted;
        Coroutine _ramp;
        bool _initialized;

        void Awake()
        {
            EnsureInit();
        }

        // In plain words: build our two speakers and fetch the baked clips the
        // first time anyone needs them. Lazy (not only in Awake) because
        // EditMode tests drive this class on GameObjects whose Awake never runs.
        void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _ambienceSource = gameObject.AddComponent<AudioSource>();
            _ambienceSource.playOnAwake = false;
            _ambienceSource.loop = true;
            _ambienceSource.volume = AmbienceVolume;

            _hopClips = new[]
            {
                LoadClip("sfx_hop_01"), LoadClip("sfx_hop_02"), LoadClip("sfx_hop_03"),
                LoadClip("sfx_hop_04"), LoadClip("sfx_hop_05"), LoadClip("sfx_hop_06")
            };
            _setDownClip = LoadClip("sfx_place");
            _invalidClip = LoadClip("sfx_invalid");
            _clickClip = LoadClip("sfx_click");
            _winClip = LoadClip("sfx_win");
            _loseClip = LoadClip("sfx_lose");
            _ambienceClip = LoadClip("amb_wood_loop");
        }

        static AudioClip LoadClip(string name)
        {
            // Null when missing — every caller treats that as "stay silent".
            return Resources.Load<AudioClip>("Ghode/Audio/" + name);
        }

        /// <summary>Play one named sound effect (respects mute; missing clip = silence).</summary>
        public void Play(Sfx sfx)
        {
            if (_muted) return;
            EnsureInit();

            var clip = ClipFor(sfx);
            if (clip == null) return; // graceful no-op if a bake is missing

            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Master mute, driven by the Sound setting. Volumes ramp over 20 ms
        /// instead of cutting, so muting mid-hop never pops the speaker.
        /// </summary>
        public void SetMuted(bool muted)
        {
            EnsureInit();
            _muted = muted;

            float sfxTarget = muted ? 0f : 1f;
            float ambienceTarget = muted ? 0f : AmbienceVolume;

            // Outside play mode (EditMode tests) coroutines cannot run — and
            // there is nothing audible to protect — so apply instantly.
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                _sfxSource.volume = sfxTarget;
                _ambienceSource.volume = ambienceTarget;
                return;
            }

            if (_ramp != null) StopCoroutine(_ramp);
            _ramp = StartCoroutine(RampVolumes(sfxTarget, ambienceTarget));
        }

        /// <summary>Start or stop the background ambience loop.</summary>
        public void SetAmbience(bool on)
        {
            EnsureInit();
            _ambienceWanted = on;

            if (on && _ambienceClip != null && !_ambienceSource.isPlaying)
            {
                _ambienceSource.clip = _ambienceClip;
                _ambienceSource.Play();
            }
            else if (!on && _ambienceSource.isPlaying)
            {
                _ambienceSource.Stop();
            }
        }

        // The 20 ms mute/unmute slide for both sources.
        IEnumerator RampVolumes(float sfxTarget, float ambienceTarget)
        {
            float sfxFrom = _sfxSource.volume;
            float ambienceFrom = _ambienceSource.volume;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MuteRampSeconds)
            {
                _sfxSource.volume = Mathf.Lerp(sfxFrom, sfxTarget, t);
                _ambienceSource.volume = Mathf.Lerp(ambienceFrom, ambienceTarget, t);
                yield return null;
            }
            _sfxSource.volume = sfxTarget;
            _ambienceSource.volume = ambienceTarget;
            _ramp = null;
        }

        // Maps the enum to a clip; hops rotate so repeats never sound robotic.
        AudioClip ClipFor(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Hop:
                    // In plain words: deal the six hop knocks like a card
                    // deck — always the next one, wrapping around.
                    for (int i = 0; i < _hopClips.Length; i++)
                    {
                        var clip = _hopClips[_nextHop];
                        _nextHop = (_nextHop + 1) % _hopClips.Length;
                        if (clip != null) return clip;
                    }
                    return null;

                case Sfx.SetDown: return _setDownClip;
                case Sfx.InvalidThud: return _invalidClip;
                case Sfx.Click: return _clickClip;
                case Sfx.Win: return _winClip;
                case Sfx.Lose: return _loseClip;
                default: return null;
            }
        }
    }
}
