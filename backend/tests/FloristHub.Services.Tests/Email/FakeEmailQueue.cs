using FloristHub.Services.Interfaces.Email;

namespace FloristHub.Services.Tests.Email;

public sealed class FakeEmailQueue : IEmailQueue
{
    public List<QueuedEmail> Messages { get; } = new();

    public ValueTask EnqueueAsync(QueuedEmail message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return ValueTask.CompletedTask;
    }
}
