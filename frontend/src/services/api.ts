import axios from 'axios';
import type {
  Configuracion,
  ActualizarConfiguracionDto,
  Plantilla,
  ActualizarPlantillaConIdDto,
  Metricas,
  LoteResumen,
  DetallesPage,
} from '../types';

// En dev, Vite redirige /api → http://localhost:5000 vía proxy.
// En producción, usar la URL base del servidor desplegado.
const api = axios.create({ baseURL: '/api' });

// ─── Dashboard ───────────────────────────────────────────────────────────────

export const getMetricas = () =>
  api.get<Metricas>('/dashboard/metricas').then(r => r.data);

// ─── Configuración ───────────────────────────────────────────────────────────

export const getConfiguracion = () =>
  api.get<Configuracion>('/configuracion').then(r => r.data);

export const actualizarConfiguracion = (dto: ActualizarConfiguracionDto) =>
  api.put<Configuracion>('/configuracion', dto).then(r => r.data);

export const incrementarLimite = () =>
  api.put<Configuracion>('/configuracion/incrementar-limite').then(r => r.data);

export const toggleEnvio = () =>
  api.put<Configuracion>('/configuracion/toggle-envio').then(r => r.data);

// ─── Plantillas ──────────────────────────────────────────────────────────────

export const getPlantillas = () =>
  api.get<Plantilla[]>('/plantillas').then(r => r.data);

export const guardarPlantillasBatch = (dtos: ActualizarPlantillaConIdDto[]) =>
  api.put<Plantilla[]>('/plantillas/batch', dtos).then(r => r.data);

// ─── Lotes ───────────────────────────────────────────────────────────────────

export const getLotes = () =>
  api.get<LoteResumen[]>('/lotes').then(r => r.data);

export const importarLote = (archivo: File, codigoPais: string) => {
  const form = new FormData();
  form.append('archivo', archivo);
  form.append('codigoPais', codigoPais);
  // ⚠️ NO pasar Content-Type manual: Axios debe auto-generar el header
  // multipart/form-data con el boundary correcto. Si se sobreescribe, el
  // backend no puede parsear los campos del formulario.
  return api.post<LoteResumen>('/lotes/importar', form).then(r => r.data);
};

export const getDetallesLote = (
  id: string,
  pagina = 1,
  tamano = 50,
  estado?: string,
) =>
  api.get<DetallesPage>(`/lotes/${id}/detalles`, {
    params: { pagina, tamano, estado },
  }).then(r => r.data);

export const reintentarFallidos = () =>
  api.post<{ cantidadReencolada: number; mensaje: string }>('/lotes/reintentar-fallidos')
     .then(r => r.data);
