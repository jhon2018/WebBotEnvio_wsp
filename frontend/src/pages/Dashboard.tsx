import { useEffect, useState } from 'react';
import MetricCard from '../components/MetricCard';
import {
  getMetricas, getPlantillas, guardarPlantillasBatch,
  incrementarLimite, toggleEnvio, reintentarFallidos,
} from '../services/api';
import type { Metricas, Plantilla } from '../types';

export default function Dashboard() {
  const [metricas,   setMetricas]   = useState<Metricas | null>(null);
  const [plantillas, setPlantillas] = useState<Plantilla[]>([]);
  const [editadas,   setEditadas]   = useState<Record<number, string>>({});
  const [loading,    setLoading]    = useState(true);
  const [saving,     setSaving]     = useState(false);
  const [toggling,   setToggling]   = useState(false);
  const [msg,        setMsg]        = useState<{ text: string; ok: boolean } | null>(null);

  // ─── Carga inicial ─────────────────────────────────────────────────────────
  useEffect(() => {
    Promise.all([getMetricas(), getPlantillas()])
      .then(([m, p]) => { setMetricas(m); setPlantillas(p); })
      .finally(() => setLoading(false));
  }, []);

  // Polling de métricas cada 8 segundos para mantener las tarjetas actualizadas.
  useEffect(() => {
    const id = setInterval(() => {
      getMetricas().then(setMetricas).catch(() => {});
    }, 8000);
    return () => clearInterval(id);
  }, []);

  // ─── Handlers ──────────────────────────────────────────────────────────────

  const handleIncrementarLimite = async () => {
    try {
      const cfg = await incrementarLimite();
      setMetricas(prev => prev ? { ...prev, limiteMaximoDia: cfg.limiteDiarioActual } : prev);
      flash(`✅ Límite actualizado a ${cfg.limiteDiarioActual} mensajes/día`, true);
    } catch { flash('❌ Error al incrementar el límite', false); }
  };

  const handleToggleEnvio = async () => {
    setToggling(true);
    try {
      const cfg = await toggleEnvio();
      setMetricas(prev => prev ? { ...prev, modoEnvioActivo: cfg.modoEnvioActivo } : prev);
      flash(cfg.modoEnvioActivo ? '▶️ Motor de envío ACTIVADO' : '⏸ Motor de envío PAUSADO', true);
    } catch { flash('❌ Error al cambiar el estado del motor', false); }
    finally { setToggling(false); }
  };

  const handleReintentarFallidos = async () => {
    try {
      const res = await reintentarFallidos();
      flash(`🔄 ${res.mensaje}`, true);
      getMetricas().then(setMetricas);
    } catch { flash('❌ Error al reintentar fallidos', false); }
  };

  const handleGuardarPlantillas = async () => {
    if (Object.keys(editadas).length === 0) return;
    setSaving(true);
    try {
      const dtos = Object.entries(editadas).map(([id, texto]) => ({
        id: Number(id),
        cuerpoTexto: texto,
        activo: plantillas.find(p => p.id === Number(id))?.activo ?? true,
      }));
      const actualizadas = await guardarPlantillasBatch(dtos);
      setPlantillas(actualizadas);
      setEditadas({});
      flash('✅ Plantillas guardadas correctamente', true);
    } catch { flash('❌ Error al guardar las plantillas', false); }
    finally { setSaving(false); }
  };

  const flash = (text: string, ok: boolean) => {
    setMsg({ text, ok });
    setTimeout(() => setMsg(null), 4000);
  };

  // ─── Render ────────────────────────────────────────────────────────────────

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400 animate-pulse text-lg">Cargando dashboard...</div>
      </div>
    );
  }

  const porcentaje = metricas
    ? Math.min(100, Math.round((metricas.enviadosHoy / (metricas.limiteMaximoDia || 1)) * 100))
    : 0;

  return (
    <div className="flex flex-col gap-8">
      {/* ── Flash mensaje ── */}
      {msg && (
        <div className={`fixed top-4 right-4 z-50 px-5 py-3 rounded-xl shadow-xl text-sm font-medium
                         transition-all duration-300 animate-fade-in
                         ${msg.ok ? 'bg-green-500/20 border border-green-500/40 text-green-300'
                                  : 'bg-red-500/20 border border-red-500/40 text-red-300'}`}>
          {msg.text}
        </div>
      )}

      {/* ── Encabezado ── */}
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard</h1>
          <p className="text-slate-500 text-sm">Control del motor de envío WAHA</p>
        </div>

        {/* Botón Play / Pause */}
        <button
          id="btn-toggle-envio"
          onClick={handleToggleEnvio}
          disabled={toggling}
          className={`
            px-6 py-3 rounded-xl font-semibold text-sm transition-all duration-200
            flex items-center gap-2 shadow-lg active:scale-95
            ${metricas?.modoEnvioActivo
              ? 'bg-red-500/20 border border-red-500/40 text-red-300 hover:bg-red-500/30'
              : 'bg-green-500/20 border border-green-500/40 text-green-300 hover:bg-green-500/30'}
            ${toggling ? 'opacity-60 cursor-not-allowed' : ''}
          `}
        >
          {toggling ? '⏳' : metricas?.modoEnvioActivo ? '⏸ Pausar Envíos' : '▶️ Iniciar Envíos'}
        </button>
      </div>

      {/* ── Tarjetas de métricas ── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <MetricCard
          title="Enviados Hoy"
          value={`${metricas?.enviadosHoy ?? 0} / ${metricas?.limiteMaximoDia ?? 0}`}
          subtitle={`${porcentaje}% del límite diario`}
          icon="📤"
          color="green"
        >
          {/* Barra de progreso */}
          <div className="w-full bg-slate-700/60 rounded-full h-1.5 mt-1">
            <div
              className="bg-green-400 h-1.5 rounded-full transition-all duration-700"
              style={{ width: `${porcentaje}%` }}
            />
          </div>
        </MetricCard>

        <MetricCard
          title="Límite Diario"
          value={metricas?.limiteMaximoDia ?? 0}
          subtitle="Mensajes máximos por día"
          icon="📊"
          color="blue"
        >
          <button
            id="btn-incrementar-limite"
            onClick={handleIncrementarLimite}
            className="mt-1 w-full py-1.5 rounded-lg bg-blue-500/20 border border-blue-500/30
                       text-blue-300 text-xs font-medium hover:bg-blue-500/30 transition-all duration-200 active:scale-95"
          >
            ➕ Incrementar Límite
          </button>
        </MetricCard>

        <MetricCard
          title="En Cola"
          value={metricas?.mensajesEnCola ?? 0}
          subtitle="Mensajes Pendientes"
          icon="⏳"
          color="yellow"
        />

        <MetricCard
          title="Con Error"
          value={metricas?.mensajesConError ?? 0}
          subtitle="Envíos fallidos"
          icon="❌"
          color="red"
        >
          {(metricas?.mensajesConError ?? 0) > 0 && (
            <button
              id="btn-reintentar-fallidos"
              onClick={handleReintentarFallidos}
              className="mt-1 w-full py-1.5 rounded-lg bg-red-500/20 border border-red-500/30
                         text-red-300 text-xs font-medium hover:bg-red-500/30 transition-all duration-200 active:scale-95"
            >
              🔄 Reintentar Fallidos
            </button>
          )}
        </MetricCard>
      </div>

      {/* ── Lote activo ── */}
      {metricas?.loteActivo && (
        <div className="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-5">
          <h2 className="text-sm font-semibold text-purple-300 mb-3">🗂 Lote Activo</h2>
          <div className="flex flex-wrap gap-4 text-sm text-slate-300">
            <span>📄 <b>{metricas.loteActivo.nombreArchivo}</b></span>
            <span>👥 {metricas.loteActivo.totalRegistros} contactos</span>
            <span className={`px-2 py-0.5 rounded-lg text-xs font-medium ${
              metricas.loteActivo.estado === 'En Progreso'
                ? 'bg-yellow-500/20 text-yellow-300'
                : 'bg-slate-500/20 text-slate-400'
            }`}>{metricas.loteActivo.estado}</span>
          </div>
        </div>
      )}

      {/* ── Editor de plantillas ── */}
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-white">📝 Plantillas de Mensajes</h2>
          <button
            id="btn-guardar-plantillas"
            onClick={handleGuardarPlantillas}
            disabled={saving || Object.keys(editadas).length === 0}
            className={`
              px-5 py-2 rounded-xl text-sm font-semibold transition-all duration-200 active:scale-95
              ${Object.keys(editadas).length > 0
                ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20'
                : 'bg-slate-700 text-slate-500 cursor-not-allowed'}
              ${saving ? 'opacity-60' : ''}
            `}
          >
            {saving ? '⏳ Guardando...' : '💾 Guardar Cambios'}
          </button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {plantillas.map(p => (
            <div key={p.id} className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-4 flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-500 uppercase tracking-wide">
                    Plantilla {p.indice}
                  </span>
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                    p.tipo === 'prestamo'  ? 'bg-blue-500/20 text-blue-300' :
                    p.tipo === 'tarjeta'   ? 'bg-purple-500/20 text-purple-300' :
                                             'bg-green-500/20 text-green-300'
                  }`}>{p.tipo}</span>
                </div>
                <span className={`text-xs ${p.activo ? 'text-green-400' : 'text-slate-600'}`}>
                  {p.activo ? '● Activa' : '○ Inactiva'}
                </span>
              </div>
              <textarea
                id={`plantilla-${p.id}`}
                rows={5}
                value={editadas[p.id] ?? p.cuerpoTexto}
                onChange={e => setEditadas(prev => ({ ...prev, [p.id]: e.target.value }))}
                className="w-full rounded-xl bg-slate-900/60 border border-slate-700 px-3 py-2.5
                           text-slate-300 text-sm resize-none font-mono leading-relaxed
                           focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-transparent
                           transition-all duration-200 placeholder:text-slate-600"
                placeholder="Escribe el mensaje aquí... Usa {Nombre} para personalizar."
              />
              {(editadas[p.id] !== undefined && editadas[p.id] !== p.cuerpoTexto) && (
                <p className="text-xs text-yellow-400/80">✏️ Sin guardar</p>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
