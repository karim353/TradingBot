import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  TrendingUp,
  TrendingDown,
  ArrowUpRight,
  ArrowDownRight,
  Activity,
  Settings,
  History,
  Wallet,
  ChevronDown,
  RefreshCw,
  Search,
  MoreVertical,
  CandlestickChart,
  PieChart as PieChartIcon,
  BarChart2
} from 'lucide-react';
import {
  AreaChart,
  Area,
  XAxis,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  Cell,
  PieChart,
  Pie,
} from 'recharts';
import { cn } from '../lib/utils';
import type { ProTrade } from '../lib/api';
import {
  getEquityCurveData,
  getMonthlyPnLData,
  getWinLoss,
  getLongShortStats,
  getAssetAllocation,
  getTradingMetrics,
  getStrategyStats,
  getEmotionStats,
  getStreakStats,
  getJournalQuality,
  parsePnlNumber,
} from '../lib/trade-stats';

function formatMonth(ym: string): string {
  const [y, m] = ym.split('-')
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
  const monthName = months[parseInt(m || '1', 10) - 1] || ym
  return y ? `${monthName} ${y.slice(2)}` : ym
}

export const MonthlyPnLChartWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const chartData = trades.length ? getMonthlyPnLData(trades).map(({ month, pnl }) => ({ month: formatMonth(month), pnl })) : []
  const totalPnL = Math.round(chartData.reduce((s, m) => s + m.pnl, 0) * 100) / 100
  const avgMonth = chartData.length ? Math.round((totalPnL / chartData.length) * 100) / 100 : 0
  const bestMonth = chartData.length ? chartData.reduce((best, m) => (m.pnl > best.pnl ? m : best), chartData[0]) : null
  const worstMonth = chartData.length ? chartData.reduce((worst, m) => (m.pnl < worst.pnl ? m : worst), chartData[0]) : null
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between mb-6 relative z-10">
        <div>
          <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Monthly P&L</h3>
          <p className="text-[10px] text-[#777777] font-medium tracking-widest mt-1 uppercase">Net Profit by Month</p>
        </div>
        <div className="w-8 h-8 rounded-lg bg-[#ffffff05] border border-[#ffffff10] flex items-center justify-center text-[#777777]">
          <BarChart2 className="w-4 h-4" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-2 mb-4">
        <div className="rounded-lg border border-[#ffffff10] bg-[#ffffff03] px-3 py-2">
          <div className="text-[9px] text-[#6d7388] uppercase tracking-wider">Total</div>
          <div className={cn("font-mono text-sm", totalPnL >= 0 ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]")}>
            {totalPnL >= 0 ? '+' : ''}{totalPnL}%
          </div>
        </div>
        <div className="rounded-lg border border-[#ffffff10] bg-[#ffffff03] px-3 py-2">
          <div className="text-[9px] text-[#6d7388] uppercase tracking-wider">Avg / Month</div>
          <div className={cn("font-mono text-sm", avgMonth >= 0 ? "text-[#e0e0e0]" : "text-[#c6c6c6]")}>
            {avgMonth >= 0 ? '+' : ''}{avgMonth}%
          </div>
        </div>
      </div>
      <div className="flex-1">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 10, right: 0, left: 0, bottom: 24 }}>
            <defs>
              <linearGradient id="pnlGreenGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="var(--color-premium-green)" stopOpacity={1} />
                <stop offset="100%" stopColor="var(--color-premium-green)" stopOpacity={0.2} />
              </linearGradient>
              <linearGradient id="pnlRedGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="var(--color-premium-red)" stopOpacity={1} />
                <stop offset="100%" stopColor="var(--color-premium-red)" stopOpacity={0.2} />
              </linearGradient>
            </defs>
            <XAxis dataKey="month" tick={{ fontSize: 10 }} stroke="rgba(255,255,255,0.2)" />
            <Tooltip
              cursor={{ fill: 'rgba(255,255,255,0.05)' }}
              contentStyle={{ backgroundColor: 'rgba(5,5,16,0.7)', backdropFilter: 'blur(12px)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '12px', boxShadow: '0 8px 32px rgba(0,0,0,0.5)' }}
              itemStyle={{ color: '#fff', fontSize: '12px', fontWeight: 'bold' }}
              formatter={(value: number) => [`${value >= 0 ? '+' : ''}${value}%`, 'P&L']}
            />
            <Bar dataKey="pnl" radius={[6, 6, 6, 6]}>
              {chartData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.pnl >= 0 ? 'url(#pnlGreenGrad)' : 'url(#pnlRedGrad)'} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
      <div className="mt-4 grid grid-cols-2 gap-2">
        <div className="text-[10px] text-[#777777]">
          Best: <span className="text-[#d6dbe8]">{bestMonth ? `${bestMonth.month} (${bestMonth.pnl > 0 ? '+' : ''}${bestMonth.pnl}%)` : '—'}</span>
        </div>
        <div className="text-[10px] text-[#777777] text-right">
          Worst: <span className="text-[#d6dbe8]">{worstMonth ? `${worstMonth.month} (${worstMonth.pnl > 0 ? '+' : ''}${worstMonth.pnl}%)` : '—'}</span>
        </div>
      </div>
    </div>
  )
};

export const WinLossDonutWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const { wins, losses, winRatePct } = getWinLoss(trades)
  const metrics = getTradingMetrics(trades)
  const donutData = [
    { name: 'Wins', value: wins, color: 'var(--color-premium-green)' },
    { name: 'Losses', value: losses, color: 'var(--color-premium-red)' },
  ].filter((d) => d.value > 0)
  if (donutData.length === 0) donutData.push({ name: 'No trades', value: 1, color: 'rgba(255,255,255,0.2)' })
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between mb-4 relative z-10">
        <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Win Rate</h3>
        <PieChartIcon className="w-4 h-4 text-[#777777]" />
      </div>
      <div className="flex-1 relative flex items-center justify-center">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={donutData}
              cx="50%"
              cy="50%"
              innerRadius={45}
              outerRadius={65}
              paddingAngle={5}
              dataKey="value"
              stroke="none"
            >
              {donutData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} style={{ filter: `drop-shadow(0 0 8px ${entry.color.includes('rgba') ? 'transparent' : entry.color})` }} />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
          <span className="text-3xl font-mono font-medium tracking-tighter text-[#e0e0e0]">{winRatePct}%</span>
          <span className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Win Rate</span>
        </div>
      </div>
      <div className="flex justify-between mt-4 border-t border-[#ffffff10] pt-4 relative z-10">
        <div className="text-center w-full border-r border-[#ffffff10]">
          <div className="text-base font-mono font-medium text-[var(--color-premium-green)]">{wins}</div>
          <div className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Wins</div>
        </div>
        <div className="text-center w-full">
          <div className="text-base font-mono font-medium text-[var(--color-premium-red)]">{losses}</div>
          <div className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Losses</div>
        </div>
      </div>
      <div className="mt-3 text-[10px] text-[#777777] flex justify-between">
        <span>Profit factor</span>
        <span className="text-[#d7dce8] font-mono">{metrics.totalTrades ? metrics.profitFactor : '—'}</span>
      </div>
    </div>
  )
};

const symbolColors: Record<string, string> = {
  BTC: 'bg-orange-500', ETH: 'bg-blue-500', SOL: 'bg-purple-500', USDT: 'bg-green-500',
  BNB: 'bg-yellow-500', XRP: 'bg-gray-500', ADA: 'bg-blue-400', DOGE: 'bg-amber-400',
}

