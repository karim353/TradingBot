"use client"

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts"
import type { Trade } from "@/lib/types"
import { getDailyPnL } from "@/lib/trade-store"

interface DailyPnlWidgetProps {
  trades: Trade[]
}

export function DailyPnlWidget({ trades }: DailyPnlWidgetProps) {
  const dailyPnl = getDailyPnL(trades)
  const cumulativePnl = dailyPnl.map((d) => ({
    ...d,
    displayDate: d.date.slice(5),
  }))

  return (
    <div>
      <p className="mb-4 text-2xl font-bold text-foreground">{dailyPnl.length} торговых дней</p>
      <div className="h-52">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={cumulativePnl}>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
            <XAxis
              dataKey="displayDate"
              stroke="rgba(255,255,255,0.3)"
              fontSize={10}
              tickLine={false}
              axisLine={false}
            />
            <YAxis
              stroke="rgba(255,255,255,0.3)"
              fontSize={10}
              tickLine={false}
              axisLine={false}
              tickFormatter={(v) => `${v}%`}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "rgba(0,0,0,0.9)",
                border: "1px solid rgba(255,255,255,0.1)",
                borderRadius: "12px",
                color: "#fff",
                fontSize: "12px",
              }}
              formatter={(value: number) => [`${value.toFixed(2)}%`, "PnL"]}
            />
            <Bar dataKey="pnl" radius={[6, 6, 0, 0]}>
              {cumulativePnl.map((entry, i) => (
                <Cell
                  key={i}
                  fill={entry.pnl >= 0 ? "hsl(142, 76%, 46%)" : "hsl(0, 72%, 51%)"}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
