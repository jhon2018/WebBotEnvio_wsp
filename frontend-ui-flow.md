# Especificación de la Interfaz de Usuario (React + Tailwind)

## Pantalla 1: Dashboard Principal
- Tarjetas con métricas en tiempo real: Mensajes Enviados Hoy / Límite Máximo, Mensajes en Cola, Estado del Lote actual.
- Botón de acción destacado: "Incrementar Límite Diario" (Ejecuta un PUT al backend para actualizar el límite en +5).
- Vista de los 8 campos de texto correspondientes a las plantillas de mensajes (cargados inicialmente desde el JSON, editables por el usuario, con botón "Guardar Cambios").

## Pantalla 2: Importación de Contactos
- Zona de arrastrar y soltar (Drag and Drop) para archivos `.xlsx` y `.csv`.
- Componente selector de Código de País (UI con banderas de Sudamérica: Perú, Colombia, Chile, Argentina, Ecuador, Bolivia, Brasil, Uruguay, Paraguay, Venezuela).
- Por defecto, el selector debe marcar siempre `+51` (Perú).
- Tabla de previsualización que muestre los primeros 10 registros del archivo cargado para verificar que las columnas `Numero` y `Nombre` se lean correctamente antes de procesar la importación final.