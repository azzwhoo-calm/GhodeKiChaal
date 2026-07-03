/**
 * SegmentedControl — a row of mutually-exclusive options (used for board size
 * and difficulty). Each option is { value, label }.
 */
export default function SegmentedControl({ label, value, options, onChange }) {
  return (
    <div className="segmented">
      {label && <span className="segmented__label">{label}</span>}
      <div className="segmented__group" role="group" aria-label={label}>
        {options.map((opt) => (
          <button
            key={opt.value}
            type="button"
            className={`segmented__option ${value === opt.value ? 'is-active' : ''}`}
            aria-pressed={value === opt.value}
            onClick={() => onChange(opt.value)}
          >
            {opt.label}
          </button>
        ))}
      </div>
    </div>
  );
}
