import type { ProTrade } from "./api"

export function parsePnlNumber(trade: ProTrade): number {
  const s = (trade.pnl || "").replace(/%/g, "").trim()
  const n = parseFloat(s)
  return Number.isFinite(n) ? n : 0
}

export function getEquityCurveData(trades: ProTrade[]): { name: string; value: number }[] {
  if (!trades?.length) return []
  const sorted = [...trades].sort((a, b) => (a.dateIso ?? String(a.id)).localeCompare(b.dateIso ?? String(b.id)) || a.id - b.id)
  let cum = 0
  return sorted.map((t, i) => {
    cum += parsePnlNumber(t)
    return { name: String(i + 1), value: Math.round(cum * 100) / 100 }
  })
}

export function getMonthlyPnLData(trades: ProTrade[]): { month: string; pnl: number }[] {
  if (!trades?.length) return []
  const byMonth = new Map<string, number>()
  for (const t of trades) {
    const month = t.dateIso ? t.dateIso.slice(0, 7) : ""
    if (!month) continue
    const prev = byMonth.get(month) ?? 0
    byMonth.set(month, prev + parsePnlNumber(t))
  }
  return Array.from(byMonth.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([month, pnl]) => ({ month, pnl: Math.round(pnl * 100) / 100 }))
}

export function getWinLoss(trades: ProTrade[]): { wins: number; losses: number; total: number; winRatePct: number } {
  if (!trades?.length) return { wins: 0, losses: 0, total: 0, winRatePct: 0 }
  const wins = trades.filter((t) => t.positive).length
  const total = trades.length
  const losses = total - wins
  const winRatePct = total ? Math.round((wins / total) * 1000) / 10 : 0
  return { wins, losses, total, winRatePct }
}

export function getLongShortStats(trades: ProTrade[]): {
  long: { count: number; pnlSum: number; wins: number }
  short: { count: number; pnlSum: number; wins: number }
} {
  const long = trades.filter((t) => t.type === "LONG")
  const short = trades.filter((t) => t.type === "SHORT")
  return {
    long: {
      count: long.length,
      pnlSum: Math.round(long.reduce((s, t) => s + parsePnlNumber(t), 0) * 100) / 100,
      wins: long.filter((t) => t.positive).length,
    },
    short: {
      count: short.length,
      pnlSum: Math.round(short.reduce((s, t) => s + parsePnlNumber(t), 0) * 100) / 100,
      wins: short.filter((t) => t.positive).length,
    },
  }
}

export function getAssetAllocation(trades: ProTrade[]): { symbol: string; count: number; pnlSum: number }[] {
  if (!trades?.length) return []
  const bySymbol = new Map<string, { count: number; pnlSum: number }>()
  for (const t of trades) {
    const symbol = t.pair?.split("/")[0]?.trim() || "OTHER"
    const cur = bySymbol.get(symbol) ?? { count: 0, pnlSum: 0 }
    cur.count += 1
    cur.pnlSum += parsePnlNumber(t)
    bySymbol.set(symbol, cur)
  }
  return Array.from(bySymbol.entries())
    .map(([symbol, v]) => ({ symbol, count: v.count, pnlSum: Math.round(v.pnlSum * 100) / 100 }))
    .sort((a, b) => b.count - a.count)
}

export function getTradingMetrics(trades: ProTrade[]): {
  winRatePct: number
  profitFactor: number
  maxDrawdownPct: number
  expectancyPct: number
  totalTrades: number
} {
  const { wins, total, winRatePct } = getWinLoss(trades)
  if (!total) return { winRatePct: 0, profitFactor: 0, maxDrawdownPct: 0, expectancyPct: 0, totalTrades: 0 }

  const pnls = trades.map(parsePnlNumber)
  const grossProfit = pnls.filter((p) => p > 0).reduce((a, b) => a + b, 0)
  const grossLoss = Math.abs(pnls.filter((p) => p < 0).reduce((a, b) => a + b, 0))
  const profitFactor = grossLoss > 0 ? Math.round((grossProfit / grossLoss) * 100) / 100 : (grossProfit > 0 ? 99 : 0)

  let peak = 0
  let maxDd = 0
  let cum = 0
  for (const p of [...trades].sort((a, b) => (a.dateIso ?? "").localeCompare(b.dateIso ?? "")).map(parsePnlNumber)) {
    cum += p
    if (cum > peak) peak = cum
    const dd = peak - cum
    if (dd > maxDd) maxDd = dd
  }
  const maxDrawdownPct = Math.round(maxDd * 100) / 100
  const expectancyPct = Math.round((pnls.reduce((a, b) => a + b, 0) / total) * 100) / 100

  return {
    winRatePct,
    profitFactor,
    maxDrawdownPct,
    expectancyPct,
    totalTrades: total,
  }
}