export const AssetAllocationWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const allocation = getAssetAllocation(trades)
  const total = allocation.reduce((s, a) => s + a.count, 0)
  const items: { symbol: string; percent: number; color: string }[] = total > 0
    ? allocation.slice(0, 8).map((a) => ({ symbol: a.symbol, percent: Math.round((a.count / total) * 100), color: symbolColors[a.symbol] || 'bg-white/30' }))
    : [
      { symbol: 'BTC', percent: 45, color: 'bg-orange-500' },
      { symbol: 'ETH', percent: 30, color: 'bg-blue-500' },
      { symbol: 'SOL', percent: 15, color: 'bg-purple-500' },
      { symbol: '—', percent: 10, color: 'bg-white/20' },
    ]
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest mb-6">Asset Allocation</div>
      <div className="flex-1 flex flex-col justify-center gap-5">
        {items.map((asset) => (
          <div key={asset.symbol} className="flex flex-col gap-2">
            <div className="flex justify-between items-end">
              <div className="flex items-center gap-3">
                <div className={cn("w-2 h-2 rounded-full", asset.color)} />
                <span className="text-xs font-medium text-[#e0e0e0]">{asset.symbol}</span>
              </div>
              <span className="text-xs font-mono text-[#a0a0a0]">{asset.percent}%</span>
            </div>
            <div className="h-1.5 w-full bg-[#111114] rounded-full overflow-hidden border border-[#ffffff05]">
              <div className={cn("h-full rounded-full", asset.color)} style={{ width: `${asset.percent}%`, color: asset.color.replace('bg-', '') }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  )
};

export const LongShortPerformanceWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const { long, short } = getLongShortStats(trades)
  const longWr = long.count ? Math.round((long.wins / long.count) * 1000) / 10 : 0
  const shortWr = short.count ? Math.round((short.wins / short.count) * 1000) / 10 : 0
  const fmt = (n: number) => (n >= 0 ? `+${n}%` : `${n}%`)
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest mb-6">Long vs Short</div>
      <div className="flex-1 grid grid-cols-2 gap-4">
        <div className="premium-glass p-5 flex flex-col justify-between relative overflow-hidden group">
          <div className="absolute top-0 right-0 w-24 h-24 bg-[var(--color-premium-green)]/10 rounded-bl-full blur-xl group-hover:bg-[var(--color-premium-green)]/20 transition-colors" />
          <div className="relative z-10">
            <div className="flex items-center gap-2 mb-1">
              <TrendingUp className="w-4 h-4 text-[var(--color-premium-green)]" />
              <span className="text-sm font-bold">Longs</span>
            </div>
            <div className="text-[10px] text-white/40 font-bold uppercase tracking-widest">{long.count} Trades</div>
          </div>
          <div className="relative z-10 mt-4">
            <div className={cn("text-2xl font-mono font-medium tracking-tight", long.pnlSum >= 0 ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]")}>{fmt(long.pnlSum)}</div>
            <div className="text-xs font-medium text-[#777777] mt-1">Win Rate: <span className="text-[#e0e0e0]">{longWr}%</span></div>
          </div>
        </div>
        <div className="premium-glass p-5 flex flex-col justify-between relative overflow-hidden group">
          <div className="absolute top-0 right-0 w-24 h-24 bg-[var(--color-premium-red)]/10 rounded-bl-full blur-xl group-hover:bg-[var(--color-premium-red)]/20 transition-colors" />
          <div className="relative z-10">
            <div className="flex items-center gap-2 mb-1">
              <TrendingDown className="w-4 h-4 text-[var(--color-premium-red)]" />
              <span className="text-sm font-bold">Shorts</span>
            </div>
            <div className="text-[10px] text-white/40 font-bold uppercase tracking-widest">{short.count} Trades</div>
          </div>
          <div className="relative z-10 mt-4">
            <div className={cn("text-2xl font-mono font-medium tracking-tight", short.pnlSum >= 0 ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]")}>{fmt(short.pnlSum)}</div>
            <div className="text-xs font-medium text-[#777777] mt-1">Win Rate: <span className="text-[#e0e0e0]">{shortWr}%</span></div>
          </div>
        </div>
      </div>
    </div>
  )
};

export const EquityCurveWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const equityData = getEquityCurveData(trades)
  const totalPnl = equityData.length ? equityData[equityData.length - 1].value : 0
  const peak = equityData.length ? Math.max(...equityData.map((d) => d.value)) : 0
  const maxDrawdown = Math.round((peak - totalPnl) * 100) / 100
  const displayData = equityData.length ? equityData : [{ name: '1', value: 0 }]
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Equity Curve</h3>
          <p className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Cumulative Net Profit %</p>
        </div>
        <div className="text-right">
          <div className={cn("text-2xl font-mono font-medium", totalPnl >= 0 ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]")}>
            {totalPnl >= 0 ? '+' : ''}{totalPnl}%
          </div>
          <div className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Total Return</div>
        </div>
      </div>
      <div className="flex-1">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={displayData}>
            <defs>
              <linearGradient id="equityGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="var(--color-neon-blue)" stopOpacity={0.3} />
                <stop offset="95%" stopColor="var(--color-neon-blue)" stopOpacity={0} />
              </linearGradient>
            </defs>
            <Area
              type="monotone"
              dataKey="value"
              stroke="var(--color-neon-blue)"
              strokeWidth={3}
              fill="url(#equityGrad)"
              animationDuration={1500}
              style={{ filter: 'drop-shadow(0 0 8px rgba(0,225,255,0.5))' }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3">
        <div className="rounded-lg border border-[#ffffff10] bg-[#ffffff03] px-3 py-2">
          <div className="text-[9px] text-[#6d7388] uppercase tracking-wider">Peak Equity</div>
          <div className="text-sm font-mono text-[#d8ddeb]">{peak > 0 ? `+${Math.round(peak * 100) / 100}%` : '0%'}</div>
        </div>
        <div className="rounded-lg border border-[#ffffff10] bg-[#ffffff03] px-3 py-2">
          <div className="text-[9px] text-[#6d7388] uppercase tracking-wider">From Peak</div>
          <div className={cn("text-sm font-mono", maxDrawdown > 0 ? "text-[var(--color-premium-red)]" : "text-[#d8ddeb]")}>
            {maxDrawdown > 0 ? `-${maxDrawdown}%` : '0%'}
          </div>
        </div>
      </div>
    </div>
  )
};

export const FearAndGreedWidget = () => {
  const value = 72; // Extreme Greed
  return (
    <div className="flex flex-col h-full items-center justify-center relative overflow-hidden z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest absolute top-0 left-0">Fear & Greed</div>

      <div className="relative w-48 h-48 flex items-center justify-center">
        <svg className="w-full h-full -rotate-90">
          <circle
            cx="96"
            cy="96"
            r="80"
            fill="none"
            stroke="currentColor"
            strokeWidth="8"
            className="text-[#111114]"
          />
          <circle
            cx="96"
            cy="96"
            r="80"
            fill="none"
            stroke="currentColor"
            strokeWidth="8"
            strokeDasharray={502}
            strokeDashoffset={502 - (502 * value) / 100}
            strokeLinecap="round"
            className="text-[var(--color-premium-green)] transition-all duration-1000 ease-out"
          />
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-5xl font-mono font-medium tracking-tighter text-[#e0e0e0]">{value}</span>
          <span className="text-[10px] font-medium text-[var(--color-premium-green)] uppercase tracking-widest mt-1">Greed</span>
        </div>
      </div>

      <div className="mt-6 flex justify-between w-full text-[9px] font-medium uppercase tracking-widest text-[#555555] px-6">
        <span>Fear</span>
        <span>Neutral</span>
        <span>Greed</span>
      </div>
    </div>
  );
};

export const TradingMetricsGrid = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const m = getTradingMetrics(trades)
  const pnlNumbers = trades.map(parsePnlNumber)
  const grossProfit = Math.round(pnlNumbers.filter((p) => p > 0).reduce((a, b) => a + b, 0) * 100) / 100
  const grossLoss = Math.round(Math.abs(pnlNumbers.filter((p) => p < 0).reduce((a, b) => a + b, 0)) * 100) / 100
  const metrics = [
    { label: 'Win Rate', value: m.totalTrades ? `${m.winRatePct}%` : '—', color: 'green' as const },
    { label: 'Profit Factor', value: m.totalTrades ? String(m.profitFactor) : '—', color: 'white' as const },
    { label: 'Max DD', value: m.totalTrades ? `${m.maxDrawdownPct}%` : '—', color: 'red' as const },
    { label: 'Expectancy', value: m.totalTrades ? `${m.expectancyPct >= 0 ? '+' : ''}${m.expectancyPct}%` : '—', color: 'green' as const },
    { label: 'Trades', value: m.totalTrades ? String(m.totalTrades) : '—', color: 'white' as const },
    { label: 'Gross P/L', value: m.totalTrades ? `+${grossProfit}% / -${grossLoss}%` : '—', color: 'white' as const },
  ]
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest mb-6">Core Metrics</div>
      <div className="grid grid-cols-2 gap-3 flex-1">
        {metrics.map((item) => (
          <div key={item.label} className="premium-glass p-4 flex flex-col justify-center transition-all duration-300 group">
            <div className="text-[9px] text-[#777777] uppercase font-medium tracking-widest mb-2 group-hover:text-[#a0a0a0] transition-colors">{item.label}</div>
            <div className={cn(
              "text-sm font-mono font-medium leading-tight",
              item.color === 'green' ? 'text-[var(--color-premium-green)]' :
                item.color === 'red' ? 'text-[var(--color-premium-red)]' : 'text-[#e0e0e0]'
            )}>{item.value}</div>
          </div>
        ))}
      </div>
    </div>
  )
};

export const MarketIndicesWidget = () => (
  <div className="flex flex-col h-full">
    <div className="text-[10px] text-white/20 font-bold uppercase tracking-widest mb-6">Global Indices</div>
    <div className="space-y-3">
      {[
        { name: 'S&P 500', value: '5,088.8', change: '+0.45%', isPositive: true },
        { name: 'DXY Index', value: '103.94', change: '-0.12%', isPositive: false },
        { name: 'BTC.D', value: '52.41%', change: '+0.85%', isPositive: true },
        { name: 'ETH/BTC', value: '0.0542', change: '-1.20%', isPositive: false },
      ].map((idx) => (
        <div key={idx.name} className="flex items-center justify-between p-3 bg-white/[0.02] rounded-xl border border-white/5">
          <span className="text-xs font-bold text-white/60">{idx.name}</span>
          <div className="text-right">
            <div className="text-xs font-mono font-bold">{idx.value}</div>
            <div className={cn("text-[8px] font-bold", idx.isPositive ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]')}>
              {idx.change}
            </div>
          </div>
        </div>
      ))}
    </div>
  </div>
);

export const StrategyPerformanceWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const raw = getStrategyStats(trades)
  const strategies = raw.slice(0, 5)
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Strategy Performance</h3>
          <p className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mt-1">Top setups by P&L</p>
        </div>
        <div className="w-8 h-8 rounded-lg bg-[#ffffff05] border border-[#ffffff10] flex items-center justify-center text-[#777777]">
          <BarChart2 className="w-4 h-4" />
        </div>
      </div>
      <div className="flex-1 space-y-4 overflow-y-auto custom-scrollbar pr-2">
        {strategies.length === 0 ? (
          <p className="text-sm text-white/30">Добавьте несколько сделок со стратегиями, чтобы увидеть статистику.</p>
        ) : (
          strategies.map((s, i) => (
            <div key={s.strategy} className={cn(
              "border rounded-[16px] p-4 flex items-center justify-between gap-3 group relative overflow-hidden transition-all hover:-translate-y-0.5",
              i === 0 ? "premium-glass" : "glass-panel"
            )}>
              <div>
                <div className="text-sm font-medium text-[#e0e0e0] mb-1">{s.strategy}</div>
                <div className="text-[10px] text-[#777777] uppercase tracking-widest font-medium">
                  {s.count} trades <span className="mx-1">•</span> WR <span className="text-[#a0a0a0]">{s.winRatePct}%</span>
                </div>
              </div>
              <div className={cn(
                "text-base font-mono font-medium tracking-tight bg-transparent",
                s.avgPnl >= 0 ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]",
              )}>
                {s.avgPnl >= 0 ? '+' : ''}{s.avgPnl}% <span className="text-[10px] text-[#555555] font-sans ml-1">avg</span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
};

export const EmotionProfileWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const emotions = getEmotionStats(trades)
  const total = emotions.reduce((s, e) => s + e.count, 0)
  const donutData = emotions.length
    ? emotions.map(e => ({ name: e.emotion, value: e.count }))
    : [{ name: 'Neutral', value: 1 }]
  const dominant = emotions[0]
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Emotion Profile</h3>
        <History className="w-4 h-4 text-[#777777]" />
      </div>
      <div className="flex-1 flex flex-col items-center justify-center gap-6 relative">
        <div className="w-full h-40 relative z-10">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={donutData}
                cx="50%"
                cy="50%"
                innerRadius={55}
                outerRadius={75}
                paddingAngle={4}
                dataKey="value"
                stroke="none"
              >
                {donutData.map((entry, index) => {
                  const color = index === 0 ? 'var(--color-neon-purple)' : 'rgba(255,255,255,0.1)';
                  return (
                    <Cell
                      key={`emotion-${index}`}
                      fill={color}
                      style={index === 0 ? { filter: `drop-shadow(0 0 8px ${color})` } : undefined}
                    />
                  );
                })}
              </Pie>
            </PieChart>
          </ResponsiveContainer>
        </div>
        <div className="text-center relative z-10">
          <div className="text-[10px] text-[#777777] uppercase font-medium tracking-widest mb-1.5">Dominant emotion</div>
          <div className="text-base font-medium text-[#e0e0e0]">
            {dominant ? dominant.emotion : 'Neutral'}
          </div>
          {dominant && total > 0 && (
            <div className="text-[10px] text-[#555555] mt-1 font-medium uppercase tracking-widest">
              {Math.round((dominant.count / total) * 100)}% of trades <span className="mx-1">•</span> WR <span className="text-[#a0a0a0]">{dominant.winRatePct}%</span>
            </div>
          )}
        </div>
      </div>
    </div>
  )
};

export const StreaksWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const s = getStreakStats(trades)
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest mb-6">Streaks</div>
      <div className="grid grid-cols-3 gap-4 flex-1">
        <div className="premium-glass p-4 flex flex-col justify-between group transition-transform hover:-translate-y-1">
          <div className="text-[9px] text-[#777777] uppercase tracking-widest mb-2 font-medium">Current</div>
          <div className={cn(
            "text-xl font-mono font-medium tracking-tighter",
            s.currentType === "win" ? "text-[var(--color-premium-green)]" :
              s.currentType === "loss" ? "text-[var(--color-premium-red)]" : "text-[#777777]",
          )}>
            {s.currentStreak}x
          </div>
          <div className="text-[10px] text-[#555555] font-medium mt-2">
            {s.currentType ? (s.currentType === "win" ? "wins in a row" : "losses in a row") : "no trades"}
          </div>
        </div>
        <div className="premium-glass p-4 flex flex-col justify-between group relative overflow-hidden transition-transform hover:-translate-y-1">
          <div className="text-[9px] text-[#777777] uppercase tracking-widest mb-2 font-medium relative z-10">Max win</div>
          <div className="text-xl font-mono font-medium tracking-tighter text-[var(--color-premium-green)] relative z-10">
            {s.maxWinStreak}x
          </div>
          <div className="text-[10px] text-[#555555] font-medium mt-2 relative z-10">best green streak</div>
        </div>
        <div className="premium-glass p-4 flex flex-col justify-between group relative overflow-hidden transition-transform hover:-translate-y-1">
          <div className="text-[9px] text-[#777777] uppercase tracking-widest mb-2 font-medium relative z-10">Max loss</div>
          <div className="text-xl font-mono font-medium tracking-tighter text-[var(--color-premium-red)] relative z-10">
            {s.maxLossStreak}x
          </div>
          <div className="text-[10px] text-[#555555] font-medium mt-2 relative z-10">worst red streak</div>
        </div>
      </div>
    </div>
  )
};

