// AudioManager.cs — the game's one and only sound desk.
// Other scripts just say "play the Hop sound" and this class worries about
// clips, mute state and the looping background ambience. Every clip field is
// optional: with nothing assigned the game stays perfectly playable, just silent.

using UnityEngine;

namespace Ghode.Audio
{
    /// <summary>The named sound effects the game can ask for.</summary>
    public enum Sfx
    {
        Hop,          // a normal successful knight hop
        SetDown,      // the very first placement of the horse
        InvalidThud,  // tapping a square you cannot hop to
        Click,        // generic UI button press
        Win,          // tour complete!
        Lose          // stuck with no moves left
    }

    /// <summary>
    /// Plays sound effects and the ambience loop. Created in code by
    /// GameBootstrap. If a clip is not assigned it quietly does nothing —
    /// audio must never block gameplay or compilation.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // TODO(azzwhoo): assign our baked SFX exported from the web synth once
        // they are bounced to .wav/.ogg and imported under Assets/_Ghode/Audio/.
        [Header("Sound effects (all optional — silent when empty)")]
        [SerializeField] AudioClip hopClip;
        [SerializeField] AudioClip setDownClip;
        [SerializeField] AudioClip invalidThudClip;
        [SerializeField] AudioClip clickClip;
        [SerializeField] AudioClip winClip;
        [SerializeField] AudioClip loseClip;

        [Header("Looping background ambience (optional)")]
        [SerializeField] AudioClip ambienceClip;

        AudioSource _sfxSource;      // one-shot effects share this source
        AudioSource _ambienceSource; // the loop gets its own source
        bool _muted;
        bool _ambienceWanted;

        void Awake()
        {
            // In plain words: build our two speakers the moment we exist.
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _ambienceSource = gameObject.AddComponent<AudioSource>();
            _ambienceSource.playOnAwake = false;
            _ambienceSource.loop = true;
            _ambienceSource.volume = 0.35f; // ambience should sit under the SFX
        }

        /// <summary>Play one named sound effect (respects mute; missing clip = silence).</summary>
        public void Play(Sfx sfx)
        {
            if (_muted) return;

            var clip = ClipFor(sfx);
            if (clip == null) return; // graceful no-op until real SFX are assigned

            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>Master mute for everything, driven by the Sound setting.</summary>
        public void SetMuted(bool muted)
        {
            _muted = muted;
            _ambienceSource.mute = muted;
        }

        /// <summary>Start or stop the background ambience loop.</summary>
        public void SetAmbience(bool on)
        {
            _ambienceWanted = on;

            if (on && ambienceClip != null && !_ambienceSource.isPlaying)
            {
                _ambienceSource.clip = ambienceClip;
                _ambienceSource.Play();
            }
            else if (!on && _ambienceSource.isPlaying)
            {
                _ambienceSource.Stop();
            }
            // If the clip is missing we simply remember the wish (_ambienceWanted)
            // so assigning a clip later can honor it.
            // TODO(azzwhoo): record a soft wood-workshop ambience loop and assign it.
        }

        // Maps the enum to whichever clip slot (possibly empty) belongs to it.
        AudioClip ClipFor(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Hop: return hopClip;
                case Sfx.SetDown: return setDownClip;
                case Sfx.InvalidThud: return invalidThudClip;
                case Sfx.Click: return clickClip;
                case Sfx.Win: return winClip;
                case Sfx.Lose: return loseClip;
                default: return null;
            }
        }
    }
}
