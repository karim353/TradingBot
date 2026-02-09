"use client"

import { cn } from "@/lib/utils"
import type { Trade } from "@/lib/types"
import { ArrowUpRight, ArrowDownRight, Minus, ChevronRight } from "lucide-react"

interface TradeListProps {
  trades: Trade[]
  onSelectTrade: (trade: Trade) => void
}

export function TradeList({ trades, onSelectTrade }: TradeListProps) {
  const sortedTrades = [...trades].sort((a, b) => b.date.localeCompare(a.date) || Number(b.id) - Number(a.id))

  return (
    <div className="flex flex-col gap-3">
      {sortedTrades.map((trade, index) => (
        <button
          key={trade.id}
          type="button"
          onClick={() => onSelectTrade(trade)}
          className="glass glass-hover rounded-2xl p-4 text-left animate-slide-up w-full group flex items-center gap-3"
          style={{ animationDelay: `${index * 0.05}s` }}
        >
          <div className="flex items-center justify-between">
            {/* Left: ticker, date, session */}
            <div className="flex items-center gap-4">
              {/* Result icon */}
              <div
                className={cn(
                  "flex h-10 w-10 items-center justify-center rounded-xl",
                  trade.result === "TP"
                    ? "bg-success/15"
                    : trade.result === "SL"
                      ? "bg-destructive/15"
                      : "bg-muted"
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

              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-foreground">{trade.ticker}</span>
                  <span
                    className={cn(
                      "rounded-md px-2 py-0.5 text-[10px] font-semibold uppercase",
                      trade.position === "LONG"
                        ? "bg-success/15 text-success"
                        : "bg-destructive/15 text-destructive"
                    )}
                  >
                    {trade.position}
                  </span>
                  <span className="rounded-md bg-secondary px-2 py-0.5 text-[10px] font-medium text-muted-foreground">
                    {trade.result}
                  </span>
                </div>
                <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                  <span>{trade.date}</span>
                  <span className="text-border">|</span>
                  <span>{trade.session}</span>
                  <span className="text-border">|</span>
                  <span>{trade.account}</span>
                </div>
              </div>
            </div>

            {/* Right: PnL, RR, chevron */}
            <div className="flex items-center gap-3 text-right">
              <div>
              <p
                className={cn(
                  "text-sm font-bold font-mono",
                  trade.pnl > 0
                    ? "text-success"
                    : trade.pnl < 0
                      ? "text-destructive"
                      : "text-muted-foreground"
                )}
              >
                {trade.pnl > 0 ? "+" : ""}
                {trade.pnl.toFixed(2)}%
              </p>
              <p className="mt-0.5 text-xs text-muted-foreground font-mono">
                RR 1:{trade.rr.toFixed(1)}
              </p>
              </div>
              <ChevronRight className="h-5 w-5 text-muted-foreground/50 transition-transform group-hover:translate-x-0.5 group-hover:text-accent" />
            </div>
          </div>

          {/* Emotions row */}
          {trade.emotions.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-1.5">
              {trade.emotions.map((emotion) => (
                <span
                  key={emotion}
                  className="rounded-full bg-secondary/60 px-2 py-0.5 text-[10px] font-medium text-muted-foreground"
                >
                  {emotion}
                </span>
              ))}
            </div>
          )}
        </button>
      ))}
    </div>
  )
}
