"use client"

import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts"

interface PnlChartWidgetProps {
  data: Array<{ displayDate: string; cumulative: number; pnl: number }>
  totalPnl: number
}

export function PnlChartWidget({ data, totalPnl }: PnlChartWidgetProps) {
  return (
    <div>
      <div className="mb-4 flex items-end gap-3">
        <span className="text-3xl font-bold font-mono text-foreground">
          {totalPnl >= 0 ? "+" : ""}{totalPnl.toFixed(2)}%
        </span>
        <span className={`text-sm font-medium ${totalPnl >= 0 ? "text-success" : "text-destructive"}`}>
          cumulative
        </span>
      </div>
      <div className="h-48">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data}>
            <defs>
              <linearGradient id="pnlGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="hsl(210, 100%, 55%)" stopOpacity={0.25} />
                <stop offset="100%" stopColor="hsl(210, 100%, 55%)" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" />
            <XAxis
              dataKey="displayDate"
              stroke="rgba(255,255,255,0.2)"
              fontSize={10}
              tickLine={false}
              axisLine={false}
            />
            <YAxis
              stroke="rgba(255,255,255,0.2)"
              fontSize={10}
              tickLine={false}
              axisLine={false}
              tickFormatter={(v) => `${v}%`}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "rgba(10,10,10,0.95)",
                border: "1px solid rgba(255,255,255,0.08)",
                borderRadius: "12px",
                color: "#f2f2f2",
                fontSize: "11px",
                padding: "8px 12px",
              }}
              formatter={(value: number) => [`${value.toFixed(2)}%`, "PnL"]}
            />
            <Area
              type="monotone"
              dataKey="cumulative"
              stroke="hsl(210, 100%, 55%)"
              strokeWidth={2}
              fill="url(#pnlGrad)"
              dot={false}
              activeDot={{ r: 4, fill: "hsl(210, 100%, 55%)" }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}
