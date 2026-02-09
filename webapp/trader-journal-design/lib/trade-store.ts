import type { Trade, DailyPnL, Stats } from "./types"

// Fallback sample data when API is unavailable (dev/demo)
export const sampleTrades: Trade[] = [
  {
    id: "1",
    ticker: "BTC/USDT",
    date: "2026-02-01",
    account: "Bybit",
    session: "NEW YORK",
    position: "LONG",
    direction: "Reversal",
    context: ["Bullish divergence", "Support test"],
    setup: ["SweepBosRTO"],
    result: "TP",
    rr: 3.2,
    risk: 1.0,
    pnl: 3.2,
    entryDetails: "Entry at 97,500 after sweep of daily low",
    comment: "Clean setup, followed the plan",
    note: "Market showed strong buying pressure at support",
    emotions: ["Confident", "Focused"],
    screenshots: [],
  },
  {
    id: "2",
    ticker: "ETH/USDT",
    date: "2026-02-01",
    account: "Bybit",
    session: "LONDON",
    position: "SHORT",
    direction: "Continuation",
    context: ["Bearish structure"],
    setup: ["Range Provocation"],
    result: "SL",
    rr: 2.0,
    risk: 0.5,
    pnl: -0.5,
    entryDetails: "Shorted at resistance with bearish engulfing",
    comment: "Market reversed unexpectedly",
    note: "Should have waited for confirmation",
    emotions: ["Anxious"],
    screenshots: [],
  },
  {
    id: "3",
    ticker: "SOL/USDT",
    date: "2026-02-02",
    account: "BingX",
    session: "ASIA",
    position: "LONG",
    direction: "Long",
    context: ["Breakout"],
    setup: ["BR=>IL=>OL"],
    result: "TP",
    rr: 2.5,
    risk: 1.0,
    pnl: 2.5,
    entryDetails: "Bought breakout of daily resistance",
    comment: "Perfect execution",
    note: "",
    emotions: ["Confident", "Calm"],
    screenshots: [],
  },
  {
    id: "4",
    ticker: "BTC/USDT",
    date: "2026-02-02",
    account: "Bybit",
    session: "NEW YORK",
    position: "LONG",
    direction: "Reversal",
    context: ["Bullish engulfing", "Order block"],
    setup: ["SweepBosRTO"],
    result: "TP",
    rr: 4.0,
    risk: 1.0,
    pnl: 4.0,
    entryDetails: "Entry on order block retest",
    comment: "Great risk to reward",
    note: "Best trade this week",
    emotions: ["Focused", "Calm"],
    screenshots: [],
  },
  {
    id: "5",
    ticker: "ETH/USDT",
    date: "2026-02-03",
    account: "Binance",
    session: "LONDON",
    position: "SHORT",
    direction: "Short",
    context: ["Resistance rejection"],
    setup: ["Range Provocation"],
    result: "BE",
    rr: 2.0,
    risk: 0.75,
    pnl: 0,
    entryDetails: "Shorted at weekly resistance",
    comment: "Moved to BE early",
    note: "Good risk management",
    emotions: ["Nervous", "Uncertain"],
    screenshots: [],
  },
  {
    id: "6",
    ticker: "DOGE/USDT",
    date: "2026-02-03",
    account: "OKX",
    session: "ASIA",
    position: "LONG",
    direction: "Continuation",
    context: ["Bullish trend"],
    setup: ["SweepBosRTO"],
    result: "TP",
    rr: 1.8,
    risk: 0.5,
    pnl: 0.9,
    entryDetails: "Trend following entry",
    comment: "Small but consistent",
    note: "",
    emotions: ["Calm"],
    screenshots: [],
  },
  {
    id: "7",
    ticker: "BTC/USDT",
    date: "2026-02-04",
    account: "Bybit",
    session: "NEW YORK",
    position: "SHORT",
    direction: "Reversal",
    context: ["Bearish divergence"],
    setup: ["BR=>IL=>OL"],
    result: "SL",
    rr: 3.0,
    risk: 1.0,
    pnl: -1.0,
    entryDetails: "Counter-trend short at resistance",
    comment: "Shouldn't have fought the trend",
    note: "Lesson: don't counter-trend in strong moves",
    emotions: ["Frustrated", "Anxious"],
    screenshots: [],
  },
  {
    id: "8",
    ticker: "XRP/USDT",
    date: "2026-02-04",
    account: "BingX",
    session: "FRANKFURT",
    position: "LONG",
    direction: "Long",
    context: ["Support bounce"],
    setup: ["Range Provocation"],
    result: "TP",
    rr: 2.2,
    risk: 0.75,
    pnl: 1.65,
    entryDetails: "Bought dip into support zone",
    comment: "Textbook setup",
    note: "",
    emotions: ["Confident", "Focused"],
    screenshots: [],
  },
  {
    id: "9",
    ticker: "ETH/USDT",
    date: "2026-02-05",
    account: "Bybit",
    session: "LONDON",
    position: "LONG",
    direction: "Reversal",
    context: ["Double bottom"],
    setup: ["SweepBosRTO"],
    result: "TP",
    rr: 3.5,
    risk: 1.0,
    pnl: 3.5,
    entryDetails: "Entry on double bottom confirmation",
    comment: "Beautiful reversal pattern",
    note: "Patience paid off",
    emotions: ["Excited", "Confident"],
    screenshots: [],
  },
  {
    id: "10",
    ticker: "BTC/USDT",
    date: "2026-02-05",
    account: "Bybit",
    session: "NEW YORK",
    position: "LONG",
    direction: "Continuation",
    context: ["Bullish flag"],
    setup: ["BR=>IL=>OL"],
    result: "SL",
    rr: 2.0,
    risk: 0.5,
    pnl: -0.5,
    entryDetails: "Flag breakout long",
    comment: "False breakout",
    note: "Volume was low, should have noticed",
    emotions: ["Frustrated"],
    screenshots: [],
  },
]

