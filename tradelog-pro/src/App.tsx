import { useState, useEffect } from 'react';
import { motion } from 'motion/react';
import { Search, Bell, Menu, ChevronRight, TrendingUp, BarChart2, Shield, Zap } from 'lucide-react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, PerspectiveCamera, Environment } from '@react-three/drei';
import { FloatingCrystal } from './components/3d/FloatingCrystal';
import { Dashboard } from './components/Dashboard';
import { Journal } from './components/Journal';
import { CommandPalette } from './components/CommandPalette';
import { NewTradeModal } from './components/NewTradeModal';
import { cn } from './lib/utils';
import { fetchApiTrades, apiTradeToProTrade, createTradeFromUi, ProTrade } from './lib/api';

import { PlaceholderPage } from './components/PlaceholderPage';

const INITIAL_TRADES: ProTrade[] = [
  { id: 1, pair: 'BTC/USDT', type: 'LONG', entry: '64,230.00', exit: '65,100.00', pnl: '+1.35%', positive: true, date: 'Today, 14:30', dateIso: '2025-02-20', strategy: 'Breakout', emotion: 'Confident', notes: 'Perfect setup on the 1H chart. Volume confirmation was there.', image: 'https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?auto=format&fit=crop&q=80&w=600' },
  { id: 2, pair: 'ETH/USDT', type: 'SHORT', entry: '3,450.00', exit: '3,420.00', pnl: '+0.87%', positive: true, date: 'Yesterday, 09:15', dateIso: '2025-02-19', strategy: 'Mean Reversion', emotion: 'Neutral', notes: 'Quick scalp off the daily resistance level.', image: null },
  { id: 3, pair: 'SOL/USDT', type: 'LONG', entry: '145.20', exit: '142.10', pnl: '-2.13%', positive: false, date: 'Oct 24, 11:00', dateIso: '2025-02-18', strategy: 'Trend Following', emotion: 'FOMO', notes: 'Entered too late. Should have waited for a pullback.', image: null },
];

const CLASSIC_URL =
  (typeof window !== 'undefined' && (window as any).CLASSIC_URL) ||
  (typeof import.meta !== 'undefined' && import.meta.env?.VITE_CLASSIC_URL) ||
  'http://localhost:3001';

const Navbar = ({ onGetStarted, onNavigate, onOpenCmd }: { onGetStarted: () => void, onNavigate: (view: string) => void, onOpenCmd: () => void }) => (
  <nav className="fixed top-0 left-0 right-0 z-50 flex items-center justify-between px-8 py-5 bg-[#020205]/60 backdrop-blur-2xl border-b border-[#ffffff08]">
    <div className="flex items-center gap-2">
      <div className="w-8 h-8 bg-gradient-to-br from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] rounded-lg flex items-center justify-center shadow-[0_0_12px_rgba(176,38,255,0.3)]">
        <TrendingUp className="w-5 h-5 text-white" />
      </div>
      <span className="text-xl font-display font-bold tracking-tight">TradeLog Pro</span>
    </div>

    <div className="hidden md:flex items-center gap-8 text-sm font-medium text-[#666666]">
      <button onClick={() => onNavigate('markets')} className="hover:text-white transition-colors">Markets</button>
      <button onClick={() => onNavigate('journal')} className="hover:text-white transition-colors">Journal</button>
      <button onClick={() => onNavigate('analytics')} className="hover:text-white transition-colors">Analytics</button>
      <button onClick={() => onNavigate('academy')} className="hover:text-white transition-colors">Academy</button>
    </div>

    <div className="flex items-center gap-4">
      <button onClick={onOpenCmd} className="p-2 text-[#666666] hover:text-white transition-colors">
        <Search className="w-5 h-5" />
      </button>
      <button onClick={() => alert('Notifications coming soon!')} className="p-2 text-[#666666] hover:text-white transition-colors relative">
        <Bell className="w-5 h-5" />
        <span className="absolute top-2 right-2 w-2 h-2 bg-[var(--color-neon-purple)] rounded-full border-2 border-[#020205] shadow-[0_0_6px_var(--color-neon-purple)]" />
      </button>
      <button
        onClick={onGetStarted}
        className="hidden md:block px-6 py-2.5 bg-gradient-to-r from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] text-white rounded-full text-sm font-semibold transition-all shadow-[0_0_20px_rgba(176,38,255,0.3)] hover:shadow-[0_0_30px_rgba(176,38,255,0.5)] hover:scale-[1.02]"
      >
        Get Started
      </button>
      <button
        onClick={() => (window.location.href = CLASSIC_URL)}
        className="hidden md:block px-4 py-2 rounded-full border border-[#ffffff15] text-xs font-semibold text-[#888888] hover:bg-[#ffffff08] transition-colors"
      >
        Classic UI
      </button>
      <button onClick={() => alert('Mobile menu coming soon!')} className="md:hidden p-2 text-[#666666]">
        <Menu className="w-6 h-6" />
      </button>
    </div>
  </nav>
);

