/**
 * PauseOverlay — a modal shown over the board when the game is paused.
 *
 * Offers Resume / Restart / How to Play / Quit, plus quick settings that take
 * effect immediately (difficulty highlighting, sound, hints, ambience). The
 * timer is paused by the parent while this is open, so it never counts here.
 */
import Toggle from './controls/Toggle.jsx';
import SegmentedControl from './controls/SegmentedControl.jsx';
import { DIFFICULTY_OPTIONS, DIFFICULTY_HINT } from '../uiOptions.js';

export default function PauseOverlay({
  settings,
  onResume,
  onRestart,
  onInstructions,
  onQuit,
  onUpdateSetting,
}) {
  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Game paused">
      <div className="panel pause-panel">
        <h2 className="panel__title">Paused</h2>

        <div className="settings-block">
          <SegmentedControl
            label="Difficulty"
            value={settings.difficulty}
            options={DIFFICULTY_OPTIONS}
            onChange={(v) => onUpdateSetting('difficulty', v)}
          />
          <p className="settings-block__note">{DIFFICULTY_HINT[settings.difficulty]}</p>

          <Toggle
            label="Hint button"
            hint="Allow on-demand best-move hints"
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
            hint="Subtle wooden-room atmosphere"
            checked={settings.ambience}
            onChange={(v) => onUpdateSetting('ambience', v)}
          />
        </div>

        <div className="panel__actions">
          <button type="button" className="btn btn--primary" onClick={onResume}>
            Resume
          </button>
          <button type="button" className="btn" onClick={onRestart}>
            Restart board
          </button>
          <button type="button" className="btn" onClick={onInstructions}>
            How to play
          </button>
          <button type="button" className="btn btn--ghost" onClick={onQuit}>
            Quit to menu
          </button>
        </div>
      </div>
    </div>
  );
}
