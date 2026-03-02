import React, { useState, useEffect, useMemo } from 'react';
import { motion } from 'motion/react';
import {
  Settings,
  Zap as ZapIcon,
  X,
  GripVertical,
  Eye,
  EyeOff,
  Menu,
  Search,
  Plus
} from 'lucide-react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  rectSortingStrategy,
  useSortable
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { cn } from '../lib/utils';
import * as Widgets from './DashboardWidgets';
import { Sidebar } from './Sidebar';
import { getTradingMetrics, parsePnlNumber } from '../lib/trade-stats';

interface WidgetConfig {
  id: string;
  title: string;
  colSpan: number;
  height?: string;
  visible: boolean;
  componentName: keyof typeof Widgets | 'CombinedPrivacyHeart' | 'CombinedHeatPump';
}

const INITIAL_WIDGETS: WidgetConfig[] = [
  { id: 'equity-curve', title: 'Equity Curve', colSpan: 8, height: '420px', visible: true, componentName: 'EquityCurveWidget' },
  { id: 'monthly-pnl', title: 'Monthly P&L', colSpan: 4, height: '420px', visible: true, componentName: 'MonthlyPnLChartWidget' },
  { id: 'crypto-price', title: 'Crypto Price', colSpan: 4, height: '360px', visible: true, componentName: 'CryptoPriceWidget' },
  { id: 'trading-metrics', title: 'Core Metrics', colSpan: 4, height: '320px', visible: true, componentName: 'TradingMetricsGrid' },
  { id: 'win-loss-donut', title: 'Win Rate', colSpan: 4, height: '320px', visible: true, componentName: 'WinLossDonutWidget' },
  { id: 'long-short-perf', title: 'Long vs Short', colSpan: 4, height: '320px', visible: true, componentName: 'LongShortPerformanceWidget' },
  { id: 'asset-allocation', title: 'Asset Allocation', colSpan: 4, height: '320px', visible: true, componentName: 'AssetAllocationWidget' },
  { id: 'strategy-performance', title: 'Strategy Performance', colSpan: 6, height: '360px', visible: true, componentName: 'StrategyPerformanceWidget' },
  { id: 'emotion-profile', title: 'Emotion Profile', colSpan: 3, height: '360px', visible: true, componentName: 'EmotionProfileWidget' },
  { id: 'streaks', title: 'Streaks', colSpan: 3, height: '360px', visible: true, componentName: 'StreaksWidget' },
  { id: 'journal-quality', title: 'Journal Quality', colSpan: 4, height: '260px', visible: true, componentName: 'JournalQualityWidget' },
  { id: 'recent-trades', title: 'Recent Trades', colSpan: 4, height: '320px', visible: true, componentName: 'RecentTradesWidget' },
];

const getResponsiveColSpan = (colSpan: number) => {
  switch (colSpan) {
    case 12: return "col-span-1 md:col-span-8 xl:col-span-12";
    case 8: return "col-span-1 md:col-span-8 xl:col-span-8";
    case 6: return "col-span-1 md:col-span-4 xl:col-span-6";
    case 4: return "col-span-1 md:col-span-4 xl:col-span-4";
    case 3: return "col-span-1 md:col-span-4 xl:col-span-3";
    case 2: return "col-span-1 md:col-span-2 xl:col-span-2";
    default: return "col-span-1";
  }
};

type PeriodKey = '7D' | '30D' | '90D' | 'ALL';