const Hero = ({ onGetStarted }: { onGetStarted: () => void }) => (
  <section className="relative min-h-screen flex flex-col items-center justify-center pt-20 overflow-hidden noise">
    {/* Grid Background */}
    <div className="absolute inset-0 z-0">
      <div className="absolute inset-0 grid-bg opacity-30" />
      <div className="absolute inset-0 grid-bg-mask" />
      <div className="absolute inset-0 bg-gradient-to-b from-[#020205] via-transparent to-[#020205]" />
      <div className="absolute inset-0 bg-gradient-to-r from-[#020205] via-transparent to-[#020205]" />

      {/* Glass chain decorative element */}
      <div className="pointer-events-none absolute -left-40 top-1/3 w-[520px] opacity-50 mix-blend-screen">
        <img
          src="/glass-chain.png"
          alt=""
          className="w-full h-auto rotate-[-12deg] drop-shadow-[0_40px_120px_rgba(0,0,0,0.8)]"
          loading="lazy"
        />
      </div>

      {/* Glass knot decorative element */}
      <div className="pointer-events-none absolute -right-24 top-10 w-[260px] opacity-70 mix-blend-screen">
        <img
          src="/glass-knot.png"
          alt=""
          className="w-full h-auto drop-shadow-[0_40px_120px_rgba(0,0,0,0.85)]"
          loading="lazy"
        />
      </div>

      {/* Dynamic Spotlight */}
      <motion.div
        animate={{
          x: [0, 100, -100, 0],
          y: [0, -50, 50, 0]
        }}
        transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
        className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[60%] h-[60%] bg-white/[0.02] rounded-full blur-[160px]"
      />
    </div>

    <div className="container mx-auto px-6 grid lg:grid-cols-2 gap-12 items-center relative z-10">
      <motion.div
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 1, ease: "easeOut" }}
        className="max-w-2xl"
      >
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/[0.03] border border-white/[0.05] text-white/60 text-xs font-medium mb-8 uppercase tracking-widest">
          <span className="w-1.5 h-1.5 rounded-full bg-[var(--color-premium-green)] animate-pulse" />
          TradeLog Pro v2.0
        </div>
        <h1 className="text-6xl lg:text-8xl font-display font-light mb-6 leading-[1.1] tracking-tighter">
          Master Your <br />
          <span className="font-bold text-gradient">Trading Edge.</span>
        </h1>
        <p className="text-lg text-white/40 mb-10 leading-relaxed font-light max-w-xl">
          The ultimate journaling platform for professional traders. Analyze performance, manage risk, and execute with precision.
        </p>
        <div className="flex flex-wrap gap-4">
          <button
            onClick={onGetStarted}
            className="px-8 py-4 bg-white text-black rounded-full font-bold flex items-center gap-2 hover:bg-white/90 transition-all group relative overflow-hidden"
          >
            <span className="relative z-10">Start Journaling</span>
            <ChevronRight className="w-5 h-5 group-hover:translate-x-1 transition-transform relative z-10" />
          </button>
          <button onClick={() => alert('Demo video coming soon!')} className="px-8 py-4 bg-transparent border border-white/20 rounded-full font-bold hover:bg-white/5 transition-all">
            View Demo
          </button>
        </div>

        <div className="mt-16 flex items-center gap-8">
          <div>
            <div className="text-2xl font-mono font-bold">$1.2B+</div>
            <div className="text-[10px] text-white/40 uppercase tracking-widest mt-1">Volume Tracked</div>
          </div>
          <div className="w-px h-8 bg-white/10" />
          <div>
            <div className="text-2xl font-mono font-bold">50k+</div>
            <div className="text-[10px] text-white/40 uppercase tracking-widest mt-1">Active Traders</div>
          </div>
          <div className="w-px h-8 bg-white/10" />
          <div>
            <div className="text-2xl font-mono font-bold">99.9%</div>
            <div className="text-[10px] text-white/40 uppercase tracking-widest mt-1">Uptime</div>
          </div>
        </div>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 1, delay: 0.2 }}
        className="h-[600px] relative"
      >
        <Canvas>
          <PerspectiveCamera makeDefault position={[0, 0, 8]} />
          <ambientLight intensity={0.5} />
          <pointLight position={[10, 10, 10]} intensity={1} />
          <spotLight position={[-10, 10, 10]} angle={0.15} penumbra={1} intensity={1} />
          <FloatingCrystal />
          <Environment preset="city" />
          <OrbitControls enableZoom={false} autoRotate autoRotateSpeed={0.5} />
        </Canvas>

        {/* Floating UI elements to mimic the example */}
        <motion.div
          animate={{ y: [0, -10, 0] }}
          transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
          className="absolute top-1/4 right-0 glass p-4 rounded-2xl shadow-2xl max-w-[200px]"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-full bg-emerald-500/20 flex items-center justify-center">
              <Shield className="w-4 h-4 text-emerald-400" />
            </div>
            <div className="text-xs font-semibold">Secure Trading</div>
          </div>
          <div className="text-[10px] text-white/40">End-to-end encryption for all your trade data.</div>
        </motion.div>

        <motion.div
          animate={{ y: [0, 10, 0] }}
          transition={{ duration: 5, repeat: Infinity, ease: "easeInOut", delay: 1 }}
          className="absolute bottom-1/4 left-0 glass p-4 rounded-2xl shadow-2xl max-w-[200px]"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="w-8 h-8 rounded-full bg-indigo-500/20 flex items-center justify-center">
              <Zap className="w-4 h-4 text-indigo-400" />
            </div>
            <div className="text-xs font-semibold">Real-time Insights</div>
          </div>
          <div className="text-[10px] text-white/40">Instant feedback on your trading strategy performance.</div>
        </motion.div>
      </motion.div>
    </div>
  </section>
);

