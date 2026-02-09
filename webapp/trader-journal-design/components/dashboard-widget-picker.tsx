"use client"

import { useState } from "react"
import { Settings2, GripVertical, Check } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  WIDGET_IDS,
  WIDGET_LABELS,
  type WidgetId,
  saveDashboardConfig,
} from "@/lib/dashboard-config"

interface DashboardWidgetPickerProps {
  currentWidgets: WidgetId[]
  onApply: (widgets: WidgetId[]) => void
}

export function DashboardWidgetPicker({ currentWidgets, onApply }: DashboardWidgetPickerProps) {
  const [open, setOpen] = useState(false)
  const [selected, setSelected] = useState<WidgetId[]>(() => currentWidgets)

  const handleOpen = () => {
    setSelected([...currentWidgets])
    setOpen(true)
  }

  const toggleWidget = (id: WidgetId) => {
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((w) => w !== id) : [...prev, id]
    )
  }

  const moveUp = (index: number) => {
    if (index <= 0) return
    setSelected((prev) => {
      const next = [...prev]
      ;[next[index - 1], next[index]] = [next[index], next[index - 1]]
      return next
    })
  }

  const moveDown = (index: number) => {
    if (index >= selected.length - 1) return
    setSelected((prev) => {
      const next = [...prev]
      ;[next[index], next[index + 1]] = [next[index + 1], next[index]]
      return next
    })
  }

  const handleApply = () => {
    saveDashboardConfig(selected)
    onApply(selected)
    setOpen(false)
  }

  const handleReset = () => {
    setSelected([...WIDGET_IDS])
  }

  return (
    <>
      <Button
        variant="outline"
        size="sm"
        onClick={handleOpen}
        className="gap-2 rounded-xl border-border/30 bg-transparent text-muted-foreground hover:bg-secondary hover:text-foreground"
      >
        <Settings2 className="h-4 w-4" />
        Настроить виджеты
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="glass-strong max-w-lg border-border/30 bg-card text-foreground">
          <DialogHeader>
            <DialogTitle className="text-lg">Настройка дашборда</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Выберите виджеты и измените их порядок. Отображенные виджеты показаны ниже.
          </p>

          <div className="space-y-3">
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Включить / выключить
            </p>
            <div className="flex flex-wrap gap-2">
              {WIDGET_IDS.map((id) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => toggleWidget(id)}
                  className={`flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-all ${
                    selected.includes(id)
                      ? "bg-accent/20 text-accent border border-accent/30"
                      : "glass text-muted-foreground hover:text-foreground"
                  }`}
                >
                  {selected.includes(id) && <Check className="h-3 w-3" />}
                  {WIDGET_LABELS[id]}
                </button>
              ))}
            </div>

            <p className="mt-6 text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Порядок отображения
            </p>
            <div className="flex flex-col gap-2 rounded-xl border border-border/20 p-3">
              {selected.map((id, index) => (
                <div
                  key={id}
                  className="flex items-center gap-2 rounded-lg bg-secondary/30 px-3 py-2"
                >
                  <GripVertical className="h-4 w-4 shrink-0 text-muted-foreground" />
                  <span className="flex-1 text-sm font-medium">{WIDGET_LABELS[id]}</span>
                  <div className="flex gap-1">
                    <button
                      type="button"
                      onClick={() => moveUp(index)}
                      disabled={index === 0}
                      className="rounded p-1 text-muted-foreground hover:bg-secondary disabled:opacity-30"
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      onClick={() => moveDown(index)}
                      disabled={index === selected.length - 1}
                      className="rounded p-1 text-muted-foreground hover:bg-secondary disabled:opacity-30"
                    >
                      ↓
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="flex items-center justify-between pt-4">
            <Button variant="ghost" size="sm" onClick={handleReset} className="text-muted-foreground">
              Сбросить
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setOpen(false)}>
                Отмена
              </Button>
              <Button onClick={handleApply}>Применить</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
