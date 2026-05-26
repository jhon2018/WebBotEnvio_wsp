// ─── Configuración ──────────────────────────────────────────────────────────

export interface Configuracion {
  id: number;
  limiteDiarioActual: number;
  factorIncremento: number;
  delayMinSegundos: number;
  delayMaxSegundos: number;
  wahaApiKey: string;
  wahaEndpointUrl: string;
  wahaSession: string;
  modoEnvioActivo: boolean;
}

export interface ActualizarConfiguracionDto {
  delayMinSegundos: number;
  delayMaxSegundos: number;
  factorIncremento: number;
  wahaApiKey: string;
  wahaEndpointUrl: string;
  wahaSession: string;
}

// ─── Imágenes ────────────────────────────────────────────────────────────────

export interface ImagenInfo {
  nombre: string;
  url: string;
  tamanoBytes: number;
  fechaSubida: string;
}


// ─── Plantillas ──────────────────────────────────────────────────────────────

export interface Plantilla {
  id: number;
  indice: number;
  cuerpoTexto: string;
  tipo: string;
  activo: boolean;
}

export interface ActualizarPlantillaConIdDto {
  id: number;
  cuerpoTexto: string;
  activo: boolean;
}

// ─── Dashboard ───────────────────────────────────────────────────────────────

export interface LoteActivo {
  id: string;
  nombreArchivo: string;
  estado: string;
  totalRegistros: number;
  fechaImportacion: string;
}

export interface Metricas {
  enviadosHoy: number;
  limiteMaximoDia: number;
  mensajesEnCola: number;
  mensajesConError: number;
  modoEnvioActivo: boolean;
  loteActivo: LoteActivo | null;
}

// ─── Lotes ───────────────────────────────────────────────────────────────────

export interface LoteResumen {
  id: string;
  nombreArchivo: string;
  codigoPais: string;
  totalRegistros: number;
  registrosSaltados: number;
  estado: string;
  fechaImportacion: string;
}

export interface DetalleEnvio {
  id: number;
  numeroCelular: string;
  nombreCliente: string;
  mensajeAsignado: string | null;
  estado: 'Pendiente' | 'Procesado' | 'Error';
  fechaRegistro: string;
  fechaProcesado: string | null;
  wahaAckCode: number | null;
  mensajeError: string | null;
}

export interface DetallesPage {
  total: number;
  pagina: number;
  tamano: number;
  items: DetalleEnvio[];
}

// ─── Países ──────────────────────────────────────────────────────────────────

export interface PaisSudamerica {
  nombre: string;
  codigo: string;
  bandera: string;
}

export const PAISES_SUDAMERICA: PaisSudamerica[] = [
  { nombre: 'Perú',      codigo: '51',  bandera: '🇵🇪' },
  { nombre: 'Colombia',  codigo: '57',  bandera: '🇨🇴' },
  { nombre: 'Chile',     codigo: '56',  bandera: '🇨🇱' },
  { nombre: 'Argentina', codigo: '54',  bandera: '🇦🇷' },
  { nombre: 'Ecuador',   codigo: '593', bandera: '🇪🇨' },
  { nombre: 'Bolivia',   codigo: '591', bandera: '🇧🇴' },
  { nombre: 'Brasil',    codigo: '55',  bandera: '🇧🇷' },
  { nombre: 'Uruguay',   codigo: '598', bandera: '🇺🇾' },
  { nombre: 'Paraguay',  codigo: '595', bandera: '🇵🇾' },
  { nombre: 'Venezuela', codigo: '58',  bandera: '🇻🇪' },
];
