import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Vite configuration for the Ghode Ki Chaal game.
// - `base: './'` makes the production build use relative asset paths so the
//   built `dist/` folder can be opened from any location (including file://).
// - The `test` block configures Vitest to run the pure game-logic unit tests
//   in a Node environment (no DOM needed for the board logic).
export default defineConfig({
  base: './',
  plugins: [react()],
  test: {
    environment: 'node',
    include: ['src/**/*.test.{js,jsx}'],
  },
});