const Features = () => {
  const features = [
    {
      icon: <BarChart2 className="w-6 h-6" />,
      title: "Advanced Analytics",
      description: "Deep dive into your trading stats with customizable dashboards and reports."
    },
    {
      icon: <TrendingUp className="w-6 h-6" />,
      title: "Performance Tracking",
      description: "Monitor your P&L, win rate, and risk-to-reward ratio in real-time."
    },
    {
      icon: <Shield className="w-6 h-6" />,
      title: "Risk Management",
      description: "Set rules and get alerts when you're over-leveraged or breaking your plan."
    }
  ];

  return (
    <section className="py-24 relative overflow-hidden">
      <div className="container mx-auto px-6">
        <div className="grid md:grid-cols-3 gap-8 mb-24">
          {features.map((f, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.1 }}
              whileHover={{ y: -5 }}
              className="p-8 rounded-3xl premium-glass premium-glass-hover group"
            >
              <div className="w-12 h-12 bg-gradient-to-br from-[var(--color-neon-purple)]/20 to-[var(--color-neon-blue)]/20 rounded-xl flex items-center justify-center text-[var(--color-neon-blue)] mb-6 group-hover:scale-110 transition-transform border border-[var(--color-neon-purple)]/20">
                {f.icon}
              </div>
              <h3 className="text-xl font-bold mb-4">{f.title}</h3>
              <p className="text-white/40 leading-relaxed">{f.description}</p>
            </motion.div>
          ))}
        </div>

        <div className="grid lg:grid-cols-2 gap-16 items-center">
          <motion.div
            initial={{ opacity: 0, x: -50 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
          >
            <h2 className="text-4xl lg:text-5xl font-display font-bold mb-8 leading-tight">
              Visualize Your <br />
              <span className="text-gradient">Trading Journey</span>
            </h2>
            <p className="text-white/50 mb-8 text-lg">
              Our intuitive interface turns complex data into actionable insights. See exactly where you're winning and where you need to improve.
            </p>
            <ul className="space-y-4">
              {[
                "Automatic trade synchronization",
                "Equity curve visualization",
                "Psychological state tracking",
                "Custom strategy tagging"
              ].map((item, i) => (
                <li key={i} className="flex items-center gap-3 text-white/70">
                  <div className="w-5 h-5 rounded-full bg-[var(--color-neon-purple)]/20 flex items-center justify-center">
                    <Zap className="w-3 h-3 text-[var(--color-neon-blue)]" />
                  </div>
                  {item}
                </li>
              ))}
            </ul>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 50 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            className="relative"
          >
            <div className="aspect-video rounded-3xl overflow-hidden border border-white/10 glass relative group">
              <img
                src="https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?auto=format&fit=crop&q=80&w=1200"
                alt="Crypto Chart"
                className="w-full h-full object-cover opacity-60 group-hover:scale-105 transition-transform duration-1000 grayscale group-hover:grayscale-0"
                referrerPolicy="no-referrer"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#000000] via-[#000000]/50 to-transparent" />
              <div className="absolute bottom-6 left-6 right-6">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-[10px] uppercase tracking-[0.2em] font-bold text-white/40 mb-1">Live Market Analysis</div>
                    <div className="text-2xl font-mono font-bold text-white">BTC/USDT <span className="text-[var(--color-premium-green)]">+4.2%</span></div>
                  </div>
                  <div className="px-3 py-1 rounded-full border border-[var(--color-premium-green)]/30 text-[var(--color-premium-green)] text-[10px] font-bold uppercase tracking-wider">
                    Real-time
                  </div>
                </div>
              </div>
            </div>

            {/* Decorative elements */}
            <div className="absolute -top-6 -right-6 w-32 h-32 bg-indigo-500/20 rounded-full blur-3xl" />
            <div className="absolute -bottom-6 -left-6 w-32 h-32 bg-purple-500/20 rounded-full blur-3xl" />
          </motion.div>
        </div>
      </div>
    </section>
  );
};

