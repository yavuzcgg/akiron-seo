"use client";

import React, { createContext, useContext, useEffect, useState } from "react";

type Theme = "light" | "dark";
type Language = "en" | "tr";

interface AppContextType {
  theme: Theme;
  toggleTheme: () => void;
  lang: Language;
  setLang: (lang: Language) => void;
  t: (key: string) => string;
}

const translations: Record<Language, Record<string, string>> = {
  en: {
    title: "AkironSeo - AI Visibility Platform",
    subtitle: "Empower your brand across SEO, AIO, GEO & AEO search engines.",
    login: "Sign In",
    register: "Get Started Free",
    dashboard: "Dashboard",
    email: "Email Address",
    password: "Password",
    tenantName: "Organization Name",
    submitLogin: "Sign In to Workspace",
    submitRegister: "Create Multi-Tenant Account",
    switchLang: "Language",
    toggleTheme: "Theme",
    verifiedIsolation: "Multi-Tenant Isolation: Verified (Phase 0 Complete)",
    switchAccount: "Switch Organization"
  },
  tr: {
    title: "AkironSeo - Yapay Zeka Görünürlük Platformu",
    subtitle: "Markanızı SEO, AIO, GEO ve AEO arama motorlarında zirveye taşıyın.",
    login: "Giriş Yap",
    register: "Ücretsiz Başla",
    dashboard: "Kontrol Paneli",
    email: "E-Posta Adresi",
    password: "Şifre",
    tenantName: "Organizasyon / Ajans Adı",
    submitLogin: "Çalışma Alanına Giriş Yap",
    submitRegister: "Çok Kiracılı Hesap Oluştur",
    switchLang: "Dil",
    toggleTheme: "Tema",
    verifiedIsolation: "Çok Kiracılı İzolasyon: Doğrulandı (Faz 0 Tamamlandı)",
    switchAccount: "Organizasyon Değiştir"
  }
};

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>("dark");
  const [lang, setLang] = useState<Language>("en");

  useEffect(() => {
    document.documentElement.classList.remove("light", "dark");
    document.documentElement.classList.add(theme);
  }, [theme]);

  const toggleTheme = () => {
    setTheme((prev) => (prev === "light" ? "dark" : "light"));
  };

  const t = (key: string): string => {
    return translations[lang][key] || key;
  };

  return (
    <AppContext.Provider value={{ theme, toggleTheme, lang, setLang, t }}>
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error("useApp must be used within AppProviders");
  }
  return context;
}
