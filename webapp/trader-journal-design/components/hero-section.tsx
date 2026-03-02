"use client"

import dynamic from "next/dynamic"
import { ArrowRight, TrendingUp, Shield, BarChart3, Activity, Zap, BookOpen, PieChart } from "lucide-react"
import { Button } from "@/components/ui/button"

const HeroIllustration = dynamic(
  () => import("@/components/hero-illustration").then((m) => m.HeroIllustration),
  { ssr: false }
)

interface HeroSectionProps {
  onNavigate: (tab: string) => void
  tradeCount?: number
  totalPnl?: number
}

const features = [
  {
    icon: BarChart3,
    title: "50+ метрик",
    desc: "Win rate, drawdown, profit factor, R:R и многое другое",
  },
  {
    icon: Zap,
    title: "Авто-синхронизация",
    desc: "Подключите биржу по API — сделки импортируются сами",
  },
  {
    icon: BookOpen,
    title: "Журнал + контекст",
    desc: "Скриншоты, эмоции, setup, context — всё в одном месте",
  },
  {
    icon: PieChart,
    title: "Кастомные дашборды",
    desc: "Настройте виджеты под свой стиль торговли",
  },
]

export function HeroSection({ onNavigate, tradeCount, totalPnl }: HeroSectionProps) {
  return (
    <section className="relative overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0 bg-grid" />
      <div className="absolute inset-0 bg-radial-hero" />
      <div className="absolute inset-0 bg-noise" />

      {/* Animated gradient orbs */}
      <div className="absolute top-10 left-[10%] h-[500px] w-[500px] rounded-full bg-accent/[0.08] blur-[140px] animate-float-slow" />
      <div className="absolute top-[40%] right-[5%] h-[400px] w-[400px] rounded-full bg-indigo-500/[0.06] blur-[120px] animate-float-slow" style={{ animationDelay: "-3s" }} />
      <div className="absolute bottom-10 left-[30%] h-[300px] w-[300px] rounded-full bg-purple-600/[0.05] blur-[100px] animate-float-slow" style={{ animationDelay: "-6s" }} />

      <div className="relative mx-auto max-w-7xl px-4 pt-28 pb-12 sm:px-6 lg:px-8">
        {/* Main hero: 2-column layout */}
        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
          {/* Left */}
          <div className="flex flex-col gap-7">
            {/* Badge */}
            <div className="animate-slide-up" style={{ animationDelay: "0.1s" }}>
              <div className="inline-flex items-center gap-2.5 rounded-full border border-accent/20 bg-accent/[0.06] px-4 py-2">
                <span className="relative flex h-2 w-2">
                  <span className="absolute inline-flex h-full w-full rounded-full bg-accent animate-pulse-ring" />
                  <span className="relative inline-flex h-2 w-2 rounded-full bg-accent" />
                </span>
                <span className="text-xs font-semibold tracking-widest uppercase text-accent">
                  Трейдинг-журнал
                </span>
              </div>
            </div>

            {/* Heading */}
            <h1
              className="text-4xl font-extrabold leading-[1.08] tracking-tight text-foreground sm:text-5xl lg:text-6xl text-balance animate-slide-up"
              style={{ animationDelay: "0.2s" }}
            >
              Знай свой{" "}
              <span className="text-gradient">edge</span>.{" "}
              <br className="hidden sm:block" />
              Торгуй с{" "}
              <span className="text-gradient-warm">уверенностью</span>.
            </h1>

            {/* Subtitle */}
            <p
              className="max-w-md text-base leading-relaxed text-muted-foreground sm:text-lg animate-slide-up"
              style={{ animationDelay: "0.3s" }}
            >
              Фиксируйте каждую сделку, анализируйте результаты и находите паттерны для роста прибыли.
            </p>

            {/* CTA */}
            <div className="flex flex-wrap items-center gap-3 animate-slide-up" style={{ animationDelay: "0.4s" }}>
              <Button
                size="lg"
                className="rounded-xl px-7 gap-2 group bg-accent text-white hover:bg-accent/90 shadow-xl shadow-accent/30 transition-all hover:shadow-accent/40 hover:-translate-y-0.5 text-sm font-semibold"
                onClick={() => onNavigate("dashboard")}
              >
                Начать бесплатно
                <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
              </Button>
              <Button
                variant="outline"
                size="lg"
                className="rounded-xl px-7 border-white/[0.08] bg-white/[0.03] text-foreground hover:bg-white/[0.06] text-sm font-semibold"
                onClick={() => onNavigate("journal")}
              >
                Журнал сделок
              </Button>
            </div>

            {/* Live stats row */}
            <div className="flex items-center gap-6 animate-slide-up" style={{ animationDelay: "0.5s" }}>
              {tradeCount != null ? (
                <div className="flex items-center gap-2">
                  <Activity className="h-4 w-4 text-accent/60" />
                  <span className="text-sm font-mono font-semibold text-foreground tabular-nums">{tradeCount}</span>
                  <span className="text-xs text-muted-foreground/60">сделок</span>
                </div>
              ) : null}
              {totalPnl != null ? (
                <div className="flex items-center gap-2">
                  <TrendingUp className="h-4 w-4 text-success/60" />
                  <span className={`text-sm font-mono font-semibold tabular-nums ${totalPnl >= 0 ? "text-success" : "text-destructive"}`}>
                    {totalPnl >= 0 ? "+" : ""}{totalPnl.toFixed(2)}
                  </span>
                  <span className="text-xs text-muted-foreground/60">PnL</span>
                </div>
              ) : null}
            </div>

            {/* Exchange badges */}
            <div className="flex items-center gap-2.5 animate-slide-up" style={{ animationDelay: "0.55s" }}>
              <span className="text-[10px] font-semibold tracking-wider uppercase text-muted-foreground/50">Биржи</span>
              {["Bybit", "BingX", "Binance", "OKX"].map((ex) => (
                <span
                  key={ex}
                  className="rounded-md border border-white/[0.06] bg-white/[0.02] px-2.5 py-1 text-[11px] font-medium text-muted-foreground/70"
                >
                  {ex}
                </span>
              ))}
            </div>
          </div>

          {/* Right: Illustration + floating cards */}
          <div className="relative flex items-center justify-center animate-slide-up" style={{ animationDelay: "0.3s" }}>
            <HeroIllustration />

            {/* Floating card: PnL */}
            <div
              className="absolute -bottom-2 -left-4 rounded-xl border border-white/[0.08] bg-[hsl(240,8%,8%)]/90 backdrop-blur-xl p-3.5 shadow-2xl animate-float-slow"
              style={{ animationDelay: "-1s" }}
            >
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-success/10">
                  <TrendingUp className="h-4 w-4 text-success" />
                </div>
                <div>
                  <p className="text-sm font-bold text-foreground tabular-nums font-mono">
                    {totalPnl != null ? `${totalPnl >= 0 ? "+" : ""}${totalPnl.toFixed(2)}` : "+12.45"}%
                  </p>
                  <p className="text-[10px] text-muted-foreground/60">Total PnL</p>
                </div>
              </div>
            </div>

            {/* Floating card: Shield */}
            <div
              className="absolute -top-2 right-4 rounded-xl border border-white/[0.08] bg-[hsl(240,8%,8%)]/90 backdrop-blur-xl px-3.5 py-2.5 shadow-2xl animate-float-slow"
              style={{ animationDelay: "-4s" }}
            >
              <div className="flex items-center gap-2">
                <Shield className="h-4 w-4 text-accent" />
                <span className="text-[11px] font-semibold text-foreground">Защищённая аналитика</span>
              </div>
            </div>

            {/* Floating card: Win Rate */}
            <div
              className="absolute top-1/2 -right-6 rounded-xl border border-white/[0.08] bg-[hsl(240,8%,8%)]/90 backdrop-blur-xl p-3 shadow-2xl animate-float-slow"
              style={{ animationDelay: "-2.5s" }}
            >
              <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/60">Win Rate</p>
              <p className="text-lg font-bold text-accent tabular-nums font-mono">67.2%</p>
            </div>
          </div>
        </div>

        {/* Features grid */}
        <div className="mt-28 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {features.map((f, i) => (
            <div
              key={f.title}
              className="group relative rounded-2xl border border-white/[0.06] bg-white/[0.02] p-6 transition-all hover:border-accent/20 hover:bg-accent/[0.03] animate-slide-up"
              style={{ animationDelay: `${0.6 + i * 0.1}s` }}
            >
              <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-xl bg-accent/10 transition-colors group-hover:bg-accent/20">
                <f.icon className="h-5 w-5 text-accent" />
              </div>
              <h3 className="text-sm font-semibold text-foreground">{f.title}</h3>
              <p className="mt-1.5 text-xs leading-relaxed text-muted-foreground/70">{f.desc}</p>
            </div>
          ))}
        </div>

        {/* Social proof strip */}
        <div
          className="mt-16 flex flex-col items-center gap-4 animate-slide-up"
          style={{ animationDelay: "1s" }}
        >
          <p className="text-xs font-semibold tracking-wider uppercase text-muted-foreground/50">
            Трейдеры уже используют TradeVault
          </p>
          <div className="flex items-center gap-6">
            <div className="text-center">
              <p className="text-2xl font-bold text-foreground tabular-nums">500+</p>
              <p className="text-[10px] text-muted-foreground/50">Трейдеров</p>
            </div>
            <div className="h-8 w-px bg-white/[0.06]" />
            <div className="text-center">
              <p className="text-2xl font-bold text-foreground tabular-nums">50K+</p>
              <p className="text-[10px] text-muted-foreground/50">Сделок</p>
            </div>
            <div className="h-8 w-px bg-white/[0.06]" />
            <div className="text-center">
              <p className="text-2xl font-bold text-foreground tabular-nums">99.9%</p>
              <p className="text-[10px] text-muted-foreground/50">Uptime</p>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
