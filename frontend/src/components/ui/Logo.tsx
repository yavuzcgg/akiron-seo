"use client";

import BrandMark from "@/components/ui/BrandMark";
import Link from "next/link";

interface LogoProps {
  /** Wordmark shown next to the mark; omit for a mark-only logo. */
  label?: string;
  href?: string;
}

export default function Logo({ label = "Akiron SEO", href = "/" }: LogoProps) {
  return (
    <Link href={href} className="flex items-center gap-2.5">
      <BrandMark size={36} className="shrink-0 drop-shadow-sm" />
      {label && (
        <span className="text-lg font-extrabold tracking-tight text-foreground">{label}</span>
      )}
    </Link>
  );
}
