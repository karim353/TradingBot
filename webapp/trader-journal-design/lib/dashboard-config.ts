export const WIDGET_IDS = [
  "stats-overview",
  "pnl-chart",
  "daily-pnl",
  "result-distribution",
  "session-performance",
  "tradingview-ticker",
  "tradingview-chart",
] as const

export type WidgetId = (typeof WIDGET_IDS)[number]

export const WIDGET_LABELS: Record<WidgetId, string> = {
  "stats-overview": "Сводка статистики",
  "pnl-chart": "Кумулятивный PnL",
  "daily-pnl": "Дневной PnL",
  "result-distribution": "Распределение TP/SL/BE",
  "session-performance": "Эффективность по сессиям",
  "tradingview-ticker": "Курсы криптовалют (TradingView)",
  "tradingview-chart": "График (TradingView)",
}

const STORAGE_KEY = "trader-dashboard-widgets"

export function getDashboardConfig(): WidgetId[] {
  if (typeof window === "undefined") {
    return ["stats-overview", "pnl-chart", "tradingview-ticker", "result-distribution", "session-performance", "daily-pnl", "tradingview-chart"]
  }
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      const parsed = JSON.parse(stored) as string[]
      return parsed.filter((id): id is WidgetId => WIDGET_IDS.includes(id as WidgetId))
    }
  } catch {
    // ignore
  }
  return ["stats-overview", "pnl-chart", "tradingview-ticker", "result-distribution", "session-performance", "daily-pnl", "tradingview-chart"]
}

export function saveDashboardConfig(widgets: WidgetId[]): void {
  if (typeof window === "undefined") return
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(widgets))
  } catch {
    // ignore
  }
}
