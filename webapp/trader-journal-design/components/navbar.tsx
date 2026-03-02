"use client"

import { useState } from "react"
import { BarChart3, Menu, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface NavbarProps {
  activeTab: string
  onTabChange: (tab: string) => void
}

const navItems = [
  { id: "home", label: "Главная" },
  { id: "dashboard", label: "Дашборд" },
  { id: "journal", label: "Журнал" },
]

const PRO_URL =
  (typeof window !== "undefined" && (window as any).PRO_URL) ||
  process.env.NEXT_PUBLIC_PRO_URL ||
  "http://localhost:3000"

export function Navbar({ activeTab, onTabChange }: NavbarProps) {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 border-b border-white/[0.04] bg-[hsl(240,10%,4%)]/80 backdrop-blur-2xl">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <button
            type="button"
            onClick={() => onTabChange("home")}
            className="flex items-center gap-2.5 transition-all active:scale-[0.97]"
            aria-label="TradeVault, главная"
          >
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-accent to-accent/70">
              <BarChart3 className="h-4 w-4 text-white" />
            </div>
            <span className="text-base font-bold tracking-tight text-foreground">
              TradeVault
            </span>
          </button>

          {/* Desktop pills */}
          <div className="hidden items-center rounded-xl bg-white/[0.04] p-1 md:flex">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => onTabChange(item.id)}
                className={cn(
                  "rounded-lg px-4 py-2 text-[13px] font-medium transition-all duration-200",
                  activeTab === item.id
                    ? "bg-accent/20 text-white shadow-sm shadow-accent/10"
                    : "text-muted-foreground hover:text-foreground"
                )}
                aria-label={item.label}
                aria-current={activeTab === item.id ? "page" : undefined}
              >
                {item.label}
              </button>
            ))}
          </div>

          <div className="hidden items-center gap-2 md:flex">
            <Button
              variant="outline"
              size="sm"
              className="rounded-lg border-white/[0.06] bg-transparent text-sm text-muted-foreground hover:bg-white/5 hover:text-foreground"
              onClick={() => onTabChange("journal")}
              aria-label="Добавить сделку"
            >
              Добавить сделку
            </Button>
            <Button
              size="sm"
              className="rounded-lg bg-accent text-white text-sm hover:bg-accent/90 shadow-lg shadow-accent/25"
              onClick={() => onTabChange("dashboard")}
              aria-label="Перейти к дашборду"
            >
              Дашборд
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="rounded-lg border-white/[0.08] bg-white/5 text-xs text-white/80 hover:bg-white/10"
              onClick={() => {
                if (typeof window !== "undefined") {
                  window.location.href = PRO_URL
                }
              }}
            >
              TradeLog Pro
            </Button>
          </div>

          <button
            type="button"
            className="md:hidden text-foreground p-2"
            onClick={() => setMobileOpen(!mobileOpen)}
            aria-label={mobileOpen ? "Закрыть меню" : "Открыть меню"}
            aria-expanded={mobileOpen}
          >
            {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>
      </div>

      {mobileOpen ? (
        <div className="border-t border-white/[0.04] bg-[hsl(240,10%,4%)]/95 backdrop-blur-2xl md:hidden animate-fade-in">
          <div className="flex flex-col gap-1 p-4">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => { onTabChange(item.id); setMobileOpen(false) }}
                className={cn(
                  "rounded-lg px-4 py-3 text-left text-sm font-medium transition-all",
                  activeTab === item.id
                    ? "bg-accent/15 text-white"
                    : "text-muted-foreground hover:text-foreground hover:bg-white/[0.04]"
                )}
              >
                {item.label}
              </button>
            ))}
            <div className="mt-3 flex gap-2 border-t border-white/[0.04] pt-3">
              <Button variant="outline" size="sm" className="flex-1 rounded-lg border-white/[0.06]" onClick={() => { onTabChange("journal"); setMobileOpen(false) }} aria-label="Добавить сделку">
                Добавить сделку
              </Button>
              <Button size="sm" className="flex-1 rounded-lg bg-accent" onClick={() => { onTabChange("dashboard"); setMobileOpen(false) }} aria-label="Дашборд">
                Дашборд
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </nav>
  )
}