const CTA = ({ onGetStarted }: { onGetStarted: () => void }) => (
  <section className="py-24 relative overflow-hidden">
    <div className="absolute inset-0 z-0">
      <img
        src="https://picsum.photos/seed/cta-bg/1920/1080?blur=5"
        alt="CTA Background"
        className="w-full h-full object-cover opacity-10"
        referrerPolicy="no-referrer"
      />
      <div className="absolute inset-0 bg-gradient-to-t from-[#050505] via-transparent to-[#050505]" />
    </div>

    <div className="container mx-auto px-6 relative z-10">
      <div className="max-w-4xl mx-auto glass p-12 lg:p-20 rounded-[40px] text-center relative overflow-hidden">
        <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-transparent via-indigo-500 to-transparent" />

        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
        >
          <h2 className="text-4xl lg:text-6xl font-display font-bold mb-8">
            Ready to Elevate Your <br />
            <span className="text-gradient">Trading Strategy?</span>
          </h2>
          <p className="text-white/50 mb-12 text-lg max-w-2xl mx-auto">
            Join thousands of professional traders who use TradeLog Pro to gain a competitive edge in the markets.
          </p>
          <button
            onClick={onGetStarted}
            className="px-12 py-5 bg-gradient-to-r from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] text-white rounded-full font-bold text-lg transition-all shadow-[0_0_30px_rgba(176,38,255,0.4)] hover:shadow-[0_0_50px_rgba(176,38,255,0.6)] hover:scale-105 active:scale-95"
          >
            Get Started for Free
          </button>
        </motion.div>
      </div>
    </div>
  </section>
);

