"use client"

import React from "react"

import { useState, useRef, useEffect } from "react"
import { X, Plus, ImagePlus, Trash2 } from "lucide-react"
import Image from "next/image"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { cn } from "@/lib/utils"
import type { Trade, Account, Session, Position, Direction, TradeResult, Emotion } from "@/lib/types"

interface TradeFormProps {
  initialTrade?: Trade | null
  onSubmit: (trade: Omit<Trade, "id"> | Trade) => void | Promise<void>
  onClose: () => void
}

const ACCOUNTS: Account[] = ["BingX", "Bybit", "Binance", "OKX"]
const SESSIONS: Session[] = ["ASIA", "LONDON", "NEW YORK", "FRANKFURT"]
const POSITIONS: Position[] = ["LONG", "SHORT"]
const DIRECTIONS: Direction[] = ["Reversal", "Long", "Short", "Continuation"]
const RESULTS: TradeResult[] = ["TP", "SL", "BE"]
const EMOTIONS: Emotion[] = ["Nervous", "Confident", "Anxious", "Calm", "Excited", "Frustrated", "Focused", "Uncertain"]
const SETUPS = ["SweepBosRTO", "Range Provocation", "BR=>IL=>OL", "FVG Entry", "Liquidity Grab", "Order Block"]
const CONTEXTS = ["Bullish divergence", "Bearish divergence", "Support test", "Resistance rejection", "Breakout", "Bullish engulfing", "Bearish engulfing", "Order block", "Bullish structure", "Bearish structure"]

