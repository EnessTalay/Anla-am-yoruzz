import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Scale, MessageSquare, Loader2, AlertCircle,
  ChevronRight, CheckCircle2, Circle, Sparkles, Brain,
  Heart, TrendingUp, Lightbulb, ArrowLeft,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { startDebate, analyzeDebate, updateActionStep } from '../api/debateApi';
import VennDiagram from '../components/VennDiagram';
import type {
  StartDebateRequest,
  StartDebateResponse,
  AnalysisResultDto,
  ActionStepDto,
} from '../types/debate';

type Step = 'entry' | 'questions' | 'analyzing' | 'result';

const ANALYZING_MESSAGES = [
  'Arabulucunuz verileri inceliyor...',
  'Ortak noktalar aranıyor...',
  'Duygusal tonlar analiz ediliyor...',
  'Eylem planı hazırlanıyor...',
];

const STEP_LABELS = ['Giriş', 'Sorular', 'Sonuç'];
const STEP_ORDER: Step[] = ['entry', 'questions', 'result'];

/* ── Gradient border glass card helper ── */
const CARD_BORDER = 'linear-gradient(135deg, rgba(255,255,255,0.14) 0%, rgba(255,255,255,0.05) 45%, rgba(255,255,255,0.01) 100%)';
const CARD_INNER = 'bg-white/[0.025] backdrop-blur-2xl rounded-[calc(2.5rem-1px)]';

/* ── Reusable input class strings ── */
const INPUT = 'w-full px-5 py-4 rounded-2xl bg-white/[0.04] border border-white/[0.07] text-white placeholder:text-white/25 focus:outline-none focus:ring-2 focus:ring-purple-500/30 focus:border-purple-500/20 transition-all duration-300 text-base';
const TEXTAREA = INPUT + ' resize-none text-sm leading-relaxed';

