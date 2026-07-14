using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebRTCSignalingServer;

internal sealed class SignalingHub(ILogger logger)
{
    private const int MaxMessageBytes = 256 * 1024;
    private const int MaxRoomSize = 2;
    private const int MaxChatLength = 1_000;
    private static readonly Regex RoomPattern = new("^[a-zA-Z0-9_-]{1,32}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, ClientConnection> clients = new();

    public int ConnectionCount => clients.Count;

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket bağlantısı gerekli.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var client = new ClientConnection(Guid.NewGuid().ToString("N"), socket);
        clients[client.Id] = client;
        logger.LogInformation("İstemci bağlandı: {ClientId}", client.Id);

        try
        {
            await ReceiveLoopAsync(client, context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("İstemci isteği iptal edildi: {ClientId}", client.Id);
        }
        catch (WebSocketException exception)
        {
            logger.LogWarning(exception, "WebSocket bağlantısı koptu: {ClientId}", client.Id);
        }
        finally
        {
            await RemoveClientAsync(client);
        }
    }

    private async Task ReceiveLoopAsync(ClientConnection client, CancellationToken cancellationToken)
    {
        while (client.Socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveTextAsync(client.Socket, cancellationToken);
            if (message is null)
            {
                return;
            }

            await HandleMessageAsync(client, message, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(
        ClientConnection client,
        string rawMessage,
        CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawMessage);
        }
        catch (JsonException)
        {
            await SendErrorAsync(client, "Geçersiz JSON mesajı.", cancellationToken);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (!TryGetText(root, "type", out var type))
            {
                await SendErrorAsync(client, "Mesaj tipi eksik.", cancellationToken);
                return;
            }

            switch (type)
            {
                case "join":
                    await JoinRoomAsync(client, root, cancellationToken);
                    break;
                case "offer":
                case "answer":
                    await ForwardSdpAsync(client, type, root, cancellationToken);
                    break;
                case "ice-candidate":
                    await ForwardIceCandidateAsync(client, root, cancellationToken);
                    break;
                case "chat":
                    await ForwardChatAsync(client, root, cancellationToken);
                    break;
                case "leave":
                    await LeaveRoomAsync(client, cancellationToken);
                    break;
                default:
                    await SendErrorAsync(client, $"Desteklenmeyen mesaj tipi: {type}", cancellationToken);
                    break;
            }
        }
    }

    private async Task JoinRoomAsync(
        ClientConnection client,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!TryGetText(root, "room", out var room) || !RoomPattern.IsMatch(room))
        {
            await SendErrorAsync(
                client,
                "Oda adı 1-32 karakter olmalı; yalnızca harf, sayı, _ ve - kullanılabilir.",
                cancellationToken);
            return;
        }

        var roomPeers = GetRoomPeers(room, client.Id).ToArray();
        if (roomPeers.Length >= MaxRoomSize)
        {
            await SendErrorAsync(client, "Bu oda dolu. Farklı bir oda adı deneyin.", cancellationToken);
            return;
        }

        if (!string.IsNullOrEmpty(client.Room))
        {
            await LeaveRoomAsync(client, cancellationToken);
        }

        client.Room = room;
        client.DisplayName = TryGetText(root, "displayName", out var displayName)
            ? displayName[..Math.Min(displayName.Length, 40)]
            : "Misafir";

        await client.SendAsync(new
        {
            type = "joined",
            clientId = client.Id,
            room,
            peerCount = roomPeers.Length,
        }, cancellationToken);

        await BroadcastAsync(room, client.Id, new
        {
            type = "peer-joined",
            clientId = client.Id,
            displayName = client.DisplayName,
        }, cancellationToken);

        logger.LogInformation("İstemci odaya katıldı: {ClientId} {Room}", client.Id, room);
    }

    private async Task ForwardSdpAsync(
        ClientConnection client,
        string type,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!EnsureJoined(client, out var room))
        {
            await SendErrorAsync(client, "Önce bir odaya katılmalısınız.", cancellationToken);
            return;
        }

        if (!TryGetText(root, "sdp", out var sdp))
        {
            await SendErrorAsync(client, "SDP içeriği eksik.", cancellationToken);
            return;
        }

        if (!SdpInspector.TryValidate(sdp, out var validationError))
        {
            await SendErrorAsync(client, validationError, cancellationToken);
            return;
        }

        var payload = new
        {
            type,
            from = client.Id,
            sdp,
        };
        await ForwardToTargetOrRoomAsync(client, room, root, payload, cancellationToken);
    }

    private async Task ForwardIceCandidateAsync(
        ClientConnection client,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!EnsureJoined(client, out var room))
        {
            await SendErrorAsync(client, "Önce bir odaya katılmalısınız.", cancellationToken);
            return;
        }