const SortableWidget = ({ widget, isCustomizing, onToggle, trades, onNavigate }: { widget: WidgetConfig, isCustomizing: boolean, onToggle: (id: string) => void, trades: any[], onNavigate: (view: string) => void }) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging
  } = useSortable({ id: widget.id, disabled: !isCustomizing });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 50 : 1,
  };

  const renderComponent = () => {
    const Component = Widgets[widget.componentName as keyof typeof Widgets] as React.ComponentType<any>;

    if (!Component) {
      return (
        <div
          className={cn("glass-panel p-6 md:p-8 h-full relative overflow-hidden flex items-center justify-center text-white/40")}
          style={{ minHeight: widget.height }}
        >
          Widget "{widget.componentName}" not found
        </div>
      );
    }

    return (
      <div
        className={cn("premium-glass premium-glass-hover p-6 md:p-8 h-full relative overflow-hidden flex flex-col group")}
        style={{ minHeight: widget.height }}
      >
        <div className="absolute inset-0 bg-gradient-to-br from-white/[0.02] to-transparent pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity duration-500" />
        <Component trades={trades} onNavigate={onNavigate} />
      </div>
    );
  };

  if (!widget.visible && !isCustomizing) return null;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "relative transition-opacity duration-300",
        getResponsiveColSpan(widget.colSpan),
        !widget.visible && "opacity-40 grayscale"
      )}
    >
      {isCustomizing && (
        <div className="absolute top-4 right-4 z-50 flex gap-2">
          <button
            onClick={() => onToggle(widget.id)}
            className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white transition-colors"
          >
            {widget.visible ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
          </button>
          <div
            {...attributes}
            {...listeners}
            className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-white cursor-grab active:cursor-grabbing transition-colors"
          >
            <GripVertical className="w-4 h-4" />
          </div>
        </div>
      )}
      <div className={cn("h-full", isDragging && "opacity-50 scale-[1.02] transition-transform shadow-2xl")}>
        {renderComponent()}
      </div>
    </div>
  );
};

