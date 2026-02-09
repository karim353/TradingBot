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

export function Navbar({ activeTab, onTabChange }: NavbarProps) {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 glass-strong">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <button
            type="button"
            onClick={() => onTabChange("home")}
            className="flex items-center gap-2 transition-opacity hover:opacity-80"
          >
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-accent">
              <BarChart3 className="h-4 w-4 text-accent-foreground" />
            </div>
            <span className="text-lg font-bold tracking-tight text-foreground">
              TradeVault
            </span>
          </button>

          {/* Desktop nav */}
          <div className="hidden items-center gap-1 md:flex">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => onTabChange(item.id)}
                className={cn(
                  "relative rounded-xl px-4 py-2.5 text-sm font-medium transition-all duration-200",
                  activeTab === item.id
                    ? "text-foreground bg-accent/10"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/50"
                )}
              >
                {item.label}
                {activeTab === item.id && (
                  <span className="absolute bottom-1 left-1/2 h-0.5 w-6 -translate-x-1/2 rounded-full bg-accent" />
                )}
              </button>
            ))}
          </div>

          {/* Desktop CTA */}
          <div className="hidden items-center gap-3 md:flex">
            <Button
              variant="outline"
              size="sm"
              className="rounded-xl border-border/50 bg-transparent text-foreground hover:bg-secondary"
              onClick={() => onTabChange("journal")}
            >
              Добавить сделку
            </Button>
            <Button
              size="sm"
              className="rounded-xl bg-accent text-accent-foreground hover:bg-accent/90"
              onClick={() => onTabChange("dashboard")}
            >
              Дашборд
            </Button>
          </div>

          {/* Mobile menu toggle */}
          <button
            type="button"
            className="md:hidden text-foreground p-2"
            onClick={() => setMobileOpen(!mobileOpen)}
            aria-label="Toggle menu"
          >
            {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="glass-strong border-t border-border/30 md:hidden animate-fade-in">
          <div className="flex flex-col gap-1 p-4">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => {
                  onTabChange(item.id)
                  setMobileOpen(false)
                }}
                className={cn(
                  "rounded-xl px-4 py-3 text-left text-sm font-medium transition-all",
                  activeTab === item.id
                    ? "bg-accent/10 text-foreground"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/50"
                )}
              >
                {item.label}
              </button>
            ))}
            <div className="mt-3 flex gap-2 border-t border-border/20 pt-3">
              <Button
                variant="outline"
                size="sm"
                className="flex-1 rounded-xl"
                onClick={() => { onTabChange("journal"); setMobileOpen(false) }}
              >
                Добавить сделку
              </Button>
              <Button
                size="sm"
                className="flex-1 rounded-xl bg-accent"
                onClick={() => { onTabChange("dashboard"); setMobileOpen(false) }}
              >
                Дашборд
              </Button>
            </div>
          </div>
        </div>
      )}
    </nav>
  )
}
