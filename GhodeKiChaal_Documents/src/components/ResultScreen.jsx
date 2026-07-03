/**
 * ResultScreen — shown when a game ends.
 *
 * Win  : celebratory "Tour Complete" with full stats and a new-best-time flag.
 * Lose : "Stuck!" with how far the player got, plus the option to undo the last
 *        move and keep trying (a gentle off-ramp from a dead end).
 */
import { moveCount, visitedCount } from '../state/gameReducer.js';
import { formatTime } from '../utils/format.js';
import HorseSvg from './HorseSvg.jsx';

function Stat({ label, value }) {
  return (
    <div className="result-stat">
      <span className="result-stat__value">{value}</span>
      <span className="result-stat__label">{label}</span>
    </div>
  );
}

export default function ResultScreen({
  result,
  game,
  elapsed,
  bestTime,
  isNewBest,
  onPlayAgain,
  onUndo,
  onMenu,
}) {
  const won = result === 'won';
  const total = game.size * game.size;

  return (
    <div className={`screen result-screen ${won ? 'result-screen--win' : 'result-screen--lose'}`}>
      <div className="panel result-panel">
        <div className={`result-badge ${won ? 'is-win' : 'is-lose'}`}>
          <HorseSvg idPrefix="result-horse" className="result-badge__horse" />
        </div>

        <h1 className="result-title">{won ? 'Tour Complete!' : 'Stuck!'}</h1>
        <p className="result-subtitle">
          {won
            ? `You guided the horse across all ${total} squares.`
            : `The horse has no legal moves left — ${visitedCount(game)} of ${total} squares filled.`}
        </p>

        {won && isNewBest && (
          <p className="result-best">★ New best time for {game.size}×{game.size}!</p>
        )}

        <div className="result-stats">
          <Stat label="Moves" value={moveCount(game)} />
          <Stat label="Time" value={formatTime(elapsed)} />
          <Stat label="Hints" value={game.hintsUsed} />
          {bestTime != null && <Stat label="Best" value={formatTime(bestTime)} />}
        </div>

        <div className="panel__actions">
          <button type="button" className="btn btn--primary" onClick={onPlayAgain}>
            Play again
          </button>
          {!won && (
            <button type="button" className="btn" onClick={onUndo}>
              ↩ Undo &amp; keep trying
            </button>
          )}
          <button type="button" className="btn btn--ghost" onClick={onMenu}>
            Main menu
          </button>
        </div>
      </div>
    </div>
  );
}
