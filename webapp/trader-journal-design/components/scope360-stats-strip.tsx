"use client"

import { memo, useMemo } from "react"
import { TrendingUp, TrendingDown, Target, Award, Activity, Flame, ArrowUpRight, ArrowDownRight } from "lucide-react"
import { cn } from "@/lib/utils"
import type { Stats, Trade } from "@/lib/types"

interface Scope360StatsStripProps {
  stats: Stats
  trades?: Trade[]
  className?: string
}

function fmt(v: number): string {
  return v >= 0 ? `+${v.toFixed(2)}` : v.toFixed(2)
}

export const Scope360StatsStrip = memo(function Scope360StatsStrip({ stats, trades, className }: Scope360StatsStripProps) {
  const extra = useMemo(() => {
    if (!trades || trades.length === 0) return { avgWin: 0, avgLoss: 0, expectancy: 0, maxDrawdown: 0 }
    const wins = trades.filter((t) => t.result === "TP")
    const losses = trades.filter((t) => t.result === "SL")
    const avgWin = wins.length > 0 ? wins.reduce((s, t) => s + t.pnl, 0) / wins.length : 0
    const avgLoss = losses.length > 0 ? losses.reduce((s, t) => s + t.pnl, 0) / losses.length : 0
    const winP = wins.length / trades.length
    const lossP = losses.length / trades.length
    const expectancy = winP * avgWin + lossP * avgLoss

    let peak = 0
    let running = 0
    let maxDD = 0
    for (const t of [...trades].sort((a, b) => a.date.localeCompare(b.date))) {
      running += t.pnl
      if (running > peak) peak = running
      const dd = peak - running
      if (dd > maxDD) maxDD = dd
    }

    return { avgWin, avgLoss, expectancy, maxDrawdown: maxDD }
  }, [trades])

  const items: { label: string; value: string; sub?: string; positive?: boolean; icon: typeof TrendingUp }[] = [
    { label: "Trades", value: String(stats.totalTrades), icon: Activity },
    { label: "Win Rate", value: `${stats.winRate.toFixed(1)}%`, positive: stats.winRate >= 50, icon: Target },
    { label: "Total PnL", value: `${fmt(stats.totalPnl)}%`, positive: stats.totalPnl >= 0, icon: stats.totalPnl >= 0 ? TrendingUp : TrendingDown },
    { label: "Profit Factor", value: stats.profitFactor === Infinity ? "∞" : stats.profitFactor.toFixed(2), positive: stats.profitFactor > 1, icon: Award },
    { label: "Win / Loss", value: `${fmt(stats.winSum)} / ${stats.lossSum > 0 ? `-${stats.lossSum.toFixed(2)}` : "0.00"}`, icon: ArrowUpRight },
    { label: "Avg RR", value: `1:${stats.avgRR.toFixed(1)}`, icon: Activity },
    { label: "Best / Worst", value: `${fmt(stats.bestTrade)} / ${stats.worstTrade.toFixed(2)}`, icon: Flame },
    { label: "Streak", value: `${stats.streak > 0 ? "+" : ""}${stats.streak}`, positive: stats.streak > 0, icon: Flame },
  ]

  if (trades && trades.length > 0) {
    items.push(
      { label: "Avg Win", value: `${fmt(extra.avgWin)}`, positive: true, icon: ArrowUpRight },
      { label: "Avg Loss", value: `${extra.avgLoss.toFixed(2)}`, positive: false, icon: ArrowDownRight },
      { label: "Expectancy", value: `${fmt(extra.expectancy)}`, positive: extra.expectancy > 0, icon: Target },
      { label: "Max DD", value: `${extra.maxDrawdown.toFixed(2)}%`, positive: false, icon: TrendingDown },
    )
  }

  return (
    <div className={cn("grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2", className)}>
      {items.map(({ label, value, positive, icon: Icon }) => (
        <div
          key={label}
          className="group rounded-xl border border-white/[0.06] bg-white/[0.02] px-3 py-3 transition-all hover:border-accent/15 hover:bg-accent/[0.03]"
        >
          <div className="flex items-center gap-1.5">
            <Icon className="h-3 w-3 text-muted-foreground/40 group-hover:text-accent/60 transition-colors" />
            <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/50">
              {label}
            </span>
          </div>
          <div
            className={cn(
              "mt-1 text-base font-bold tabular-nums font-mono",
              positive === true && "text-success",
              positive === false && "text-destructive",
              positive === undefined && "text-foreground"
            )}
          >
            {value}
          </div>
        </div>
      ))}
    </div>
  )
})
