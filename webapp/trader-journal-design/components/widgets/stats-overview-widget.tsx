"use client"

import { Target, DollarSign, Award, Flame, Activity, TrendingUp, TrendingDown, BarChart3 } from "lucide-react"
import { StatCard } from "@/components/stat-card"
import type { Trade } from "@/lib/types"
import { calculateStats } from "@/lib/trade-store"

interface StatsOverviewWidgetProps {
  trades: Trade[]
}

export function StatsOverviewWidget({ trades }: StatsOverviewWidgetProps) {
  const stats = calculateStats(trades)

  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <StatCard
        label="Win Rate"
        value={`${stats.winRate.toFixed(1)}%`}
        positive
        icon={Target}
      />
      <StatCard
        label="Total PnL"
        value={`${stats.totalPnl >= 0 ? "+" : ""}${stats.totalPnl.toFixed(2)}%`}
        positive={stats.totalPnl >= 0}
        icon={DollarSign}
      />
      <StatCard
        label="Profit Factor"
        value={stats.profitFactor === Infinity ? "∞" : stats.profitFactor.toFixed(2)}
        positive
        icon={Award}
      />
      <StatCard
        label="Streak"
        value={`${stats.streak > 0 ? "+" : ""}${stats.streak}`}
        positive={stats.streak > 0}
        icon={stats.streak >= 0 ? Flame : Activity}
      />
      <StatCard label="Trades" value={stats.totalTrades.toString()} icon={BarChart3} />
      <StatCard label="Avg RR" value={`1:${stats.avgRR.toFixed(1)}`} icon={Activity} />
      <StatCard label="Best" value={`+${stats.bestTrade.toFixed(2)}%`} icon={TrendingUp} />
      <StatCard label="Worst" value={`${stats.worstTrade.toFixed(2)}%`} icon={TrendingDown} />
    </div>
  )
}
