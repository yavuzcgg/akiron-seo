import { useId } from "react";

interface BrandMarkProps {
  /** Rendered width and height in pixels. */
  size?: number;
  className?: string;
}

/**
 * The Akiron SEO mark: a rising "A" with an amber visibility dot. This is the
 * same artwork as src/app/icon.svg (favicon, apple-icon, PWA icons are rendered
 * from that file), so keep the two in sync when the mark changes.
 */
export default function BrandMark({ size = 36, className }: BrandMarkProps) {
  const gradientId = `akiron-mark-${useId().replace(/:/g, "")}`;
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 64 64"
      width={size}
      height={size}
      className={className}
      aria-hidden
      focusable="false"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="64" y2="64" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#3b82f6" />
          <stop offset="1" stopColor="#1e3a8a" />
        </linearGradient>
      </defs>
      <rect width="64" height="64" rx="15" fill={`url(#${gradientId})`} />
      <path
        d="M15 49 L30 15 L45 49"
        fill="none"
        stroke="#ffffff"
        strokeWidth="7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="M22.5 37 H37.5" fill="none" stroke="#ffffff" strokeWidth="6" strokeLinecap="round" />
      <circle cx="47" cy="17" r="6.5" fill="#fbbf24" />
    </svg>
  );
}