export function TradeForm({ initialTrade, onSubmit, onClose }: TradeFormProps) {
  const [ticker, setTicker] = useState(initialTrade?.ticker ?? "")
  const [date, setDate] = useState(initialTrade?.date ?? new Date().toISOString().split("T")[0])
  const [account, setAccount] = useState<Account>(initialTrade?.account ?? "Bybit")
  const [session, setSession] = useState<Session>(initialTrade?.session ?? "NEW YORK")
  const [position, setPosition] = useState<Position>(initialTrade?.position ?? "LONG")
  const [direction, setDirection] = useState<Direction>(initialTrade?.direction ?? "Reversal")
  const [selectedContexts, setSelectedContexts] = useState<string[]>(initialTrade?.context ?? [])
  const [selectedSetups, setSelectedSetups] = useState<string[]>(initialTrade?.setup ?? [])
  const [result, setResult] = useState<TradeResult>(initialTrade?.result ?? "TP")
  const [rr, setRr] = useState(initialTrade != null ? String(initialTrade.rr) : "2")
  const [risk, setRisk] = useState(initialTrade != null ? String(initialTrade.risk) : "1")
  const [pnl, setPnl] = useState(initialTrade != null ? String(initialTrade.pnl) : "")
  const [entryDetails, setEntryDetails] = useState(initialTrade?.entryDetails ?? "")
  const [comment, setComment] = useState(initialTrade?.comment ?? "")
  const [note, setNote] = useState(initialTrade?.note ?? "")
  const [selectedEmotions, setSelectedEmotions] = useState<Emotion[]>(initialTrade?.emotions ?? [])
  const [screenshots, setScreenshots] = useState<string[]>(initialTrade?.screenshots ?? [])
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (initialTrade) {
      setTicker(initialTrade.ticker)
      setDate(initialTrade.date)
      setAccount(initialTrade.account as Account)
      setSession(initialTrade.session as Session)
      setPosition(initialTrade.position as Position)
      setDirection(initialTrade.direction as Direction)
      setSelectedContexts(initialTrade.context ?? [])
      setSelectedSetups(initialTrade.setup ?? [])
      setResult(initialTrade.result)
      setRr(String(initialTrade.rr))
      setRisk(String(initialTrade.risk))
      setPnl(String(initialTrade.pnl))
      setEntryDetails(initialTrade.entryDetails ?? "")
      setComment(initialTrade.comment ?? "")
      setNote(initialTrade.note ?? "")
      setSelectedEmotions(initialTrade.emotions ?? [])
      setScreenshots(initialTrade.screenshots ?? [])
    }
  }, [initialTrade])

  const toggleItem = <T extends string>(arr: T[], item: T, setter: (v: T[]) => void) => {
    setter(arr.includes(item) ? arr.filter((i) => i !== item) : [...arr, item])
  }

  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (!files) return

    Array.from(files).forEach((file) => {
      if (file.type.startsWith("image/")) {
        const reader = new FileReader()
        reader.onload = (ev) => {
          const dataUrl = ev.target?.result as string
          if (dataUrl) {
            setScreenshots((prev) => [...prev, dataUrl])
          }
        }
        reader.readAsDataURL(file)
      }
    })

    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }

  const removeScreenshot = (index: number) => {
    setScreenshots((prev) => prev.filter((_, i) => i !== index))
  }

  const handleSubmit = () => {
    if (!ticker || !date) return

    const calculatedPnl = pnl
      ? Number.parseFloat(pnl)
      : result === "TP"
        ? Number.parseFloat(risk) * Number.parseFloat(rr)
        : result === "SL"
          ? -Number.parseFloat(risk)
          : 0

    const tradeData = {
      ticker: ticker.toUpperCase(),
      date,
      account,
      session,
      position,
      direction,
      context: selectedContexts,
      setup: selectedSetups,
      result,
      rr: Number.parseFloat(rr),
      risk: Number.parseFloat(risk),
      pnl: calculatedPnl,
      entryDetails,
      comment,
      note,
      emotions: selectedEmotions,
      screenshots,
    }

    if (initialTrade) {
      onSubmit({ ...tradeData, id: initialTrade.id })
    } else {
      onSubmit(tradeData)
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-md animate-fade-in"
      role="dialog"
      aria-modal="true"
      aria-labelledby="trade-form-title"
    >
      <div className="mx-4 max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl border border-white/[0.08] bg-[hsl(240,8%,8%)] p-6 sm:p-8 shadow-2xl animate-slide-up">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 id="trade-form-title" className="text-xl font-bold text-foreground">
              {initialTrade ? "Редактировать сделку" : "Новая сделка"}
            </h2>
            <p className="text-sm text-muted-foreground">
              {initialTrade ? "Измените данные и сохраните" : "Заполните данные сделки"}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-white/[0.06] hover:text-foreground"
            aria-label="Закрыть"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="flex flex-col gap-6">
          {/* Row 1: Ticker + Date */}
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Ticker (Pair)
              </Label>
              <Input
                placeholder="BTC/USDT"
                value={ticker}
                onChange={(e) => setTicker(e.target.value)}
                className="glass border-border/30 bg-transparent text-foreground placeholder:text-muted-foreground focus:border-accent"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Date
              </Label>
              <Input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="glass border-border/30 bg-transparent text-foreground focus:border-accent"
              />
            </div>
          </div>

          {/* Row 2: Account + Session */}
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Account
              </Label>
              <Select value={account} onValueChange={(v) => setAccount(v as Account)}>
                <SelectTrigger className="glass border-border/30 bg-transparent text-foreground">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="glass-strong border-border/30 bg-card text-foreground">
                  {ACCOUNTS.map((a) => (
                    <SelectItem key={a} value={a}>{a}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Session
              </Label>
              <Select value={session} onValueChange={(v) => setSession(v as Session)}>
                <SelectTrigger className="glass border-border/30 bg-transparent text-foreground">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="glass-strong border-border/30 bg-card text-foreground">
                  {SESSIONS.map((s) => (
                    <SelectItem key={s} value={s}>{s}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Row 3: Position + Direction */}
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Position
              </Label>
              <div className="flex gap-2">
                {POSITIONS.map((p) => (
                  <button
                    key={p}
                    type="button"
                    onClick={() => setPosition(p)}
                    className={cn(
                      "flex-1 rounded-xl px-4 py-2.5 text-sm font-medium transition-all",
                      position === p
                        ? p === "LONG"
                          ? "bg-success/20 text-success border border-success/30"
                          : "bg-destructive/20 text-destructive border border-destructive/30"
                        : "glass text-muted-foreground hover:text-foreground"
                    )}
                  >
                    {p}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Direction
              </Label>
              <Select value={direction} onValueChange={(v) => setDirection(v as Direction)}>
                <SelectTrigger className="glass border-border/30 bg-transparent text-foreground">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="glass-strong border-border/30 bg-card text-foreground">
                  {DIRECTIONS.map((d) => (
                    <SelectItem key={d} value={d}>{d}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Context Multi-select */}
          <div className="flex flex-col gap-2">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Context
            </Label>
            <div className="flex flex-wrap gap-2">
              {CONTEXTS.map((ctx) => (
                <button
                  key={ctx}
                  type="button"
                  onClick={() => toggleItem(selectedContexts, ctx, setSelectedContexts)}
                  className={cn(
                    "rounded-full px-3 py-1.5 text-xs font-medium transition-all",
                    selectedContexts.includes(ctx)
                      ? "bg-accent/20 text-accent border border-accent/30"
                      : "glass text-muted-foreground hover:text-foreground"
                  )}
                >
                  {ctx}
                </button>
              ))}
            </div>
          </div>

          {/* Setup Multi-select */}
          <div className="flex flex-col gap-2">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Setup
            </Label>
            <div className="flex flex-wrap gap-2">
              {SETUPS.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => toggleItem(selectedSetups, s, setSelectedSetups)}
                  className={cn(
                    "rounded-full px-3 py-1.5 text-xs font-medium transition-all",
                    selectedSetups.includes(s)
                      ? "bg-accent/20 text-accent border border-accent/30"
                      : "glass text-muted-foreground hover:text-foreground"
                  )}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>

          {/* Result + RR + Risk */}
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Result
              </Label>
              <div className="flex gap-2">
                {RESULTS.map((r) => (
                  <button
                    key={r}
                    type="button"
                    onClick={() => setResult(r)}
                    className={cn(
                      "flex-1 rounded-xl px-3 py-2.5 text-sm font-medium transition-all",
                      result === r
                        ? r === "TP"
                          ? "bg-success/20 text-success border border-success/30"
                          : r === "SL"
                            ? "bg-destructive/20 text-destructive border border-destructive/30"
                            : "bg-accent/20 text-accent border border-accent/30"
                        : "glass text-muted-foreground hover:text-foreground"
                    )}
                  >
                    {r}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                RR (Risk/Reward)
              </Label>
              <Input
                type="number"
                step="0.1"
                value={rr}
                onChange={(e) => setRr(e.target.value)}
                className="glass border-border/30 bg-transparent text-foreground focus:border-accent"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Risk %
              </Label>
              <Input
                type="number"
                step="0.25"
                value={risk}
                onChange={(e) => setRisk(e.target.value)}
                className="glass border-border/30 bg-transparent text-foreground focus:border-accent"
              />
            </div>
          </div>

          {/* PnL override */}
          <div className="flex flex-col gap-2">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              PnL % (optional, auto-calculated)
            </Label>
            <Input
              type="number"
              step="0.01"
              placeholder="Auto-calculated from Risk x RR"
              value={pnl}
              onChange={(e) => setPnl(e.target.value)}
              className="glass border-border/30 bg-transparent text-foreground placeholder:text-muted-foreground focus:border-accent"
            />
          </div>

          {/* Photos / Screenshots */}
          <div className="flex flex-col gap-3">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Фото сделки (скриншоты, графики)
            </Label>
            <div
              role="button"
              tabIndex={0}
              onDragOver={(e) => {
                e.preventDefault()
                e.currentTarget.classList.add("border-accent/60", "bg-accent/5")
              }}
              onDragLeave={(e) => {
                e.currentTarget.classList.remove("border-accent/60", "bg-accent/5")
              }}
              onDrop={(e) => {
                e.preventDefault()
                e.currentTarget.classList.remove("border-accent/60", "bg-accent/5")
                const files = e.dataTransfer?.files
                if (files) handleImageUpload({ target: { files } } as React.ChangeEvent<HTMLInputElement>)
              }}
              onClick={() => fileInputRef.current?.click()}
              onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
              className="flex min-h-[140px] flex-col items-center justify-center gap-3 rounded-2xl border-2 border-dashed border-border/40 bg-secondary/20 p-6 text-center transition-all hover:border-accent/50 hover:bg-accent/5 cursor-pointer"
            >
              <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-accent/15">
                <ImagePlus className="h-7 w-7 text-accent" />
              </div>
              <div>
                <p className="text-sm font-medium text-foreground">Перетащите фото сюда или нажмите</p>
                <p className="mt-0.5 text-xs text-muted-foreground">PNG, JPG, WEBP • до 10 МБ</p>
              </div>
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              multiple
              onChange={handleImageUpload}
              className="hidden"
            />
            {screenshots.length > 0 && (
              <div className="flex flex-col gap-2">
                <p className="text-xs font-medium text-muted-foreground">
                  {screenshots.length} фото прикреплено
                </p>
                <div className="flex flex-wrap gap-3">
                  {screenshots.map((src, i) => (
                    <div key={`screenshot-${i}`} className="group relative">
                      <div className="relative h-28 w-40 overflow-hidden rounded-xl border border-border/30 shadow-lg">
                        <Image
                          src={src || "/placeholder.svg"}
                          alt={`Фото сделки ${i + 1}`}
                          fill
                          className="object-cover"
                          unoptimized
                        />
                      </div>
                      <button
                        type="button"
                        onClick={(e) => { e.stopPropagation(); removeScreenshot(i) }}
                        className="absolute -right-2 -top-2 flex h-7 w-7 items-center justify-center rounded-full bg-destructive text-destructive-foreground shadow-lg transition-opacity hover:opacity-90"
                        aria-label="Удалить фото"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  ))}
                  <button
                    type="button"
                    onClick={(e) => { e.stopPropagation(); fileInputRef.current?.click() }}
                    className="flex h-28 w-20 flex-col items-center justify-center gap-1 rounded-xl border border-dashed border-border/40 text-muted-foreground transition-all hover:border-accent/50 hover:text-accent"
                  >
                    <ImagePlus className="h-5 w-5" />
                    <span className="text-[10px]">Ещё</span>
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Entry Details */}
          <div className="flex flex-col gap-2">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Entry Details
            </Label>
            <Textarea
              placeholder="Describe your entry..."
              value={entryDetails}
              onChange={(e) => setEntryDetails(e.target.value)}
              className="glass min-h-[80px] border-border/30 bg-transparent text-foreground placeholder:text-muted-foreground focus:border-accent resize-none"
            />
          </div>

          {/* Comment + Note */}
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Comment
              </Label>
              <Textarea
                placeholder="Your comment..."
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                className="glass min-h-[80px] border-border/30 bg-transparent text-foreground placeholder:text-muted-foreground focus:border-accent resize-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Note
              </Label>
              <Textarea
                placeholder="Additional notes..."
                value={note}
                onChange={(e) => setNote(e.target.value)}
                className="glass min-h-[80px] border-border/30 bg-transparent text-foreground placeholder:text-muted-foreground focus:border-accent resize-none"
              />
            </div>
          </div>

          {/* Emotions */}
          <div className="flex flex-col gap-2">
            <Label className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Emotions
            </Label>
            <div className="flex flex-wrap gap-2">
              {EMOTIONS.map((e) => (
                <button
                  key={e}
                  type="button"
                  onClick={() => toggleItem(selectedEmotions, e, setSelectedEmotions)}
                  className={cn(
                    "rounded-full px-3 py-1.5 text-xs font-medium transition-all",
                    selectedEmotions.includes(e)
                      ? "bg-accent/20 text-accent border border-accent/30"
                      : "glass text-muted-foreground hover:text-foreground"
                  )}
                >
                  {e}
                </button>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-2">
            <Button
              variant="outline"
              onClick={onClose}
              className="rounded-xl border-border/30 bg-transparent text-foreground hover:bg-secondary"
            >
              Отмена
            </Button>
            <Button
              onClick={handleSubmit}
              disabled={!ticker || !date}
              className="gap-2 rounded-xl bg-accent text-accent-foreground hover:bg-accent/90"
            >
              {initialTrade ? (
                "Сохранить"
              ) : (
                <>
                  <Plus className="h-4 w-4" />
                  Добавить сделку
                </>
              )}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
