/**
 * AudioEngine.js
 * ----------------------------------------------------------------------------
 * All sound for the game is SYNTHESIZED at runtime with the Web Audio API —
 * there are no audio asset files to ship or license.
 *
 * The signature sound is a "wood knock" (`woodHit`): a fast-decaying pitched
 * body (triangle/sine that drops in pitch) layered with a short band-passed
 * noise transient for the percussive attack. Tuning the parameters yields the
 * hop, set-down, invalid thud, button tick, and the win/lose motifs — all of
 * which read as tactile and wooden rather than as generic beeps.
 *
 * A single shared instance is exported as the default. Because browsers block
 * audio until a user gesture, call `engine.unlock()` from a click/tap handler
 * (the game does this on every button and square press).
 */

class AudioEngine {
  constructor() {
    /** @type {AudioContext|null} */
    this.ctx = null;
    /** @type {GainNode|null} master output bus */
    this.master = null;
    this.muted = false;
    this.ambienceOn = false;
    this._ambience = null; // { nodes..., interval }
    this._noiseBuffer = null;
  }

  /** Lazily create / resume the AudioContext. Safe to call repeatedly. */
  unlock() {
    if (!this.ctx) {
      const Ctx = window.AudioContext || window.webkitAudioContext;
      if (!Ctx) return; // Audio simply unavailable; game stays playable.
      this.ctx = new Ctx();
      this.master = this.ctx.createGain();
      this.master.gain.value = this.muted ? 0 : 0.9;
      this.master.connect(this.ctx.destination);
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
  }

  setMuted(muted) {
    this.muted = muted;
    if (this.master) {
      // Smooth ramp avoids clicks when toggling.
      const now = this.ctx.currentTime;
      this.master.gain.cancelScheduledValues(now);
      this.master.gain.setTargetAtTime(muted ? 0 : 0.9, now, 0.02);
    }
    if (muted) this.stopAmbience();
  }

  /** A small reusable buffer of white noise for percussive attacks. */
  _noise() {
    if (this._noiseBuffer) return this._noiseBuffer;
    const length = Math.floor(this.ctx.sampleRate * 0.25);
    const buffer = this.ctx.createBuffer(1, length, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < length; i++) data[i] = Math.random() * 2 - 1;
    this._noiseBuffer = buffer;
    return buffer;
  }

  /**
   * The core "wooden knock" voice.
   * @param {object} o
   * @param {number} o.freq    body start frequency (Hz)
   * @param {number} o.dur     body duration (s)
   * @param {string} o.type    oscillator type
   * @param {number} o.gain    body peak gain
   * @param {number} o.noise   noise-transient peak gain (0 disables)
   * @param {number} o.bp      noise band-pass centre (Hz)
   * @param {number} o.when    delay before the hit (s)
   * @param {number} o.drop    fractional pitch drop over the duration
   */
  woodHit({
    freq = 320,
    dur = 0.16,
    type = 'triangle',
    gain = 0.5,
    noise = 0.4,
    bp = 1900,
    when = 0,
    drop = 0.72,
  } = {}) {
    if (!this.ctx || this.muted) return;
    const ctx = this.ctx;
    const t = ctx.currentTime + when;

    // --- Pitched body ---
    const osc = ctx.createOscillator();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, t);
    osc.frequency.exponentialRampToValueAtTime(Math.max(40, freq * drop), t + dur);
    const bodyGain = ctx.createGain();
    bodyGain.gain.setValueAtTime(0.0001, t);
    bodyGain.gain.exponentialRampToValueAtTime(gain, t + 0.006);
    bodyGain.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    osc.connect(bodyGain).connect(this.master);
    osc.start(t);
    osc.stop(t + dur + 0.03);

    // --- Noise attack (the "tock") ---
    if (noise > 0) {
      const src = ctx.createBufferSource();
      src.buffer = this._noise();
      const filter = ctx.createBiquadFilter();
      filter.type = 'bandpass';
      filter.frequency.value = bp;
      filter.Q.value = 5;
      const nGain = ctx.createGain();
      nGain.gain.setValueAtTime(noise, t);
      nGain.gain.exponentialRampToValueAtTime(0.0001, t + 0.05);
      src.connect(filter).connect(nGain).connect(this.master);
      src.start(t);
      src.stop(t + 0.07);
    }
  }

