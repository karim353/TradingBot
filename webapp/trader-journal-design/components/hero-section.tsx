"use client"

import { ArrowRight, TrendingUp, Shield, Zap, BarChart3 } from "lucide-react"
import { Button } from "@/components/ui/button"

interface HeroSectionProps {
  onNavigate: (tab: string) => void
}

const stats = [
  { value: "99.2%", label: "Доступность", icon: Zap },
  { value: "50K+", label: "Сделок записано", icon: TrendingUp },
  { value: "256-bit", label: "Шифрование", icon: Shield },
  { value: "Real-time", label: "Аналитика", icon: BarChart3 },
]

export function HeroSection({ onNavigate }: HeroSectionProps) {
  return (
    <section className="relative min-h-screen overflow-hidden bg-background">
      {/* Background layers */}
      <div className="absolute inset-0 bg-grid" />
      <div className="absolute inset-0 bg-radial" />

      {/* Decorative circles */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 h-[600px] w-[600px] rounded-full border border-border/20 opacity-30" />
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 h-[400px] w-[400px] rounded-full border border-border/20 opacity-20" />

      <div className="relative mx-auto max-w-7xl px-4 pt-32 pb-20 sm:px-6 lg:px-8">
        <div className="grid items-center gap-12 lg:grid-cols-2">
          {/* Left side - Content */}
          <div className="flex flex-col gap-8">
            {/* Tag */}
            <div className="flex items-center gap-2 animate-slide-up" style={{ animationDelay: "0.1s" }}>
              <div className="flex items-center gap-2 rounded-full glass px-4 py-1.5">
                <span className="h-2 w-2 rounded-full bg-accent animate-pulse-glow" />
                <span className="text-xs font-medium tracking-wider uppercase text-muted-foreground">
                  Трейдинг-журнал
                </span>
              </div>
            </div>

            {/* Heading */}
            <div className="animate-slide-up" style={{ animationDelay: "0.2s" }}>
              <h1 className="text-4xl font-bold leading-tight tracking-tight text-foreground sm:text-5xl lg:text-6xl text-balance">
                Самый{" "}
                <span className="text-gradient">удобный</span>{" "}
                трейдинг-журнал
              </h1>
            </div>

            {/* Subheading */}
            <p
              className="max-w-lg text-lg leading-relaxed text-muted-foreground animate-slide-up"
              style={{ animationDelay: "0.3s" }}
            >
              Фиксируйте каждую сделку, анализируйте результаты и находите паттерны
              для роста прибыли. Для серьёзных трейдеров.
            </p>

            {/* CTA Buttons */}
            <div
              className="flex flex-wrap items-center gap-4 animate-slide-up"
              style={{ animationDelay: "0.4s" }}
            >
              <Button
                size="lg"
                className="bg-accent text-accent-foreground hover:bg-accent/90 rounded-full px-8 gap-2 group shadow-lg shadow-accent/25"
                onClick={() => onNavigate("dashboard")}
              >
                Открыть дашборд
                <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
              </Button>
              <Button
                variant="outline"
                size="lg"
                className="rounded-full px-8 border-border/50 bg-transparent text-foreground hover:bg-secondary"
                onClick={() => onNavigate("journal")}
              >
                Журнал сделок
              </Button>
            </div>

            {/* Supported */}
            <div
              className="flex items-center gap-4 pt-4 animate-slide-up"
              style={{ animationDelay: "0.5s" }}
            >
              <span className="text-xs font-medium tracking-wider uppercase text-muted-foreground">
                Поддержка
              </span>
              <div className="flex items-center gap-3">
                {["Bybit", "BingX", "Binance", "OKX"].map((exchange) => (
                  <div
                    key={exchange}
                    className="flex h-8 items-center rounded-full glass px-3 text-xs font-medium text-muted-foreground"
                  >
                    {exchange}
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Right side - 3D visual */}
          <div
            className="relative flex items-center justify-center animate-slide-up"
            style={{ animationDelay: "0.3s" }}
          >
            <div className="relative aspect-square w-full max-w-lg">
              {/* Glow behind image */}
              <div className="absolute inset-0 rounded-3xl bg-accent/10 blur-3xl" />

              {/* Hero image */}
              <div className="relative animate-float rounded-3xl overflow-hidden">
                <div className="relative w-full aspect-square rounded-3xl overflow-hidden bg-gradient-to-br from-accent/20 to-accent/5 border border-border/30 flex items-center justify-center">
                  <img
                    src="/app/hero-3d.png"
                    alt="Trading analytics"
                    className="w-full h-full object-contain"
                  />
                </div>
              </div>

              {/* Floating stats card */}
              <div className="absolute -bottom-4 -left-4 glass rounded-2xl p-4 animate-slide-up" style={{ animationDelay: "0.6s" }}>
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-success/20">
                    <TrendingUp className="h-5 w-5 text-success" />
                  </div>
                  <div>
                  <p className="text-sm font-semibold text-foreground">+14.25%</p>
                  <p className="text-xs text-muted-foreground">PnL за неделю</p>
                  </div>
                </div>
              </div>

              {/* Floating badge */}
              <div className="absolute -top-2 -right-2 glass rounded-2xl px-4 py-3 animate-slide-up" style={{ animationDelay: "0.7s" }}>
                <div className="flex items-center gap-2">
                  <Shield className="h-4 w-4 text-accent" />
                  <span className="text-xs font-medium text-foreground">Защищённая аналитика</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Stats bar */}
        <div
          className="mt-20 grid grid-cols-2 gap-4 sm:grid-cols-4 animate-slide-up"
          style={{ animationDelay: "0.6s" }}
        >
          {stats.map((stat) => (
            <div
              key={stat.label}
              className="glass glass-hover rounded-2xl p-6 text-center"
            >
              <stat.icon className="mx-auto mb-3 h-5 w-5 text-accent" />
              <p className="text-2xl font-bold text-foreground">{stat.value}</p>
              <p className="mt-1 text-xs text-muted-foreground">{stat.label}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