export function Dashboard({ onBack, onNavigate, onNewTrade, trades = [] }: { onBack: () => void; onNavigate: (view: string) => void; onNewTrade: () => void; trades?: any[] }) {
  const [widgets, setWidgets] = useState<WidgetConfig[]>(() => {
    const saved = localStorage.getItem('dashboard-widgets');
    return saved ? JSON.parse(saved) : INITIAL_WIDGETS;
  });
  const [isCustomizing, setIsCustomizing] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [period, setPeriod] = useState<PeriodKey>('30D');

  useEffect(() => {
    localStorage.setItem('dashboard-widgets', JSON.stringify(widgets));
  }, [widgets]);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      setWidgets((items) => {
        const oldIndex = items.findIndex((i) => i.id === active.id);
        const newIndex = items.findIndex((i) => i.id === over.id);
        return arrayMove(items, oldIndex, newIndex);
      });
    }
  };

  const toggleWidget = (id: string) => {
    setWidgets(prev => prev.map(w => w.id === id ? { ...w, visible: !w.visible } : w));
  };

  const resetWidgets = () => {
    setWidgets(INITIAL_WIDGETS);
  };

  const filteredTrades = useMemo(() => {
    if (period === 'ALL') return trades;
    const days = period === '7D' ? 7 : period === '30D' ? 30 : 90;
    const now = Date.now();
    const cutoff = now - days * 24 * 60 * 60 * 1000;
    return trades.filter((t: any) => {
      const raw = t?.dateIso || t?.date;
      if (!raw) return false;
      const time = new Date(raw).getTime();
      return Number.isFinite(time) && time >= cutoff;
    });
  }, [trades, period]);

  const overview = useMemo(() => {
    const m = getTradingMetrics(filteredTrades);
    const pnlSum = Math.round(filteredTrades.reduce((s: number, t: any) => s + parsePnlNumber(t), 0) * 100) / 100;
    return {
      trades: m.totalTrades,
      winRate: m.winRatePct,
      expectancy: m.expectancyPct,
      pnlSum,
    };
  }, [filteredTrades]);

  const kpiItems = [
    { label: 'Total Trades', value: String(overview.trades), tone: 'text-white' },
    { label: 'Win Rate', value: `${overview.winRate}%`, tone: 'text-white' },
    {
      label: 'Expectancy',
      value: `${overview.expectancy >= 0 ? '+' : ''}${overview.expectancy}%`,
      tone: overview.expectancy >= 0 ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]',
    },
    {
      label: 'Net P&L',
      value: `${overview.pnlSum >= 0 ? '+' : ''}${overview.pnlSum}%`,
      tone: overview.pnlSum >= 0 ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]',
    },
  ];

  const marketTape = useMemo(() => {
    const byPair = new Map<string, { pnl: number; count: number }>();
    for (const t of filteredTrades as any[]) {
      const pair = (t?.pair || 'UNKNOWN/USDT').toUpperCase();
      const cur = byPair.get(pair) ?? { pnl: 0, count: 0 };
      cur.pnl += parsePnlNumber(t);
      cur.count += 1;
      byPair.set(pair, cur);
    }
    return Array.from(byPair.entries())
      .map(([pair, v]) => ({
        pair,
        avgPnl: v.count ? Math.round((v.pnl / v.count) * 100) / 100 : 0,
      }))
      .sort((a, b) => Math.abs(b.avgPnl) - Math.abs(a.avgPnl))
      .slice(0, 5);
  }, [filteredTrades]);

  return (
    <div className="min-h-screen bg-transparent text-white p-4 md:p-6 font-sans selection:bg-white/20 overflow-x-hidden relative">
      {/* 3D Glassmorphism Background Elements */}
      <div className="fixed inset-0 grid-bg opacity-30 pointer-events-none z-0" />
      <div className="fixed inset-0 grid-bg-mask pointer-events-none z-0" />

      {/* Ambient Glows */}
      <div className="fixed top-[-10%] left-[-5%] w-[40%] h-[40%] ambient-glow ambient-glow-purple z-0" />
      <div className="fixed top-[40%] right-[-10%] w-[30%] h-[50%] ambient-glow ambient-glow-blue opacity-10 z-0" />
      <div className="fixed bottom-[-20%] left-[20%] w-[50%] h-[40%] ambient-glow ambient-glow-purple opacity-[0.08] z-0" />

      <div className="max-w-[1600px] mx-auto flex flex-col xl:flex-row gap-4 xl:gap-8 relative z-10 w-full min-h-[calc(100vh-48px)]">

        {/* Mobile Header */}
        <div className="xl:hidden flex items-center justify-between glass-panel rounded-[20px] p-4 mb-2 border-none">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-[#ffffff05] border border-[#ffffff10] rounded-xl flex items-center justify-center shadow-inner">
              <ZapIcon className="w-5 h-5 text-white/80" />
            </div>
            <span className="text-xl font-display font-medium tracking-tight text-white/90">Scope</span>
          </div>
          <button
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            className="w-10 h-10 bg-[#ffffff05] hover:bg-[#ffffff10] border border-[#ffffff10] rounded-xl flex items-center justify-center transition-colors"
          >
            {isMobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>

        {/* Sidebar Container - Adds a glass wrapper around the sidebar for consistency */}
        <div className="hidden xl:block w-[280px] shrink-0">
          <Sidebar
            currentView="dashboard"
            onNavigate={onNavigate}
            onBack={onBack}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
          />
        </div>

        {/* Mobile Sidebar Render */}
        <div className="xl:hidden">
          <Sidebar
            currentView="dashboard"
            onNavigate={onNavigate}
            onBack={onBack}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
          />
        </div>

        {/* Main Content Area */}
        <main className="flex-1 py-2 xl:py-4 flex flex-col dashboard-shell">
          {/* Top Header */}
          <header className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-4 p-6 premium-glass">
            <div className="pr-8 relative">
              <p className="text-[11px] tracking-[0.18em] uppercase text-[#7b8092] mb-2">Overview / Dashboard</p>
              <h1 className="text-3xl md:text-[36px] font-display font-medium tracking-tight text-[#e0e0e0] leading-[1]">
                Trading Dashboard
              </h1>
              <p className="text-xs text-[#777777] font-medium tracking-wide mt-2 flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-[#666a78]"></span>
                Last update: Just now
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-4 xl:gap-6">
              <div className="hidden sm:flex items-center gap-3 bg-[#ffffff05] border border-[#ffffff10] rounded-xl px-4 py-2 cursor-pointer hover:bg-[#ffffff0A] transition-colors shadow-inner">
                <Search className="w-4 h-4 text-[#777777]" />
                <span className="text-xs font-medium text-[#777777] mr-12">Search...</span>
              </div>
              <button
                onClick={onNewTrade}
                className="px-4 py-2 rounded-xl bg-[#ffffff05] border border-[#ffffff12] text-[#c7cbd7] hover:bg-[#ffffff08] transition-colors text-xs font-medium inline-flex items-center gap-2"
              >
                <Plus className="w-4 h-4" />
                New trade
              </button>
              <button
                onClick={() => setIsCustomizing(!isCustomizing)}
                className={cn(
                  "px-4 py-2 rounded-xl font-medium text-xs transition-all flex items-center gap-2 border",
                  isCustomizing
                    ? "bg-[#ffffff10] text-white border-[#ffffff30] shadow-[0_0_15px_rgba(255,255,255,0.05)]"
                    : "bg-[#ffffff03] text-[#777777] border-[#ffffff0a] hover:bg-[#ffffff08] hover:text-[#a0a0a0]"
                )}
              >
                {isCustomizing ? <X className="w-4 h-4" /> : <Settings className="w-4 h-4" />}
                <span className="hidden sm:inline">{isCustomizing ? "Finish Customizing" : "Manage widgets"}</span>
                <span className="sm:hidden">{isCustomizing ? "Finish" : "Manage"}</span>
              </button>
            </div>
          </header>

          <section className="mb-8 premium-glass p-0 overflow-hidden">
            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 px-4 md:px-6 py-3 border-b border-[#ffffff0b] bg-[#060912]/75">
              {marketTape.length === 0 ? (
                <div className="text-xs text-[#727991]">No market tape data yet</div>
              ) : (
                marketTape.map((item, idx) => (
                  <div key={item.pair} className="inline-flex items-center gap-2 text-xs">
                    <span className="text-[#aeb5cd]">{item.pair}</span>
                    <span
                      className={cn(
                        'font-mono',
                        item.avgPnl >= 0 ? 'text-[var(--color-premium-green)]' : 'text-[var(--color-premium-red)]'
                      )}
                    >
                      {item.avgPnl >= 0 ? '+' : ''}
                      {item.avgPnl}%
                    </span>
                    {idx < marketTape.length - 1 && <span className="text-[#363d52]">|</span>}
                  </div>
                ))
              )}
            </div>
          </section>

          <section className="mb-8 p-4 md:p-5 premium-glass">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
              <div className="inline-flex rounded-xl border border-[#ffffff10] bg-[#ffffff03] p-1">
                {(['7D', '30D', '90D', 'ALL'] as PeriodKey[]).map((p) => (
                  <button
                    key={p}
                    onClick={() => setPeriod(p)}
                    className={cn(
                      'px-3 py-1.5 rounded-lg text-xs font-medium transition-all',
                      period === p
                        ? 'bg-[#ffffff10] text-white'
                        : 'text-[#777777] hover:text-[#a0a7be]'
                    )}
                  >
                    {p}
                  </button>
                ))}
              </div>
              <div className="text-xs text-[#777777] inline-flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-[#5f667d]"></span>
                Showing <span className="text-[#d0d0d0]">{filteredTrades.length}</span> trades
              </div>
            </div>

            <div className="mt-4 grid grid-cols-2 md:grid-cols-4 gap-3">
              {kpiItems.map((item, idx) => (
                <div
                  key={item.label}
                  className="rounded-xl border border-[#ffffff10] bg-[#ffffff03] px-4 py-3 hover:border-[#ffffff16] transition-colors"
                >
                  <div className="text-[10px] uppercase tracking-wider text-[#6f6f6f]">{item.label}</div>
                  <div className={cn('mt-1 text-lg font-medium', item.tone)}>{item.value}</div>
                </div>
              ))}
            </div>
          </section>

          {isCustomizing && (
            <motion.div
              initial={{ opacity: 0, y: -20 }}
              animate={{ opacity: 1, y: 0 }}
              className="mb-8 p-4 md:p-6 premium-glass flex flex-col sm:flex-row sm:items-center justify-between gap-4 relative overflow-hidden border-[#ffffff20]"
            >
              <div className="relative z-10">
                <h4 className="text-sm font-medium mb-1 text-white">Customization Mode Active</h4>
                <p className="text-xs text-[#777777]">Drag widgets to reorder or use the eye icon to toggle visibility.</p>
              </div>
              <button
                onClick={resetWidgets}
                className="relative z-10 px-5 py-2.5 rounded-xl bg-[#ffffff05] hover:bg-[#ffffff0A] border border-[#ffffff10] text-[#a0a0a0] hover:text-white text-xs font-medium transition-colors whitespace-nowrap"
              >
                Reset to Default
              </button>
            </motion.div>
          )}

          {/* Bento Grid Layout */}
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={widgets.map(w => w.id)}
              strategy={rectSortingStrategy}
            >
              <div className="grid grid-cols-1 md:grid-cols-8 xl:grid-cols-12 gap-4 md:gap-6 flex-1">
                {widgets.map((widget) => (
                  <SortableWidget
                    key={widget.id}
                    widget={widget}
                    isCustomizing={isCustomizing}
                    onToggle={toggleWidget}
                    trades={filteredTrades}
                    onNavigate={onNavigate}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        </main>
      </div>
    </div>
  );
}