export const JournalQualityWidget = ({ trades = [] }: { trades?: ProTrade[] }) => {
  const q = getJournalQuality(trades)
  const items = [
    { label: 'Trades with notes', value: q.withNotesPct, color: 'bg-[var(--color-premium-green)]' },
    { label: 'Trades with screenshots', value: q.withScreenshotPct, color: 'bg-blue-500' },
  ]
  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="text-[11px] text-[#777777] font-medium uppercase tracking-widest mb-6">Journal Quality</div>
      <div className="flex-1 flex flex-col gap-5">
        {items.map((item) => (
          <div key={item.label} className="space-y-2">
            <div className="flex justify-between text-[10px] text-[#777777] font-medium uppercase tracking-widest">
              <span>{item.label}</span>
              <span className="text-[#e0e0e0] font-mono">{item.value}%</span>
            </div>
            <div className="h-1.5 w-full bg-[#111114] rounded-full overflow-hidden border border-[#ffffff05]">
              <div className={cn("h-full rounded-full", item.color)} style={{ width: `${item.value}%`, color: item.color.replace('bg-', '') }} />
            </div>
          </div>
        ))}
        <div className="mt-4 pt-4 border-t border-[#ffffff10] text-[10px] text-[#777777] font-medium uppercase tracking-widest flex justify-between items-center">
          <span>Avg note length</span>
          <span><span className="font-mono font-medium text-[#e0e0e0] text-sm">{q.avgNoteLength}</span> chars</span>
        </div>
      </div>
    </div>
  )
};

export const RecentTradesWidget = ({ trades = [], onNavigate }: { trades?: any[], onNavigate?: (view: string) => void }) => {
  const displayTrades = trades.length > 0 ? trades.slice(0, 5) : [
    { pair: 'BTC/USDT', type: 'Long', pnl: '+$420.00', positive: true, date: '2h ago' },
    { pair: 'ETH/USDT', type: 'Short', pnl: '-$120.00', positive: false, date: '5h ago' },
    { pair: 'SOL/USDT', type: 'Long', pnl: '+$850.00', positive: true, date: '1d ago' },
  ];

  return (
    <div className="flex flex-col h-full relative z-10">
      <div className="flex items-center justify-between mb-8">
        <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Recent Journal</h3>
        <button
          onClick={() => onNavigate && onNavigate('journal')}
          className="px-4 py-2 rounded-xl bg-[#0b0b0c] text-[10px] font-medium text-[#777777] uppercase tracking-widest hover:bg-[#111114] hover:text-[#a0a0a0] transition-all border border-[#ffffff0a]"
        >
          View All
        </button>
      </div>
      <div className="space-y-3 flex-1 overflow-y-auto custom-scrollbar pr-2">
        {displayTrades.map((trade, i) => (
          <div key={i} className="flex items-center justify-between p-4 premium-glass group cursor-pointer">
            <div className="flex items-start gap-4">
              <div className={cn(
                "w-1 h-10 rounded-full",
                trade.positive ? 'bg-[var(--color-premium-green)]' : 'bg-[var(--color-premium-red)]'
              )} />
              <div>
                <div className="text-sm font-medium text-[#e0e0e0] mb-1">{trade.pair}</div>
                <div className="text-[10px] text-[#777777] uppercase font-medium tracking-widest">{(trade.type === 'SHORT' ? 'Short' : 'Long')} <span className="mx-1">•</span> {trade.date || (trade as any).time}</div>
                <div className="text-[10px] text-[#60657a] mt-1">
                  {(trade.strategy || 'Manual')} {trade.notes ? `• ${String(trade.notes).slice(0, 36)}${String(trade.notes).length > 36 ? '…' : ''}` : ''}
                </div>
              </div>
            </div>
            <div className={cn(
              "text-lg font-mono font-medium tracking-tight",
              trade.positive ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]'
            )}>{trade.pnl}</div>
          </div>
        ))}
      </div>
    </div>
  );
};

export const CircularProgressWidget = () => (
  <div className="flex flex-col items-center justify-center h-full">
    <div className="w-24 h-24 rounded-full border-8 border-white/5 relative flex items-center justify-center">
      <div className="absolute inset-0 rounded-full border-8 border-white/20 border-t-transparent border-l-transparent rotate-45" />
      <div className="w-2 h-2 rounded-full bg-white absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2" />
    </div>
  </div>
);

const initialCandleData = [
  { time: '1', open: 2380.00, high: 2395.00, low: 2370.00, close: 2390.00 },
  { time: '2', open: 2390.00, high: 2410.00, low: 2385.00, close: 2405.00 },
  { time: '3', open: 2405.00, high: 2415.00, low: 2390.00, close: 2395.00 },
  { time: '4', open: 2395.00, high: 2400.00, low: 2375.00, close: 2385.00 },
  { time: '5', open: 2385.00, high: 2420.00, low: 2380.00, close: 2415.00 },
  { time: '6', open: 2415.00, high: 2435.50, low: 2410.00, close: 2428.00 },
  { time: '7', open: 2428.00, high: 2440.00, low: 2415.00, close: 2418.00 },
  { time: '8', open: 2418.00, high: 2438.00, low: 2412.00, close: 2432.00 },
  { time: '9', open: 2432.00, high: 2445.00, low: 2425.00, close: 2440.00 },
  { time: '10', open: 2440.00, high: 2455.00, low: 2435.00, close: 2450.00 },
  { time: '11', open: 2450.00, high: 2460.00, low: 2440.00, close: 2445.00 },
  { time: '12', open: 2445.00, high: 2475.00, low: 2440.00, close: 2468.00 },
  { time: '13', open: 2468.00, high: 2470.00, low: 2450.00, close: 2462.00 },
  { time: '14', open: 2462.00, high: 2490.00, low: 2455.00, close: 2485.00 },
  { time: '15', open: 2485.00, high: 2495.00, low: 2470.00, close: 2479.00 },
  { time: '16', open: 2479.00, high: 2515.00, low: 2475.00, close: 2510.00 },
  { time: '17', open: 2510.00, high: 2520.00, low: 2495.00, close: 2502.00 },
  { time: '18', open: 2502.00, high: 2540.00, low: 2500.00, close: 2535.00 },
  { time: '19', open: 2535.00, high: 2545.00, low: 2520.00, close: 2528.00 },
  { time: '20', open: 2528.00, high: 2555.00, low: 2525.00, close: 2545.96 },
];

