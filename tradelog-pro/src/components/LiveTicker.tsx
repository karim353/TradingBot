import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity } from 'lucide-react';

const PAIRS = ['BTC/USDT', 'ETH/USDT', 'SOL/USDT'];

export function LiveTicker() {
  const [prices, setPrices] = useState({ 'BTC/USDT': 64230.50, 'ETH/USDT': 3450.20, 'SOL/USDT': 145.20 });
  const [ping, setPing] = useState(12);

  useEffect(() => {
    const interval = setInterval(() => {
      setPrices(prev => ({
        'BTC/USDT': prev['BTC/USDT'] * (1 + (Math.random() - 0.5) * 0.001),
        'ETH/USDT': prev['ETH/USDT'] * (1 + (Math.random() - 0.5) * 0.002),
        'SOL/USDT': prev['SOL/USDT'] * (1 + (Math.random() - 0.5) * 0.003),
      }));
      setPing(Math.floor(Math.random() * 15 + 8));
    }, 2000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="flex items-center gap-6">
      <div className="flex items-center gap-2 text-white/30 text-[10px] font-bold uppercase tracking-widest">
        <span className="w-1.5 h-1.5 rounded-full bg-[var(--color-premium-green)] animate-pulse shadow-[0_0_8px_rgba(0,230,118,0.6)]" />
        Live Data
        <span className="ml-1 text-white/20 font-mono">{ping}ms</span>
      </div>
      <div className="h-4 w-px bg-white/10" />
      <div className="flex items-center gap-6 text-xs font-mono">
        {PAIRS.map(pair => (
          <div key={pair} className="flex items-center gap-2">
            <span className="text-white/40">{pair}</span>
            <motion.span 
              key={prices[pair as keyof typeof prices]}
              initial={{ opacity: 0.5, y: -2 }}
              animate={{ opacity: 1, y: 0 }}
              className="text-white font-bold"
            >
              {prices[pair as keyof typeof prices].toFixed(2)}
            </motion.span>
          </div>
        ))}
      </div>
    </div>
  );
}
