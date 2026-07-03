/**
 * MainMenu — the home screen.
 *
 * Lets the player start a game, choose board size and difficulty, toggle
 * sound / hints / ambience, read the instructions, and review their best
 * completion times and recent game history (all from localStorage).
 */
import HorseSvg from './HorseSvg.jsx';
import Toggle from './controls/Toggle.jsx';
import SegmentedControl from './controls/SegmentedControl.jsx';
import {
  SIZE_OPTIONS,
  DIFFICULTY_OPTIONS,
  DIFFICULTY_HINT,
} from '../uiOptions.js';
import { BOARD_SIZES } from '../game/knightLogic.js';
import { bestTimeFor } from '../state/records.js';
import { formatTime, formatDate } from '../utils/format.js';

export default function MainMenu({
  settings,
  records,
  onUpdateSetting,
  onNewGame,
  onInstructions,
}) {
  const games = records?.games ?? [];

  return (
    <div className="screen menu-screen">
      <header className="menu-hero">
        <HorseSvg idPrefix="menu-horse" className="menu-hero__horse" />
        <div className="menu-hero__titles">
          <h1 className="menu-hero__title">Ghode Ki Chaal</h1>
          <p className="menu-hero__subtitle">The Horse Tour · a Knight’s Tour puzzle</p>
        </div>
      </header>

      <div className="menu-grid">
        <section className="panel menu-card">
          <button
            type="button"
            className="btn btn--primary btn--big"
            onClick={onNewGame}
          >
            ▶ New Game
          </button>

          <SegmentedControl
            label="Board size"
            value={settings.boardSize}
            options={SIZE_OPTIONS}
            onChange={(v) => onUpdateSetting('boardSize', v)}
          />

          <SegmentedControl
            label="Difficulty"
            value={settings.difficulty}
            options={DIFFICULTY_OPTIONS}
            onChange={(v) => onUpdateSetting('difficulty', v)}
          />
          <p className="settings-block__note">{DIFFICULTY_HINT[settings.difficulty]}</p>

          <div className="menu-toggles">
            <Toggle
              label="Hint button"
              checked={settings.hints}
              onChange={(v) => onUpdateSetting('hints', v)}
            />
            <Toggle
              label="Sound effects"
              checked={settings.sound}
              onChange={(v) => onUpdateSetting('sound', v)}
            />
            <Toggle
              label="Background ambience"
              checked={settings.ambience}
              onChange={(v) => onUpdateSetting('ambience', v)}
            />
          </div>

          <button type="button" className="btn btn--ghost" onClick={onInstructions}>
            How to play
          </button>
        </section>

        <section className="panel menu-card menu-records">
          <h2 className="menu-card__heading">Best Times</h2>
          <ul className="best-list">
            {BOARD_SIZES.map((size) => {
              const best = bestTimeFor(records, size);
              return (
                <li key={size} className="best-list__row">
                  <span className="best-list__size">{size}×{size}</span>
                  <span className="best-list__time">
                    {best != null ? formatTime(best) : '—'}
                  </span>
                </li>
              );
            })}
          </ul>

          <h2 className="menu-card__heading">Recent Games</h2>
          {games.length === 0 ? (
            <p className="menu-empty">No games yet. Start your first tour!</p>
          ) : (
            <ul className="history-list">
              {games.map((g, i) => (
                <li key={i} className={`history-row history-row--${g.result}`}>
                  <span className={`history-row__pill history-row__pill--${g.result}`}>
                    {g.result === 'won' ? 'Win' : 'Lose'}
                  </span>
                  <span className="history-row__detail">
                    {g.size}×{g.size} · {g.moves} moves · {formatTime(g.timeMs)}
                  </span>
                  <span className="history-row__date">{formatDate(g.date)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <footer className="menu-footer">
        Tip: corners are hard to reach — plan your route through them early.
      </footer>
    </div>
  );
}
