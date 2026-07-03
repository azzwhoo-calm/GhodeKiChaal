/**
 * Hud — the in-game heads-up display.
 *
 * Top row: live stats (progress, moves, time, hints used).
 * Bottom row: action buttons (Hint, Undo, Restart, Menu) plus a quick
 * sound toggle. Buttons are large enough for comfortable touch targets.
 */
import { formatTime } from '../utils/format.js';

function IconButton({ onClick, disabled, label, children, variant = '' }) {
  return (
    <button
      type="button"
      className={`hud__btn ${variant}`}
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      title={label}
    >
      {children}
    </button>
  );
}

export default function Hud({
  size,
  visitedCount,
  total,
  moves,
  elapsed,
  hintsUsed,
  canUndo,
  hintsEnabled,
  soundOn,
  onHint,
  onUndo,
  onRestart,
  onMenu,
  onToggleSound,
}) {
  return (
    <div className="hud">
      <div className="hud__stats">
        <div className="stat">
          <span className="stat__label">Board</span>
          <span className="stat__value">{size}×{size}</span>
        </div>
        <div className="stat">
          <span className="stat__label">Filled</span>
          <span className="stat__value">
            {visitedCount}<span className="stat__sub">/{total}</span>
          </span>
        </div>
        <div className="stat">
          <span className="stat__label">Moves</span>
          <span className="stat__value">{moves}</span>
        </div>
        <div className="stat">
          <span className="stat__label">Time</span>
          <span className="stat__value">{formatTime(elapsed)}</span>
        </div>
        <div className="stat">
          <span className="stat__label">Hints</span>
          <span className="stat__value">{hintsUsed}</span>
        </div>
      </div>

      <div className="hud__controls">
        {hintsEnabled && (
          <IconButton onClick={onHint} label="Show a hint" variant="hud__btn--accent">
            💡 Hint
          </IconButton>
        )}
        <IconButton onClick={onUndo} disabled={!canUndo} label="Undo last move">
          ↩ Undo
        </IconButton>
        <IconButton onClick={onRestart} label="Restart this board">
          ⟳ Restart
        </IconButton>
        <IconButton
          onClick={onToggleSound}
          label={soundOn ? 'Mute sound' : 'Unmute sound'}
        >
          {soundOn ? '🔊' : '🔈'}
        </IconButton>
        <IconButton onClick={onMenu} label="Pause and open menu">
          ☰ Menu
        </IconButton>
      </div>
    </div>
  );
}
