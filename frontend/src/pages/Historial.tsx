import { useEffect, useState } from 'react';
import { getLotes, getDetallesLote } from '../services/api';
import type { LoteResumen, DetalleEnvio } from '../types';

const ESTADOS_FILTRO = ['Todos', 'Pendiente', 'Procesado', 'Error'];

function estadoBadge(estado: string) {
  const map: Record<string, string> = {
    Pendiente:    'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
    'En Progreso':'bg-blue-500/20 text-blue-300 border-blue-500/30',
    Procesado:    'bg-green-500/20 text-green-300 border-green-500/30',
    Completado:   'bg-green-500/20 text-green-300 border-green-500/30',
    Error:        'bg-red-500/20 text-red-300 border-red-500/30',
  };
  return map[estado] ?? 'bg-slate-500/20 text-slate-300 border-slate-500/30';
}

function ackLabel(code: number | null) {
  if (code === null) return '—';
  if (code === 0) return '⏳ Enviado';
  if (code === 1) return '✅ Recibido';
  if (code === 2) return '✅✅ Leído';
  return `ACK ${code}`;
}

export default function Historial() {
  const [lotes,        setLotes]        = useState<LoteResumen[]>([]);
  const [loteSelec,    setLoteSelec]    = useState<LoteResumen | null>(null);
  const [detalles,     setDetalles]     = useState<DetalleEnvio[]>([]);
  const [total,        setTotal]        = useState(0);
  const [pagina,       setPagina]       = useState(1);
  const [filtroEstado, setFiltroEstado] = useState('Todos');
  const [loadingLotes, setLoadingLotes] = useState(true);
  const [loadingDet,   setLoadingDet]   = useState(false);
  const [expandMsg,    setExpandMsg]    = useState<number | null>(null);

  const TAMANO = 15;

  // ─── Cargar lista de lotes ────────────────────────────────────────────────
  useEffect(() => {
    getLotes()
      .then(data => {
        setLotes(data);
        if (data.length > 0) setLoteSelec(data[0]);
      })
      .catch(() => {})
      .finally(() => setLoadingLotes(false));
  }, []);

  // ─── Cargar detalles del lote seleccionado ────────────────────────────────
  useEffect(() => {
    if (!loteSelec) return;
    setLoadingDet(true);
    setPagina(1);
    getDetallesLote(
      loteSelec.id,
      1,
      TAMANO,
      filtroEstado === 'Todos' ? undefined : filtroEstado,
    )
      .then(data => { setDetalles(data.items); setTotal(data.total); })
      .catch(() => {})
      .finally(() => setLoadingDet(false));
  }, [loteSelec, filtroEstado]);

  // ─── Polling automático (refresco cada 5s sin loader visible) ─────────────
  useEffect(() => {
    const id = setInterval(() => {
      // 1. Refrescar lista de lotes (Dashboard izquierdo)
      getLotes().then(setLotes).catch(() => {});

      // 2. Refrescar detalles del lote seleccionado (Dashboard derecho)
      if (loteSelec) {
        getDetallesLote(
          loteSelec.id,
          pagina,
          TAMANO,
          filtroEstado === 'Todos' ? undefined : filtroEstado
        )
          .then(data => { setDetalles(data.items); setTotal(data.total); })
          .catch(() => {});
      }
    }, 5000); // 5 segundos

    return () => clearInterval(id);
  }, [loteSelec, pagina, filtroEstado]);

  const cambiarPagina = async (nuevaPagina: number) => {
    if (!loteSelec) return;
    setLoadingDet(true);
    try {
      const data = await getDetallesLote(
        loteSelec.id,
        nuevaPagina,
        TAMANO,
        filtroEstado === 'Todos' ? undefined : filtroEstado,
      );
      setDetalles(data.items);
      setTotal(data.total);
      setPagina(nuevaPagina);
    } catch { /* noop */ }
    finally { setLoadingDet(false); }
  };

  const totalPaginas = Math.max(1, Math.ceil(total / TAMANO));

  // ─── Render ───────────────────────────────────────────────────────────────
  return (
    <div className="flex flex-col gap-6">
      {/* Encabezado */}
      <div>
        <h1 className="text-2xl font-bold text-white">📋 Historial de Lotes</h1>
        <p className="text-slate-500 text-sm mt-1">
          Consulta el estado de cada envío procesado por el motor WAHA.
        </p>
      </div>

      {loadingLotes ? (
        <div className="text-slate-400 animate-pulse">Cargando lotes...</div>
      ) : lotes.length === 0 ? (
        <div className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-10 text-center text-slate-500">
          No hay lotes importados todavía. Ve a <b>Importar</b> para cargar tu primer archivo.
        </div>
      ) : (
        <div className="flex flex-col xl:flex-row gap-6">

          {/* ── Panel izquierdo: lista de lotes ── */}
          <div className="xl:w-72 flex-shrink-0 flex flex-col gap-2">
            <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-wide px-1">
              Lotes ({lotes.length})
            </h2>
            <div className="flex flex-col gap-1 max-h-[70vh] overflow-y-auto pr-1">
              {lotes.map(l => (
                <button
                  key={l.id}
                  onClick={() => setLoteSelec(l)}
                  className={`text-left rounded-xl border px-4 py-3 transition-all duration-200
                    ${loteSelec?.id === l.id
                      ? 'border-blue-500/50 bg-blue-500/10'
                      : 'border-slate-700/60 bg-slate-800/40 hover:border-slate-600'}
                  `}
                >
                  <p className="text-sm font-medium text-white truncate">{l.nombreArchivo}</p>
                  <div className="flex items-center gap-2 mt-1">
                    <span className={`px-2 py-0.5 rounded-full text-xs border ${estadoBadge(l.estado)}`}>
                      {l.estado}
                    </span>
                    <span className="text-xs text-slate-500">{l.totalRegistros} contactos</span>
                  </div>
                  <p className="text-xs text-slate-600 mt-1">
                    {new Date(l.fechaImportacion).toLocaleString('es-PE')}
                  </p>
                </button>
              ))}
            </div>
          </div>

          {/* ── Panel derecho: detalle del lote ── */}
          <div className="flex-1 flex flex-col gap-4 min-w-0">

            {loteSelec && (
              <>
                {/* Info del lote */}
                <div className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-4 flex flex-wrap gap-4 text-sm">
                  <div>
                    <span className="text-slate-500">Archivo:</span>{' '}
                    <span className="text-white font-medium">{loteSelec.nombreArchivo}</span>
                  </div>
                  <div>
                    <span className="text-slate-500">País:</span>{' '}
                    <span className="text-white">+{loteSelec.codigoPais}</span>
                  </div>
                  <div>
                    <span className="text-slate-500">Total:</span>{' '}
                    <span className="text-white">{loteSelec.totalRegistros}</span>
                  </div>
                  <div>
                    <span className="text-slate-500">Saltados:</span>{' '}
                    <span className="text-yellow-400">{loteSelec.registrosSaltados}</span>
                  </div>
                  <div>
                    <span className={`px-2 py-0.5 rounded-full text-xs border ${estadoBadge(loteSelec.estado)}`}>
                      {loteSelec.estado}
                    </span>
                  </div>
                </div>

                {/* Filtros */}
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-xs text-slate-500 mr-1">Filtrar:</span>
                  {ESTADOS_FILTRO.map(e => (
                    <button
                      key={e}
                      onClick={() => setFiltroEstado(e)}
                      className={`px-3 py-1 rounded-lg text-xs font-medium border transition-all duration-150
                        ${filtroEstado === e
                          ? 'border-blue-500/60 bg-blue-500/20 text-blue-300'
                          : 'border-slate-700 bg-slate-800/60 text-slate-400 hover:text-white'}`}
                    >
                      {e}
                    </button>
                  ))}
                  <span className="ml-auto text-xs text-slate-500">
                    {total} registros
                  </span>
                </div>

                {/* Tabla */}
                {loadingDet ? (
                  <div className="text-slate-400 animate-pulse py-6 text-center">Cargando detalles...</div>
                ) : detalles.length === 0 ? (
                  <div className="rounded-xl border border-slate-700/60 bg-slate-800/40 p-8 text-center text-slate-500">
                    No hay registros con ese filtro.
                  </div>
                ) : (
                  <div className="overflow-x-auto rounded-xl border border-slate-700/60">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-slate-700/60 bg-slate-800/60">
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">#</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Número</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Nombre</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Estado</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">ACK</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Mensaje</th>
                          <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Procesado</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detalles.map((d, i) => (
                          <tr
                            key={d.id}
                            className="border-b border-slate-800/60 hover:bg-slate-800/40 transition-colors"
                          >
                            <td className="px-4 py-2.5 text-slate-600 text-xs">
                              {(pagina - 1) * TAMANO + i + 1}
                            </td>
                            <td className="px-4 py-2.5 text-slate-300 font-mono text-xs">
                              {d.numeroCelular}
                            </td>
                            <td className="px-4 py-2.5 text-slate-300">
                              {d.nombreCliente}
                            </td>
                            <td className="px-4 py-2.5">
                              <span className={`px-2 py-0.5 rounded-full text-xs border ${estadoBadge(d.estado)}`}>
                                {d.estado}
                              </span>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-slate-400">
                              {ackLabel(d.wahaAckCode)}
                            </td>
                            <td className="px-4 py-2.5 text-slate-400 max-w-[180px]">
                              {d.mensajeError ? (
                                <span className="text-red-400 text-xs">{d.mensajeError}</span>
                              ) : d.mensajeAsignado ? (
                                <button
                                  onClick={() => setExpandMsg(expandMsg === d.id ? null : d.id)}
                                  className="text-xs text-blue-400 hover:text-blue-300 transition-colors text-left"
                                >
                                  {expandMsg === d.id
                                    ? d.mensajeAsignado
                                    : d.mensajeAsignado.slice(0, 40) + (d.mensajeAsignado.length > 40 ? '…' : '')}
                                </button>
                              ) : (
                                <span className="text-slate-600 text-xs">—</span>
                              )}
                            </td>
                            <td className="px-4 py-2.5 text-slate-600 text-xs whitespace-nowrap">
                              {d.fechaProcesado
                                ? new Date(d.fechaProcesado).toLocaleString('es-PE')
                                : '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {/* Paginación */}
                {totalPaginas > 1 && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-slate-500 text-xs">
                      Página {pagina} de {totalPaginas}
                    </span>
                    <div className="flex gap-2">
                      <button
                        onClick={() => cambiarPagina(pagina - 1)}
                        disabled={pagina <= 1 || loadingDet}
                        className="px-3 py-1.5 rounded-lg border border-slate-700 text-slate-400
                                   hover:text-white hover:border-slate-500 disabled:opacity-30
                                   disabled:cursor-not-allowed transition-all duration-150 text-xs"
                      >
                        ← Anterior
                      </button>
                      {/* Números de página (máx 5 visibles) */}
                      {Array.from({ length: Math.min(5, totalPaginas) }, (_, idx) => {
                        const start = Math.max(1, Math.min(pagina - 2, totalPaginas - 4));
                        const p = start + idx;
                        return (
                          <button
                            key={p}
                            onClick={() => cambiarPagina(p)}
                            disabled={loadingDet}
                            className={`px-3 py-1.5 rounded-lg text-xs border transition-all duration-150
                              ${p === pagina
                                ? 'border-blue-500/60 bg-blue-500/20 text-blue-300'
                                : 'border-slate-700 text-slate-400 hover:text-white'}`}
                          >
                            {p}
                          </button>
                        );
                      })}
                      <button
                        onClick={() => cambiarPagina(pagina + 1)}
                        disabled={pagina >= totalPaginas || loadingDet}
                        className="px-3 py-1.5 rounded-lg border border-slate-700 text-slate-400
                                   hover:text-white hover:border-slate-500 disabled:opacity-30
                                   disabled:cursor-not-allowed transition-all duration-150 text-xs"
                      >
                        Siguiente →
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
