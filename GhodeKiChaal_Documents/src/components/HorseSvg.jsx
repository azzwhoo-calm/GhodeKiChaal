/**
 * HorseSvg — a stylized, carved-wood chess-knight graphic.
 *
 * Pure SVG so it stays crisp at any size and is cheap to animate via CSS
 * transforms. Colours come from wood gradients defined in <defs>; an `idPrefix`
 * keeps gradient ids unique when more than one horse is on the page (e.g. the
 * board piece plus the menu logo).
 *
 * The horse faces left and rests on a turned wooden plinth, echoing a real
 * hand-carved chess knight.
 */
export default function HorseSvg({ idPrefix = 'horse', className = '' }) {
  const wood = `${idPrefix}-wood`;
  const woodDark = `${idPrefix}-wood-dark`;
  const base = `${idPrefix}-base`;
  const sheen = `${idPrefix}-sheen`;

  return (
    <svg
      className={className}
      viewBox="0 0 120 128"
      xmlns="http://www.w3.org/2000/svg"
      role="img"
      aria-label="Carved wooden horse chess knight"
    >
      <defs>
        {/* Main warm-wood gradient, light at top-left to dark at bottom-right. */}
        <linearGradient id={wood} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#e7b578" />
          <stop offset="0.45" stopColor="#bd853f" />
          <stop offset="1" stopColor="#6e451d" />
        </linearGradient>
        {/* Darker wood for the base/plinth. */}
        <linearGradient id={base} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#a06e34" />
          <stop offset="1" stopColor="#5a3617" />
        </linearGradient>
        <linearGradient id={woodDark} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#7a4d22" />
          <stop offset="1" stopColor="#46290f" />
        </linearGradient>
        {/* Soft top-left sheen to suggest a polished, oiled finish. */}
        <radialGradient id={sheen} cx="0.32" cy="0.26" r="0.7">
          <stop offset="0" stopColor="#fff3da" stopOpacity="0.55" />
          <stop offset="0.4" stopColor="#fff3da" stopOpacity="0.12" />
          <stop offset="1" stopColor="#fff3da" stopOpacity="0" />
        </radialGradient>
      </defs>

      {/* ---- Plinth / base ---- */}
      <g stroke="#3f250f" strokeWidth="1.5" strokeLinejoin="round">
        <rect x="20" y="113" width="80" height="11" rx="5" fill={`url(#${base})`} />
        <rect x="29" y="103" width="62" height="12" rx="4" fill={`url(#${base})`} />
      </g>

      {/* ---- Head + neck silhouette ---- */}
      <path
        d="M84 104
           C88 86 84 67 72 57
           C67 53 62 49 62 41
           C62 35 63 29 66 23
           L72 12 L64 23 L56 12 L50 25
           C42 23 32 27 24 37
           C18 44 12 49 9 57
           C7 61 9 65 14 65
           L26 63
           C30 63 31 67 27 70
           L38 70
           C46 71 52 75 56 83
           C60 91 62 97 66 104
           Z"
        fill={`url(#${wood})`}
        stroke="#3f250f"
        strokeWidth="2"
        strokeLinejoin="round"
      />

      {/* Mane ridge along the back of the neck (carved groove). */}
      <path
        d="M64 26 C61 40 64 58 70 74 C74 84 78 92 80 100"
        fill="none"
        stroke={`url(#${woodDark})`}
        strokeWidth="4"
        strokeLinecap="round"
        opacity="0.75"
      />
      {/* A couple of fine grain lines on the face. */}
      <path
        d="M30 40 C36 44 42 50 46 60"
        fill="none"
        stroke="#5a3617"
        strokeWidth="1.4"
        strokeLinecap="round"
        opacity="0.5"
      />
      <path
        d="M20 52 C26 54 33 58 38 64"
        fill="none"
        stroke="#5a3617"
        strokeWidth="1.2"
        strokeLinecap="round"
        opacity="0.4"
      />

      {/* Eye and nostril. */}
      <circle cx="40" cy="42" r="3.1" fill="#2c1808" />
      <circle cx="39" cy="41" r="1" fill="#e7b578" opacity="0.7" />
      <circle cx="16" cy="58" r="1.7" fill="#2c1808" opacity="0.7" />

      {/* Polished sheen overlay. */}
      <path
        d="M84 104
           C88 86 84 67 72 57
           C67 53 62 49 62 41
           C62 35 63 29 66 23
           L72 12 L64 23 L56 12 L50 25
           C42 23 32 27 24 37
           C18 44 12 49 9 57
           C7 61 9 65 14 65
           L26 63
           C30 63 31 67 27 70
           L38 70
           C46 71 52 75 56 83
           C60 91 62 97 66 104
           Z"
        fill={`url(#${sheen})`}
        stroke="none"
      />
    </svg>
  );
}
