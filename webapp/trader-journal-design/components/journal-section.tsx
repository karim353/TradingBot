"use client"

import { useState } from "react"
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
  onDeleteTrade?: (id: string) => void | Promise<void>
  onRefresh?: () => void | Promise<void>
}

export function JournalSection({
  trades,
  loading = false,
  onAddTrade,
  onDeleteTrade,
  onRefresh,
}: JournalSectionProps) {
  const [showForm, setShowForm] = useState(false)
  const [selectedTrade, setSelectedTrade] = useState<Trade | null>(null)
  const [search, setSearch] = useState("")
  const [resultFilter, setResultFilter] = useState<string>("all")

  const filteredTrades = trades.filter((trade) => {
    const matchesSearch =
      search === "" ||
      trade.ticker.toLowerCase().includes(search.toLowerCase()) ||
      trade.account.toLowerCase().includes(search.toLowerCase()) ||
      trade.session.toLowerCase().includes(search.toLowerCase())
    const matchesResult =
      resultFilter === "all" || trade.result === resultFilter
    return matchesSearch && matchesResult
  })

  return (
    <section className="min-h-screen bg-background pt-24 pb-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between animate-slide-up">
          <div>
            <h2 className="text-3xl font-bold tracking-tight text-foreground">
              Журнал сделок
            </h2>
            <p className="mt-1 text-muted-foreground">
              {trades.length} {trades.length === 1 ? "сделка" : trades.length < 5 ? "сделки" : "сделок"} в журнале
            </p>
          </div>
          <Button
            onClick={() => setShowForm(true)}
            className="gap-2 rounded-xl bg-accent text-accent-foreground hover:bg-accent/90 shadow-lg shadow-accent/20"
          >
            <Plus className="h-4 w-4" />
            Новая сделка
          </Button>
        </div>

        {/* Filters */}
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center animate-slide-up" style={{ animationDelay: "0.1s" }}>
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Поиск по тикеру, бирже, сессии..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="glass border-border/30 bg-transparent pl-10 text-foreground placeholder:text-muted-foreground focus:border-accent"
            />
          </div>
          <div className="flex items-center gap-2">
            <Filter className="h-4 w-4 text-muted-foreground" />
            <Select value={resultFilter} onValueChange={setResultFilter}>
              <SelectTrigger className="glass w-[140px] border-border/30 bg-transparent text-foreground">
                <SelectValue placeholder="Result" />
              </SelectTrigger>
              <SelectContent className="glass-strong border-border/30 bg-card text-foreground">
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
          <div className="glass rounded-2xl p-12 text-center animate-pulse">
            <p className="text-muted-foreground">Загрузка...</p>
          </div>
        ) : filteredTrades.length > 0 ? (
          <TradeList trades={filteredTrades} onSelectTrade={setSelectedTrade} />
        ) : (
          <div className="glass rounded-2xl p-12 text-center animate-slide-up">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-secondary/50">
              <Plus className="h-8 w-8 text-muted-foreground/50" />
            </div>
            <p className="text-lg font-medium text-foreground">Сделок не найдено</p>
            <p className="mt-1 text-sm text-muted-foreground">
              {trades.length === 0
                ? "Добавьте первую сделку"
                : "Попробуйте изменить поиск или фильтры"}
            </p>
            {trades.length === 0 && (
              <Button
                onClick={() => setShowForm(true)}
                className="mt-4 gap-2 rounded-xl bg-accent text-accent-foreground hover:bg-accent/90"
              >
                <Plus className="h-4 w-4" />
                Добавить сделку
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Modals */}
      {showForm && (
        <TradeForm
          onSubmit={async (trade) => {
            await onAddTrade(trade)
            setShowForm(false)
          }}
          onClose={() => setShowForm(false)}
        />
      )}
      {selectedTrade && (
        <TradeDetail
          trade={selectedTrade}
          onClose={() => setSelectedTrade(null)}
          onDelete={
            onDeleteTrade
              ? async () => {
                  await onDeleteTrade(selectedTrade.id)
                  setSelectedTrade(null)
                }
              : undefined
          }
        />
      )}
    </section>
  )
}
