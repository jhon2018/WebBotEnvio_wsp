# Reglas Globales del Proyecto - WAHA Sender

## Stack Tecnológico Obligatorio
- **Backend:** .NET 8 (C#) Web API, utilizando Inyección de Dependencias nativa y Controladores limpios.
- **Frontend:** React (Vite + TypeScript) con Tailwind CSS para los estilos de la interfaz (Tema Oscuro).
- **Base de Datos:** SQLite gestionado exclusivamente mediante Entity Framework Core (Code First).

## Reglas de Arquitectura y Flujo
1. **Aislamiento del Motor de Envío:** Las peticiones HTTP del frontend solo registran datos en SQLite en estado `Pendiente`. El frontend NUNCA interactúa directamente con el contenedor de WAHA.
2. **Procesamiento Asíncrono:** Todo envío a WhatsApp debe ser ejecutado por un `BackgroundService` (.NET Worker) leyendo de la base de datos de forma secuencial.
3. **Control Anti-Spam Estricto:** Cada envío individual debe calcular un delay aleatorio usando `Task.Delay` basándose en los parámetros de configuración (`DelayMin` y `DelayMax`). Queda prohibido enviar ráfagas de mensajes consecutivas sin pausa.
4. **Validación de Datos:** El backend debe validar que los números de teléfono tengan formato numérico y limpiar caracteres especiales antes de guardarlos.

## Comunicación con WAHA Docker
- El endpoint local de WAHA es: `http://localhost:3000/api/sendText`
- Toda petición HTTP hacia WAHA debe incluir la cabecera `X-Api-Key` y el `ContentType: application/json`.
- El payload JSON enviado a WAHA debe respetar estrictamente la estructura: `{ "session": "default", "chatId": "51XXXXXXXXX@c.us", "text": "..." }`.