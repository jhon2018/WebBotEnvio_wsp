Actúa como un Arquitecto de Software Fullstack Senior y experto en automatizaciones. Necesito que diseñes y generes el código base para una aplicación web con Arquitectura Monolítica orientada a la gestión controlada y segura de envíos masivos de WhatsApp a través de la API de WAHA (que corre localmente en Docker).

**Stack Tecnológico:**
- Backend: .NET 8 (C#) Web API estructurado de forma limpia.
- Frontend: React (Vite + TypeScript) con Tailwind CSS.
- Base de Datos: SQLite local mediante Entity Framework Core.

**Requerimientos de Lógica de Negocio y UI:**

1. **Gestión de Archivos y Contactos (React UI):**
   - Interfaz para subir archivos Excel (.xlsx/.csv). El archivo tendrá siempre dos columnas base: `Numero` y `Nombre` (pueden ser hasta 1,000+ registros).
   - Un componente `Select` visual para el código de país. Por defecto debe estar seleccionado "+51 (Perú)", pero debe incluir los códigos y banderas (por UI) de los demás países de Sudamérica.
   - Al cargar el Excel, los números deben formatearse correctamente y guardarse en base de datos en estado "Pendiente".

2. **Gestión de Mensajes (Plantillas):**
   - El sistema debe manejar exactamente 8 campos de mensajes diferentes.
   - Estos 8 mensajes deben precargarse por defecto desde un archivo JSON local, pero permitir que el usuario los edite desde la interfaz web.
   - Los mensajes deben soportar la variable interpolada `{Nombre}`, la cual será reemplazada dinámicamente con el dato del Excel.

3. **Módulo de Configuración y Control de Spam:**
   - Parámetros almacenados en SQLite: Límite diario de envíos (ej. 50 inicial), Delay mínimo (segundos) y Delay máximo (segundos).
   - En el Dashboard web de React, debe existir un botón de control manual llamado "Incrementar Límite Diario" que sume +5 (u otra cantidad parametrizada) al límite actual, para calentar el número de WhatsApp de forma progresiva.

4. **Motor de Envío (Procesamiento en Segundo Plano - .NET):**
   - La API debe contar con un `BackgroundService` o `IHostedService` que corra independiente de las peticiones HTTP de los usuarios.
   - Este servicio buscará registros en estado "Pendiente", seleccionará *aleatoriamente* 1 de los 8 mensajes disponibles para cada número, reemplazará la variable `{Nombre}`, y hará el POST a la API local de WAHA (`http://localhost:3000/api/sendText`).
   - Obligatorio: Aplicar un `Task.Delay` aleatorio (basado en la configuración) entre cada envío para evitar bloqueos.
   - Actualizar el estado en SQLite a "Procesado", registrando la fecha/hora y almacenando el ACK de la respuesta JSON.

**Por favor, bríndame lo siguiente para iniciar el desarrollo:**
1. La estructura de carpetas sugerida para esta solución monolítica.
2. Los modelos de clases (Entities) en C# para SQLite (`Configuracion`, `PlantillaMensaje`, `LoteEnvio`, `DetalleEnvio`).
3. El código en C# del `BackgroundService` que implementa la lógica segura de envío y el randomizado de mensajes y tiempos.
4. El código del componente de React (carga de Excel + Selector de País Sudamérica).


{
  "mensajes": [
    {
      "id": 1,
      "texto": "💰 ¡Crédito aprobado a sola firma!\nSolo con tu DNI puedes acceder a tu préstamo inmediato.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "prestamo"
    },
    {
      "id": 2,
      "texto": "💰 Banco Santander tiene un préstamo pre-aprobado para ti.\nAccede rápido, sin papeleos y con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "prestamo"
    },
    {
      "id": 3,
      "texto": "💰 ¡Tu oportunidad está aquí!\nPréstamo personal disponible con aprobación inmediata.\nSolo necesitas tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "prestamo"
    },
    {
      "id": 4,
      "texto": "💳 ¡Ya tienes tu tarjeta Santander aprobada!\nDisfruta beneficios exclusivos y compras sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "tarjeta"
    },
    {
      "id": 5,
      "texto": "💳 Banco Santander te ofrece tarjeta de crédito con aprobación inmediata.\nEmpieza a disfrutar descuentos y facilidades hoy.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "tarjeta"
    },
    {
      "id": 6,
      "texto": "💳 ¡Activa y disfruta tu tarjeta Santander VISA!\nAprovecha promociones y meses sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "tarjeta"
    },
    {
      "id": 7,
      "texto": "👋 Hola, tienes beneficios disponibles en Banco Santander.\nPuedes acceder a préstamo o tarjeta con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "bienvenida"
    },
    {
      "id": 8,
      "texto": "👋 Banco Santander te da la bienvenida.\nTienes opciones de crédito disponibles listas para ti.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743",
      "tipo": "bienvenida"
    }
  ]
}


DATOS EXTRA DOCKER:
PS D:\Docker> docker run --rm -v "${PWD}:/app/env" devlikeapro/waha init-waha /app/env
Credentials generated.

Generated env values:
  - WAHA_API_KEY=119ce04a85dd41818809be61aba87066
  - WAHA_API_KEY_PLAIN=119ce04a85dd41818809be61aba87066
  - WAHA_DASHBOARD_USERNAME=admin
  - WAHA_DASHBOARD_PASSWORD=8a50a38e0bf24819b97ea5f58c6fd05e
  - WHATSAPP_SWAGGER_USERNAME=admin
  - WHATSAPP_SWAGGER_PASSWORD=8a50a38e0bf24819b97ea5f58c6fd05e

Use these credentials to login in Dashboard or Swagger:
  - Username: admin
  - Password: 8a50a38e0bf24819b97ea5f58c6fd05e

Use this API key in the x-api-key header:
  - 119ce04a85dd41818809be61aba87066

Read more:
  - https://waha.devlike.pro/docs/how-to/dashboard/#api-key
  - https://waha.devlike.pro/docs/how-to/security/


--- Lanza el servidor WAHA
docker run -d --env-file "${PWD}/.env" -v "${PWD}/sessions:/app/.sessions" -p 3000:3000 --name waha devlikeapro/waha



--- PRUEBA DE ENVIO DESDE SHELL exitoso
$url = "http://localhost:3000/api/sendText"
$body = @{ session = "default"; chatId = "51945430381@c.us"; text = "Prueba desde PowerShell 2" } | ConvertTo-Json
$headers = @{ "Content-Type" = "application/json"; "Accept" = "application/json"; "X-Api-Key" = "119ce04a85dd41818809be61aba87066" }
Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body -ContentType "application/json; charset=utf-8"