import React from "react"
import type { Metadata, Viewport } from "next"
import { Plus_Jakarta_Sans, JetBrains_Mono } from "next/font/google"
import { ClientToaster } from "@/components/client-toaster"

import "./globals.css"

const plusJakarta = Plus_Jakarta_Sans({
  subsets: ["latin", "cyrillic"],
  variable: "--font-sans",
  display: "swap",
})
const jetbrainsMono = JetBrains_Mono({ subsets: ["latin"], variable: "--font-mono" })

export const metadata: Metadata = {
  title: 'TradeVault | Трейдинг-журнал',
  description: 'Фиксируйте сделки, анализируйте результаты и оптимизируйте торговлю с удобным визуальным журналом.',
}

export const viewport: Viewport = {
  themeColor: '#000000',
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="ru" className={`${plusJakarta.variable} ${jetbrainsMono.variable}`}>
      <body className="font-sans antialiased min-h-screen bg-background text-foreground">
        {children}
        <ClientToaster />
      </body>
    </html>
  )
}
