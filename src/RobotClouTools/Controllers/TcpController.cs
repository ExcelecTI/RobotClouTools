using Microsoft.AspNetCore.Mvc;
using RobotClouTools.Services.Domain;

namespace RobotClouTools.Controllers;

[ApiController]
[Route("tcp")]
public class TcpController : ControllerBase
{
    private readonly ConnectionRegistry _registry;
    public TcpController(ConnectionRegistry registry) => _registry = registry;

    [HttpGet("connections")]
    public IActionResult Connections()
    {
        var list = _registry.Snapshot().Select(x => new {
            id = x.id,
            remote = x.remote,
            connectedAtUtc = x.connectedAt,
            bytesIn = x.inB,
            bytesOut = x.outB
        });
        return Ok(list);
    }

    public record BroadcastReq(string message);

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastReq body)
    {
        var msg = body.message.EndsWith("\n") ? body.message : body.message + "\n";
        await _registry.BroadcastAsync(msg);
        return Ok(new { sent = true, length = body.message.Length });
    }
}
