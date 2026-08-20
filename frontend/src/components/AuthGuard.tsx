"use client";

import { useApp } from "@/components/providers";
import { SUPER_ADMIN_ROLE, useSession } from "@/hooks/useSession";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

interface AuthGuardProps {
  children: React.ReactNode;
  requireSuperAdmin?: boolean;
}

export default function AuthGuard({ children, requireSuperAdmin = false }: AuthGuardProps) {
  const { t } = useApp();
  const router = useRouter();
  const session = useSession();
  const isForbidden = requireSuperAdmin && session.data?.role !== SUPER_ADMIN_ROLE;

  useEffect(() => {
    if (session.isError) {
      router.replace("/login");
    } else if (session.isSuccess && isForbidden) {
      router.replace("/dashboard");
    }
  }, [isForbidden, router, session.isError, session.isSuccess]);

  if (session.isPending || session.isError || isForbidden) {
    return (
      <div className="flex min-h-dvh items-center justify-center text-sm text-muted" role="status">
        {t("sessionChecking")}
      </div>
    );
  }

  return <>{children}</>;
}
