import { useEffect, useRef, useState } from 'react';
import {
  getConfiguracion,
  actualizarConfiguracion,
  getImagenes,
  subirImagen,
  eliminarImagen,
} from '../services/api';
import type { Configuracion, ActualizarConfiguracionDto, ImagenInfo } from '../types';

const MAX_IMAGENES = 5;

export default function Configuracion() {
  const [cfg,        setCfg]        = useState<Configuracion | null>(null);
  const [form,       setForm]       = useState<ActualizarConfiguracionDto | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [saving,     setSaving]     = useState(false);
  const [msg,        setMsg]        = useState<{ text: string; ok: boolean } | null>(null);
  const [showKey,    setShowKey]    = useState(false);

  // ── Galería de imágenes ──────────────────────────────────────────────────
  const [imagenes,      setImagenes]      = useState<ImagenInfo[]>([]);
  const [cargandoImgs,  setCargandoImgs]  = useState(true);
  const [subiendoImg,   setSubiendoImg]   = useState(false);
  const [eliminandoImg, setEliminandoImg] = useState<string | null>(null);
  const inputFileRef = useRef<HTMLInputElement>(null);

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

    // Cargar imágenes existentes al montar
    cargarImagenes();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Helpers ──────────────────────────────────────────────────────────────

  const flash = (text: string, ok: boolean) => {
    setMsg({ text, ok });
    setTimeout(() => setMsg(null), 4000);
  };

  const cargarImagenes = () => {
    setCargandoImgs(true);
    getImagenes()
      .then(setImagenes)
      .catch(() => flash('❌ No se pudieron cargar las imágenes', false))
      .finally(() => setCargandoImgs(false));
  };

  const handleChange = (key: keyof ActualizarConfiguracionDto, value: string | number) => {
    setForm(prev => prev ? { ...prev, [key]: value } : prev);
  };

  const handleGuardar = async () => {
    if (!form) return;
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

  // ── Imagen: subir ─────────────────────────────────────────────────────────
  const handleSeleccionarArchivo = () => inputFileRef.current?.click();

  const handleArchivoSeleccionado = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const archivo = e.target.files?.[0];
    if (!archivo) return;

    if (imagenes.length >= MAX_IMAGENES) {
      flash(`❌ Límite de ${MAX_IMAGENES} imágenes alcanzado. Elimina una primero.`, false);
      return;
    }

    setSubiendoImg(true);
    try {
      const nueva = await subirImagen(archivo);
      setImagenes(prev => {
        const idx = prev.findIndex(i => i.nombre === nueva.nombre);
        if (idx >= 0) {
          const copia = [...prev];
          copia[idx] = nueva;
          return copia;
        }
        return [...prev, nueva];
      });
      flash(`✅ "${nueva.nombre}" subida correctamente`, true);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: string | { detail?: string } } })?.response?.data;
      flash(`❌ ${typeof msg === 'string' ? msg : 'Error al subir la imagen'}`, false);
    } finally {
      setSubiendoImg(false);
      // Resetear input para permitir subir el mismo archivo de nuevo
      if (inputFileRef.current) inputFileRef.current.value = '';
    }
  };

  // ── Imagen: eliminar ──────────────────────────────────────────────────────
  const handleEliminar = async (nombre: string) => {
    setEliminandoImg(nombre);
    try {
      await eliminarImagen(nombre);
      setImagenes(prev => prev.filter(i => i.nombre !== nombre));
      flash(`🗑 "${nombre}" eliminada`, true);
    } catch {
      flash('❌ Error al eliminar la imagen', false);
    } finally {
      setEliminandoImg(null);
    }
  };

  // ── Helpers de UI ─────────────────────────────────────────────────────────
  const formatBytes = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  // ── Render ────────────────────────────────────────────────────────────────
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

          {/* ── Sección: Galería de Imágenes ── */}
          <section className="rounded-2xl border border-slate-700/60 bg-slate-800/40 p-6 flex flex-col gap-5">
            {/* Input file oculto */}
            <input
              ref={inputFileRef}
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              className="hidden"
              onChange={handleArchivoSeleccionado}
            />

            {/* Header de la sección */}
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base font-semibold text-white flex items-center gap-2">
                  <span className="text-purple-400">📸</span> Galería de Imágenes
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Se elige una aleatoriamente en cada mensaje enviado.
                </p>
              </div>
              <div className="flex items-center gap-3">
                {/* Badge contador */}
                <span className={`text-xs font-semibold px-2.5 py-1 rounded-full border
                  ${imagenes.length >= MAX_IMAGENES
                    ? 'bg-red-500/20 border-red-500/40 text-red-300'
                    : 'bg-purple-500/20 border-purple-500/40 text-purple-300'}`}>
                  {imagenes.length}/{MAX_IMAGENES}
                </span>
                {/* Botón subir */}
                <button
                  id="btn-subir-imagen"
                  onClick={handleSeleccionarArchivo}
                  disabled={subiendoImg || imagenes.length >= MAX_IMAGENES}
                  className={`flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-medium
                    transition-all duration-200 active:scale-95
                    ${(subiendoImg || imagenes.length >= MAX_IMAGENES)
                      ? 'bg-slate-700 text-slate-500 cursor-not-allowed'
                      : 'bg-purple-600 hover:bg-purple-500 text-white shadow-lg shadow-purple-500/20'}`}
                >
                  {subiendoImg ? '⏳ Subiendo...' : '➕ Subir imagen'}
                </button>
              </div>
            </div>

            {/* Nota sobre los formatos */}
            <div className="rounded-xl bg-slate-900/40 border border-slate-700/40 px-4 py-3 text-xs text-slate-400 -mt-1">
              📎 Formatos aceptados: JPG, JPEG, PNG, WEBP · Máximo 10 MB por imagen · Máximo {MAX_IMAGENES} imágenes activas
              {imagenes.length >= MAX_IMAGENES && (
                <span className="ml-2 text-red-400 font-semibold">— Límite alcanzado, elimina una para subir otra.</span>
              )}
            </div>

            {/* Grid de imágenes */}
            {cargandoImgs ? (
              <div className="flex items-center justify-center py-8">
                <span className="text-slate-500 animate-pulse text-sm">Cargando imágenes...</span>
              </div>
            ) : imagenes.length === 0 ? (
              // Estado vacío
              <div
                onClick={handleSeleccionarArchivo}
                className="flex flex-col items-center justify-center gap-3 py-10 rounded-2xl
                           border-2 border-dashed border-slate-700 hover:border-purple-500/50
                           cursor-pointer transition-all duration-200 group"
              >
                <span className="text-4xl group-hover:scale-110 transition-transform duration-200">🖼️</span>
                <p className="text-slate-500 text-sm text-center">
                  No hay imágenes cargadas.<br/>
                  <span className="text-purple-400 group-hover:text-purple-300 transition-colors">
                    Haz clic aquí para subir la primera
                  </span>
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                {imagenes.map(img => (
                  <div
                    key={img.nombre}
                    className="relative group rounded-xl overflow-hidden border border-slate-700/60
                               bg-slate-900/40 aspect-square flex items-center justify-center"
                  >
                    {/* Thumbnail */}
                    <img
                      src={`http://localhost:5000/imagenes/${img.nombre}`}
                      alt={img.nombre}
                      className="w-full h-full object-cover"
                      onError={e => {
                        // Fallback si la imagen no carga
                        (e.target as HTMLImageElement).style.display = 'none';
                      }}
                    />

                    {/* Overlay con info y botón eliminar */}
                    <div className="absolute inset-0 bg-slate-950/80 opacity-0 group-hover:opacity-100
                                    transition-all duration-200 flex flex-col items-center justify-center gap-2 p-2">
                      <p className="text-white text-xs font-medium text-center break-all leading-tight">
                        {img.nombre}
                      </p>
                      <p className="text-slate-400 text-xs">{formatBytes(img.tamanoBytes)}</p>
                      <button
                        onClick={() => handleEliminar(img.nombre)}
                        disabled={eliminandoImg === img.nombre}
                        className="mt-1 px-3 py-1.5 rounded-lg bg-red-600/80 hover:bg-red-500
                                   text-white text-xs font-semibold transition-colors duration-150
                                   disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                      >
                        {eliminandoImg === img.nombre ? '⏳' : '🗑'} Eliminar
                      </button>
                    </div>

                    {/* Badge nombre (visible siempre, se oculta en hover) */}
                    <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-slate-950/90 to-transparent
                                    px-2 py-1.5 group-hover:opacity-0 transition-opacity duration-200">
                      <p className="text-white text-xs truncate">{img.nombre}</p>
                    </div>
                  </div>
                ))}

                {/* Celda "agregar" si hay espacio */}
                {imagenes.length < MAX_IMAGENES && (
                  <button
                    onClick={handleSeleccionarArchivo}
                    disabled={subiendoImg}
                    className="aspect-square rounded-xl border-2 border-dashed border-slate-700
                               hover:border-purple-500/50 flex flex-col items-center justify-center gap-2
                               text-slate-600 hover:text-purple-400 transition-all duration-200
                               disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <span className="text-2xl">{subiendoImg ? '⏳' : '➕'}</span>
                    <span className="text-xs">{subiendoImg ? 'Subiendo...' : 'Agregar'}</span>
                  </button>
                )}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
