import { useEffect, useState } from 'react';
import { getConfiguracion, actualizarConfiguracion } from '../services/api';
import type { Configuracion, ActualizarConfiguracionDto } from '../types';

export default function Configuracion() {
  const [cfg,      setCfg]      = useState<Configuracion | null>(null);
  const [form,     setForm]     = useState<ActualizarConfiguracionDto | null>(null);
  const [loading,  setLoading]  = useState(true);
  const [saving,   setSaving]   = useState(false);
  const [msg,      setMsg]      = useState<{ text: string; ok: boolean } | null>(null);
  const [showKey,  setShowKey]  = useState(false);

  useEffect(() => {
    getConfiguracion()
      .then(data => {
        setCfg(data);
        setForm({
          delayMinSegundos:  data.delayMinSegundos,
          delayMaxSegundos:  data.delayMaxSegundos,
          factorIncremento:  data.factorIncremento,
          wahaApiKey:        data.wahaApiKey,
          wahaEndpointUrl:   data.wahaEndpointUrl,
          wahaSession:       data.wahaSession,
        });
      })
      .catch(() => flash('❌ No se pudo cargar la configuración', false))
      .finally(() => setLoading(false));
  }, []);

  const flash = (text: string, ok: boolean) => {
    setMsg({ text, ok });
    setTimeout(() => setMsg(null), 4000);
  };

  const handleChange = (key: keyof ActualizarConfiguracionDto, value: string | number) => {
    setForm(prev => prev ? { ...prev, [key]: value } : prev);
  };

  const handleGuardar = async () => {
    if (!form) return;
    // Validaciones
    if (form.delayMinSegundos < 1 || form.delayMaxSegundos < form.delayMinSegundos) {
      flash('❌ El delay mínimo debe ser ≥ 1 y menor que el máximo', false);
      return;
    }
    setSaving(true);
    try {
      const updated = await actualizarConfiguracion(form);
      setCfg(updated);
      flash('✅ Configuración guardada correctamente', true);
    } catch {
      flash('❌ Error al guardar la configuración', false);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400 animate-pulse text-lg">Cargando configuración...</div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-8 max-w-2xl mx-auto">
      {/* Flash */}
      {msg && (
        <div className={`fixed top-4 right-4 z-50 px-5 py-3 rounded-xl shadow-xl text-sm font-medium
                         transition-all duration-300 animate-fade-in
                         ${msg.ok ? 'bg-green-500/20 border border-green-500/40 text-green-300'
                                  : 'bg-red-500/20 border border-red-500/40 text-red-300'}`}>
          {msg.text}
        </div>
      )}

      {/* Encabezado */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">⚙️ Configuración</h1>
          <p className="text-slate-500 text-sm mt-1">
            Ajusta los parámetros del motor de envío WAHA.
          </p>
        </div>
        <button
          id="btn-guardar-config"
          onClick={handleGuardar}
          disabled={saving || !form}
          className={`px-6 py-2.5 rounded-xl font-semibold text-sm transition-all duration-200 active:scale-95
            ${saving ? 'bg-slate-700 text-slate-500 cursor-not-allowed'
                     : 'bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20'}`}
        >
          {saving ? '⏳ Guardando...' : '💾 Guardar'}
        </button>
      </div>

      {form && (
        <>
          {/* ── Sección: Control Anti-Spam ── */}
          <section className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-6 flex flex-col gap-5">
            <h2 className="text-base font-semibold text-white flex items-center gap-2">
              <span className="text-yellow-400">⏱</span> Control Anti-Spam
            </h2>
            <p className="text-xs text-slate-500 -mt-2">
              Delay aleatorio entre cada envío individual para evitar bloqueos de WhatsApp.
            </p>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-slate-300">Delay Mínimo (seg)</label>
                <input
                  id="input-delay-min"
                  type="number"
                  min={1}
                  max={3600}
                  value={form.delayMinSegundos}
                  onChange={e => handleChange('delayMinSegundos', parseInt(e.target.value) || 1)}
                  className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm
                             focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                             transition-all duration-200"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-slate-300">Delay Máximo (seg)</label>
                <input
                  id="input-delay-max"
                  type="number"
                  min={1}
                  max={3600}
                  value={form.delayMaxSegundos}
                  onChange={e => handleChange('delayMaxSegundos', parseInt(e.target.value) || 1)}
                  className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm
                             focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                             transition-all duration-200"
                />
              </div>
            </div>

            {/* Preview visual del delay */}
            <div className="rounded-xl bg-slate-900/40 border border-slate-700/40 px-4 py-3 text-xs text-slate-400">
              💡 El motor esperará entre{' '}
              <span className="text-yellow-300 font-semibold">{form.delayMinSegundos}s</span> y{' '}
              <span className="text-yellow-300 font-semibold">{form.delayMaxSegundos}s</span> entre cada mensaje.
              Con 50 mensajes esto toma entre{' '}
              <span className="text-white">{Math.round(50 * form.delayMinSegundos / 60)} min</span> y{' '}
              <span className="text-white">{Math.round(50 * form.delayMaxSegundos / 60)} min</span>.
            </div>
          </section>

          {/* ── Sección: Límite Diario ── */}
          <section className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-6 flex flex-col gap-5">
            <h2 className="text-base font-semibold text-white flex items-center gap-2">
              <span className="text-blue-400">📊</span> Límite Diario
            </h2>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-slate-300">Límite actual hoy</label>
                <div className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-blue-400 font-bold text-lg">
                  {cfg?.limiteDiarioActual}
                </div>
                <p className="text-xs text-slate-600">Modifícalo desde el Dashboard con el botón ➕</p>
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-slate-300">Factor de Incremento</label>
                <input
                  id="input-factor-incremento"
                  type="number"
                  min={1}
                  max={100}
                  value={form.factorIncremento}
                  onChange={e => handleChange('factorIncremento', parseInt(e.target.value) || 5)}
                  className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm
                             focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                             transition-all duration-200"
                />
                <p className="text-xs text-slate-600">Cuánto suma cada clic de ➕ Incrementar Límite.</p>
              </div>
            </div>
          </section>

          {/* ── Sección: WAHA Docker ── */}
          <section className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-6 flex flex-col gap-5">
            <h2 className="text-base font-semibold text-white flex items-center gap-2">
              <span className="text-green-400">🐳</span> Conexión WAHA Docker
            </h2>

            <div className="flex flex-col gap-1.5">
              <label className="text-sm font-medium text-slate-300">Endpoint URL</label>
              <input
                id="input-waha-url"
                type="text"
                value={form.wahaEndpointUrl}
                onChange={e => handleChange('wahaEndpointUrl', e.target.value)}
                placeholder="http://localhost:3000/api/sendText"
                className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm font-mono
                           focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                           transition-all duration-200"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-sm font-medium text-slate-300">Sesión WAHA</label>
              <input
                id="input-waha-session"
                type="text"
                value={form.wahaSession}
                onChange={e => handleChange('wahaSession', e.target.value)}
                placeholder="default"
                className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm
                           focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                           transition-all duration-200"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium text-slate-300">API Key (X-Api-Key)</label>
                <button
                  onClick={() => setShowKey(s => !s)}
                  className="text-xs text-slate-500 hover:text-slate-300 transition-colors"
                >
                  {showKey ? '🙈 Ocultar' : '👁 Mostrar'}
                </button>
              </div>
              <input
                id="input-waha-apikey"
                type={showKey ? 'text' : 'password'}
                value={form.wahaApiKey}
                onChange={e => handleChange('wahaApiKey', e.target.value)}
                placeholder="119ce04a85dd41818809be61aba87066"
                className="rounded-xl border border-slate-700 bg-slate-900/60 px-4 py-2.5 text-white text-sm font-mono
                           focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                           transition-all duration-200"
              />
              <p className="text-xs text-slate-600">
                La clave generada por WAHA al ejecutar <code className="text-slate-400">docker run ... init-waha</code>.
              </p>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
