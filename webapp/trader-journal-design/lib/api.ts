import type { Trade } from "./types"

const API_BASE =
  typeof process !== "undefined" && process.env?.NEXT_PUBLIC_API_URL
    ? process.env.NEXT_PUBLIC_API_URL
    : "/api"

const RETRY_COUNT = 2
const RETRY_DELAY_MS = 1000

/** Ошибка API с кодом и телом ответа (валидация и т.д.) */
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public body?: string
  ) {
    super(message)
    this.name = "ApiError"
  }
}

async function getErrorBody(res: Response): Promise<string> {
  try {
    const text = await res.text()
    if (!text) return res.statusText || "Ошибка запроса"
    try {
      const j = JSON.parse(text) as { message?: string; errors?: string[]; title?: string }
      if (typeof j.message === "string") return j.message
      if (Array.isArray(j.errors) && j.errors.length) return j.errors.join(". ")
      if (typeof j.title === "string") return j.title
    } catch {
      // not JSON
    }
    return text.length > 200 ? text.slice(0, 200) + "…" : text
  } catch {
    return res.statusText || "Ошибка запроса"
  }
}

async function fetchWithRetry(
  url: string,
  options?: RequestInit,
  retriesLeft = RETRY_COUNT
): Promise<Response> {
  let lastError: unknown
  for (let attempt = 0; attempt <= retriesLeft; attempt++) {
    try {
      const res = await fetch(url, options)
      const isRetryable = res.status >= 500 || res.status === 429
      if (res.ok || !isRetryable || attempt === retriesLeft) return res
      lastError = new Error(`HTTP ${res.status}`)
    } catch (e) {
      lastError = e
      if (attempt === retriesLeft) throw e
    }
    await new Promise((r) => setTimeout(r, RETRY_DELAY_MS))
  }
  throw lastError
}

async function apiRequest<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetchWithRetry(url, options)
  if (!res.ok) {
    const body = await getErrorBody(res)
    throw new ApiError(body || res.statusText || "Ошибка запроса", res.status, body)
  }
  return res.json() as Promise<T>
}

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

export interface UpdateTradePayload {
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
  pnL?: number
  screenshots?: string[]
}

export async function fetchTrades(from?: string, to?: string): Promise<ApiTrade[]> {
  const params = new URLSearchParams()
  if (from) params.set("from", from)
  if (to) params.set("to", to)
  const q = params.toString()
  return apiRequest<ApiTrade[]>(`${API_BASE}/trades${q ? `?${q}` : ""}`)
}

export async function fetchStats(from?: string, to?: string): Promise<ApiStats> {
  const params = new URLSearchParams()
  if (from) params.set("from", from)
  if (to) params.set("to", to)
  const q = params.toString()
  return apiRequest<ApiStats>(`${API_BASE}/stats${q ? `?${q}` : ""}`)
}

export async function createTrade(payload: CreateTradePayload): Promise<ApiTrade> {
  return apiRequest<ApiTrade>(`${API_BASE}/trades`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  })
}

export async function updateTrade(id: number, payload: UpdateTradePayload): Promise<ApiTrade> {
  return apiRequest<ApiTrade>(`${API_BASE}/trades/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  })
}

export async function deleteTrade(id: number): Promise<void> {
  const res = await fetchWithRetry(`${API_BASE}/trades/${id}`, { method: "DELETE" })
  if (!res.ok) {
    const body = await getErrorBody(res)
    throw new ApiError(body || res.statusText || "Не удалось удалить сделку", res.status, body)
  }
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

export function tradeToUpdatePayload(trade: Trade): UpdateTradePayload {
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
