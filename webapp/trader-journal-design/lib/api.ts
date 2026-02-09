import type { Trade } from "./types"

const API_BASE =
  typeof process !== "undefined" && process.env?.NEXT_PUBLIC_API_URL
    ? process.env.NEXT_PUBLIC_API_URL
    : "/api"

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

export interface ApiStats {
  tradeCount: number
  profit: number
  loss: number
  winRate: number
  averagePnL: number
  bestResult: number
  worstResult: number
  totalPnL: number
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

export async function fetchTrades(from?: string, to?: string): Promise<ApiTrade[]> {
  const params = new URLSearchParams()
  if (from) params.set("from", from)
  if (to) params.set("to", to)
  const q = params.toString()
  const res = await fetch(`${API_BASE}/trades${q ? `?${q}` : ""}`)
  if (!res.ok) throw new Error("Failed to fetch trades")
  return res.json()
}

export async function fetchStats(from?: string, to?: string): Promise<ApiStats> {
  const params = new URLSearchParams()
  if (from) params.set("from", from)
  if (to) params.set("to", to)
  const q = params.toString()
  const res = await fetch(`${API_BASE}/stats${q ? `?${q}` : ""}`)
  if (!res.ok) throw new Error("Failed to fetch stats")
  return res.json()
}

export async function createTrade(payload: CreateTradePayload): Promise<ApiTrade> {
  const res = await fetch(`${API_BASE}/trades`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error("Failed to create trade")
  return res.json()
}

export async function deleteTrade(id: number): Promise<void> {
  const res = await fetch(`${API_BASE}/trades/${id}`, { method: "DELETE" })
  if (!res.ok) throw new Error("Failed to delete trade")
}

export function apiTradeToTrade(api: ApiTrade): Trade {
  const dateOnly = api.date.includes("T") ? api.date.slice(0, 10) : api.date.slice(0, 10)
  return {
    id: String(api.id),
    ticker: api.ticker ?? "",
    date: dateOnly,
    account: (api.account as Trade["account"]) ?? "Bybit",
    session: (api.session as Trade["session"]) ?? "LONDON",
    position: (api.position as Trade["position"]) ?? "LONG",
    direction: (api.direction as Trade["direction"]) ?? "Reversal",
    context: api.context ?? [],
    setup: api.setup ?? [],
    result: (api.result as Trade["result"]) ?? "TP",
    rr: api.rr ? parseFloat(api.rr) : 0,
    risk: api.risk ?? 0,
    pnl: api.pnL ?? 0,
    entryDetails: api.entryDetails ?? "",
    comment: api.comment ?? "",
    note: api.note ?? "",
    emotions: api.emotions ?? [],
    notionPageId: api.notionPageId,
    screenshots: api.screenshots,
  }
}

export function tradeToCreatePayload(trade: Omit<Trade, "id">): CreateTradePayload {
  return {
    ticker: trade.ticker,
    date: trade.date,
    account: typeof trade.account === "string" ? trade.account : undefined,
    session: typeof trade.session === "string" ? trade.session : undefined,
    position: typeof trade.position === "string" ? trade.position : undefined,
    direction: typeof trade.direction === "string" ? trade.direction : undefined,
    context: trade.context.length ? trade.context : undefined,
    setup: trade.setup.length ? trade.setup : undefined,
    result: trade.result,
    rr: String(trade.rr),
    risk: trade.risk,
    entryDetails: trade.entryDetails || undefined,
    comment: trade.comment || undefined,
    note: trade.note || undefined,
    emotions: trade.emotions.length ? trade.emotions : undefined,
    pnL: trade.pnl,
    screenshots: trade.screenshots?.length ? trade.screenshots : undefined,
  }
}