const Marquee = () => (
  <div className="py-12 border-y border-white/5 bg-white/[0.01] overflow-hidden">
    <div className="flex whitespace-nowrap animate-marquee">
      {[
        "BINANCE", "COINBASE", "BYBIT", "KRAKEN", "METAMASK", "TRADINGVIEW", "OKX", "KUCOIN"
      ].map((brand, i) => (
        <div key={i} className="flex items-center mx-12">
          <span className="text-2xl font-display font-black text-white/10 hover:text-white/30 transition-colors cursor-default tracking-tighter">
            {brand}
          </span>
        </div>
      ))}
      {/* Duplicate for seamless loop */}
      {[
        "BINANCE", "COINBASE", "BYBIT", "KRAKEN", "METAMASK", "TRADINGVIEW", "OKX", "KUCOIN"
      ].map((brand, i) => (
        <div key={i + 8} className="flex items-center mx-12">
          <span className="text-2xl font-display font-black text-white/10 hover:text-white/30 transition-colors cursor-default tracking-tighter">
            {brand}
          </span>
        </div>
      ))}
    </div>
  </div>
);

const Testimonials = () => (
  <section className="py-24 relative overflow-hidden bg-white/[0.01]">
    <div className="container mx-auto px-6">
      <div className="text-center mb-16">
        <h2 className="text-4xl font-display font-bold mb-4">Trusted by Professionals</h2>
        <p className="text-white/40">Join the elite circle of traders who have mastered their edge.</p>
      </div>
      <div className="grid md:grid-cols-3 gap-8">
        {[
          {
            name: "Marcus Chen",
            role: "Hedge Fund Trader",
            content: "TradeLog Pro transformed my post-trade analysis. The insights into my psychological state are invaluable.",
            avatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&q=80&w=100&h=100"
          },
          {
            name: "Sarah Jenkins",
            role: "Crypto Specialist",
            content: "The cleanest interface I've ever used. It makes the tedious task of journaling actually enjoyable.",
            avatar: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&q=80&w=100&h=100"
          },
          {
            name: "David Miller",
            role: "Day Trader",
            content: "The risk management alerts saved me from several emotional trades this month. Worth every penny.",
            avatar: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&q=80&w=100&h=100"
          }
        ].map((t, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ delay: i * 0.1 }}
            className="premium-glass p-8 relative group"
          >
            <div className="flex items-center gap-4 mb-6">
              <img src={t.avatar} alt={t.name} className="w-12 h-12 rounded-full object-cover border border-white/10" />
              <div>
                <div className="font-bold text-sm">{t.name}</div>
                <div className="text-xs text-white/40">{t.role}</div>
              </div>
            </div>
            <p className="text-white/60 text-sm leading-relaxed italic">"{t.content}"</p>
          </motion.div>
        ))}
      </div>
    </div>
  </section>
);

