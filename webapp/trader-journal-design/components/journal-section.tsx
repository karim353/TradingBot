"use client"

import { useState, useMemo } from "react"
import { Plus, Search, Filter } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TradeList } from "@/components/trade-list"
import { TradeForm } from "@/components/trade-form"
import { TradeDetail } from "@/components/trade-detail"
import type { Trade } from "@/lib/types"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface JournalSectionProps {
  trades: Trade[]
  loading?: boolean
  onAddTrade: (trade: Omit<Trade, "id">) => void | Promise<void>
  onUpdateTrade?: (trade: Trade) => void | Promise<void>
  onDeleteTrade?: (id: string) => void | Promise<void>
  onRefresh?: () => void | Promise<void>
}

export function JournalSection({
  trades,
  loading = false,
  onAddTrade,
  onUpdateTrade,
  onDeleteTrade,
  onRefresh,
}: JournalSectionProps) {
  const [showForm, setShowForm] = useState(false)
  const [editingTrade, setEditingTrade] = useState<Trade | null>(null)
  const [selectedTrade, setSelectedTrade] = useState<Trade | null>(null)
  const [search, setSearch] = useState("")
  const [resultFilter, setResultFilter] = useState<string>("all")

  const filteredTrades = useMemo(() => {
    const lowerSearch = search.toLowerCase()
    return trades.filter((trade) => {
      const matchesSearch =
        search === "" ||
        trade.ticker.toLowerCase().includes(lowerSearch) ||
        trade.account.toLowerCase().includes(lowerSearch) ||
        trade.session.toLowerCase().includes(lowerSearch)
      const matchesResult = resultFilter === "all" || trade.result === resultFilter
      return matchesSearch && matchesResult
    })
  }, [trades, search, resultFilter])

  return (
    <section className="min-h-screen pt-24 pb-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between animate-slide-up">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              Журнал сделок
            </h2>
            <p className="mt-1 text-sm text-muted-foreground/70">
              {trades.length} {trades.length === 1 ? "сделка" : trades.length < 5 ? "сделки" : "сделок"} в журнале
            </p>
          </div>
          <Button
            onClick={() => setShowForm(true)}
            className="gap-2 rounded-lg bg-accent text-white hover:bg-accent/90 shadow-lg shadow-accent/25 text-sm font-semibold"
            aria-label="Добавить новую сделку"
          >
            <Plus className="h-4 w-4" />
            Новая сделка
          </Button>
        </div>

        {/* Filters */}
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center animate-slide-up" style={{ animationDelay: "0.1s" }}>
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground/50" />
            <Input
              placeholder="Поиск по тикеру, бирже, сессии..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="rounded-lg border-white/[0.06] bg-white/[0.02] pl-10 text-foreground placeholder:text-muted-foreground/50 focus:border-accent focus:ring-1 focus:ring-accent"
            />
          </div>
          <div className="flex items-center gap-2">
            <Filter className="h-4 w-4 text-muted-foreground/50" />
            <Select value={resultFilter} onValueChange={setResultFilter}>
              <SelectTrigger className="w-[150px] rounded-lg border-white/[0.06] bg-white/[0.02] text-foreground text-sm">
                <SelectValue placeholder="Результат" />
              </SelectTrigger>
              <SelectContent className="border-white/[0.06] bg-card text-foreground">
                <SelectItem value="all">Все результаты</SelectItem>
                <SelectItem value="TP">Take Profit</SelectItem>
                <SelectItem value="SL">Stop Loss</SelectItem>
                <SelectItem value="BE">Break Even</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Trade List */}
        {loading ? (
          <div className="rounded-2xl border border-white/[0.06] bg-white/[0.02] p-16 text-center">
            <div className="mx-auto mb-4 h-8 w-8 animate-spin rounded-full border-2 border-accent/30 border-t-accent" />
            <p className="text-muted-foreground">Загрузка...</p>
          </div>
        ) : filteredTrades.length > 0 ? (
          <TradeList trades={filteredTrades} onSelectTrade={setSelectedTrade} />
        ) : (
          <div className="rounded-2xl border border-white/[0.06] bg-white/[0.02] p-16 text-center animate-slide-up">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-xl bg-accent/10">
              <Plus className="h-7 w-7 text-accent/50" />
            </div>
            <p className="text-lg font-semibold text-foreground">Сделок не найдено</p>
            <p className="mt-1 text-sm text-muted-foreground/70">
              {trades.length === 0 ? "Добавьте первую сделку" : "Попробуйте изменить поиск или фильтры"}
            </p>
            {trades.length === 0 ? (
              <Button
                onClick={() => setShowForm(true)}
                className="mt-4 gap-2 rounded-lg bg-accent text-white hover:bg-accent/90"
              >
                <Plus className="h-4 w-4" />
                Добавить сделку
              </Button>
            ) : null}
          </div>
        )}
      </div>

      {/* Modals */}
      {showForm && !editingTrade ? (
        <TradeForm
          onSubmit={async (trade) => {
            await onAddTrade(trade as Omit<Trade, "id">)
            setShowForm(false)
          }}
          onClose={() => setShowForm(false)}
        />
      ) : null}
      {editingTrade ? (
        <TradeForm
          initialTrade={editingTrade}
          onSubmit={async (trade) => {
            if ("id" in trade && onUpdateTrade) {
              await onUpdateTrade(trade)
              setEditingTrade(null)
            }
          }}
          onClose={() => setEditingTrade(null)}
        />
      ) : null}
      {selectedTrade ? (
        <TradeDetail
          trade={selectedTrade}
          onClose={() => setSelectedTrade(null)}
          onEdit={onUpdateTrade ? (t) => setEditingTrade(t) : undefined}
          onDelete={
            onDeleteTrade
              ? async () => {
                  await onDeleteTrade(selectedTrade.id)
                  setSelectedTrade(null)
                }
              : undefined
          }
        />
      ) : null}
    </section>
  )
}
