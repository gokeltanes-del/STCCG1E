using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

/// <summary>
/// One live game pipe. Direct IP uses NetClient or NetServer. Relay uses RelayNetLink.
/// The session does not care which.
/// </summary>
public interface INetLink : IDisposable
{
    bool IsConnected { get; }
    Task SendAsync(NetMessage message, CancellationToken cancellationToken = default);
    Task<NetMessage> ReceiveAsync(CancellationToken cancellationToken = default);
    void Disconnect();
}