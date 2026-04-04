import type { Metadata } from "next";

import "./globals.css";

export const metadata: Metadata = {
  title: "Library Lite",
  description: "Minimal frontend scaffold for the Library Lite experience."
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