const Pricing = ({ onGetStarted }: { onGetStarted: () => void }) => (
  <section className="py-24 relative overflow-hidden">
    <div className="container mx-auto px-6">
      <div className="text-center mb-16">
        <h2 className="text-4xl font-display font-bold mb-4">Simple, Transparent Pricing</h2>
        <p className="text-white/40">Choose the plan that fits your trading volume.</p>
      </div>
      <div className="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
        {[
          {
            title: "Starter",
            price: "Free",
            features: ["Up to 50 trades/mo", "Basic Analytics", "Manual Sync", "Community Support"],
            cta: "Get Started",
            popular: false
          },
          {
            title: "Pro",
            price: "$29",
            features: ["Unlimited trades", "Advanced AI Insights", "Auto-Sync (API)", "Priority Support", "Custom Strategy Tags"],
            cta: "Start Free Trial",
            popular: true
          },
          {
            title: "Elite",
            price: "$99",
            features: ["Multiple Accounts", "Portfolio Correlation", "1-on-1 Coaching", "Early Access Features", "White-label Reports"],
            cta: "Contact Sales",
            popular: false
          }
        ].map((plan, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ delay: i * 0.1 }}
            className={cn(
              "premium-glass p-10 relative flex flex-col",
              plan.popular && "border-[var(--color-neon-purple)]/30 shadow-[0_0_40px_rgba(176,38,255,0.1)] scale-105 z-10"
            )}
          >
            {plan.popular && (
              <div className="absolute -top-4 left-1/2 -translate-x-1/2 px-4 py-1 bg-white text-black rounded-full text-[10px] font-bold uppercase tracking-widest">
                Most Popular
              </div>
            )}
            <div className="mb-8">
              <h3 className="text-xl font-bold mb-2">{plan.title}</h3>
              <div className="flex items-baseline gap-1">
                <span className="text-4xl font-mono font-bold">{plan.price}</span>
                {plan.price !== "Free" && <span className="text-white/40 text-sm">/mo</span>}
              </div>
            </div>
            <ul className="space-y-4 mb-10 flex-1">
              {plan.features.map((f, j) => (
                <li key={j} className="flex items-center gap-3 text-sm text-white/60">
                  <Zap className="w-4 h-4 text-white/40" />
                  {f}
                </li>
              ))}
            </ul>
            <button
              onClick={onGetStarted}
              className={cn(
                "w-full py-4 rounded-full font-bold transition-all",
                plan.popular ? "bg-white hover:bg-white/90 text-black" : "bg-white/5 hover:bg-white/10 text-white"
              )}
            >
              {plan.cta}
            </button>
          </motion.div>
        ))}
      </div>
    </div>
  </section>
);

