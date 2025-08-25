using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RobotClouTools.Config;
using RobotClouTools.Services.Domain;

namespace RobotClouTools.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ConnectionRegistry _registry;
    private readonly TcpOptions _tcp;

    public HealthController(ConnectionRegistry registry, IOptions<TcpOptions> tcp)
    {
        _registry = registry;
        _tcp = tcp.Value;
    }

    [HttpGet]
    public IActionResult Get()
        => Ok(new
        {
            ok = true,
            tcp = new { _tcp.BindAddress, _tcp.Port, _tcp.MaxConcurrentConnections },
            connections = _registry.Count,
            timeUtc = DateTime.UtcNow
        });
}
