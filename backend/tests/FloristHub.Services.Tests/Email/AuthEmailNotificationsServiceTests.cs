using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.Services.Email;

namespace FloristHub.Services.Tests.Email;

public class AuthEmailNotificationsServiceTests
{
    [Fact]
    public void Welcome_registered_enqueues_email()
    {
        var queue = new FakeEmailQueue();
        var sut = new AuthEmailNotificationsService(queue);
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Flores", Slug = "flores" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "ana@demo.pt",
            UserName = "ana@demo.pt",
            FullName = "Ana",
            TenantId = tenant.Id
        };

        sut.WelcomeRegistered(user, tenant, Roles.Customer);

        var msg = Assert.Single(queue.Messages);
        Assert.Equal("ana@demo.pt", msg.To);
        Assert.Contains("Bem-vindo", msg.Subject, StringComparison.OrdinalIgnoreCase);
    }
}
