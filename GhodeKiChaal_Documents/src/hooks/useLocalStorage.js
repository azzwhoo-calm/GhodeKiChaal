/**
 * useLocalStorage — a small persisted-state hook.
 *
 * Behaves like useState but mirrors the value into window.localStorage under
 * `key`. Reads are guarded so the app still works where storage is blocked
 * (e.g. private-mode / file:// in some browsers).
 */
import { useEffect, useState } from 'react';

export function useLocalStorage(key, initialValue) {
  const [value, setValue] = useState(() => {
    try {
      const stored = window.localStorage.getItem(key);
      return stored != null ? JSON.parse(stored) : initialValue;
    } catch {
      return initialValue;
    }
  });

  useEffect(() => {
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch {
      /* storage unavailable — keep running with in-memory state only */
    }
  }, [key, value]);

  return [value, setValue];
}
