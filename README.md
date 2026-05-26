
🖥️ Pasos para levantar el proyecto
Terminal 1 — Backend .NET:

-- COMIENZO MIGRAR SQL LITE
cd d:\JONATHAN\Proyectos\BotEnvio_wsp\backend
dotnet ef migrations add Initial
dotnet ef database update
dotnet build
dotnet run

----- REACT -----
cd D:\JONATHAN\Proyectos\BotEnvio_wsp\frontend
npm run dev 2>&1
http://localhost:5173/importar

---BACKEND---
cd d:\JONATHAN\Proyectos\BotEnvio_wsp\backend
dotnet run
http://localhost:5000/swagger/index.html