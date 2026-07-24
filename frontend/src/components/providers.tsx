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
    title: "Akiron SEO - AI Visibility Platform",
    subtitle: "Empower your brand across SEO, AIO, GEO & AEO search engines.",
    login: "Sign In",
    register: "Get Started Free",
    dashboard: "Dashboard",
    email: "Email Address",
    password: "Password",
    tenantName: "Organization Name",
    fullName: "Full Name",
    submitLogin: "Sign In to Workspace",
    submitRegister: "Create Multi-Tenant Account",
    switchLang: "Language",
    toggleTheme: "Theme",
    rightsReserved: "© 2026 Akiron SEO. All rights reserved.",
  },
  tr: {
    title: "Akiron SEO - Yapay Zeka Görünürlük Platformu",
    subtitle: "Markanızı SEO, AIO, GEO ve AEO arama motorlarında zirveye taşıyın.",
    login: "Giriş Yap",
    register: "Ücretsiz Başla",
    dashboard: "Kontrol Paneli",
    email: "E-Posta Adresi",
    password: "Şifre",
    tenantName: "Organizasyon / Ajans Adı",
    fullName: "Ad Soyad",
    submitLogin: "Çalışma Alanına Giriş Yap",
    submitRegister: "Çok Kiracılı Hesap Oluştur",
    switchLang: "Dil",
    toggleTheme: "Tema",
    rightsReserved: "© 2026 Akiron SEO. Tüm hakları saklıdır.",
  }
};

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>("dark");
  const [lang, setLang] = useState<Language>("en");
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    // Detect & Restore saved Theme
    const savedTheme = localStorage.getItem("akiron_theme") as Theme;
    if (savedTheme === "light" || savedTheme === "dark") {
      setTheme(savedTheme);
    } else if (window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches) {
      setTheme("light");
    }

    // Detect & Restore saved Language
    const savedLang = localStorage.getItem("akiron_lang") as Language;
    if (savedLang === "tr" || savedLang === "en") {
      setLang(savedLang);
    } else if (navigator.language && navigator.language.toLowerCase().startsWith("tr")) {
      setLang("tr");
    }
  }, []);

  useEffect(() => {
    if (!mounted) return;
    document.documentElement.classList.remove("light", "dark");
    document.documentElement.classList.add(theme);
    localStorage.setItem("akiron_theme", theme);
  }, [theme, mounted]);

  const handleSetLang = (newLang: Language) => {
    setLang(newLang);
    localStorage.setItem("akiron_lang", newLang);
  };

  const toggleTheme = () => {
    setTheme((prev) => (prev === "light" ? "dark" : "light"));
  };

  const t = (key: string): string => {
    return translations[lang][key] || key;
  };

  return (
    <AppContext.Provider value={{ theme, toggleTheme, lang, setLang: handleSetLang, t }}>
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
