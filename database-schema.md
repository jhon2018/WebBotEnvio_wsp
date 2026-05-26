# Esquema de Base de Datos - SQLite (EF Core)

## 1. Tabla: Configuracion
- `Id` (int, PK, Autoincrement)
- `LimiteDiarioActual` (int) - Cantidad máxima de mensajes a enviar hoy.
- `FactorIncremento` (int) - Cuánto sumará el botón manual (por defecto +5).
- `DelayMinSegundos` (int) - Pausa mínima entre mensajes.
- `DelayMaxSegundos` (int) - Pausa máxima entre mensajes.
- `WahaApiKey` (string) - Token de autenticación para la API local.

## 2. Tabla: PlantillasMensajes
- `Id` (int, PK, Autoincrement)
- `Indice` (int) - Del 1 al 8.
- `CuerpoTexto` (string) - Soporta el token `{Nombre}` para interpolación dinámica.
- `Activo` (bool)

## 3. Tabla: LotesEnvios
- `Id` (Guid, PK)
- `FechaImportacion` (DateTime)
- `NombreArchivo` (string)
- `TotalRegistros` (int)
- `Estado` (string) - 'Pendiente', 'En Progreso', 'Completado'.

## 4. Tabla: DetallesEnvios
- `Id` (int, PK, Autoincrement)
- `LoteId` (Guid, FK -> LotesEnvios.Id)
- `NumeroCelular` (string) - Formato limpio (ej: '51995799743').
- `NombreCliente` (string)
- `MensajeAsignado` (string) - El texto final tras la selección aleatoria e interpolación.
- `Estado` (string) - 'Pendiente', 'Procesado', 'Error'.
- `FechaRegistro` (DateTime)
- `FechaProcesado` (DateTime, Nullable)
- `WahaAckCode` (int, Nullable) - Estado de entrega devuelto por WAHA (0, 1, 2).
- `MensajeError` (string, Nullable)