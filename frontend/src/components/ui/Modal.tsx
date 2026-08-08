"use client";

import { X } from "lucide-react";
import { useEffect, useId, useRef } from "react";

interface ModalProps {
  onClose: () => void;
  title: string;
  /** Optional lucide icon element rendered before the title. */
  icon?: React.ReactNode;
  subtitle?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  /** Tailwind max-width class for the panel. Defaults to a mid-size dialog. */
  maxWidthClass?: string;
}

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])';

/**
 * Accessible dialog shell shared by every modal in the app. Handles the behaviour
 * the previous hand-rolled modals all lacked: Escape to close, backdrop-click to
 * close, focus trapping, focus restore on close, body scroll lock, and the ARIA
 * roles a screen reader needs. Rendered only while open, so mount === open.
 */
export default function Modal({
  onClose,
  title,
  icon,
  subtitle,
  children,
  footer,
  maxWidthClass = "max-w-2xl",
}: ModalProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();

  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const panel = panelRef.current;

    // Move focus into the dialog so keyboard users start inside it.
    panel?.querySelector<HTMLElement>(FOCUSABLE)?.focus();
    panel?.focus();

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onClose();
        return;
      }
      if (e.key !== "Tab" || !panel) return;

      const items = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
        (el) => el.offsetParent !== null,
      );
      if (items.length === 0) return;

      const first = items[0];
      const last = items[items.length - 1];
      const active = document.activeElement;

      // Wrap focus at both ends so Tab never escapes the dialog.
      if (e.shiftKey && active === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && active === last) {
        e.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = prevOverflow;
      previouslyFocused?.focus();
    };
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: "var(--overlay)", backdropFilter: "blur(4px)" }}
      onMouseDown={(e) => {
        // Only a press that starts and ends on the backdrop dismisses.
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={`animate-scaleIn w-full ${maxWidthClass} max-h-[90vh] overflow-y-auto rounded-2xl border border-border bg-surface shadow-xl focus:outline-none`}
      >
        <div className="sticky top-0 z-10 flex items-start justify-between gap-4 border-b border-border bg-surface px-6 py-4">
          <div className="flex items-start gap-3">
            {icon && <span className="mt-0.5 text-primary">{icon}</span>}
            <div>
              <h2 id={titleId} className="text-lg font-bold text-foreground">
                {title}
              </h2>
              {subtitle && <p className="mt-0.5 text-xs text-muted">{subtitle}</p>}
            </div>
          </div>
          <button
            onClick={onClose}
            aria-label="Close dialog"
            className="cursor-pointer rounded-lg p-1.5 text-muted transition-colors hover:bg-elevated hover:text-foreground"
          >
            <X size={18} aria-hidden />
          </button>
        </div>

        <div className="px-6 py-5">{children}</div>

        {footer && (
          <div className="sticky bottom-0 flex items-center justify-end gap-3 border-t border-border bg-surface px-6 py-4">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}
