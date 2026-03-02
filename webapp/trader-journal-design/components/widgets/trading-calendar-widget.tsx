"use client"

import { useMemo } from "react"
import { cn } from "@/lib/utils"
import type { Trade } from "@/lib/types"

interface TradingCalendarWidgetProps {
  trades: Trade[]
}

const WEEKDAYS = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"]
const MONTHS_RU = ["Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"]

function getMonthDays(year: number, month: number) {
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const startPad = (firstDay.getDay() + 6) % 7
  const totalDays = lastDay.getDate()
  return { startPad, totalDays }
}

export function TradingCalendarWidget({ trades }: TradingCalendarWidgetProps) {
  const dailyMap = useMemo(() => {
    const map = new Map<string, { pnl: number; trades: number; wins: number }>()
    for (const t of trades) {
      const existing = map.get(t.date) ?? { pnl: 0, trades: 0, wins: 0 }
      map.set(t.date, {
        pnl: existing.pnl + t.pnl,
        trades: existing.trades + 1,
        wins: existing.wins + (t.result === "TP" ? 1 : 0),
      })
    }
    return map
  }, [trades])

  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth()
  const { startPad, totalDays } = getMonthDays(year, month)

  const maxAbsPnl = useMemo(() => {
    let max = 0
    dailyMap.forEach((v) => { if (Math.abs(v.pnl) > max) max = Math.abs(v.pnl) })
    return max || 1
  }, [dailyMap])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h4 className="text-sm font-semibold text-foreground">
          {MONTHS_RU[month]} {year}
        </h4>
        <div className="flex items-center gap-3 text-[10px] text-muted-foreground/60">
          <span className="flex items-center gap-1">
            <span className="h-2.5 w-2.5 rounded-sm bg-success/40" /> Прибыль
          </span>
          <span className="flex items-center gap-1">
            <span className="h-2.5 w-2.5 rounded-sm bg-destructive/40" /> Убыток
          </span>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-1">
        {WEEKDAYS.map((d) => (
          <div key={d} className="pb-1 text-center text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/40">
            {d}
          </div>
        ))}

        {Array.from({ length: startPad }).map((_, i) => (
          <div key={`pad-${i}`} />
        ))}

        {Array.from({ length: totalDays }).map((_, i) => {
          const day = i + 1
          const dateStr = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`
          const data = dailyMap.get(dateStr)
          const isToday = day === now.getDate()

          const intensity = data ? Math.min(Math.abs(data.pnl) / maxAbsPnl, 1) : 0
          const bgClass = data
            ? data.pnl > 0
              ? `bg-success/${Math.round(10 + intensity * 40)}`
              : data.pnl < 0
                ? `bg-destructive/${Math.round(10 + intensity * 40)}`
                : "bg-white/[0.04]"
            : ""

          return (
            <div
              key={day}
              className={cn(
                "group relative flex flex-col items-center justify-center rounded-lg py-2 transition-all",
                isToday && "ring-1 ring-accent/40",
                data ? "hover:ring-1 hover:ring-white/20 cursor-default" : "",
                bgClass || "bg-transparent"
              )}
              style={
                data
                  ? {
                      backgroundColor: data.pnl > 0
                        ? `rgba(34, 197, 94, ${0.08 + intensity * 0.3})`
                        : data.pnl < 0
                          ? `rgba(239, 68, 68, ${0.08 + intensity * 0.3})`
                          : "rgba(255,255,255,0.03)"
                    }
                  : undefined
              }
            >
              <span className={cn(
                "text-xs tabular-nums",
                data ? "font-semibold text-foreground" : "text-muted-foreground/40"
              )}>
                {day}
              </span>
              {data ? (
                <span className={cn(
                  "mt-0.5 text-[9px] font-bold tabular-nums font-mono",
                  data.pnl > 0 ? "text-success" : data.pnl < 0 ? "text-destructive" : "text-muted-foreground/50"
                )}>
                  {data.pnl > 0 ? "+" : ""}{data.pnl.toFixed(1)}
                </span>
              ) : null}

              {/* Tooltip */}
              {data ? (
                <div className="absolute -top-12 left-1/2 -translate-x-1/2 rounded-lg border border-white/[0.08] bg-[hsl(240,8%,8%)] px-3 py-1.5 opacity-0 group-hover:opacity-100 transition-opacity z-10 whitespace-nowrap shadow-xl pointer-events-none">
                  <p className="text-[10px] font-semibold text-foreground">{dateStr}</p>
                  <p className="text-[10px] text-muted-foreground/70">{data.trades} сд. | {data.wins} win</p>
                </div>
              ) : null}
            </div>
          )
        })}
      </div>
    </div>
  )
}
