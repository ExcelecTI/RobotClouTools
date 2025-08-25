# 🤖 RobotClouTools

Proyecto en **.NET 8.0** que combina:

- **API Web ASP.NET Core** (minimal API + controllers).
- **Listener TCP/IP multitarea** (BackgroundService).
- **Native AOT** para binarios rápidos, livianos y listos para producción.
- Arquitectura organizada en carpetas (`Config`, `Controllers`, `Services`, etc.).

---

## 📂 Estructura de carpetas

src/RobotClouTools/
├─ Config/ # Configuración (TcpOptions, etc.)
├─ Controllers/ # API controllers (Health, Tcp)
├─ Exceptions/ # Futuras excepciones custom
├─ logs/ # Salida de logs
├─ Models/ # DTOs y entidades
├─ Services/
│ ├─ Abstractions/ # Interfaces de servicios
│ └─ Domain/ # Lógica del Listener TCP
│ ├─ ConnectionRegistry.cs
│ ├─ TcpListenerService.cs
│ └─ TcpSession.cs
├─ Utils/ # Helpers
├─ appsettings.json # Configuración Kestrel + TCP
├─ Program.cs # Bootstrap principal
└─ RobotClouTools.csproj # Proyecto con Native AOT