export function getStrategyStats(trades: ProTrade[]): {
  strategy: string
  count: number
  avgPnl: number
  winRatePct: number
}[] {
  if (!trades?.length) return []
  const byStrategy = new Map<string, { count: number; pnlSum: number; wins: number }>()
  for (const t of trades) {
    const key = t.strategy?.trim() || "Unspecified"
    const cur = byStrategy.get(key) ?? { count: 0, pnlSum: 0, wins: 0 }
    cur.count += 1
    const pnl = parsePnlNumber(t)
    cur.pnlSum += pnl
    if (t.positive) cur.wins += 1
    byStrategy.set(key, cur)
  }
  return Array.from(byStrategy.entries())
    .map(([strategy, v]) => ({
      strategy,
      count: v.count,
      avgPnl: v.count ? Math.round((v.pnlSum / v.count) * 100) / 100 : 0,
      winRatePct: v.count ? Math.round((v.wins / v.count) * 1000) / 10 : 0,
    }))
    .sort((a, b) => b.count - a.count)
}

export function getEmotionStats(trades: ProTrade[]): {
  emotion: string
  count: number
  wins: number
  winRatePct: number
}[] {
  if (!trades?.length) return []
  const byEmotion = new Map<string, { count: number; wins: number }>()
  for (const t of trades) {
    const key = t.emotion?.trim() || "Neutral"
    const cur = byEmotion.get(key) ?? { count: 0, wins: 0 }
    cur.count += 1
    if (t.positive) cur.wins += 1
    byEmotion.set(key, cur)
  }
  return Array.from(byEmotion.entries())
    .map(([emotion, v]) => ({
      emotion,
      count: v.count,
      wins: v.wins,
      winRatePct: v.count ? Math.round((v.wins / v.count) * 1000) / 10 : 0,
    }))
    .sort((a, b) => b.count - a.count)
}

export function getStreakStats(trades: ProTrade[]): {
  currentStreak: number
  currentType: "win" | "loss" | null
  maxWinStreak: number
  maxLossStreak: number
} {
  if (!trades?.length) {
    return { currentStreak: 0, currentType: null, maxWinStreak: 0, maxLossStreak: 0 }
  }

  const sorted = [...trades].sort((a, b) => (a.dateIso ?? String(a.id)).localeCompare(b.dateIso ?? String(b.id)) || a.id - b.id)

  let currentStreak = 0
  let currentType: "win" | "loss" | null = null
  let maxWinStreak = 0
  let maxLossStreak = 0

  for (const t of sorted) {
    const type: "win" | "loss" = t.positive ? "win" : "loss"
    if (currentType === type) {
      currentStreak += 1
    } else {
      currentType = type
      currentStreak = 1
    }
    if (type === "win" && currentStreak > maxWinStreak) maxWinStreak = currentStreak
    if (type === "loss" && currentStreak > maxLossStreak) maxLossStreak = currentStreak
  }

  return { currentStreak, currentType, maxWinStreak, maxLossStreak }
}

export function getJournalQuality(trades: ProTrade[]): {
  withNotesPct: number
  withScreenshotPct: number
  avgNoteLength: number
} {
  if (!trades?.length) return { withNotesPct: 0, withScreenshotPct: 0, avgNoteLength: 0 }
  let notesCount = 0
  let screenshotCount = 0
  let totalLen = 0

  for (const t of trades) {
    const hasNotes = !!t.notes && t.notes.trim() && t.notes.trim() !== "No notes provided."
    if (hasNotes) {
      notesCount += 1
      totalLen += t.notes!.trim().length
    }
    if (t.image) screenshotCount += 1
  }

  const total = trades.length
  const withNotesPct = Math.round((notesCount / total) * 100)
  const withScreenshotPct = Math.round((screenshotCount / total) * 100)
  const avgNoteLength = notesCount ? Math.round(totalLen / notesCount) : 0

  return { withNotesPct, withScreenshotPct, avgNoteLength }
}
