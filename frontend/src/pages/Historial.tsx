import { useEffect, useState } from 'react';
import { getLotes, getDetallesLote, getNoRegistrados, exportarNoRegistrados } from '../services/api';
import type { LoteResumen, DetalleEnvio, NoRegistradoItem } from '../types';

type Vista = 'detalles' | 'no-registrados';

const ESTADOS_FILTRO = ['Todos', 'Pendiente', 'Procesado', 'Error', 'No Registrado'];

function estadoBadge(estado: string) {
  const map: Record<string, string> = {
    Pendiente:      'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
    'En Progreso':  'bg-blue-500/20 text-blue-300 border-blue-500/30',
    Procesado:      'bg-green-500/20 text-green-300 border-green-500/30',
    Completado:     'bg-green-500/20 text-green-300 border-green-500/30',
    Error:          'bg-red-500/20 text-red-300 border-red-500/30',
    'No Registrado':'bg-orange-500/20 text-orange-300 border-orange-500/30',
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

function formatFecha(iso: string | null) {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('es-PE', { timeZone: 'America/Lima' });
}

export default function Historial() {
  // ─── Vista activa ──────────────────────────────────────────────────────────
  const [vista, setVista] = useState<Vista>('detalles');

  // ─── Lotes ────────────────────────────────────────────────────────────────
  const [lotes, setLotes] = useState<LoteResumen[]>([]);
  const [loteSelec, setLoteSelec] = useState<LoteResumen | null>(null);
  const [loadingLotes, setLoadingLotes] = useState(true);

  // ─── Vista Detalles ────────────────────────────────────────────────────────
  const [detalles, setDetalles] = useState<DetalleEnvio[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [filtroEstado, setFiltroEstado] = useState('Todos');
  const [busqueda, setBusqueda] = useState('');
  const [loadingDet, setLoadingDet] = useState(false);
  const [expandMsg, setExpandMsg] = useState<number | null>(null);

  // ─── Vista No Registrados ──────────────────────────────────────────────────
  const [noReg, setNoReg] = useState<NoRegistradoItem[]>([]);
  const [totalNoReg, setTotalNoReg] = useState(0);
  const [paginaNoReg, setPaginaNoReg] = useState(1);
  const [filtroLoteNoReg, setFiltroLoteNoReg] = useState<string>('todos');
  const [busquedaNoReg, setBusquedaNoReg] = useState('');
  const [loadingNoReg, setLoadingNoReg] = useState(false);

  const TAMANO = 15;

  // ─── Cargar lista de lotes ────────────────────────────────────────────────
  useEffect(() => {
    getLotes()
      .then(data => {
        setLotes(data);
        if (data.length > 0) setLoteSelec(data[0]);
      })
      .catch(() => { })
      .finally(() => setLoadingLotes(false));
  }, []);

  // ─── Cargar detalles del lote seleccionado ────────────────────────────────
  useEffect(() => {
    if (!loteSelec || vista !== 'detalles') return;
    setLoadingDet(true);
    setPagina(1);
    getDetallesLote(
      loteSelec.id, 1, TAMANO,
      filtroEstado === 'Todos' ? undefined : filtroEstado,
      busqueda || undefined
    )
      .then(data => { setDetalles(data.items); setTotal(data.total); })
      .catch(() => { })
      .finally(() => setLoadingDet(false));
  }, [loteSelec, filtroEstado, busqueda, vista]);

  // ─── Cargar No Registrados ────────────────────────────────────────────────
  useEffect(() => {
    if (vista !== 'no-registrados') return;
    setLoadingNoReg(true);
    setPaginaNoReg(1);
    getNoRegistrados(
      1, TAMANO,
      filtroLoteNoReg === 'todos' ? undefined : filtroLoteNoReg,
      busquedaNoReg || undefined
    )
      .then(data => { setNoReg(data.items); setTotalNoReg(data.total); })
      .catch(() => { })
      .finally(() => setLoadingNoReg(false));
  }, [vista, filtroLoteNoReg, busquedaNoReg]);

  // ─── Polling automático (refresco cada 5s) ────────────────────────────────
  useEffect(() => {
    const id = setInterval(() => {
      getLotes().then(setLotes).catch(() => { });
      if (vista === 'detalles' && loteSelec) {
        getDetallesLote(
          loteSelec.id, pagina, TAMANO,
          filtroEstado === 'Todos' ? undefined : filtroEstado,
          busqueda || undefined
        ).then(data => { setDetalles(data.items); setTotal(data.total); }).catch(() => { });
      }
      if (vista === 'no-registrados') {
        getNoRegistrados(
          paginaNoReg, TAMANO,
          filtroLoteNoReg === 'todos' ? undefined : filtroLoteNoReg,
          busquedaNoReg || undefined
        ).then(data => { setNoReg(data.items); setTotalNoReg(data.total); }).catch(() => { });
      }
    }, 5000);
    return () => clearInterval(id);
  }, [vista, loteSelec, pagina, filtroEstado, busqueda, paginaNoReg, filtroLoteNoReg, busquedaNoReg]);

  // ─── Paginación detalles ──────────────────────────────────────────────────
  const cambiarPagina = async (nuevaPagina: number) => {
    if (!loteSelec) return;
    setLoadingDet(true);
    try {
      const data = await getDetallesLote(
        loteSelec.id, nuevaPagina, TAMANO,
        filtroEstado === 'Todos' ? undefined : filtroEstado,
        busqueda || undefined
      );
      setDetalles(data.items);
      setTotal(data.total);
      setPagina(nuevaPagina);
    } catch { /* noop */ }
    finally { setLoadingDet(false); }
  };

  // ─── Paginación no registrados ────────────────────────────────────────────
  const cambiarPaginaNoReg = async (nuevaPagina: number) => {
    setLoadingNoReg(true);
    try {
      const data = await getNoRegistrados(
        nuevaPagina, TAMANO,
        filtroLoteNoReg === 'todos' ? undefined : filtroLoteNoReg,
        busquedaNoReg || undefined
      );
      setNoReg(data.items);
      setTotalNoReg(data.total);
      setPaginaNoReg(nuevaPagina);
    } catch { /* noop */ }
    finally { setLoadingNoReg(false); }
  };

  const totalPaginas      = Math.max(1, Math.ceil(total / TAMANO));
  const totalPaginasNoReg = Math.max(1, Math.ceil(totalNoReg / TAMANO));

  // ─── Render ───────────────────────────────────────────────────────────────
  return (
    <div className="flex flex-col gap-6">
      {/* Encabezado */}
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">📋 Historial de Lotes</h1>
          <p className="text-slate-500 text-sm mt-1">
            Consulta el estado de cada envío procesado por el motor WAHA.
          </p>
        </div>

        {/* Tabs de vista */}
        <div className="flex rounded-xl overflow-hidden border border-slate-700/60 text-sm font-medium">
          <button
            onClick={() => setVista('detalles')}
            className={`px-4 py-2 transition-colors ${vista === 'detalles'
              ? 'bg-blue-500/20 text-blue-300 border-r border-slate-700/60'
              : 'bg-slate-800/40 text-slate-400 hover:text-white border-r border-slate-700/60'}`}
          >
            📋 Por Lote
          </button>
          <button
            onClick={() => setVista('no-registrados')}
            className={`px-4 py-2 transition-colors flex items-center gap-2 ${vista === 'no-registrados'
              ? 'bg-orange-500/15 text-orange-300'
              : 'bg-slate-800/40 text-slate-400 hover:text-white'}`}
          >
            📵 No Registrados
            {totalNoReg > 0 && (
              <span className="px-1.5 py-0.5 rounded-full text-xs bg-orange-500/30 text-orange-200">
                {totalNoReg}
              </span>
            )}
          </button>
        </div>
      </div>

      {loadingLotes ? (
        <div className="text-slate-400 animate-pulse">Cargando lotes...</div>
      ) : lotes.length === 0 ? (
        <div className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-10 text-center text-slate-500">
          No hay lotes importados todavía. Ve a <b>Importar</b> para cargar tu primer archivo.
        </div>
      ) : vista === 'detalles' ? (

        // ════════════════════════════════════════════════════════════════
        // VISTA: POR LOTE
        // ════════════════════════════════════════════════════════════════
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
                    {new Date(l.fechaImportacion).toLocaleString('es-PE', { timeZone: 'America/Lima' })}
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
                          ? e === 'No Registrado'
                            ? 'border-orange-500/60 bg-orange-500/20 text-orange-300'
                            : 'border-blue-500/60 bg-blue-500/20 text-blue-300'
                          : 'border-slate-700 bg-slate-800/60 text-slate-400 hover:text-white'}`}
                    >
                      {e === 'No Registrado' ? '📵 ' : ''}{e}
                    </button>
                  ))}
                  <div className="ml-4 relative">
                    <input
                      type="text"
                      value={busqueda}
                      onChange={e => setBusqueda(e.target.value)}
                      placeholder="Buscar nombre, número, documento o fecha..."
                      className="px-3 py-1.5 rounded-lg text-xs bg-slate-800/60 border border-slate-700 text-slate-300 placeholder-slate-500 focus:outline-none focus:border-blue-500/50 min-w-[220px]"
                    />
                  </div>
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
                          <th className="text-left px-4 py-2.5 text-purple-400 font-medium">Documento</th>
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
                            className={`border-b border-slate-800/60 transition-colors
                              ${d.esNumeroNoRegistrado
                                ? 'bg-orange-500/5 hover:bg-orange-500/10'
                                : 'hover:bg-slate-800/40'}`}
                          >
                            <td className="px-4 py-2.5 text-slate-600 text-xs">
                              {(pagina - 1) * TAMANO + i + 1}
                            </td>
                            <td className="px-4 py-2.5 text-slate-300 font-mono text-xs">
                              {d.numeroCelular}
                              {d.esNumeroNoRegistrado && (
                                <span title="Número no registrado en WhatsApp" className="ml-1 text-orange-400">📵</span>
                              )}
                            </td>
                            <td className="px-4 py-2.5 text-slate-300">{d.nombreCliente}</td>
                            <td className="px-4 py-2.5 text-purple-300 font-mono text-xs">
                              {d.documento || <span className="text-slate-600">—</span>}
                            </td>
                            <td className="px-4 py-2.5">
                              <span className={`px-2 py-0.5 rounded-full text-xs border ${estadoBadge(
                                d.esNumeroNoRegistrado ? 'No Registrado' : d.estado
                              )}`}>
                                {d.esNumeroNoRegistrado ? '📵 No Reg.' : d.estado}
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
                              {formatFecha(d.fechaProcesado)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {/* Paginación */}
                {totalPaginas > 1 && (
                  <PaginacionControl
                    pagina={pagina}
                    totalPaginas={totalPaginas}
                    loading={loadingDet}
                    onCambiar={cambiarPagina}
                  />
                )}
              </>
            )}
          </div>
        </div>

      ) : (

        // ════════════════════════════════════════════════════════════════
        // VISTA: NÚMEROS NO REGISTRADOS (AUDITORÍA)
        // ════════════════════════════════════════════════════════════════
        <div className="flex flex-col gap-4">

          {/* Encabezado del panel */}
          <div className="rounded-2xl border border-orange-500/30 bg-orange-500/5 p-4 flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 className="text-orange-300 font-semibold">📵 Números No Registrados en WhatsApp</h2>
              <p className="text-slate-500 text-xs mt-1">
                Contactos cuyo número no existe en WhatsApp según la respuesta de WAHA.
                Úsalos para actualizar tu base de datos.
              </p>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-sm text-slate-400">
                <span className="text-orange-300 font-bold">{totalNoReg}</span> números no registrados
              </span>
              <button
                id="btn-exportar-no-registrados"
                onClick={() => exportarNoRegistrados(
                  filtroLoteNoReg === 'todos' ? undefined : filtroLoteNoReg,
                  busquedaNoReg || undefined
                )}
                className="flex items-center gap-1.5 px-4 py-2 rounded-xl border border-orange-500/40
                           bg-orange-500/15 text-orange-300 text-sm font-medium
                           hover:bg-orange-500/25 transition-all duration-200 active:scale-95"
              >
                ⬇ Descargar CSV
              </button>
            </div>
          </div>

          {/* Filtros */}
          <div className="flex items-center gap-3 flex-wrap">
            {/* Filtro por lote */}
            <div className="flex items-center gap-2">
              <span className="text-xs text-slate-500">Lote:</span>
              <select
                value={filtroLoteNoReg}
                onChange={e => setFiltroLoteNoReg(e.target.value)}
                className="px-3 py-1.5 rounded-lg text-xs bg-slate-800/60 border border-slate-700 text-slate-300
                           focus:outline-none focus:border-orange-500/50"
              >
                <option value="todos">Todos los lotes</option>
                {lotes.map(l => (
                  <option key={l.id} value={l.id}>{l.nombreArchivo}</option>
                ))}
              </select>
            </div>

            {/* Búsqueda */}
            <input
              type="text"
              value={busquedaNoReg}
              onChange={e => setBusquedaNoReg(e.target.value)}
              placeholder="Buscar nombre, número o documento..."
              className="px-3 py-1.5 rounded-lg text-xs bg-slate-800/60 border border-slate-700 text-slate-300
                         placeholder-slate-500 focus:outline-none focus:border-orange-500/50 min-w-[220px]"
            />

            <span className="ml-auto text-xs text-slate-500">{totalNoReg} registros</span>
          </div>

          {/* Tabla */}
          {loadingNoReg ? (
            <div className="text-slate-400 animate-pulse py-6 text-center">Cargando...</div>
          ) : noReg.length === 0 ? (
            <div className="rounded-xl border border-slate-700/60 bg-slate-800/40 p-12 text-center">
              <div className="text-4xl mb-3">📵</div>
              <p className="text-slate-400 font-medium">No hay números no registrados</p>
              <p className="text-slate-600 text-sm mt-1">
                {filtroLoteNoReg !== 'todos' || busquedaNoReg
                  ? 'Prueba cambiando los filtros.'
                  : 'Los números sin WhatsApp aparecerán aquí automáticamente al procesarse.'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-slate-700/60">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-700/60 bg-slate-800/60">
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">#</th>
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Número</th>
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Nombre</th>
                    <th className="text-left px-4 py-2.5 text-purple-400 font-medium">Documento</th>
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Lote / Archivo</th>
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Error WAHA</th>
                    <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Fecha</th>
                  </tr>
                </thead>
                <tbody>
                  {noReg.map((nr, i) => (
                    <tr
                      key={nr.id}
                      className="border-b border-slate-800/60 hover:bg-orange-500/5 transition-colors"
                    >
                      <td className="px-4 py-2.5 text-slate-600 text-xs">
                        {(paginaNoReg - 1) * TAMANO + i + 1}
                      </td>
                      <td className="px-4 py-2.5 text-orange-300 font-mono text-xs">
                        {nr.numeroCelular}
                      </td>
                      <td className="px-4 py-2.5 text-slate-300">{nr.nombreCliente}</td>
                      <td className="px-4 py-2.5 text-purple-300 font-mono text-xs">
                        {nr.documento || <span className="text-slate-600">—</span>}
                      </td>
                      <td className="px-4 py-2.5 text-slate-400 text-xs max-w-[160px] truncate" title={nr.nombreArchivo}>
                        {nr.nombreArchivo}
                      </td>
                      <td className="px-4 py-2.5 text-red-400 text-xs max-w-[200px]">
                        <span className="truncate block" title={nr.mensajeError ?? ''}>
                          {nr.mensajeError ? nr.mensajeError.slice(0, 60) + (nr.mensajeError.length > 60 ? '…' : '') : '—'}
                        </span>
                      </td>
                      <td className="px-4 py-2.5 text-slate-600 text-xs whitespace-nowrap">
                        {formatFecha(nr.fechaProcesado)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Paginación No Registrados */}
          {totalPaginasNoReg > 1 && (
            <PaginacionControl
              pagina={paginaNoReg}
              totalPaginas={totalPaginasNoReg}
              loading={loadingNoReg}
              onCambiar={cambiarPaginaNoReg}
            />
          )}
        </div>
      )}
    </div>
  );
}

// ─── Componente de paginación reutilizable ───────────────────────────────────

function PaginacionControl({
  pagina, totalPaginas, loading, onCambiar
}: {
  pagina: number;
  totalPaginas: number;
  loading: boolean;
  onCambiar: (p: number) => void;
}) {
  return (
    <div className="flex items-center justify-between text-sm">
      <span className="text-slate-500 text-xs">
        Página {pagina} de {totalPaginas}
      </span>
      <div className="flex gap-2">
        <button
          onClick={() => onCambiar(pagina - 1)}
          disabled={pagina <= 1 || loading}
          className="px-3 py-1.5 rounded-lg border border-slate-700 text-slate-400
                     hover:text-white hover:border-slate-500 disabled:opacity-30
                     disabled:cursor-not-allowed transition-all duration-150 text-xs"
        >
          ← Anterior
        </button>
        {Array.from({ length: Math.min(5, totalPaginas) }, (_, idx) => {
          const start = Math.max(1, Math.min(pagina - 2, totalPaginas - 4));
          const p = start + idx;
          return (
            <button
              key={p}
              onClick={() => onCambiar(p)}
              disabled={loading}
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
          onClick={() => onCambiar(pagina + 1)}
          disabled={pagina >= totalPaginas || loading}
          className="px-3 py-1.5 rounded-lg border border-slate-700 text-slate-400
                     hover:text-white hover:border-slate-500 disabled:opacity-30
                     disabled:cursor-not-allowed transition-all duration-150 text-xs"
        >
          Siguiente →
        </button>
      </div>
    </div>
  );
}
