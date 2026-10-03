using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

/// <summary>
/// Talks to the in-memory lobby service. Deck JSON never leaves this process through here.
/// The socket URL is always ws://host:port/lobby. No other path is fetched.
/// </summary>
public sealed class LobbyPlayerInfo
{
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string DeckName { get; init; } = "";
    public string DeckHash { get; init; } = "";
}

public sealed class MatchmakingClient : IDisposable
{
    private readonly SemaphoreSlim _send = new(1, 1);
    private readonly object _waitGate = new();
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _read;
    private TaskCompletionSource<JsonElement>? _pending;
    private TaskCompletionSource<bool>? _matchWait;
    private TaskCompletionSource<bool>? _deckWait;

    public string? PlayerId { get; private set; }
    public string? DisplayName { get; private set; }
    public string Mode { get; private set; } = "sandbox";
    public string? Token { get; private set; }
    public string? MatchId { get; private set; }
    public string? ReportSecret { get; private set; }
    public string? Role { get; private set; }
    public string VersionNote { get; private set; } = "";
    public bool VersionKnown { get; private set; }
    public bool VersionBlocksStart { get; private set; }
    public bool InRoom { get; private set; }
    public string? RoomName { get; private set; }
    public string? DirectHost { get; private set; }
    public int DirectPort { get; private set; }

    public event Action<string>? StateChanged;
    public event Action<string>? PlayersChanged;
    public event Action<IReadOnlyList<LobbyPlayerInfo>>? PlayerListChanged;
    public event Action<string>? ChatChanged;
    public event Action<string, int>? AddressAnnounced;
    public event Action? SavesChanged;
    /// <summary>Guest only. The host load reply is not raised here. Blob is empty.</summary>
    public event Action<string, string, string>? ResumeOffered;
    /// <summary>Target only. from, challengeId. No mode yet.</summary>
    public event Action<string, string>? InviteReceived;
    /// <summary>Challenger only. who accepted, challengeId.</summary>
    public event Action<string, string>? InviteAccepted;
    /// <summary>Target only. from, inviteId.</summary>
    public event Action<string, string>? PrivateChatInviteReceived;
    /// <summary>Requester only. The other player was asked.</summary>
    public event Action<string>? PrivateChatInviteSent;
    /// <summary>Requester only. to, message. Decline is a separate event.</summary>
    public event Action<string, string>? PrivateChatInviteFailed;
    public event Action<string>? PrivateChatDeclined;
    public event Action<string>? PrivateChatInviteClosed;
    public event Action<IReadOnlyList<string>>? PrivateChatJoined;
    public event Action<string, string>? PrivateChatLine;
    public event Action<string>? PrivateChatEnded;
    /// <summary>Target only. from, requestId.</summary>
    public event Action<string, string>? TradeAsked;
    /// <summary>Requester only. The other player was asked.</summary>
    public event Action<string>? TradeAskSent;
    /// <summary>to, message. Nothing was moved.</summary>
    public event Action<string, string>? TradeAskFailed;
    public event Action<string>? TradeDeclined;
    /// <summary>sessionId, partner. Both players.</summary>
    public event Action<string, string>? TradeOpened;
    /// <summary>sessionId, mine, theirs, mineLocked, theirsLocked.</summary>
    public event Action<string, string[], string[], bool, bool>? TradeStateReceived;
    public event Action<string>? TradeDone;
    public event Action<string, string>? TradeAborted;
    public event Action<string, string>? TradeClosed;
    /// <summary>Target only. from, mode (account or sandbox), challengeId.</summary>
    public event Action<string, string, string>? ModeOfferReceived;
    /// <summary>Mode was declined. The invite stays open for another choice.</summary>
    public event Action<string>? ModeDeclined;
    public event Action? MatchSeated;
    public event Action<string, string>? FriendAskReceived;
    public event Action<string>? FriendNotice;
    public event Action<IReadOnlyList<string>, IReadOnlyList<bool>>? FriendsChanged;

