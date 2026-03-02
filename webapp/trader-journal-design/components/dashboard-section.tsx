"use client"

import { useState, useMemo, useCallback } from "react"
import dynamic from "next/dynamic"
import { Settings2, MoreVertical } from "lucide-react"
import { WidgetWrapper } from "@/components/widgets/widget-wrapper"
import { StatsOverviewWidget } from "@/components/widgets/stats-overview-widget"
import { DashboardWidgetPicker } from "@/components/dashboard-widget-picker"
import { Scope360StatsStrip } from "@/components/scope360-stats-strip"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { cn } from "@/lib/utils"
import type { Trade } from "@/lib/types"
import { calculateStats, getDailyPnL } from "@/lib/trade-store"
import {
  WIDGET_LABELS,
  getDashboardConfig,
  type WidgetId,
} from "@/lib/dashboard-config"

const PnlChartWidget = dynamic(
  () => import("@/components/widgets/pnl-chart-widget").then((m) => m.PnlChartWidget),
  { ssr: false }
)
const TradingViewWidget = dynamic(
  () => import("@/components/widgets/tradingview-widget").then((m) => m.TradingViewWidget),
  { ssr: false }
)
const TradingViewTickerWidget = dynamic(
  () => import("@/components/widgets/tradingview-ticker-widget").then((m) => m.TradingViewTickerWidget),
  { ssr: false }
)
const SessionPerformanceWidget = dynamic(
  () => import("@/components/widgets/session-performance-widget").then((m) => m.SessionPerformanceWidget),
  { ssr: false }
)
const ResultDistributionWidget = dynamic(
  () => import("@/components/widgets/result-distribution-widget").then((m) => m.ResultDistributionWidget),
  { ssr: false }
)
const DailyPnlWidget = dynamic(
  () => import("@/components/widgets/daily-pnl-widget").then((m) => m.DailyPnlWidget),
  { ssr: false }
)
const TradingCalendarWidget = dynamic(
  () => import("@/components/widgets/trading-calendar-widget").then((m) => m.TradingCalendarWidget),
  { ssr: false }
)

type PeriodKey = "all" | "q" | "m" | "w" | "custom"

interface DashboardSectionProps {
  trades: Trade[]
  loading?: boolean
  fromDate?: string
  toDate?: string
  tickerFilter?: string
  tickers?: string[]
  onFromChange?: (v: string) => void
  onToChange?: (v: string) => void
  onTickerChange?: (v: string) => void
  onPresetApply?: (from: string, to: string) => void
  onApply?: () => void
}

function presetRange(days: number): { from: string; to: string } {
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - days)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

function formatDateRange(from: string, to: string): string {
  try {
    const fromD = new Date(from + "T00:00:00")
    const toD = new Date(to + "T00:00:00")
    const opts: Intl.DateTimeFormatOptions = { day: "numeric", month: "short" }
    return `${fromD.toLocaleDateString("ru-RU", opts)} – ${toD.toLocaleDateString("ru-RU", opts)}`
  } catch {
    return from && to ? `${from} – ${to}` : "Период"
  }
}

function derivePeriod(fromDate: string, toDate: string): PeriodKey {
  if (!fromDate || !toDate) return "custom"
  const { from: wFrom, to: wTo } = presetRange(7)
  const { from: mFrom, to: mTo } = presetRange(30)
  const { from: qFrom, to: qTo } = presetRange(90)
  const { from: aFrom, to: aTo } = presetRange(365)
  if (fromDate === wFrom && toDate === wTo) return "w"
  if (fromDate === mFrom && toDate === mTo) return "m"
  if (fromDate === qFrom && toDate === qTo) return "q"
  if (fromDate === aFrom && toDate === aTo) return "all"
  return "custom"
}

