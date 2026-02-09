"use client"

import { X, ArrowUpRight, ArrowDownRight, Minus, Trash2 } from "lucide-react"
import Image from "next/image"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type { Trade } from "@/lib/types"

interface TradeDetailProps {
  trade: Trade
  onClose: () => void
  onDelete?: () => void | Promise<void>
}

export function TradeDetail({ trade, onClose, onDelete }: TradeDetailProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm animate-fade-in">
      <div className="glass-strong mx-4 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl p-6 sm:p-8 animate-slide-up">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div
              className={cn(
                "flex h-12 w-12 items-center justify-center rounded-2xl",
                trade.result === "TP"
                  ? "bg-success/15"
                  : trade.result === "SL"
                    ? "bg-destructive/15"
                    : "bg-muted"
              )}
            >
              {trade.result === "TP" ? (
                <ArrowUpRight className="h-6 w-6 text-success" />
              ) : trade.result === "SL" ? (
                <ArrowDownRight className="h-6 w-6 text-destructive" />
              ) : (
                <Minus className="h-6 w-6 text-muted-foreground" />
              )}
            </div>
            <div>
              <h2 className="text-xl font-bold text-foreground">{trade.ticker}</h2>
              <p className="text-sm text-muted-foreground">{trade.date}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {onDelete && (
              <Button
                variant="outline"
                size="sm"
                className="rounded-lg border-destructive/50 text-destructive hover:bg-destructive/10"
                onClick={onDelete}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
            <button
              type="button"
              onClick={onClose}
              className="flex h-8 w-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
              aria-label="Close details"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Key metrics */}
        <div className="mb-6 grid grid-cols-3 gap-3">
          <div className="rounded-xl bg-secondary/50 p-3 text-center">
            <p className="text-xs text-muted-foreground">PnL</p>
            <p
              className={cn(
                "text-lg font-bold font-mono",
                trade.pnl > 0 ? "text-success" : trade.pnl < 0 ? "text-destructive" : "text-foreground"
              )}
            >
              {trade.pnl > 0 ? "+" : ""}
              {trade.pnl.toFixed(2)}%
            </p>
          </div>
          <div className="rounded-xl bg-secondary/50 p-3 text-center">
            <p className="text-xs text-muted-foreground">RR</p>
            <p className="text-lg font-bold font-mono text-foreground">1:{trade.rr.toFixed(1)}</p>
          </div>
          <div className="rounded-xl bg-secondary/50 p-3 text-center">
            <p className="text-xs text-muted-foreground">Risk</p>
            <p className="text-lg font-bold font-mono text-foreground">{trade.risk}%</p>
          </div>
        </div>

        {/* Details */}
        <div className="flex flex-col gap-4">
          <DetailRow label="Position" value={trade.position} highlight={trade.position === "LONG" ? "success" : "destructive"} />
          <DetailRow label="Direction" value={trade.direction} />
          <DetailRow label="Result" value={trade.result} highlight={trade.result === "TP" ? "success" : trade.result === "SL" ? "destructive" : "accent"} />
          <DetailRow label="Account" value={trade.account} />
          <DetailRow label="Session" value={trade.session} />

          {trade.context.length > 0 && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Context</span>
              <div className="flex flex-wrap gap-1.5">
                {trade.context.map((c) => (
                  <span key={c} className="rounded-full bg-accent/10 px-2.5 py-1 text-xs text-accent">{c}</span>
                ))}
              </div>
            </div>
          )}

          {trade.setup.length > 0 && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Setup</span>
              <div className="flex flex-wrap gap-1.5">
                {trade.setup.map((s) => (
                  <span key={s} className="rounded-full bg-accent/10 px-2.5 py-1 text-xs text-accent">{s}</span>
                ))}
              </div>
            </div>
          )}

          {trade.emotions.length > 0 && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Emotions</span>
              <div className="flex flex-wrap gap-1.5">
                {trade.emotions.map((e) => (
                  <span key={e} className="rounded-full bg-secondary px-2.5 py-1 text-xs text-muted-foreground">{e}</span>
                ))}
              </div>
            </div>
          )}

          {trade.entryDetails && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Entry Details</span>
              <p className="text-sm text-foreground leading-relaxed">{trade.entryDetails}</p>
            </div>
          )}

          {trade.comment && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Comment</span>
              <p className="text-sm text-foreground leading-relaxed">{trade.comment}</p>
            </div>
          )}

          {trade.note && (
            <div className="flex flex-col gap-1">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Note</span>
              <p className="text-sm text-foreground leading-relaxed">{trade.note}</p>
            </div>
          )}

          {trade.screenshots && trade.screenshots.length > 0 && (
            <div className="flex flex-col gap-2">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Фото</span>
              <div className="flex flex-wrap gap-3">
                {trade.screenshots.map((src, i) => (
                  <div key={i} className="relative h-36 w-48 overflow-hidden rounded-xl border border-border/30">
                    <Image src={src} alt={`Фото ${i + 1}`} fill className="object-cover" unoptimized />
                  </div>
                ))}
              </div>
            </div>
          )}

          {trade.notionPageId && (
            <a
              href={`https://notion.so/${trade.notionPageId.replace(/-/g, "")}`}
              target="_blank"
              rel="noreferrer"
              className="mt-4 inline-flex items-center gap-2 rounded-xl border border-border/30 px-4 py-2 text-sm text-accent hover:bg-accent/10"
            >
              Open in Notion →
            </a>
          )}
        </div>
      </div>
    </div>
  )
}

function DetailRow({
  label,
  value,
  highlight,
}: {
  label: string
  value: string
  highlight?: "success" | "destructive" | "accent"
}) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
        {label}
      </span>
      <span
        className={cn(
          "text-sm font-medium",
          highlight === "success"
            ? "text-success"
            : highlight === "destructive"
              ? "text-destructive"
              : highlight === "accent"
                ? "text-accent"
                : "text-foreground"
        )}
      >
        {value}
      </span>
    </div>
  )
}
