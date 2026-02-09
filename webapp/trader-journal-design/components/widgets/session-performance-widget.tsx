"use client"

import type { Trade } from "@/lib/types"

interface SessionPerformanceWidgetProps {
  trades: Trade[]
}

export function SessionPerformanceWidget({ trades }: SessionPerformanceWidgetProps) {
  const sessions = ["ASIA", "LONDON", "NEW YORK", "FRANKFURT"]
  const sessionData = sessions.map((session) => {
    const sessionTrades = trades.filter((t) => t.session === session)
    const wins = sessionTrades.filter((t) => t.result === "TP").length
    const total = sessionTrades.length
    return {
      session,
      winRate: total > 0 ? Math.round((wins / total) * 100) : 0,
      trades: total,
    }
  })

  return (
    <div className="flex flex-col gap-4">
      {sessionData.map((s) => (
        <div key={s.session} className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium text-foreground">{s.session}</span>
            <span className="text-sm font-mono text-muted-foreground">
              {s.winRate}% ({s.trades} trades)
            </span>
          </div>
          <div className="h-2 w-full overflow-hidden rounded-full bg-secondary">
            <div
              className="h-full rounded-full transition-all duration-1000 ease-out"
              style={{
                width: `${s.winRate}%`,
                backgroundColor:
                  s.winRate >= 60 ? "hsl(142, 76%, 46%)" : s.winRate >= 40 ? "hsl(45, 93%, 58%)" : "hsl(0, 72%, 51%)",
              }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}
