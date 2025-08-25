namespace RobotClouTools.Config;

public sealed class TcpOptions
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 9000;
    public int Backlog { get; set; } = 200;
    public int MaxConcurrentConnections { get; set; } = 200;
    public int ReadTimeoutMs { get; set; } = 30_000;
    public int WriteTimeoutMs { get; set; } = 30_000;
    public string LineDelimiter { get; set; } = "\n";
    public int MaxLineBytes { get; set; } = 32_768;
}
