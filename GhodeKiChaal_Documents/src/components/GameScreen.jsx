/**
 * GameScreen — orchestrates a live game: HUD, board, click handling, the hint
 * system, and the pause overlay.
 *
 * Difficulty controls the *passive* highlighting:
 *   - easy   : legal moves highlighted + Warnsdorff best move always marked
 *   - normal : legal moves highlighted
 *   - hard   : no highlights (the player must see the L-shapes themselves)
 *
 * The Hint button (enabled via Settings, independent of difficulty) briefly
 * reveals the Warnsdorff-recommended move so a stuck player can learn the rule.
 */
import { useEffect, useMemo, useRef, useState } from 'react';
import { legalMoves, warnsdorffNext } from '../game/knightLogic.js';
import { moveCount, visitedCount } from '../state/gameReducer.js';
import sound from '../audio/AudioEngine.js';
import Board from './Board.jsx';
import Hud from './Hud.jsx';
import PauseOverlay from './PauseOverlay.jsx';

const HINT_VISIBLE_MS = 2600;

export default function GameScreen({
  game,
  paused,
  settings,
  elapsed,
  reduceMotion,
  dispatch,
  onToggleSound,
  onOpenInstructions,
  onUpdateSetting,
}) {
  const { size, current, visited, phase } = game;
  const total = size * size;

  const [invalidIndex, setInvalidIndex] = useState(null);
  const [hintActive, setHintActive] = useState(false);
  const invalidTimer = useRef(null);
  const hintTimer = useRef(null);

  // Difficulty-driven highlighting.
  const showLegal = settings.difficulty !== 'hard';
  const alwaysRecommend = settings.difficulty === 'easy';

  // Legal moves from the current square (empty while placing).
  const legalList = useMemo(
    () => (phase === 'playing' ? legalMoves(current, size, visited) : []),
    [phase, current, size, visited]
  );
  const legalSet = useMemo(() => new Set(legalList), [legalList]);

  // The Warnsdorff recommendation, shown when easy mode or the hint is active.
  const recommended = useMemo(() => {
    if (phase !== 'playing') return null;
    if (!alwaysRecommend && !hintActive) return null;
    return warnsdorffNext(current, size, visited);
  }, [phase, alwaysRecommend, hintActive, current, size, visited]);

  // Clear any pending timers on unmount.
  useEffect(
    () => () => {
      clearTimeout(invalidTimer.current);
      clearTimeout(hintTimer.current);
    },
    []
  );

  const flashInvalid = (index) => {
    setInvalidIndex(index);
    clearTimeout(invalidTimer.current);
    invalidTimer.current = setTimeout(() => setInvalidIndex(null), 420);
  };

  const handleSquareClick = (index) => {
    // Ignore clicks while paused or after the game has ended (won/lost).
    if (paused || (phase !== 'placing' && phase !== 'playing')) return;
    sound.unlock();

    if (phase === 'placing') {
      sound.place();
      dispatch({ type: 'PLACE', index });
      return;
    }

    // phase === 'playing'
    if (legalSet.has(index)) {
      sound.hop();
      dispatch({ type: 'MOVE', index });
    } else {
      sound.invalid();
      flashInvalid(index);
    }
  };

  const handleHint = () => {
    sound.click();
    if (phase !== 'playing') return;
    dispatch({ type: 'USE_HINT' });
    setHintActive(true);
    clearTimeout(hintTimer.current);
    hintTimer.current = setTimeout(() => setHintActive(false), HINT_VISIBLE_MS);
  };

  const handleUndo = () => {
    sound.click();
    dispatch({ type: 'UNDO' });
  };

  const handleRestart = () => {
    sound.click();
    dispatch({ type: 'RESTART' });
  };

  const handleMenu = () => {
    sound.click();
    dispatch({ type: 'PAUSE' });
  };

  const handleQuit = () => {
    sound.click();
    dispatch({ type: 'GO_MENU' });
  };

  const placingHint =
    phase === 'placing'
      ? 'Tap any square to set the horse down and begin the tour.'
      : null;

  return (
    <div className="screen game-screen">
      <Hud
        size={size}
        visitedCount={visitedCount(game)}
        total={total}
        moves={moveCount(game)}
        elapsed={elapsed}
        hintsUsed={game.hintsUsed}
        canUndo={game.history.length > 0}
        hintsEnabled={settings.hints}
        soundOn={settings.sound}
        onHint={handleHint}
        onUndo={handleUndo}
        onRestart={handleRestart}
        onMenu={handleMenu}
        onToggleSound={onToggleSound}
      />

      {placingHint && <p className="game-screen__prompt">{placingHint}</p>}

      <Board
        game={game}
        legalSet={legalSet}
        recommended={recommended}
        invalidIndex={invalidIndex}
        onSquareClick={handleSquareClick}
        showLegal={showLegal}
        interactionLocked={paused || phase === 'lost'}
      />

      {/* Non-blocking loss notice: the board stays fully visible above it so
          the player can retrace the route and spot the mistake. */}
      {phase === 'lost' && (
        <div className="lose-banner" role="alert">
          <div className="lose-banner__msg">
            <span className="lose-banner__title">Stuck!</span>
            <span className="lose-banner__detail">
              No legal moves left — {visitedCount(game)}/{total} squares filled in{' '}
              {moveCount(game)} moves. Study the board, then undo or restart.
            </span>
          </div>
          <div className="lose-banner__actions">
            <button type="button" className="btn btn--primary" onClick={handleUndo}>
              ↩ Undo &amp; keep trying
            </button>
            <button type="button" className="btn" onClick={handleRestart}>
              ⟳ Restart
            </button>
            <button type="button" className="btn btn--ghost" onClick={handleQuit}>
              Main menu
            </button>
          </div>
        </div>
      )}

      {paused && (
        <PauseOverlay
          settings={settings}
          onResume={() => {
            sound.click();
            dispatch({ type: 'RESUME' });
          }}
          onRestart={() => {
            sound.click();
            dispatch({ type: 'RESTART' });
          }}
          onInstructions={() => {
            sound.click();
            onOpenInstructions();
          }}
          onQuit={() => {
            sound.click();
            dispatch({ type: 'GO_MENU' });
          }}
          onUpdateSetting={onUpdateSetting}
        />
      )}
    </div>
  );
}
