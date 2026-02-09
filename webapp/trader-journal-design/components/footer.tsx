"use client"

import { BarChart3 } from "lucide-react"

export function Footer() {
  return (
    <footer className="border-t border-border/20 bg-background/80 backdrop-blur-sm">
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-accent/20">
              <BarChart3 className="h-4 w-4 text-accent" />
            </div>
            <span className="text-sm font-medium text-foreground">TradeVault</span>
          </div>
          <p className="text-xs text-muted-foreground">
            Трейдинг-журнал для анализа и улучшения торговли
          </p>
        </div>
      </div>
    </footer>
  )
}
