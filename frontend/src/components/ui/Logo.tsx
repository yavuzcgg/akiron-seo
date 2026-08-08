"use client";

import Link from "next/link";

interface LogoProps {
  /** Wordmark shown next to the mark; omit for a mark-only logo. */
  label?: string;
  href?: string;
}

export default function Logo({ label = "Akiron SEO", href = "/" }: LogoProps) {
  return (
    <Link href={href} className="flex items-center gap-2.5">
      <span
        className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-lg font-extrabold text-on-primary shadow-md"
        aria-hidden
      >
        A
      </span>
      {label && (
        <span className="text-lg font-extrabold tracking-tight text-foreground">{label}</span>
      )}
    </Link>
  );
}
