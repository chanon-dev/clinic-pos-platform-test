import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/shared/lib/auth";

export const metadata: Metadata = {
  title: "Clinic POS",
  description: "Clinic Point-of-Service Platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="th">
      <body className="bg-gray-50 min-h-screen antialiased">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
