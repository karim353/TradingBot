"use client"

import { useState, useMemo, useCallback, useEffect } from "react"
import { WidgetWrapper } from "@/components/widgets/widget-wrapper"
import { PnlChartWidget } from "@/components/widgets/pnl-chart-widget"
import { TradingViewWidget } from "@/components/widgets/tradingview-widget"
import { TradingViewTickerWidget } from "@/components/widgets/tradingview-ticker-widget"
import { StatsOverviewWidget } from "@/components/widgets/stats-overview-widget"
import { SessionPerformanceWidget } from "@/components/widgets/session-performance-widget"
import { ResultDistributionWidget } from "@/components/widgets/result-distribution-widget"
import { DailyPnlWidget } from "@/components/widgets/daily-pnl-widget"
import { DashboardWidgetPicker } from "@/components/dashboard-widget-picker"
import type { Trade } from "@/lib/types"
import { calculateStats, getDailyPnL } from "@/lib/trade-store"
import {
  WIDGET_LABELS,
  getDashboardConfig,
  type WidgetId,
} from "@/lib/dashboard-config"

interface DashboardSectionProps {
  trades: Trade[]
  loading?: boolean
}

export function DashboardSection({ trades, loading = false }: DashboardSectionProps) {
  const [widgetIds, setWidgetIds] = useState<WidgetId[]>([])

  useEffect(() => {
    setWidgetIds(getDashboardConfig())
  }, [])

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
      <section className="min-h-screen bg-background pt-24 pb-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="glass rounded-2xl p-12 text-center animate-pulse">
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

  return (
    <section className="min-h-screen bg-background pt-24 pb-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between animate-slide-up">
          <div>
            <h2 className="text-3xl font-bold tracking-tight text-foreground">
              Дашборд
            </h2>
            <p className="mt-1 text-muted-foreground">
              Ваша торговля в одном месте — настраивайте виджеты под себя
            </p>
          </div>
          <DashboardWidgetPicker currentWidgets={widgetIds} onApply={handleApplyWidgets} />
        </div>

        {/* Widget Grid */}
        <div className="grid gap-6 lg:grid-cols-2">
          {widgetIds.map((id) => renderWidget(id))}
        </div>
      </div>
    </section>
  )
}
