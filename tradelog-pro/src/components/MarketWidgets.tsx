import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { cn } from '../lib/utils';
import { Gauge, Layers, Zap } from 'lucide-react';

export function MarketSentiment() {
  const [value, setValue] = useState(72);

  useEffect(() => {
    const interval = setInterval(() => {
      setValue(prev => {
        const change = (Math.random() - 0.5) * 2;
        return Math.min(Math.max(prev + change, 0), 100);
      });
    }, 3000);
    return () => clearInterval(interval);
  }, []);

  const rotation = (value / 100) * 180 - 90;

  return (
    <div className="premium-glass p-6 relative overflow-hidden">
      <div className="text-[10px] font-bold text-[#555555] uppercase tracking-[0.2em] mb-6 flex items-center gap-2 relative z-10">
        <Gauge className="w-3 h-3 text-[var(--color-neon-blue)]" />
        Market Sentiment
      </div>

      <div className="relative h-32 flex items-center justify-center">
        <svg className="w-48 h-24" viewBox="0 0 100 50">
          <path
            d="M 10 50 A 40 40 0 0 1 90 50"
            fill="none"
            stroke="white"
            strokeWidth="2"
            strokeOpacity="0.05"
          />
          <path
            d="M 10 50 A 40 40 0 0 1 90 50"
            fill="none"
            stroke="url(#sentimentGradient)"
            strokeWidth="4"
            strokeDasharray="125.6"
            strokeDashoffset={125.6 * (1 - value / 100)}
            className="transition-all duration-1000 ease-out"
            style={{ filter: 'drop-shadow(0 0 6px rgba(0,225,255,0.4))' }}
          />
          <defs>
            <linearGradient id="sentimentGradient" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="var(--color-premium-red)" />
              <stop offset="50%" stopColor="var(--color-neon-purple)" />
              <stop offset="100%" stopColor="var(--color-premium-green)" />
            </linearGradient>
          </defs>
        </svg>

        <motion.div
          className="absolute bottom-4 left-1/2 w-1 h-16 bg-gradient-to-t from-[var(--color-neon-blue)] to-transparent origin-bottom rounded-full shadow-[0_0_8px_var(--color-neon-blue)]"
          animate={{ rotate: rotation }}
          transition={{ type: "spring", stiffness: 50 }}
          style={{ translateX: "-50%" }}
        />

        <div className="absolute bottom-0 left-1/2 -translate-x-1/2 text-center">
          <div className="text-2xl font-mono font-bold text-[#e0e0e0]">{value.toFixed(1)}%</div>
          <div className="text-[8px] text-[#555555] uppercase tracking-widest font-bold">Greed Index</div>
        </div>
      </div>
    </div>
  );
}

export function OrderBook() {
  const [orders, setOrders] = useState<{ price: number, size: number, type: 'bid' | 'ask' }[]>([]);

  useEffect(() => {
    const generateOrders = () => {
      const basePrice = 64230;
      const newOrders = [];
      for (let i = 0; i < 8; i++) {
        newOrders.push({
          price: basePrice + (Math.random() * 100),
          size: Math.random() * 2,
          type: 'ask' as const
        });
        newOrders.push({
          price: basePrice - (Math.random() * 100),
          size: Math.random() * 2,
          type: 'bid' as const
        });
      }
      setOrders(newOrders.sort((a, b) => b.price - a.price));
    };
    generateOrders();
    const interval = setInterval(generateOrders, 2000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="premium-glass p-6 flex flex-col h-full relative overflow-hidden">
      <div className="text-[10px] font-bold text-[#555555] uppercase tracking-[0.2em] mb-6 flex items-center justify-between relative z-10">
        <div className="flex items-center gap-2">
          <Layers className="w-3 h-3 text-[var(--color-neon-purple)]" />
          Order Book
        </div>
        <div className="flex items-center gap-1">
          <div className="w-1 h-1 rounded-full bg-[var(--color-premium-green)] animate-pulse shadow-[0_0_4px_var(--color-premium-green)]" />
          <span className="text-[8px] text-[#444444]">Live</span>
        </div>
      </div>

      <div className="flex-1 font-mono text-[10px] space-y-1 overflow-hidden relative z-10">
        <div className="grid grid-cols-3 text-[#444444] mb-2 font-bold uppercase tracking-widest">
          <span>Price</span>
          <span className="text-right">Size</span>
          <span className="text-right">Total</span>
        </div>
        {orders.map((order, i) => (
          <div key={i} className="grid grid-cols-3 group cursor-pointer hover:bg-[#ffffff05] py-0.5 rounded transition-colors">
            <span className={cn(
              order.type === 'ask' ? "text-[var(--color-premium-red)]" : "text-[var(--color-premium-green)]"
            )}>
              {order.price.toFixed(2)}
            </span>
            <span className="text-right text-[#888888]">{order.size.toFixed(4)}</span>
            <span className="text-right text-[#555555]">{(order.price * order.size / 1000).toFixed(2)}k</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export function MarketDepth() {
  return (
    <div className="premium-glass p-6 relative overflow-hidden h-full">
      <div className="text-[10px] font-bold text-[#555555] uppercase tracking-[0.2em] mb-6 flex items-center gap-2 relative z-10">
        <Zap className="w-3 h-3 text-[var(--color-premium-green)]" />
        Market Depth
      </div>

      <div className="h-32 flex items-end gap-1 px-2 relative z-10">
        {[...Array(20)].map((_, i) => {
          const isBid = i < 10;
          const height = isBid ? (10 - i) * 10 : (i - 9) * 10;
          return (
            <div
              key={i}
              className={cn(
                "flex-1 rounded-t-sm transition-all duration-500",
                isBid ? "bg-[var(--color-premium-green)]/20" : "bg-[var(--color-premium-red)]/20"
              )}
              style={{
                height: `${height}%`,
                boxShadow: isBid
                  ? `0 0 ${height / 5}px rgba(0,255,163,0.1)`
                  : `0 0 ${height / 5}px rgba(255,51,102,0.1)`
              }}
            />
          );
        })}
      </div>
      <div className="flex justify-between mt-2 text-[8px] font-mono text-[#444444] uppercase tracking-widest relative z-10">
        <span>Bids</span>
        <span>Asks</span>
      </div>
    </div>
  );
}
