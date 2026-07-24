import "./globals.css";
import { AppProviders } from "@/components/providers";

export const metadata = {
  title: "AkironSeo - AI Visibility Platform",
  description: "Enterprise-grade SEO, AIO, GEO & AEO AI Optimization SaaS",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark">
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
