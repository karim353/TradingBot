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
      className="glass glass-hover rounded-2xl p-5 animate-slide-up"
      style={{ animationDelay: `${delay}s` }}
    >
      <div className="flex items-start justify-between">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            {label}
          </span>
          <span className="text-2xl font-bold text-foreground animate-count-up">
            {value}
          </span>
        </div>
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-accent/10">
          <Icon className="h-5 w-5 text-accent" />
        </div>
      </div>
      {change && (
        <div className="mt-3 flex items-center gap-1">
          <span
            className={cn(
              "text-xs font-semibold",
              positive ? "text-success" : "text-destructive"
            )}
          >
            {change}
          </span>
          <span className="text-xs text-muted-foreground">vs last week</span>
        </div>
      )}
    </div>
  )
}