  // ---- Game-facing sound effects -------------------------------------------

  /** Knight hops to a new square: a crisp, slightly bright wooden knock. */
  hop() {
    this.unlock();
    const jitter = 0.94 + Math.random() * 0.12; // tiny variation per hop
    this.woodHit({ freq: 300 * jitter, dur: 0.16, gain: 0.5, noise: 0.45, bp: 2100 });
    // A subtle lower "thunk" layer for body.
    this.woodHit({ freq: 150 * jitter, dur: 0.12, gain: 0.28, noise: 0, type: 'sine' });
  }

  /** Placing the horse on its starting square: a deeper, softer set-down. */
  place() {
    this.unlock();
    this.woodHit({ freq: 210, dur: 0.24, gain: 0.5, noise: 0.3, bp: 1200, drop: 0.6 });
  }

  /** Illegal move attempt: a dull, dampened thud. */
  invalid() {
    this.unlock();
    this.woodHit({ freq: 120, dur: 0.14, type: 'sine', gain: 0.4, noise: 0.18, bp: 500, drop: 0.85 });
    this.woodHit({ freq: 96, dur: 0.1, type: 'square', gain: 0.12, noise: 0, when: 0.02 });
  }

  /** UI tick for buttons / menu navigation. */
  click() {
    this.unlock();
    this.woodHit({ freq: 620, dur: 0.05, gain: 0.22, noise: 0.5, bp: 3200, drop: 0.9 });
  }

  /** Win fanfare: ascending wooden-marimba pentatonic flourish. */
  win() {
    this.unlock();
    const notes = [392, 523.25, 659.25, 783.99, 1046.5]; // G A-ish pentatonic-ish climb
    notes.forEach((f, i) => {
      this.woodHit({ freq: f, dur: 0.26, gain: 0.42, noise: 0.18, bp: f * 2, when: i * 0.11, drop: 0.85 });
    });
    // A final shimmering double-hit.
    this.woodHit({ freq: 1318.5, dur: 0.4, gain: 0.3, noise: 0.1, bp: 3000, when: 0.6, drop: 0.9 });
  }

  /** Lose motif: a short, somber descending knock. */
  lose() {
    this.unlock();
    const notes = [233.08, 174.61, 130.81];
    notes.forEach((f, i) => {
      this.woodHit({ freq: f, dur: 0.32, type: 'sine', gain: 0.4, noise: 0.14, bp: f * 2.2, when: i * 0.18, drop: 0.7 });
    });
  }

  // ---- Optional background ambience ----------------------------------------

  /**
   * A very subtle wooden-room ambience: low filtered noise (like distant air)
   * plus occasional soft creaks. Off by default and fully toggleable.
   */
  startAmbience() {
    this.unlock();
    if (!this.ctx || this.ambienceOn) return;
    this.ambienceOn = true;
    const ctx = this.ctx;

    const src = ctx.createBufferSource();
    src.buffer = this._noise();
    src.loop = true;
    const lp = ctx.createBiquadFilter();
    lp.type = 'lowpass';
    lp.frequency.value = 260;
    const bedGain = ctx.createGain();
    bedGain.gain.value = 0.015;
    src.connect(lp).connect(bedGain).connect(this.master);
    src.start();

    // Occasional distant creak.
    const interval = setInterval(() => {
      if (!this.ambienceOn || this.muted) return;
      if (Math.random() < 0.5) {
        this.woodHit({
          freq: 90 + Math.random() * 60,
          dur: 0.5,
          type: 'sine',
          gain: 0.05,
          noise: 0.03,
          bp: 400,
          drop: 0.6,
        });
      }
    }, 4200);

    this._ambience = { src, bedGain, interval };
  }

  stopAmbience() {
    this.ambienceOn = false;
    if (this._ambience) {
      try {
        this._ambience.src.stop();
      } catch {
        /* already stopped */
      }
      clearInterval(this._ambience.interval);
      this._ambience = null;
    }
  }
}

const engine = new AudioEngine();
export default engine;
