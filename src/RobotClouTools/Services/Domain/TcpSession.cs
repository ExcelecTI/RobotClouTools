using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace RobotClouTools.Services.Domain;

/// <summary>
/// Sesión TCP: lectura por líneas + cola de salida para no bloquear.
/// </summary>
public sealed class TcpSession : IAsyncDisposable
{
    private static int _nextId = 0;

    public int Id { get; } = Interlocked.Increment(ref _nextId);
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;
    public EndPoint? RemoteEndPoint => Client.Client.RemoteEndPoint;

    private readonly Channel<string> _outbox = Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _writerLoop;

    public long BytesIn { get; private set; }
    public long BytesOut { get; private set; }

    public TcpSession(TcpClient client)
    {
        Client = client;
        Stream = client.GetStream();
        _writerLoop = Task.Run(WriterLoopAsync);
    }

    public async Task HandleAsync(string lineDelimiter, int maxLineBytes, CancellationToken token)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder(4096);
        var delim = lineDelimiter;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

        while (!linked.IsCancellationRequested)
        {
            int read = await Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), linked.Token);
            if (read == 0) break; // cliente cerró
            BytesIn += read;

            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

            // Mitigar DoS por línea gigante
            if (sb.Length > maxLineBytes)
                throw new InvalidOperationException("Line too long");

            while (true)
            {
                var text = sb.ToString();
                int idx = text.IndexOf(delim, StringComparison.Ordinal);
                if (idx < 0) break;

                var line = text[..idx];
                sb.Remove(0, idx + delim.Length);

                // TODO: Reemplazar por tu protocolo real.
                var response = $"OK {line}{delim}";
                await SendAsync(response);
            }
        }
    }

    public ValueTask SendAsync(string data)
        => _outbox.Writer.WriteAsync(data);

    private async Task WriterLoopAsync()
    {
        try
        {
            while (await _outbox.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_outbox.Reader.TryRead(out var msg))
                {
                    var bytes = Encoding.UTF8.GetBytes(msg);
                    await Stream.WriteAsync(bytes, 0, bytes.Length, _cts.Token);
                    BytesOut += bytes.Length;
                }
                await Stream.FlushAsync(_cts.Token);
            }
        }
        catch { /* cierre */ }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
            _outbox.Writer.TryComplete();
            await _writerLoop.ContinueWith(_ => { });
        }
        catch { }
        finally
        {
            Stream.Dispose();
            Client.Close();
            Client.Dispose();
            _cts.Dispose();
        }
    }
}
