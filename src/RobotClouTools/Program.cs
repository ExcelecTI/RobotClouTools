using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RobotClouTools.Config;
using RobotClouTools.Services.Domain;

var builder = WebApplication.CreateBuilder(args);

// Ejecutar como servicio
if (OperatingSystem.IsWindows()) builder.Host.UseWindowsService();
if (OperatingSystem.IsLinux()) builder.Host.UseSystemd();

// Add services to the container.
builder.Services.AddControllers();

// TCP config + DI
builder.Services.Configure<TcpOptions>(builder.Configuration.GetSection("Tcp"));
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddHostedService<TcpListenerService>();

var app = builder.Build();

app.MapControllers();

app.Run();
