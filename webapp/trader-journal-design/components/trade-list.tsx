"use client"

import { memo, useMemo } from "react"
import { cn } from "@/lib/utils"
import type { Trade } from "@/lib/types"
import { ArrowUpRight, ArrowDownRight, Minus, ChevronRight } from "lucide-react"

interface TradeListProps {
  trades: Trade[]
  onSelectTrade: (trade: Trade) => void
}

export const TradeList = memo(function TradeList({ trades, onSelectTrade }: TradeListProps) {
  const sortedTrades = useMemo(
    () => [...trades].sort((a, b) => b.date.localeCompare(a.date) || Number(b.id) - Number(a.id)),
    [trades]
  )

  return (
    <div className="flex flex-col gap-2">
      {sortedTrades.map((trade, index) => (
        <button
          key={trade.id}
          type="button"
          onClick={() => onSelectTrade(trade)}
          className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-4 text-left w-full group flex flex-col gap-3 transition-all hover:border-accent/20 hover:bg-accent/[0.03] animate-slide-up"
          style={{ animationDelay: `${index * 0.04}s` }}
        >
          <div className="flex items-center justify-between gap-4 w-full min-w-0">
            <div className="flex items-center gap-3 min-w-0 flex-1">
              <div
                className={cn(
                  "flex h-10 w-10 shrink-0 items-center justify-center rounded-lg",
                  trade.result === "TP"
                    ? "bg-success/10"
                    : trade.result === "SL"
                      ? "bg-destructive/10"
                      : "bg-white/[0.06]"
                )}
              >
                {trade.result === "TP" ? (
                  <ArrowUpRight className="h-5 w-5 text-success" />
                ) : trade.result === "SL" ? (
                  <ArrowDownRight className="h-5 w-5 text-destructive" />
                ) : (
                  <Minus className="h-5 w-5 text-muted-foreground" />
                )}
              </div>

              <div className="min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm font-semibold text-foreground">{trade.ticker}</span>
                  <span
                    className={cn(
                      "rounded-md px-1.5 py-0.5 text-[10px] font-semibold uppercase",
                      trade.position === "LONG"
                        ? "bg-success/10 text-success"
                        : "bg-destructive/10 text-destructive"
                    )}
                  >
                    {trade.position}
                  </span>
                  <span className="rounded-md bg-white/[0.06] px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                    {trade.result}
                  </span>
                </div>
                <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground/70">
                  <span>{trade.date}</span>
                  <span className="text-white/10">|</span>
                  <span>{trade.session}</span>
                  <span className="text-white/10">|</span>
                  <span>{trade.account}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3 text-right shrink-0">
              <div>
                <p
                  className={cn(
                    "text-sm font-bold font-mono tabular-nums",
                    trade.pnl > 0 ? "text-success" : trade.pnl < 0 ? "text-destructive" : "text-muted-foreground"
                  )}
                >
                  {trade.pnl > 0 ? "+" : ""}{trade.pnl.toFixed(2)}
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground/60 font-mono">
                  RR 1:{trade.rr.toFixed(1)}
                </p>
              </div>
              <ChevronRight className="h-4 w-4 text-muted-foreground/30 transition-all group-hover:translate-x-0.5 group-hover:text-accent" />
            </div>
          </div>

          {trade.emotions.length > 0 ? (
            <div className="flex flex-wrap gap-1.5 pt-2 border-t border-white/[0.04]">
              {trade.emotions.map((emotion) => (
                <span
                  key={emotion}
                  className="rounded-md bg-white/[0.06] px-2 py-0.5 text-[10px] font-medium text-muted-foreground/70"
                >
                  {emotion}
                </span>
              ))}
            </div>
          ) : null}
        </button>
      ))}
    </div>
  )
})
