using System.Collections.Concurrent;

namespace RobotClouTools.Services.Domain;

public sealed class ConnectionRegistry
{
    private readonly ConcurrentDictionary<int, TcpSession> _sessions = new();

    public int Count => _sessions.Count;

    public IEnumerable<(int id, string remote, DateTime connectedAt, long inB, long outB)>
        Snapshot() => _sessions.Values.Select(s => (
            s.Id,
            s.RemoteEndPoint?.ToString() ?? "unknown",
            s.ConnectedAt,
            s.BytesIn,
            s.BytesOut
        ));

    public bool TryAdd(TcpSession s) => _sessions.TryAdd(s.Id, s);
    public bool TryRemove(int id, out TcpSession? s) => _sessions.TryRemove(id, out s);

    public Task BroadcastAsync(string message)
        => Task.WhenAll(_sessions.Values.Select(s => s.SendAsync(message).AsTask()));
}
