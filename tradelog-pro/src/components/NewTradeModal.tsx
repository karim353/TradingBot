import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Image as ImageIcon, Tag, Activity } from 'lucide-react';
import { cn } from '../lib/utils';

export function NewTradeModal({ isOpen, onClose, onSave }: { isOpen: boolean; onClose: () => void; onSave?: (trade: any) => void }) {
  const [direction, setDirection] = useState<'LONG' | 'SHORT'>('LONG');
  const [pair, setPair] = useState('');
  const [entry, setEntry] = useState('');
  const [exit, setExit] = useState('');
  const [notes, setNotes] = useState('');

  const handleSave = () => {
    if (!onSave) {
      onClose();
      return;
    }

    const entryNum = parseFloat(entry.replace(/,/g, ''));
    const exitNum = parseFloat(exit.replace(/,/g, ''));

    let pnlNum = 0;
    if (!isNaN(entryNum) && !isNaN(exitNum) && entryNum !== 0) {
      pnlNum = direction === 'LONG'
        ? ((exitNum - entryNum) / entryNum) * 100
        : ((entryNum - exitNum) / entryNum) * 100;
    }

    const positive = pnlNum >= 0;
    const pnlStr = `${positive ? '+' : ''}${pnlNum.toFixed(2)}%`;

    const newTrade = {
      id: Date.now(),
      pair: pair || 'UNKNOWN',
      type: direction,
      entry: entry || '0.00',
      exit: exit || '0.00',
      pnl: pnlStr,
      positive,
      date: 'Just now',
      strategy: 'Manual Entry',
      emotion: 'Neutral',
      notes: notes || 'No notes provided.',
      image: null,
      pnlNumber: pnlNum,
    };

    onSave(newTrade);

    setPair('');
    setEntry('');
    setExit('');
    setNotes('');
    setDirection('LONG');

    onClose();
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/70 backdrop-blur-md"
        />
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative w-full max-w-2xl bg-[#0a0a10]/90 backdrop-blur-2xl border border-[#ffffff0a] rounded-[24px] p-8 shadow-[0_32px_80px_rgba(0,0,0,0.8)] overflow-hidden"
        >
          {/* Neon top gradient line */}
          <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-[var(--color-neon-purple)] to-transparent opacity-60" />
          <div className="absolute top-0 left-0 w-full h-8 bg-gradient-to-b from-[var(--color-neon-purple)]/5 to-transparent pointer-events-none" />

          <div className="flex items-center justify-between mb-8 relative z-10">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[var(--color-neon-purple)]/20 to-[var(--color-neon-blue)]/20 border border-[var(--color-neon-purple)]/20 flex items-center justify-center">
                <Activity className="w-5 h-5 text-[var(--color-neon-blue)]" />
              </div>
              <div>
                <h2 className="text-2xl font-display font-medium tracking-tight text-[#e0e0e0]">New Trade</h2>
                <p className="text-xs text-[#555555] font-mono uppercase tracking-widest">Log execution details</p>
              </div>
            </div>
            <button onClick={onClose} className="p-2 text-[#555555] hover:text-white hover:bg-[#ffffff08] rounded-xl transition-colors border border-transparent hover:border-[#ffffff10]">
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="grid grid-cols-2 gap-5 mb-8 relative z-10">
            <div className="space-y-2">
              <label className="text-[10px] font-bold uppercase tracking-widest text-[#555555]">Pair</label>
              <input
                type="text"
                placeholder="BTC/USDT"
                value={pair}
                onChange={(e) => setPair(e.target.value)}
                className="w-full bg-[#ffffff04] border border-[#ffffff0a] rounded-xl px-4 py-3 text-sm font-mono text-white placeholder:text-[#444444] focus:outline-none focus:border-[var(--color-neon-purple)]/40 focus:shadow-[0_0_10px_rgba(176,38,255,0.1)] transition-all"
              />
            </div>
            <div className="space-y-2">
              <label className="text-[10px] font-bold uppercase tracking-widest text-[#555555]">Direction</label>
              <div className="flex gap-2">
                <button
                  onClick={() => setDirection('LONG')}
                  className={cn(
                    "flex-1 py-3 rounded-xl text-xs font-bold tracking-widest uppercase transition-all border",
                    direction === 'LONG' ? "bg-[var(--color-premium-green)]/10 text-[var(--color-premium-green)] border-[var(--color-premium-green)]/20 shadow-[0_0_10px_rgba(0,255,163,0.1)]" : "bg-[#ffffff04] text-[#555555] border-[#ffffff0a] hover:bg-[#ffffff08]"
                  )}
                >
                  Long
                </button>
                <button
                  onClick={() => setDirection('SHORT')}
                  className={cn(
                    "flex-1 py-3 rounded-xl text-xs font-bold tracking-widest uppercase transition-all border",
                    direction === 'SHORT' ? "bg-[var(--color-premium-red)]/10 text-[var(--color-premium-red)] border-[var(--color-premium-red)]/20 shadow-[0_0_10px_rgba(255,51,102,0.1)]" : "bg-[#ffffff04] text-[#555555] border-[#ffffff0a] hover:bg-[#ffffff08]"
                  )}
                >
                  Short
                </button>
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-[10px] font-bold uppercase tracking-widest text-[#555555]">Entry Price</label>
              <input
                type="text"
                placeholder="0.00"
                value={entry}
                onChange={(e) => setEntry(e.target.value)}
                className="w-full bg-[#ffffff04] border border-[#ffffff0a] rounded-xl px-4 py-3 text-sm font-mono text-white placeholder:text-[#444444] focus:outline-none focus:border-[var(--color-neon-purple)]/40 focus:shadow-[0_0_10px_rgba(176,38,255,0.1)] transition-all"
              />
            </div>
            <div className="space-y-2">
              <label className="text-[10px] font-bold uppercase tracking-widest text-[#555555]">Exit Price</label>
              <input
                type="text"
                placeholder="0.00"
                value={exit}
                onChange={(e) => setExit(e.target.value)}
                className="w-full bg-[#ffffff04] border border-[#ffffff0a] rounded-xl px-4 py-3 text-sm font-mono text-white placeholder:text-[#444444] focus:outline-none focus:border-[var(--color-neon-purple)]/40 focus:shadow-[0_0_10px_rgba(176,38,255,0.1)] transition-all"
              />
            </div>
            <div className="col-span-2 space-y-2">
              <label className="text-[10px] font-bold uppercase tracking-widest text-[#555555]">Notes & Analysis</label>
              <textarea
                placeholder="What was the setup? How did you feel?"
                rows={3}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="w-full bg-[#ffffff04] border border-[#ffffff0a] rounded-xl px-4 py-3 text-sm text-white placeholder:text-[#444444] focus:outline-none focus:border-[var(--color-neon-purple)]/40 focus:shadow-[0_0_10px_rgba(176,38,255,0.1)] transition-all resize-none"
              />
            </div>
          </div>

          <div className="flex items-center gap-4 mb-8 relative z-10">
            <button onClick={() => alert('Tag selection coming soon!')} className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#ffffff04] border border-[#ffffff0a] text-xs text-[#777777] hover:text-white hover:bg-[#ffffff08] transition-colors">
              <Tag className="w-4 h-4" />
              Add Tags
            </button>
            <button onClick={() => alert('Image upload coming soon!')} className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#ffffff04] border border-[#ffffff0a] text-xs text-[#777777] hover:text-white hover:bg-[#ffffff08] transition-colors">
              <ImageIcon className="w-4 h-4" />
              Attach Chart
            </button>
          </div>

          <div className="flex justify-end gap-4 relative z-10">
            <button onClick={onClose} className="px-6 py-3 rounded-xl text-sm font-medium text-[#777777] hover:text-white transition-colors hover:bg-[#ffffff05]">Cancel</button>
            <button onClick={handleSave} className="px-8 py-3 bg-gradient-to-r from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] text-white rounded-xl text-sm font-bold hover:shadow-[0_0_25px_rgba(176,38,255,0.4)] transition-all hover:scale-[1.02]">Save Trade</button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
