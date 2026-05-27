Explicación del paso a paso de los Commits:
1. docs: inicializar repositorio y documentar especificaciones y guias de QA (Commit #1)
Objetivo: Establecer las bases del proyecto, qué debe ignorar Git, y dotar al equipo de soporte/QA de guías para pruebas.
Archivos incluidos: .gitignore, especificaciones del backend/frontend (waha-app-spec.md, database-schema.md), archivos Excel de prueba y la exhaustiva guía 

guia-pruebas-qa.md
.
2. feat(backend): configurar base de datos SQLite, entidades de EF Core y migraciones iniciales (Commit #2)
Objetivo: Definir el modelo de datos de la aplicación.
Archivos incluidos: Configuración del proyecto .NET (.csproj, appsettings.json), los modelos relacionales (Entities/ como LoteEnvio, DetalleEnvio, Configuracion, PlantillaMensaje), el contexto AppDbContext.cs y las migraciones de Entity Framework Core (Migrations/).
3. feat(backend): implementar controladores de API REST y servicio de envio de WhatsApp en segundo plano con WAHA (Commit #3)
Objetivo: Crear el motor del sistema.
Archivos incluidos: Controladores del API (Controllers/ para Dashboard, Configuracion, Lotes, Plantillas), lógica para formatear teléfonos (TelefonoHelper.cs) y el servicio en segundo plano WahaSenderBackgroundService.cs que conecta con la API de WAHA y envía mensajes periódicamente sin bloquear el servidor.
4. feat(frontend): inicializar proyecto React con Vite, TypeScript, Tailwind CSS y cliente de API con Axios (Commit #4)
Objetivo: Montar la estructura de la aplicación cliente y prepararla para consumir el backend.
Archivos incluidos: Configuraciones del entorno de frontend (package.json, tailwind.config.js, postcss.config.js, tsconfig.json, vite.config.ts), estilos globales (index.css), definición de tipos TypeScript y el servicio cliente HTTP en 

api.ts
.
5. feat(frontend): desarrollar componentes de interfaz de usuario reutilizables y plantilla base de navegacion (Commit #5)
Objetivo: Crear los elementos visuales comunes y el layout.
Archivos incluidos: Plantilla principal con barra de navegación (Layout.tsx), tarjetas de métricas (MetricCard.tsx), el selector de código de país (PaisSelector.tsx), el área de drag & drop para Excel (FileDropzone.tsx) y los mensajes por defecto en JSON.
6. feat(frontend): implementar paginas principales (Dashboard, Importacion, Historial, Configuracion) e integracion de rutas (Commit #6)
Objetivo: Armar las vistas funcionales que conectan los componentes anteriores con las llamadas al API, y configurar las rutas de la App.
Archivos incluidos: Páginas individuales (Dashboard.tsx, Importacion.tsx, Historial.tsx, Configuracion.tsx), e integración de rutas React Router en App.tsx y main.tsx.