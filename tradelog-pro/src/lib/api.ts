const API_BASE =
  (typeof import.meta !== "undefined" && import.meta.env?.VITE_API_URL) ||
  "http://localhost:5000/api"

export interface ApiTrade {
  id: number
  userId: number
  date: string
  ticker: string
  account?: string
  session?: string
  position?: string
  direction?: string
  context: string[]
  setup: string[]
  result?: string
  rr?: string
  risk?: number
  entryDetails?: string
  comment?: string
  note?: string
  emotions: string[]
  pnL: number
  notionPageId?: string
  screenshots?: string[]
}

export type ProTrade = {
  id: number
  pair: string
  type: "LONG" | "SHORT"
  entry: string
  exit: string
  pnl: string
  positive: boolean
  date: string
  dateIso?: string
  strategy: string
  emotion: string
  notes: string
  image: string | null
}

export interface CreateTradePayload {
  date?: string
  ticker?: string
  account?: string
  session?: string
  position?: string
  direction?: string
  context?: string[]
  setup?: string[]
  result?: string
  rr?: string
  risk?: number
  entryDetails?: string
  comment?: string
  note?: string
  emotions?: string[]
  pnL: number
  screenshots?: string[]
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options?.headers ?? {}),
    },
  })
  if (!res.ok) {
    const text = await res.text().catch(() => "")
    const msg = text || res.statusText || "Request failed"
    throw new Error(msg)
  }
  return res.json() as Promise<T>
}

export async function fetchApiTrades(): Promise<ApiTrade[]> {
  const res = await request<ApiTrade[]>(`${API_BASE}/trades`)
  return res
}

export function apiTradeToProTrade(api: ApiTrade): ProTrade {
  let pair = api.ticker?.trim().toUpperCase() || "UNKNOWN"
  if (pair && !pair.includes("/")) {
    pair = `${pair}/USDT`
  }

  const pnlNumber = api.pnL ?? 0
  const positive = pnlNumber >= 0
  const pnl =
    (positive ? "+" : "") +
    (Number.isFinite(pnlNumber) ? pnlNumber.toFixed(2) : "0.00") +
    "%"

  const primaryEmotion = api.emotions?.[0] || "Neutral"
  const primarySetup = api.setup?.[0] || "Manual Entry"
  const notes = api.note || api.comment || ""

  let dateLabel = api.date
  try {
    const d = new Date(api.date)
    if (!Number.isNaN(d.getTime())) {
      dateLabel = d.toLocaleString("ru-RU", {
        year: "numeric",
        month: "short",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      })
    }
  } catch {
    // keep raw
  }

  let dateIso: string | undefined
  try {
    const d = new Date(api.date)
    if (!Number.isNaN(d.getTime())) dateIso = d.toISOString().slice(0, 10)
  } catch {
    dateIso = undefined
  }

  return {
    id: api.id,
    pair,
    type: (api.position === "SHORT" ? "SHORT" : "LONG") as "LONG" | "SHORT",
    entry: api.entryDetails || "—",
    exit: "",
    pnl,
    positive,
    date: dateLabel,
    dateIso,
    strategy: primarySetup,
    emotion: primaryEmotion,
    notes: notes || "No notes provided.",
    image: api.screenshots?.[0] ?? null,
  }
}

export async function createTradeFromUi(ui: {
  pair: string
  type: "LONG" | "SHORT"
  entry: string
  exit: string
  pnlNumber: number
  notes: string
}): Promise<ProTrade> {
  const nowIso = new Date().toISOString()
  const rawTicker = (ui.pair || "UNKNOWN").toUpperCase()
  const ticker = rawTicker.includes("/") ? rawTicker.split("/")[0] : rawTicker

  const pnl = ui.pnlNumber
  const result = pnl > 0 ? "TP" : pnl < 0 ? "SL" : "BE"

  const payload: CreateTradePayload = {
    ticker,
    date: nowIso,
    account: "Bybit",
    session: "LONDON",
    position: ui.type,
    direction: ui.type === "LONG" ? "Reversal" : "Continuation",
    context: [],
    setup: ["Manual Entry"],
    result,
    rr: "2",
    risk: 1,
    entryDetails: `Entry ${ui.entry || "0.00"}, Exit ${ui.exit || "0.00"}`,
    comment: "",
    note: ui.notes,
    emotions: ["Neutral"],
    pnL: pnl,
    screenshots: [],
  }

  const created = await request<ApiTrade>(`${API_BASE}/trades`, {
    method: "POST",
    body: JSON.stringify(payload),
  })

  return apiTradeToProTrade(created)
}

