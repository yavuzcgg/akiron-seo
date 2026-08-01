"use client";

import { getToken, isSuperAdmin } from "@/lib/session";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

interface AuthGuardProps {
  children: React.ReactNode;
  /** Requires the SuperAdmin role in addition to a session. */
  requireSuperAdmin?: boolean;
}

/**
 * Client-side gate that keeps unauthenticated visitors out of the app shell.
 *
 * The token lives in localStorage, so a server-side proxy cannot read it — this
 * check runs in the browser and is a usability boundary, not the security one.
 * Authorization is enforced by the API (see AdminEndpoints' SuperAdmin policy).
 */
export default function AuthGuard({ children, requireSuperAdmin = false }: AuthGuardProps) {
  const router = useRouter();
  const [allowed, setAllowed] = useState(false);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }

    if (requireSuperAdmin && !isSuperAdmin()) {
      router.replace("/dashboard");
      return;
    }

    setAllowed(true);
  }, [router, requireSuperAdmin]);

  if (!allowed) {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm text-slate-400">
        Checking your session…
      </div>
    );
  }

  return <>{children}</>;
}
