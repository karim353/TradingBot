"use client"

import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Calendar, RefreshCw } from "lucide-react"

interface FilterBarProps {
  fromDate: string
  toDate: string
  tickerFilter: string
  tickers: string[]
  onFromChange: (v: string) => void
  onToChange: (v: string) => void
  onTickerChange: (v: string) => void
  onApply: () => void
  onPresetApply?: (from: string, to: string) => void
  loading?: boolean
}

function presetRange(days: number): { from: string; to: string } {
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - days)
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) }
}

export function FilterBar({
  fromDate,
  toDate,
  tickerFilter,
  tickers,
  onFromChange,
  onToChange,
  onTickerChange,
  onApply,
  onPresetApply,
  loading = false,
}: FilterBarProps) {
  const applyPreset = (days: number) => {
    const { from, to } = presetRange(days)
    if (onPresetApply) {
      onPresetApply(from, to)
    } else {
      onFromChange(from)
      onToChange(to)
      onApply()
    }
  }

  return (
    <div className="sticky top-16 z-40 border-b border-white/[0.04] bg-[hsl(240,10%,4%)]/90 backdrop-blur-2xl">
      <div className="mx-auto max-w-7xl px-4 py-3.5 sm:px-6 lg:px-8">
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2 text-muted-foreground/70">
            <Calendar className="h-4 w-4" />
            <span className="text-xs font-semibold uppercase tracking-wider">Период</span>
          </div>
          <div className="flex items-center gap-1.5">
            {[7, 30, 90].map((d) => (
              <button
                key={d}
                type="button"
                onClick={() => applyPreset(d)}
                className="rounded-lg border border-white/[0.06] bg-white/[0.02] px-2.5 py-1.5 text-xs font-medium text-muted-foreground transition-all hover:bg-accent/10 hover:text-foreground hover:border-accent/20"
              >
                {d} дн.
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2">
            <label htmlFor="filter-from" className="sr-only">Дата с</label>
            <input
              id="filter-from"
              type="date"
              value={fromDate}
              onChange={(e) => onFromChange(e.target.value)}
              className="h-8 rounded-lg border border-white/[0.06] bg-white/[0.02] px-2.5 text-xs text-foreground focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
            />
            <span className="text-muted-foreground/40">—</span>
            <label htmlFor="filter-to" className="sr-only">Дата по</label>
            <input
              id="filter-to"
              type="date"
              value={toDate}
              onChange={(e) => onToChange(e.target.value)}
              className="h-8 rounded-lg border border-white/[0.06] bg-white/[0.02] px-2.5 text-xs text-foreground focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          <div className="flex items-center gap-2">
            <label htmlFor="filter-ticker" className="text-xs text-muted-foreground/70 whitespace-nowrap">Тикер</label>
            <Select value={tickerFilter} onValueChange={onTickerChange}>
              <SelectTrigger id="filter-ticker" className="w-[140px] h-8 rounded-lg border-white/[0.06] bg-white/[0.02] text-xs text-foreground">
                <SelectValue placeholder="Все тикеры" />
              </SelectTrigger>
              <SelectContent className="border-white/[0.06] bg-card text-foreground">
                <SelectItem value="all">Все тикеры</SelectItem>
                {tickers.map((t) => (
                  <SelectItem key={t} value={t}>{t}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button
            onClick={onApply}
            disabled={loading}
            size="sm"
            className="h-8 gap-1.5 rounded-lg bg-accent/15 text-accent text-xs font-semibold hover:bg-accent/25 border border-accent/20"
            aria-label={loading ? "Загрузка" : "Применить фильтр"}
          >
            <RefreshCw className={loading ? "h-3.5 w-3.5 animate-spin" : "h-3.5 w-3.5"} />
            {loading ? "Загрузка…" : "Применить"}
          </Button>
        </div>
      </div>
    </div>
  )
}
