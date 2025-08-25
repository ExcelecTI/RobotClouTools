using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using RobotClouTools.Config;

namespace RobotClouTools.Services.Domain;

/// <summary>
/// Listener TCP multitarea con límite de concurrencia.
/// </summary>
public sealed class TcpListenerService : BackgroundService
{
    private readonly ILogger<TcpListenerService> _logger;
    private readonly TcpOptions _opts;
    private readonly ConnectionRegistry _registry;
    private TcpListener? _listener;
    private readonly SemaphoreSlim _gate;

    public TcpListenerService(
        ILogger<TcpListenerService> logger,
        IOptions<TcpOptions> opts,
        ConnectionRegistry registry)
    {
        _logger = logger;
        _opts = opts.Value;
        _registry = registry;
        _gate = new SemaphoreSlim(_opts.MaxConcurrentConnections);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ip = IPAddress.Parse(_opts.BindAddress);
        _listener = new TcpListener(ip, _opts.Port);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start(_opts.Backlog);

        _logger.LogInformation(
            "TCP listener @ {addr}:{port} (backlog={backlog}, max={max})",
            _opts.BindAddress, _opts.Port, _opts.Backlog, _opts.MaxConcurrentConnections);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);

                client.NoDelay = true;
                client.ReceiveTimeout = _opts.ReadTimeoutMs;
                client.SendTimeout = _opts.WriteTimeoutMs;

                await _gate.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    TcpSession? session = null;
                    try
                    {
                        session = new TcpSession(client);
                        if (!_registry.TryAdd(session))
                        {
                            await session.DisposeAsync();
                            return;
                        }

                        _logger.LogInformation("Conexión #{id} desde {remote}",
                            session.Id,
                            session.RemoteEndPoint?.ToString()); // forzamos string

                        await session.HandleAsync(_opts.LineDelimiter, _opts.MaxLineBytes, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // normal en shutdown
                    }
                    catch (Exception ex)
                    {
                        // Orden correcto del overload:
                        _logger.LogError(ex, "Error en sesión TCP");
                    }
                    finally
                    {
                        try
                        {
                            if (session != null && _registry.TryRemove(session.Id, out var s))
                                await s!.DisposeAsync();
                        }
                        catch { /* noop */ }
                        _gate.Release();
                    }
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal en shutdown
        }
        finally
        {
            _listener?.Stop();
            _logger.LogInformation("TCP listener detenido.");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        return base.StopAsync(cancellationToken);
    }
}
