"use client"

import React from "react"

import { GripVertical, X, Maximize2, Minimize2 } from "lucide-react"
import { cn } from "@/lib/utils"

interface WidgetWrapperProps {
  id: string
  title: string
  children: React.ReactNode
  onRemove?: (id: string) => void
  isExpanded?: boolean
  onToggleExpand?: (id: string) => void
  className?: string
  noPadding?: boolean
  isDragging?: boolean
  dragHandleProps?: Record<string, unknown>
}

export function WidgetWrapper({
  id,
  title,
  children,
  onRemove,
  isExpanded,
  onToggleExpand,
  className,
  noPadding,
  isDragging,
  dragHandleProps,
}: WidgetWrapperProps) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-white/[0.06] bg-white/[0.02] transition-all duration-200",
        isDragging && "ring-2 ring-accent/30 scale-[1.02] z-50",
        className
      )}
    >
      <div className="flex items-center justify-between border-b border-white/[0.04] px-5 py-3">
        <div className="flex items-center gap-2">
          <div
            {...dragHandleProps}
            className="cursor-grab text-muted-foreground/30 hover:text-muted-foreground active:cursor-grabbing"
          >
            <GripVertical className="h-4 w-4" />
          </div>
          <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        </div>
        <div className="flex items-center gap-1">
          {onToggleExpand ? (
            <button
              type="button"
              onClick={() => onToggleExpand(id)}
              className="flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground/40 transition-colors hover:bg-white/[0.04] hover:text-foreground"
              aria-label={isExpanded ? "Minimize widget" : "Expand widget"}
            >
              {isExpanded ? <Minimize2 className="h-3 w-3" /> : <Maximize2 className="h-3 w-3" />}
            </button>
          ) : null}
          {onRemove ? (
            <button
              type="button"
              onClick={() => onRemove(id)}
              className="flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground/40 transition-colors hover:bg-destructive/20 hover:text-destructive"
              aria-label="Remove widget"
            >
              <X className="h-3 w-3" />
            </button>
          ) : null}
        </div>
      </div>

      <div className={cn(noPadding ? "p-0" : "p-5")}>
        {children}
      </div>
    </div>
  )
}
