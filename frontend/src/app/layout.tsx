import "./globals.css";
import { AppProviders } from "@/components/providers";
import { Fira_Code, Fira_Sans } from "next/font/google";

const firaSans = Fira_Sans({
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
  variable: "--font-fira-sans",
  display: "swap",
});

const firaCode = Fira_Code({
  subsets: ["latin"],
  variable: "--font-fira-code",
  display: "swap",
});

export const metadata = {
  title: "Akiron SEO — AI Visibility Platform",
  description: "Multi-tenant SEO & Generative Engine Optimization for agencies.",
};

// Runs before first paint so the saved theme is applied without a flash of the
// wrong one. Dark is the default identity when nothing is stored. The providers
// re-sync on mount; this only wins the race before hydration.
const noFlashTheme = `
(function () {
  try {
    var t = localStorage.getItem('akiron_theme');
    if (t !== 'light' && t !== 'dark') {
      t = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    }
    document.documentElement.classList.add(t);
  } catch (e) {
    document.documentElement.classList.add('dark');
  }
})();
`;

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      className={`${firaSans.variable} ${firaCode.variable}`}
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: noFlashTheme }} />
      </head>
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
