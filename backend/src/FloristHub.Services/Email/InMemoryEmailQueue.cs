using System.Threading.Channels;
using FloristHub.Services.Interfaces.Email;

namespace FloristHub.Services.Email;

public sealed class InMemoryEmailQueue : IEmailQueue
{
    private readonly Channel<QueuedEmail> _channel;

    public InMemoryEmailQueue()
    {
        _channel = Channel.CreateUnbounded<QueuedEmail>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(QueuedEmail message, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    internal ChannelReader<QueuedEmail> Reader => _channel.Reader;
}