export default function App() {
  const [view, setView] = useState<'landing' | 'dashboard' | 'journal' | 'charts' | 'analytics' | 'components' | 'portfolio' | 'live-feed' | 'academy' | 'markets' | 'settings'>('landing');
  const [isCmdOpen, setIsCmdOpen] = useState(false);
  const [isNewTradeOpen, setIsNewTradeOpen] = useState(false);
  const [trades, setTrades] = useState<ProTrade[]>(() => {
    const saved = localStorage.getItem('trades');
    return saved ? JSON.parse(saved) : INITIAL_TRADES;
  });
  const [loading, setLoading] = useState(false);

  const handleNavigate = (v: string) => {
    setView(v as any);
  };

  const handleAddTrade = async (trade: any) => {
    try {
      const pnlNumber =
        typeof trade.pnlNumber === 'number'
          ? trade.pnlNumber
          : typeof trade.pnl === 'string' && trade.pnl.endsWith('%')
            ? parseFloat(trade.pnl.replace('%', ''))
            : 0;

      const created = await createTradeFromUi({
        pair: trade.pair,
        type: trade.type,
        entry: trade.entry,
        exit: trade.exit,
        pnlNumber,
        notes: trade.notes,
      });

      const newTrades = [created, ...trades];
      setTrades(newTrades);
      localStorage.setItem('trades', JSON.stringify(newTrades));
    } catch (err) {
      console.error('Failed to create trade via API, falling back to local only.', err);
      const newTrades = [trade, ...trades];
      setTrades(newTrades);
      localStorage.setItem('trades', JSON.stringify(newTrades));
    }
  };

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const apiTrades = await fetchApiTrades();
        const mapped = apiTrades.map(apiTradeToProTrade);
        setTrades(mapped);
        localStorage.setItem('trades', JSON.stringify(mapped));
      } catch (err) {
        console.warn('Failed to load trades from API, using local/initial data.', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setIsCmdOpen(prev => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  return (
    <>
      <CommandPalette
        isOpen={isCmdOpen}
        onClose={() => setIsCmdOpen(false)}
        onNavigate={handleNavigate}
        onNewTrade={() => setIsNewTradeOpen(true)}
      />
      <NewTradeModal
        isOpen={isNewTradeOpen}
        onClose={() => setIsNewTradeOpen(false)}
        onSave={handleAddTrade}
      />

      {view === 'dashboard' ? (
        <Dashboard
          onBack={() => setView('landing')}
          onNavigate={handleNavigate}
          onNewTrade={() => setIsNewTradeOpen(true)}
          trades={trades}
        />
      ) : view === 'journal' ? (
        <Journal
          onNavigate={handleNavigate}
          onNewTrade={() => setIsNewTradeOpen(true)}
          trades={trades}
        />
      ) : view === 'charts' ? (
        <PlaceholderPage title="Magic Charts" description="Advanced Charting Tools" currentView="charts" onNavigate={handleNavigate} />
      ) : view === 'analytics' ? (
        <PlaceholderPage title="Analytics" description="Deep Performance Insights" currentView="analytics" onNavigate={handleNavigate} />
      ) : view === 'components' ? (
        <PlaceholderPage title="Components" description="UI Elements Library" currentView="components" onNavigate={handleNavigate} />
      ) : view === 'portfolio' ? (
        <PlaceholderPage title="Portfolio" description="Asset Management" currentView="portfolio" onNavigate={handleNavigate} />
      ) : view === 'live-feed' ? (
        <PlaceholderPage title="Live Feed" description="Real-time Market Data" currentView="live-feed" onNavigate={handleNavigate} />
      ) : view === 'academy' ? (
        <PlaceholderPage title="Academy" description="Learn to Trade" currentView="academy" onNavigate={handleNavigate} />
      ) : view === 'markets' ? (
        <PlaceholderPage title="Markets" description="Global Market Overview" currentView="markets" onNavigate={handleNavigate} />
      ) : view === 'settings' ? (
        <PlaceholderPage title="Settings" description="Manage your account" currentView="settings" onNavigate={handleNavigate} />
      ) : (
        <div className="min-h-screen font-sans selection:bg-white/20">
          <Navbar onGetStarted={() => setView('dashboard')} onNavigate={handleNavigate} onOpenCmd={() => setIsCmdOpen(true)} />
          <main>
            <Hero onGetStarted={() => setView('dashboard')} />
            <Marquee />
            <Features />
            <Testimonials />
            <Pricing onGetStarted={() => setView('dashboard')} />
            <CTA onGetStarted={() => setView('dashboard')} />
          </main>

          <footer className="py-24 border-t border-white/5">
            <div className="container mx-auto px-6 grid md:grid-cols-4 gap-12">
              <div className="col-span-2">
                <div className="flex items-center gap-2 mb-6">
                  <div className="w-8 h-8 bg-gradient-to-br from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] rounded-lg flex items-center justify-center shadow-[0_0_10px_rgba(176,38,255,0.3)]">
                    <TrendingUp className="w-5 h-5 text-white" />
                  </div>
                  <span className="text-xl font-display font-bold tracking-tight">TradeLog Pro</span>
                </div>
                <p className="text-white/40 max-w-sm leading-relaxed">
                  Empowering traders with data-driven insights and professional journaling tools since 2024.
                </p>
              </div>
              <div>
                <h4 className="font-bold mb-6">Product</h4>
                <ul className="space-y-4 text-sm text-white/40">
                  <li><a href="#" className="hover:text-white transition-colors">Features</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Pricing</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Integrations</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Changelog</a></li>
                </ul>
              </div>
              <div>
                <h4 className="font-bold mb-6">Company</h4>
                <ul className="space-y-4 text-sm text-white/40">
                  <li><a href="#" className="hover:text-white transition-colors">About Us</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Careers</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Privacy Policy</a></li>
                  <li><a href="#" className="hover:text-white transition-colors">Terms of Service</a></li>
                </ul>
              </div>
            </div>
            <div className="container mx-auto px-6 mt-24 pt-12 border-t border-white/5 text-center text-white/20 text-xs">
              © 2026 TradeLog Pro. All rights reserved. Built for professional traders.
            </div>
          </footer>
        </div>
      )}
    </>
  );
}
