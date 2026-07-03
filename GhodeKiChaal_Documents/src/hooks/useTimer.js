/**
 * useTimer — an accumulating stopwatch that survives pause/resume.
 *
 * Returns { elapsed (ms), running, start, pause, reset }.
 * Time is measured with performance.now() and accumulated across pauses, so
 * pausing the game does not count against the player's completion time.
 * The displayed value updates on a 100ms interval (smooth enough, cheap).
 */
import { useEffect, useRef, useState, useCallback } from 'react';

export function useTimer() {
  const [elapsed, setElapsed] = useState(0);
  const [running, setRunning] = useState(false);
  const accumulatedRef = useRef(0); // ms banked from previous run segments
  const startedAtRef = useRef(null); // performance.now() when current segment began
  const intervalRef = useRef(null);

  const clear = () => {
    if (intervalRef.current != null) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  };

  const tick = useCallback(() => {
    if (startedAtRef.current != null) {
      setElapsed(accumulatedRef.current + (performance.now() - startedAtRef.current));
    }
  }, []);

  const start = useCallback(() => {
    if (startedAtRef.current != null) return; // already running
    startedAtRef.current = performance.now();
    setRunning(true);
    clear();
    intervalRef.current = setInterval(tick, 100);
  }, [tick]);

  const pause = useCallback(() => {
    if (startedAtRef.current == null) return;
    accumulatedRef.current += performance.now() - startedAtRef.current;
    startedAtRef.current = null;
    setRunning(false);
    clear();
    setElapsed(accumulatedRef.current);
  }, []);

  const reset = useCallback(() => {
    accumulatedRef.current = 0;
    startedAtRef.current = null;
    setRunning(false);
    clear();
    setElapsed(0);
  }, []);

  // Read the precise current elapsed time synchronously (running or paused),
  // independent of the throttled `elapsed` state value.
  const read = useCallback(() => {
    if (startedAtRef.current != null) {
      return accumulatedRef.current + (performance.now() - startedAtRef.current);
    }
    return accumulatedRef.current;
  }, []);

  // Clean up the interval on unmount.
  useEffect(() => clear, []);

  return { elapsed, running, start, pause, reset, read };
}
