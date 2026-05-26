import { useState } from 'react';
import * as XLSX from 'xlsx';
import FileDropzone from '../components/FileDropzone';
import PaisSelector from '../components/PaisSelector';
import { importarLote } from '../services/api';
import type { LoteResumen } from '../types';

// Preview parcial: filas que el usuario ve antes de confirmar importación.
interface FilaPreview {
  numero: string;
  nombre: string;
}

export default function Importacion() {
  const [codigoPais, setCodigoPais]     = useState('51');
  const [archivo,    setArchivo]        = useState<File | null>(null);
  const [preview,    setPreview]        = useState<FilaPreview[]>([]);
  const [loading,    setLoading]        = useState(false);
  const [resultado,  setResultado]      = useState<LoteResumen | null>(null);
  const [error,      setError]          = useState<string | null>(null);

  // ─── Carga del archivo para previsualización ─────────────────────────────
  const handleFile = async (file: File) => {
    setArchivo(file);
    setResultado(null);
    setError(null);

    // Previsualización local: leer los primeros 10 registros sin enviar al backend.
    try {
      const filas = await leerPrimeras10Filas(file);
      setPreview(filas);
    } catch {
      setPreview([]);
    }
  };

  // ─── Importación real al backend ──────────────────────────────────────────
  const handleImportar = async () => {
    if (!archivo) return;
    setLoading(true);
    setError(null);
    try {
      const res = await importarLote(archivo, codigoPais);
      setResultado(res);
      setPreview([]);
      setArchivo(null);
    } catch (e: any) {
      const msg = e?.response?.data || 'Error al importar el archivo. Verifica el formato.';
      setError(typeof msg === 'string' ? msg : JSON.stringify(msg));
    } finally {
      setLoading(false);
    }
  };

  // ─── Render ───────────────────────────────────────────────────────────────
  return (
    <div className="flex flex-col gap-8 max-w-4xl mx-auto">
      {/* Encabezado */}
      <div>
        <h1 className="text-2xl font-bold text-white">📥 Importar Contactos</h1>
        <p className="text-slate-500 text-sm mt-1">
          Sube tu archivo Excel (.xlsx) o CSV con las columnas <code className="text-blue-400">Numero</code> y <code className="text-blue-400">Nombre</code>.
        </p>
      </div>

      {/* Resultado exitoso */}
      {resultado && (
        <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-6 flex flex-col gap-2">
          <h2 className="text-green-400 font-bold text-lg">✅ Importación exitosa</h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 text-sm text-slate-300 mt-2">
            <div><span className="text-slate-500">Archivo:</span><br /><b>{resultado.nombreArchivo}</b></div>
            <div><span className="text-slate-500">Importados:</span><br /><b className="text-green-400">{resultado.totalRegistros}</b></div>
            <div><span className="text-slate-500">Saltados:</span><br /><b className="text-yellow-400">{resultado.registrosSaltados}</b></div>
            <div><span className="text-slate-500">País:</span><br /><b>+{resultado.codigoPais}</b></div>
            <div><span className="text-slate-500">Estado:</span><br /><b className="text-blue-400">{resultado.estado}</b></div>
          </div>
          <p className="text-slate-500 text-xs mt-2">
            Ve al Dashboard y presiona <b>▶️ Iniciar Envíos</b> para comenzar el procesamiento.
          </p>
        </div>
      )}

      {/* Formulario */}
      {!resultado && (
        <div className="flex flex-col gap-6">
          {/* Selector de país */}
          <PaisSelector value={codigoPais} onChange={setCodigoPais} />

          {/* Dropzone */}
          <FileDropzone onFile={handleFile} isLoading={loading} />

          {/* Error */}
          {error && (
            <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-red-300 text-sm">
              ⚠️ {error}
            </div>
          )}

          {/* Preview de las primeras 10 filas */}
          {preview.length > 0 && (
            <div className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold text-slate-300">
                👁 Vista previa — primeros {preview.length} registros
              </h2>
              <div className="overflow-x-auto rounded-xl border border-slate-700/60">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-slate-700/60 bg-slate-800/60">
                      <th className="text-left px-4 py-2.5 text-slate-400 font-medium">#</th>
                      <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Número</th>
                      <th className="text-left px-4 py-2.5 text-slate-400 font-medium">Nombre</th>
                      <th className="text-left px-4 py-2.5 text-slate-400 font-medium">ChatId resultante</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.map((f, i) => {
                      const numLimpio = f.numero.replace(/\D/g, '');
                      const yaTieneCodigo = numLimpio.startsWith(codigoPais);
                      const chatId = `${yaTieneCodigo ? numLimpio : codigoPais + numLimpio}@c.us`;
                      return (
                        <tr key={i} className="border-b border-slate-800/60 hover:bg-slate-800/40 transition-colors">
                          <td className="px-4 py-2.5 text-slate-600">{i + 1}</td>
                          <td className="px-4 py-2.5 text-slate-300 font-mono">{f.numero}</td>
                          <td className="px-4 py-2.5 text-slate-300">{f.nombre}</td>
                          <td className="px-4 py-2.5 text-blue-400/80 font-mono text-xs">{chatId}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <p className="text-xs text-slate-600">
                La columna <i>ChatId resultante</i> es una previsualización de cómo quedará el número formateado. El backend aplica la sanitización final.
              </p>
            </div>
          )}

          {/* Botón de importación */}
          {archivo && (
            <button
              id="btn-importar"
              onClick={handleImportar}
              disabled={loading}
              className={`
                w-full py-3.5 rounded-xl font-semibold text-base transition-all duration-200
                flex items-center justify-center gap-2 shadow-lg active:scale-[0.98]
                ${loading
                  ? 'bg-slate-700 text-slate-500 cursor-not-allowed'
                  : 'bg-blue-600 hover:bg-blue-500 text-white shadow-blue-500/20'}
              `}
            >
              {loading ? (
                <><span className="animate-spin">⏳</span> Importando...</>
              ) : (
                <>📤 Importar {archivo.name}</>
              )}
            </button>
          )}
        </div>
      )}

      {/* Nueva importación */}
      {resultado && (
        <button
          id="btn-nueva-importacion"
          onClick={() => setResultado(null)}
          className="self-start px-5 py-2.5 rounded-xl border border-slate-700 text-slate-400
                     hover:text-white hover:border-slate-500 transition-all duration-200 text-sm"
        >
          ← Nueva Importación
        </button>
      )}
    </div>
  );
}


// ─── Helper: leer primeras 10 filas del archivo localmente ───────────────────
// Se hace en el browser sin enviar al servidor para dar feedback inmediato.

const COL_NUMERO_NAMES = ['numero', 'número', 'phone', 'celular', 'telefono', 'teléfono'];
const COL_NOMBRE_NAMES = ['nombre', 'name', 'cliente', 'contacto'];

async function leerPrimeras10Filas(file: File): Promise<FilaPreview[]> {
  const ext = file.name.split('.').pop()?.toLowerCase();
  if (ext === 'csv') return leerCSV(file);
  if (ext === 'xlsx' || ext === 'xls') return leerXLSX(file);
  return [];
}

async function leerXLSX(file: File): Promise<FilaPreview[]> {
  const buffer = await file.arrayBuffer();
  const wb = XLSX.read(buffer, { type: 'array' });
  const ws = wb.Sheets[wb.SheetNames[0]];
  // sheet_to_json incluye la fila de cabecera como clave del objeto
  const rows = XLSX.utils.sheet_to_json<Record<string, unknown>>(ws, { defval: '' });
  if (rows.length === 0) return [];

  // Encontrar las columnas de forma case-insensitive
  const firstRow = rows[0];
  const keys = Object.keys(firstRow);
  const keyNumero = keys.find(k => COL_NUMERO_NAMES.includes(k.trim().toLowerCase()));
  const keyNombre = keys.find(k => COL_NOMBRE_NAMES.includes(k.trim().toLowerCase()));

  if (!keyNumero || !keyNombre) return [];

  return rows.slice(0, 10).map(row => ({
    numero: String(row[keyNumero] ?? ''),
    nombre: String(row[keyNombre] ?? ''),
  }));
}

async function leerCSV(file: File): Promise<FilaPreview[]> {
  const text = await file.text();
  const lineas = text.split('\n').filter(l => l.trim().length > 0);
  if (lineas.length < 2) return [];

  const sep        = lineas[0].includes(';') ? ';' : ',';
  const headers    = lineas[0].split(sep).map(h => h.trim().toLowerCase().replace(/['"]/g, ''));
  const idxNumero  = headers.findIndex(h => COL_NUMERO_NAMES.includes(h));
  const idxNombre  = headers.findIndex(h => COL_NOMBRE_NAMES.includes(h));

  if (idxNumero === -1 || idxNombre === -1) return [];

  return lineas.slice(1, 11).map(linea => {
    const cols = linea.split(sep).map(c => c.trim().replace(/['"]/g, ''));
    return {
      numero: cols[idxNumero] ?? '',
      nombre: cols[idxNombre] ?? '',
    };
  });
}
