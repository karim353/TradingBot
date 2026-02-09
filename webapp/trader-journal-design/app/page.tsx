"use client"

import { useState, useCallback, useEffect } from "react"
import { Navbar } from "@/components/navbar"
import { Footer } from "@/components/footer"
import { HeroSection } from "@/components/hero-section"
import { DashboardSection } from "@/components/dashboard-section"
import { JournalSection } from "@/components/journal-section"
import { sampleTrades } from "@/lib/trade-store"
import {
  fetchTrades,
  createTrade,
  deleteTrade,
  apiTradeToTrade,
  tradeToCreatePayload,
} from "@/lib/api"
import type { Trade } from "@/lib/types"
import { toast } from "sonner"

export default function Page() {
  const [activeTab, setActiveTab] = useState("home")
  const [trades, setTrades] = useState<Trade[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadTrades = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchTrades()
      setTrades(data.map(apiTradeToTrade))
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load")
      setTrades(sampleTrades)
      toast.error("Using demo data. API unavailable.")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadTrades()
  }, [loadTrades])

  const handleAddTrade = useCallback(
    async (trade: Omit<Trade, "id">) => {
      try {
        const created = await createTrade(tradeToCreatePayload(trade))
        setTrades((prev) => [apiTradeToTrade(created), ...prev])
        toast.success("Trade saved!")
      } catch (e) {
        toast.error(e instanceof Error ? e.message : "Failed to save trade")
      }
    },
    []
  )

  const handleDeleteTrade = useCallback(async (id: string) => {
    const numId = parseInt(id, 10)
    if (isNaN(numId)) return
    try {
      await deleteTrade(numId)
      setTrades((prev) => prev.filter((t) => t.id !== id))
      toast.success("Trade deleted")
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Failed to delete")
    }
  }, [])

  const handleTabChange = useCallback((tab: string) => {
    setActiveTab(tab)
    window.scrollTo({ top: 0, behavior: "smooth" })
  }, [])

  return (
    <main className="relative min-h-screen">
      <Navbar activeTab={activeTab} onTabChange={handleTabChange} />

      {activeTab === "home" && <HeroSection onNavigate={handleTabChange} />}
      {activeTab === "dashboard" && (
        <DashboardSection trades={trades} loading={loading} />
      )}
      {activeTab === "journal" && (
        <JournalSection
          trades={trades}
          loading={loading}
          onAddTrade={handleAddTrade}
          onDeleteTrade={handleDeleteTrade}
          onRefresh={loadTrades}
        />
      )}

      <Footer />
    </main>
  )
}
