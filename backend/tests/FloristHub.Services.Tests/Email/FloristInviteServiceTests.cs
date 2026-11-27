using FloristHub.DAL.Entities;
using FloristHub.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FloristHub.Services.Tests.Email;

public class FloristInviteServiceTests
{
    [Fact]
    public async Task Send_invite_enqueues_email_and_returns_link_in_log_mode()
    {
        var queue = new FakeEmailQueue();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var florist = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "nova@demo.pt",
            UserName = "nova@demo.pt",
            FullName = "Nova"
        };
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Loja Nova", Slug = "loja-nova" };

        userManager
            .Setup(m => m.GeneratePasswordResetTokenAsync(florist))
            .ReturnsAsync("reset-token");
        userManager
            .Setup(m => m.UpdateSecurityStampAsync(florist))
            .ReturnsAsync(IdentityResult.Success);

        var inviteStore = new Mock<IFloristInviteStore>();
        inviteStore
            .Setup(s => s.IssueAsync(florist.Id, tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FloristInvite
            {
                Id = Guid.NewGuid(),
                UserId = florist.Id,
                TenantId = tenant.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(72),
            });

        var sut = new FloristInviteService(
            userManager.Object,
            queue,
            Options.Create(new EmailOptions { Provider = "Log", WebBaseUrl = "http://localhost:5173" }),
            NullLogger<FloristInviteService>.Instance,
            inviteStore.Object);

        var devLink = await sut.SendInviteAsync(florist, tenant);

        var msg = Assert.Single(queue.Messages);
        Assert.Equal("nova@demo.pt", msg.To);
        Assert.Contains("Convite", msg.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(devLink);
        Assert.Contains("convite", devLink!, StringComparison.OrdinalIgnoreCase);
    }
}
