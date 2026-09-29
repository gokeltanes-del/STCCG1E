using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StarTrekCCG.Network;

/// <summary>
/// Opens the host listen port on the local router. UPnP IGD first, then PCP to the gateway.
/// SSDP and the gateway only. No relay, no matchmaking, no public IP web service.
/// </summary>
public static class NatPortForward
{
    public const int LeaseSeconds = 7200;

    public static async Task<NatPortLease> TryOpenAsync(int port, CancellationToken cancellationToken = default)
    {
        if (port is < 1 or > 65535)
            return NatPortLease.Failed("port is not valid");

        string? upnp = null;
        try
        {
            var upnpLease = await TryUpnpAsync(port, cancellationToken).ConfigureAwait(false);
            if (upnpLease != null)
                return upnpLease;
            upnp = "no UPnP gateway answered";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            upnp = ex.Message;
        }

        try
        {
            var pcpLease = await TryPcpAsync(port, cancellationToken).ConfigureAwait(false);
            if (pcpLease != null)
                return pcpLease;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return NatPortLease.Failed($"UPnP: {upnp}. PCP: {ex.Message}");
        }

        return NatPortLease.Failed($"UPnP: {upnp}. PCP: no answer from the gateway");
    }

    private static async Task<NatPortLease?> TryUpnpAsync(int port, CancellationToken ct)
    {
        var locations = await SsdpLocationsAsync(ct).ConfigureAwait(false);
        if (locations.Count == 0)
            return null;

        foreach (var location in locations)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var lease = await AddUpnpMappingAsync(location, port, ct).ConfigureAwait(false);
                if (lease != null)
                    return lease;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // next device
            }
        }
        return null;
    }

    private static async Task<List<string>> SsdpLocationsAsync(CancellationToken ct)
    {
        string[] searches =
        {
            "urn:schemas-upnp-org:device:InternetGatewayDevice:1",
            "urn:schemas-upnp-org:service:WANIPConnection:1"
        };
        var found = new List<string>();
        foreach (var st in searches)
        {
            foreach (var loc in await SsdpOnceAsync(st, 1200, ct).ConfigureAwait(false))
            {
                if (!found.Contains(loc))
                    found.Add(loc);
            }
            if (found.Count > 0)
                break;
        }
        return found;
    }

    private static async Task<List<string>> SsdpOnceAsync(string searchTarget, int waitMs, CancellationToken ct)
    {
        var found = new List<string>();
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        try
        {
            udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
        }
        catch
        {
            // TTL is optional
        }

        var payload = Encoding.ASCII.GetBytes(
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 1\r\n" +
            "ST: " + searchTarget + "\r\n" +
            "\r\n");
        var dest = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        await udp.SendAsync(payload, dest, ct).ConfigureAwait(false);

        var until = Environment.TickCount64 + waitMs;
        while (Environment.TickCount64 < until)
        {
            ct.ThrowIfCancellationRequested();
            var left = (int)Math.Max(1, until - Environment.TickCount64);
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
            wait.CancelAfter(left);
            try
            {
                var result = await udp.ReceiveAsync(wait.Token).ConfigureAwait(false);
                var text = Encoding.ASCII.GetString(result.Buffer);
                var loc = HeaderValue(text, "LOCATION");
                if (!string.IsNullOrWhiteSpace(loc) && !found.Contains(loc))
                    found.Add(loc);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                break;
            }
        }
        return found;
    }

    private static async Task<NatPortLease?> AddUpnpMappingAsync(string location, int port, CancellationToken ct)
    {
        if (!Uri.TryCreate(location, UriKind.Absolute, out var locationUri))
            return null;
        if (!IsLanIpv4(locationUri.Host))
            return null;

        using var http = NewHttp();
        using var desc = await http.GetAsync(locationUri, ct).ConfigureAwait(false);
        if (!desc.IsSuccessStatusCode)
            return null;
        var xml = await desc.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!TryFindWanService(xml, locationUri, out var serviceUrn, out var control))
            return null;
        if (!IsLanIpv4(control.Host))
            return null;

        var local = LocalAddressToward(control.Host);
        if (local == null)
            return null;

        var add = await SoapAsync(http, control, serviceUrn, "AddPortMapping", new Dictionary<string, string>
        {
            ["NewRemoteHost"] = "",
            ["NewExternalPort"] = port.ToString(),
            ["NewProtocol"] = "TCP",
            ["NewInternalPort"] = port.ToString(),
            ["NewInternalClient"] = local.ToString(),
            ["NewEnabled"] = "1",
            ["NewPortMappingDescription"] = "StarTrekCCG",
            ["NewLeaseDuration"] = LeaseSeconds.ToString()
        }, ct).ConfigureAwait(false);

        if (!add.Ok && add.ErrorCode == "725")
        {
            add = await SoapAsync(http, control, serviceUrn, "AddPortMapping", new Dictionary<string, string>
            {
                ["NewRemoteHost"] = "",
                ["NewExternalPort"] = port.ToString(),
                ["NewProtocol"] = "TCP",
                ["NewInternalPort"] = port.ToString(),
                ["NewInternalClient"] = local.ToString(),
                ["NewEnabled"] = "1",
                ["NewPortMappingDescription"] = "StarTrekCCG",
                ["NewLeaseDuration"] = "0"
            }, ct).ConfigureAwait(false);
        }
        if (!add.Ok)
            return null;

        string? external = null;
        var ip = await SoapAsync(http, control, serviceUrn, "GetExternalIPAddress",
            new Dictionary<string, string>(), ct).ConfigureAwait(false);
        if (ip.Ok)
            external = XmlValue(ip.Body, "NewExternalIPAddress");
        if (string.IsNullOrWhiteSpace(external) || external == "0.0.0.0")
            external = null;

        return NatPortLease.Upnp(control, serviceUrn, external, port);
    }

    private static bool TryFindWanService(string xml, Uri location, out string serviceUrn, out Uri control)
    {
        serviceUrn = "";
        control = location;
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch
        {
            return false;
        }

        string? urlBase = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "URLBase")?.Value;
        string? bestType = null;
        string? bestControl = null;
        int bestRank = int.MaxValue;
        foreach (var svc in doc.Descendants().Where(e => e.Name.LocalName == "service"))
        {
            var type = svc.Elements().FirstOrDefault(e => e.Name.LocalName == "serviceType")?.Value ?? "";
            var ctrl = svc.Elements().FirstOrDefault(e => e.Name.LocalName == "controlURL")?.Value ?? "";
            int rank = WanRank(type);
            if (rank < bestRank && !string.IsNullOrWhiteSpace(ctrl))
            {
                bestRank = rank;
                bestType = type.Trim();
                bestControl = ctrl.Trim();
            }
        }
        if (bestType == null || bestControl == null || bestRank == int.MaxValue)
            return false;

        serviceUrn = bestType;
        control = ResolveControl(location, urlBase, bestControl);
        return true;
    }

    private static int WanRank(string type)
    {
        if (type.Contains("WANIPConnection:1", StringComparison.OrdinalIgnoreCase)) return 0;
        if (type.Contains("WANPPPConnection:1", StringComparison.OrdinalIgnoreCase)) return 1;
        if (type.Contains("WANIPConnection:2", StringComparison.OrdinalIgnoreCase)) return 2;
        if (type.Contains("WANPPPConnection:2", StringComparison.OrdinalIgnoreCase)) return 3;
        return int.MaxValue;
    }

    private static Uri ResolveControl(Uri location, string? urlBase, string control)
    {
        if (Uri.TryCreate(control, UriKind.Absolute, out var abs))
            return abs;
        Uri baseUri = location;
        if (!string.IsNullOrWhiteSpace(urlBase) && Uri.TryCreate(urlBase, UriKind.Absolute, out var parsed))
            baseUri = parsed;
        return new Uri(baseUri, control);
    }

    private static async Task<SoapResult> SoapAsync(
        HttpClient http, Uri control, string serviceUrn, string action,
        Dictionary<string, string> args, CancellationToken ct)
    {
        var body = new StringBuilder();
        body.Append("<?xml version=\"1.0\"?>");
        body.Append("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
        body.Append("<s:Body>");
        body.Append("<u:").Append(action).Append(" xmlns:u=\"").Append(serviceUrn).Append("\">");
        foreach (var kv in args)
            body.Append('<').Append(kv.Key).Append('>').Append(kv.Value).Append("</").Append(kv.Key).Append('>');
        body.Append("</u:").Append(action).Append('>');
        body.Append("</s:Body></s:Envelope>");

        using var req = new HttpRequestMessage(HttpMethod.Post, control);
        req.Headers.TryAddWithoutValidation("SOAPAction", "\"" + serviceUrn + "#" + action + "\"");
        req.Content = new StringContent(body.ToString(), Encoding.UTF8, "text/xml");
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode || text.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
            return new SoapResult(false, text, XmlValue(text, "errorCode"));
        return new SoapResult(true, text, null);
    }

    private static async Task<NatPortLease?> TryPcpAsync(int port, CancellationToken ct)
    {
        var gateway = DefaultGateway();
        if (gateway == null)
            return null;
        var local = LocalAddressToward(gateway);
        if (local == null)
            return null;

        var nonce = new byte[12];
        Random.Shared.NextBytes(nonce);
        var packet = BuildPcpMap(local, port, port, LeaseSeconds, nonce);
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(local, 0));
        var dest = new IPEndPoint(gateway, 5351);
        await udp.SendAsync(packet, dest, ct).ConfigureAwait(false);

        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(1500);
        byte[] buf;
        try
        {
            var result = await udp.ReceiveAsync(wait.Token).ConfigureAwait(false);
            buf = result.Buffer;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        if (!TryParsePcpMap(buf, out var externalPort, out var externalIp))
            return null;
        return NatPortLease.Pcp(gateway, local, nonce, externalIp, externalPort, port);
    }

    internal static byte[] BuildPcpMap(IPAddress local, int internalPort, int externalPort, int lifetime, byte[] nonce)
    {
        var buf = new byte[60];
        buf[0] = 2;
        buf[1] = 1;
        buf[4] = (byte)((lifetime >> 24) & 0xff);
        buf[5] = (byte)((lifetime >> 16) & 0xff);
        buf[6] = (byte)((lifetime >> 8) & 0xff);
        buf[7] = (byte)(lifetime & 0xff);
        var v4 = local.GetAddressBytes();
        buf[18] = 0xff;
        buf[19] = 0xff;
        buf[20] = v4[0];
        buf[21] = v4[1];
        buf[22] = v4[2];
        buf[23] = v4[3];
        Buffer.BlockCopy(nonce, 0, buf, 24, 12);
        buf[36] = 6;
        buf[40] = (byte)((internalPort >> 8) & 0xff);
        buf[41] = (byte)(internalPort & 0xff);
        buf[42] = (byte)((externalPort >> 8) & 0xff);
        buf[43] = (byte)(externalPort & 0xff);
        return buf;
    }

    private static bool TryParsePcpMap(byte[] buf, out int externalPort, out string? externalIp)
    {
        externalPort = 0;
        externalIp = null;
        if (buf.Length < 60 || buf[0] != 2)
            return false;
        if ((buf[1] & 0x7f) != 1 || (buf[1] & 0x80) == 0)
            return false;
        if (buf[3] != 0)
            return false;
        externalPort = (buf[42] << 8) | buf[43];
        // Assigned external IP at offset 44. IPv4-mapped form is ::ffff:a.b.c.d.
        bool mapped = buf[54] == 0xff && buf[55] == 0xff;
        if (mapped)
        {
            externalIp = new IPAddress(new[] { buf[56], buf[57], buf[58], buf[59] }).ToString();
            if (externalIp == "0.0.0.0")
                externalIp = null;
        }
        return externalPort is > 0 and <= 65535;
    }

    private static IPAddress? DefaultGateway()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            foreach (var gw in ni.GetIPProperties().GatewayAddresses)
            {
                if (gw.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (IPAddress.IsLoopback(gw.Address) || gw.Address.Equals(IPAddress.Any))
                    continue;
                return gw.Address;
            }
        }
        return null;
    }

    private static IPAddress? LocalAddressToward(string host)
    {
        // Router address from SSDP only. Do not resolve a name through DNS.
        if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return null;
        return LocalAddressToward(ip);
    }

    private static IPAddress? LocalAddressToward(IPAddress remote)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(remote, 9));
            return (socket.LocalEndPoint as IPEndPoint)?.Address;
        }
        catch
        {
            return null;
        }
    }

    private static HttpClient NewHttp()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
    }

    private static bool IsLanIpv4(string host)
    {
        if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return false;
        var b = ip.GetAddressBytes();
        if (b[0] == 10) return true;
        if (b[0] == 192 && b[1] == 168) return true;
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
        if (b[0] == 169 && b[1] == 254) return true;
        if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
        return false;
    }

    private static string? HeaderValue(string message, string name)
    {
        foreach (var line in message.Split('\n'))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            if (!line[..colon].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;
            return line[(colon + 1)..].Trim().Trim('\r');
        }
        return null;
    }

    private static string? XmlValue(string xml, string localName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private readonly struct SoapResult
    {
        public SoapResult(bool ok, string body, string? errorCode)
        {
            Ok = ok;
            Body = body;
            ErrorCode = errorCode;
        }
        public bool Ok { get; }
        public string Body { get; }
        public string? ErrorCode { get; }
    }
}

