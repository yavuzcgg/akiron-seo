"use client";

import Logo from "@/components/ui/Logo";
import LangToggle from "@/components/ui/LangToggle";
import ThemeToggle from "@/components/ui/ThemeToggle";

interface HeaderProps {
  /** Logo wordmark; e.g. "Akiron SEO Dashboard" on the app shell. */
  label?: string;
  /** Action cluster rendered on the right, before the theme/language toggles. */
  children?: React.ReactNode;
}

/**
 * The one top bar. Previously the logo + theme + language block was copy-pasted
 * into the landing, dashboard, and admin pages, which had already drifted apart.
 */
export default function Header({ label, children }: HeaderProps) {
  return (
    <header className="flex flex-wrap items-center justify-between gap-4 border-b border-border py-3">
      <Logo label={label} />
      <div className="flex items-center gap-2.5">
        {children}
        <LangToggle />
        <ThemeToggle />
      </div>
    </header>
  );
}
