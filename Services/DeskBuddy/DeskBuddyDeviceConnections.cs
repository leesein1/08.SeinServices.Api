using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using SeinServices.Api.Models.DeskBuddy.Responses;

namespace SeinServices.Api.Services.DeskBuddy;

// One bounded queue and one writer per socket; slow/offline displays never block /chat.
public sealed class DeskBuddyDeviceConnections
{
    private readonly ConcurrentDictionary<Guid, Channel<byte[]>> connections = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public int Count => connections.Count;

    public void Publish(ChatResponseDto answer)
    {
        if (connections.IsEmpty) return;
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "chat.answer", id = Guid.NewGuid().ToString("N"),
            reply = answer.Reply, emotion = answer.Emotion, searchUsed = answer.SearchUsed
        }, JsonOptions);
        foreach (var queue in connections.Values) queue.Writer.TryWrite(payload);
    }

    public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var queue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false
        });
        queue.Writer.TryWrite(JsonSerializer.SerializeToUtf8Bytes(new { type = "device.ready" }, JsonOptions));
        connections[id] = queue;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var writer = SendAsync(socket, queue.Reader, lifetime.Token);
        try
        {
            var reader = ReceiveAsync(socket, lifetime.Token);
            await Task.WhenAny(reader, writer);
            lifetime.Cancel();
            await Task.WhenAll(reader, writer);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (WebSocketException) { }
        finally
        {
            connections.TryRemove(id, out _);
            queue.Writer.TryComplete();
            lifetime.Cancel();
            // Do not keep an HTTP request alive while waiting for an unresponsive peer.
            socket.Abort();
        }
    }

    private static async Task SendAsync(WebSocket socket, ChannelReader<byte[]> queue, CancellationToken cancellationToken)
    {
        await foreach (var payload in queue.ReadAllAsync(cancellationToken))
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            try { await socket.SendAsync(payload.AsMemory(), WebSocketMessageType.Text, true, deadline.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { socket.Abort(); return; }
        }
    }

    private static async Task ReceiveAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[256];
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return;
            // This endpoint only pushes display events. WebSocket ping/pong is handled by the transport.
            if (result.Count > 0) return;
        }
    }
}
