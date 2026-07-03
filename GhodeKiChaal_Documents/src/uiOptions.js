/**
 * Shared option lists for the menu and pause settings, so the labels stay
 * consistent in both places.
 */
import { BOARD_SIZES } from './game/knightLogic.js';

export const SIZE_OPTIONS = BOARD_SIZES.map((n) => ({
  value: n,
  label: `${n}×${n}`,
}));

export const DIFFICULTY_OPTIONS = [
  { value: 'easy', label: 'Apprentice' },
  { value: 'normal', label: 'Knight' },
  { value: 'hard', label: 'Master' },
];

export const DIFFICULTY_HINT = {
  easy: 'Legal moves shown, plus the recommended best move.',
  normal: 'Legal moves are highlighted for you.',
  hard: 'No highlights — spot the knight moves yourself.',
};

export const DEFAULT_SETTINGS = {
  boardSize: 5,
  difficulty: 'normal',
  sound: true,
  hints: true,
  ambience: false,
};
