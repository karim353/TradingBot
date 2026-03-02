"use client"

import { useState, useCallback, useEffect, useMemo, useRef } from "react"
import { Navbar } from "@/components/navbar"
import { Footer } from "@/components/footer"
import { HeroSection } from "@/components/hero-section"
import { DashboardSection } from "@/components/dashboard-section"
import { JournalSection } from "@/components/journal-section"
import { FilterBar } from "@/components/filter-bar"
import { sampleTrades } from "@/lib/trade-store"
import {
  fetchTrades,
  createTrade,
  updateTrade,
  deleteTrade,
  apiTradeToTrade,
  tradeToCreatePayload,
  tradeToUpdatePayload,
  ApiError,
} from "@/lib/api"
import type { Trade } from "@/lib/types"
import { toast } from "sonner"

function defaultFromDate(): string {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().slice(0, 10)
}

function defaultToDate(): string {
  return new Date().toISOString().slice(0, 10)
}

export default function Page() {
  const [activeTab, setActiveTab] = useState("home")
  const [trades, setTrades] = useState<Trade[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fromDate, setFromDate] = useState(defaultFromDate)
  const [toDate, setToDate] = useState(defaultToDate)
  const [tickerFilter, setTickerFilter] = useState("all")

  const fromDateRef = useRef(fromDate)
  const toDateRef = useRef(toDate)
  fromDateRef.current = fromDate
  toDateRef.current = toDate

  const loadTrades = useCallback(async (overrideFrom?: string, overrideTo?: string) => {
    const from = overrideFrom ?? fromDateRef.current
    const to = overrideTo ?? toDateRef.current
    if (overrideFrom != null) setFromDate(overrideFrom)
    if (overrideTo != null) setToDate(overrideTo)
    setLoading(true)
    setError(null)
    const fromIso = from ? `${from}T00:00:00` : undefined
    const toIso = to ? `${to}T23:59:59` : undefined
    try {
      const data = await fetchTrades(fromIso, toIso)
      setTrades(data.map(apiTradeToTrade))
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : e instanceof Error ? e.message : "Не удалось загрузить данные"
      const isNetwork = e instanceof TypeError && (e.message === "Failed to fetch" || e.message.includes("network"))
      setError(isNetwork ? "Нет соединения с сервером" : msg)
      setTrades(sampleTrades)
      toast.error(isNetwork ? "Нет соединения. Показаны демо-данные." : `Используются демо-данные. ${msg}`)
    } finally {
      setLoading(false)
    }
  }, [])

  const [isOnline, setIsOnline] = useState(
    typeof navigator !== "undefined" ? navigator.onLine : true
  )
  useEffect(() => {
    const onOnline = () => setIsOnline(true)
    const onOffline = () => setIsOnline(false)
    window.addEventListener("online", onOnline)
    window.addEventListener("offline", onOffline)
    return () => {
      window.removeEventListener("online", onOnline)
      window.removeEventListener("offline", onOffline)
    }
  }, [])

  useEffect(() => {
    loadTrades()
  }, [loadTrades])

  const uniqueTickers = useMemo(
    () => Array.from(new Set(trades.map((t) => t.ticker.trim()).filter(Boolean))).sort(),
    [trades]
  )

  const filteredTrades = useMemo(() => {
    if (tickerFilter === "all") return trades
    return trades.filter((t) => t.ticker.toUpperCase() === tickerFilter.toUpperCase())
  }, [trades, tickerFilter])

  const handleAddTrade = useCallback(
    async (trade: Omit<Trade, "id">) => {
      try {
        const created = await createTrade(tradeToCreatePayload(trade))
        setTrades((prev) => [apiTradeToTrade(created), ...prev])
        toast.success("Сделка сохранена!")
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Не удалось сохранить сделку")
      }
    },
    []
  )

  const handleUpdateTrade = useCallback(async (trade: Trade) => {
    const numId = parseInt(trade.id, 10)
    if (isNaN(numId)) return
    try {
      const updated = await updateTrade(numId, tradeToUpdatePayload(trade))
      setTrades((prev) => prev.map((t) => (t.id === trade.id ? apiTradeToTrade(updated) : t)))
      toast.success("Сделка обновлена")
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Не удалось обновить сделку")
    }
  }, [])

  const handleDeleteTrade = useCallback(async (id: string) => {
    const numId = parseInt(id, 10)
    if (isNaN(numId)) return
    try {
      await deleteTrade(numId)
      setTrades((prev) => prev.filter((t) => t.id !== id))
      toast.success("Сделка удалена")
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Не удалось удалить")
    }
  }, [])

  const handleTabChange = useCallback((tab: string) => {
    setActiveTab(tab)
    window.scrollTo({ top: 0, behavior: "smooth" })
  }, [])

  return (
    <main className="relative min-h-screen">
      <Navbar activeTab={activeTab} onTabChange={handleTabChange} />

      {!isOnline && (
        <div className="sticky top-16 z-40 flex items-center justify-center gap-2 border-b border-amber-500/30 bg-amber-500/10 px-4 py-2.5 text-sm text-amber-200" role="status" aria-live="polite">
          <span>Нет соединения. Данные могут быть неактуальны.</span>
        </div>
      )}

      {error && isOnline ? (
        <div className="sticky top-16 z-40 flex items-center justify-between gap-4 border-b border-destructive/20 bg-destructive/10 px-4 py-2.5 text-sm text-destructive">
          <span>Ошибка загрузки: {error}</span>
          <button
            type="button"
            onClick={() => loadTrades()}
            className="rounded-lg bg-destructive/20 px-3 py-1.5 text-xs font-semibold text-destructive hover:bg-destructive/30 transition-colors"
            aria-label="Повторить загрузку"
          >
            Повторить
          </button>
        </div>
      ) : null}

      {activeTab === "journal" && (
        <FilterBar
          fromDate={fromDate}
          toDate={toDate}
          tickerFilter={tickerFilter}
          tickers={uniqueTickers}
          onFromChange={setFromDate}
          onToChange={setToDate}
          onTickerChange={setTickerFilter}
          onApply={() => loadTrades()}
          onPresetApply={(from, to) => loadTrades(from, to)}
          loading={loading}
        />
      )}

      {activeTab === "home" && (
        <HeroSection
          onNavigate={handleTabChange}
          tradeCount={!loading && !error ? trades.length : undefined}
          totalPnl={
            !loading && !error && trades.length > 0
              ? trades.reduce((s, t) => s + t.pnl, 0)
              : undefined
          }
        />
      )}
      {activeTab === "dashboard" && (
        <DashboardSection
          trades={filteredTrades}
          loading={loading}
          fromDate={fromDate}
          toDate={toDate}
          tickerFilter={tickerFilter}
          tickers={uniqueTickers}
          onFromChange={setFromDate}
          onToChange={setToDate}
          onTickerChange={setTickerFilter}
          onPresetApply={(from, to) => loadTrades(from, to)}
          onApply={loadTrades}
        />
      )}
      {activeTab === "journal" && (
        <JournalSection
          trades={filteredTrades}
          loading={loading}
          onAddTrade={handleAddTrade}
          onUpdateTrade={handleUpdateTrade}
          onDeleteTrade={handleDeleteTrade}
          onRefresh={loadTrades}
        />
      )}

      <Footer />
    </main>
  )
}