    private bool _expectMatch;

    public IReadOnlyList<LobbySaveInfo> Saves { get; private set; } = Array.Empty<LobbySaveInfo>();

    public async Task ConnectAsync(string host, int port, string name, string mode, string? token, CancellationToken cancellationToken = default)
    {
        await ConnectSocketAsync(host, port, cancellationToken).ConfigureAwait(false);
        Mode = string.Equals(mode, "account", StringComparison.Ordinal) ? "account" : "sandbox";
        Token = Mode == "account" ? token : null;
        var hello = Envelope("hello");
        hello["name"] = name ?? "";
        hello["mode"] = Mode;
        await SendAsync(hello).ConfigureAwait(false);
    }

    public async Task ConnectSocketAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        Dispose();
        var socket = new ClientWebSocket();
        var cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}:{port}/lobby");
        await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        _socket = socket;
        _cts = cts;
        _read = Task.Run(() => ReadLoopAsync(socket, cts.Token));
    }

    public async Task CreateRoomAsync(string room, int gamePort)
    {
        await SendVersionAsync().ConfigureAwait(false);
        var env = Envelope("create");
        env["room"] = room;
        env["gamePort"] = gamePort;
        await SendAsync(env).ConfigureAwait(false);
    }

    public async Task JoinRoomAsync(string room)
    {
        await SendVersionAsync().ConfigureAwait(false);
        var env = Envelope("join");
        env["room"] = room;
        await SendAsync(env).ConfigureAwait(false);
    }

    private Task SendVersionAsync()
    {
        var env = Envelope("version");
        env["engine"] = EngineStamp.Id;
        env["cardHash"] = EngineStamp.CardHash;
        return SendAsync(env);
    }

    public Task RefreshAsync()
        => SendAsync(Envelope("list"));

    public Task SendChallengeAsync(string toName)
    {
        var env = Envelope("challenge");
        env["to"] = toName ?? "";
        return SendAsync(env);
    }

    public Task SendFriendRequestAsync(string toName)
    {
        var env = Envelope("friendRequest");
        env["to"] = toName ?? "";
        return SendAsync(env);
    }

    public Task ReplyFriendAsync(string requestId, bool accept)
    {
        var env = Envelope("friendReply");
        env["requestId"] = requestId ?? "";
        env["accept"] = accept;
        return SendAsync(env);
    }

    public Task RemoveFriendAsync(string name)
    {
        var env = Envelope("friendRemove");
        env["name"] = name ?? "";
        return SendAsync(env);
    }

    public Task RequestFriendsAsync()
    {
        return SendAsync(Envelope("friendList"));
    }

    public Task ReplyChallengeAsync(string challengeId, bool accept)
    {
        var env = Envelope("challengeReply");
        env["challengeId"] = challengeId ?? "";
        env["accept"] = accept;
        return SendAsync(env);
    }

    public Task SendModeAsync(string challengeId, string mode)
    {
        var env = Envelope("challengeMode");
        env["challengeId"] = challengeId ?? "";
        env["mode"] = mode ?? "";
        return SendAsync(env);
    }

    public Task ReplyModeAsync(string challengeId, bool accept)
    {
        if (accept)
            _expectMatch = true;
        var env = Envelope("modeReply");
        env["challengeId"] = challengeId ?? "";
        env["accept"] = accept;
        return SendAsync(env);
    }

    public Task CancelChallengeAsync(string challengeId)
    {
        _expectMatch = false;
        var env = Envelope("challengeCancel");
        env["challengeId"] = challengeId ?? "";
        return SendAsync(env);
    }

    public Task SendChatAsync(string text)
    {
        var env = Envelope("chat");
        env["text"] = text;
        return SendAsync(env);
    }

    public Task SendChatInviteAsync(string toName)
    {
        var env = Envelope("chatInvite");
        env["to"] = toName ?? "";
        return SendAsync(env);
    }

    public Task SendTradeAskAsync(string toName)
    {
        var env = Envelope("tradeAsk");
        env["to"] = toName ?? "";
        return SendAsync(env);
    }

    public Task ReplyTradeAskAsync(string requestId, bool accept)
    {
        var env = Envelope("tradeReply");
        env["requestId"] = requestId ?? "";
        env["accept"] = accept;
        return SendAsync(env);
    }

    public Task SendTradeSlotsAsync(string sessionId, IReadOnlyList<string> cards)
    {
        var env = Envelope("tradeSlots");
        env["sessionId"] = sessionId ?? "";
        var row = new string[4];
        for (var i = 0; i < 4; i++)
            row[i] = cards != null && i < cards.Count ? (cards[i] ?? "") : "";
        env["cards"] = row;
        return SendAsync(env);
    }

    public Task SendTradeLockAsync(string sessionId)
    {
        var env = Envelope("tradeLock");
        env["sessionId"] = sessionId ?? "";
        return SendAsync(env);
    }

    public Task CancelTradeAsync(string sessionId)
    {
        var env = Envelope("tradeCancel");
        env["sessionId"] = sessionId ?? "";
        return SendAsync(env);
    }

    public Task ReplyChatInviteAsync(string inviteId, bool accept)
    {
        var env = Envelope("chatInviteReply");
        env["inviteId"] = inviteId ?? "";
        env["accept"] = accept;
        return SendAsync(env);
    }

    public Task SendPrivateChatAsync(string text)
    {
        var env = Envelope("privateChat");
        env["text"] = text ?? "";
        return SendAsync(env);
    }

    public async Task SendDeckAsync(string deckName, string deckHash, string? cardIdsJson)
    {
        var env = Envelope("deck");
        env["deckName"] = deckName;
        env["deckHash"] = deckHash;
        if (!string.IsNullOrEmpty(cardIdsJson))
            env["cardIds"] = JsonSerializer.Deserialize<JsonElement>(cardIdsJson);
        TaskCompletionSource<bool> wait;
        lock (_waitGate)
        {
            if (_deckWait != null)
                throw new InvalidOperationException("Already waiting for a deck check.");
            wait = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _deckWait = wait;
        }
        try
        {
            await SendAsync(env).ConfigureAwait(false);
            await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        }
        catch
        {
            lock (_waitGate)
            {
                if (ReferenceEquals(_deckWait, wait))
                    _deckWait = null;
            }
            throw;
        }
    }

    public Task SendAddressAsync(string host, int port)
    {
        var env = Envelope("address");
        env["host"] = host;
        env["port"] = port;
        return SendAsync(env);
    }

    public Task LeaveAsync()
        => SendAsync(Envelope("leave"));

    public async Task BeginAsync()
    {
        TaskCompletionSource<bool> wait;
        lock (_waitGate)
        {
            if (_matchWait != null)
                throw new InvalidOperationException("Already waiting for a match freeze.");
            wait = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _matchWait = wait;
        }
        await SendAsync(Envelope("begin")).ConfigureAwait(false);
        await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(MatchId) || string.IsNullOrEmpty(ReportSecret))
            throw new InvalidOperationException("The server did not freeze the match.");
    }

    public async Task WaitForMatchAsync()
    {
        if (!string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(ReportSecret))
            return;
        TaskCompletionSource<bool> wait;
        lock (_waitGate)
        {
            if (!string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(ReportSecret))
                return;
            _matchWait ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            wait = _matchWait;
        }
        await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(MatchId) || string.IsNullOrEmpty(ReportSecret))
            throw new InvalidOperationException("The server did not freeze the match.");
    }

    public async Task<JsonElement> RegisterAsync(string name, string password)
        => await RequestAsync(new { type = "register", name, password }).ConfigureAwait(false);

    public async Task<JsonElement> LoginAsync(string name, string password)
        => await RequestAsync(new { type = "login", name, password }).ConfigureAwait(false);

    public async Task<JsonElement> DeleteAccountAsync(string password)
    {
        var env = Envelope("deleteAccount");
        env["password"] = password ?? "";
        return await RequestAsync(env).ConfigureAwait(false);
    }

    public async Task<JsonElement> SaveDeckAsync(string token, string name, string cardIdsJson)
        => await RequestAsync(new
        {
            type = "deckSave",
            token,
            name,
            cardIds = JsonSerializer.Deserialize<JsonElement>(cardIdsJson)
        }).ConfigureAwait(false);

    public async Task<JsonElement> ListDecksAsync(string token)
        => await RequestAsync(new { type = "deckList", token }).ConfigureAwait(false);

    public async Task<JsonElement> GetDeckAsync(string token, string name)
        => await RequestAsync(new { type = "deckGet", token, name }).ConfigureAwait(false);

    public async Task<JsonElement> GetPoolAsync(string token)
        => await RequestAsync(new { type = "get_pool", token }).ConfigureAwait(false);

    public async Task<JsonElement> BuyPackAsync(string token, string packType)
        => await RequestAsync(new { type = "buy_pack", token, packType }).ConfigureAwait(false);

    // DEV-ONLY HOOK. Remove for release. The server adds a fixed 500. This message sends no amount.
    public async Task<JsonElement> DevGrantLatinumAsync(string token)
        => await RequestAsync(new { type = "devGrantLatinum", token }).ConfigureAwait(false);

    public async Task<JsonElement> ReportAsync(string matchId, string secret, string winner, bool raiseTheStakes = false)
        => await RequestAsync(new { type = "report", matchId, secret, winner, raiseTheStakes }).ConfigureAwait(false);

    public async Task<JsonElement> TradeOfferAsync(string token, string toName, JsonElement offerCards, JsonElement requestCards)
        => await RequestAsync(new { type = "trade_offer", token, to = toName, offerCards, requestCards }).ConfigureAwait(false);

    public async Task<JsonElement> TradeListAsync(string token)
        => await RequestAsync(new { type = "trade_list", token }).ConfigureAwait(false);

    public async Task<JsonElement> TradeAcceptAsync(string token, long offerId)
        => await RequestAsync(new { type = "trade_accept", token, offerId }).ConfigureAwait(false);

    public async Task<JsonElement> TradeDeclineAsync(string token, long offerId)
        => await RequestAsync(new { type = "trade_decline", token, offerId }).ConfigureAwait(false);

    public async Task<JsonElement> SaveAutoAsync(string matchId, string secret, string blob)
        => await RequestAsync(new { type = "saveAuto", matchId, secret, blob }).ConfigureAwait(false);

    public async Task<JsonElement> SaveManualAsync(string matchId, string secret, string name, string blob)
        => await RequestAsync(new { type = "saveManual", matchId, secret, name, blob }).ConfigureAwait(false);

    public async Task<JsonElement> LoadAsync(string matchId, string kind, string name)
    {
        var env = Envelope("load");
        env["matchId"] = matchId;
        env["kind"] = kind;
        env["name"] = name ?? "";
        return await RequestAsync(env).ConfigureAwait(false);
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _socket?.Abort(); } catch { /* ignore */ }
        try { _socket?.Dispose(); } catch { /* ignore */ }
        _socket = null;
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
        PlayerId = null;
        DisplayName = null;
        Token = null;
        Mode = "sandbox";
        MatchId = null;
        ReportSecret = null;
        Role = null;
        InRoom = false;
        VersionNote = "";
        VersionKnown = false;
        VersionBlocksStart = false;
        RoomName = null;
        DirectHost = null;
        DirectPort = 0;
    }

    private async Task SendAsync(object payload)
    {
        var socket = _socket;
        if (socket == null || socket.State != WebSocketState.Open)
            throw new InvalidOperationException("Matchmaking is not connected.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await _send.WaitAsync().ConfigureAwait(false);
        try
        {
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _send.Release();
        }
    }

    private async Task ReadLoopAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    ms.Write(buffer, 0, result.Count);
                    if (ms.Length > 2097152)
                        return;
                }
                while (!result.EndOfMessage);

                Handle(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
            }
        }
        catch (OperationCanceledException)
        {
            // disposed
        }
        catch (WebSocketException)
        {
            StateChanged?.Invoke("Matchmaking connection lost. Direct IP and Localhost still work.");
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke("Matchmaking stopped: " + ex.Message);
        }
    }



    private void FinishDeckWait(string? error)
    {
        TaskCompletionSource<bool>? wait;
        lock (_waitGate)
        {
            wait = _deckWait;
            _deckWait = null;
        }
        if (wait == null)
            return;
        if (string.IsNullOrWhiteSpace(error))
            wait.TrySetResult(true);
        else
            wait.TrySetException(new InvalidOperationException(error));
    }

    private Dictionary<string, object?> Envelope(string type)
    {
        var env = new Dictionary<string, object?> { ["type"] = type };
        if (string.Equals(Mode, "account", StringComparison.Ordinal) && !string.IsNullOrEmpty(Token))
            env["token"] = Token;
        return env;
    }

    private async Task<JsonElement> RequestAsync(object payload)
    {
        var wait = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_waitGate)
        {
            if (_pending != null)
                throw new InvalidOperationException("A lobby request is already waiting.");
            _pending = wait;
        }
        try
        {
            await SendAsync(payload).ConfigureAwait(false);
            return await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        }
        finally
        {
            lock (_waitGate)
            {
                if (ReferenceEquals(_pending, wait))
                    _pending = null;
            }
        }
    }

    private void Handle(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (string.Equals(type, "resume", StringComparison.Ordinal))
            {
                MatchId = ReadString(root, "matchId");
                ReportSecret = ReadString(root, "secret");
                var blob = ReadString(root, "blob");
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    pending = _pending;
                    if (pending != null)
                        _pending = null;
                }
                if (pending != null)
                {
                    pending.TrySetResult(root.Clone());
                    return;
                }
                ResumeOffered?.Invoke(MatchId ?? "", ReportSecret ?? "", blob);
                return;
            }
            if (string.Equals(type, "match", StringComparison.Ordinal))
            {
                MatchId = ReadString(root, "matchId");
                ReportSecret = ReadString(root, "secret");
                TaskCompletionSource<bool>? wait;
                lock (_waitGate)
                {
                    wait = _matchWait;
                    _matchWait = null;
                }
                wait?.TrySetResult(true);
                return;
            }
            if (string.Equals(type, "deckOk", StringComparison.Ordinal))
            {
                FinishDeckWait(null);
                return;
            }
            if (string.Equals(type, "error", StringComparison.Ordinal))
            {
                var message = ReadString(root, "message");
                if (_deckWait != null)
                {
                    FinishDeckWait(string.IsNullOrWhiteSpace(message) ? "deck was not accepted" : message);
                    return;
                }
                TaskCompletionSource<bool>? matchWait;
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    matchWait = _matchWait;
                    _matchWait = null;
                    pending = _pending;
                    _pending = null;
                }
                if (matchWait != null)
                {
                    matchWait.TrySetException(new InvalidOperationException(message));
                    return;
                }
                if (pending != null)
                {
                    pending.TrySetResult(root.Clone());
                    return;
                }
                if (_expectMatch)
                    _expectMatch = false;
                StateChanged?.Invoke(message);
                return;
            }
            if (_pending != null && type is "auth" or "deckSaved" or "deckList" or "deckBody" or "pool" or "pack" or "latinum" or "report" or "saveStored" or "accountDeleted")
            {
                var copy = root.Clone();
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    pending = _pending;
                    _pending = null;
                }
                pending?.TrySetResult(copy);
                return;
            }
            switch (type)
            {
                case "welcome":
                    PlayerId = ReadString(root, "playerId");
                    var bound = ReadString(root, "name");
                    if (bound.Length > 0)
                        DisplayName = bound;
                    StateChanged?.Invoke("Connected to matchmaking. Create or join a room.");
                    break;
                case "rooms":
                    StateChanged?.Invoke(FormatRooms(root));
                    break;
                case "room":
                    ApplyRoom(root);
                    break;
                case "challenge":
                    InviteReceived?.Invoke(ReadString(root, "from"), ReadString(root, "challengeId"));
                    break;
                case "challengeSent":
                    StateChanged?.Invoke("Challenge sent to " + ReadString(root, "to") + ". Waiting for an answer.");
                    break;
                case "inviteAccepted":
                    InviteAccepted?.Invoke(ReadString(root, "by"), ReadString(root, "challengeId"));
                    break;
                case "inviteWaiting":
                    StateChanged?.Invoke("Waiting for the match choice.");
                    break;
                case "modeOffer":
                    ModeOfferReceived?.Invoke(ReadString(root, "from"), ReadString(root, "mode"), ReadString(root, "challengeId"));
                    break;
                case "modeOffered":
                    StateChanged?.Invoke("Mode offered. Waiting for an answer.");
                    break;
                case "modeDeclined":
                    _expectMatch = false;
                    ModeDeclined?.Invoke(ReadString(root, "message"));
                    break;
                case "matchReady":
                    _expectMatch = true;
                    break;
                case "challengeClosed":
                    _expectMatch = false;
                    StateChanged?.Invoke(ReadString(root, "message"));
                    break;
                case "friendAsk":
                    FriendAskReceived?.Invoke(ReadString(root, "from"), ReadString(root, "requestId"));
                    break;
                case "friendRequestSent":
                    FriendNotice?.Invoke("friendRequestSent:" + ReadString(root, "to"));
                    break;
                case "friendAccepted":
                    FriendNotice?.Invoke("friendAccepted:" + ReadString(root, "name"));
                    break;
                case "friendDeclined":
                    FriendNotice?.Invoke("friendDeclined:" + ReadString(root, "name"));
                    break;
                case "friendRemoved":
                    FriendNotice?.Invoke("friendRemoved:" + ReadString(root, "name"));
                    break;
                case "friendClosed":
                    FriendNotice?.Invoke("friendClosed:" + ReadString(root, "message"));
                    break;
                case "friends":
                    FriendsChanged?.Invoke(ReadNames(root), ReadFlags(root));
                    break;
                case "chatInvite":
                    PrivateChatInviteReceived?.Invoke(ReadString(root, "from"), ReadString(root, "inviteId"));
                    break;
                case "chatInviteSent":
                    PrivateChatInviteSent?.Invoke(ReadString(root, "to"));
                    break;
                case "chatInviteFailed":
                    PrivateChatInviteFailed?.Invoke(ReadString(root, "to"), ReadString(root, "message"));
                    break;
                case "chatInviteDeclined":
                    PrivateChatDeclined?.Invoke(ReadString(root, "by"));
                    break;
                case "chatInviteClosed":
                    PrivateChatInviteClosed?.Invoke(ReadString(root, "message"));
                    break;
                case "chatJoined":
                    PrivateChatJoined?.Invoke(ReadNames(root));
                    break;
                case "privateChat":
                    PrivateChatLine?.Invoke(ReadString(root, "from"), ReadString(root, "text"));
                    break;
                case "privateChatEnded":
                    PrivateChatEnded?.Invoke(ReadString(root, "message"));
                    break;
                case "tradeAsked":
                    TradeAsked?.Invoke(ReadString(root, "from"), ReadString(root, "requestId"));
                    break;
                case "tradeAskSent":
                    TradeAskSent?.Invoke(ReadString(root, "to"));
                    break;
                case "tradeAskFailed":
                    TradeAskFailed?.Invoke(ReadString(root, "to"), ReadString(root, "message"));
                    break;
                case "tradeDeclined":
                    TradeDeclined?.Invoke(ReadString(root, "by"));
                    break;
                case "tradeOpened":
                    TradeOpened?.Invoke(ReadString(root, "sessionId"), ReadString(root, "partner"));
                    break;
                case "tradeState":
                    TradeStateReceived?.Invoke(
                        ReadString(root, "sessionId"),
                        ReadCardRow(root, "mine"),
                        ReadCardRow(root, "theirs"),
                        root.TryGetProperty("mineLocked", out var mineLock) && mineLock.ValueKind == JsonValueKind.True,
                        root.TryGetProperty("theirsLocked", out var theirLock) && theirLock.ValueKind == JsonValueKind.True);
                    break;
                case "tradeDone":
                    TradeDone?.Invoke(ReadString(root, "sessionId"));
                    break;
                case "tradeAborted":
                    TradeAborted?.Invoke(ReadString(root, "sessionId"), ReadString(root, "message"));
                    break;
                case "tradeClosed":
                    TradeClosed?.Invoke(ReadString(root, "sessionId"), ReadString(root, "message"));
                    break;
                case "gone":
                    InRoom = false;
                    Role = null;
                    RoomName = null;
                    DirectHost = null;
                    DirectPort = 0;
                    Saves = Array.Empty<LobbySaveInfo>();
                    PlayersChanged?.Invoke("");
                    PlayerListChanged?.Invoke(Array.Empty<LobbyPlayerInfo>());
                    SavesChanged?.Invoke();
                    StateChanged?.Invoke(ReadString(root, "message"));
                    break;
                default:
                    StateChanged?.Invoke("Ignored lobby message.");
                    break;
            }
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke("Bad lobby message: " + ex.Message);
        }
    }

    private static IReadOnlyList<string> ReadNames(JsonElement root)
    {
        var names = new List<string>();
        if (root.TryGetProperty("names", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var name = item.GetString() ?? "";
                if (name.Length > 0)
                    names.Add(name);
            }
        }
        return names;
    }

    private static IReadOnlyList<bool> ReadFlags(JsonElement root)
    {
        var flags = new List<bool>();
        if (root.TryGetProperty("online", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                flags.Add(item.ValueKind == JsonValueKind.True);
        }
        return flags;
    }

    private void ApplyRoom(JsonElement root)
    {
        InRoom = true;
        Role = ReadString(root, "role");
        RoomName = ReadString(root, "name");
        var address = ReadString(root, "address");
        var port = root.TryGetProperty("port", out var portEl) && portEl.TryGetInt32(out var n) ? n : 0;
        if (!string.IsNullOrWhiteSpace(address) && port > 0)
        {
            var changed = !string.Equals(DirectHost, address, StringComparison.Ordinal) || DirectPort != port;
            DirectHost = address;
            DirectPort = port;
            if (changed)
                AddressAnnounced?.Invoke(address, port);
        }
        else if (DirectHost != null || DirectPort != 0)
        {
            DirectHost = null;
            DirectPort = 0;
        }

        var players = new StringBuilder();
        var playerList = new List<LobbyPlayerInfo>();
        if (root.TryGetProperty("players", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in arr.EnumerateArray())
            {
                var pName = ReadString(p, "name");
                var pRole = ReadString(p, "role");
                var pDeck = ReadString(p, "deckName");
                var hash = ReadString(p, "deckHash");
                var shortHash = hash.Length >= 8 ? hash[..8] : (hash.Length == 0 ? "no hash" : hash);
                playerList.Add(new LobbyPlayerInfo
                {
                    Name = pName,
                    Role = pRole,
                    DeckName = pDeck,
                    DeckHash = hash
                });
                if (players.Length > 0)
                    players.AppendLine();
                players.Append(pRole);
                players.Append(' ');
                players.Append(pName);
                players.Append(" — ");
                players.Append(pDeck.Length == 0 ? "no deck" : "deck");
                players.Append(" (");
                players.Append(shortHash);
                players.Append(')');
            }
        }
        PlayersChanged?.Invoke(players.ToString());
        PlayerListChanged?.Invoke(playerList);

        var chat = new StringBuilder();
        if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            foreach (var line in lines.EnumerateArray())
            {
                if (chat.Length > 0)
                    chat.AppendLine();
                chat.Append(ReadString(line, "from"));
                chat.Append(": ");
                chat.Append(ReadString(line, "text"));
            }
        }
        ChatChanged?.Invoke(chat.ToString());

        var saves = new List<LobbySaveInfo>();
        if (root.TryGetProperty("saves", out var saveEl) && saveEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in saveEl.EnumerateArray())
            {
                var kind = ReadString(row, "kind");
                if (kind is not ("auto" or "manual"))
                    continue;
                var id = ReadString(row, "matchId");
                if (id.Length == 0)
                    continue;
                saves.Add(new LobbySaveInfo
                {
                    MatchId = id,
                    Kind = kind,
                    Name = ReadString(row, "name"),
                    SavedUtc = ReadString(row, "savedUtc")
                });
            }
        }
        Saves = saves;
        SavesChanged?.Invoke();

        var saveNote = ReadString(root, "saveNote");
        VersionNote = ReadString(root, "versionNote");
        VersionKnown = root.TryGetProperty("versionKnown", out var known) && known.ValueKind == JsonValueKind.True;
        var versionOk = root.TryGetProperty("versionOk", out var okEl) && okEl.ValueKind == JsonValueKind.True;
        VersionBlocksStart = VersionKnown && !versionOk;

        var saveTail = Saves.Count > 0
            ? " Saved game for these two accounts. Play via relay, then Load game."
            : "";
        if (saveNote.Length > 0)
            StateChanged?.Invoke(saveNote);
        else if (VersionBlocksStart)
            StateChanged?.Invoke(VersionNote);
        else if (!VersionKnown)
            StateChanged?.Invoke((VersionNote.Length == 0 ? "Waiting for both version stamps." : VersionNote) + saveTail);
        else if (string.IsNullOrWhiteSpace(address) || port <= 0)
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct address not published yet." + saveTail);
        else
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct {address}:{port}." + saveTail);

        if (_expectMatch
            && !string.Equals(RoomName, "Lounge", StringComparison.OrdinalIgnoreCase)
            && Role is "host" or "guest")
        {
            _expectMatch = false;
            MatchSeated?.Invoke();
        }
    }

    private static string FormatRooms(JsonElement root)
    {
        if (!root.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array || rooms.GetArrayLength() == 0)
            return "No rooms.";
        var sb = new StringBuilder("Rooms: ");
        var first = true;
        foreach (var room in rooms.EnumerateArray())
        {
            if (!first)
                sb.Append(", ");
            first = false;
            sb.Append(ReadString(room, "name"));
            sb.Append(" (");
            sb.Append(room.TryGetProperty("players", out var n) && n.TryGetInt32(out var count) ? count : 0);
            sb.Append(ReadString(room, "open") == "true" || (room.TryGetProperty("open", out var open) && open.ValueKind == JsonValueKind.True) ? ", open" : ", full");
            sb.Append(')');
        }
        return sb.ToString();
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? "")
            : "";

    private static string[] ReadCardRow(JsonElement root, string name)
    {
        var cards = new string[] { "", "", "", "" };
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return cards;
        var index = 0;
        foreach (var item in arr.EnumerateArray())
        {
            if (index >= 4)
                break;
            cards[index++] = item.ValueKind == JsonValueKind.String ? (item.GetString() ?? "").Trim() : "";
        }
        return cards;
    }
}

public sealed class LobbySaveInfo
{
    public string MatchId { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string SavedUtc { get; init; } = "";
}
