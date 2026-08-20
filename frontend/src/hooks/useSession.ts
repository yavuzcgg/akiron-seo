"use client";

import { apiClient } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";

export const SUPER_ADMIN_ROLE = "SuperAdmin";

export function useSession() {
  return useQuery({
    queryKey: queryKeys.session,
    queryFn: apiClient.auth.session,
    retry: false,
    staleTime: 60_000,
  });
}

export function useLogout() {
  const queryClient = useQueryClient();
  const router = useRouter();

  return useMutation({
    mutationFn: apiClient.auth.logout,
    onSettled: async () => {
      queryClient.clear();
      router.replace("/login");
      router.refresh();
    },
  });
}
