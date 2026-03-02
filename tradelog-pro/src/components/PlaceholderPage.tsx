import { useState } from 'react';
import { ZapIcon, Menu, X, Construction } from 'lucide-react';
import { Sidebar } from './Sidebar';

export function PlaceholderPage({ title, description, currentView, onNavigate }: { title: string, description: string, currentView: string, onNavigate: (view: string) => void }) {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-[#020205] text-white p-4 md:p-6 font-sans selection:bg-white/20 overflow-x-hidden relative">
      {/* Background Effects */}
      <div className="fixed inset-0 grid-bg opacity-30 pointer-events-none z-0" />
      <div className="fixed inset-0 grid-bg-mask pointer-events-none z-0" />
      <div className="fixed top-[-10%] right-[-5%] w-[30%] h-[30%] ambient-glow ambient-glow-blue z-0" />
      <div className="fixed bottom-[-10%] left-[-5%] w-[30%] h-[30%] ambient-glow ambient-glow-purple opacity-10 z-0" />

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
          currentView={currentView}
          onNavigate={onNavigate}
          onBack={() => onNavigate('landing')}
          isMobileMenuOpen={isMobileMenuOpen}
          setIsMobileMenuOpen={setIsMobileMenuOpen}
        />

        {/* Main Content */}
        <main className="flex-1 py-2 xl:py-4 flex flex-col">
          {/* Header */}
          <header className="flex flex-col md:flex-row md:items-center justify-between gap-6 mb-8 xl:mb-10 p-6 premium-glass">
            <div className="pr-8 relative">
              <h1 className="text-3xl font-display font-medium tracking-tight text-[#e0e0e0] leading-tight">{title}</h1>
              <p className="text-xs text-[#777777] font-medium tracking-wide mt-2">{description}</p>
            </div>
          </header>

          <div className="flex-1 premium-glass flex flex-col items-center justify-center p-12 text-center relative overflow-hidden">
            {/* Ambient glow behind icon */}
            <div className="absolute w-40 h-40 bg-[var(--color-neon-purple)] blur-[100px] opacity-10 pointer-events-none" />

            <div className="w-20 h-20 bg-gradient-to-br from-[var(--color-neon-purple)]/10 to-[var(--color-neon-blue)]/10 border border-[var(--color-neon-purple)]/20 rounded-full flex items-center justify-center mb-6 relative z-10">
              <Construction className="w-10 h-10 text-[var(--color-neon-purple)]" />
            </div>
            <h2 className="text-2xl font-display font-medium mb-4 text-[#e0e0e0] relative z-10">Under Construction</h2>
            <p className="text-[#666666] max-w-md mx-auto relative z-10">
              We are currently working hard to bring you the {title} feature. Stay tuned for updates!
            </p>
            <button
              onClick={() => onNavigate('dashboard')}
              className="mt-8 px-8 py-3 bg-gradient-to-r from-[var(--color-neon-purple)] to-[var(--color-neon-blue)] text-white rounded-xl font-bold text-sm hover:shadow-[0_0_25px_rgba(176,38,255,0.4)] transition-all hover:scale-[1.02] relative z-10"
            >
              Back to Dashboard
            </button>
          </div>
        </main>
      </div>
    </div>
  );
}
