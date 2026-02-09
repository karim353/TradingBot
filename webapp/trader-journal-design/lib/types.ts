export type TradeResult = "TP" | "SL" | "BE"
export type Position = "LONG" | "SHORT"
export type Session = "ASIA" | "LONDON" | "NEW YORK" | "FRANKFURT"
export type Account = "BingX" | "Bybit" | "Binance" | "OKX" | string
export type Direction = "Reversal" | "Long" | "Short" | "Continuation" | string
export type Emotion = "Nervous" | "Confident" | "Anxious" | "Calm" | "Excited" | "Frustrated" | "Focused" | "Uncertain" | string

export interface Trade {
  id: string
  ticker: string
  date: string
  account: Account
  session: Session
  position: Position
  direction: Direction
  context: string[]
  setup: string[]
  result: TradeResult
  rr: number
  risk: number
  pnl: number
  entryDetails: string
  comment: string
  note: string
  emotions: Emotion[]
  screenshots?: string[]
  notionPageId?: string
}

export interface DailyPnL {
  date: string
  pnl: number
  trades: number
}

export interface Stats {
  totalTrades: number
  winRate: number
  totalPnl: number
  avgRR: number
  bestTrade: number
  worstTrade: number
  profitFactor: number
  streak: number
}
