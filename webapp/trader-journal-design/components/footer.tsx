"use client"

import { BarChart3, LineChart } from "lucide-react"

export function Footer() {
  return (
    <footer className="border-t border-white/[0.04]">
      <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="flex flex-col items-center justify-between gap-6 sm:flex-row">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-accent/30 to-accent/10">
              <BarChart3 className="h-4 w-4 text-accent" />
            </div>
            <span className="text-sm font-semibold text-foreground">TradeVault</span>
          </div>
          <p className="text-center text-sm text-muted-foreground/70">
            Трейдинг-журнал для анализа и улучшения торговли
          </p>
          <a
            href="/metrics-dashboard.html"
            className="inline-flex items-center gap-2 rounded-lg border border-white/[0.06] px-4 py-2 text-sm font-medium text-muted-foreground transition-all hover:bg-white/[0.04] hover:text-foreground"
          >
            <LineChart className="h-4 w-4" />
            Метрики
          </a>
        </div>
      </div>
    </footer>
  )
}
