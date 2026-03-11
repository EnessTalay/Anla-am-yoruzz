import { motion } from 'framer-motion';
import type { VennDto } from '../types/debate';

interface VennDiagramProps {
  venn: VennDto;
  leftName: string;
  rightName: string;
}

const VennDiagram = ({ venn, leftName, rightName }: VennDiagramProps) => {
  return (
    <div className="w-full">
      <div className="flex items-center gap-2 mb-6">
        <h3 className="text-xs font-bold text-slate-500 uppercase tracking-widest">Görüş Haritası</h3>
      </div>

      {/* Decorative SVG Venn circles */}
      <div className="flex justify-center mb-6">
        <svg width="240" height="110" viewBox="0 0 240 110" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <radialGradient id="lgLeft" cx="35%" cy="50%" r="65%">
              <stop offset="0%" stopColor="rgba(139,92,246,0.35)" />
              <stop offset="100%" stopColor="rgba(139,92,246,0.04)" />
            </radialGradient>
            <radialGradient id="lgRight" cx="65%" cy="50%" r="65%">
              <stop offset="0%" stopColor="rgba(244,63,94,0.28)" />
              <stop offset="100%" stopColor="rgba(244,63,94,0.03)" />
            </radialGradient>
          </defs>
          <motion.circle
            cx="88"
            cy="55"
            r="50"
            fill="url(#lgLeft)"
            stroke="rgba(139,92,246,0.45)"
            strokeWidth="1"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.6, delay: 0.1 }}
          />
          <motion.circle
            cx="152"
            cy="55"
            r="50"
            fill="url(#lgRight)"
            stroke="rgba(244,63,94,0.38)"
            strokeWidth="1"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.6, delay: 0.2 }}
          />
          {/* Intersection glow */}
          <motion.ellipse
            cx="120"
            cy="55"
            rx="18"
            ry="42"
            fill="rgba(52,211,153,0.1)"
            stroke="rgba(52,211,153,0.25)"
            strokeWidth="0.5"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.5, delay: 0.4 }}
          />
        </svg>
      </div>

      {/* 3-column content grid */}
      <div className="grid grid-cols-3 gap-3">
        {/* Left — Party 1 */}
        <div className="bg-violet-500/[0.07] border border-violet-500/20 rounded-2xl p-4">
          <div className="flex items-center gap-1.5 mb-3">
            <div className="w-2 h-2 rounded-full bg-violet-400 shadow-sm shadow-violet-400/60 shrink-0" />
            <span className="text-[10px] font-bold text-violet-400 uppercase tracking-widest truncate">
              {leftName}
            </span>
          </div>
          {venn.leftPoints.length === 0 ? (
            <p className="text-[10px] text-slate-600 italic">Özel görüş yok</p>
          ) : (
            <div className="space-y-1.5">
              {venn.leftPoints.map((point, i) => (
                <motion.div
                  key={i}
                  initial={{ opacity: 0, x: -8 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.2 + i * 0.08 }}
                  className="text-[11px] text-violet-200/70 bg-white/[0.03] rounded-lg px-2.5 py-2 border border-white/[0.04] leading-relaxed"
                >
                  {point}
                </motion.div>
              ))}
            </div>
          )}
        </div>

        {/* Center — Common */}
        <div className="bg-emerald-500/[0.07] border border-emerald-500/20 rounded-2xl p-4">
          <div className="flex items-center gap-1.5 mb-3">
            <div className="w-2 h-2 rounded-full bg-emerald-400 shadow-sm shadow-emerald-400/60 shrink-0" />
            <span className="text-[10px] font-bold text-emerald-400 uppercase tracking-widest">Ortak</span>
          </div>
          {venn.bothPoints.length === 0 ? (
            <p className="text-[10px] text-slate-600 italic">Ortak nokta yok</p>
          ) : (
            <div className="space-y-1.5">
              {venn.bothPoints.map((point, i) => (
                <motion.div
                  key={i}
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.3 + i * 0.1 }}
                  className="text-[11px] text-emerald-200/70 bg-white/[0.03] rounded-lg px-2.5 py-2 border border-white/[0.04] leading-relaxed"
                >
                  {point}
                </motion.div>
              ))}
            </div>
          )}
        </div>

        {/* Right — Party 2 */}
        <div className="bg-rose-500/[0.07] border border-rose-500/20 rounded-2xl p-4">
          <div className="flex items-center gap-1.5 mb-3">
            <div className="w-2 h-2 rounded-full bg-rose-400 shadow-sm shadow-rose-400/60 shrink-0" />
            <span className="text-[10px] font-bold text-rose-400 uppercase tracking-widest truncate">
              {rightName}
            </span>
          </div>
          {venn.rightPoints.length === 0 ? (
            <p className="text-[10px] text-slate-600 italic">Özel görüş yok</p>
          ) : (
            <div className="space-y-1.5">
              {venn.rightPoints.map((point, i) => (
                <motion.div
                  key={i}
                  initial={{ opacity: 0, x: 8 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.2 + i * 0.08 }}
                  className="text-[11px] text-rose-200/70 bg-white/[0.03] rounded-lg px-2.5 py-2 border border-white/[0.04] leading-relaxed"
                >
                  {point}
                </motion.div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Legend */}
      <div className="flex justify-center gap-6 mt-5">
        <div className="flex items-center gap-2">
          <div className="w-2.5 h-2.5 rounded-full bg-violet-500/60 border border-violet-400/40" />
          <span className="text-[10px] text-slate-600">{leftName}</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-2.5 h-2.5 rounded-full bg-emerald-500/60 border border-emerald-400/40" />
          <span className="text-[10px] text-slate-600">Ortak Nokta</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-2.5 h-2.5 rounded-full bg-rose-500/60 border border-rose-400/40" />
          <span className="text-[10px] text-slate-600">{rightName}</span>
        </div>
      </div>
    </div>
  );
};

export default VennDiagram;
