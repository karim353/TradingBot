import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, Activity, BookOpen, LayoutDashboard, Settings, LogOut, ChevronRight } from 'lucide-react';
import { cn } from '../lib/utils';

export function CommandPalette({ isOpen, onClose, onNavigate, onNewTrade }: { isOpen: boolean; onClose: () => void; onNavigate: (view: string) => void; onNewTrade: () => void }) {
  const [query, setQuery] = useState('');

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  if (!isOpen) return null;

  const commands = [
    { id: 'new-trade', label: 'Log New Trade', icon: Activity, action: () => { onNewTrade(); onClose(); } },
    { id: 'go-dashboard', label: 'Go to Dashboard', icon: LayoutDashboard, action: () => { onNavigate('dashboard'); onClose(); } },
    { id: 'go-journal', label: 'Go to Journal', icon: BookOpen, action: () => { onNavigate('journal'); onClose(); } },
    { id: 'go-charts', label: 'Go to Magic Charts', icon: LayoutDashboard, action: () => { onNavigate('charts'); onClose(); } },
    { id: 'go-analytics', label: 'Go to Analytics', icon: LayoutDashboard, action: () => { onNavigate('analytics'); onClose(); } },
    { id: 'go-components', label: 'Go to Components', icon: LayoutDashboard, action: () => { onNavigate('components'); onClose(); } },
    { id: 'go-portfolio', label: 'Go to Portfolio', icon: LayoutDashboard, action: () => { onNavigate('portfolio'); onClose(); } },
    { id: 'go-live-feed', label: 'Go to Live Feed', icon: LayoutDashboard, action: () => { onNavigate('live-feed'); onClose(); } },
    { id: 'go-academy', label: 'Go to Academy', icon: BookOpen, action: () => { onNavigate('academy'); onClose(); } },
    { id: 'go-markets', label: 'Go to Markets', icon: LayoutDashboard, action: () => { onNavigate('markets'); onClose(); } },
    { id: 'settings', label: 'Open Settings', icon: Settings, action: () => { onNavigate('settings'); onClose(); } },
    { id: 'logout', label: 'Log Out', icon: LogOut, action: () => { onNavigate('landing'); onClose(); } },
  ];

  const filteredCommands = commands.filter(c => c.label.toLowerCase().includes(query.toLowerCase()));

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-[100] flex items-start justify-center pt-[15vh] px-4">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/70 backdrop-blur-md"
        />
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: -20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: -20 }}
          className="relative w-full max-w-xl bg-[#0a0a10]/90 backdrop-blur-2xl border border-[#ffffff0a] rounded-[20px] shadow-[0_32px_80px_rgba(0,0,0,0.8)] overflow-hidden"
        >
          {/* Top neon line */}
          <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-[var(--color-neon-blue)] to-transparent opacity-60" />

          <div className="flex items-center px-5 py-4 border-b border-[#ffffff08]">
            <Search className="w-5 h-5 text-[#555555] mr-3" />
            <input
              autoFocus
              type="text"
              placeholder="Type a command or search..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="flex-1 bg-transparent text-white placeholder:text-[#444444] focus:outline-none text-lg font-mono"
            />
            <div className="px-2 py-1 rounded-lg bg-[#ffffff05] border border-[#ffffff0a] text-[10px] text-[#555555] font-bold uppercase tracking-widest">ESC</div>
          </div>
          <div className="max-h-[300px] overflow-y-auto p-2">
            {filteredCommands.length === 0 ? (
              <div className="p-8 text-center text-[#555555] text-sm font-mono">No commands found.</div>
            ) : (
              filteredCommands.map((cmd) => (
                <button
                  key={cmd.id}
                  onClick={cmd.action}
                  className="w-full flex items-center justify-between px-4 py-3 rounded-xl hover:bg-gradient-to-r hover:from-[var(--color-neon-purple)]/5 hover:to-transparent text-left group transition-all"
                >
                  <div className="flex items-center gap-3">
                    <cmd.icon className="w-4 h-4 text-[#555555] group-hover:text-[var(--color-neon-blue)] transition-colors" />
                    <span className="text-sm font-medium text-[#aaaaaa] group-hover:text-white transition-colors">{cmd.label}</span>
                  </div>
                  <ChevronRight className="w-4 h-4 text-[#333333] group-hover:text-[#666666] transition-colors" />
                </button>
              ))
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