const HomePage = () => {
  const [step, setStep] = useState<Step>('entry');
  const [form, setForm] = useState<StartDebateRequest>({
    topic: '',
    person1Name: '',
    person1View: '',
    person2Name: '',
    person2View: '',
  });
  const [isLoading, setIsLoading] = useState(false);
  const [debateData, setDebateData] = useState<StartDebateResponse | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [analysisResult, setAnalysisResult] = useState<AnalysisResultDto | null>(null);
  const [actionSteps, setActionSteps] = useState<ActionStepDto[]>([]);
  const [analyzingMsgIdx, setAnalyzingMsgIdx] = useState(0);

  const handleChange = (field: keyof StartDebateRequest) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => setForm(prev => ({ ...prev, [field]: e.target.value }));

  const isFormValid =
    form.topic.trim() &&
    form.person1Name.trim() &&
    form.person1View.trim().length >= 3 &&
    form.person2Name.trim() &&
    form.person2View.trim().length >= 3;

  const handleStartDebate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading) return;
    setIsLoading(true);
    try {
      const response = await startDebate(form);
      setDebateData(response);
      const init: Record<string, string> = {};
      response.questions.forEach(q => { init[q.questionId] = ''; });
      setAnswers(init);
      setStep('questions');
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number; data?: { errors?: Record<string, string[]> } } };
      if (axiosErr?.response?.status === 400 && axiosErr.response.data?.errors) {
        const firstMsg = Object.values(axiosErr.response.data.errors).flat()[0];
        toast.error(firstMsg ?? 'Lütfen tüm alanları eksiksiz doldurun.');
      } else {
        toast.error('Tartışma başlatılamadı. Backend servisinin çalıştığından emin olun.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const areAnswersFilled = debateData
    ? debateData.questions.every(q => answers[q.questionId]?.trim())
    : false;

  const handleAnalyze = async () => {
    if (!debateData || isLoading) return;
    setIsLoading(true);
    setStep('analyzing');
    const interval = setInterval(() => {
      setAnalyzingMsgIdx(prev => (prev + 1) % ANALYZING_MESSAGES.length);
    }, 2200);
    try {
      const result = await analyzeDebate(debateData.debateId, {
        debateId: debateData.debateId,
        answers: debateData.questions.map(q => ({
          questionId: q.questionId,
          answerText: answers[q.questionId] ?? '',
        })),
      });
      setAnalysisResult(result);
      setActionSteps(result.actionSteps);
      setStep('result');
    } catch {
      toast.error('Analiz sırasında bir hata oluştu. Lütfen tekrar deneyin.');
      setStep('questions');
    } finally {
      clearInterval(interval);
      setIsLoading(false);
    }
  };

  const handleToggleStep = async (stepItem: ActionStepDto) => {
    if (!debateData || stepItem.isCompleted) return;
    const prev = [...actionSteps];
    setActionSteps(s => s.map(x => x.id === stepItem.id ? { ...x, isCompleted: true } : x));
    try {
      await updateActionStep(debateData.debateId, stepItem.id);
      toast.success('Adım tamamlandı olarak işaretlendi!');
    } catch {
      setActionSteps(prev);
      toast.error('Adım güncellenemedi. Lütfen tekrar deneyin.');
    }
  };

  const handleReset = () => {
    setStep('entry');
    setForm({ topic: '', person1Name: '', person1View: '', person2Name: '', person2View: '' });
    setDebateData(null);
    setAnswers({});
    setAnalysisResult(null);
    setActionSteps([]);
    setAnalyzingMsgIdx(0);
  };

  const p1Questions = debateData?.questions.filter(q => q.forSide === 'P1') ?? [];
  const p2Questions = debateData?.questions.filter(q => q.forSide === 'P2') ?? [];
  const completedCount = actionSteps.filter(s => s.isCompleted).length;
  const currentIdx = STEP_ORDER.indexOf(step === 'analyzing' ? 'questions' : step);

  return (
    <div className="relative min-h-screen w-full flex items-center justify-center overflow-x-hidden">

      {/* ── Fixed background glow orbs ── */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        <div
          className="absolute rounded-full"
          style={{
            top: '-25%', left: '-15%',
            width: 700, height: 700,
            background: 'radial-gradient(circle, rgba(124,58,237,0.22) 0%, transparent 70%)',
            filter: 'blur(0px)',
          }}
        />
        <div
          className="absolute rounded-full"
          style={{
            bottom: '-25%', right: '-15%',
            width: 600, height: 600,
            background: 'radial-gradient(circle, rgba(67,56,202,0.18) 0%, transparent 70%)',
            filter: 'blur(0px)',
          }}
        />
        <div
          className="absolute rounded-full"
          style={{
            top: '35%', right: '-5%',
            width: 350, height: 350,
            background: 'radial-gradient(circle, rgba(139,92,246,0.1) 0%, transparent 70%)',
            filter: 'blur(0px)',
          }}
        />
      </div>

      {/* ── Page content ── */}
      <div className="relative z-10 w-full max-w-5xl mx-auto px-6 py-14">

        {/* ── Stepper ── */}
        {step !== 'analyzing' && (
          <motion.nav
            initial={{ opacity: 0, y: -12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45 }}
            className="flex items-center justify-center mb-12 gap-3"
          >
            {STEP_ORDER.flatMap((s, i) => {
              const isActive = s === (step === 'analyzing' ? 'questions' : step);
              const isDone = i < currentIdx;
              const stepEl = (
                <div key={s} className="relative flex flex-col items-center gap-1.5 px-3 pb-3">
                  <span className={`text-sm font-semibold transition-all duration-300 ${
                    isActive ? 'text-white' : isDone ? 'text-emerald-400/60' : 'text-white/20'
                  }`}>
                    {STEP_LABELS[i]}
                  </span>
                  {isActive && (
                    <motion.div
                      layoutId="step-underline"
                      className="absolute bottom-0 left-0 right-0 h-[2px] rounded-full"
                      style={{
                        background: 'linear-gradient(90deg, #a855f7, #3b82f6)',
                        boxShadow: '0 0 12px rgba(168,85,247,0.9), 0 0 24px rgba(168,85,247,0.4)',
                      }}
                    />
                  )}
                  {isDone && !isActive && (
                    <div className="absolute bottom-0 left-0 right-0 h-[2px] rounded-full bg-emerald-500/35" />
                  )}
                </div>
              );
              return i < 2
                ? [stepEl, <div key={`conn-${i}`} className="w-14 h-px bg-white/[0.07] mb-3" />]
                : [stepEl];
            })}
          </motion.nav>
        )}

        {/* ── Step content ── */}
        <AnimatePresence mode="wait">

          {/* ════════════════════════════════
              STEP 1 — ENTRY
          ════════════════════════════════ */}
          {step === 'entry' && (
            <motion.div
              key="entry"
              initial={{ opacity: 0, y: 28 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              transition={{ duration: 0.45 }}
            >
              <form onSubmit={handleStartDebate}>
                {/* Gradient-border glass card */}
                <div className="rounded-[2.5rem] p-px" style={{ background: CARD_BORDER }}>
                  <div className={`${CARD_INNER} p-10 md:p-12 space-y-10`}>

                    {/* Hero section */}
                    <motion.div
                      initial={{ opacity: 0, y: 16 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.12 }}
                      className="text-center space-y-5"
                    >
                      <div className="inline-flex items-center justify-center">
                        <div className="relative">
                          <div className="absolute inset-0 rounded-2xl bg-purple-500/30 blur-2xl scale-[1.6]" />
                          <div className="relative w-14 h-14 rounded-2xl bg-white/[0.06] border border-white/[0.12] flex items-center justify-center shadow-2xl">
                            <Scale className="w-6 h-6 text-white/65" />
                          </div>
                        </div>
                      </div>
                      <h1
                        className="text-6xl md:text-7xl font-bold tracking-tighter bg-clip-text text-transparent leading-[1.05]"
                        style={{ backgroundImage: 'linear-gradient(180deg, #ffffff 0%, rgba(255,255,255,0.4) 100%)' }}
                      >
                        Anlaşamıyoruz
                      </h1>
                      <p className="text-white/35 text-base font-light tracking-wide max-w-sm mx-auto">
                        Tartışmaları veriye, gerginliği çözüme dönüştürün.
                      </p>
                    </motion.div>

                    {/* Divider */}
                    <div className="h-px" style={{ background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.08), transparent)' }} />

                    {/* Topic input */}
                    <motion.div
                      initial={{ opacity: 0, y: 12 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.22 }}
                    >
                      <label className="flex items-center gap-2 text-xs font-semibold text-white/30 uppercase tracking-widest mb-3">
                        <MessageSquare className="w-3.5 h-3.5" />
                        Tartışma Konusu
                      </label>
                      <input
                        type="text"
                        value={form.topic}
                        onChange={handleChange('topic')}
                        placeholder="Ne üzerine anlaşamıyorsunuz?"
                        className={INPUT}
                        disabled={isLoading}
                      />
                    </motion.div>

                    {/* Parties — Bento boxes with VS divider */}
                    <motion.div
                      initial={{ opacity: 0, y: 12 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.3 }}
                      className="rounded-3xl overflow-hidden border border-white/[0.06]"
                      style={{ display: 'grid', gridTemplateColumns: '1fr' }}
                    >
                      {/* Mobile layout: stacked */}
                      <div className="md:hidden">
                        {/* Party 1 */}
                        <div className="relative p-6 space-y-4 overflow-hidden">
                          <div
                            className="absolute -bottom-12 -left-12 w-56 h-56 rounded-full pointer-events-none"
                            style={{ background: 'radial-gradient(circle, rgba(59,130,246,0.15) 0%, transparent 70%)' }}
                          />
                          <div className="flex items-center gap-2.5">
                            <div className="w-2 h-2 rounded-full bg-blue-400" style={{ boxShadow: '0 0 8px rgba(96,165,250,0.9)' }} />
                            <span className="text-xs font-bold text-blue-400/75 uppercase tracking-widest">Taraf 1</span>
                          </div>
                          <input type="text" value={form.person1Name} onChange={handleChange('person1Name')} placeholder="Kişi adı..." className={INPUT + ' border-blue-500/[0.1] focus:ring-blue-500/25'} disabled={isLoading} />
                          <textarea value={form.person1View} onChange={handleChange('person1View')} placeholder="Bu kişinin görüşü nedir? (örn: Brewmood daha iyi)" rows={4} className={TEXTAREA + ' border-blue-500/[0.1] focus:ring-blue-500/25'} disabled={isLoading} />
                        </div>

                        {/* Mobile VS */}
                        <div className="flex items-center gap-3 px-6 py-4 border-y border-white/[0.05]">
                          <div className="flex-1 h-px" style={{ background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.1))' }} />
                          <div className="w-9 h-9 rounded-full flex items-center justify-center" style={{ background: '#030014', border: '1px solid rgba(168,85,247,0.45)', boxShadow: '0 0 18px rgba(168,85,247,0.3)' }}>
                            <span className="text-[9px] font-black text-purple-400 tracking-wider">VS</span>
                          </div>
                          <div className="flex-1 h-px" style={{ background: 'linear-gradient(270deg, transparent, rgba(255,255,255,0.1))' }} />
                        </div>

                        {/* Party 2 */}
                        <div className="relative p-6 space-y-4 overflow-hidden">
                          <div
                            className="absolute -bottom-12 -right-12 w-56 h-56 rounded-full pointer-events-none"
                            style={{ background: 'radial-gradient(circle, rgba(244,63,94,0.15) 0%, transparent 70%)' }}
                          />
                          <div className="flex items-center gap-2.5">
                            <div className="w-2 h-2 rounded-full bg-rose-400" style={{ boxShadow: '0 0 8px rgba(251,113,133,0.9)' }} />
                            <span className="text-xs font-bold text-rose-400/75 uppercase tracking-widest">Taraf 2</span>
                          </div>
                          <input type="text" value={form.person2Name} onChange={handleChange('person2Name')} placeholder="Kişi adı..." className={INPUT + ' border-rose-500/[0.1] focus:ring-rose-500/25'} disabled={isLoading} />
                          <textarea value={form.person2View} onChange={handleChange('person2View')} placeholder="Bu kişinin görüşü nedir? (örn: Mackbear daha iyi)" rows={4} className={TEXTAREA + ' border-rose-500/[0.1] focus:ring-rose-500/25'} disabled={isLoading} />
                        </div>
                      </div>

                      {/* Desktop layout: side by side */}
                      <div className="hidden md:grid" style={{ gridTemplateColumns: '1fr 68px 1fr' }}>
                        {/* Party 1 */}
                        <div className="relative p-8 space-y-5 overflow-hidden">
                          <div
                            className="absolute -bottom-16 -left-16 w-64 h-64 rounded-full pointer-events-none"
                            style={{ background: 'radial-gradient(circle, rgba(59,130,246,0.16) 0%, transparent 70%)' }}
                          />
                          <div className="flex items-center gap-2.5">
                            <div className="w-2 h-2 rounded-full bg-blue-400" style={{ boxShadow: '0 0 8px rgba(96,165,250,0.9)' }} />
                            <span className="text-xs font-bold text-blue-400/75 uppercase tracking-widest">Taraf 1</span>
                          </div>
                          <input type="text" value={form.person1Name} onChange={handleChange('person1Name')} placeholder="Kişi adı..." className={INPUT + ' border-blue-500/[0.1] focus:ring-blue-500/25'} disabled={isLoading} />
                          <textarea value={form.person1View} onChange={handleChange('person1View')} placeholder="Bu kişinin görüşü nedir? (örn: Brewmood daha iyi)" rows={5} className={TEXTAREA + ' border-blue-500/[0.1] focus:ring-blue-500/25'} disabled={isLoading} />
                        </div>

                        {/* VS Divider */}
                        <div className="relative flex flex-col items-center justify-center border-x border-white/[0.05]" style={{ background: 'rgba(255,255,255,0.01)' }}>
                          <div className="absolute inset-0" style={{ background: 'linear-gradient(180deg, transparent, rgba(168,85,247,0.06), transparent)' }} />
                          <div className="w-px flex-1" style={{ background: 'linear-gradient(180deg, transparent, rgba(255,255,255,0.1), transparent)' }} />
                          <div
                            className="relative z-10 my-4 w-11 h-11 rounded-full flex items-center justify-center"
                            style={{
                              background: '#030014',
                              border: '1px solid rgba(168,85,247,0.5)',
                              boxShadow: '0 0 22px rgba(168,85,247,0.38), inset 0 0 12px rgba(168,85,247,0.06)',
                            }}
                          >
                            <span className="text-[10px] font-black text-purple-400 tracking-[0.12em]">VS</span>
                          </div>
                          <div className="w-px flex-1" style={{ background: 'linear-gradient(180deg, transparent, rgba(255,255,255,0.1), transparent)' }} />
                        </div>

                        {/* Party 2 */}
                        <div className="relative p-8 space-y-5 overflow-hidden">
                          <div
                            className="absolute -bottom-16 -right-16 w-64 h-64 rounded-full pointer-events-none"
                            style={{ background: 'radial-gradient(circle, rgba(244,63,94,0.16) 0%, transparent 70%)' }}
                          />
                          <div className="flex items-center gap-2.5">
                            <div className="w-2 h-2 rounded-full bg-rose-400" style={{ boxShadow: '0 0 8px rgba(251,113,133,0.9)' }} />
                            <span className="text-xs font-bold text-rose-400/75 uppercase tracking-widest">Taraf 2</span>
                          </div>
                          <input type="text" value={form.person2Name} onChange={handleChange('person2Name')} placeholder="Kişi adı..." className={INPUT + ' border-rose-500/[0.1] focus:ring-rose-500/25'} disabled={isLoading} />
                          <textarea value={form.person2View} onChange={handleChange('person2View')} placeholder="Bu kişinin görüşü nedir? (örn: Mackbear daha iyi)" rows={5} className={TEXTAREA + ' border-rose-500/[0.1] focus:ring-rose-500/25'} disabled={isLoading} />
                        </div>
                      </div>
                    </motion.div>

                  </div>
                </div>

                {/* CTA Button */}
                <div className="flex justify-center mt-10">
                  <motion.button
                    type="submit"
                    disabled={!isFormValid || isLoading}
                    whileHover={isFormValid && !isLoading ? { scale: 1.04 } : {}}
                    whileTap={isFormValid && !isLoading ? { scale: 0.97 } : {}}
                    className="flex items-center gap-3 py-4 px-14 rounded-2xl text-white font-semibold text-base shadow-xl disabled:opacity-25 disabled:cursor-not-allowed transition-all duration-300"
                    style={{
                      background: 'linear-gradient(135deg, #9333ea, #3b82f6)',
                      boxShadow: isFormValid && !isLoading
                        ? '0 8px 32px rgba(168,85,247,0.25)'
                        : undefined,
                    }}
                    onMouseEnter={e => {
                      if (isFormValid && !isLoading)
                        (e.currentTarget as HTMLButtonElement).style.boxShadow = '0 0 32px rgba(168,85,247,0.5), 0 8px 32px rgba(168,85,247,0.3)';
                    }}
                    onMouseLeave={e => {
                      (e.currentTarget as HTMLButtonElement).style.boxShadow = '0 8px 32px rgba(168,85,247,0.25)';
                    }}
                  >
                    {isLoading ? (
                      <><Loader2 className="w-5 h-5 animate-spin" />Sorular hazırlanıyor...</>
                    ) : (
                      <>Devam Et<ChevronRight className="w-5 h-5" /></>
                    )}
                  </motion.button>
                </div>
              </form>
            </motion.div>
          )}

          {/* ════════════════════════════════
              STEP 2 — QUESTIONS
          ════════════════════════════════ */}
          {step === 'questions' && debateData && (
            <motion.div
              key="questions"
              initial={{ opacity: 0, y: 28 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              transition={{ duration: 0.45 }}
              className="space-y-7"
            >
              {/* Glass card */}
              <div className="rounded-[2.5rem] p-px" style={{ background: CARD_BORDER }}>
                <div className={`${CARD_INNER} p-8 md:p-10 space-y-8`}>

                  {/* Header */}
                  <div className="flex items-center gap-4">
                    <div className="p-3 rounded-2xl border" style={{ background: 'rgba(245,158,11,0.08)', borderColor: 'rgba(245,158,11,0.18)' }}>
                      <Sparkles className="w-5 h-5 text-amber-400" />
                    </div>
                    <div>
                      <h2 className="text-xl font-bold text-white">Derinleştirici Sorular</h2>
                      <p className="text-sm text-white/35 mt-0.5">
                        Arabulucunuz her iki taraf için sorular hazırladı. İçtenlikle yanıtlayın.
                      </p>
                    </div>
                  </div>

                  <div className="h-px" style={{ background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.08), transparent)' }} />

                  {/* Two-column questions */}
                  <div className="grid grid-cols-1 gap-8 md:grid-cols-2">
                    {p1Questions.length > 0 && (
                      <div className="space-y-5">
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full bg-blue-400" style={{ boxShadow: '0 0 8px rgba(96,165,250,0.7)' }} />
                          <span className="text-xs font-bold text-blue-400/70 uppercase tracking-widest">{form.person1Name}</span>
                        </div>
                        {p1Questions.map((q, i) => (
                          <motion.div
                            key={q.questionId}
                            initial={{ opacity: 0, x: -18 }}
                            animate={{ opacity: 1, x: 0 }}
                            transition={{ delay: i * 0.09 }}
                            className="space-y-3"
                          >
                            <p className="text-sm text-white/65 font-medium leading-relaxed">{q.questionText}</p>
                            <textarea
                              value={answers[q.questionId] ?? ''}
                              onChange={e => setAnswers(prev => ({ ...prev, [q.questionId]: e.target.value }))}
                              placeholder={`${form.person1Name} olarak yanıtla...`}
                              rows={3}
                              className="w-full px-4 py-3.5 rounded-2xl bg-white/[0.04] border border-white/[0.07] text-white placeholder:text-white/20 focus:outline-none focus:ring-2 focus:ring-blue-500/25 focus:border-blue-500/20 transition-all duration-300 resize-none text-sm leading-relaxed"
                            />
                          </motion.div>
                        ))}
                      </div>
                    )}
                    {p2Questions.length > 0 && (
                      <div className="space-y-5">
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full bg-rose-400" style={{ boxShadow: '0 0 8px rgba(251,113,133,0.7)' }} />
                          <span className="text-xs font-bold text-rose-400/70 uppercase tracking-widest">{form.person2Name}</span>
                        </div>
                        {p2Questions.map((q, i) => (
                          <motion.div
                            key={q.questionId}
                            initial={{ opacity: 0, x: 18 }}
                            animate={{ opacity: 1, x: 0 }}
                            transition={{ delay: i * 0.09 }}
                            className="space-y-3"
                          >
                            <p className="text-sm text-white/65 font-medium leading-relaxed">{q.questionText}</p>
                            <textarea
                              value={answers[q.questionId] ?? ''}
                              onChange={e => setAnswers(prev => ({ ...prev, [q.questionId]: e.target.value }))}
                              placeholder={`${form.person2Name} olarak yanıtla...`}
                              rows={3}
                              className="w-full px-4 py-3.5 rounded-2xl bg-white/[0.04] border border-white/[0.07] text-white placeholder:text-white/20 focus:outline-none focus:ring-2 focus:ring-rose-500/25 focus:border-rose-500/20 transition-all duration-300 resize-none text-sm leading-relaxed"
                            />
                          </motion.div>
                        ))}
                      </div>
                    )}
                  </div>

                </div>
              </div>

              {/* Navigation */}
              <div className="flex items-center justify-between">
                <button
                  onClick={() => setStep('entry')}
                  className="flex items-center gap-2 py-3 px-6 rounded-2xl text-white/45 font-medium transition-all duration-200 hover:text-white/70"
                  style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
                >
                  <ArrowLeft className="w-4 h-4" />
                  Geri Dön
                </button>
                <motion.button
                  onClick={handleAnalyze}
                  disabled={!areAnswersFilled || isLoading}
                  whileHover={areAnswersFilled && !isLoading ? { scale: 1.04 } : {}}
                  whileTap={areAnswersFilled && !isLoading ? { scale: 0.97 } : {}}
                  className="flex items-center gap-3 py-3.5 px-10 rounded-2xl text-white font-semibold shadow-xl disabled:opacity-25 disabled:cursor-not-allowed transition-all duration-300"
                  style={{ background: 'linear-gradient(135deg, #9333ea, #3b82f6)' }}
                  onMouseEnter={e => {
                    if (areAnswersFilled && !isLoading)
                      (e.currentTarget as HTMLButtonElement).style.boxShadow = '0 0 30px rgba(168,85,247,0.45)';
                  }}
                  onMouseLeave={e => {
                    (e.currentTarget as HTMLButtonElement).style.boxShadow = '';
                  }}
                >
                  {isLoading ? (
                    <><Loader2 className="w-5 h-5 animate-spin" />Analiz başlatılıyor...</>
                  ) : (
                    <><Brain className="w-5 h-5" />Analizi Başlat<ChevronRight className="w-4 h-4" /></>
                  )}
                </motion.button>
              </div>
            </motion.div>
          )}

          {/* ════════════════════════════════
              STEP 3 — ANALYZING
          ════════════════════════════════ */}
          {step === 'analyzing' && (
            <motion.div
              key="analyzing"
              initial={{ opacity: 0, scale: 0.94 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.94 }}
              transition={{ duration: 0.45 }}
              className="flex flex-col items-center justify-center min-h-[540px] text-center"
            >
              <div className="relative w-52 h-52 mb-14">
                {/* Outer ring */}
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ duration: 16, repeat: Infinity, ease: 'linear' }}
                  className="absolute inset-0 rounded-full"
                  style={{ border: '1px dashed rgba(168,85,247,0.22)' }}
                />
                {/* Mid ring */}
                <motion.div
                  animate={{ rotate: -360 }}
                  transition={{ duration: 9, repeat: Infinity, ease: 'linear' }}
                  className="absolute inset-6 rounded-full"
                  style={{ border: '1px dashed rgba(244,63,94,0.18)' }}
                />
                {/* Inner glow */}
                <motion.div
                  animate={{ scale: [1, 1.3, 1], opacity: [0.2, 0.55, 0.2] }}
                  transition={{ duration: 2.8, repeat: Infinity, ease: 'easeInOut' }}
                  className="absolute inset-14 rounded-full"
                  style={{ background: 'rgba(168,85,247,0.28)', filter: 'blur(16px)' }}
                />
                {/* Center icon */}
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="relative">
                    <div className="absolute inset-0 rounded-2xl" style={{ background: 'rgba(168,85,247,0.3)', filter: 'blur(20px)' }} />
                    <div
                      className="relative w-[76px] h-[76px] rounded-2xl flex items-center justify-center"
                      style={{ background: 'rgba(168,85,247,0.15)', border: '1px solid rgba(168,85,247,0.35)', boxShadow: '0 8px 40px rgba(168,85,247,0.25)' }}
                    >
                      <Scale className="w-8 h-8 text-purple-300" />
                    </div>
                  </div>
                </div>
                {/* Orbiting dots */}
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ duration: 3.5, repeat: Infinity, ease: 'linear' }}
                  style={{ position: 'absolute', inset: 0 }}
                >
                  <div
                    className="absolute left-1/2 -translate-x-1/2 w-4 h-4 rounded-full"
                    style={{ top: '-8px', background: '#a855f7', boxShadow: '0 0 12px rgba(168,85,247,0.8)' }}
                  />
                </motion.div>
                <motion.div
                  animate={{ rotate: -360 }}
                  transition={{ duration: 5.5, repeat: Infinity, ease: 'linear' }}
                  style={{ position: 'absolute', inset: 24 }}
                >
                  <div
                    className="absolute left-1/2 -translate-x-1/2 w-2.5 h-2.5 rounded-full"
                    style={{ top: '-5px', background: '#f43f5e', boxShadow: '0 0 10px rgba(244,63,94,0.8)' }}
                  />
                </motion.div>
              </div>

              <AnimatePresence mode="wait">
                <motion.p
                  key={analyzingMsgIdx}
                  initial={{ opacity: 0, y: 14 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -14 }}
                  transition={{ duration: 0.4 }}
                  className="text-2xl font-semibold text-white mb-3"
                >
                  {ANALYZING_MESSAGES[analyzingMsgIdx]}
                </motion.p>
              </AnimatePresence>

              <p className="text-white/25 max-w-xs">
                Bu işlem birkaç saniye sürebilir. Lütfen sayfayı kapatmayın.
              </p>

              <div className="flex gap-3 mt-12">
                {[0, 1, 2].map(i => (
                  <motion.div
                    key={i}
                    animate={{ y: [0, -14, 0], opacity: [0.3, 1, 0.3] }}
                    transition={{ duration: 1.1, repeat: Infinity, delay: i * 0.22, ease: 'easeInOut' }}
                    style={{ width: 10, height: 10, borderRadius: '50%', background: '#a855f7' }}
                  />
                ))}
              </div>
            </motion.div>
          )}

          {/* ════════════════════════════════
              STEP 4 — RESULT
          ════════════════════════════════ */}
          {step === 'result' && analysisResult && (
            <motion.div
              key="result"
              initial={{ opacity: 0, y: 28 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5 }}
              className="space-y-5 pb-10"
            >
              {/* Success banner */}
              <div
                className="rounded-2xl p-5 flex items-center gap-4"
                style={{ background: 'rgba(16,185,129,0.07)', border: '1px solid rgba(16,185,129,0.18)' }}
              >
                <div
                  className="w-11 h-11 rounded-xl flex items-center justify-center shrink-0"
                  style={{ background: 'rgba(16,185,129,0.12)', border: '1px solid rgba(16,185,129,0.2)' }}
                >
                  <CheckCircle2 className="w-5 h-5 text-emerald-400" />
                </div>
                <div>
                  <p className="font-bold text-white">Analiz tamamlandı!</p>
                  <p className="text-sm text-white/35 mt-0.5">
                    <span className="font-semibold text-purple-400">{form.person1Name}</span> ve{' '}
                    <span className="font-semibold text-rose-400">{form.person2Name}</span> arasındaki tartışma başarıyla analiz edildi.
                  </p>
                </div>
              </div>

              {/* Venn + Scores side-by-side */}
              <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-5">
                <div className="rounded-[2rem] p-px" style={{ background: CARD_BORDER }}>
                  <div className={`${CARD_INNER} p-6`}>
                    <VennDiagram venn={analysisResult.venn} leftName={form.person1Name} rightName={form.person2Name} />
                  </div>
                </div>

                <div className="rounded-[2rem] p-px" style={{ background: CARD_BORDER }}>
                  <div className={`${CARD_INNER} p-6 flex flex-col justify-center h-full`}>
                    <div className="flex items-center gap-2 mb-6">
                      <TrendingUp className="w-4 h-4 text-white/25" />
                      <h3 className="text-xs font-bold text-white/25 uppercase tracking-widest">Uzlaşma Skoru</h3>
                    </div>
                    <div className="space-y-7">
                      <div className="space-y-3">
                        <div className="flex items-end justify-between">
                          <span className="text-sm font-semibold text-blue-400/80">{form.person1Name}</span>
                          <span className="text-4xl font-bold text-white tabular-nums leading-none">
                            {analysisResult.scores.leftScore}<span className="text-lg text-white/25 font-normal">%</span>
                          </span>
                        </div>
                        <div className="h-2 rounded-full overflow-hidden" style={{ background: 'rgba(255,255,255,0.05)' }}>
                          <motion.div
                            initial={{ width: 0 }}
                            animate={{ width: `${analysisResult.scores.leftScore}%` }}
                            transition={{ duration: 1.4, delay: 0.3, ease: 'easeOut' }}
                            className="h-full rounded-full"
                            style={{ background: 'linear-gradient(90deg, #1d4ed8, #60a5fa)' }}
                          />
                        </div>
                      </div>
                      <div className="space-y-3">
                        <div className="flex items-end justify-between">
                          <span className="text-sm font-semibold text-rose-400/80">{form.person2Name}</span>
                          <span className="text-4xl font-bold text-white tabular-nums leading-none">
                            {analysisResult.scores.rightScore}<span className="text-lg text-white/25 font-normal">%</span>
                          </span>
                        </div>
                        <div className="h-2 rounded-full overflow-hidden" style={{ background: 'rgba(255,255,255,0.05)' }}>
                          <motion.div
                            initial={{ width: 0 }}
                            animate={{ width: `${analysisResult.scores.rightScore}%` }}
                            transition={{ duration: 1.4, delay: 0.5, ease: 'easeOut' }}
                            className="h-full rounded-full"
                            style={{ background: 'linear-gradient(90deg, #9f1239, #fb7185)' }}
                          />
                        </div>
                      </div>
                    </div>
                    {analysisResult.scores.description && (
                      <p className="text-xs text-white/30 mt-6 leading-relaxed p-4 rounded-2xl" style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.05)' }}>
                        {analysisResult.scores.description}
                      </p>
                    )}
                  </div>
                </div>
              </div>

              {/* Emotions */}
              {analysisResult.emotions.length > 0 && (
                <div className="rounded-[2rem] p-px" style={{ background: CARD_BORDER }}>
                  <div className={`${CARD_INNER} p-6`}>
                    <div className="flex items-center gap-2 mb-4">
                      <Heart className="w-4 h-4 text-white/25" />
                      <h3 className="text-xs font-bold text-white/25 uppercase tracking-widest">Duygusal Ton</h3>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {analysisResult.emotions.map((emo, i) => (
                        <span
                          key={i}
                          className="inline-flex items-center gap-2 px-3.5 py-2 rounded-full text-xs font-semibold"
                          style={emo.side === 'P1'
                            ? { background: 'rgba(59,130,246,0.1)', color: 'rgba(147,197,253,0.85)', border: '1px solid rgba(59,130,246,0.18)' }
                            : { background: 'rgba(244,63,94,0.1)', color: 'rgba(253,164,175,0.85)', border: '1px solid rgba(244,63,94,0.18)' }
                          }
                        >
                          <span style={{ opacity: 0.55, fontSize: 10, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase' }}>
                            {emo.side === 'P1' ? form.person1Name : form.person2Name}
                          </span>
                          {emo.emotion}
                          <span style={{ background: 'rgba(255,255,255,0.08)', borderRadius: 99, padding: '2px 6px', opacity: 0.55, fontSize: 10 }}>
                            {emo.intensity}/10
                          </span>
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              )}

              {/* Verdict & Suggestion */}
              <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                <div className="rounded-[2rem] p-5" style={{ background: 'rgba(245,158,11,0.06)', border: '1px solid rgba(245,158,11,0.18)' }}>
                  <div className="flex items-center gap-2.5 mb-4">
                    <div className="p-2 rounded-xl" style={{ background: 'rgba(245,158,11,0.12)', border: '1px solid rgba(245,158,11,0.18)' }}>
                      <Scale className="w-4 h-4 text-amber-400" />
                    </div>
                    <h3 className="text-xs font-bold text-white/30 uppercase tracking-widest">Karar</h3>
                  </div>
                  <p className="text-white/65 leading-relaxed">{analysisResult.verdict}</p>
                </div>
                <div className="rounded-[2rem] p-5" style={{ background: 'rgba(20,184,166,0.06)', border: '1px solid rgba(20,184,166,0.18)' }}>
                  <div className="flex items-center gap-2.5 mb-4">
                    <div className="p-2 rounded-xl" style={{ background: 'rgba(20,184,166,0.12)', border: '1px solid rgba(20,184,166,0.18)' }}>
                      <Lightbulb className="w-4 h-4 text-teal-400" />
                    </div>
                    <h3 className="text-xs font-bold text-white/30 uppercase tracking-widest">Öneri</h3>
                  </div>
                  <p className="text-white/65 leading-relaxed">{analysisResult.suggestion}</p>
                </div>
              </div>

              {/* Action Steps */}
              {actionSteps.length > 0 && (
                <div className="rounded-[2rem] p-px" style={{ background: CARD_BORDER }}>
                  <div className={`${CARD_INNER} p-6`}>
                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="w-4 h-4 text-white/25" />
                        <h3 className="text-xs font-bold text-white/25 uppercase tracking-widest">Eylem Planı</h3>
                      </div>
                      <span className="text-xs font-semibold text-white/30 px-3 py-1.5 rounded-full" style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.08)' }}>
                        {completedCount}/{actionSteps.length} tamamlandı
                      </span>
                    </div>

                    <div className="h-1 rounded-full overflow-hidden mb-6" style={{ background: 'rgba(255,255,255,0.05)' }}>
                      <motion.div
                        animate={{ width: `${actionSteps.length ? (completedCount / actionSteps.length) * 100 : 0}%` }}
                        transition={{ duration: 0.5, ease: 'easeOut' }}
                        className="h-full rounded-full"
                        style={{ background: 'linear-gradient(90deg, #059669, #34d399)' }}
                      />
                    </div>

                    <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
                      {actionSteps.map((s, i) => (
                        <motion.button
                          key={s.id}
                          initial={{ opacity: 0, y: 8 }}
                          animate={{ opacity: 1, y: 0 }}
                          transition={{ delay: i * 0.06 }}
                          onClick={() => handleToggleStep(s)}
                          disabled={s.isCompleted}
                          className="flex items-start gap-3 p-4 rounded-2xl text-left transition-all duration-200"
                          style={s.isCompleted
                            ? { background: 'rgba(16,185,129,0.07)', border: '1px solid rgba(16,185,129,0.14)', cursor: 'default' }
                            : { background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)', cursor: 'pointer' }
                          }
                          onMouseEnter={e => {
                            if (!s.isCompleted) (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.05)';
                          }}
                          onMouseLeave={e => {
                            if (!s.isCompleted) (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.02)';
                          }}
                        >
                          {s.isCompleted
                            ? <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
                            : <Circle className="w-5 h-5 shrink-0 mt-0.5" style={{ color: 'rgba(255,255,255,0.15)' }} />
                          }
                          <span className="text-sm leading-relaxed" style={s.isCompleted
                            ? { color: 'rgba(52,211,153,0.6)', textDecoration: 'line-through', textDecorationColor: 'rgba(52,211,153,0.35)' }
                            : { color: 'rgba(255,255,255,0.5)' }
                          }>
                            {s.stepText}
                          </span>
                        </motion.button>
                      ))}
                    </div>
                  </div>
                </div>
              )}

              {/* Error */}
              {analysisResult.status && analysisResult.status.toLowerCase().includes('error') && (
                <div className="flex items-start gap-3 rounded-2xl p-4" style={{ background: 'rgba(239,68,68,0.07)', border: '1px solid rgba(239,68,68,0.18)' }}>
                  <AlertCircle className="w-4 h-4 text-red-400 mt-0.5 shrink-0" />
                  <p className="text-sm text-red-400/80">Analiz sırasında bir sorun oluştu. Veriler eksik olabilir.</p>
                </div>
              )}

              {/* Reset */}
              <div className="flex justify-center pt-2">
                <button
                  onClick={handleReset}
                  className="flex items-center gap-2 py-3 px-8 rounded-2xl text-white/35 text-sm font-medium transition-all duration-200 hover:text-white/60"
                  style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.06)' }}
                  onMouseEnter={e => { (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.06)'; }}
                  onMouseLeave={e => { (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,0.03)'; }}
                >
                  <ArrowLeft className="w-4 h-4" />
                  Yeni tartışma başlat
                </button>
              </div>
            </motion.div>
          )}

        </AnimatePresence>
      </div>
    </div>
  );
};

export default HomePage;
