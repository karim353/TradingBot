"use client"

import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
} from "recharts"
import type { Trade } from "@/lib/types"

interface ResultDistributionWidgetProps {
  trades: Trade[]
}

export function ResultDistributionWidget({ trades }: ResultDistributionWidgetProps) {
  const tp = trades.filter((t) => t.result === "TP").length
  const sl = trades.filter((t) => t.result === "SL").length
  const be = trades.filter((t) => t.result === "BE").length
  const data = [
    { name: "TP", value: tp, color: "hsl(142, 76%, 46%)" },
    { name: "SL", value: sl, color: "hsl(0, 72%, 51%)" },
    { name: "BE", value: be, color: "hsl(45, 93%, 58%)" },
  ]

  return (
    <div className="flex items-center gap-6">
      <div className="h-40 w-40 flex-shrink-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={40}
              outerRadius={65}
              paddingAngle={4}
              dataKey="value"
              strokeWidth={0}
            >
              {data.map((entry, i) => (
                <Cell key={i} fill={entry.color} />
              ))}
            </Pie>
            <Tooltip
              contentStyle={{
                backgroundColor: "rgba(0,0,0,0.9)",
                border: "1px solid rgba(255,255,255,0.1)",
                borderRadius: "12px",
                color: "#fff",
                fontSize: "12px",
              }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
      <div className="flex flex-col gap-3">
        {data.map((item) => (
          <div key={item.name} className="flex items-center gap-3">
            <div className="h-3 w-3 rounded-full" style={{ backgroundColor: item.color }} />
            <div>
              <p className="text-sm font-medium text-foreground">{item.name}</p>
              <p className="text-xs text-muted-foreground">{item.value} сделок</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
