/**
 * Toggle — an accessible on/off switch styled as a wooden slider.
 */
export default function Toggle({ checked, onChange, label, hint }) {
  return (
    <label className="toggle">
      <span className="toggle__text">
        <span className="toggle__label">{label}</span>
        {hint && <span className="toggle__hint">{hint}</span>}
      </span>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-label={label}
        className={`toggle__switch ${checked ? 'is-on' : ''}`}
        onClick={() => onChange(!checked)}
      >
        <span className="toggle__knob" />
      </button>
    </label>
  );
}