        if (!TryGetText(root, "candidate", out var candidate))
        {
            await SendErrorAsync(client, "ICE adayı eksik.", cancellationToken);
            return;
        }

        var payload = new
        {
            type = "ice-candidate",
            from = client.Id,
            candidate,
            sdpMid = TryGetText(root, "sdpMid", out var sdpMid) ? sdpMid : null,
            sdpMLineIndex = TryGetInt(root, "sdpMLineIndex", out var lineIndex) ? lineIndex : null,
        };
        await ForwardToTargetOrRoomAsync(client, room, root, payload, cancellationToken);
    }

    private async Task ForwardChatAsync(
        ClientConnection client,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!EnsureJoined(client, out var room))
        {
            await SendErrorAsync(client, "Önce bir odaya katılmalısınız.", cancellationToken);
            return;
        }

        if (!TryGetText(root, "content", out var content) || content.Length > MaxChatLength)
        {
            await SendErrorAsync(client, $"Mesaj 1-{MaxChatLength} karakter arasında olmalı.", cancellationToken);
            return;
        }

        await BroadcastAsync(room, client.Id, new
        {
            type = "chat",
            from = client.Id,
            displayName = client.DisplayName,
            content,
            sentAt = DateTimeOffset.UtcNow,
        }, cancellationToken);
    }

    private async Task ForwardToTargetOrRoomAsync(
        ClientConnection sender,
        string room,
        JsonElement root,
        object payload,
        CancellationToken cancellationToken)
    {
        if (TryGetText(root, "target", out var targetId)
            && clients.TryGetValue(targetId, out var target)
            && string.Equals(target.Room, room, StringComparison.Ordinal))
        {
            await target.SendAsync(payload, cancellationToken);
            return;
        }

        await BroadcastAsync(room, sender.Id, payload, cancellationToken);
    }

    private async Task LeaveRoomAsync(ClientConnection client, CancellationToken cancellationToken)
    {
        var room = client.Room;
        if (string.IsNullOrEmpty(room))
        {
            return;
        }

        client.Room = null;
        await BroadcastAsync(room, client.Id, new
        {
            type = "peer-left",
            clientId = client.Id,
        }, cancellationToken);
    }

    private async Task RemoveClientAsync(ClientConnection client)
    {
        clients.TryRemove(client.Id, out _);
        await LeaveRoomAsync(client, CancellationToken.None);
        await client.CloseAsync();
        logger.LogInformation("İstemci ayrıldı: {ClientId}", client.Id);
    }

    private IEnumerable<ClientConnection> GetRoomPeers(string room, string? excludedClientId = null) =>
        clients.Values.Where(client =>
            client.Id != excludedClientId
            && client.Socket.State == WebSocketState.Open
            && string.Equals(client.Room, room, StringComparison.Ordinal));

    private async Task BroadcastAsync(
        string room,
        string senderId,
        object payload,
        CancellationToken cancellationToken)
    {
        var sends = GetRoomPeers(room, senderId)
            .Select(client => client.SendAsync(payload, cancellationToken));
        await Task.WhenAll(sends);
    }

    private static bool EnsureJoined(ClientConnection client, out string room)
    {
        room = client.Room ?? string.Empty;
        return room.Length > 0;
    }

    private static bool TryGetText(JsonElement root, string property, out string value)
    {
        value = string.Empty;
        return root.TryGetProperty(property, out var element)
            && element.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value = element.GetString()!.Trim());
    }

    private static bool TryGetInt(JsonElement root, string property, out int? value)
    {
        value = null;
        if (!root.TryGetProperty(property, out var element) || !element.TryGetInt32(out var number))
        {
            return false;
        }

        value = number;
        return true;
    }

    private static async Task<string?> ReceiveTextAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8 * 1024];
        using var stream = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new WebSocketException("Yalnızca metin mesajları desteklenir.");
            }

            stream.Write(buffer, 0, result.Count);
            if (stream.Length > MaxMessageBytes)
            {
                throw new WebSocketException("Mesaj boyutu sınırı aşıldı.");
            }
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static Task SendErrorAsync(
        ClientConnection client,
        string message,
        CancellationToken cancellationToken) =>
        client.SendAsync(new { type = "error", message }, cancellationToken);

    private sealed class ClientConnection(string id, WebSocket socket)
    {
        private readonly SemaphoreSlim sendLock = new(1, 1);

        public string Id { get; } = id;
        public WebSocket Socket { get; } = socket;
        public string? Room { get; set; }
        public string DisplayName { get; set; } = "Misafir";

        public async Task SendAsync(object payload, CancellationToken cancellationToken)
        {
            if (Socket.State != WebSocketState.Open)
            {
                return;
            }

            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            await sendLock.WaitAsync(cancellationToken);
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
                }
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await Socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Bağlantı kapatıldı",
                    CancellationToken.None);
            }

            sendLock.Dispose();
        }
    }
}
