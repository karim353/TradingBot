"use client"

import { useEffect, useRef, useState } from "react"
import { RefreshCw } from "lucide-react"

export function TradingViewTickerWidget() {
  const containerRef = useRef<HTMLDivElement>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!containerRef.current) return
    setLoading(true)
    containerRef.current.innerHTML = ""

    const script = document.createElement("script")
    script.src = "https://s3.tradingview.com/external-embedding/embed-widget-ticker-tape.js"
    script.type = "text/javascript"
    script.async = true
    script.innerHTML = JSON.stringify({
      symbols: [
        { description: "Bitcoin", proName: "BINANCE:BTCUSDT" },
        { description: "Ethereum", proName: "BINANCE:ETHUSDT" },
        { description: "Solana", proName: "BINANCE:SOLUSDT" },
        { description: "XRP", proName: "BINANCE:XRPUSDT" },
        { description: "Dogecoin", proName: "BINANCE:DOGEUSDT" },
        { description: "BNB", proName: "BINANCE:BNBUSDT" },
        { description: "Avalanche", proName: "BINANCE:AVAXUSDT" },
      ],
      showSymbolLogo: true,
      isTransparent: true,
      displayMode: "compact",
      colorTheme: "dark",
      locale: "en",
    })

    script.onload = () => setLoading(false)
    setTimeout(() => setLoading(false), 2000)
    containerRef.current.appendChild(script)
  }, [])

  return (
    <div className="relative w-full overflow-hidden rounded-xl">
      {loading && (
        <div className="absolute inset-0 z-10 flex items-center justify-center bg-card/80 rounded-xl">
          <RefreshCw className="h-5 w-5 animate-spin text-accent" />
        </div>
      )}
      <div
        ref={containerRef}
        className="tradingview-widget-container"
        style={{ height: "46px" }}
      />
    </div>
  )
}
