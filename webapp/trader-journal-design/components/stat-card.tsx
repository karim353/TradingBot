"use client"

import type { LucideIcon } from "lucide-react"
import { cn } from "@/lib/utils"

interface StatCardProps {
  label: string
  value: string
  change?: string
  positive?: boolean
  icon: LucideIcon
  delay?: number
}

export function StatCard({ label, value, change, positive, icon: Icon, delay = 0 }: StatCardProps) {
  return (
    <div
      className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-5 transition-all hover:border-accent/20 hover:bg-accent/[0.03] animate-slide-up"
      style={{ animationDelay: `${delay}s` }}
    >
      <div className="flex items-start justify-between">
        <div className="flex flex-col gap-1">
          <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground/70">
            {label}
          </span>
          <span className="text-2xl font-bold text-foreground tabular-nums">
            {value}
          </span>
        </div>
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-accent/10">
          <Icon className="h-4 w-4 text-accent" />
        </div>
      </div>
      {change ? (
        <div className="mt-3 flex items-center gap-1">
          <span className={cn("text-xs font-semibold", positive ? "text-success" : "text-destructive")}>
            {change}
          </span>
          <span className="text-xs text-muted-foreground/60">vs last week</span>
        </div>
      ) : null}
    </div>
  )
}
