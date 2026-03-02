import React from 'react';
import {
  LayoutDashboard,
  History,
  PieChart,
  Settings,
  LogOut,
  Activity,
  Wallet,
  CandlestickChart,
  Layers as LayersIcon,
  Zap as ZapIcon
} from 'lucide-react';
import { cn } from '../lib/utils';

interface SidebarProps {
  currentView: string;
  onNavigate: (view: string) => void;
  onBack: () => void;
  isMobileMenuOpen: boolean;
  setIsMobileMenuOpen: (open: boolean) => void;
}

export const SidebarItem = ({ icon: Icon, label, active = false, badge, dot, onClick }: { icon: any, label: string, active?: boolean, badge?: string, dot?: boolean, onClick?: () => void }) => (
  <div onClick={onClick} className={cn(
    "flex items-center gap-4 px-4 py-3 rounded-xl cursor-pointer transition-all duration-300 group relative overflow-hidden",
    active ? "bg-[#ffffff0c] text-white border border-[#ffffff14]" : "text-[#777777] hover:bg-[#ffffff05] hover:text-[#d8dbea]"
  )}>
    {active && (
      <div className="absolute left-0 top-1/2 -translate-y-1/2 h-1/2 w-1 bg-[#8f96ad] rounded-r-md" />
    )}
    <Icon className={cn("w-5 h-5 transition-colors relative z-10", active ? "text-[#d8dbea]" : "text-[#555555] group-hover:text-[#a0a0a0]")} />
    <span className={cn("text-sm tracking-wide relative z-10 transition-all", active ? "font-medium" : "font-medium")}>{label}</span>
    {badge && (
      <span className={cn(
        "ml-auto px-2 py-0.5 rounded-md text-[10px] font-bold relative z-10 border",
        badge === 'NEW' ? "bg-[var(--color-neon-purple)]/10 text-[var(--color-neon-purple)] border-[var(--color-neon-purple)]/20" : "bg-[#222222] text-[#aaaaaa] border-[#ffffff10]"
      )}>{badge}</span>
    )}
    {dot && (
      <div className="ml-auto w-1.5 h-1.5 rounded-full bg-[var(--color-neon-blue)] relative z-10 shadow-[0_0_8px_var(--color-neon-blue)]" />
    )}
  </div>
);

export function Sidebar({ currentView, onNavigate, onBack, isMobileMenuOpen, setIsMobileMenuOpen }: SidebarProps) {
  return (
    <aside className={cn(
      "xl:w-[280px] shrink-0 flex flex-col gap-4 transition-all duration-300 z-40",
      isMobileMenuOpen ? "block" : "hidden xl:flex"
    )}>
      <div className="bg-[#030308]/80 backdrop-blur-2xl border-r border-[#ffffff08] p-6 flex flex-col xl:h-screen sticky top-0 sidebar-shell">
        <div className="hidden xl:flex items-center gap-4 mb-10 flex-shrink-0 cursor-pointer group px-2" onClick={onBack}>
          <div className="w-8 h-8 bg-[#ffffff12] rounded-lg flex items-center justify-center border border-[#ffffff1f]">
            <ZapIcon className="w-4 h-4 text-white" />
          </div>
          <div>
            <span className="text-xl font-display font-medium tracking-tight text-[#e0e0e0] block">Scope</span>
            <span className="text-[10px] text-[#555555] uppercase tracking-widest font-medium">Trading Terminal</span>
          </div>
        </div>

        <div className="hidden xl:block mb-7 rounded-2xl border border-[#ffffff0c] bg-[#ffffff04] px-4 py-4">
          <div className="text-[11px] text-[#cfd4e6] font-medium leading-tight">Welcome back</div>
          <div className="text-[28px] font-display tracking-tight text-white leading-none mt-1">Trader</div>
          <div className="text-[10px] text-[#7d8296] mt-2">Last login: now</div>
        </div>

        <nav className="flex-1 space-y-1 overflow-y-auto pr-2 custom-scrollbar">
          <SidebarItem
            icon={LayoutDashboard}
            label="Dashboard"
            active={currentView === 'dashboard'}
            onClick={() => { onNavigate('dashboard'); setIsMobileMenuOpen(false); }}
          />
          <SidebarItem
            icon={History}
            label="Journal"
            active={currentView === 'journal'}
            onClick={() => { onNavigate('journal'); setIsMobileMenuOpen(false); }}
            badge="4"
          />
          <SidebarItem
            icon={CandlestickChart}
            label="Magic Charts"
            active={currentView === 'charts'}
            onClick={() => { onNavigate('charts'); setIsMobileMenuOpen(false); }}
          />
          <SidebarItem
            icon={PieChart}
            label="Analytics"
            active={currentView === 'analytics'}
            onClick={() => { onNavigate('analytics'); setIsMobileMenuOpen(false); }}
          />
          <SidebarItem
            icon={LayersIcon}
            label="Components"
            dot
            active={currentView === 'components'}
            onClick={() => { onNavigate('components'); setIsMobileMenuOpen(false); }}
          />

          <div className="pt-8 pb-4">
            <div className="text-[10px] font-bold text-white/20 uppercase tracking-[0.25em] px-5 mb-4">Market</div>
            <SidebarItem
              icon={Wallet}
              label="Portfolio"
              active={currentView === 'portfolio'}
              onClick={() => { onNavigate('portfolio'); setIsMobileMenuOpen(false); }}
              badge="NEW"
            />
            <SidebarItem
              icon={Activity}
              label="Live Feed"
              active={currentView === 'live-feed'}
              onClick={() => { onNavigate('live-feed'); setIsMobileMenuOpen(false); }}
            />
          </div>
        </nav>

        <div className="mt-auto space-y-4 pt-8 border-t border-[#ffffff08] flex-shrink-0">
          <div className="bg-[#ffffff04] rounded-xl p-1 flex items-center gap-1 border border-[#ffffff08]">
            <button onClick={() => alert('Light mode coming soon!')} className="flex-1 py-1.5 rounded-lg text-[10px] font-medium uppercase tracking-widest text-[#555555] hover:bg-[#ffffff05] transition-colors flex items-center justify-center gap-2">
              <div className="w-1.5 h-1.5 rounded-full bg-[#444444]" /> Light
            </button>
            <button className="flex-1 py-1.5 rounded-lg bg-[#ffffff10] border border-[#ffffff12] text-[10px] font-medium uppercase tracking-widest text-white flex items-center justify-center gap-2 shadow-sm">
              <div className="w-1.5 h-1.5 rounded-full bg-[#cfd4e6]" /> Dark
            </button>
          </div>
          <button
            onClick={onBack}
            className="w-full py-3 bg-transparent hover:bg-[#ffffff05] border border-transparent hover:border-[#ffffff10] rounded-xl text-sm font-medium flex items-center justify-center gap-3 transition-all group"
          >
            <span className="text-[#666666] group-hover:text-white transition-colors">Log Out</span>
            <LogOut className="w-4 h-4 text-[#444444] group-hover:text-white transition-colors" />
          </button>
        </div>
      </div>
    </aside>
  );
}
