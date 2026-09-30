import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { ThemeProvider, AccentColorProvider } from "@/components/theme";
import { AppToaster } from "@/components/toast";
import { TooltipProvider } from "@/components/ui/tooltip";
import "./globals.css";

const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Admin Dashboard",
  description: "Admin dashboard for the ModularMonolith starter kit.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${inter.variable} h-full antialiased`}
      suppressHydrationWarning
    >
      <body className="flex min-h-full flex-col">
        <ThemeProvider>
          <AccentColorProvider>
            <TooltipProvider>{children}</TooltipProvider>
            <AppToaster />
          </AccentColorProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
