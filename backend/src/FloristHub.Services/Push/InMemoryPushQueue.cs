using System.Threading.Channels;

namespace FloristHub.Services.Push;

public sealed class InMemoryPushQueue : IPushQueue
{
    private readonly Channel<PushWorkItem> _channel = Channel.CreateUnbounded<PushWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public void Enqueue(PushWorkItem item) => _channel.Writer.TryWrite(item);

    internal ChannelReader<PushWorkItem> Reader => _channel.Reader;
}
