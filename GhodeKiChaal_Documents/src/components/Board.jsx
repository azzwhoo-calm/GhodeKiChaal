/**
 * Board — renders the wooden grid, the visited-path overlay, and the horse.
 *
 * It measures its own pixel width with a ResizeObserver so everything (cell
 * size, the SVG path overlay, and the absolutely-positioned KnightPiece) stays
 * perfectly aligned and fully responsive.
 */
import { useLayoutEffect, useRef, useState } from 'react';
import { toRowCol } from '../game/knightLogic.js';
import Square from './Square.jsx';

export default function Board({
  game,
  legalSet,
  recommended,
  invalidIndex,
  onSquareClick,
  showLegal,
  interactionLocked,
}) {
  const { size, visited, current, phase, history } = game;
  const gridRef = useRef(null);
  const [boardPx, setBoardPx] = useState(0);

  // Keep the measured board width in sync with layout (responsive).
  useLayoutEffect(() => {
    const el = gridRef.current;
    if (!el) return undefined;
    const measure = () => setBoardPx(el.clientWidth);
    measure();
    const ro = new ResizeObserver(measure);
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  // Map each visited square -> its 1-based step number for the badges.
  const orderByIndex = new Map();
  history.forEach((idx, i) => orderByIndex.set(idx, i + 1));

  const cellSize = boardPx ? boardPx / size : 0;
  const center = (idx) => {
    const [r, c] = toRowCol(idx, size);
    return { x: (c + 0.5) * cellSize, y: (r + 0.5) * cellSize };
  };
  const pathPoints = history.map(center);

  return (
    <div className="board-frame">
      <div
        className="board"
        ref={gridRef}
        style={{ gridTemplateColumns: `repeat(${size}, 1fr)` }}
      >
        {Array.from({ length: size * size }, (_, index) => {
          const [r, c] = toRowCol(index, size);
          const isDark = (r + c) % 2 === 1;
          const isVisited = visited[index];
          const isCurrent = index === current;
          const isStart = history.length > 0 && history[0] === index;
          const isLegal = showLegal && legalSet.has(index);
          const isRecommended = recommended === index;
          const isPlaceable = phase === 'placing';
          return (
            <Square
              key={index}
              index={index}
              isDark={isDark}
              isVisited={isVisited}
              order={orderByIndex.get(index)}
              isCurrent={isCurrent}
              isStart={isStart}
              isLegal={isLegal}
              isRecommended={isRecommended}
              isInvalid={invalidIndex === index}
              isPlaceable={isPlaceable}
              onClick={onSquareClick}
              disabled={interactionLocked}
            />
          );
        })}

        {/* Each visited square now keeps its own horse (rendered inside Square),
            so there is no single travelling piece overlay. */}

        {/* Visited-path overlay (drawn above squares, below the horse). */}
        {cellSize > 0 && pathPoints.length > 1 && (
          <svg
            className="board__path"
            width={boardPx}
            height={boardPx}
            viewBox={`0 0 ${boardPx} ${boardPx}`}
            aria-hidden="true"
          >
            <polyline
              points={pathPoints.map((p) => `${p.x},${p.y}`).join(' ')}
              fill="none"
              stroke="rgba(58, 32, 12, 0.45)"
              strokeWidth={Math.max(2, cellSize * 0.05)}
              strokeLinejoin="round"
              strokeLinecap="round"
              strokeDasharray={`${cellSize * 0.12} ${cellSize * 0.12}`}
            />
            {pathPoints.map((p, i) => (
              <circle
                key={i}
                cx={p.x}
                cy={p.y}
                r={Math.max(1.5, cellSize * 0.04)}
                fill="rgba(58, 32, 12, 0.5)"
              />
            ))}
          </svg>
        )}
      </div>
    </div>
  );
}