public sealed class NatPortLease : IDisposable
{
    private int _released;
    private readonly Action? _release;

    private NatPortLease(bool mapped, string protocol, string? externalAddress, int externalPort, string? failure, Action? release)
    {
        Mapped = mapped;
        Protocol = protocol;
        ExternalAddress = externalAddress;
        ExternalPort = externalPort;
        Failure = failure;
        _release = release;
    }

    public bool Mapped { get; }
    public string Protocol { get; }
    public string? ExternalAddress { get; }
    public int ExternalPort { get; }
    public string? Failure { get; }

    public static NatPortLease Failed(string reason)
        => new(false, "", null, 0, reason, null);

    public static NatPortLease Upnp(Uri control, string serviceUrn, string? externalAddress, int port)
        => new(true, "UPnP", externalAddress, port, null, () => DeleteUpnp(control, serviceUrn, port));

    public static NatPortLease Pcp(IPAddress gateway, IPAddress local, byte[] nonce, string? externalAddress, int externalPort, int internalPort)
        => new(true, "PCP", externalAddress, externalPort, null,
            () => DeletePcp(gateway, local, nonce, internalPort, externalPort));

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return;
        if (_release == null)
            return;
        try
        {
            // Stop() can run on the UI thread. Do not block it on the router for long.
            if (!Task.Run(_release).Wait(TimeSpan.FromSeconds(2)))
                Debug.WriteLine("Nat port map delete timed out.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Nat port map delete failed: " + ex.Message);
        }
    }

    private static void DeleteUpnp(Uri control, string serviceUrn, int port)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var args = new Dictionary<string, string>
            {
                ["NewRemoteHost"] = "",
                ["NewExternalPort"] = port.ToString(),
                ["NewProtocol"] = "TCP"
            };
            var body = new StringBuilder();
            body.Append("<?xml version=\"1.0\"?>");
            body.Append("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body>");
            body.Append("<u:DeletePortMapping xmlns:u=\"").Append(serviceUrn).Append("\">");
            foreach (var kv in args)
                body.Append('<').Append(kv.Key).Append('>').Append(kv.Value).Append("</").Append(kv.Key).Append('>');
            body.Append("</u:DeletePortMapping></s:Body></s:Envelope>");
            using var req = new HttpRequestMessage(HttpMethod.Post, control);
            req.Headers.TryAddWithoutValidation("SOAPAction", "\"" + serviceUrn + "#DeletePortMapping\"");
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "text/xml");
            using var resp = http.Send(req);
            if (!resp.IsSuccessStatusCode)
                Debug.WriteLine("UPnP DeletePortMapping HTTP " + (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("UPnP DeletePortMapping failed: " + ex.Message);
        }
    }

    private static void DeletePcp(IPAddress gateway, IPAddress local, byte[] nonce, int internalPort, int externalPort)
    {
        try
        {
            var packet = NatPortForward.BuildPcpMap(local, internalPort, externalPort, 0, nonce);
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            udp.Client.Bind(new IPEndPoint(local, 0));
            udp.Send(packet, packet.Length, new IPEndPoint(gateway, 5351));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("PCP delete failed: " + ex.Message);
        }
    }
}