export const TradingViewChart = () => {
  const [data, setData] = useState(initialCandleData);
  const [currentPrice, setCurrentPrice] = useState(2545.96);
  const [priceChange, setPriceChange] = useState(12.93);

  useEffect(() => {
    const interval = setInterval(() => {
      setData(prev => {
        const newData = [...prev];
        const last = { ...newData[newData.length - 1] };

        // Simulate live price movement
        const change = (Math.random() - 0.5) * 8;
        last.close = last.close + change;
        if (last.close > last.high) last.high = last.close;
        if (last.close < last.low) last.low = last.close;

        newData[newData.length - 1] = last;
        setCurrentPrice(last.close);
        setPriceChange(prevChange => prevChange + (change / 200));

        return newData;
      });
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  // SVG calculations
  const minPrice = Math.min(...data.map(d => d.low));
  const maxPrice = Math.max(...data.map(d => d.high));
  const padding = (maxPrice - minPrice) * 0.1;
  const yMin = minPrice - padding;
  const yMax = maxPrice + padding;
  const yRange = yMax - yMin;

  const getYPercent = (price: number) => 100 - ((price - yMin) / yRange) * 100;

  return (
    <div className="flex flex-col h-full relative z-10">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div className="flex items-center gap-4">
          <div>
            <div className="flex items-center gap-3 mb-1">
              <span className="text-4xl font-mono font-bold tracking-tighter text-white drop-shadow-md">
                $ {currentPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </span>
              <span className={cn(
                "text-sm font-bold px-2 py-1 rounded-lg backdrop-blur-md shadow-inner border",
                priceChange >= 0 ? "bg-[var(--color-premium-green)]/10 text-[var(--color-premium-green)] border-[var(--color-premium-green)]/20" : "bg-[var(--color-premium-red)]/10 text-[var(--color-premium-red)] border-[var(--color-premium-red)]/20"
              )}>
                {priceChange > 0 ? '+' : ''}{priceChange.toFixed(2)}%
              </span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-5 h-5 bg-gradient-to-br from-blue-400 to-blue-600 rounded-full flex items-center justify-center text-[9px] font-bold shadow-[0_0_10px_rgba(59,130,246,0.5)]">E</div>
              <span className="text-sm text-white/50 font-medium tracking-wide">Ethereum <span className="text-white/30 mx-1">•</span> ETH</span>
            </div>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <div className="flex bg-white/[0.03] p-1 rounded-2xl border border-white/5 shadow-inner">
            <button onClick={() => alert('Line chart coming soon!')} className="p-2.5 rounded-xl text-white/40 hover:text-white hover:bg-white/5 transition-all"><Activity className="w-4 h-4" /></button>
            <button onClick={() => alert('Candlestick chart active')} className="p-2.5 rounded-xl bg-white/10 text-white shadow-lg border border-white/10"><CandlestickChart className="w-4 h-4" /></button>
          </div>
          <div className="flex bg-white/[0.03] p-1 rounded-2xl border border-white/5 shadow-inner hidden sm:flex">
            <button onClick={() => alert('Market cap view coming soon!')} className="px-5 py-2 rounded-xl text-xs font-bold text-white/40 hover:text-white hover:bg-white/5 transition-all">Market cap</button>
            <button onClick={() => alert('Price view active')} className="px-5 py-2 rounded-xl bg-white/10 text-xs font-bold text-white shadow-lg border border-white/10">Price</button>
          </div>
          <div className="flex bg-white/[0.03] p-1 rounded-2xl border border-white/5 shadow-inner">
            {['1D', '2D', '7D', '1M'].map(t => (
              <button key={t} onClick={() => alert(`Timeframe ${t} selected`)} className={cn(
                "px-4 py-2 rounded-xl text-xs font-bold transition-all",
                t === '1M' ? "bg-gradient-to-br from-[var(--color-premium-green)]/20 to-[var(--color-premium-green)]/5 text-[var(--color-premium-green)] border border-[var(--color-premium-green)]/20 shadow-[0_0_10px_rgba(0,230,118,0.1)]" : "text-white/40 hover:text-white hover:bg-white/5"
              )}>{t}</button>
            ))}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 flex flex-col md:flex-row gap-6">
        {/* Sidebar Stats */}
        <div className="w-full md:w-40 flex flex-row md:flex-col gap-4 overflow-x-auto custom-scrollbar pb-2 md:pb-0 z-10">
          {[
            { label: 'Market Cap', value: '2.25 T', active: true },
            { label: 'Avg Vol (24h)', value: '53.86 M' },
            { label: 'Volume', value: '53.86 M' },
          ].map((stat) => (
            <div key={stat.label} className={cn(
              "rounded-[20px] p-5 flex flex-col gap-2 min-w-[140px] md:min-w-0 transition-all cursor-pointer relative overflow-hidden group hover:scale-[1.02]",
              stat.active ? "bg-gradient-to-br from-blue-600/20 to-transparent border border-blue-500/30 shadow-[0_0_20px_rgba(59,130,246,0.15)]" : "bg-gradient-to-br from-white/[0.04] to-transparent border border-white/5 hover:border-white/15"
            )}>
              {stat.active && <div className="absolute top-0 right-0 w-16 h-16 bg-blue-500/10 blur-xl rounded-bl-full pointer-events-none" />}
              <div className="text-[10px] text-white/50 uppercase font-bold tracking-widest relative z-10">{stat.label}</div>
              <div className={cn("text-xl font-mono font-bold tracking-tight relative z-10", stat.active && "text-blue-400 drop-shadow-[0_0_8px_rgba(96,165,250,0.5)]")}>{stat.value}</div>
            </div>
          ))}
        </div>

        {/* Chart Area */}
        <div className="flex-1 relative min-h-[300px] md:min-h-0 pr-14">
          <svg width="100%" height="100%" style={{ overflow: 'visible' }}>
            {/* Grid */}
            {[0.25, 0.5, 0.75].map(ratio => {
              const y = ratio * 100;
              const price = yMax - (yRange * ratio);
              return (
                <g key={ratio}>
                  <line x1="0" y1={`${y}%`} x2="100%" y2={`${y}%`} stroke="rgba(255,255,255,0.05)" strokeDasharray="4 4" />
                  <text x="calc(100% + 10px)" y={`${y}%`} dy="4" fill="rgba(255,255,255,0.4)" fontSize="10" fontFamily="monospace">
                    {price.toLocaleString('en-US', { maximumFractionDigits: 0 })}
                  </text>
                </g>
              );
            })}

            {/* Candles */}
            {data.map((d, i) => {
              const xCenter = ((i + 0.5) / data.length) * 100;
              const candleWidth = (0.6 / data.length) * 100;
              const xLeft = xCenter - candleWidth / 2;

              const isUp = d.close >= d.open;
              const color = isUp ? 'var(--color-premium-green)' : 'var(--color-premium-red)';

              const yHigh = getYPercent(d.high);
              const yLow = getYPercent(d.low);
              const yOpen = getYPercent(d.open);
              const yClose = getYPercent(d.close);

              const bodyTop = Math.min(yOpen, yClose);
              const bodyHeight = Math.max(Math.abs(yOpen - yClose), 0.5);

              return (
                <g key={i}>
                  <line
                    x1={`${xCenter}%`} y1={`${yHigh}%`}
                    x2={`${xCenter}%`} y2={`${yLow}%`}
                    stroke={color} strokeWidth="1.5"
                  />
                  <rect
                    x={`${xLeft}%`} y={`${bodyTop}%`}
                    width={`${candleWidth}%`} height={`${bodyHeight}%`}
                    fill={color} rx="1"
                  />
                </g>
              );
            })}

            {/* Current Price Line */}
            <line
              x1="0" y1={`${getYPercent(currentPrice)}%`}
              x2="100%" y2={`${getYPercent(currentPrice)}%`}
              stroke={currentPrice >= data[data.length - 1].open ? 'var(--color-premium-green)' : 'var(--color-premium-red)'}
              strokeDasharray="2 2"
            />
            <rect
              x="calc(100% + 5px)" y={`calc(${getYPercent(currentPrice)}% - 10px)`}
              width="56" height="20"
              fill={currentPrice >= data[data.length - 1].open ? 'var(--color-premium-green)' : 'var(--color-premium-red)'}
              rx="4"
            />
            <text
              x="calc(100% + 33px)" y={`calc(${getYPercent(currentPrice)}% + 3px)`}
              fill="#000" fontSize="10" fontWeight="bold" fontFamily="monospace" textAnchor="middle"
            >
              {currentPrice.toLocaleString('en-US', { maximumFractionDigits: 1, minimumFractionDigits: 1 })}
            </text>
          </svg>
        </div>
      </div>
    </div>
  );
};

// NEW WIDGETS

export const ExchangeWidget = () => (
  <div className="flex flex-col h-full">
    <div className="flex items-center justify-between mb-6">
      <h3 className="text-lg font-bold tracking-tight">Exchange</h3>
      <div className="flex gap-2">
        <button onClick={() => alert('Refreshing...')} className="w-6 h-6 rounded-full bg-white/5 flex items-center justify-center text-white/20">
          <RefreshCw className="w-3 h-3" />
        </button>
        <button onClick={() => alert('More options coming soon!')} className="w-6 h-6 rounded-full bg-white/5 flex items-center justify-center text-white/20">
          <MoreVertical className="w-3 h-3" />
        </button>
      </div>
    </div>
    <div className="space-y-4">
      <div className="bg-white/5 rounded-2xl p-4 border border-white/5">
        <div className="flex justify-between text-[10px] text-white/40 uppercase font-bold mb-2">
          <span>You send</span>
          <span>Balance: 2.213 BTC</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xl font-mono font-bold">0.0002568</span>
          <div className="flex items-center gap-2 bg-white/10 px-3 py-1.5 rounded-xl">
            <div className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[8px] font-bold">B</div>
            <span className="text-xs font-bold uppercase">Bitcoin</span>
            <ChevronDown className="w-3 h-3 opacity-40" />
          </div>
        </div>
      </div>
      <div className="flex justify-center -my-2 relative z-10">
        <button onClick={() => alert('Swapped!')} className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center shadow-lg shadow-blue-600/20">
          <RefreshCw className="w-4 h-4" />
        </button>
      </div>
      <div className="bg-white/5 rounded-2xl p-4 border border-white/5">
        <div className="flex justify-between text-[10px] text-white/40 uppercase font-bold mb-2">
          <span>You receive</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xl font-mono font-bold">0.06985</span>
          <div className="flex items-center gap-2 bg-white/10 px-3 py-1.5 rounded-xl">
            <div className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[8px] font-bold">E</div>
            <span className="text-xs font-bold uppercase">Ethereum</span>
            <ChevronDown className="w-3 h-3 opacity-40" />
          </div>
        </div>
      </div>
      <button onClick={() => alert('Exchange executed successfully!')} className="w-full py-4 bg-blue-600 hover:bg-blue-500 rounded-2xl text-sm font-bold transition-all shadow-lg shadow-blue-600/20 uppercase tracking-widest">
        Exchange
      </button>
    </div>
  </div>
);

export const WatchlistWidget = () => (
  <div className="flex flex-col h-full relative z-10">
    <div className="flex items-center justify-between mb-6">
      <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Watchlist</h3>
      <button onClick={() => alert('More options coming soon!')} className="w-6 h-6 rounded-md bg-[#ffffff05] hover:bg-[#ffffff10] flex items-center justify-center text-[#777777] border border-[#ffffff10] transition-colors">
        <MoreVertical className="w-3 h-3" />
      </button>
    </div>
    <div className="space-y-4 flex-1 overflow-y-auto custom-scrollbar pr-2">
      {[
        { name: 'Bitcoin', symbol: 'BTC', price: '$109,687.6', change: '+1.09%', color: 'orange' },
        { name: 'Ethereum', symbol: 'ETH', price: '$2,687.4', change: '-2.01%', color: 'blue' },
        { name: 'Solana', symbol: 'SOL', price: '$141.7', change: '+7.85%', color: 'purple' },
        { name: 'Tether', symbol: 'USDT', price: '$1.00', change: '+0.18%', color: 'green' },
      ].map((coin) => (
        <div key={coin.symbol} className="flex items-center justify-between group cursor-pointer py-2 border-b border-[#ffffff0a] last:border-0 hover:bg-[#ffffff03] px-2 -mx-2 rounded-lg transition-colors">
          <div className="flex items-center gap-3">
            <div className={cn("w-6 h-6 rounded-md flex items-center justify-center text-[10px] font-medium border border-[#ffffff0a] bg-transparent",
              coin.color === 'orange' ? 'text-orange-500' :
                coin.color === 'blue' ? 'text-blue-500' :
                  coin.color === 'purple' ? 'text-purple-500' :
                    'text-green-500'
            )}>
              {coin.symbol[0]}
            </div>
            <div>
              <div className="text-sm font-medium text-[#e0e0e0] transition-colors">{coin.name}</div>
              <div className="text-[10px] text-[#777777] font-mono uppercase">{coin.symbol}</div>
            </div>
          </div>
          <div className="text-right">
            <div className="text-sm font-mono font-medium text-[#e0e0e0]">{coin.price}</div>
            <div className={cn("text-[10px] font-medium", coin.change.startsWith('+') ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]')}>
              {coin.change}
            </div>
          </div>
        </div>
      ))}
    </div>
  </div>
);

export const MarketOverviewTable = () => (
  <div className="flex flex-col h-full relative z-10">
    <div className="flex items-center justify-between mb-6">
      <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">Market Overview</h3>
      <div className="flex gap-2">
        <button onClick={() => alert('Showing all markets')} className="px-3 py-1.5 rounded-lg bg-[#ffffff05] hover:bg-[#ffffff0a] text-[10px] font-medium uppercase tracking-widest transition-colors border border-[#ffffff10] text-[#777777]">All</button>
        <button onClick={() => alert('Search coming soon!')} className="w-8 h-8 rounded-lg bg-[#ffffff05] flex items-center justify-center text-[#777777] hover:text-white hover:bg-[#ffffff0a] transition-colors border border-[#ffffff10]">
          <Search className="w-4 h-4" />
        </button>
      </div>
    </div>
    <div className="overflow-x-auto">
      <table className="w-full text-left">
        <thead>
          <tr className="text-[10px] text-[#555555] uppercase font-medium tracking-widest border-b border-[#ffffff10]">
            <th className="pb-3 px-2 font-medium">No</th>
            <th className="pb-3 font-medium">Coin name</th>
            <th className="pb-3 font-medium">Price</th>
            <th className="pb-3 font-medium">7D%</th>
            <th className="pb-3 text-right font-medium">7D%</th>
          </tr>
        </thead>
        <tbody className="text-sm">
          {[
            { no: '#1', name: 'Bitcoin', symbol: 'BTC', price: '$102,648.00', change1: '+5.24%', change2: '-2.13%' },
            { no: '#2', name: 'Tether', symbol: 'USDT', price: '1.01', change1: '+0.18%', change2: '+0.25%' },
            { no: '#3', name: 'Ethereum', symbol: 'ETH', price: '$3,529.42', change1: '+3.92%', change2: '-1.78%' },
            { no: '#4', name: 'Solana', symbol: 'SOL', price: '$141.75', change1: '-3.44%', change2: '+7.85%' },
          ].map((row) => (
            <tr key={row.symbol} className="border-b border-[#ffffff0a] hover:bg-[#ffffff05] transition-colors group">
              <td className="py-4 text-[#777777] font-mono text-xs font-medium pl-2">{row.no}</td>
              <td className="py-4">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-lg bg-[#ffffff05] border border-[#ffffff10] flex items-center justify-center text-[10px] font-medium text-[#e0e0e0]">{row.symbol[0]}</div>
                  <span className="font-medium tracking-tight text-[#e0e0e0]">{row.name} <span className="text-[#777777] ml-2 font-mono uppercase text-[10px] tracking-widest">{row.symbol}</span></span>
                </div>
              </td>
              <td className="py-4 font-mono font-medium tracking-tight text-[#e0e0e0]">{row.price}</td>
              <td className={cn("py-4 font-medium text-sm tracking-tight", row.change1.startsWith('+') ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]')}>
                {row.change1}
              </td>
              <td className={cn("py-4 text-right font-medium text-sm tracking-tight pr-2", row.change2.startsWith('+') ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]')}>
                {row.change2}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  </div>
);

export const PortfolioCard = () => (
  <div className="flex flex-col h-full relative z-10">
    <div className="flex items-center justify-between mb-6">
      <h3 className="text-base font-medium tracking-tight text-[#e0e0e0]">My card</h3>
      <button onClick={() => alert('Add card modal coming soon!')} className="px-3 py-1.5 rounded-lg bg-[#ffffff05] text-[10px] font-medium uppercase tracking-widest hover:bg-[#ffffff10] transition-colors border border-[#ffffff10] text-[#e0e0e0]">+ Add card</button>
    </div>
    <div className="premium-glass p-6 relative overflow-hidden mb-6 group transition-all">
      <div className="flex justify-between items-start mb-8">
        <span className="text-xl font-medium italic tracking-widest text-[#e0e0e0]">VISA</span>
        <div className="w-10 h-8 bg-transparent rounded-md border border-[#ffffff20]" />
      </div>
      <div className="text-xl font-mono font-medium tracking-[0.2em] mb-8 text-[#e0e0e0]">0239 8347 9493 3424</div>
      <div className="flex justify-between items-end">
        <div>
          <div className="text-[8px] text-[#777777] uppercase font-medium mb-1">Card holder</div>
          <div className="text-sm font-medium text-[#e0e0e0]">Allen thomson</div>
        </div>
        <div className="text-right">
          <div className="text-[8px] text-[#777777] uppercase font-medium mb-1">Expiry</div>
          <div className="text-sm font-medium text-[#e0e0e0]">08/28</div>
        </div>
      </div>
    </div>
    <div className="flex items-center justify-between p-4 glass-panel">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-[#ffffff05] flex items-center justify-center border border-[#ffffff10]">
          <Wallet className="w-5 h-5 text-[#777777]" />
        </div>
        <div>
          <div className="text-lg font-mono font-medium text-[#e0e0e0]">$ 25.596 USD</div>
          <div className="text-[10px] text-[#777777] font-medium uppercase tracking-widest">All wallet balance</div>
        </div>
      </div>
      <ChevronDown className="w-4 h-4 text-[#777777]" />
    </div>
    <div className="grid grid-cols-2 gap-4 mt-4">
      <button onClick={() => alert('Transfer initiated!')} className="py-3 bg-[#ffffff05] hover:bg-[#ffffff0a] rounded-xl text-[10px] font-medium transition-colors border border-[#ffffff10] text-[#e0e0e0] uppercase tracking-widest">Transfer</button>
      <button onClick={() => alert('Withdrawal initiated!')} className="py-3 bg-[#ffffff05] hover:bg-[#ffffff0a] rounded-xl text-[10px] font-medium transition-colors border border-[#ffffff10] text-[#e0e0e0] uppercase tracking-widest">Withdraw</button>
    </div>
  </div>
);

// ─────────────────────────────────────────────
// Crypto Price Widget (CoinGecko + custom canvas chart)
// ─────────────────────────────────────────────

type CoinConfig = {
  symbol: string; // CoinGecko ID, e.g. "ethereum"
  name: string;
  ticker: string; // e.g. "ETH"
};

type Kline = { t: number; c: number };
type TooltipState = { visible: boolean; x: number; price: number; date: string };
type TvSearchResult = { symbol: string; exchange: string; description: string };

const DEFAULT_COINS: CoinConfig[] = [
  { symbol: 'ethereum', name: 'Ethereum', ticker: 'ETH' },
  { symbol: 'bitcoin', name: 'Bitcoin', ticker: 'BTC' },
  { symbol: 'solana', name: 'Solana', ticker: 'SOL' },
  { symbol: 'binancecoin', name: 'BNB', ticker: 'BNB' },
  { symbol: 'ripple', name: 'XRP', ticker: 'XRP' },
];

// TradingView ticker → CoinGecko ID
const TV_TO_CG: Record<string, string> = {
  BTC: 'bitcoin',
  ETH: 'ethereum',
  SOL: 'solana',
  BNB: 'binancecoin',
  XRP: 'ripple',
  ADA: 'cardano',
  DOT: 'polkadot',
  DOGE: 'dogecoin',
  AVAX: 'avalanche-2',
  MATIC: 'matic-network',
  LINK: 'chainlink',
  UNI: 'uniswap',
  ATOM: 'cosmos',
  LTC: 'litecoin',
  BCH: 'bitcoin-cash',
  NEAR: 'near',
  APT: 'aptos',
  ARB: 'arbitrum',
  OP: 'optimism',
  FIL: 'filecoin',
  ICP: 'internet-computer',
  VET: 'vechain',
  ALGO: 'algorand',
  XLM: 'stellar',
  HBAR: 'hedera-hashgraph',
  TRX: 'tron',
  TON: 'the-open-network',
  SHIB: 'shiba-inu',
  PEPE: 'pepe',
  WIF: 'dogwifcoin',
  BONK: 'bonk',
  SUI: 'sui',
  SEI: 'sei-network',
  TIA: 'celestia',
  INJ: 'injective-protocol',
  RENDER: 'render-token',
};

function baseTicker(s: string) {
  return s.replace(/USDT|USDC|USD|BUSD|PERP/gi, '').toUpperCase();
}

function tvIconUrl(ticker: string) {
  return `https://s3-symbol-logo.tradingview.com/crypto/XTVC${ticker.toUpperCase()}--big.svg`;
}

function formatPrice(v: number) {
  const decimals = v >= 10000 ? 0 : v >= 1000 ? 2 : v >= 1 ? 4 : 6;
  const s = v.toLocaleString('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
  const dot = s.indexOf('.');
  if (dot === -1) return { major: '$' + s, minor: '' };
  return { major: '$' + s.slice(0, dot), minor: s.slice(dot) };
}

const CG_BASE = 'https://api.coingecko.com/api/v3';

async function cgFetchPrice(id: string): Promise<{ price: number; change: number } | null> {
  try {
    const r = await fetch(
      `${CG_BASE}/simple/price?ids=${id}&vs_currencies=usd&include_24hr_change=true`,
    );
    if (!r.ok) return null;
    const d = (await r.json()) as Record<string, { usd: number; usd_24h_change: number }>;
    const info = d[id];
    if (!info) return null;
    return { price: info.usd, change: info.usd_24h_change };
  } catch {
    return null;
  }
}

async function cgFetchHistory(id: string, days = 90): Promise<Kline[]> {
  try {
    const r = await fetch(
      `${CG_BASE}/coins/${id}/market_chart?vs_currency=usd&days=${days}&interval=daily`,
    );
    if (!r.ok) return [];
    const d = (await r.json()) as { prices: [number, number][] };
    return (d.prices ?? []).map(([t, c]) => ({ t, c }));
  } catch {
    return [];
  }
}

function drawChart(canvas: HTMLCanvasElement, data: Kline[], hoverIdx: number | null) {
  const dpr = window.devicePixelRatio || 1;
  const W = canvas.offsetWidth;
  const H = canvas.offsetHeight;
  if (!W || !H) return;
  canvas.width = W * dpr;
  canvas.height = H * dpr;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

  if (!data.length) return;

  const closes = data.map((d) => d.c);
  const min = Math.min(...closes) * 0.998;
  const max = Math.max(...closes) * 1.002;
  const range = max - min || 1;

  const pL = 0;
  const pR = 18;
  const pT = 22;
  const pB = 6;
  const cW = W - pL - pR;
  const cH = H - pT - pB;

  const xOf = (i: number) => pL + (i / (data.length - 1 || 1)) * cW;
  const yOf = (v: number) => pT + (1 - (v - min) / range) * cH;

  const buildPath = () => {
    ctx.beginPath();
    ctx.moveTo(xOf(0), yOf(closes[0]));
    for (let i = 1; i < closes.length; i++) {
      const x0 = xOf(i - 1);
      const y0 = yOf(closes[i - 1]);
      const x1 = xOf(i);
      const y1 = yOf(closes[i]);
      ctx.bezierCurveTo((x0 + x1) / 2, y0, (x0 + x1) / 2, y1, x1, y1);
    }
  };

  const grad = ctx.createLinearGradient(0, pT, 0, H);
  grad.addColorStop(0, 'rgba(255,255,255,0.10)');
  grad.addColorStop(1, 'rgba(255,255,255,0.00)');
  buildPath();
  ctx.lineTo(xOf(closes.length - 1), H);
  ctx.lineTo(xOf(0), H);
  ctx.closePath();
  ctx.fillStyle = grad;
  ctx.fill();

  buildPath();
  ctx.strokeStyle = 'rgba(255,255,255,0.78)';
  ctx.lineWidth = 1.6;
  ctx.lineJoin = 'round';
  ctx.stroke();

  if (hoverIdx !== null) {
    const tx = xOf(hoverIdx);
    const ty = yOf(closes[hoverIdx]);

    ctx.beginPath();
    ctx.setLineDash([3, 5]);
    ctx.moveTo(tx, pT);
    ctx.lineTo(tx, H);
    ctx.strokeStyle = 'rgba(255,255,255,0.20)';
    ctx.lineWidth = 1;
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.beginPath();
    ctx.arc(tx, ty, 7, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(255,255,255,0.15)';
    ctx.fill();

    ctx.beginPath();
    ctx.arc(tx, ty, 3.5, 0, Math.PI * 2);
    ctx.fillStyle = '#ffffff';
    ctx.fill();
  }
}

function SearchOverlay({
  coins,
  onAdd,
  onClose,
}: {
  coins: CoinConfig[];
  onAdd: (c: CoinConfig) => void;
  onClose: () => void;
}) {
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<TvSearchResult[]>([]);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    const id = setTimeout(() => inputRef.current?.focus(), 50);
    return () => clearTimeout(id);
  }, []);

  useEffect(() => {
    const fn = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', fn);
    return () => window.removeEventListener('keydown', fn);
  }, [onClose]);

  const doSearch = useCallback(async (q: string) => {
    if (!q.trim()) {
      setResults([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const url = `https://symbol-search.tradingview.com/symbol_search/v3/?text=${encodeURIComponent(
        q,
      )}&hl=0&exchange=BINANCE&lang=en&search_type=crypto&domain=production`;
      const res = await fetch(url);
      const data = (await res.json()) as { symbols?: TvSearchResult[] };
      const seen = new Set<string>();
      const out: TvSearchResult[] = [];
      for (const s of data.symbols ?? []) {
        const sym = (s.symbol ?? '').toUpperCase();
        if (!sym.endsWith('USDT')) continue;
        if (/DOWN|UP|BEAR|BULL|3L|3S/.test(sym)) continue;
        if (!seen.has(sym) && out.length < 22) {
          seen.add(sym);
          out.push({ ...s, symbol: sym });
        }
      }
      setResults(out);
    } catch {
      setResults([]);
    } finally {
      setLoading(false);
    }
  }, [setResults]);

  const onChange = (v: string) => {
    setQuery(v);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => doSearch(v), 360);
  };

  return (
    <div
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 999,
        background: 'rgba(0,0,0,0.72)',
        backdropFilter: 'blur(12px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          width: 440,
          maxWidth: '94vw',
          background: '#0a1018',
          border: '1px solid rgba(255,255,255,0.09)',
          borderRadius: 20,
          boxShadow: '0 32px 80px rgba(0,0,0,0.85)',
          overflow: 'hidden',
          animation: 'cpw-popin 0.18s ease',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            padding: '13px 18px',
            borderBottom: '1px solid rgba(255,255,255,0.07)',
          }}
        >
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="rgba(255,255,255,0.28)"
            strokeWidth="2.5"
            strokeLinecap="round"
          >
            <circle cx="11" cy="11" r="8" />
            <path d="m21 21-4.35-4.35" />
          </svg>
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => onChange(e.target.value)}
            placeholder="Поиск: BTC, Ethereum, PEPE…"
            style={{
              flex: 1,
              background: 'none',
              border: 'none',
              outline: 'none',
              fontFamily: "'DM Mono', monospace",
              fontSize: 13,
              color: '#fff',
              letterSpacing: '0.03em',
            }}
          />
          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'rgba(255,255,255,0.3)',
              fontSize: 18,
              lineHeight: 1,
            }}
          >
            ✕
          </button>
        </div>

        <div style={{ maxHeight: 350, overflowY: 'auto' }}>
          {loading && (
            <div
              style={{
                padding: '30px 0',
                display: 'flex',
                justifyContent: 'center',
              }}
            >
              <span
                style={{
                  width: 18,
                  height: 18,
                  border: '2px solid rgba(255,255,255,0.1)',
                  borderTopColor: 'rgba(255,255,255,0.55)',
                  borderRadius: '50%',
                  animation: 'cpw-spin 0.7s linear infinite',
                  display: 'inline-block',
                }}
              />
            </div>
          )}
          {!loading && !query && (
            <p
              style={{
                padding: '28px 20px',
                textAlign: 'center',
                fontFamily: "'DM Mono', monospace",
                fontSize: 11,
                color: 'rgba(255,255,255,0.22)',
                letterSpacing: '0.04em',
              }}
            >
              Введи тикер или название монеты
            </p>
          )}
          {!loading && !!query && results.length === 0 && (
            <p
              style={{
                padding: '28px 20px',
                textAlign: 'center',
                fontFamily: "'DM Mono', monospace",
                fontSize: 11,
                color: 'rgba(255,255,255,0.22)',
              }}
            >
              Ничего не найдено
            </p>
          )}
          {!loading &&
            results.map((s) => {
              const base = baseTicker(s.symbol);
              const cgId = TV_TO_CG[base];
              const already = !!coins.find((c) => c.ticker === base);
              const coinCfg: CoinConfig = {
                symbol: cgId || base.toLowerCase(),
                name: s.description || base,
                ticker: base,
              };

              return (
                <button
                  key={s.symbol}
                  disabled={already}
                  onClick={() => !already && onAdd(coinCfg)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 12,
                    width: '100%',
                    padding: '9px 18px',
                    background: 'none',
                    border: 'none',
                    cursor: already ? 'default' : 'pointer',
                    opacity: already ? 0.36 : 1,
                    transition: 'background 0.12s',
                  }}
                  onMouseEnter={(e) => {
                    if (!already)
                      (e.currentTarget as HTMLElement).style.background =
                        'rgba(255,255,255,0.05)';
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLElement).style.background = 'none';
                  }}
                >
                  <div
                    style={{
                      width: 30,
                      height: 30,
                      borderRadius: '50%',
                      background: 'rgba(255,255,255,0.06)',
                      overflow: 'hidden',
                      flexShrink: 0,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <img
                      src={tvIconUrl(base)}
                      alt={base}
                      style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                      onError={(e) => {
                        (e.currentTarget as HTMLImageElement).style.display = 'none';
                      }}
                    />
                  </div>
                  <div
                    style={{
                      flex: 1,
                      minWidth: 0,
                      textAlign: 'left',
                    }}
                  >
                    <div
                      style={{
                        fontFamily: "'DM Mono', monospace",
                        fontSize: 12,
                        color: '#fff',
                        letterSpacing: '0.05em',
                      }}
                    >
                      {base}
                    </div>
                    <div
                      style={{
                        fontSize: 11,
                        color: 'rgba(255,255,255,0.35)',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {s.description}
                    </div>
                  </div>
                  <div
                    style={{
                      fontFamily: "'DM Mono', monospace",
                      fontSize: 9,
                      color: 'rgba(255,255,255,0.22)',
                      letterSpacing: '0.08em',
                    }}
                  >
                    {s.exchange}
                  </div>
                  <div
                    style={{
                      fontFamily: "'DM Mono', monospace",
                      fontSize: 10,
                      color: already
                        ? 'rgba(255,255,255,0.35)'
                        : 'rgba(255,255,255,0.55)',
                      background: 'rgba(255,255,255,0.07)',
                      borderRadius: 6,
                      padding: '3px 8px',
                      flexShrink: 0,
                    }}
                  >
                    {already ? '✓ добавлено' : '+ add'}
                  </div>
                </button>
              );
            })}
        </div>
      </div>
    </div>
  );
}

export const CryptoPriceWidget = () => {
  const [coins, setCoins] = useState<CoinConfig[]>(() => {
    if (typeof window === 'undefined') return DEFAULT_COINS;
    try {
      const raw = window.localStorage.getItem('cpw_v2_coins');
      const parsed = raw ? (JSON.parse(raw) as CoinConfig[]) : null;
      return Array.isArray(parsed) && parsed.length ? parsed : DEFAULT_COINS;
    } catch {
      return DEFAULT_COINS;
    }
  });

  const [active, setActive] = useState<CoinConfig>(coins[0] ?? DEFAULT_COINS[0]);
  const [price, setPrice] = useState<number | null>(null);
  const [change, setChange] = useState<number | null>(null);
  const [klines, setKlines] = useState<Kline[]>([]);
  const [chartLoading, setChartLoading] = useState(true);
  const [searchOpen, setSearchOpen] = useState(false);
  const [tooltip, setTooltip] = useState<TooltipState>({
    visible: false,
    x: 0,
    price: 0,
    date: '',
  });

  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const klinesRef = useRef<Kline[]>([]);
  const hoverRef = useRef<number | null>(null);

  useEffect(() => {
    try {
      window.localStorage.setItem('cpw_v2_coins', JSON.stringify(coins));
    } catch {
      // ignore
    }
  }, [coins]);

  const loadCoin = useCallback(async (coin: CoinConfig) => {
    setPrice(null);
    setChange(null);
    setKlines([]);
    setChartLoading(true);
    hoverRef.current = null;
    setTooltip((t) => ({ ...t, visible: false }));

    const [priceData, histData] = await Promise.all([
      cgFetchPrice(coin.symbol),
      cgFetchHistory(coin.symbol, 90),
    ]);

    setChartLoading(false);

    if (priceData) {
      setPrice(priceData.price);
      setChange(priceData.change);
    }

    if (histData.length) {
      klinesRef.current = histData;
      setKlines(histData);
    }
  }, []);

  useEffect(() => {
    loadCoin(active);
    const id = window.setInterval(() => {
      cgFetchPrice(active.symbol).then((d) => {
        if (d) {
          setPrice(d.price);
          setChange(d.change);
        }
      });
    }, 60_000);
    return () => window.clearInterval(id);
  }, [active, loadCoin]);

  const redraw = useCallback(() => {
    if (canvasRef.current) drawChart(canvasRef.current, klinesRef.current, hoverRef.current);
  }, []);

  useEffect(() => {
    redraw();
  }, [klines, redraw]);

  const onMouseMove = useCallback(
    (e: React.MouseEvent<HTMLCanvasElement>) => {
      const c = canvasRef.current;
      if (!c || !klinesRef.current.length) return;
      const rect = c.getBoundingClientRect();
      const mx = e.clientX - rect.left;
      const W = c.offsetWidth;
      const idx = Math.round((mx / (W - 18 || 1)) * (klinesRef.current.length - 1));
      const i = Math.max(0, Math.min(klinesRef.current.length - 1, idx));
      if (hoverRef.current === i) return;
      hoverRef.current = i;
      const d = klinesRef.current[i];
      const date = new Date(d.t).toLocaleDateString('ru-RU', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      });
      setTooltip({ visible: true, x: mx, price: d.c, date });
      drawChart(c, klinesRef.current, i);
    },
    [setTooltip],
  );

  const onMouseLeave = useCallback(() => {
    hoverRef.current = null;
    setTooltip((t) => ({ ...t, visible: false }));
    if (canvasRef.current) drawChart(canvasRef.current, klinesRef.current, null);
  }, []);

  const selectCoin = (c: CoinConfig) => {
    if (c.symbol === active.symbol) return;
    setActive(c);
  };

  const removeCoin = (sym: string) => {
    setCoins((prev) => {
      const next = prev.filter((c) => c.symbol !== sym);
      if (!next.length) return DEFAULT_COINS;
      if (active.symbol === sym) setActive(next[0]);
      return next;
    });
  };

  const addCoin = (c: CoinConfig) => {
    setCoins((prev) => (prev.find((x) => x.symbol === c.symbol) ? prev : [...prev, c]));
    setActive(c);
    setSearchOpen(false);
  };

  const priceDir = change === null ? 'neu' : change > 0 ? 'up' : change < 0 ? 'down' : 'neu';
  const { major, minor } =
    price !== null ? formatPrice(price) : { major: '—', minor: '' };

  const tooltipPriceStr =
    tooltip.price > 0
      ? (() => {
        const fp = formatPrice(tooltip.price);
        return fp.major + fp.minor;
      })()
      : '';

  const tooltipLeft = Math.min(Math.max(tooltip.x - 55, 8), 480 - 145);

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Syne:wght@400;600;700&family=DM+Mono:wght@300;400;500&display=swap');
        .cpw * { box-sizing: border-box; }
        @keyframes cpw-popin  { from{opacity:0;transform:scale(.95) translateY(6px)} to{opacity:1;transform:scale(1) translateY(0)} }
        @keyframes cpw-spin   { to{transform:rotate(360deg)} }
        @keyframes cpw-pulse  { 0%,80%,100%{opacity:.18;transform:scale(1)} 40%{opacity:1;transform:scale(1.4)} }
        @keyframes cpw-fadein { from{opacity:0;transform:translateY(4px)} to{opacity:1;transform:translateY(0)} }
        .cpw-price { animation: cpw-fadein 0.35s ease forwards; }
        .cpw-pill  { display:inline-flex;align-items:center;gap:6px;border-radius:100px;font-family:'DM Mono',monospace;font-size:11px;letter-spacing:.06em;cursor:pointer;transition:all .2s;padding:5px 10px 5px 13px;border:1px solid rgba(255,255,255,.09);background:rgba(255,255,255,.04);color:rgba(255,255,255,.45); }
        .cpw-pill:hover { background:rgba(255,255,255,.08);color:rgba(255,255,255,.8);border-color:rgba(255,255,255,.18); }
        .cpw-pill.active { background:rgba(255,255,255,.10);border-color:rgba(255,255,255,.28);color:#fff; }
        .cpw-rm { display:flex;align-items:center;justify-content:center;width:14px;height:14px;border-radius:50%;border:none;background:transparent;color:rgba(255,255,255,0);font-size:8px;cursor:pointer;transition:all .15s;flex-shrink:0; }
        .cpw-pill:hover .cpw-rm,.cpw-pill.active .cpw-rm { background:rgba(255,255,255,.12);color:rgba(255,255,255,.6); }
        .cpw-rm:hover { background:rgba(239,68,68,.45)!important;color:#fff!important; }
        .cpw-add { display:inline-flex;align-items:center;gap:5px;border-radius:100px;font-family:'DM Mono',monospace;font-size:11px;letter-spacing:.06em;cursor:pointer;transition:all .2s;padding:5px 13px;border:1px dashed rgba(255,255,255,.14);background:rgba(255,255,255,.02);color:rgba(255,255,255,.30); }
        .cpw-add:hover { background:rgba(255,255,255,.07);color:rgba(255,255,255,.65);border-color:rgba(255,255,255,.24); }
      `}</style>

      <div
        className="cpw"
        style={{
          fontFamily: "'Syne', sans-serif",
          display: 'flex',
          flexDirection: 'column',
          gap: 20,
          alignItems: 'flex-start',
          width: '100%',
        }}
      >
        <div
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 7,
            alignItems: 'center',
          }}
        >
          {coins.map((c) => (
            <button
              key={c.symbol}
              className={`cpw-pill${c.symbol === active.symbol ? ' active' : ''}`}
              onClick={() => selectCoin(c)}
            >
              <span>{c.ticker}</span>
              <button
                type="button"
                className="cpw-rm"
                onClick={(e) => {
                  e.stopPropagation();
                  removeCoin(c.symbol);
                }}
              >
                ✕
              </button>
            </button>
          ))}
          <button type="button" className="cpw-add" onClick={() => setSearchOpen(true)}>
            <span style={{ fontSize: 14, lineHeight: 1, marginBottom: 1 }}>+</span>
            <span>добавить</span>
          </button>
        </div>

        <div
          style={{
            position: 'relative',
            width: '100%',
            maxWidth: 480,
            height: 300,
            borderRadius: 28,
            overflow: 'hidden',
            background:
              'radial-gradient(ellipse 90% 65% at 30% 20%, #1b2637 0%, #0e1624 38%, #080e18 100%)',
            boxShadow:
              '0 0 0 1px rgba(255,255,255,0.07), 0 50px 100px rgba(0,0,0,0.9), inset 0 1px 0 rgba(255,255,255,0.11)',
          }}
        >
          <div
            style={{
              position: 'absolute',
              inset: 0,
              borderRadius: 28,
              background:
                'linear-gradient(175deg, rgba(255,255,255,0.055) 0%, transparent 45%)',
              pointerEvents: 'none',
              zIndex: 10,
            }}
          />
          <div
            style={{
              position: 'absolute',
              top: -60,
              left: '50%',
              transform: 'translateX(-50%)',
              width: 240,
              height: 110,
              background:
                'radial-gradient(ellipse, rgba(255,255,255,0.065) 0%, transparent 70%)',
              pointerEvents: 'none',
              zIndex: 11,
            }}
          />

          <div
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              padding: '22px 26px 0',
              zIndex: 6,
              pointerEvents: 'none',
            }}
          >
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                marginBottom: 15,
              }}
            >
              <div
                style={{
                  width: 28,
                  height: 28,
                  borderRadius: '50%',
                  background: 'rgba(255,255,255,0.07)',
                  overflow: 'hidden',
                  flexShrink: 0,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <img
                  src={tvIconUrl(active.ticker)}
                  alt={active.ticker}
                  style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                  onError={(e) => {
                    (e.currentTarget as HTMLImageElement).style.display = 'none';
                  }}
                />
              </div>
              <div>
                <div
                  style={{
                    fontFamily: "'Syne', sans-serif",
                    fontSize: 14,
                    fontWeight: 600,
                    color: 'rgba(255,255,255,0.92)',
                    lineHeight: 1.2,
                    letterSpacing: '0.01em',
                  }}
                >
                  {active.name}
                </div>
                <div
                  style={{
                    fontFamily: "'DM Mono', monospace",
                    fontSize: 10,
                    color: 'rgba(255,255,255,0.28)',
                    letterSpacing: '0.12em',
                  }}
                >
                  {active.ticker}
                </div>
              </div>
            </div>

            <div
              key={active.symbol + String(price)}
              className="cpw-price"
              style={{
                fontFamily: "'DM Mono', monospace",
                lineHeight: 1,
                letterSpacing: '-0.02em',
                marginBottom: 7,
              }}
            >
              <span
                style={{
                  fontSize: 34,
                  fontWeight: 400,
                  color: '#fff',
                }}
              >
                {major}
              </span>
              <span
                style={{
                  fontSize: 20,
                  color: 'rgba(255,255,255,0.40)',
                  verticalAlign: 'top',
                  display: 'inline-block',
                  marginTop: 5,
                }}
              >
                {minor}
              </span>
            </div>

            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 5,
                fontFamily: "'DM Mono', monospace",
                fontSize: 12,
                letterSpacing: '0.02em',
              }}
            >
              {priceDir === 'up' && (
                <span
                  style={{
                    display: 'inline-block',
                    width: 0,
                    height: 0,
                    borderLeft: '4px solid transparent',
                    borderRight: '4px solid transparent',
                    borderBottom: '6px solid #4ade80',
                  }}
                />
              )}
              {priceDir === 'down' && (
                <span
                  style={{
                    display: 'inline-block',
                    width: 0,
                    height: 0,
                    borderLeft: '4px solid transparent',
                    borderRight: '4px solid transparent',
                    borderTop: '6px solid #f87171',
                  }}
                />
              )}
              <span
                style={{
                  color:
                    priceDir === 'up'
                      ? '#4ade80'
                      : priceDir === 'down'
                        ? '#f87171'
                        : 'rgba(255,255,255,0.32)',
                }}
              >
                {change !== null ? `${change > 0 ? '+' : ''}${change.toFixed(2)}%` : '—'}
              </span>
              {change !== null && (
                <span style={{ color: 'rgba(255,255,255,0.22)', fontSize: 10 }}>24h</span>
              )}
            </div>
          </div>

          {tooltip.visible && (
            <div
              style={{
                position: 'absolute',
                top: 18,
                left: tooltipLeft,
                zIndex: 20,
                background: 'rgba(8,16,28,0.93)',
                border: '1px solid rgba(255,255,255,0.13)',
                borderRadius: 10,
                padding: '6px 12px',
                fontFamily: "'DM Mono', monospace",
                pointerEvents: 'none',
                backdropFilter: 'blur(10px)',
                animation: 'cpw-popin 0.12s ease',
              }}
            >
              <div
                style={{
                  fontSize: 12,
                  color: '#fff',
                  fontWeight: 500,
                }}
              >
                {tooltipPriceStr}
              </div>
              <div
                style={{
                  fontSize: 10,
                  color: 'rgba(255,255,255,0.38)',
                  marginTop: 2,
                }}
              >
                {tooltip.date}
              </div>
            </div>
          )}

          <div
            style={{
              position: 'absolute',
              bottom: 0,
              left: 0,
              right: 0,
              height: 178,
              maskImage: 'linear-gradient(to top, black 0%, black 58%, transparent 100%)',
              WebkitMaskImage:
                'linear-gradient(to top, black 0%, black 58%, transparent 100%)',
            }}
          >
            <canvas
              ref={canvasRef}
              style={{
                display: 'block',
                width: '100%',
                height: '100%',
                cursor: klines.length ? 'crosshair' : 'default',
              }}
              onMouseMove={onMouseMove}
              onMouseLeave={onMouseLeave}
            />
          </div>

          {chartLoading && (
            <div
              style={{
                position: 'absolute',
                bottom: 0,
                left: 0,
                right: 0,
                height: 178,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                zIndex: 4,
                pointerEvents: 'none',
              }}
            >
              {[0, 0.22, 0.44].map((d, i) => (
                <span
                  key={i}
                  style={{
                    display: 'block',
                    width: 4,
                    height: 4,
                    borderRadius: '50%',
                    background: 'rgba(255,255,255,0.22)',
                    margin: '0 3px',
                    animation: `cpw-pulse 1.4s ease-in-out ${d}s infinite`,
                  }}
                />
              ))}
            </div>
          )}
        </div>

        <div
          style={{
            fontFamily: "'DM Mono', monospace",
            fontSize: 10,
            color: 'rgba(255,255,255,0.14)',
            letterSpacing: '0.06em',
            marginTop: -8,
          }}
        >
          CoinGecko API · наведи на график
        </div>
      </div>

      {searchOpen && (
        <SearchOverlay coins={coins} onAdd={addCoin} onClose={() => setSearchOpen(false)} />
      )}
    </>
  );
};
