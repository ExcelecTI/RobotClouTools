# RobotClouTools

API **ASP.NET Core 8** con **listener TCP/IP multitarea** embebido, lista para publicar con **Native AOT** y correr como **servicio en Windows/Linux**.  
Diseño simple, escalable y mantenible.

> TL;DR: `dotnet run`, pega un `nc localhost 9901`, manda una línea y te responde `OK <linea>`.  
> Después lo publicas AOT y lo instalas como servicio. Fin.

---

## 🚀 Features

- **HTTP API** con Controllers: endpoints de _health_ y utilidades TCP.
- **Listener TCP/IP** en `BackgroundService` (acepta N clientes en paralelo).
- **Seguridad básica:** límites de concurrencia, timeouts y tamaño de línea.
- **Configuración por `appsettings.json`:** IP/puerto, backlog, delimitador, etc.
- **Listo para Native AOT** (binario _single-file_, optimizado).
- **Despliegue como servicio:** Windows (`UseWindowsService`) y Linux (`systemd`).

---

## 📁 Estructura

src/RobotClouTools/
├─ Config/
│ └─ TcpOptions.cs
├─ Controllers/
│ ├─ HealthController.cs
│ └─ TcpController.cs
├─ Exceptions/
├─ logs/
├─ Models/
├─ Services/
│ ├─ Abstractions/
│ └─ Domain/
│ ├─ ConnectionRegistry.cs
│ ├─ TcpListenerService.cs
│ └─ TcpSession.cs
├─ Utils/
├─ appsettings.json
├─ Program.cs
└─ RobotClouTools.csproj

markdown
Copiar
Editar

---

## 🧩 Archivos clave (qué hace cada uno)

### `Program.cs`
- Configura el host para correr como **servicio** (Windows/Linux).
- Registra **Controllers**, `TcpOptions`, `ConnectionRegistry` y `TcpListenerService`.
- Arranca la app HTTP y el **listener TCP** en _background_.

### `appsettings.json`
- Configuración de **Kestrel (HTTP)** y **Tcp**:
  - `Tcp.BindAddress` → IP para el listener (ej: `0.0.0.0` o `127.0.0.1`).
  - `Tcp.Port` → puerto TCP (ej: `9000` o `9901`).
  - `Backlog`, `MaxConcurrentConnections`, `ReadTimeoutMs`, `WriteTimeoutMs`.
  - `LineDelimiter` (por defecto `\n`) y `MaxLineBytes` (límite anti DoS).

### `Config/TcpOptions.cs`
- POCO que mapea la sección `Tcp` del `appsettings.json`.  
- ✔️ **Sí, aquí se reflejan los cambios de IP/puerto**: basta con editar `appsettings.json` (no hay que tocar código).

### `Services/Domain/TcpListenerService.cs`
- `BackgroundService` que abre el `TcpListener` y acepta conexiones concurrentes (con `SemaphoreSlim`).
- Configura cada `TcpClient` (`NoDelay`, timeouts).
- Crea una **`TcpSession`** por cliente y la registra en **`ConnectionRegistry`**.
- Manejo robusto de cierre y logging.

### `Services/Domain/TcpSession.cs`
- Representa una sesión TCP por cliente:
  - Lee bytes a **buffer** y separa por `LineDelimiter`.
  - Responde (por ahora) con `OK <line>` (_echo_ con prefijo).
  - Cola de salida con `Channel<string>` para no bloquear escrituras.
  - Métricas por sesión: `BytesIn`, `BytesOut`, `ConnectedAt`, `RemoteEndPoint`.
  - Cierre limpio (`IAsyncDisposable`).

### `Services/Domain/ConnectionRegistry.cs`
- Registro concurrente de sesiones activas (in-memory).
- `Snapshot()` para exponer conexiones por API.
- `BroadcastAsync()` para enviar un mensaje a **todas** las sesiones.

### `Controllers/HealthController.cs`
- `GET /health` → estado simple: conexiones activas, config TCP, hora servidor.

### `Controllers/TcpController.cs`
- `GET /tcp/connections` → **listado** de conexiones (id, remote, bytes I/O, etc.).
- `POST /tcp/broadcast` → envía un mensaje a **todas** las sesiones (`{ "message": "hola" }`).

### `RobotClouTools.csproj`
- Target **.NET 8** con **Native AOT** (al publicar).
- _Single-file build_, strip de símbolos.
- Paquetes para correr como **servicio** en Windows y Linux.
- Incluye carpetas (logs, etc.) y soporta `README.md`/`.gitignore`.

---

## ⚙️ Configuración

`src/RobotClouTools/appsettings.json` (ejemplo con **127.0.0.1:9901**):
```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft": "Warning", "Microsoft.Hosting.Lifetime": "Information" }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      }
    }
  },
  "Tcp": {
    "BindAddress": "127.0.0.1",
    "Port": 9901,
    "Backlog": 200,
    "MaxConcurrentConnections": 200,
    "ReadTimeoutMs": 30000,
    "WriteTimeoutMs": 30000,
    "LineDelimiter": "\n",
    "MaxLineBytes": 8192
  }
}