export function calculateStats(trades: Trade[]): Stats {
  if (trades.length === 0) {
    return {
      totalTrades: 0,
      winRate: 0,
      totalPnl: 0,
      avgRR: 0,
      bestTrade: 0,
      worstTrade: 0,
      profitFactor: 0,
      streak: 0,
    }
  }

  const wins = trades.filter((t) => t.result === "TP")
  const losses = trades.filter((t) => t.result === "SL")
  const totalPnl = trades.reduce((sum, t) => sum + t.pnl, 0)
  const avgRR = trades.reduce((sum, t) => sum + t.rr, 0) / trades.length
  const bestTrade = Math.max(...trades.map((t) => t.pnl))
  const worstTrade = Math.min(...trades.map((t) => t.pnl))
  const grossProfit = wins.reduce((sum, t) => sum + t.pnl, 0)
  const grossLoss = Math.abs(losses.reduce((sum, t) => sum + t.pnl, 0))
  const profitFactor = grossLoss > 0 ? grossProfit / grossLoss : grossProfit > 0 ? Infinity : 0

  // Calculate current streak
  let streak = 0
  for (let i = trades.length - 1; i >= 0; i--) {
    if (i === trades.length - 1) {
      streak = trades[i].result === "TP" ? 1 : trades[i].result === "SL" ? -1 : 0
    } else {
      const currentWin = trades[i].result === "TP"
      const streakPositive = streak > 0
      if (currentWin && streakPositive) streak++
      else if (!currentWin && !streakPositive && trades[i].result === "SL") streak--
      else break
    }
  }

  return {
    totalTrades: trades.length,
    winRate: (wins.length / trades.length) * 100,
    totalPnl,
    avgRR,
    bestTrade,
    worstTrade,
    profitFactor,
    streak,
  }
}

export function getDailyPnL(trades: Trade[]): DailyPnL[] {
  const dailyMap = new Map<string, { pnl: number; trades: number }>()

  for (const trade of trades) {
    const existing = dailyMap.get(trade.date) || { pnl: 0, trades: 0 }
    dailyMap.set(trade.date, {
      pnl: existing.pnl + trade.pnl,
      trades: existing.trades + 1,
    })
  }

  return Array.from(dailyMap.entries())
    .map(([date, data]) => ({
      date,
      pnl: Number(data.pnl.toFixed(2)),
      trades: data.trades,
    }))
    .sort((a, b) => a.date.localeCompare(b.date))
}
