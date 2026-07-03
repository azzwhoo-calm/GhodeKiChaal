/**
 * App — top-level orchestrator.
 *
 * Owns:
 *   - the screen/game state machine (useReducer + gameReducer)
 *   - persisted settings and records (useLocalStorage)
 *   - the completion timer (useTimer)
 *   - syncing settings -> the audio engine (mute + ambience)
 *   - recording results into best-times / history on game end
 *
 * It renders exactly one screen at a time, so there are no dead-end states:
 * menu ⇄ instructions, menu → playing → result → (play again | menu).
 */
import { useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { gameReducer, initialState, moveCount } from './state/gameReducer.js';
import { recordGame, bestTimeFor, emptyRecords } from './state/records.js';
import { useLocalStorage } from './hooks/useLocalStorage.js';
import { useTimer } from './hooks/useTimer.js';
import { DEFAULT_SETTINGS } from './uiOptions.js';
import sound from './audio/AudioEngine.js';

import MainMenu from './components/MainMenu.jsx';
import Instructions from './components/Instructions.jsx';
import GameScreen from './components/GameScreen.jsx';
import ResultScreen from './components/ResultScreen.jsx';

const SETTINGS_KEY = 'ghodekichaal.settings.v1';
const RECORDS_KEY = 'ghodekichaal.records.v1';

export default function App() {
  const [state, dispatch] = useReducer(gameReducer, initialState);
  const [settings, setSettings] = useLocalStorage(SETTINGS_KEY, DEFAULT_SETTINGS);
  const [records, setRecords] = useLocalStorage(RECORDS_KEY, emptyRecords);
  const timer = useTimer();

  // Snapshot of the just-finished game's stats, shown on the result screen.
  const [resultStats, setResultStats] = useState({ timeMs: 0, isNewBest: false, bestTime: null });

  const reduceMotion = useMemo(
    () =>
      typeof window !== 'undefined' &&
      window.matchMedia &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches,
    []
  );

  // ---- Settings -> audio engine -------------------------------------------
  useEffect(() => {
    sound.setMuted(!settings.sound);
  }, [settings.sound]);

  useEffect(() => {
    if (settings.ambience && settings.sound) sound.startAmbience();
    else sound.stopAmbience();
  }, [settings.ambience, settings.sound]);

  // Resume the audio context (and ambience) on the first user gesture, since
  // browsers block audio until then.
  useEffect(() => {
    const onFirstGesture = () => {
      sound.unlock();
      if (settings.ambience && settings.sound) sound.startAmbience();
    };
    window.addEventListener('pointerdown', onFirstGesture, { once: true });
    return () => window.removeEventListener('pointerdown', onFirstGesture);
  }, [settings.ambience, settings.sound]);

  // ---- Timer control ------------------------------------------------------
  // (Depend on the stable callbacks, not the timer object, which is a fresh
  // reference every render.)
  const { reset: timerReset, start: timerStart, pause: timerPause, read: timerRead } = timer;

  // Reset whenever a fresh game (New Game / Restart / Play Again) begins.
  useEffect(() => {
    timerReset();
  }, [state.gameId, timerReset]);

  // Run only while actively playing and not paused. `gameId` is included so a
  // Restart (which keeps the screen on 'playing') re-fires this effect and
  // starts the freshly-reset timer instead of leaving it stuck at zero. We also
  // stop the clock the instant the game ends — a loss now keeps the player on
  // the 'playing' screen (phase 'lost'), so the screen check alone isn't enough.
  const activePhase =
    state.game && (state.game.phase === 'playing' || state.game.phase === 'placing');
  useEffect(() => {
    if (state.screen === 'playing' && !state.paused && activePhase) timerStart();
    else timerPause();
  }, [state.screen, state.paused, state.gameId, activePhase, timerStart, timerPause]);

  // ---- Record results on game end -----------------------------------------
  // Triggered by the board's PHASE reaching a terminal state ('won' or 'lost'),
  // which works whether or not the screen changes (a loss stays on the board).
  const prevPhaseRef = useRef(null);
  useEffect(() => {
    const phase = state.game ? state.game.phase : null;
    const prev = prevPhaseRef.current;
    prevPhaseRef.current = phase;

    const isTerminal = phase === 'won' || phase === 'lost';
    const wasTerminal = prev === 'won' || prev === 'lost';
    if (!isTerminal || wasTerminal) return; // only on a fresh transition

    const g = state.game;
    const timeMs = timerRead();
    const priorBest = bestTimeFor(records, g.size);
    const won = phase === 'won';
    const isNewBest = won && (priorBest == null || timeMs < priorBest);

    setResultStats({
      timeMs,
      isNewBest,
      bestTime: won ? (isNewBest ? timeMs : priorBest) : priorBest,
    });
    setRecords((prevRecords) =>
      recordGame(prevRecords, {
        size: g.size,
        result: phase,
        moves: moveCount(g),
        timeMs,
        hintsUsed: g.hintsUsed,
      })
    );

    if (won) sound.win();
    else sound.lose();
  }, [state.game, records, setRecords, timerRead]);

  // ---- Setting updates (with a click tick) --------------------------------
  const updateSetting = (key, value) => {
    sound.click();
    setSettings((prev) => ({ ...prev, [key]: value }));
  };

  const toggleSound = () => {
    // Toggling sound off shouldn't itself be silent feedback for "on".
    setSettings((prev) => ({ ...prev, sound: !prev.sound }));
    if (!settings.sound) sound.click();
  };

  // ---- Render the active screen -------------------------------------------
  let screen;
  if (state.screen === 'menu') {
    screen = (
      <MainMenu
        settings={settings}
        records={records}
        onUpdateSetting={updateSetting}
        onNewGame={() => {
          sound.click();
          dispatch({ type: 'NEW_GAME', size: settings.boardSize });
        }}
        onInstructions={() => {
          sound.click();
          dispatch({ type: 'GO_INSTRUCTIONS' });
        }}
      />
    );
  } else if (state.screen === 'instructions') {
    screen = (
      <Instructions
        onBack={() => {
          sound.click();
          dispatch({ type: 'CLOSE_INSTRUCTIONS' });
        }}
      />
    );
  } else if (state.screen === 'playing' && state.game) {
    screen = (
      <GameScreen
        game={state.game}
        paused={state.paused}
        settings={settings}
        elapsed={timer.elapsed}
        reduceMotion={reduceMotion}
        dispatch={dispatch}
        onToggleSound={toggleSound}
        onOpenInstructions={() => dispatch({ type: 'GO_INSTRUCTIONS' })}
        onUpdateSetting={updateSetting}
      />
    );
  } else if (state.screen === 'result' && state.game) {
    screen = (
      <ResultScreen
        result={state.result}
        game={state.game}
        elapsed={resultStats.timeMs}
        bestTime={resultStats.bestTime}
        isNewBest={resultStats.isNewBest}
        onPlayAgain={() => {
          sound.click();
          dispatch({ type: 'RESTART' });
        }}
        onUndo={() => {
          sound.click();
          dispatch({ type: 'UNDO' });
        }}
        onMenu={() => {
          sound.click();
          dispatch({ type: 'GO_MENU' });
        }}
      />
    );
  } else {
    // Defensive fallback — should be unreachable. Recover to the menu.
    screen = (
      <MainMenu
        settings={settings}
        records={records}
        onUpdateSetting={updateSetting}
        onNewGame={() => dispatch({ type: 'NEW_GAME', size: settings.boardSize })}
        onInstructions={() => dispatch({ type: 'GO_INSTRUCTIONS' })}
      />
    );
  }

  return <div className="app">{screen}</div>;
}
