"use client";

import { ApiError, SESSION_EXPIRED_EVENT } from "@/lib/apiClient";
import { Language, TranslationKey, translations } from "@/lib/i18n";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import React, { createContext, useContext, useEffect, useState, useSyncExternalStore } from "react";

type Theme = "light" | "dark";

interface AppContextType {
  theme: Theme;
  toggleTheme: () => void;
  lang: Language;
  setLang: (lang: Language) => void;
  t: (key: TranslationKey) => string;
}

const THEME_KEY = "akiron_theme";
const LANGUAGE_KEY = "akiron_lang";
const PREFERENCES_EVENT = "akiron:preferences-changed";

const AppContext = createContext<AppContextType | undefined>(undefined);

function subscribePreferences(onStoreChange: () => void): () => void {
  window.addEventListener("storage", onStoreChange);
  window.addEventListener(PREFERENCES_EVENT, onStoreChange);
  return () => {
    window.removeEventListener("storage", onStoreChange);
    window.removeEventListener(PREFERENCES_EVENT, onStoreChange);
  };
}

function getThemeSnapshot(): Theme {
  const saved = localStorage.getItem(THEME_KEY);
  if (saved === "light" || saved === "dark") return saved;
  return window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark";
}

function getLanguageSnapshot(): Language {
  const saved = localStorage.getItem(LANGUAGE_KEY);
  if (saved === "en" || saved === "tr") return saved;
  return navigator.language.toLowerCase().startsWith("tr") ? "tr" : "en";
}

function emitPreferencesChanged(): void {
  window.dispatchEvent(new Event(PREFERENCES_EVENT));
}

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 1,
      },
      mutations: { retry: false },
    },
  }));
  const theme = useSyncExternalStore<Theme>(subscribePreferences, getThemeSnapshot, () => "dark");
  const lang = useSyncExternalStore<Language>(subscribePreferences, getLanguageSnapshot, () => "en");

  useEffect(() => {
    document.documentElement.classList.remove("light", "dark");
    document.documentElement.classList.add(theme);
  }, [theme]);

  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  useEffect(() => {
    const handleSessionExpired = () => {
      queryClient.clear();
      if (!window.location.pathname.startsWith("/login")) {
        window.location.assign("/login");
      }
    };
    window.addEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired);
  }, [queryClient]);

  const setLang = (nextLanguage: Language) => {
    localStorage.setItem(LANGUAGE_KEY, nextLanguage);
    emitPreferencesChanged();
  };

  const toggleTheme = () => {
    localStorage.setItem(THEME_KEY, theme === "light" ? "dark" : "light");
    emitPreferencesChanged();
  };

  const value: AppContextType = {
    theme,
    toggleTheme,
    lang,
    setLang,
    t: (key) => translations[lang][key],
  };

  return (
    <QueryClientProvider client={queryClient}>
      <AppContext.Provider value={value}>
        <a href="#main-content" className="sr-only z-[100] rounded-lg bg-primary px-4 py-3 font-semibold text-on-primary focus:not-sr-only focus:fixed focus:left-4 focus:top-4">
          {translations[lang].skipToContent}
        </a>
        {children}
      </AppContext.Provider>
    </QueryClientProvider>
  );
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error("useApp must be used within AppProviders");
  }
  return context;
}
