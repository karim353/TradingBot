import { useState } from 'react';
import { Filter, Calendar, Activity, Search, ArrowUpRight, ArrowDownRight, Tag, ZapIcon, Menu, X } from 'lucide-react';
import { motion } from 'framer-motion';
import { cn } from '../lib/utils';
import { Sidebar } from './Sidebar';

export function Journal({ onNavigate, onNewTrade, trades }: { onNavigate: (view: string) => void; onNewTrade: () => void; trades: any[] }) {
  const [filter, setFilter] = useState('All');
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const filteredTrades = trades.filter(trade => {
    if (filter === 'All') return true;
    if (filter === 'Winning') return trade.positive;
    if (filter === 'Losing') return !trade.positive;
    if (filter === 'Breakout') return trade.strategy === 'Breakout';
    if (filter === 'Scalp') return trade.strategy === 'Scalp';
    return true;
  });

  return (
    <div className="min-h-screen bg-[#020205] text-white p-4 md:p-6 font-sans selection:bg-white/20 overflow-x-hidden relative">
      {/* Background Effects */}
      <div className="fixed inset-0 grid-bg opacity-30 pointer-events-none z-0" />
      <div className="fixed inset-0 grid-bg-mask pointer-events-none z-0" />
      <div className="fixed top-[-10%] left-[-5%] w-[40%] h-[40%] ambient-glow ambient-glow-purple z-0" />
      <div className="fixed bottom-[-10%] right-[-5%] w-[30%] h-[30%] ambient-glow ambient-glow-blue opacity-10 z-0" />

      <div className="max-w-[1600px] mx-auto flex flex-col xl:flex-row gap-4 xl:gap-8 relative z-10">

        {/* Mobile Header */}
        <div className="xl:hidden flex items-center justify-between glass-panel p-4 mb-2">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-gradient-to-br from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] rounded-xl flex items-center justify-center shadow-[0_0_15px_rgba(176,38,255,0.3)]">
              <ZapIcon className="w-5 h-5 text-white" />
            </div>
            <span className="text-xl font-display font-medium tracking-tight">Scope</span>
          </div>
          <button
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            className="w-10 h-10 bg-[#ffffff05] hover:bg-[#ffffff0A] rounded-xl flex items-center justify-center transition-colors border border-[#ffffff0a]"
          >
            {isMobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>

        <Sidebar
          currentView="journal"
          onNavigate={onNavigate}
          onBack={() => onNavigate('landing')}
          isMobileMenuOpen={isMobileMenuOpen}
          setIsMobileMenuOpen={setIsMobileMenuOpen}
        />

        {/* Main Content */}
        <main className="flex-1 py-2 xl:py-4">
          {/* Header */}
          <header className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-8 xl:mb-10 p-6 premium-glass">
            <div className="pr-8 relative">
              <h1 className="text-3xl md:text-3xl font-display font-medium tracking-tight text-[#e0e0e0] leading-tight flex items-center gap-3">
                Overview <span className="text-[#ffffff20]">/</span> <span className="text-white">Journal</span>
              </h1>
              <p className="text-xs text-[#777777] font-medium tracking-wide mt-2 flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-[#777777] animate-pulse"></span>
                Review your past executions
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-4">
              <div className="premium-glass px-4 py-2.5 flex items-center gap-3 w-full md:w-64">
                <Search className="w-4 h-4 text-[#555555]" />
                <input type="text" placeholder="Search pairs, tags..." className="bg-transparent border-none text-sm text-white placeholder:text-[#555555] focus:outline-none w-full font-mono" />
              </div>
              <button onClick={onNewTrade} className="px-6 py-3 bg-gradient-to-r from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] text-white rounded-xl font-bold text-sm transition-all flex items-center gap-2 shadow-[0_0_20px_rgba(176,38,255,0.3)] hover:shadow-[0_0_30px_rgba(176,38,255,0.5)] hover:scale-[1.02] w-full md:w-auto justify-center">
                <Activity className="w-4 h-4" />
                Log Trade
              </button>
            </div>
          </header>

          {/* Filters */}
          <div className="flex flex-wrap items-center gap-3 mb-8">
            {['All', 'Winning', 'Losing', 'Breakout', 'Scalp'].map(f => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={cn(
                  "px-5 py-2 rounded-xl text-[10px] font-bold uppercase tracking-widest transition-all border",
                  filter === f
                    ? "bg-gradient-to-r from-[var(--color-neon-purple)]/20 to-[var(--color-neon-blue)]/20 text-white border-[var(--color-neon-purple)]/30 shadow-[0_0_10px_rgba(176,38,255,0.15)]"
                    : "bg-[#ffffff03] text-[#777777] border-[#ffffff0a] hover:bg-[#ffffff08] hover:text-white"
                )}
              >
                {f}
              </button>
            ))}
            <button onClick={() => alert('Advanced filtering coming soon!')} className="md:ml-auto flex items-center gap-2 px-4 py-2 rounded-xl bg-[#ffffff03] border border-[#ffffff0a] text-xs text-[#777777] hover:text-white hover:bg-[#ffffff08] transition-colors">
              <Filter className="w-4 h-4" />
              More Filters
            </button>
          </div>

          {/* Trade Cards */}
          <div className="space-y-5">
            {filteredTrades.map((trade, i) => (
              <motion.div
                key={trade.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.08 }}
                className="premium-glass premium-glass-hover p-6 md:p-8 group"
              >
                <div className="flex flex-col md:flex-row md:items-start justify-between gap-4 mb-6">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 rounded-2xl bg-[#ffffff05] border border-[#ffffff0a] flex items-center justify-center text-xs font-bold shrink-0">
                      {trade.pair.split('/')[0]}
                    </div>
                    <div>
                      <div className="flex items-center gap-3 mb-1">
                        <h3 className="text-xl font-bold tracking-tight text-[#e0e0e0]">{trade.pair}</h3>
                        <span className={cn(
                          "px-2 py-0.5 rounded-lg text-[10px] font-bold tracking-wider uppercase border",
                          trade.type === 'LONG' ? "bg-[var(--color-premium-green)]/10 text-[var(--color-premium-green)] border-[var(--color-premium-green)]/20" : "bg-[var(--color-premium-red)]/10 text-[var(--color-premium-red)] border-[var(--color-premium-red)]/20"
                        )}>
                          {trade.type}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-[#555555] text-[10px] font-mono uppercase tracking-widest">
                        <Calendar className="w-3 h-3" />
                        {trade.date}
                      </div>
                    </div>
                  </div>
                  <div className="text-left md:text-right">
                    <div className={cn(
                      "text-2xl font-mono font-bold mb-1 flex items-center md:justify-end gap-2",
                      trade.positive ? "text-[var(--color-premium-green)]" : "text-[var(--color-premium-red)]"
                    )}>
                      {trade.positive ? <ArrowUpRight className="w-5 h-5" /> : <ArrowDownRight className="w-5 h-5" />}
                      {trade.pnl}
                    </div>
                    <div className="text-[10px] text-[#555555] uppercase tracking-widest font-bold">P&L</div>
                  </div>
                </div>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 md:gap-5 mb-6">
                  <div className="glass-panel p-4">
                    <div className="text-[10px] text-[#555555] uppercase tracking-widest font-bold mb-1">Entry</div>
                    <div className="font-mono text-sm text-[#e0e0e0]">{trade.entry}</div>
                  </div>
                  <div className="glass-panel p-4">
                    <div className="text-[10px] text-[#555555] uppercase tracking-widest font-bold mb-1">Exit</div>
                    <div className="font-mono text-sm text-[#e0e0e0]">{trade.exit}</div>
                  </div>
                  <div className="col-span-2 glass-panel p-4 flex items-center gap-2 flex-wrap">
                    <div className="text-[10px] text-[#555555] uppercase tracking-widest font-bold w-full mb-1">Tags</div>
                    <span className="px-3 py-1 bg-[var(--color-neon-blue)]/10 text-[var(--color-neon-blue)] border border-[var(--color-neon-blue)]/20 rounded-lg text-[10px] font-bold uppercase tracking-wider flex items-center gap-1">
                      <Tag className="w-3 h-3" /> {trade.strategy}
                    </span>
                    <span className={cn(
                      "px-3 py-1 rounded-lg text-[10px] font-bold uppercase tracking-wider flex items-center gap-1 border",
                      trade.emotion === 'FOMO' ? "bg-orange-500/10 text-orange-400 border-orange-500/20" : "bg-[var(--color-neon-purple)]/10 text-[var(--color-neon-purple)] border-[var(--color-neon-purple)]/20"
                    )}>
                      {trade.emotion}
                    </span>
                  </div>
                </div>

                <div className="flex flex-col md:flex-row gap-6">
                  <div className="flex-1 glass-panel p-5">
                    <div className="text-[10px] text-[#555555] uppercase tracking-widest font-bold mb-3">Trader Notes</div>
                    <p className="text-sm text-[#888888] leading-relaxed font-light italic">"{trade.notes}"</p>
                  </div>
                  {trade.image && (
                    <div onClick={() => alert('Image viewer coming soon!')} className="w-full md:w-48 h-32 md:h-24 rounded-2xl overflow-hidden border border-[#ffffff0a] relative group cursor-pointer shrink-0">
                      <img src={trade.image} alt="Chart" className="w-full h-full object-cover opacity-60 group-hover:opacity-100 transition-opacity grayscale group-hover:grayscale-0" />
                      <div className="absolute inset-0 flex items-center justify-center bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity">
                        <Search className="w-5 h-5 text-white" />
                      </div>
                    </div>
                  )}
                </div>
              </motion.div>
            ))}
          </div>
        </main>
      </div>
    </div>
  );
}
