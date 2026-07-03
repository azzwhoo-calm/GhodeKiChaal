/**
 * Square — a single clickable board cell.
 *
 * Rendered as a <button> for keyboard/touch accessibility. Visual state is
 * driven entirely by class names so the styling lives in CSS:
 *   - checkerboard light/dark
 *   - visited (shows its move-order number)
 *   - current (the horse's square)
 *   - legal / recommended (move hints)
 *   - placeable (any square during the placing phase)
 *   - invalid (brief shake when an illegal move is attempted)
 */
import { memo } from 'react';
import HorseSvg from './HorseSvg.jsx';

function Square({
  index,
  isDark,
  isVisited,
  order,
  isCurrent,
  isLegal,
  isRecommended,
  isInvalid,
  isPlaceable,
  isStart,
  onClick,
  disabled,
}) {
  const classes = ['square', isDark ? 'square--dark' : 'square--light'];
  if (isVisited) classes.push('square--visited');
  if (isCurrent) classes.push('square--current');
  if (isStart) classes.push('square--start');
  if (isLegal) classes.push('square--legal');
  if (isRecommended) classes.push('square--recommended');
  if (isInvalid) classes.push('square--invalid');
  if (isPlaceable) classes.push('square--placeable');

  const label = isVisited
    ? `Square ${index + 1}, visited, step ${order}`
    : isLegal
      ? `Square ${index + 1}, legal move`
      : `Square ${index + 1}`;

  return (
    <button
      type="button"
      className={classes.join(' ')}
      onClick={() => onClick(index)}
      disabled={disabled}
      aria-label={label}
    >
      {/* A wooden horse is placed on every visited square and stays for the
          rest of the round. The newest one animates in via CSS on mount. */}
      {isVisited && (
        <span className="square__horse">
          <HorseSvg idPrefix={`sq${index}`} className="square__horse-svg" />
        </span>
      )}
      {/* Move-order badge in the corner so it stays readable behind the horse. */}
      {isVisited && <span className="square__order">{order}</span>}
      {/* Pulsing dot to mark a legal destination (and a ring if recommended). */}
      {!isVisited && isLegal && <span className="square__dot" />}
    </button>
  );
}

export default memo(Square);