export function DashboardSection({
  trades,
  loading = false,
  fromDate = "",
  toDate = "",
  tickerFilter = "all",
  tickers = [],
  onFromChange,
  onToChange,
  onTickerChange,
  onPresetApply,
  onApply,
}: DashboardSectionProps) {
  const [widgetIds, setWidgetIds] = useState<WidgetId[]>(() => getDashboardConfig())
  const period = useMemo(() => derivePeriod(fromDate, toDate), [fromDate, toDate])
  const dateRangeLabel = useMemo(() => formatDateRange(fromDate, toDate), [fromDate, toDate])

  const handleApplyWidgets = useCallback((widgets: WidgetId[]) => {
    setWidgetIds(widgets)
  }, [])

  const stats = useMemo(() => calculateStats(trades), [trades])
  const dailyPnl = useMemo(() => getDailyPnL(trades), [trades])
  const cumulativePnl = useMemo(() => {
    let running = 0
    return dailyPnl.map((d) => {
      running += d.pnl
      return {
        ...d,
        cumulative: Number(running.toFixed(2)),
        displayDate: d.date.slice(5),
      }
    })
  }, [dailyPnl])

  if (loading) {
    return (
      <section className="min-h-screen pt-24 pb-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-white/[0.06] bg-white/[0.02] p-16 text-center">
            <div className="mx-auto mb-4 h-8 w-8 animate-spin rounded-full border-2 border-accent/30 border-t-accent" />
            <p className="text-muted-foreground">Загрузка дашборда...</p>
          </div>
        </div>
      </section>
    )
  }

  const renderWidget = (id: WidgetId) => {
    switch (id) {
      case "stats-overview":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <StatsOverviewWidget trades={trades} />
          </WidgetWrapper>
        )
      case "pnl-chart":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <PnlChartWidget data={cumulativePnl} totalPnl={stats.totalPnl} />
          </WidgetWrapper>
        )
      case "daily-pnl":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <DailyPnlWidget trades={trades} />
          </WidgetWrapper>
        )
      case "trading-calendar":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <TradingCalendarWidget trades={trades} />
          </WidgetWrapper>
        )
      case "result-distribution":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <ResultDistributionWidget trades={trades} />
          </WidgetWrapper>
        )
      case "session-performance":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]}>
            <SessionPerformanceWidget trades={trades} />
          </WidgetWrapper>
        )
      case "tradingview-ticker":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]} noPadding>
            <div className="p-4">
              <TradingViewTickerWidget />
            </div>
          </WidgetWrapper>
        )
      case "tradingview-chart":
        return (
          <WidgetWrapper key={id} id={id} title={WIDGET_LABELS[id]} noPadding>
            <div className="p-4">
              <TradingViewWidget />
            </div>
          </WidgetWrapper>
        )
      default:
        return null
    }
  }

  const onPeriodTab = useCallback(
    (key: PeriodKey) => {
      if (key === "custom" || !onPresetApply) return
      const days = key === "all" ? 365 : key === "q" ? 90 : key === "m" ? 30 : 7
      const { from, to } = presetRange(days)
      onPresetApply(from, to)
    },
    [onPresetApply]
  )

  const periodTabs: { key: PeriodKey; label: string }[] = [
    { key: "all", label: "All" },
    { key: "q", label: "Q" },
    { key: "m", label: "M" },
    { key: "w", label: "W" },
    { key: "custom", label: "Custom" },
  ]

  return (
    <section className="relative min-h-screen pt-24 pb-20 overflow-hidden">
      <div className="absolute top-0 right-0 h-[400px] w-[400px] rounded-full bg-accent/[0.04] blur-[120px]" />
      <div className="absolute bottom-40 left-0 h-[300px] w-[300px] rounded-full bg-indigo-500/[0.03] blur-[100px]" />

      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Scope360-style top bar */}
        <div className="sticky top-16 z-30 -mx-4 border-b border-white/[0.06] bg-[hsl(240,10%,4%)]/95 backdrop-blur-xl sm:-mx-6 lg:-mx-8">
          <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
            <div className="flex items-center gap-6">
              <h1 className="text-lg font-semibold text-foreground">Dashboard</h1>
              <div className="flex items-center gap-0.5 rounded-lg border border-white/[0.06] bg-white/[0.02] p-0.5">
                {periodTabs.map(({ key, label }) => (
                  <button
                    key={key}
                    type="button"
                    onClick={() => onPeriodTab(key)}
                    className={cn(
                      "rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors",
                      period === key
                        ? "bg-white/10 text-foreground"
                        : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex items-center gap-2">
              {onTickerChange && tickers.length >= 0 && (
                <Select value={tickerFilter} onValueChange={onTickerChange}>
                  <SelectTrigger className="h-8 w-[140px] rounded-lg border-white/[0.06] bg-white/[0.02] text-xs text-foreground">
                    <SelectValue placeholder="Тикер" />
                  </SelectTrigger>
                  <SelectContent className="border-white/[0.06] bg-card text-foreground">
                    <SelectItem value="all">Все тикеры</SelectItem>
                    {tickers.map((t) => (
                      <SelectItem key={t} value={t}>{t}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              <span className="text-xs text-muted-foreground tabular-nums">
                {dateRangeLabel}
              </span>
              <DashboardWidgetPicker variant="toolbar" currentWidgets={widgetIds} onApply={handleApplyWidgets} />
              <button
                type="button"
                className="rounded-lg p-2 text-muted-foreground hover:bg-white/[0.06] hover:text-foreground"
                aria-label="Ещё"
              >
                <MoreVertical className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>

        {/* Single summary bar (Scope360) */}
        <div className="mt-4 flex flex-wrap items-center gap-6 border-b border-white/[0.04] pb-4 text-sm">
          <span className="tabular-nums text-muted-foreground">
            Winrate <span className="font-semibold text-foreground">{stats.winRate.toFixed(0)}%</span>
          </span>
          <span className="tabular-nums text-muted-foreground">
            Profit factor{" "}
            <span className="font-semibold text-foreground">
              {stats.profitFactor === Infinity ? "∞" : stats.profitFactor.toFixed(2)}
            </span>
          </span>
          <span className="tabular-nums text-muted-foreground">
            Total trades{" "}
            <span className="font-semibold text-foreground">{stats.totalTrades}</span>
          </span>
          <span className="tabular-nums text-muted-foreground">
            Commission{" "}
            <span className="font-semibold text-foreground">$0</span>
          </span>
          <span className="tabular-nums text-muted-foreground">
            P&L{" "}
            <span
              className={cn(
                "font-semibold",
                stats.totalPnl >= 0 ? "text-success" : "text-destructive"
              )}
            >
              {stats.totalPnl >= 0 ? "+" : ""}{stats.totalPnl.toFixed(2)}%
            </span>
          </span>
        </div>

        {/* Scope360-style stats strip */}
        <div className="mb-8 mt-6 animate-slide-up" style={{ animationDelay: "0.1s" }}>
          <Scope360StatsStrip stats={stats} trades={trades} />
        </div>

        {/* Widget Grid */}
        <div className="grid gap-5 lg:grid-cols-2">
          {widgetIds.map((id) => renderWidget(id))}
        </div>
      </div>
    </section>
  )
}
