/**
 * Instructions — how-to-play screen. Explains the L-shaped move with a small
 * live diagram, the win/lose conditions, the hint system, and the controls.
 *
 * `onBack` returns to wherever the player came from (menu or the pause overlay).
 */
import { knightTargets, toRowCol } from '../game/knightLogic.js';

function MoveDiagram() {
  const size = 5;
  const center = 12; // index of the centre square on a 5x5 board
  const targets = new Set(knightTargets(center, size));
  return (
    <div className="diagram" aria-hidden="true">
      <div className="diagram__board" style={{ gridTemplateColumns: `repeat(${size}, 1fr)` }}>
        {Array.from({ length: size * size }, (_, i) => {
          const [r, c] = toRowCol(i, size);
          const isDark = (r + c) % 2 === 1;
          const isCenter = i === center;
          const isTarget = targets.has(i);
          return (
            <div
              key={i}
              className={`diagram__cell ${isDark ? 'is-dark' : 'is-light'} ${
                isCenter ? 'is-center' : ''
              } ${isTarget ? 'is-target' : ''}`}
            >
              {isCenter && <span className="diagram__horse">♞</span>}
              {isTarget && <span className="diagram__dot" />}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default function Instructions({ onBack }) {
  return (
    <div className="screen instructions-screen">
      <div className="panel instructions-panel">
        <h1 className="panel__title">How to Play</h1>

        <section className="how">
          <div className="how__text">
            <h3>The move</h3>
            <p>
              The wooden horse moves like a chess knight: an <strong>“L” shape</strong> —
              two squares in one direction, then one square at a right angle.
              From the centre it can reach up to eight squares.
            </p>
          </div>
          <MoveDiagram />
        </section>

        <section className="how">
          <div className="how__text">
            <h3>The goal</h3>
            <p>
              First, tap any square to place the horse. Then keep hopping to
              squares you haven’t visited yet. Each square you land on is
              <strong> filled in and locked</strong> — you can’t step on it again.
            </p>
            <ul>
              <li>
                <strong>Win:</strong> visit <em>every</em> square on the board —
                a complete Knight’s Tour.
              </li>
              <li>
                <strong>Lose:</strong> get stranded with no legal move left while
                squares are still empty.
              </li>
            </ul>
          </div>
        </section>

        <section className="how">
          <div className="how__text">
            <h3>Hints &amp; difficulty</h3>
            <p>
              On <strong>Apprentice</strong> and <strong>Knight</strong> difficulty,
              legal moves are highlighted. The <strong>Hint</strong> button reveals
              the move suggested by <em>Warnsdorff’s rule</em> — always head to the
              square with the fewest onward options. Learn that habit and full
              tours become much easier. <strong>Master</strong> hides all helpers.
            </p>
          </div>
        </section>

        <section className="how">
          <div className="how__text">
            <h3>Tips</h3>
            <ul>
              <li>Use <strong>Undo</strong> to take back a move and explore another path.</li>
              <li>Corners and edges have few entrances — visit them early.</li>
              <li>On 5×5, a complete tour only exists from certain start squares.</li>
            </ul>
          </div>
        </section>

        <div className="panel__actions">
          <button type="button" className="btn btn--primary" onClick={onBack}>
            Back
          </button>
        </div>
      </div>
    </div>
  );
}
