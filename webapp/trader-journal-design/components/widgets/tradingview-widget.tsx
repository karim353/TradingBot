"use client"

import { useEffect, useRef, useState } from "react"
import { RefreshCw } from "lucide-react"

const SYMBOLS = [
  { label: "BTC/USD", value: "BINANCE:BTCUSDT" },
  { label: "ETH/USD", value: "BINANCE:ETHUSDT" },
  { label: "SOL/USD", value: "BINANCE:SOLUSDT" },
  { label: "XRP/USD", value: "BINANCE:XRPUSDT" },
  { label: "DOGE/USD", value: "BINANCE:DOGEUSDT" },
]

export function TradingViewWidget() {
  const containerRef = useRef<HTMLDivElement>(null)
  const [activeSymbol, setActiveSymbol] = useState(SYMBOLS[0])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!containerRef.current) return
    setLoading(true)

    // Clean up previous widget
    containerRef.current.innerHTML = ""

    const script = document.createElement("script")
    script.src = "https://s3.tradingview.com/external-embedding/embed-widget-advanced-chart.js"
    script.type = "text/javascript"
    script.async = true
    script.innerHTML = JSON.stringify({
      autosize: true,
      symbol: activeSymbol.value,
      interval: "60",
      timezone: "Etc/UTC",
      theme: "dark",
      style: "1",
      locale: "en",
      backgroundColor: "rgba(0, 0, 0, 0)",
      gridColor: "rgba(255, 255, 255, 0.04)",
      hide_top_toolbar: false,
      hide_legend: false,
      allow_symbol_change: true,
      save_image: false,
      calendar: false,
      hide_volume: false,
      support_host: "https://www.tradingview.com",
    })

    script.onload = () => setLoading(false)
    setTimeout(() => setLoading(false), 3000)

    containerRef.current.appendChild(script)
  }, [activeSymbol])

  return (
    <div className="flex h-full flex-col">
      {/* Symbol selector tabs */}
      <div className="flex items-center gap-1 border-b border-border/10 px-1 pb-3">
        {SYMBOLS.map((sym) => (
          <button
            key={sym.value}
            type="button"
            onClick={() => setActiveSymbol(sym)}
            className={`rounded-lg px-2.5 py-1.5 text-xs font-medium transition-all ${
              activeSymbol.value === sym.value
                ? "bg-accent/15 text-accent"
                : "text-muted-foreground hover:text-foreground"
            }`}
          >
            {sym.label}
          </button>
        ))}
      </div>

      {/* Chart container */}
      <div className="relative flex-1 min-h-[300px] mt-3 overflow-hidden rounded-xl">
        {loading && (
          <div className="absolute inset-0 z-10 flex items-center justify-center bg-card/50">
            <RefreshCw className="h-5 w-5 animate-spin text-accent" />
          </div>
        )}
        <div
          ref={containerRef}
          className="tradingview-widget-container h-full w-full"
        />
      </div>
    </div>
  )
}
